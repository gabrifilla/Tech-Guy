"""Offline protocol/safety contracts. Mock payloads are NOT Editor evidence."""

import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import unittest
from unittest import mock
import urllib.error


spec = importlib.util.spec_from_file_location("observe_gameplay", Path(__file__).with_name("observe_gameplay.py"))
observation = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = observation
spec.loader.exec_module(observation)


class HttpResponse:
    def __init__(self, body, session="synthetic-session"):
        self.body = body.encode("utf-8")
        self.headers = {"Mcp-Session-Id": session} if session else {}

    def __enter__(self):
        return self

    def __exit__(self, *args):
        return False

    def read(self):
        return self.body


class AdvertisedServer:
    """MCP envelope shapes only: no real requests, captures, or approval."""
    def __init__(self, project=None, instances=None):
        self.project = project or str(observation.PROJECT_ROOT)
        self.instances = instances if instances is not None else [{"id": "TechGuy@synthetic"}]
        self.calls = []

    @staticmethod
    def resource_result(uri, value):
        return {"contents": [{"uri": uri, "mimeType": "application/json", "text": json.dumps(value)}]}

    def __call__(self, method, params=None, notification=False):
        self.calls.append((method, params, notification))
        if method == "initialize":
            return {"protocolVersion": "2025-03-26", "serverInfo": {"name": "SYNTHETIC_TEST_SERVER", "version": "1"},
                    "capabilities": {"tools": {"listChanged": True}, "resources": {"listChanged": True}}}
        if method == "notifications/initialized":
            return None
        if method == "tools/list":
            if not params.get("cursor"):
                return {"tools": [{"name": "set_active_instance", "inputSchema": {"type": "object", "required": ["instance"],
                    "properties": {"instance": {"type": "string"}}}}], "nextCursor": "page-2"}
            return {"tools": [{"name": "execute_code", "inputSchema": {"type": "object", "required": ["action", "code"],
                "properties": {"action": {"type": "string", "enum": ["execute"]}, "code": {"type": "string"}}}}]}
        if method == "resources/list":
            if not params.get("cursor"):
                return {"resources": [{"uri": "mcpforunity://instances"}, {"uri": "mcpforunity://project/info"}], "nextCursor": "resources-2"}
            return {"resources": [{"uri": "mcpforunity://editor/state"}, {"uri": "mcpforunity://custom-tools"}]}
        if method == "resources/read":
            uri = params["uri"]
            payloads = {"mcpforunity://instances": {"success": True, "data": {"instances": self.instances}},
                        "mcpforunity://project/info": {"success": True, "data": {"projectRoot": self.project}},
                        "mcpforunity://editor/state": {"success": True, "data": {"isPlaying": False, "isCompiling": False}},
                        "mcpforunity://custom-tools": {"success": True, "data": {"tools": []}}}
            return self.resource_result(uri, payloads[uri])
        if method == "tools/call":
            if params["name"] == "set_active_instance":
                return {"content": [{"type": "text", "text": '{"success":true}'}]}
            return {"structuredContent": {"success": True, "data": {"result": '{"status":"complete","synthetic":true}'}}}
        raise AssertionError(f"Unexpected mocked RPC: {method}")


