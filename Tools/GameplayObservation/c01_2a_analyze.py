"""Read-only source/GUID checks and CSV summary for the isolated C01.2a experiment."""
import csv
import hashlib
import json
import pathlib
import re
import statistics
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Docs/validation/artifacts/gameplay-rework/c01-2a"

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding="utf-8")

initial = json.loads((OUT / "integridade-inicial.json").read_text(encoding="utf-8-sig"))
changed, missing = [], []
for entry in initial:
    path = ROOT / entry["Path"]
    if not path.exists():
        missing.append(entry["Path"])
    elif hashlib.sha256(path.read_bytes()).hexdigest().upper() != entry["Hash"]:
        changed.append(entry["Path"])
write("integridade-final.json", {"checked": len(initial), "changed": changed, "missing": missing})

metrics = {}
for path in sorted(OUT.glob("*.csv")):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        groups = {}
        for row in csv.DictReader(stream):
            groups.setdefault(row["case"], []).append(row)
    for label, rows in groups.items():
        speed = [float(r["actualPlanarSpeed"]) for r in rows]
        cpu = sorted(float(r["cpuMicroseconds"]) for r in rows)
        metrics[label] = {
            "frames": len(rows), "requestedDt": float(rows[0]["dt"]),
            "meanPlanarSpeed": statistics.mean(speed), "minSpeed": min(speed), "maxSpeed": max(speed),
            "speedStdDev": statistics.pstdev(speed),
            "distanceFromSamples": sum(float(r["dt"]) * float(r["actualPlanarSpeed"]) for r in rows),
            "agentVelocityMax": max(float(r["agentVelocity"]) for r in rows),
            "lastXYZ": [float(rows[-1][axis]) for axis in ("x", "y", "z")],
            "stepCpuMedianUs": statistics.median(cpu), "stepCpuP95Us": cpu[min(len(cpu)-1, int(.95 * len(cpu)))],
        }
write("medicoes-resumo.json", metrics)

new_dir = "Assets/_Project/Scripts/Tests/GprMovementPrototype/"
paths = subprocess.check_output(["rg", "--files", "Assets", "-g", "*.meta"], cwd=ROOT, text=True).splitlines()
guids = {}
for item in paths:
    match = re.search(r"^guid: ([0-9a-f]{32})$", (ROOT / item).read_text(encoding="utf-8-sig"), re.M)
    if match:
        guids.setdefault(match[1], []).append(item.replace("\\", "/"))
new_guids = {key: values for key, values in guids.items() if any(x.startswith(new_dir) for x in values)}
write("metadados-alternativa.json", {"method": "GUIDs novos comparados com metadados Assets enumerados por rg; nao substitui checker global", "metasRead": len(paths), "prototypeGuids": new_guids, "duplicates": {k:v for k,v in new_guids.items() if len(v) > 1}})
print(json.dumps({"integrityChecked":len(initial), "changed":changed, "missing":missing, "scenarios":len(metrics), "newGuidCount":len(new_guids), "duplicateNewGuids":sum(len(v)>1 for v in new_guids.values())}))
