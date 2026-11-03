"""Copy actual Unity captures into a portable, checked review package. No rendered mockups."""
from pathlib import Path
import hashlib
import html
from html.parser import HTMLParser
import json
import shutil
import zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-04'
REVIEW=OUT/'review'
REVIEW.mkdir(parents=True,exist_ok=True)
OLD=ROOT/'Docs/validation/artifacts/run-exp-03'

def copy(source,category):
    relative=Path('images')/category/source.parent.name/source.name
    target=REVIEW/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    return relative.as_posix()

def figure(source,caption,category):
    path=copy(source,category)
    return f'<figure><a href="{html.escape(path)}"><img loading="lazy" src="{html.escape(path)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>'

def latest(folder,pattern):
    found=sorted(p for p in (OLD/folder).glob('*/'+pattern) if p.parent.name.startswith('20261102'))
    if not found:raise RuntimeError('Required real capture missing: '+pattern)
    return found[-1]

parts=['<h1>Tech Guy — RUN-EXP-04</h1><p>Comparação real na Unity. UI ainda não publicada; arte intermediária. Clique numa captura para ampliar. Resultados técnicos não representam aprovação humana.</p><nav><a href="#ui">Recompensas</a> · <a href="#art">Inimigos</a> · <a href="#rooms">Salas</a> · <a href="#play">Como testar</a> · <a href="#tests">Evidências</a></nav><h2 id="ui">Original / A / B / C</h2><p>Mesmas ofertas e inventário em cada linha. Sem aquisição nestas capturas. C mantém três alternativas compactas e apresenta uma transformação por vez; não recomenda uma build obrigatória.</p>']
ui=latest('rewards','1280x800-Gauntlet-Synthesis-selected-0.png').parent
for family in ('Gauntlet','Bow','Spear'):
    for resolution in ('1280x800','1160x600'):
        parts.append(f'<h3>{family} · {resolution}</h3><div class="compare">')
        for suffix,label in (('original','Publicada'),('Artifacts-selected-0','A — Artefatos'),('Reconfiguration-selected-0','B — Reconfiguração'),('Synthesis-selected-0','C — Síntese')):
            source=ui/f'{resolution}-{family}-{suffix}.png'
            if not source.exists():raise RuntimeError(str(source))
            parts.append(figure(source,label,'rewards'))
        parts.append('</div>')
parts.append('<h3>Seleção, detalhes e contexto adquirido</h3><p>Sinergia/rank são fixtures visuais assistidas com aquisições reais em runtime e lista comparativa explicitamente montada; não são prova de obtenção natural ou de RNG.</p><div class="compare">')
for source,caption in ((ui/'1280x800-Gauntlet-Synthesis-selected-1.png','Outra seleção, sem aquisição'),(ui/'1280x800-Spear-Synthesis-details.png','Detalhes acessíveis'),(latest('rewards','assisted-owned-titan-Synthesis.png'),'Titanic adquirido: contexto real de Conductor'),(latest('rewards','04-assisted-rank-upgrade-Synthesis.png'),'Protocolo já adquirido: evolução de rank')):
    parts.append(figure(source,caption,'rewards'))
parts.append('</div><h2 id="art">Geometria antes / depois</h2><p>Mesmo ator, pose pausada, câmera, escala e iluminação. Par controlado pelo diretor real; imunidade apenas para comparação visual. Revisão04 não equivale a arte final ou aprovação física.</p>')
for enemy in ('Fragmento','Emissor'):
    parts.append('<h3>'+enemy+'</h3><div class="compare">')
    for version in ('03','04'):
        parts.append(figure(latest('firstsector',f'04-art-{enemy}-{version}-identical-closeup.png'),version+' · mesma câmera','art'))
    parts.append('</div>')
parts.append('<div class="compare">')
for version in ('03','04'):
    parts.append(figure(latest('firstsector',f'04-art-comparison-{version}-normal-camera.png'),version+' · câmera de gameplay','art'))
parts.append('</div><h3>Preparação e ataque em câmera de gameplay — antes / depois</h3><p>Executores nativos alcançam cada fase; congela-se o mesmo instante e trocam-se apenas as malhas03/04. Poses/timing são os da revisão atual em ambos os lados, para comparar forma durante a ação, sem fingir uma nova cadência de dano.</p>')
for enemy in ('Fragmento','Emissor'):
    for phase in ('windup','attack'):
        parts.append('<h4>'+enemy+' · '+phase+'</h4><div class="compare">')
        for version in ('03','04'):
            parts.append(figure(latest('firstsector',f'04-art-phase-{enemy}-{phase}-{version}-normal-camera.png'),version+' · mesmo instante nativo','art-phases'))
        parts.append('</div>')
parts.append('<h2 id="rooms">Salas antes / depois</h2><p>Mesma pose/câmera por par. Colliders, duas circulações e LOS foram verificados separadamente das imagens. Imunidade somente na comparação visual.</p>')
for kind in ('open','cover'):
    parts.append('<h3>'+('Arena aberta' if kind=='open' else 'Arena com cobertura')+'</h3><div class="compare">')
    for state in ('before','after'):
        parts.append(figure(latest('firstsector',f'04-room-{kind}-{state}-normal-camera.png'),state+' · câmera normal','rooms'))
    parts.append('</div>')
