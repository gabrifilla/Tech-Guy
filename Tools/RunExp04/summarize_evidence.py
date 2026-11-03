"""Summarize real RUN-EXP-04 captures; never restore or normalize user state."""
from pathlib import Path
import base64
import hashlib
import json
import subprocess
import winreg

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/validation/artifacts/run-exp-04'
def write(name, value):
    (OUT/name).write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding='utf-8')
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
def pct(values, p):
    values=sorted(values)
    return values[round((len(values)-1)*p)] if values else None

performance=[]
for count in (8,32,64):
    source=sorted((ROOT/'Docs/validation/artifacts/run-exp-03/firstsector').glob('20261102-*/integrated-active-'+str(count)+'.json'))[-1]
    report=json.loads(source.read_text(encoding='utf-8'))
    (OUT/('performance-raw-'+str(count)+'.json')).write_bytes(source.read_bytes())
    rows=report.pop('samples')
    markers={name: {'p50':pct([r['markers'][i] for r in rows],.5), 'p95':pct([r['markers'][i] for r in rows],.95), 'unit':'bytes' if name=='GC Allocated In Frame' else 'nanoseconds' if name not in ('Draw Calls Count','Batches Count') else 'count'} for i,name in enumerate(report['markerNames'])}
    performance.append({**report, 'source':source.relative_to(ROOT).as_posix(), 'frames':len(rows), 'dtMs':{'p50':pct([r['dtMs'] for r in rows],.5),'p95':pct([r['dtMs'] for r in rows],.95)}, 'activeMin':min(r['active'] for r in rows), 'movingMax':max(r['moving'] for r in rows), 'windupsMax':max(r['windups'] for r in rows), 'projectilesMax':max(r['projectiles'] for r in rows), 'drawCallsEditor':{'p50':pct([r['drawCalls'] for r in rows],.5),'p95':pct([r['drawCalls'] for r in rows],.95)}, 'queueMax':max(r['queue'] for r in rows),'liveRootsMax':max(r['roots'] for r in rows), 'markers':markers})
write('performance-summary.json', {'environment':'Unity 6000.5.10f1 Windows Editor; no standalone budget approved', 'limits':['BehaviourUpdate/Main Thread/NavMesh markers are Editor aggregates, not isolated enemy AI CPU.', 'Sampling with UTF/LINQ and captures adds CPU and allocations; no before/after performance gain claimed.', 'MeshRenderer triangles/materials are separated from native Grunt SkinnedMeshRenderer triangles.', 'Draw calls are UnityStats Editor totals; batches=-1 means unavailable.', 'cleanupMs includes deliberate 0.4 second wait, not isolated destruction CPU.', 'Enemy AI/nav/damage/poses active. Player immune only in cost fixture; player does not attack, so queue zero does not stress boon budgets. See selected Spear dense-target regression separately.', 'VFX characterized by live native projectiles/poses and aggregate counters; no isolated GPU VFX timing.'], 'populations':performance})

assets=[]
for folder in ('Assets/_Project/Art/RunExp04','Assets/_Project/Prefabs/FirstSector/RunExp04'):
    for path in sorted((ROOT/folder).rglob('*')):
        if path.is_file():assets.append({'path':path.relative_to(ROOT).as_posix(),'sha256':sha(path),'bytes':path.stat().st_size})
write('asset-manifest.json',{'provenance':'Original intermediate meshes authored by RunExp04EnemyArtAuthoring in Unity; copies of approved 03 candidates preserve native contracts. No Blender connection or final DCC/rig/atlas/LOD claimed. Room URP materials and visual-only generated meshes; reused 03 UI glyphs, no new third-party art.', 'files':assets})

baseline=json.loads((OUT/'baseline-files-prefs.json').read_text(encoding='utf-8'))
changed=[];missing=[]
for name,old in baseline['sha256'].items():
    path=ROOT/name
    if not path.exists():missing.append(name)
    elif sha(path)!=old:changed.append(name)
prefs=[];pref_changes=[]
for previous in baseline['prefs']:
    values=[]
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,previous['path']) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name,value,kind=winreg.EnumValue(key,i)
            text=base64.b64encode(value).decode('ascii') if isinstance(value,bytes) else str(value)
            values.append(dict(name=name,kind=kind,hash=hashlib.sha256(text.encode()).hexdigest()))
    prefs.append(dict(path=previous['path'],values=values))
    old={v['name']:v for v in previous['values']};new={v['name']:v for v in values}
    for name in sorted(old.keys()|new.keys()):
        if old.get(name)!=new.get(name):pref_changes.append({'path':previous['path'],'name':name,'gameplay':name.startswith('TechGuy')})
git=lambda *args:subprocess.check_output(['git',*args],cwd=ROOT,text=True,encoding='utf-8').strip()
write('integrity-final.json',{'baselineFiles':len(baseline['sha256']), 'unchangedFiles':len(baseline['sha256'])-len(changed)-len(missing),'changedFiles':changed,'missingFiles':missing,'head':git('rev-parse','HEAD'),'baselineHead':baseline['head'],'branch':git('branch','--show-current'),'prefsHashed':prefs,'preferenceChanges':pref_changes,'gameplayPreferencesCompared':sum(v['name'].startswith('TechGuy') for p in baseline['prefs'] for v in p['values']), 'limit':'Current RUN-EXP-04 baseline only. Product registry scopes and listed file roots; not proof of universal save/file integrity. No restore performed by this script. Prior user changes are preserved.'})
print(json.dumps({'protectedFiles':len(baseline['sha256']),'changed':changed,'missing':missing,'preferenceChanges':pref_changes,'populations':[{'count':p['count'],'frames':p['frames'],'dtMs':p['dtMs'],'activeMin':p['activeMin']} for p in performance]},indent=2))
