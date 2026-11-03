"""Capture the current06 entry state; never restore an older milestone's preferences."""
from pathlib import Path
import base64,hashlib,json,subprocess,winreg,zipfile
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/validation/artifacts/run-exp-06';OUT.mkdir(parents=True,exist_ok=True)
target=OUT/'baseline-files-prefs.json'
if target.exists():raise RuntimeError('06 baseline already captured; do not overwrite')
paths=set()
for folder in ('Assets/_Project','ProjectSettings','Packages','Tools/RunExp05/Source'):
    paths.update(p for p in (ROOT/folder).rglob('*') if p.is_file())
paths.update((ROOT/'Assets').rglob('*.meta'))
paths.update((ROOT/'Docs').rglob('*.md'))
paths.add(ROOT/'Assets/_ThirdParty/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset')
files={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(paths)}
prefs=[];private=[]
for keypath in (r'Software\Unity\UnityEditor\DefaultCompany\T.G - Tech Guy',r'Software\DefaultCompany\T.G - Tech Guy'):
    values=[]
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name,value,kind=winreg.EnumValue(key,i);text=base64.b64encode(value).decode('ascii') if isinstance(value,bytes) else str(value)
            values.append({'name':name,'kind':kind,'hash':hashlib.sha256(text.encode()).hexdigest()});private.append({'path':keypath,'name':name,'kind':kind,'value':text})
    prefs.append({'path':keypath,'values':values})
git=lambda *args:subprocess.check_output(['git',*args],cwd=ROOT,text=True,encoding='utf-8').strip()
target.write_text(json.dumps({'head':git('rev-parse','HEAD'),'branch':git('branch','--show-current'),'status':git('status','--short'),'sha256':files,'prefs':prefs},indent=2,ensure_ascii=False),encoding='utf-8')
private_path=ROOT/'Library/RunExp06/private-registry-baseline.json';private_path.parent.mkdir(exist_ok=True);private_path.write_text(json.dumps(private),encoding='utf-8')
with zipfile.ZipFile(OUT/'restorable-source-baseline.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(paths):
        if p.suffix in ('.cs','.meta','.asset','.prefab','.unity','.mat','.json','.shader','.asmdef','.md'):z.write(p,p.relative_to(ROOT))
tests=['GauntletNaturalSeed0ReachesBossTicketsAndNexus','Dcc05ArtComparisonsKeepNativePhasesFeetDamageAndCleanup','Archive05ComparisonPreservesCoverRoutesLosMovementAndAbilities','IntegratedActiveCost8','IntegratedActiveCost32','IntegratedActiveCost64']
args={'mode':'PlayMode','test_names':['TechGuy.Tests.RunExp03FirstSectorTests.'+t for t in tests]+['TechGuy.Tests.RunExp03RewardTests'],'include_details':True,'include_failed_tests':True}
(ROOT/'Library/RunExp06/baseline-play.json').write_text(json.dumps(args),encoding='utf-8')
print('06 entry baseline:',len(files),'file hashes;',sum(v['name'].startswith('TechGuy') for p in prefs for v in p['values']),'existing gameplay preference hashes. No restore.')
