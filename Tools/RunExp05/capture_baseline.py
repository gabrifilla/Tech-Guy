"""Capture the current user state before RUN-EXP-05; never restore an older state."""
import base64
import hashlib
import json
import subprocess
import winreg
import zipfile
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'Docs/validation/artifacts/run-exp-05'
out.mkdir(parents=True,exist_ok=True)
target=out/'baseline-files-prefs.json'
if target.exists():raise RuntimeError('Baseline already exists; never overwrite the initial state.')
files={}
for folder in ('Assets/_Project','ProjectSettings','Packages'):
    for path in (root/folder).rglob('*'):
        if path.is_file():files[path.relative_to(root).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
prefs=[];private=[]
for keypath in (r'Software\Unity\UnityEditor\DefaultCompany\T.G - Tech Guy',r'Software\DefaultCompany\T.G - Tech Guy'):
    values=[]
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,keypath) as key:
        for i in range(winreg.QueryInfoKey(key)[1]):
            name,value,kind=winreg.EnumValue(key,i)
            text=base64.b64encode(value).decode('ascii') if isinstance(value,bytes) else str(value)
            values.append(dict(name=name,kind=kind,hash=hashlib.sha256(text.encode()).hexdigest()))
            private.append(dict(path=keypath,name=name,kind=kind,value=text))
    prefs.append(dict(path=keypath,values=values))
git=lambda *args:subprocess.check_output(['git',*args],cwd=root,text=True,encoding='utf-8')
target.write_text(json.dumps(dict(head=git('rev-parse','HEAD').strip(),branch=git('branch','--show-current').strip(),status=git('status','--short'),sha256=files,prefs=prefs),indent=2,ensure_ascii=False),encoding='utf-8')
(root/'Library/RunExp05/private-registry-baseline.json').write_text(json.dumps(private),encoding='utf-8')
with zipfile.ZipFile(out/'restorable-source-baseline.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for name in files:
        if Path(name).suffix in ('.cs','.meta','.unity','.prefab','.asset','.mat','.json','.shader','.asmdef'):
            archive.write(root/name,name)
tests=['GauntletNaturalSeed0ReachesBossTicketsAndNexus','Art04BeforeAfterUsesIdenticalRuntimeCameraAndPreservesNativePresentation','RefinedRoomComparisonsPreserveCollidersCoverAndBothCirculationRoutes','IntegratedActiveCost8','IntegratedActiveCost32','IntegratedActiveCost64']
args=dict(mode='PlayMode',test_names=['TechGuy.Tests.RunExp03FirstSectorTests.'+x for x in tests]+['TechGuy.Tests.RunExp03RewardTests'],include_details=True,include_failed_tests=True)
(root/'Library/RunExp05/baseline-play.json').write_text(json.dumps(args),encoding='utf-8')
print('Captured',len(files),'file hashes and',sum(len(p['values']) for p in prefs),'preference hashes; no restore.')
