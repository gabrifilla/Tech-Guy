"""GPR-01.2b: keep new Unity evidence separate from the accepted instant-wave experiment."""
import json
import pathlib
import sys
import gpr_01_2_mcp as client

OUT = client.ROOT / 'Docs/validation/artifacts/gameplay-rework/gpr-01-2b'
call = client.call
if __name__ == '__main__':
    result = call(sys.argv[1], json.loads(pathlib.Path(sys.argv[2]).read_text(encoding='utf-8-sig')))
    if len(sys.argv) > 3:
        OUT.mkdir(parents=True, exist_ok=True)
        (OUT / sys.argv[3]).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))
