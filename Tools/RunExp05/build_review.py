"""Portable review assembled solely from actual captures, with checked local links."""
from pathlib import Path
from html import escape
from html.parser import HTMLParser
import hashlib,json,shutil,zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-05'
REVIEW=OUT/'review';REVIEW.mkdir(exist_ok=True)
SOURCE=ROOT/'Docs/validation/artifacts/run-exp-03'
used={}

def latest(area,pattern):
    values=sorted((SOURCE/area).glob('*/'+pattern))
    if not values:raise FileNotFoundError((area,pattern))
    return values[-1]

def fig(source,caption,group='runtime'):
    digest=hashlib.sha256(source.read_bytes()).hexdigest()[:10]
    name=group+'/'+digest+'-'+source.name;target=REVIEW/'images'/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    used['images/'+name]=str(source.relative_to(ROOT))
    return '<figure><a href="images/'+name+'"><img loading="lazy" src="images/'+name+'" alt="'+escape(caption)+'"></a><figcaption>'+escape(caption)+'</figcaption></figure>'

p=['<h1>Tech-Guy · Arquivo estratificado</h1><p class="lead">RUN-EXP-05 · revisão visual jogável · 02/11/2026</p><p>Direção de trabalho, pendente de aprovação física. C05 continua opcional. Imagens de Unity são capturas reais; o conceito ilustrado está identificado separadamente. Conclusão automatizada não prova sobrevivência humana ou qualidade artística aceita.</p><nav><a href="#guide">Guia</a> · <a href="#ui">Recompensas</a> · <a href="#enemy">Inimigos</a> · <a href="#room">Ambiente</a> · <a href="#play">Reproduzir</a> · <a href="#evidence">Validação e limites</a></nav>']
p+=['<h2 id="guide">Guia de linguagem visual</h2><p>Planos quebrados largos, encaixes e camadas. Cerâmica protege, grafite sustenta, cobre marca junções; turquesa indica energia contida. Vermelho de antecipação continua prioritário. O piso fica silencioso; o jogador e inimigos carregam o contraste.</p><div class="swatches">']
for color,title in [('#aca38a','Cerâmica · placas'),('#17232a','Grafite · estrutura'),('#784a2e','Cobre · encaixes'),('#32998d','Turquesa · energia seletiva'),('#29343a','Piso · valor baixo')]:p.append(f'<span style="border-top:30px solid {color}">{title}</span>')
p.append('</div><div class="compare">'+fig(ROOT/'Tools/RunExp05/Source/fragmento-concept.png','CONCEITO gerado com image_gen; referência de direção, não screenshot ou modelo final.','guide')+fig(ROOT/'Assets/_Project/Art/RunExp05/Enemies/Character_BaseColor.png','Atlas original de materiais1024; UVs por famílias. Não é pintura exclusiva final de cada parte.','guide')+'</div>')
p+=['<h2 id="ui">Recompensas · original, C04 e C05</h2><p>Mesmos offers e inventário por comparação. As três alternativas e informação focada continuam. Ilustrações originais representam efeitos existentes; confirmação, foco e detalhes têm transições breves. Motion reduzido é opcional. Nenhum ticket real é concedido ao clicar numa comparação.</p>']
for weapon in ('Gauntlet','Spear','Bow'):
    p.append('<h3>'+weapon+' · 1280×800</h3><div class="compare">')
    for name,label in [('published','UI publicada'),('Synthesis-selected-0','C04 · síntese anterior'),('Synthesis05-selected-0','C05 · estudo atual')]:p.append(fig(latest('rewards','05-1280x800-'+weapon+'-'+name+'.png'),label,'ui'))
    p.append('</div>')
p.append('<h3>Contexto, rank e acessibilidade</h3><p>Os exemplos de sinergia/rank são mostruários assistidos explícitos com definições e inventário reais do laboratório. Não são alegação de distribuição natural. Mostruário sem confirmação; nenhuma oferta futura é descrita como poder já ativo.</p><div class="compare">')
for source in sorted({latest('rewards',pattern) for pattern in ('05-assisted-*.png','05-1160x600-Bow-Synthesis05-details.png','05-1920x1080-Gauntlet-Synthesis05-selected-0.png')}):p.append(fig(source,source.stem,'ui-context'))
# Include every assisted/rank/reduced-motion state from the latest dedicated visual fixture.
latest_ui=latest('rewards','05-assisted-conductor-rank-upgrade.png').parent
for source in sorted(latest_ui.glob('05-assisted-*.png')):p.append(fig(source,source.stem,'ui-context'))
for source in sorted(latest_ui.glob('*reduced*.png')):p.append(fig(source,'Movimento reduzido · '+source.stem,'ui-context'))
p.append('</div><details><summary>Outros formatos e detalhes · três armas</summary><div class="compare">')
for resolution in ('1160x600','1920x1080'):
    for weapon in ('Gauntlet','Spear','Bow'):
        for state in ('selected-0','details'):p.append(fig(latest('rewards','05-'+resolution+'-'+weapon+'-Synthesis05-'+state+'.png'),resolution+' · '+weapon+' · '+state,'ui-aspects'))