class ObservationProtocolTests(unittest.TestCase):
    def test_resource_and_tool_envelopes_unwrap_to_payload_without_losing_errors(self):
        payload = {"success": True, "data": {"projectRoot": "SYNTHETIC-PROJECT"}}
        resource = AdvertisedServer.resource_result("mcpforunity://project/info", payload)
        self.assertEqual(observation.decode_payload(resource), payload)
        self.assertEqual(observation.decode_payload({"structuredContent": resource}), payload)
        with self.assertRaises(observation.ObservationError):
            observation.decode_payload({"isError": True, "content": [{"type": "text", "text": "Compiler unavailable"}]})

    def test_sse_ignores_notifications_and_unrelated_ids_and_preserves_session(self):
        client = observation.UnityMcp()
        responses = [HttpResponse('event: message\ndata: {"jsonrpc":"2.0","method":"notifications/message"}\n\n'
                                 'data: {"jsonrpc":"2.0","id":999,"result":{"wrong":true}}\n\n'
                                 'data: {"jsonrpc":"2.0","id":1,"result":{"right":true}}\n\n'),
                     HttpResponse('{"jsonrpc":"2.0","id":2,"result":{"second":true}}', session=None)]
        with mock.patch.object(observation.urllib.request, "urlopen", side_effect=responses) as opened:
            self.assertEqual(client.rpc("tools/list"), {"right": True})
            self.assertEqual(client.rpc("resources/list"), {"second": True})
        headers = {key.lower(): value for key, value in opened.call_args_list[1].args[0].header_items()}
        self.assertEqual(headers["mcp-session-id"], "synthetic-session")

    def test_missing_correlated_response_and_rpc_error_are_rejected(self):
        for body in ('{"jsonrpc":"2.0","id":99,"result":{}}',
                     '{"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"Unknown method"}}'):
            with self.subTest(body=body):
                with mock.patch.object(observation.urllib.request, "urlopen", return_value=HttpResponse(body)):
                    with self.assertRaises(observation.ObservationError):
                        observation.UnityMcp().rpc("tools/list")

    def test_unavailable_server_does_not_launch_a_replacement_or_capture(self):
        with mock.patch.object(observation.urllib.request, "urlopen", side_effect=urllib.error.URLError("synthetic offline")):
            with self.assertRaisesRegex(observation.ObservationError, "no replacement server"):
                observation.UnityMcp().connect()

    def test_discovery_retains_advertised_schemas_resources_and_paginated_capabilities(self):
        server = AdvertisedServer()
        with mock.patch.object(observation.UnityMcp, "rpc", side_effect=server):
            result = observation.main(["discover"])
        tools = {tool["name"]: tool["inputSchema"] for tool in result["tools"]}
        self.assertEqual(tools["execute_code"]["required"], ["action", "code"])
        self.assertEqual(tools["execute_code"]["properties"]["action"]["enum"], ["execute"])
        self.assertIn("mcpforunity://editor/state", result["resources"])
        self.assertEqual(result["server"]["name"], "SYNTHETIC_TEST_SERVER")
        self.assertFalse(any(method == "tools/call" and params["name"] == "execute_code" for method, params, _ in server.calls))

    def test_wrong_project_refuses_run_before_any_execute_code(self):
        server = AdvertisedServer(project=str(observation.PROJECT_ROOT.parent / "WrongProject"))
        with mock.patch.object(observation.UnityMcp, "rpc", side_effect=server):
            with self.assertRaisesRegex(observation.ObservationError, "Refusing wrong Unity project"):
                observation.main(["run", "--scenario", "GAUNTLET_SMALL_GROUP"])
        self.assertFalse(any(method == "tools/call" and params["name"] == "execute_code" for method, params, _ in server.calls))

    def test_multiple_instances_require_explicit_selection_before_editor_commands(self):
        server = AdvertisedServer(instances=[{"id": "TechGuy@a"}, {"id": "TechGuy@b"}])
        with mock.patch.object(observation.UnityMcp, "rpc", side_effect=server):
            with self.assertRaisesRegex(observation.ObservationError, "use --instance explicitly"):
                observation.main(["status"])
        self.assertFalse(any(method == "tools/call" for method, _, _ in server.calls))

    def test_explicit_instance_is_selected_and_project_still_verified(self):
        server = AdvertisedServer(instances=[{"id": "TechGuy@a"}, {"id": "TechGuy@b"}])
        with mock.patch.object(observation.UnityMcp, "rpc", side_effect=server):
            result = observation.main(["--instance", "TechGuy@b", "status"])
        selected = [params["arguments"]["instance"] for method, params, _ in server.calls
                    if method == "tools/call" and params["name"] == "set_active_instance"]
        self.assertEqual(selected, ["TechGuy@b"])
        self.assertEqual(result["status"], "complete")

    def test_unadvertised_tools_and_resources_are_refused_without_network_dispatch(self):
        client = observation.UnityMcp()
        with mock.patch.object(client, "rpc") as rpc:
            with self.assertRaisesRegex(observation.ObservationError, "not advertised"):
                client.tool("imagined_capture")
            with self.assertRaisesRegex(observation.ObservationError, "not advertised"):
                client.resource("mcpforunity://imagined-video")
        rpc.assert_not_called()

    def test_advertised_tool_failure_is_not_reported_as_success(self):
        client = observation.UnityMcp()
        client.tools = {"execute_code": {"name": "execute_code"}}
        with mock.patch.object(client, "rpc", return_value={"content": [{"type": "text", "text": '{"success":false,"message":"compiling"}'}]}):
            with self.assertRaisesRegex(observation.ObservationError, "compiling"):
                client.tool("execute_code", {"action": "execute", "code": "return null;"})

    def test_sse_without_space_and_multiline_data(self):
        body = 'data:{"jsonrpc":"2.0",\ndata:"id":1,"result":{"ok":true}}\n\n'
        with mock.patch.object(observation.urllib.request, "urlopen", return_value=HttpResponse(body)):
            self.assertEqual(observation.UnityMcp().rpc("tools/list"), {"ok": True})

    def test_non_object_rpc_response_is_rejected(self):
        with mock.patch.object(observation.urllib.request, "urlopen", return_value=HttpResponse('[]')):
            with self.assertRaises(observation.ObservationError):
                observation.UnityMcp().rpc("tools/list")

    def test_invalid_wait_timeout_never_connects_or_starts_capture(self):
        for timeout in ("0", "-1", "nan", "inf"):
            with mock.patch.object(observation.UnityMcp, "connect") as connect:
                with self.assertRaises(observation.ObservationError):
                    observation.main(["run", "--scenario", "GAUNTLET_SMALL_GROUP", "--timeout", timeout])
                connect.assert_not_called()

    def test_rejected_begin_fails_without_polling_or_sleep(self):
        server = AdvertisedServer()
        with mock.patch.object(observation.UnityMcp, "rpc", side_effect=server):
            with mock.patch.object(observation.UnityMcp, "observation", return_value={"status": "rejected", "message": "dirty scene"}) as invoke:
                with mock.patch.object(observation.time, "sleep") as sleep:
                    with self.assertRaisesRegex(observation.ObservationError, "dirty scene"):
                        observation.main(["run", "--scenario", "GAUNTLET_SMALL_GROUP", "--wait"])
                    self.assertEqual(invoke.call_count, 1)
                    sleep.assert_not_called()

    def test_help_does_not_connect_or_change_editor(self):
        with mock.patch.object(observation.UnityMcp, "connect") as connect:
            with contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit) as exited:
                    observation.main(["--help"])
        self.assertEqual(exited.exception.code, 0)
        connect.assert_not_called()


if __name__ == "__main__":
    unittest.main()
