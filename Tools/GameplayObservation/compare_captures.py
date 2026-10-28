"""Compare real Unity capture manifests without assigning visual/gameplay approval.

Usage: python compare_captures.py BEFORE AFTER --output Library/GameplayObservation/comparison-id
BEFORE/AFTER may be a manifest.json or its directory. Pillow is optional; --no-images
keeps validation, frame alignment and factual event summaries available with stdlib.
No capture, Unity execution, gameplay mutation or historical aggregation occurs here.
"""

from __future__ import annotations

import argparse
from collections import Counter
from dataclasses import dataclass
import hashlib
import json
import math
from pathlib import Path
import re
import struct
import sys
import tempfile
import zlib

try:
    from PIL import Image, ImageChops, ImageDraw, ImageStat
except ImportError:
    Image = ImageChops = ImageDraw = ImageStat = None


REQUIRED_CONDITIONS = (
    "scenario_version", "scene_guid", "seed", "weapon", "node_id", "enemy_count",
    "width", "height", "simulation_fps", "capture_every_frames", "input_schedule",
    "initial_actor_fingerprint", "camera_json", "render_json", "random_control",
)
LIMITATION = (
    "Pixel differences and event deltas describe observations, not visual improvement, "
    "game feel, balance or developer approval. Event transitions are sampled/callback "
    "evidence; they do not establish unobserved causes."
)


class CaptureValidationError(ValueError):
    """Input evidence is incomplete, corrupt or unsafe to compare."""


@dataclass
class Capture:
    manifest_path: Path
    manifest: dict
    conditions: dict
    frames: list[dict]
    events: list[dict]


def _integer(value, label, minimum=0):
    if type(value) is not int or value < minimum:
        raise CaptureValidationError(f"{label}: expected integer >= {minimum}")
    return value


def _number(value, label, minimum=0):
    if type(value) not in (int, float) or not math.isfinite(value) or value < minimum:
        raise CaptureValidationError(f"{label}: expected finite number >= {minimum}")
    return value


def _canonical(value):
    try:
        return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False, allow_nan=False)
    except (TypeError, ValueError) as error:
        raise CaptureValidationError(f"Non-JSON metadata: {error}") from error


def _read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeError, ValueError) as error:
        raise CaptureValidationError(f"Cannot read JSON {path}: {error}") from error


def _artifact(root, relative, expected_hash):
    if not isinstance(relative, str) or not relative or Path(relative).is_absolute():
        raise CaptureValidationError(f"Artifact path must be relative: {relative!r}")
    path = (root / relative).resolve()
    if not path.is_relative_to(root) or path == root:
        raise CaptureValidationError(f"Artifact escapes its run directory: {relative}")
    if not isinstance(expected_hash, str) or re.fullmatch(r"[0-9a-fA-F]{64}", expected_hash) is None:
        raise CaptureValidationError(f"Missing/invalid SHA-256: {relative}")
    try:
        content = path.read_bytes()
    except OSError as error:
        raise CaptureValidationError(f"Missing/unreadable artifact {relative}: {error}") from error
    if hashlib.sha256(content).hexdigest() != expected_hash.lower():
        raise CaptureValidationError(f"SHA-256 mismatch: {relative}")
    return path, content