p.append('</div></details><h2 id="enemy">Inimigos · mesma câmera e iluminação</h2><p>Troca apenas de meshes/materiais no mesmo instante nativo; pose e timing atuais são compartilhados nos pares. Fragmento remodelado emDCC, Emissor com passe secundário coerente e canal preservado. Rig rígido/IK existente, sem clipsDCC finais. Fases a seguir vêm dos executores reais.</p>')
for enemy in ('Fragmento','Emissor'):
    p.append('<h3>'+enemy+'</h3><div class="compare">')
    for version in ('04','05'):p.append(fig(latest('firstsector','05-art-'+enemy+'-'+version+'-neutral-closeup.png'),version+' · apresentação neutra','enemy'))
    p.append('</div>')
p.append('<h3>Escala de gameplay</h3><div class="compare">')
for version in ('04','05'):p.append(fig(latest('firstsector','05-art-'+version+'-normal-camera.png'),version+' · câmera normal','enemy'))
p.append('</div>')
for enemy in ('Fragmento','Emissor'):
    for phase in ('windup','attack'):
        p.append('<h3>'+enemy+' · '+phase+'</h3><div class="compare">')
        for version in ('04','05'):p.append(fig(latest('firstsector','05-art-phase-'+enemy+'-'+phase+'-'+version+'.png'),version+' · '+phase,'phases'))
        p.append('</div>')
p.append('<h3>Impacto e desmontagem</h3><div class="compare">')
for pattern in ('05-art-hit-Fragmento.png','05-art-hit-Emissor.png','05-art-disassembly.png'):p.append(fig(latest('firstsector',pattern),pattern.replace('.png',''),'phases'))
p.append('</div><h2 id="room">Ambiente · cartuchos e infraestrutura</h2><p>Volumes físicos aprovados mantidos: duas rotas, coberturas que param projéteis e pressão melee que circula. Cartuchos laminados ocupam os mesmos volumes. Racks ficam dentro das paredes e não adicionam colisão; miolo visual recuado até0,11m. Piso sem sinais que imitem ataques. Comparação abaixo mantém a pose pausada de cada par.</p>')
for kind in ('cover','open'):
    p.append('<h3>'+('Sala com cobertura' if kind=='cover' else 'Coerência limitada · aberta')+'</h3><div class="compare">')
    for version in ('before','after'):p.append(fig(latest('firstsector','05-room-'+kind+'-'+version+'-normal-camera.png'),('04' if version=='before' else '05')+' · '+kind,'room'))
    p.append('</div>')
p.append('<h3>Circulação, ameaças e LOS</h3><div class="compare">')
for pattern in ('05-room-player-circulating.png','natural-Fragmento-move-node-1.png','natural-Fragmento-windup-node-1.png','natural-Emissor-attack-node-1.png','emissor-blocked-by-cover-node-1.png','emissor-reacquired-los-node-1.png','refined-native-damage-mixed-roster.png'):
    p.append(fig(latest('firstsector',pattern),pattern.replace('.png',''),'room-runtime'))
p.append('</div><h3>Habilidades e recuperação nas três armas</h3><p>Q/E/R (slots internos0/1/2), especial3 e dash, AI ativa. Imunidade e recursos explicitamente preparados somente na fixture de capturas. Estes quadros mostram execução/leitura; atribuição deHP é validada por seleções próprias. Básicos têm fixture de colisão separada comAI congelada.</p><div class="compare">')
for weapon in (0,1,2):
    for slot in (0,1,2,3):p.append(fig(latest('firstsector',f'05-room-weapon-{weapon}-native-slot-{slot}.png'),f'Arma{weapon} · slot{slot}','weapons'))
    p.append(fig(latest('firstsector',f'05-room-weapon-{weapon}-dash-recovered.png'),f'Arma{weapon} · dash recuperado','weapons'))
