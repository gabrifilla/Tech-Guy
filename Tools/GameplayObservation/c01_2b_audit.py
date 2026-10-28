"""Read-only comparison of the protected initial files and new movement GUIDs."""
import hashlib
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Docs/validation/artifacts/gameplay-rework/c01-2b"
initial = json.loads((OUT / "integridade-inicial.json").read_text(encoding="utf-8-sig"))
changed, missing = [], []
for entry in initial:
    path = ROOT / entry["path"]
    if not path.exists():
        missing.append(entry["path"])
    elif hashlib.sha256(path.read_bytes()).hexdigest() != entry["sha256"]:
        changed.append(entry["path"])
(OUT / "integridade-final.json").write_text(json.dumps({"checked": len(initial), "unchanged": len(initial)-len(changed)-len(missing), "changed": changed, "missing": missing}, indent=2), encoding="utf-8")

files = subprocess.check_output(["rg", "--files", "Assets", "-g", "*.meta"], cwd=ROOT, text=True).splitlines()
guids = {}
for name in files:
    match = re.search(r"^guid: ([0-9a-f]{32})$", (ROOT / name).read_text(encoding="utf-8-sig"), re.M)
    if match:
        guids.setdefault(match[1], []).append(name.replace("\\", "/"))
new_names = ("PlayerNavigationMotor.cs.meta", "CharControlScript.DirectMovement.cs.meta", "DirectMovementIntegrationPlayModeTests.cs.meta")
new = {key:value for key,value in guids.items() if any(path.endswith(new_names) for path in value)}
(OUT / "novos-guids.json").write_text(json.dumps({"metasRead":len(files),"newGuids":new,"duplicates":{key:value for key,value in new.items() if len(value)>1}},indent=2),encoding="utf-8")
print(json.dumps({"checked":len(initial),"changedCount":len(changed),"missing":missing,"changed":changed,"newGuids":len(new)}))