def _png_dimensions(content, label):
    """Validate PNG chunks/CRC and noninterlaced scanlines, also without Pillow."""
    if content[:8] != b"\x89PNG\r\n\x1a\n":
        raise CaptureValidationError(f"Invalid PNG signature: {label}")
    offset, header, compressed, ended = 8, None, bytearray(), False
    while offset < len(content):
        if offset + 12 > len(content):
            raise CaptureValidationError(f"Truncated PNG chunk: {label}")
        length = struct.unpack_from(">I", content, offset)[0]
        kind = content[offset + 4:offset + 8]
        end = offset + 12 + length
        if end > len(content):
            raise CaptureValidationError(f"Truncated PNG payload: {label}")
        payload = content[offset + 8:offset + 8 + length]
        checksum = struct.unpack_from(">I", content, offset + 8 + length)[0]
        if zlib.crc32(kind + payload) & 0xffffffff != checksum:
            raise CaptureValidationError(f"PNG CRC mismatch: {label}")
        if header is None and kind != b"IHDR":
            raise CaptureValidationError(f"PNG must start with IHDR: {label}")
        if kind == b"IHDR":
            if header is not None or length != 13:
                raise CaptureValidationError(f"Invalid PNG IHDR: {label}")
            header = struct.unpack(">IIBBBBB", payload)
        elif kind == b"IDAT":
            compressed.extend(payload)
        elif kind == b"IEND":
            if length != 0 or end != len(content):
                raise CaptureValidationError(f"Invalid PNG ending: {label}")
            ended = True
            break
        offset = end
    if header is None or not ended or not compressed:
        raise CaptureValidationError(f"Incomplete PNG: {label}")
    width, height, depth, color, compression, filtering, interlace = header
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}.get(color)
    depths = {0: (1, 2, 4, 8, 16), 2: (8, 16), 3: (1, 2, 4, 8), 4: (8, 16), 6: (8, 16)}
    if not width or not height or channels is None or depth not in depths[color] or compression or filtering or interlace not in (0, 1):
        raise CaptureValidationError(f"Unsupported/invalid PNG format: {label}")
    if interlace:
        if Image is None:
            raise CaptureValidationError(f"Interlaced PNG requires Pillow for validation: {label}")
    else:
        stride = (width * channels * depth + 7) // 8 + 1
        expected = stride * height
        try:
            decoder = zlib.decompressobj()
            raw = decoder.decompress(compressed, expected + 1)
            if len(raw) != expected or not decoder.eof or decoder.unused_data:
                raise ValueError("unexpected decoded size or compressed stream ending")
            if any(raw[row * stride] > 4 for row in range(height)):
                raise ValueError("invalid scanline filter")
        except (zlib.error, ValueError) as error:
            raise CaptureValidationError(f"Corrupt PNG data {label}: {error}") from error
    return width, height


