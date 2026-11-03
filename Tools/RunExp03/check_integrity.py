"""Read-only file/registry comparison against the saved RUNEXP03 baseline."""
import base64
import hashlib
import json
from pathlib import Path
import subprocess
import winreg

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/validation/artifacts/run-exp-03'
read = lambda path: json.loads(path.read_text(encoding='utf-8'))
baseline = read(OUT/'baseline-files.json')
changed = []
for name,digest in baseline['sha256'].items():
    path = ROOT/name
    now = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
    if now != digest:
        changed.append({'path':name,'before':digest,'after':now})
authorized = {'Assets/_Project/Scenes/FirstSector.unity', 'Assets/_Project/Scripts/Core/Run/ManualRunRandomizer.cs', 'Assets/_Project/Scripts/Core/Run/ManualStageEnvironment.Procedural.cs', 'Assets/_Project/Scripts/Core/Run/ProgressionDirector.Manual.cs', 'Assets/_Project/Scripts/Stages/RunSeedSource.cs'}

before = json.loads(read(OUT/'baseline-context.json')['data']['result'])
after = json.loads(read(OUT/'final-context.json')['data']['result'])
index = lambda context: {(p['path'],v['name']):v for p in context['prefs'] for v in p['values']}
a,b = index(before),index(after)
live = {}
kind_names = {winreg.REG_BINARY:'Binary',winreg.REG_DWORD:'DWord',winreg.REG_QWORD:'QWord',winreg.REG_SZ:'String',winreg.REG_EXPAND_SZ:'ExpandString'}
for pref in before['prefs']:
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,pref['path']) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name,value,kind = winreg.EnumValue(key,i)
            text = base64.b64encode(value).decode('ascii') if isinstance(value,bytes) else str(value)
            live[(pref['path'],name)] = {'name':name,'kind':kind_names.get(kind,str(kind)),'hash':hashlib.sha256(text.encode('utf-8')).hexdigest()}

def changes(left,right):
    return [{'registryPath':key[0],'name':key[1],'before':left.get(key),'after':right.get(key)} for key in sorted(left.keys()|right.keys()) if left.get(key)!=right.get(key)]

prefs = changes(a,b)
live_changes = changes(a,live)
result = {'baselineFiles':len(baseline['sha256']), 'unchangedFiles':len(baseline['sha256'])-len(changed), 'modifiedExistingFiles':changed, 'unexpectedExistingFileChanges':[p for p in changed if p['path'] not in authorized], 'gamePreferenceCount':sum(1 for key in a if key[1].startswith('TechGuy.')), 'unityRecordedPreferenceChanges':prefs, 'currentRegistryChanges':live_changes, 'gamePreferenceChanges':[p for p in live_changes if p['name'].startswith('TechGuy.')], 'expectedNonGameplayChanges':'PT_Run is the Unity performance test framework metadata (Utils.PlayerPrefKeyRunJSON); unity.* session IDs/count change across Editor sessions. Their hashes are not claimed identical.', 'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(), 'branch':subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip(), 'status':subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True), 'lastUnityVerifiedScene':after['scene'], 'lastUnityVerifiedDirty':after['dirty'], 'lastUnityVerifiedAssistance':after['assistance']}
result['unityRecordedGamePreferenceChanges'] = [p for p in prefs if p['name'].startswith('TechGuy.')]
result['scope'] = 'The saved final Unity checkpoint on30/10 preserved all32 TechGuy values. Current registry is a later read after an interruption/Editor closure; later differences are observed, not attributed to this work and never restored over current user data.'
(OUT/'integrity-final.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')

historical = read(ROOT/'Docs/validation/artifacts/run-exp-02/save-files-final.json')
files = []
for record in historical:
    path = Path(record['path'])
    digest = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
    files.append({'path':str(path),'reference02':record['after'],'current':digest,'equal':digest==record['after'],'kind':record['kind']})
known = {str(Path(r['path'])) for r in historical}
folder = Path(historical[0]['path']).parent
extra = [{'path':str(p),'hash':hashlib.sha256(p.read_bytes()).hexdigest()} for p in folder.rglob('*') if p.is_file() and str(p) not in known]
(OUT/'persistent-files-final.json').write_text(json.dumps({'method':'Historical02 reference, not a newly invented03 pre-edit file baseline. Gameplay preferences have their own complete03 registry comparison. TestResults/log/analytics are explicitly not gameplay saves.','files':files,'extraFiles':extra},indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'files':result['unchangedFiles'],'unexpected':result['unexpectedExistingFileChanges'],'gamePrefs':result['gamePreferenceCount'],'gameChanges':result['gamePreferenceChanges'],'nonGameChanged':[r['name'] for r in live_changes],'extraPersistentFiles':len(extra)},ensure_ascii=False))
