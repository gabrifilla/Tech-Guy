"""Use the project's existing Unity MCP client; no server or test runner is implemented here."""
import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/GameplayObservation'))
from observe_gameplay import UnityMcp

p = argparse.ArgumentParser()
p.add_argument('name')
p.add_argument('--args')
p.add_argument('--code')
p.add_argument('--out')
a = p.parse_args()
c = UnityMcp(timeout=55)
c.connect()
c.select_project(ROOT)
if a.name.startswith('mcpforunity:'):
    result = c.resource(a.name)
else:
    args = json.loads(Path(a.args).read_text(encoding='utf-8-sig')) if a.args else {}
    if a.code:
        args.update(action='execute', compiler='codedom', code=Path(a.code).read_text(encoding='utf-8-sig'))
    result = c.tool(a.name, args)
if a.out:
    target = Path(a.out)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding='utf-8')
data = result.get('data', result) if isinstance(result, dict) else result
if isinstance(data, dict) and 'progress' in data:
    print(json.dumps({k:data.get(k) for k in ('job_id','status','progress','result')},ensure_ascii=False)[:6500])
else:
    print(json.dumps(result, ensure_ascii=False)[:6500])
