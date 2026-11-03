"""Deterministic atlas slicing and alpha QA; no repainting/generated-image substitution."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-05'
SOURCE=ROOT/'Tools/RunExp05/Source'
TARGET=ROOT/'Assets/_Project/Resources/UI/RunExp05'
TARGET.mkdir(parents=True,exist_ok=True)
atlas=Image.open(SOURCE/'reward-atlas-02.png').convert('RGBA')
assert atlas.width==atlas.height*2
cell=atlas.width/4
names=('Titanic','TemporalEcho','Conductor','Frost','Ignite','SpearOrbit','Judgment','Piercing')
entries=[]
for i,name in enumerate(names):
    x,y=round(i%4*cell),round(i//4*cell)
    right,bottom=round((i%4+1)*cell),round((i//4+1)*cell)
    tile=atlas.crop((x,y,right,bottom))
    bounds=tile.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    assert bounds and bounds[0]>1 and bounds[1]>1 and bounds[2]<tile.width-1 and bounds[3]<tile.height-1,(name,bounds)
    tile=tile.resize((512,512),Image.Resampling.LANCZOS)
    path=TARGET/(name+'.png');tile.save(path)
    entries.append(dict(name=name,path=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),size=tile.size,alpha=tile.getchannel('A').getextrema(),sourceCell=[x,y,right-x,bottom-y],sourceAlphaBounds=bounds,approval='PENDING DEVELOPER APPROVAL'))
(OUT/'reward-art-manifest.json').write_text(json.dumps(dict(tool='Built-in image_gen, original atlas plus one spacing correction; not external game assets',source='Tools/RunExp05/Source/reward-atlas-02.png',sourceSize=atlas.size,prompt='Tools/RunExp05/reward-art-prompt.txt',normalization='Exact4x2 crop and Lanczos512 resize only; original pixels/alpha retained',entries=entries),indent=2),encoding='utf-8')
print('Eight original illustrations sliced; alpha/cell padding verified:',atlas.size)

# Freeze actual baseline captures generated before05.1. Keep old source evidence intact.
job=json.loads((OUT/'baseline-play.json').read_text(encoding='utf-8'))['data']
index=[]
for category in ('firstsector','rewards'):
    for source in (ROOT/'Docs/validation/artifacts/run-exp-03'/category).glob('*/*'):
        if source.is_file() and job['started_unix_ms']-2000<=source.stat().st_mtime*1000<=job['finished_unix_ms']+2000:
            relative=Path('baseline')/category/source.parent.name/source.name
            target=OUT/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
            index.append(dict(source=source.relative_to(ROOT).as_posix(),copy=relative.as_posix(),sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
assert index,'Missing current baseline capture set'
(OUT/'baseline-capture-index.json').write_text(json.dumps(index,indent=2),encoding='utf-8')
print('Current baseline files preserved:',len(index))
