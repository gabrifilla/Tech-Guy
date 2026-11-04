"""Read-only project/prefs fingerprint; never restores historical state."""
from pathlib import Path
import hashlib,json,subprocess,winreg,sys
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-07/evidence'
OUT.mkdir(parents=True,exist_ok=True)
def snapshot():
    files={}
    for folder in ('Assets/_Project','ProjectSettings','Packages','SourceArt'):
        for p in (ROOT/folder).rglob('*'):
            if p.is_file(): files[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    prefs={}
    for kp in (r'Software\Unity\UnityEditor\DefaultCompany\T.G - Tech Guy',r'Software\DefaultCompany\T.G - Tech Guy'):
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER,kp) as k:
                for i in range(winreg.QueryInfoKey(k)[1]):
                    name,value,kind=winreg.EnumValue(k,i)
                    if name.startswith('TechGuy'):
                        raw=value if isinstance(value,bytes) else str(value).encode()
                        prefs[kp+'/'+name]={'kind':kind,'sha256':hashlib.sha256(raw).hexdigest()}
        except FileNotFoundError: pass
    git=lambda *a:subprocess.check_output(['git',*a],cwd=ROOT,text=True,encoding='utf-8').strip()
    return {'head':git('rev-parse','HEAD'),'branch':git('branch','--show-current'),'status':git('status','--short'),'files':files,'prefs':prefs}
phase=sys.argv[1]
target=OUT/(phase+'-integrity.json')
if target.exists(): raise RuntimeError('Snapshot exists; do not overwrite')
data=snapshot()
target.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
if phase=='after':
    before=json.loads((OUT/'before-integrity.json').read_text(encoding='utf-8'))
    result={}
    for scope in ('files','prefs'):
        a,b=before[scope],data[scope]
        result[scope]={'before_count':len(a),'after_count':len(b),'changed':[k for k in a if k in b and a[k]!=b[k]],'removed':sorted(a.keys()-b.keys()),'added':sorted(b.keys()-a.keys())}
    (OUT/'integrity-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))
else: print('Protected files:',len(data['files']),'gameplay preference hashes:',len(data['prefs']))
