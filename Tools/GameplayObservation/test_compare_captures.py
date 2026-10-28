"""Contract tests using SYNTHETIC PNG/JSON fixtures, never gameplay evidence."""

from copy import deepcopy
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest import mock
import zlib


spec = importlib.util.spec_from_file_location("compare_captures", Path(__file__).with_name("compare_captures.py"))
comparison = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = comparison
spec.loader.exec_module(comparison)


def synthetic_png(width=8, height=6, value=32):
    def chunk(kind, payload):
        return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xffffffff)
    rows = (b"\0" + bytes([value, value, value]) * width) * height
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)) +
            chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b""))


class ComparisonContractTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="synthetic-capture-tests-")
        self.root = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def fixture(self, name, relative_frames=(0, 2), value=32, events=None):
        root = self.root / name
        root.mkdir()
        records = []
        for index, relative in enumerate(relative_frames):
            content = synthetic_png(value=value)
            path = f"frame-{index:04d}.png"
            (root / path).write_bytes(content)
            records.append({"index": index, "relative_frame": relative, "time": relative / 60,
                            "path": path, "sha256": hashlib.sha256(content).hexdigest(), "width": 8, "height": 6})
        if events is None:
            events = [{"seq": 0, "frame": 1, "t": 1 / 60, "unscaled_t": 1 / 60, "kind": "ActorDamageReceived",
                       "actor": "SYNTHETIC-enemy-001", "value": 7, "has_value": True,
                       "source": "synthetic-test", "evidence_kind": "callback"}]
        event_bytes = ("".join(json.dumps(item) + "\n" for item in events)).encode("utf-8")
        (root / "events.jsonl").write_bytes(event_bytes)
        manifest = {"schema_version": 1, "run_id": name, "status": "complete", "scenario_id": "SYNTHETIC_TEST_ONLY",
                    "phase": "before", "unity_version": "SYNTHETIC-NO-UNITY",
                    "provenance": {"code_hash": "old", "notice": "Synthetic contract fixture; not gameplay evidence"},
                    "conditions": {"scenario_version": 1, "scene_guid": "synthetic", "seed": 123, "weapon": "Gauntlet",
                                   "node_id": 1, "enemy_count": 5, "width": 8, "height": 6, "simulation_fps": 60,
                                   "capture_every_frames": 2, "input_schedule": [{"frame": 1, "command": "Q"}],
                                   "initial_actor_fingerprint": "synthetic-hash", "camera_json": '{"x":1,"y":2}',
                                   "render_json": '{"postprocess":true}', "random_control": "synthetic seeded"},
                    "frames": records, "events_path": "events.jsonl", "events_sha256": hashlib.sha256(event_bytes).hexdigest(),
                    "restoration": {"restored": True, "diagnostics": "Synthetic"}}
        self.save(root, manifest)
        return root, manifest

    @staticmethod
    def save(root, manifest):
        (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")

    def test_different_code_provenance_and_before_after_labels_are_allowed(self):
        before, _ = self.fixture("before")
        after, manifest = self.fixture("after")
        manifest["provenance"]["code_hash"] = "different-authorized-code"
        manifest["phase"] = "after"
        self.save(after, manifest)
        result = comparison.compare(comparison.load_capture(before), comparison.load_capture(after))
        self.assertTrue(result["comparable"])
        self.assertEqual(result["after"]["provenance"]["code_hash"], "different-authorized-code")
        self.assertEqual(result["after"]["phase"], "after")

    def test_json_camera_field_order_does_not_change_conditions(self):
        before, _ = self.fixture("before")
        after, manifest = self.fixture("after")
        manifest["conditions"]["camera_json"] = '{"y":2,"x":1}'
        self.save(after, manifest)
        self.assertTrue(comparison.compare(comparison.load_capture(before), comparison.load_capture(after))["comparable"])

    def test_seed_camera_resolution_postprocess_and_input_changes_are_blocked(self):
        before, _ = self.fixture("before")
        capture = comparison.load_capture(before)
        for field, changed in (("seed", 124), ("camera_json", {"x": 3, "y": 2}),
                               ("width", 9), ("render_json", {"postprocess": False}), ("input_schedule", [])):
            with self.subTest(field=field):
                after = deepcopy(capture)
                after.conditions[field] = changed
                result = comparison.compare(capture, after)
                self.assertFalse(result["comparable"])
                self.assertEqual(result["condition_differences"][0]["condition"], field)

    def test_authorized_condition_override_is_explicit_and_does_not_hide_other_changes(self):
        root, _ = self.fixture("before")
        before = comparison.load_capture(root)
        after = deepcopy(before)
        after.conditions["render_json"] = {"postprocess": False}
        result = comparison.compare(before, after, ["render_json"])
        self.assertTrue(result["comparable"])
        self.assertTrue(result["condition_differences"][0]["allowed_explicitly"])
        after.conditions["seed"] = 456
        self.assertFalse(comparison.compare(before, after, ["render_json"])["comparable"])
        with self.assertRaises(comparison.CaptureValidationError):
            comparison.compare(before, after, ["typo"])

    def test_missing_same_condition_on_both_sides_is_invalid(self):
        for field in comparison.REQUIRED_CONDITIONS:
            with self.subTest(field=field):
                root, manifest = self.fixture("missing-" + field)
                del manifest["conditions"][field]
                self.save(root, manifest)
                with self.assertRaisesRegex(comparison.CaptureValidationError, field):
                    comparison.load_capture(root)

    def test_only_complete_runs_with_confirmed_restoration_are_valid(self):
        for index, (state, restoration) in enumerate((("failed", {"restored": True}), ("cancelled", {"restored": True}),
                                                    ("complete", {"restored": False}), ("complete", {}))):
            with self.subTest(state=state, restoration=restoration):
                root, manifest = self.fixture(f"state-{index}")
                manifest["status"], manifest["restoration"] = state, restoration
                self.save(root, manifest)
                with self.assertRaises(comparison.CaptureValidationError):
                    comparison.load_capture(root)

    def test_manifest_identity_and_unity_changes_cannot_be_silently_compared(self):
        root, _ = self.fixture("before")
        before = comparison.load_capture(root)
        for field in ("scenario_id", "unity_version"):
            after = deepcopy(before)
            after.manifest[field] = "different"
            self.assertFalse(comparison.compare(before, after)["comparable"])

    def test_artifact_traversal_and_absolute_paths_are_rejected(self):
        for index, bad in enumerate(("../outside.png", str(self.root / "outside.png"))):
            root, manifest = self.fixture(f"escape-{index}")
            manifest["frames"][0]["path"] = bad
            self.save(root, manifest)
            with self.assertRaises(comparison.CaptureValidationError):
                comparison.load_capture(root)

    def test_missing_frames_and_events_and_hash_mismatch_are_rejected(self):
        for index, target in enumerate(("frame-0000.png", "events.jsonl", "hash")):
            root, manifest = self.fixture(f"missing-{index}")
            if target == "hash":
                manifest["frames"][0]["sha256"] = "0" * 64
                self.save(root, manifest)
            else:
                (root / target).unlink()
            with self.assertRaises(comparison.CaptureValidationError):
                comparison.load_capture(root)

    def test_corrupt_png_fails_even_when_manifest_hash_matches_without_pillow(self):
        root, manifest = self.fixture("corrupt")
        content = synthetic_png()[:-4]
        (root / "frame-0000.png").write_bytes(content)
        manifest["frames"][0]["sha256"] = hashlib.sha256(content).hexdigest()
        self.save(root, manifest)
        with mock.patch.object(comparison, "Image", None):
            with self.assertRaisesRegex(comparison.CaptureValidationError, "PNG"):
                comparison.load_capture(root)

    def test_png_dimensions_must_match_metadata(self):
        root, manifest = self.fixture("dimensions")
        content = synthetic_png(width=9)
        (root / "frame-0000.png").write_bytes(content)
        manifest["frames"][0]["sha256"] = hashlib.sha256(content).hexdigest()
        self.save(root, manifest)
        with self.assertRaisesRegex(comparison.CaptureValidationError, "dimensions"):
            comparison.load_capture(root)

    def test_duplicate_unordered_frames_and_reused_paths_are_invalid(self):
        mutations = (lambda frames: frames.reverse(),
                     lambda frames: frames[1].update(relative_frame=frames[0]["relative_frame"]),
                     lambda frames: frames[1].update(index=frames[0]["index"]),
                     lambda frames: frames[1].update(path=frames[0]["path"]))
        for index, mutate in enumerate(mutations):
            root, manifest = self.fixture(f"unordered-{index}")
            mutate(manifest["frames"])
            self.save(root, manifest)
            with self.assertRaises(comparison.CaptureValidationError):
                comparison.load_capture(root)

    def test_alignment_keeps_gaps_and_timing_differences_instead_of_shifting_to_hit(self):
        before, _ = self.fixture("before", relative_frames=(0, 2, 4))
        after, manifest = self.fixture("after", relative_frames=(0, 2, 6))
        manifest["frames"][1]["time"] += 0.01
        self.save(after, manifest)
        result = comparison.compare(comparison.load_capture(before), comparison.load_capture(after))
        self.assertEqual([frame["relative_frame"] for frame in result["aligned_frames"]], [0, 2])
        self.assertEqual(result["gaps"], [{"relative_frame": 4, "missing_from": "after"}, {"relative_frame": 6, "missing_from": "before"}])
        self.assertAlmostEqual(result["aligned_frames"][1]["time_delta"], 0.01)
        self.assertIn("never shifted", result["alignment_basis"])

    def test_no_overlapping_render_frames_is_incomparable(self):
        before, _ = self.fixture("before", relative_frames=(0, 2))
        after, _ = self.fixture("after", relative_frames=(1, 3))
        self.assertFalse(comparison.compare(comparison.load_capture(before), comparison.load_capture(after))["comparable"])

    def test_hp_summary_does_not_double_count_protocol_hits_or_estimated_values(self):
        events = [{"seq": index, "frame": index, "t": index / 60, "kind": kind, "value": value,
                   "has_value": has_value, "actor": "synthetic-target"}
                  for index, (kind, value, has_value) in enumerate((("ActorDamageReceived", 7, True),
                      ("OnHit", 7, True), ("ProtocolImpact", 7, True), ("ActorDamageReceived", 70, False),
                      ("ActorDamageReceived", 3, True)))]
        root, _ = self.fixture("events", events=events)
        summary = comparison._event_summary(comparison.load_capture(root).events)
        self.assertEqual(summary["confirmed_hp_received"], 10)
        self.assertEqual(summary["events"], 5)

    def test_nonfinite_event_values_and_duplicate_sequence_are_invalid(self):
        for index, events in enumerate((
                [{"seq": 0, "frame": 0, "t": 0, "kind": "ActorDamageReceived", "has_value": True, "value": float("nan")}],
                [{"seq": 0, "frame": 0, "t": 0, "kind": "sample"}] * 2)):
            root, _ = self.fixture(f"bad-events-{index}", events=events)
            with self.assertRaises(comparison.CaptureValidationError):
                comparison.load_capture(root)

    def test_standard_library_fallback_publishes_factual_report_without_images(self):
        before, _ = self.fixture("before")
        after, _ = self.fixture("after")
        with mock.patch.object(comparison, "Image", None):
            result = comparison.write_comparison(before, after, self.root / "fallback")
        self.assertTrue(result["comparable"])
        self.assertEqual(result["images"], [])
        self.assertTrue((self.root / "fallback/comparison.json").exists())
        self.assertTrue(any("Pillow unavailable" in item for item in result["limitations"]))

    @unittest.skipIf(comparison.Image is None, "Pillow optional, unavailable")
    def test_contact_sheet_contains_gaps_and_difference_is_not_quality_approval(self):
        before, _ = self.fixture("before", relative_frames=(0, 2))
        after, _ = self.fixture("after", relative_frames=(0, 4), value=60)
        output = self.root / "comparison"
        result = comparison.write_comparison(before, after, output)
        self.assertTrue(result["images"])
        for path in result["images"]:
            with comparison.Image.open(output / path) as image:
                image.load()
                self.assertGreater(image.height, 270)
        self.assertGreater(result["pixel_metrics"][0]["mean_absolute_rgb_difference_0_255"], 0)
        self.assertIn("not visual improvement", result["limitations"][0])

    def test_invalid_input_publishes_only_invalid_report_no_success_or_image(self):
        before, manifest = self.fixture("before")
        after, _ = self.fixture("after")
        manifest["status"] = "failed"
        self.save(before, manifest)
        result = comparison.write_comparison(before, after, self.root / "invalid")
        self.assertEqual(result["status"], "invalid")
        self.assertFalse(result["comparable"])
        self.assertEqual([path.name for path in (self.root / "invalid").iterdir()], ["comparison.json"])

    def test_image_generation_failure_leaves_no_output_or_staging_and_inputs_unchanged(self):
        before, _ = self.fixture("before")
        after, _ = self.fixture("after")
        snapshot = {path: path.read_bytes() for root in (before, after) for path in root.iterdir()}
        output = self.root / "failure"
        with mock.patch.object(comparison, "_image_outputs", side_effect=OSError("synthetic disk failure")):
            with self.assertRaises(OSError):
                comparison.write_comparison(before, after, output)
        self.assertFalse(output.exists())
        self.assertFalse(list(self.root.glob(".comparison-staging-*")))
        self.assertEqual({path: path.read_bytes() for path in snapshot}, snapshot)

    def test_output_cannot_overwrite_run_nested_evidence_or_existing_report(self):
        before, _ = self.fixture("before")
        after, _ = self.fixture("after")
        existing = self.root / "existing"
        existing.mkdir()
        for output in (before, before / "nested", self.root, existing):
            with self.subTest(output=output):
                with self.assertRaises(comparison.CaptureValidationError):
                    comparison.write_comparison(before, after, output)


if __name__ == "__main__":
    unittest.main()
