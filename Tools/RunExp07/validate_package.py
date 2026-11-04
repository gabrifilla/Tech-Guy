"""Offline structural QA. Does not claim browser rendering or edit any bitmap."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit,unquote
import json,struct,zlib,hashlib,zipfile,shutil,subprocess,re
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-07'
REVIEW=OUT/'review'
errors=[];refs=[];pngs=[]
class Parser(HTMLParser):
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        for key in ('src','href'):
            if key in a:refs.append((self.path,a[key]))
        if tag in ('img','script','link'):
            url=a.get('src',a.get('href',''))
            if urlsplit(url).scheme:errors.append('External display dependency:'+url)
for p in REVIEW.rglob('*.html'):
    parser=Parser();parser.path=p;parser.feed(p.read_text(encoding='utf-8'))
for p,url in refs:
    parsed=urlsplit(url)
    if parsed.scheme or not parsed.path:continue
    target=(p.parent/unquote(parsed.path)).resolve()
    if not target.is_relative_to(REVIEW.resolve()):errors.append('Path escapes package:'+str(p)+':'+url)
    elif not target.is_file():errors.append('Broken relative path:'+str(p)+':'+url)
for p in REVIEW.rglob('*.png'):
    raw=p.read_bytes()
    if raw[:8]!=b'\x89PNG\r\n\x1a\n':errors.append('Invalid PNG signature:'+str(p));continue
    w,h=struct.unpack('>II',raw[16:24]);at=8;chunks=[]
    while at<len(raw):
        length=struct.unpack('>I',raw[at:at+4])[0];typ=raw[at+4:at+8];data=raw[at+8:at+8+length];crc=struct.unpack('>I',raw[at+8+length:at+12+length])[0]
        if zlib.crc32(typ+data)&0xffffffff!=crc:errors.append('PNG CRC:'+str(p))
        chunks.append(typ.decode());at+=length+12
    if 'IEND' not in chunks:errors.append('PNG no IEND:'+str(p))
    pngs.append({'file':p.relative_to(REVIEW).as_posix(),'width':w,'height':h,'valid_chunks':len(chunks)})
manifest=json.loads((REVIEW/'asset-manifest.json').read_text(encoding='utf-8'))
for asset in manifest['assets']:
    p=REVIEW/asset['file']
    if hashlib.sha256(p.read_bytes()).hexdigest()!=asset['sha256']:errors.append('Manifest hash:'+asset['file'])
    if 'prompt' in asset and not (REVIEW/asset['prompt']).is_file():errors.append('Missing prompt:'+asset['prompt'])
    if 'reference' in asset and not (REVIEW/asset['reference']).is_file():errors.append('Missing reference:'+asset['reference'])
    if 'original' in asset:
        original=Path(asset['original'])
        if not original.is_file() or hashlib.sha256(original.read_bytes()).hexdigest()!=asset['sha256']:errors.append('Original copy mismatch:'+asset['file'])
scripts=re.findall(r'<script>(.*?)</script>',(REVIEW/'index.html').read_text(encoding='utf-8'),re.S)
syntax=[]
for script in scripts:
    result=subprocess.run(['node','--check','--input-type=commonjs'],input=script,text=True,capture_output=True)
    syntax.append({'exit_code':result.returncode,'stderr':result.stderr})
    if result.returncode:errors.append('JavaScript syntax:'+result.stderr)
# Include raw evidence and reproduction tools in the portable review.
dest=REVIEW/'evidence';dest.mkdir(exist_ok=True)
for p in (OUT/'evidence').glob('*.json'):shutil.copy2(p,dest/p.name)
src=REVIEW/'source';src.mkdir(exist_ok=True)
for p in Path(__file__).parent.glob('*.py'):shutil.copy2(p,src/p.name)
qa={'status':'PASS' if not errors else 'FAIL','relative_references_checked':len(refs),'png_count':len(pngs),'pngs':pngs,'manifest_records':len(manifest['assets']),'javascript_syntax':syntax,'errors':errors,'browser_render':'NOT TESTED — file protocol blocked by browser security policy','image_visual_review':'Six ImageGen outputs inspected; actual Unity cover/windup captures inspected','scope':'File paths, PNG signatures/chunk CRC/dimensions, manifest/original SHA256, JavaScript syntax, portable ZIP only'}
(REVIEW/'package-qa.json').write_text(json.dumps(qa,indent=2,ensure_ascii=False),encoding='utf-8')
inventory={p.relative_to(REVIEW).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in REVIEW.rglob('*') if p.is_file() and p.name!='package-inventory.json'}
(REVIEW/'package-inventory.json').write_text(json.dumps(inventory,indent=2),encoding='utf-8')
target=OUT/'RUN_EXP_07_REVIEW.zip'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(REVIEW.rglob('*')):
        if p.is_file():z.write(p,p.relative_to(OUT))
with zipfile.ZipFile(target) as z:
    bad=z.testzip()
    if bad:errors.append('ZIP CRC:'+bad)
    count=len(z.namelist())
print(json.dumps({'status':'PASS' if not errors else 'FAIL','errors':errors,'relative_references_checked':len(refs),'png_count':len(pngs),'zip_files':count,'zip_bytes':target.stat().st_size},ensure_ascii=False))
if errors:raise SystemExit(1)
