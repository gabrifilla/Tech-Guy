"""Thin client for the installed Unity MCP and CombatPolishPreview observation mode.

This does not implement a test runner, capture engine, or gameplay evaluator.
The Unity Editor owns rendering and execution; comparison is a separate command.
"""
from __future__ import annotations

import argparse
import json
import math
import os
from pathlib import Path
import sys
import time
import urllib.error
import urllib.request


PROJECT_ROOT = Path(__file__).resolve().parents[2]


class ObservationError(RuntimeError):
    pass


def decode_payload(value):
    """Unwrap MCP resources/tool results without assuming a Unity tool schema."""
    if not isinstance(value, dict):
        return value
    if value.get("isError"):
        raise ObservationError(json.dumps(value, ensure_ascii=False))
    if "structuredContent" in value:
        return decode_payload(value["structuredContent"])
    for collection in ("contents", "content"):
        if collection in value:
            for item in reversed(value[collection]):
                if "text" in item:
                    try:
                        return decode_payload(json.loads(item["text"]))
                    except json.JSONDecodeError:
                        continue
    if "result" in value and "success" not in value and len(value) < 4:
        return decode_payload(value["result"])
    return value


class UnityMcp:
    def __init__(self, endpoint="http://127.0.0.1:8080/mcp", timeout=45):
        self.endpoint = endpoint
        self.timeout = timeout
        self.session = None
        self.sequence = 0
        self.tools = {}
        self.resource_uris = set()

    def rpc(self, method, params=None, notification=False):
        self.sequence += 1
        body = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            body["params"] = params
        if not notification:
            body["id"] = self.sequence
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers["Mcp-Session-Id"] = self.session
        request = urllib.request.Request(self.endpoint, json.dumps(body).encode("utf-8"), headers, method="POST")
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as response:
                self.session = response.headers.get("Mcp-Session-Id", self.session)
                raw = response.read().decode("utf-8")
        except (urllib.error.URLError, TimeoutError) as exc:
            raise ObservationError("Unity MCP unavailable: " + str(exc) +
                ". Start the existing MCP server/bridge in Unity; no replacement server is created by this client.") from exc
        if not raw.strip():
            return None
        if raw.lstrip().startswith(("{", "[")):
            events = [json.loads(raw)]
        else:
            events, data = [], []
            for line in raw.splitlines() + [""]:
                if line.startswith("data:"):
                    data.append(line[5:].removeprefix(" "))
                elif not line and data:
                    events.append(json.loads("\n".join(data)))
                    data = []
        if any(not isinstance(item, dict) for item in events):
            raise ObservationError("Invalid JSON-RPC response object")
        response = next((item for item in reversed(events) if item.get("id") == body.get("id")), None)
        if response is None:
            if notification:
                return None
            raise ObservationError("MCP response did not contain the requested id")
        if "error" in response:
            raise ObservationError(json.dumps(response["error"], ensure_ascii=False))
        return response.get("result")

    def connect(self):
        server = self.rpc("initialize", {"protocolVersion": "2025-03-26", "capabilities": {},
            "clientInfo": {"name": "TechGuyGameplayObservation", "version": "1"}})
        self.rpc("notifications/initialized", notification=True)
        cursor = None
        while True:
            page = self.rpc("tools/list", {"cursor": cursor} if cursor else {})
            self.tools.update({tool["name"]: tool for tool in page.get("tools", [])})
            cursor = page.get("nextCursor")
            if not cursor:
                break
        cursor = None
        while True:
            page = self.rpc("resources/list", {"cursor": cursor} if cursor else {})
            self.resource_uris.update(item["uri"] for item in page.get("resources", []))
            cursor = page.get("nextCursor")
            if not cursor:
                break
        return server

    def resource(self, uri):
        if uri not in self.resource_uris:
            raise ObservationError("Resource not advertised by this server: " + uri)
        return decode_payload(self.rpc("resources/read", {"uri": uri}))

    def tool(self, name, arguments=None):
        if name not in self.tools:
            raise ObservationError("Tool not advertised by this session: " + name)
        value = decode_payload(self.rpc("tools/call", {"name": name, "arguments": arguments or {}}))
        if isinstance(value, dict) and value.get("success") is False:
            raise ObservationError(json.dumps(value, ensure_ascii=False))
        return value

    def select_project(self, expected_root=PROJECT_ROOT, instance=None):
        listed = self.resource("mcpforunity://instances")
        instances = listed.get("instances", listed.get("data", {}).get("instances", []))
        if not instance:
            if len(instances) != 1:
                raise ObservationError("Expected one Unity instance; use --instance explicitly: " +
                    json.dumps([item.get("id") for item in instances]))
            instance = instances[0]["id"]
        self.tool("set_active_instance", {"instance": instance})
        self.resource("mcpforunity://custom-tools")
        info = self.resource("mcpforunity://project/info")
        actual = info.get("data", {}).get("projectRoot")
        if not actual or os.path.normcase(str(Path(actual).resolve())) != os.path.normcase(str(Path(expected_root).resolve())):
            raise ObservationError("Refusing wrong Unity project: expected " + str(expected_root) + ", got " + str(actual))
        return {"instance": instance, "project": info, "state": self.resource("mcpforunity://editor/state")}

    def observation(self, expression):
        result = self.tool("execute_code", {"action": "execute", "code": "return " + expression + ";", "compiler": "codedom"})
        value = result.get("data", {}).get("result", result)
        return json.loads(value) if isinstance(value, str) else value