p.append('</div><h2 id="play">Revisão no projeto real</h2><ol><li>Abra D:/Dev/Personal/My_games/Tech-Guy. Fora dePlay: Tools → Tech Guy → RUN-EXP-04 → Revisão → Próxima incursão seed63. Assistência desligada; entre pelo portal doNexus. Primeiro combate: cobertura e os dois candidatos com famílias antigas. Para abertura inicial, use seed0 pelo menu03.</li><li>Contorne ambos os lados, quebre/recupereLOS, observe aproximação/antecipação/impacto e experimente Gauntlet, Spear eBow com seus bindings atuais. As rotas automatizadas0/1/2 alcançam boss, tickets eNexus com conclusão de fixture; experimente a pressão pessoalmente.</li><li>Opcional durantePlay: Tools → Tech Guy → RUN-EXP-05 → Revisão → C05 temporário na run atual. Confirmar aqui consome um ticket legítimo. Restaurar recompensa publicada remove a prévia. Sair dePlay também a remove.</li><li>Laboratório separado pelo menu05: Play, escolha arma e C05/C04/Original. Mostruário assistido da arma atual é explicitamente somente leitura; botão da arma volta aos offers normais. Reduzir movimento é opção de revisão.</li><li>Ambiente04/05 durantePlay: menu05 de comparação, apenas meshes/materiais; nunca altera física ou saves. Todos os boons/bindings/weights de produção permanecem nativos.</li></ol><h2>Perfil standalone · comparação limitada</h2>')
perf_path=OUT/'performance-summary.json'
if perf_path.exists():
    perf=json.loads(perf_path.read_text(encoding='utf-8'));samples=perf.get('standalone',[])
    if samples:
        device=samples[0];p.append('<p>'+escape(device['cpu']+' · '+device['gpu'])+f' · resolução efetiva {device["width"]}×{device["height"]}. Release Mono; semUTF. Mesma população ativa por par,1,5s aquecimento e4s amostrados; AI evolui entre versões. 32 inverte a ordem. Não há ganho de otimização demonstrado.</p><table><tr><th>Ativos</th><th>Quadro p95 04→05 ms</th><th>GPU p95 04→05 ms</th><th>Triângulos rígidos 04→05</th></tr>')
        for count in (8,32,64):
            old=next(x for x in samples if x['population']==count and x['version']=='04');new=next(x for x in samples if x['population']==count and x['version']=='05')
            p.append(f'<tr><td>{count}</td><td>{old["frameMs"]["p95"]:.2f} → {new["frameMs"]["p95"]:.2f}</td><td>{old["gpuMs"]["p95"]:.2f} → {new["gpuMs"]["p95"]:.2f}</td><td>{old["triangles"]} → {new["triangles"]}</td></tr>')
        p.append('</table><p>Aumento de geometria/texturas explicitamente medido; imagens abaixo são stress fixtures, não encontros naturais nem comparação de poses idênticas. Projéteis/tells e AI não foram desligados. Profiling build usa produto separado; app.info confirma TechGuyReviewLocal/RUNEXP05Profile. Adições prévias no registro do Player original foram preservadas e registradas com limites de atribuição.</p><details><summary>Seis capturas do aplicativo de perfil</summary><div class="compare">')
        for source in sorted((ROOT/'Builds/RunExp05Profile/ProfileResults').glob('*.png')):p.append(fig(source,source.stem+' · stress controlado','profile'))
        p.append('</div></details>')
p.append('<h2 id="evidence">Validação técnica e limites</h2><table><tr><th>Seleção</th><th>Job</th><th>Resultado</th></tr>')
data=REVIEW/'data';data.mkdir(exist_ok=True);jobs=[]
for source in sorted(OUT.glob('*.json')):
    value=json.loads(source.read_text(encoding='utf-8-sig'))
    if not isinstance(value,dict):continue
    item=value.get('data',value);result=item.get('result') if isinstance(item,dict) else None
    if isinstance(result,dict) and 'summary' in result:
        jobs.append({'file':source.name,'job':item.get('job_id'),**result['summary']});shutil.copy2(source,data/source.name)
        p.append(f'<tr><td><a href="data/{source.name}">{source.name}</a></td><td>{item.get("job_id")}</td><td>{result["summary"]["passed"]}/{result["summary"]["total"]} · {result["summary"]["resultState"]}</td></tr>')
    elif isinstance(item,dict) and item.get('status')=='failed':
        shutil.copy2(source,data/source.name)
        p.append(f'<tr><td><a href="data/{source.name}">{source.name}</a></td><td>{item.get("job_id")}</td><td>Não iniciou · '+escape(item.get('error','Falha sem resumo'))+'</td></tr>')
    elif source.name in ('performance-summary.json','integrity-final.json','asset-manifest.json','standalone-build.json','validation-selections.json'):
        shutil.copy2(source,data/source.name)
