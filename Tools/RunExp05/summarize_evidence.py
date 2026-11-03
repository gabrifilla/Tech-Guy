"""Summarize captured observations; compare current05 baseline without restoring any data."""
from pathlib import Path
import base64,hashlib,json,subprocess,winreg,shutil

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/validation/artifacts/run-exp-05'
def write(name,value):(OUT/name).write_text(json.dumps(value,indent=2,ensure_ascii=False),encoding='utf-8')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def pct(values,p):
    values=sorted(values);return values[round((len(values)-1)*p)] if values else None
def stats(values):return {'p50':pct(values,.5),'p95':pct(values,.95)}

performance={'editor':[],'standalone':[],'limits':'EditorUTF/LINQ/IMGUI overhead; baseline before Blender connection and later editor load differs. Prefer paired standalone for limited comparison; sequential pairs do not freeze AI positions. Not hardware target or global performance budget.'}
for version,folder in [('baseline04',OUT/'baseline/firstsector'),('current05',ROOT/'Docs/validation/artifacts/run-exp-03/firstsector')]:
    for count in (8,32,64):
        paths=sorted(folder.glob('*/integrated-active-'+str(count)+'.json'))
        if not paths:continue
        path=paths[-1];item=json.loads(path.read_text(encoding='utf-8'));rows=item.pop('samples');name='performance-'+version+'-'+str(count)+'.json';shutil.copy2(path,OUT/name)
        markers={key:{**stats([row['markers'][i] for row in rows]),'unit':'bytes' if key=='GC Allocated In Frame' else 'count' if key in ('Draw Calls Count','Batches Count') else 'nanoseconds'} for i,key in enumerate(item['markerNames'])}
        performance['editor'].append({**item,'version':version,'source':path.relative_to(ROOT).as_posix(),'frames':len(rows),'frameMs':stats([r['dtMs'] for r in rows]),'activeMin':min(r['active'] for r in rows),'movingMax':max(r['moving'] for r in rows),'projectilesMax':max(r['projectiles'] for r in rows),'windupsMax':max(r['windups'] for r in rows),'drawCallsEditor':stats([r['drawCalls'] for r in rows]),'markers':markers})
for path in sorted((ROOT/'Builds/RunExp05Profile/ProfileResults').glob('*.json')):
    item=json.loads(path.read_text(encoding='utf-8'));rows=item.pop('samples');shutil.copy2(path,OUT/path.name)
    markers={name:{**stats([r['markers'][i] for r in rows]),'unit':'bytes' if name=='GC Allocated In Frame' else 'count' if name in ('Draw Calls Count','Batches Count') else 'nanoseconds'} for i,name in enumerate(item['markerNames'])}
    valid_gpu=[r['gpuMs'] for r in rows if r['gpuMs']>0]
    performance['standalone'].append({**item,'frames':len(rows),'frameMs':stats([r['frameMs'] for r in rows]),'gpuMs':stats(valid_gpu),'gpuValidFrames':len(valid_gpu),'allocatedMemoryBytes':stats([r['memory'] for r in rows]),'activeMin':min(r['active'] for r in rows),'movingMax':max(r['moving'] for r in rows),'windupsMax':max(r['windups'] for r in rows),'projectilesMax':max(r['projectiles'] for r in rows),'markers':markers})
write('performance-summary.json',performance)
baseline=json.loads((OUT/'baseline-files-prefs.json').read_text(encoding='utf-8'));changed=[];missing=[]
for name,old in baseline['sha256'].items():
    path=ROOT/name
    if not path.exists():missing.append(name)
    elif sha(path)!=old:changed.append(name)
preferences=[];differences=[]
for previous in baseline['prefs']:
    values=[]
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,previous['path']) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name,value,kind=winreg.EnumValue(key,i);value=base64.b64encode(value).decode('ascii') if isinstance(value,bytes) else str(value)
            values.append({'name':name,'kind':kind,'hash':hashlib.sha256(value.encode()).hexdigest()})
    preferences.append({'path':previous['path'],'values':values});old={v['name']:v for v in previous['values']};new={v['name']:v for v in values}
    for name in sorted(old.keys()|new.keys()):
        if old.get(name)!=new.get(name):differences.append({'path':previous['path'],'name':name,'gameplay':name.startswith('TechGuy'),'change':'added' if name not in old else 'removed' if name not in new else 'modified'})
git=lambda *args:subprocess.check_output(['git',*args],cwd=ROOT,text=True,encoding='utf-8').strip()
write('integrity-final.json',{'baselineFiles':len(baseline['sha256']),'unchangedFiles':len(baseline['sha256'])-len(changed)-len(missing),'changedFiles':changed,'missingFiles':missing,'head':git('rev-parse','HEAD'),'baselineHead':baseline['head'],'branch':git('branch','--show-current'),'prefsHashed':preferences,'preferenceChanges':differences,'gameplayPreferencesCompared':sum(v['name'].startswith('TechGuy') for p in baseline['prefs'] for v in p['values']),'baselineGameplayPreferencesChanged':sum(d['gameplay'] and d['change']!='added' for d in differences),'addedGameplayPreferences':sum(d['gameplay'] and d['change']=='added' for d in differences),'limit':'Only listed product registry scopes and2058 baseline files. No universal disk/save guarantee. No restore or normalization by this script. Current05 baseline, not03/04 snapshot. Added original-Player preferences predate profiling build per registry lastWrite time; preserved, attribution not established.'})
assets=[]
for folder in ('Assets/_Project/Art/RunExp05','Assets/_Project/Resources/UI/RunExp05','Assets/_Project/Prefabs/FirstSector/RunExp05','Tools/RunExp05/Source'):
    for path in sorted((ROOT/folder).rglob('*')):
        if path.is_file():assets.append({'path':path.relative_to(ROOT).as_posix(),'bytes':path.stat().st_size,'sha256':sha(path)})
write('asset-manifest.json',{'provenance':'Original Blender5.2.2LTS MCP geometry/UV atlas/normal plus original image_gen reward illustrations and clearly labeled concept. No sourced franchise/game assets. Runtime IK/rigid posing retained, not final DCC rigging. Prompt log reward-art-prompt.txt retains original atlas instruction; exact correction/concept prompts not retained in repo, descriptions in reports are not verbatim recovery. No claim of pixel matching concept.','files':assets})
print(json.dumps({'changedFiles':changed,'missingFiles':missing,'preferenceChanges':differences,'editorSamples':len(performance['editor']),'standaloneSamples':len(performance['standalone'])},indent=2))