def load_request(args):
    if args.request:
        request = json.loads(Path(args.request).read_text(encoding="utf-8-sig"))
    elif args.scenario:
        path = PROJECT_ROOT / "Tools/GameplayObservation/scenarios.json"
        profiles = json.loads(path.read_text(encoding="utf-8-sig"))
        entries = profiles.get("scenarios", [])
        request = next((dict(item) for item in entries if item.get("scenario_id") == args.scenario), None)
        if request is None:
            raise ObservationError("Unknown scenario; choose from " + ", ".join(item["scenario_id"] for item in entries))
    else:
        raise ObservationError("Provide --request or --scenario")
    if not isinstance(request, dict):
        raise ObservationError("Observation request must be a JSON object")
    if args.phase:
        request["phase"] = args.phase
    if args.seed is not None:
        request["seed"] = args.seed
    return request


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--endpoint", default="http://127.0.0.1:8080/mcp")
    parser.add_argument("--instance")
    parser.add_argument("--project", type=Path, default=PROJECT_ROOT)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("discover")
    sub.add_parser("status")
    sub.add_parser("cancel")
    run = sub.add_parser("run")
    run.add_argument("--request", type=Path)
    run.add_argument("--scenario")
    run.add_argument("--phase", choices=("before", "after", "baseline", "candidate"))
    run.add_argument("--seed")
    run.add_argument("--wait", action="store_true")
    run.add_argument("--timeout", type=float, default=300)
    call = sub.add_parser("tool", help="Call an advertised existing Unity MCP tool; schemas are available through discover")
    call.add_argument("name")
    call.add_argument("--params", default="{}")
    call.add_argument("--params-file", type=Path)
    args = parser.parse_args(argv)
    if args.command == "run" and (not math.isfinite(args.timeout) or args.timeout <= 0):
        raise ObservationError("--timeout must be finite and positive")
    request = load_request(args) if args.command == "run" else None
    client = UnityMcp(args.endpoint)
    server = client.connect()
    context = client.select_project(args.project, args.instance)
    if args.command == "discover":
        return {"server": server.get("serverInfo"), "context": context,
            "tools": [{"name": value["name"], "inputSchema": value["inputSchema"]} for value in client.tools.values()],
            "resources": sorted(client.resource_uris)}
    if args.command == "tool":
        params = json.loads(args.params_file.read_text(encoding="utf-8-sig") if args.params_file else args.params)
        return client.tool(args.name, params)
    if args.command == "cancel":
        return client.observation("CombatPolishPreview.CancelObservation()")
    if args.command == "status":
        return client.observation("CombatPolishPreview.ObservationStatus()")
    # A JSON string literal is also a valid C# quoted literal for these ASCII-escaped fields.
    literal = json.dumps(json.dumps(request, ensure_ascii=True))
    result = client.observation("CombatPolishPreview.BeginObservation(" + literal + ")")
    if isinstance(result, dict) and result.get("status") in ("rejected", "blocked"):
        raise ObservationError(json.dumps(result, ensure_ascii=False))
    if not args.wait:
        return result
    deadline = time.monotonic() + args.timeout
    while time.monotonic() < deadline:
        status = result.get("status", result.get("state")) if isinstance(result, dict) else None
        if status in ("complete", "failed", "cancelled", "aborted", "rejected", "blocked"):
            if status != "complete":
                raise ObservationError(json.dumps(result, ensure_ascii=False))
            return result
        time.sleep(2)
        result = client.observation("CombatPolishPreview.ObservationStatus()")
    raise ObservationError("Observation wait timed out; use status/cancel to inspect and restore the Editor. Last state: " + json.dumps(result))


if __name__ == "__main__":
    try:
        print(json.dumps(main(), ensure_ascii=True, indent=2))
    except (ObservationError, OSError, ValueError) as error:
        print(json.dumps({"success": False, "error": str(error)}, ensure_ascii=True), file=sys.stderr)
        sys.exit(1)