p.append('</table><p>Seleções sobrepostas não somam testes únicos. Consulte falhas intermediárias nos relatórios. Não houve execução universal da suíte histórica. Custos deEditor incluemUTF/LINQ/IMGUI; standalone é amostra release limitada, mesma população com posições evoluindo entre pares e jogador sem ataque. GPU ausente é desconhecido, nunca custo zero. Não há orçamento de hardware aceito.</p>')
for name in ('performance-summary.json','integrity-final.json','asset-manifest.json','standalone-build.json','validation-selections.json'):
    if (data/name).exists():p.append('<p><a href="data/'+name+'">'+name+'</a></p>')
p.append('<p><strong>DONE:</strong> recorte técnico disponível com evidências. <strong>PENDING DEVELOPER APPROVAL:</strong> C05, força visual dos modelos/ambiente e legibilidade sob pressão. <strong>NOT DONE:</strong> rig deformável/clips finaisDCC, arte de catálogo completo, redesenho global de cenários. <strong>NOT TESTED:</strong> desempenho emoutros hardwares e combinações cartesianas de todo boon/arma/inimigo. STOP05.4; nenhuma autorização06 presumida.</p>')
reports=REVIEW/'reports';reports.mkdir(exist_ok=True)
p.append('<h3>Relatórios completos · texto incluído no pacote</h3><ul>')
for source in sorted((ROOT/'Docs/decisions').glob('RUN_EXP_05_*.md')):
    name=source.stem+'.txt';(reports/name).write_text(source.read_text(encoding='utf-8'),encoding='utf-8');p.append(f'<li><a href="reports/{name}">{source.stem}</a></li>')
source=ROOT/'Docs/decisions/MAP_ART_2_5D_FUTURE_PLAN.md';name=source.stem+'.txt';(reports/name).write_text(source.read_text(encoding='utf-8'),encoding='utf-8')
p.append(f'<li><a href="reports/{name}">Pedido futuro · mapas híbridos2.5D</a></li></ul><p>Arquivos de texto preservam os relatórios para leitura offline; suas referências de projeto indicam os caminhos do repositório original. Todas as imagens desta página estão incluídas localmente.</p>')
page='<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Tech-Guy · RUN-EXP-05</title><style>body{background:#121c22;color:#e4dfd0;font:17px system-ui;margin:32px auto;padding:0 24px;max-width:1500px}h1,h2,h3{font-weight:650;color:#d2bd92}h1{font-size:44px}h2{margin-top:64px;border-top:1px solid #44515a;padding-top:22px}p,li{line-height:1.65;max-width:1150px}.lead{font-size:22px;color:#9eaaa8}a{color:#88c6c0}nav{padding:22px 0}.compare{display:grid;grid-template-columns:repeat(auto-fit,minmax(330px,1fr));gap:18px}figure{margin:8px 0 22px}img{width:100%;height:auto;display:block;background:#1e2a31;border:1px solid #48585e}figcaption{padding:10px 0;color:#b9c1b9;font-size:15px}.swatches{display:flex;gap:12px;flex-wrap:wrap}.swatches span{padding:12px;width:180px}td,th{border:1px solid #48585e;padding:8px;text-align:left}table{border-collapse:collapse;word-break:break-word}details{padding:15px;border:1px solid #48585e}</style>'+''.join(p)+'</html>'
(REVIEW/'index.html').write_text(page,encoding='utf-8')
missing=[]
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('src','href') and not value.startswith('#') and not (REVIEW/value).exists():missing.append(value)
Links().feed(page)
if missing:raise RuntimeError(missing)
(REVIEW/'sources.json').write_text(json.dumps(used,indent=2,ensure_ascii=False),encoding='utf-8')
archive_path=OUT/'RUN_EXP_05_REVIEW.zip'
with zipfile.ZipFile(archive_path,'w',zipfile.ZIP_DEFLATED) as archive:
    for source in REVIEW.rglob('*'):
        if source.is_file():archive.write(source,source.relative_to(REVIEW))
with zipfile.ZipFile(archive_path) as archive:crc=archive.testzip()
check={'missing':missing,'images':len(used),'jobs':jobs,'zipBytes':archive_path.stat().st_size,'crcError':crc}
(OUT/'review-check.json').write_text(json.dumps(check,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'images':len(used),'jobs':len(jobs),'missing':missing,'crc':crc,'zipBytes':check['zipBytes']}))