def load_capture(location):
    path = Path(location).resolve()
    if path.is_dir():
        path /= "manifest.json"
    manifest = _read_json(path)
    if not isinstance(manifest, dict) or type(manifest.get("schema_version")) is not int or manifest["schema_version"] != 1:
        raise CaptureValidationError("Expected manifest schema_version 1")
    for field in ("run_id", "scenario_id", "phase", "unity_version"):
        if not isinstance(manifest.get(field), str) or not manifest[field].strip():
            raise CaptureValidationError(f"Missing manifest field: {field}")
    if manifest.get("status") != "complete":
        raise CaptureValidationError(f"Run {manifest['run_id']} is not complete: {manifest.get('status')!r}")
    if not isinstance(manifest.get("provenance"), dict):
        raise CaptureValidationError("Missing provenance object")
    restoration = manifest.get("restoration")
    if not isinstance(restoration, dict):
        raise CaptureValidationError("Missing restoration object")
    if restoration.get("restored") is not True or restoration.get("success") is False:
        raise CaptureValidationError("Run does not confirm successful Editor restoration")
    conditions = manifest.get("conditions")
    if not isinstance(conditions, dict):
        raise CaptureValidationError("Missing conditions object")
    conditions = dict(conditions)
    for field in REQUIRED_CONDITIONS:
        if field not in conditions or conditions[field] is None or conditions[field] == "":
            raise CaptureValidationError(f"Missing required condition: {field}")
    for field in ("width", "height", "capture_every_frames"):
        _integer(conditions[field], f"conditions.{field}", 1)
    _integer(conditions["enemy_count"], "conditions.enemy_count")
    if _number(conditions["simulation_fps"], "conditions.simulation_fps") == 0:
        raise CaptureValidationError("conditions.simulation_fps must be positive")
    for field in ("camera_json", "render_json"):
        if isinstance(conditions[field], str):
            try:
                conditions[field] = json.loads(conditions[field])
            except ValueError as error:
                raise CaptureValidationError(f"Invalid JSON in conditions.{field}") from error
        if not isinstance(conditions[field], dict):
            raise CaptureValidationError(f"conditions.{field} must contain a JSON object")
    _canonical(conditions)
    frames = manifest.get("frames")
    if not isinstance(frames, list) or not frames:
        raise CaptureValidationError("Complete run must contain frames")
    root = path.parent.resolve()
    verified, seen_paths, previous = [], set(), (-1, -1, -1)
    for frame in frames:
        if not isinstance(frame, dict):
            raise CaptureValidationError("Invalid frame record")
        index = _integer(frame.get("index"), "frame.index")
        relative = _integer(frame.get("relative_frame"), "frame.relative_frame")
        timestamp = _number(frame.get("time"), "frame.time")
        if index <= previous[0] or relative <= previous[1] or timestamp < previous[2]:
            raise CaptureValidationError("Frames must have unique increasing index/relative_frame and nondecreasing time")
        previous = index, relative, timestamp
        for field in ("width", "height"):
            if _integer(frame.get(field), f"frame.{field}", 1) != conditions[field]:
                raise CaptureValidationError(f"Frame {index} {field} differs from capture conditions")
        artifact, content = _artifact(root, frame.get("path"), frame.get("sha256"))
        if artifact in seen_paths:
            raise CaptureValidationError("Frame records reuse an artifact path")
        seen_paths.add(artifact)
        if _png_dimensions(content, frame["path"]) != (frame["width"], frame["height"]):
            raise CaptureValidationError(f"PNG dimensions differ from frame metadata: {frame['path']}")
        if Image is not None:
            try:
                with Image.open(artifact) as image:
                    image.load()
            except (OSError, ValueError) as error:
                raise CaptureValidationError(f"Cannot decode PNG {frame['path']}: {error}") from error
        verified.append({**frame, "artifact": artifact})
    _, event_bytes = _artifact(root, manifest.get("events_path"), manifest.get("events_sha256"))
    events, previous_seq = [], -1
    try:
        for line_number, line in enumerate(event_bytes.decode("utf-8-sig").splitlines(), 1):
            if not line.strip():
                continue
            event = json.loads(line)
            if not isinstance(event, dict) or not isinstance(event.get("kind"), str) or not event["kind"]:
                raise CaptureValidationError(f"Invalid event kind on line {line_number}")
            _integer(event.get("frame"), f"event line {line_number}.frame")
            _number(event.get("t"), f"event line {line_number}.t")
            seq = _integer(event.get("seq"), f"event line {line_number}.seq")
            if seq <= previous_seq:
                raise CaptureValidationError("Event seq must be unique and increasing")
            previous_seq = seq
            if event.get("kind") == "ActorDamageReceived" and event.get("has_value") is True:
                _number(event.get("value"), "ActorDamageReceived.value")
            _canonical(event)
            events.append(event)
    except (UnicodeError, ValueError) as error:
        if isinstance(error, CaptureValidationError):
            raise
        raise CaptureValidationError(f"Invalid events JSONL: {error}") from error
    return Capture(path, manifest, conditions, verified, events)


def _event_summary(events):
    damage = [event for event in events if event["kind"] == "ActorDamageReceived" and event.get("has_value") is True]
    by_actor = {}
    for event in damage:
        actor = str(event.get("actor", "unspecified"))
        by_actor[actor] = by_actor.get(actor, 0) + event["value"]
    return {
        "events": len(events), "kinds": dict(sorted(Counter(event["kind"] for event in events).items())),
        "confirmed_hp_received": sum(event["value"] for event in damage),
        "confirmed_hp_received_by_actor": by_actor,
        "damage_basis": "Only ActorDamageReceived callbacks with has_value=true; no OnHit/ProtocolImpact double counting.",
        "timeline": events,
    }


