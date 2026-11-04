from pathlib import Path
import hashlib,json,winreg,subprocess,sys
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-09/evidence';OUT.mkdir(parents=True,exist_ok=True)
phase=sys.argv[1];target=OUT/(phase+'-integrity.json')
if target.exists():raise RuntimeError('Do not overwrite entry evidence')
files={}
for directory in ('Assets/_Project','ProjectSettings','Packages','SourceArt'):
    for p in (ROOT/directory).rglob('*'):
        if p.is_file():files[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
prefs={}
for kp in (r'Software\Unity\UnityEditor\DefaultCompany\T.G - Tech Guy',r'Software\DefaultCompany\T.G - Tech Guy'):
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,kp) as k:
            for i in range(winreg.QueryInfoKey(k)[1]):
                name,value,kind=winreg.EnumValue(k,i)
                if name.startswith('TechGuy'):
                    value=value if isinstance(value,bytes) else str(value).encode()
                    prefs[kp+'/'+name]={'kind':kind,'sha256':hashlib.sha256(value).hexdigest()}
    except FileNotFoundError:pass
git=lambda *a:subprocess.check_output(['git',*a],cwd=ROOT,text=True,encoding='utf-8').strip()
data={'files':files,'prefs':prefs,'head':git('rev-parse','HEAD'),'status':git('status','--short')}
target.write_text(json.dumps(data,indent=2),encoding='utf-8')
if phase in ('after','final','closure'):
    b=json.loads((OUT/'before-integrity.json').read_text());result={}
    for scope in ('files','prefs'):
        old,new=b[scope],data[scope]
        result[scope]={'before':len(old),'after':len(new),'changed':[k for k in old if k in new and old[k]!=new[k]],'removed':sorted(old.keys()-new.keys()),'added':sorted(new.keys()-old.keys())}
    (OUT/'integrity-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
else:print('Entry hashes',len(files),'preferences',len(prefs))

