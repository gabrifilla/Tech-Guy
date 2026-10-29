"""Reproducible GPR-01.2 requests against the real local Unity MCP bridge."""
import json
import pathlib
import sys
import tempfile
import c01_2b_mcp as transport

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/validation/artifacts/gameplay-rework/gpr-01-2'
SESSION = pathlib.Path(tempfile.gettempdir()) / 'tech-guy-gpr012-session.json'

def call(name, args):
    if SESSION.exists():
        session = json.loads(SESSION.read_text())['session']
    else:
        _, session = transport.rpc('initialize', {'protocolVersion': '2025-03-26', 'capabilities': {},
            'clientInfo': {'name': 'TechGuy-GPR01.2', 'version': '1'}})
        transport.rpc('notifications/initialized', notification=True, session=session)
        SESSION.write_text(json.dumps({'session': session}), encoding='utf-8')
    response, _ = transport.rpc('tools/call', {'name': name, 'arguments': args}, session=session)
    result = response.get('result', response)
    if result.get('structuredContent') is not None:
        return result['structuredContent']
    for item in result.get('content', []):
        if item.get('type') == 'text':
            try: return json.loads(item['text'])
            except ValueError: pass
    return result

if __name__ == '__main__':
    name, filename = sys.argv[1:3]
    request = json.loads(pathlib.Path(filename).read_text(encoding='utf-8-sig'))
    result = call(name, request)
    if len(sys.argv) > 3:
        OUT.mkdir(parents=True, exist_ok=True)
        (OUT / sys.argv[3]).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))