def compare(before, after, allow_conditions=()):
    allowed = set(allow_conditions)
    unknown = allowed - set(REQUIRED_CONDITIONS) - set(before.conditions) - set(after.conditions)
    if unknown:
        raise CaptureValidationError(f"Unknown --allow-condition: {', '.join(sorted(unknown))}")
    differences = []
    for field in sorted(set(before.conditions) | set(after.conditions)):
        left, right = before.conditions.get(field), after.conditions.get(field)
        if _canonical(left) != _canonical(right):
            differences.append({"condition": field, "before": left, "after": right, "allowed_explicitly": field in allowed})
    identity_differences = []
    for field in ("scenario_id", "unity_version"):
        if before.manifest[field] != after.manifest[field]:
            identity_differences.append({"field": field, "before": before.manifest[field], "after": after.manifest[field]})
    left_frames = {frame["relative_frame"]: frame for frame in before.frames}
    right_frames = {frame["relative_frame"]: frame for frame in after.frames}
    aligned, gaps = [], []
    for relative in sorted(set(left_frames) | set(right_frames)):
        left, right = left_frames.get(relative), right_frames.get(relative)
        if left is None or right is None:
            gaps.append({"relative_frame": relative, "missing_from": "before" if left is None else "after"})
            continue
        aligned.append({"relative_frame": relative, "before_index": left["index"], "after_index": right["index"],
                        "before_time": left["time"], "after_time": right["time"],
                        "time_delta": right["time"] - left["time"],
                        "before_path": left["path"], "after_path": right["path"]})
    comparable = not identity_differences and not any(not item["allowed_explicitly"] for item in differences) and bool(aligned)
    return {
        "schema_version": 1, "status": "comparable" if comparable else "incomparable", "comparable": comparable,
        "before": {"run_id": before.manifest["run_id"], "phase": before.manifest["phase"], "manifest": str(before.manifest_path), "provenance": before.manifest["provenance"]},
        "after": {"run_id": after.manifest["run_id"], "phase": after.manifest["phase"], "manifest": str(after.manifest_path), "provenance": after.manifest["provenance"]},
        "identity_differences": identity_differences, "condition_differences": differences,
        "explicitly_allowed_conditions": sorted(allowed),
        "override_notice": "Allowed conditions are caller-declared authorized variables, remain visible and limit attribution; this tool cannot verify human authorization.",
        "alignment_basis": "Exact relative rendered-frame number; never shifted to impact/event timing.",
        "aligned_frames": aligned, "gaps": gaps,
        "events": {"before": _event_summary(before.events), "after": _event_summary(after.events)},
        "images": [], "limitations": [LIMITATION, "Gaps remain unobserved; aligned frames do not certify deterministic physics or human acceptance."],
    }


