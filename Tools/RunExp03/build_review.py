"""Assemble existing Unity evidence; does not render or invent gameplay captures."""
import html
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/validation/artifacts/run-exp-03'

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding='utf-8')

def percentile(values, fraction):
    values = sorted(values)
    return values[min(len(values)-1, int((len(values)-1)*fraction))]

costs = {}
for path in sorted((OUT / 'firstsector').glob('*/integrated-active-*.json')):
    data = json.loads(path.read_text(encoding='utf-8'))
    samples = data.pop('samples')
    data['source'] = path.relative_to(OUT).as_posix()
    data['samples'] = len(samples)
    data['frameMs'] = {str(p): percentile([s['dtMs'] for s in samples], p) for p in (.5,.95,.99)}
    data['active_min'] = min(s['active'] for s in samples)
    for key in ('moving', 'windups', 'projectiles', 'drawCalls', 'batches', 'queue', 'roots'):
        if key in samples[0]:
            data[key] = {'min': min(s[key] for s in samples), 'max': max(s[key] for s in samples), 'p95': percentile([s[key] for s in samples], .95)}
    data['markers'] = {name: {'p50': percentile([s['markers'][i] for s in samples], .5), 'p95': percentile([s['markers'][i] for s in samples], .95)} for i,name in enumerate(data['markerNames'])}
    costs[data['count']] = data
write('performance-summary.json', [costs[n] for n in sorted(costs)])

assets = []
for folder in ('Assets/_Project/Art/RunExp03', 'Assets/_Project/Resources/UI/RunExp03', 'Assets/_Project/Prefabs/FirstSector/RunExp03'):
    for path in sorted((ROOT/folder).rglob('*')):
        if path.is_file():
            assets.append({'path': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
write('asset-manifest.json', {'origin': 'Geometria facetada, materiais e glifos originais autorados em C# nos builders RunExp03EnemyArtAuthoring e RunExp03RewardsAuthoring. Prefabs derivados dos candidatos RUNEXP02 preservados. Sem Blender executado, gerador externo, texturas de terceiros ou alegação de arte final.', 'textures': 'PNG RGBA 512x512, sRGB, alphaIsTransparency, bilinear, sem mipmap, uncompressed; importer versionado em .meta.', 'uv': 'UV planar simples das malhas novas. Materiais URP/Lit; sem atlas de produção ou textura final.', 'files': assets})

jobs = []
for path in sorted(OUT.glob('*.json')):
    try:
        value = json.loads(path.read_text(encoding='utf-8'))
        data = value.get('data', value)
        if data.get('result', {}).get('summary'):
            jobs.append({'file': path.name, 'job': data.get('job_id'), **data['result']['summary']})
    except (ValueError, AttributeError, TypeError):
        pass
write('test-index.json', jobs)

def figure(path, caption):
    return f'<figure><a href="{html.escape(path)}"><img loading="lazy" src="{html.escape(path)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>'

sections = ['<h1>Tech Guy — revisão RUN-EXP-03</h1><p>Capturas reais da Unity. Integração de gameplay disponível na FirstSector. UI e arte dependem de aprovação humana. Abra as imagens para ampliar.</p><p>Na Unity: Tools → Tech Guy → RUN-EXP-03. Para cobertura e dupla na primeira sala, configure a próxima incursão com seed 63 e use o portal normal do Nexus. A seed é consumida uma vez. Assistência de recompensas deve ficar desligada.</p>']
for family in ('Gauntlet','Bow','Spear'):
    groups = sorted((OUT/'rewards').glob(f'*/1280x800-{family}-original.png'))
    if not groups:
        continue
    folder = groups[-1].parent
    sections.append('<h2>'+html.escape(family)+' — mesmas ofertas, nenhuma aquisição</h2><div class="compare">')
    for suffix,caption in (('original','Atual publicada'),('Artifacts-selected-0','Conceito A — artefatos'),('Reconfiguration-selected-0','Conceito B — reconfiguração')):
        path = folder/f'1280x800-{family}-{suffix}.png'
        if path.exists():
            sections.append(figure(path.relative_to(OUT).as_posix(),caption))
    sections.append('</div>')
sections.append('<h2>Contexto já adquirido</h2><p>Fixture visual assistida: Punhos Titânicos realmente aplicado ao inventário runtime. A lista comparativa foi montada explicitamente; não prova RNG nem obtenção natural.</p><div class="compare">')
for name in ('assisted-owned-titan-Artifacts.png','assisted-owned-titan-Reconfiguration.png'):
    matches = sorted((OUT/'rewards').glob('*/'+name))
    if matches:
        sections.append(figure(matches[-1].relative_to(OUT).as_posix(),name))
sections.append('</div><h2>FirstSector — gameplay e refino intermediário</h2><p>Rotas: rosters e tickets reais, travessia/abates automatizados e jogador imune declarados. LOS/lifecycle: fixture posicional na sala real, AI ativa. Close-ups usam câmera temporária; câmera normal foi restaurada.</p><div class="compare">')
for pattern in ('*cover-combat.png','*open-combat.png','*blocked-by-cover*.png','*reacquired-los*.png','fragmento-refined-closeup-real-runtime.png','emissor-refined-closeup-real-runtime.png','natural-Fragmento-windup*.png','natural-Emissor-attack*.png','refined-native-despawn.png'):
    matches = sorted((OUT/'firstsector').glob('*/'+pattern))
    if matches:
        sections.append(figure(matches[-1].relative_to(OUT).as_posix(),matches[-1].name))
sections.append('</div><h2>Validação</h2><table><tr><th>Artefato</th><th>Job</th><th>Resultado</th></tr>')
for job in jobs:
    sections.append(f'<tr><td><a href="{job["file"]}">{job["file"]}</a></td><td>{job["job"]}</td><td>{job["passed"]}/{job["total"]}, {job["resultState"]}</td></tr>')
sections.append('</table><p><a href="performance-summary.json">Medições Editor 8/32/64</a> · <a href="asset-manifest.json">Origem dos assets</a> · <a href="../../../decisions/RUN_EXP_03_RELATORIO.md">Relatório e instruções de playtest</a></p>')
page = '<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>RUN-EXP-03 — revisão</title><style>body{margin:30px;background:#0d151c;color:#e5e3db;font:16px system-ui}h1,h2{color:#f0c17a}p{max-width:1000px;line-height:1.6}a{color:#7ad8d3}.compare{display:grid;grid-template-columns:repeat(auto-fit,minmax(350px,1fr));gap:18px}figure{margin:0 0 24px}img{width:100%;border:1px solid #37525c}figcaption{padding:10px}table{border-collapse:collapse}td,th{padding:8px;border:1px solid #37525c;text-align:left}</style>'+''.join(sections)+'</html>'
(OUT/'REVIEW.html').write_text(page, encoding='utf-8')
print('Review assembled from existing evidence:', len(jobs), 'jobs;', len(assets), 'asset files')
