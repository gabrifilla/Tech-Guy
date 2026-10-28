"""Local MCP HTTP client: fallback while the desktop connector retains a startup error."""
import json
import pathlib
import sys
import tempfile
import urllib.request
import uuid
sys.stdout.reconfigure(encoding="utf-8")

ENDPOINT = "http://127.0.0.1:8080/mcp"
SESSION = pathlib.Path(tempfile.gettempdir()) / "tech-guy-c01-2b-mcp-session.json"

def rpc(method, params=None, notification=False, session=None):
    payload = {"jsonrpc": "2.0", "method": method}
    if not notification:
        payload["id"] = uuid.uuid4().hex
    if params is not None:
        payload["params"] = params
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if session:
        headers["Mcp-Session-Id"] = session
    request = urllib.request.Request(ENDPOINT, json.dumps(payload).encode(), headers)
    with urllib.request.urlopen(request, timeout=55) as response:
        received_session = response.headers.get("Mcp-Session-Id", session)
        if notification:
            return {}, received_session
        if "text/event-stream" in response.headers.get("Content-Type", ""):
            for line in response:
                if line.startswith(b"data: "):
                    data = json.loads(line[6:])
                    if data.get("id") == payload.get("id"):
                        return data, received_session
        else:
            return json.load(response), received_session
    raise RuntimeError("MCP response ended without a result")

def main():
    if SESSION.exists():
        session = json.loads(SESSION.read_text())["session"]
    else:
        _, session = rpc("initialize", {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "TechGuy-C01.2b-validation", "version": "1"}})
        rpc("notifications/initialized", notification=True, session=session)
        SESSION.write_text(json.dumps({"session": session}))
    command = sys.argv[1]
    if command == "discover":
        result, _ = rpc("tools/list", session=session)
    elif command == "read":
        result, _ = rpc("resources/read", {"uri": sys.argv[2]}, session=session)
    elif command == "call":
        arguments = json.loads(pathlib.Path(sys.argv[3]).read_text(encoding="utf-8-sig")) if len(sys.argv) > 3 else {}
        result, _ = rpc("tools/call", {"name": sys.argv[2], "arguments": arguments}, session=session)
    elif command == "code":
        code = pathlib.Path(sys.argv[2]).read_text(encoding="utf-8-sig")
        result, _ = rpc("tools/call", {"name": "execute_code", "arguments": {"action": "execute", "compiler": "codedom", "code": code}}, session=session)
    else:
        raise ValueError(command)
    print(json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()