def _image_outputs(before, after, report, directory):
    if not report["comparable"]:
        return
    if Image is None:
        report["limitations"].append("Pillow unavailable: PNG integrity and metadata validated, no contact sheet/diff rendered.")
        return
    left = {frame["relative_frame"]: frame for frame in before.frames}
    right = {frame["relative_frame"]: frame for frame in after.frames}
    keys = sorted(set(left) | set(right))
    # Every sample is represented; pages keep labels readable and memory bounded.
    thumb_w, thumb_h, label_h, per_page = 480, 270, 42, 8
    metrics = []
    for page_start in range(0, len(keys), per_page):
        page = keys[page_start:page_start + per_page]
        sheet = Image.new("RGB", (thumb_w * 3, (thumb_h + label_h) * len(page)), "#171b25")
        draw = ImageDraw.Draw(sheet)
        for row, relative in enumerate(page):
            y = row * (thumb_h + label_h)
            pair = []
            for column, (name, source) in enumerate((("BEFORE", left), ("AFTER", right))):
                frame = source.get(relative)
                if frame is None:
                    draw.text((column * thumb_w + 8, y + 8), f"{name}: GAP frame {relative}", fill="#ffbb88")
                    pair.append(None)
                    continue
                with Image.open(frame["artifact"]) as image:
                    rgb = image.convert("RGB")
                pair.append(rgb)
                thumb = rgb.copy()
                thumb.thumbnail((thumb_w, thumb_h))
                sheet.paste(thumb, (column * thumb_w, y + label_h))
                draw.text((column * thumb_w + 8, y + 8), f"{name} frame {relative} t={frame['time']:.4f}s", fill="white")
            if pair[0] is not None and pair[1] is not None:
                if pair[0].size != pair[1].size:
                    draw.text((thumb_w * 2 + 8, y + 8), "DIFF unavailable: dimensions changed", fill="#ffbb88")
                    continue
                diff = ImageChops.difference(pair[0], pair[1])
                stat = ImageStat.Stat(diff)
                metrics.append({"relative_frame": relative, "mean_absolute_rgb_difference_0_255": sum(stat.mean) / 3})
                diff.thumbnail((thumb_w, thumb_h))
                sheet.paste(diff, (thumb_w * 2, y + label_h))
                draw.text((thumb_w * 2 + 8, y + 8), "ABS PIXEL DIFF: does not measure improvement", fill="white")
            else:
                draw.text((thumb_w * 2 + 8, y + 8), "DIFF unavailable: GAP", fill="#ffbb88")
        filename = f"contact-sheet-{page_start // per_page + 1:03d}.png"
        sheet.save(directory / filename)
        report["images"].append(filename)
    report["pixel_metrics"] = metrics


def write_comparison(before_location, after_location, output, allow_conditions=(), images=True):
    """Atomically publish one new report directory, never overwrite input evidence."""
    output = Path(output).resolve()
    inputs = []
    for location in (before_location, after_location):
        path = Path(location).resolve()
        inputs.append(path if path.is_dir() else path.parent)
    for root in inputs:
        if output.is_relative_to(root) or root.is_relative_to(output):
            raise CaptureValidationError("Output must be separate from both input run directories")
    if output.exists():
        raise CaptureValidationError(f"Output already exists; choose a new comparison directory: {output}")
    before = after = None
    try:
        before, after = load_capture(before_location), load_capture(after_location)
        report = compare(before, after, allow_conditions)
    except CaptureValidationError as error:
        report = {"schema_version": 1, "status": "invalid", "comparable": False,
                  "error": str(error), "images": [], "limitations": [LIMITATION]}
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".comparison-staging-", dir=output.parent) as temporary:
        staging = Path(temporary) / "result"
        staging.mkdir()
        if images and before is not None and after is not None:
            _image_outputs(before, after, report, staging)
        elif not images:
            report["limitations"].append("Image rendering disabled by --no-images; no visual examination is implied.")
        (staging / "comparison.json").write_text(json.dumps(report, indent=2, ensure_ascii=False, allow_nan=False) + "\n", encoding="utf-8")
        if output.exists():
            raise CaptureValidationError("Output appeared during comparison; refusing to overwrite it")
        staging.rename(output)
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before")
    parser.add_argument("after")
    parser.add_argument("--output", required=True, help="New directory, separate from input runs")
    parser.add_argument("--allow-condition", action="append", default=[], metavar="KEY",
                        help="Explicit authorized changed variable; remains disclosed in report")
    parser.add_argument("--no-images", action="store_true", help="Validate/compare using only stdlib metadata and event analysis")
    args = parser.parse_args(argv)
    try:
        result = write_comparison(args.before, args.after, args.output, args.allow_condition, not args.no_images)
    except (CaptureValidationError, OSError, ValueError) as error:
        print(f"Comparison failed; no success report published: {error}", file=sys.stderr)
        return 2
    print(json.dumps({"status": result["status"], "comparable": result["comparable"],
                      "report": str(Path(args.output).resolve() / "comparison.json")}, ensure_ascii=False))
    return 0 if result["comparable"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