parts.append('<h3>Movimento, ataque e LOS — instantes reais</h3><div class="compare">')
for pattern in ('natural-Fragmento-windup*.png','natural-Fragmento-attack*.png','natural-Emissor-windup*.png','natural-Emissor-attack*.png','emissor-blocked-by-cover*.png','emissor-reacquired-los*.png','04-art-disassembly.png'):
    parts.append(figure(latest('firstsector',pattern),pattern.replace('*.png','').replace('.png',''),'runtime'))
parts.append('</div><h3>Progressão e recuperação de controle</h3><p>As duas primeiras capturas vêm de rotas naturais automatizadas. As demais vêm da matriz de rosters controlados com ganchos nativos, sem imunidade; movimento e dash após soltura foram verificados por input. Não são playthrough humano.</p><div class="compare">')
for pattern in ('node-1-open-combat.png','node-1-cover-combat.png','04-mixed-hooks-1-seed-63-action-window.png','04-mixed-hooks-2-seed-63-action-window.png'):
    parts.append(figure(latest('firstsector',pattern),pattern.replace('.png',''),'routes'))
parts.append('</div><h2 id="play">Como testar na Unity</h2><ol><li>Projeto D:/Dev/Personal/My_games/Tech-Guy. Tools → Tech Guy → RUN-EXP-04 → Revisão → Próxima incursão seed63, fora de Play. Assistência de recompensas desligada. Use o portal normal do Nexus.</li><li>Primeiro combate63: dupla e cobertura no sorteio legítimo, junto de famílias antigas. Para sala inicial aberta: Tools → Tech Guy → RUN-EXP-03 → Próxima incursão com seed 0. Não há garantia dessa sequência em toda run.</li><li>Contorne as massas, quebre LOS e retorne à linha de tiro. Observe a pressão melee e a retaguarda. Teste as três armas, skills, especial e dash conforme seus bindings atuais.</li><li>Abra o laboratório de recompensas pelo mesmo menu, entre em Play e alterne Original/A/B/C e armas. Selecionar não concede; confirmar concede somente ao inventário temporário do laboratório. Sair de Play descarta a revisão.</li><li>Para ofertas reais de uma run: menu Síntese C temporária na run atual. Uma confirmação nesse contexto consome o ticket real. Restaurar recompensa publicada remove a prévia. Nenhuma cena salva instala C automaticamente.</li><li>Menus Salas antes/com polimento comparam somente apresentação durante Play; não alteram colisões nem saves.</li></ol><h2 id="tests">Evidências técnicas</h2><table><tr><th>Artefato</th><th>Job</th><th>Resultado</th></tr>')
jobs=[]
data_dir=REVIEW/'data';data_dir.mkdir(exist_ok=True)
for source in sorted(OUT.glob('*.json')):
    value=json.loads(source.read_text(encoding='utf-8'))
    if not isinstance(value,dict):continue
    data=value.get('data',value);result=data.get('result') if isinstance(data,dict) else None
    if isinstance(result,dict) and 'summary' in result:
        summary=result['summary'];jobs.append({'file':source.name,'job':data.get('job_id'),**summary});shutil.copy2(source,data_dir/source.name)
        parts.append(f'<tr><td><a href="data/{source.name}">{source.name}</a></td><td>{data.get("job_id")}</td><td>{summary["passed"]}/{summary["total"]} · {summary["resultState"]}</td></tr>')
parts.append('</table>')
for name in ('performance-summary.json','integrity-final.json','role-audit.json','asset-manifest.json'):
    source=OUT/name
    if source.exists():shutil.copy2(source,data_dir/name);parts.append(f'<p><a href="data/{name}">{name}</a></p>')
parts.append('<p>Decisões humanas pendentes: direção de UI, arte final e pressão/legibilidade dos encontros. A/B não foram aprovados, e C não é publicado por recomendação técnica. STOP04.5; sem novos boons, bosses, Singularity ou módulosQ/E/R.</p>')
page='<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>RUN-EXP-04 — revisão</title><style>body{margin:28px;background:#10181e;color:#e8e3d6;font:17px system-ui}h1,h2,h3{color:#e6c58e}p,li{max-width:1150px;line-height:1.6}a{color:#82d1cd}nav{margin:24px 0}.compare{display:grid;grid-template-columns:repeat(auto-fit,minmax(285px,1fr));gap:15px}figure{margin:0 0 26px}img{width:100%;border:1px solid #39515e}figcaption{padding:9px}table{border-collapse:collapse}td,th{border:1px solid #39515e;padding:8px;text-align:left}</style>'+''.join(parts)+'</html>'
(REVIEW/'index.html').write_text(page,encoding='utf-8')
missing=[]
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src') and not value.startswith('#') and not (REVIEW/value).exists():missing.append(value)
Links().feed(page)
if missing:raise RuntimeError('Missing review links: '+str(missing))
(OUT/'review-check.json').write_text(json.dumps({'missing':missing,'images':len(list((REVIEW/'images').rglob('*.png'))),'jobs':jobs},indent=2,ensure_ascii=False),encoding='utf-8')
with zipfile.ZipFile(OUT/'RUN_EXP_04_REVIEW.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for source in REVIEW.rglob('*'):
        if source.is_file():archive.write(source,source.relative_to(REVIEW))
print('Portable review:',len(list((REVIEW/'images').rglob('*.png'))),'images;',len(jobs),'jobs; missing links:',len(missing))
