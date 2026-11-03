"""Portable local review from real06 captures, reports and catalogue; no remote assets."""
from pathlib import Path
import csv, hashlib, html, json, re, shutil, zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-06'
REVIEW=OUT/'review';ASSETS=REVIEW/'assets';REPORTS=REVIEW/'reports'
ASSETS.mkdir(parents=True,exist_ok=True);REPORTS.mkdir(exist_ok=True)
manifest=[]
def picture(path,caption):
    if not path or not path.exists():raise FileNotFoundError(str(path))
    name=f'{len(manifest):02d}-{path.name}';target=ASSETS/name;shutil.copy2(path,target)
    manifest.append({'source':path.relative_to(ROOT).as_posix(),'copy':'assets/'+name,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
    return f'<figure><a href="assets/{html.escape(name)}"><img loading="lazy" src="assets/{html.escape(name)}" alt="{html.escape(caption)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>'
def choose(folder,pattern):
    found=sorted(folder.glob(pattern))
    if not found:raise FileNotFoundError(str(folder/pattern))
    return found[-1]
routes={}
for text in sorted((OUT/'gameplay').glob('*/route06.txt')):
    body=text.read_text(encoding='utf-8');line=body.splitlines()[1]
    if 'random=False' in line:routes[int(re.search(r'weapon=(\d)',line)[1])]=(text.parent,line)
game=''
for weapon,title in [(0,'Manopla ·1160×600'),(1,'Arco ·1280×800'),(2,'Lança ·1920×1080')]:
    folder,line=routes[weapon]
    game+=f'<h3>{title}</h3><p class="meta">{html.escape(line)}</p><div class="grid">'
    game+=picture(choose(folder,'06-published-C05-*-node-1-ticket-0.png'),'Ticket real, C05 instalada pelo owner normal; não é mostruário.')
    game+=picture(choose(folder,'06-normal-*-cover-*.png'),'Encontro coberto no fluxo normal; conclusão de fixture automatizada.')
    game+=picture(choose(folder,'06-returned-Nexus-*.png'),'Retorno ao Nexus após boss/extração pela autoridade nativa.')+'</div>'
game+='<h3>MouseWindows em recompensa real</h3><div class="grid">'+picture(OUT/'mouse-real-ticket-details.png','Punho Ecoante: detalhes abertos pelo mouse, antes de adquirir.')+picture(OUT/'mouse-next-real-ticket.png','Oferta seguinte aberta após adquirir uma recompensa; categoria e ordinal reais.')+'</div>'

comparisons='<div class="grid">'
comparisons+=picture(choose(OUT/'baseline/rewards','*/1280x800-Gauntlet-original.png'),'Baseline06: apresentação legada, captura isolada de ofertas reais; anterior à publicação.')
comparisons+=picture(choose(OUT/'final/rewards','*/05-1280x800-Gauntlet-Synthesis05-selected-0.png'),'C05 compartilhada na comparação isolada final. A prova de publicação é a rota normal acima.')+'</div>'
comparisons+='<p class="note">As duas imagens acima pertencem a capturas distintas; não afirmam os mesmos IDs sorteados. Dentro da matriz final, cada comparadorOriginal/C04/C05 usa o mesmo conjunto de ofertas e não concede recompensas durante a comparação.</p>'

actors='<div class="grid">'
for pattern,caption in [('*/*fragmento-refined-closeup-real-runtime.png','Fragmento aprovado: captura do ator real em execução.'),('*/*emissor-refined-closeup-real-runtime.png','Emissor aprovado: conchas e orientação do ator real.'),('*/*fragmento-native-hit-control.png','Controle nativo finito; captura com relógio pausado para preservar a janela.'),('*/*emissor-native-hit-control.png','Mesmo teste de pausa/controle no Emissor, sem retunar sua duração.')]:
    actors+=picture(choose(OUT/'final/firstsector',pattern),caption)
actors+='</div>'

catalogue=sorted(OUT.glob('catalogue/*/catalogue06.json'))[-1];catalog=json.loads(catalogue.read_text(encoding='utf-8'))
families='<div class="grid">'
for path in sorted(catalogue.parent.glob('*.png')):
    if 'details' in path.name or 'family' in path.name or 'continu' in path.name:families+=picture(path,'Mostruário assistido somente leitura · '+path.stem)
families+='</div>'
rows=list(csv.DictReader((OUT/'reward-catalogue.csv').open(encoding='utf-8-sig')))
missing='<table><thead><tr><th>Nome</th><th>Família</th><th>Armas</th><th>ID</th></tr></thead><tbody>'
for row in rows:
    if row['ilustracao_dedicada']!='False':continue
    missing+='<tr>'+''.join('<td>'+html.escape(row[k])+'</td>' for k in ('nome','familia','armas','id'))+'</tr>'
missing+='</tbody></table>'

tests=''
for name in ('final-play','regression','skill-effects','extra-combo-camera-final','final-edit'):
    d=json.loads((OUT/(name+'.json')).read_text(encoding='utf-8'))['data'];s=d['result']['summary']
    tests+=f'<tr><td>{name}</td><td>{s["passed"]}/{s["total"]}</td><td><code>{d["job_id"]}</code></td><td>{s["durationSeconds"]:.3f}s</td></tr>'
performance=json.loads((OUT/'performance-summary.json').read_text(encoding='utf-8'));perfrows=''
for count in (8,32,64):
    r=next(r for r in performance['standalone'] if r['population']==count and r['version']=='05')
    perfrows+=f'<tr><td>{count}</td><td>{r["frameMs"]["p95"]:.2f}ms</td><td>{r["gpuMs"]["p95"]:.2f}ms</td><td>{r["allocatedMemoryBytes"]["p95"]/1048576:.2f}MiB</td></tr>'
standalone='<div class="grid">'+''.join(picture(OUT/f'standalone-{n}-05.png',f'Standalone atual06, arte05, {n} inimigos ativos; jogador imune/ocioso na fixture.') for n in (8,32,64))+'</div>'

links=''
for path in sorted((ROOT/'Docs/decisions').glob('RUN_EXP_06_*.md')):
    if 'RESUME_CHECKPOINT' in path.name:continue
    target=REPORTS/(path.stem+'.txt');shutil.copy2(path,target);links+=f'<li><a href="reports/{target.name}">{path.stem}</a></li>'
path=ROOT/'Docs/decisions/RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md';target=REPORTS/(path.stem+'.txt');shutil.copy2(path,target);links+=f'<li><a href="reports/{target.name}">Brief07 · somente proposta futura</a></li>'
for name in ('reward-catalogue.csv','reward-illustrations-missing.md','performance-summary.json','integrity-final.json','mouse06-contract.txt'):
    shutil.copy2(OUT/name,REPORTS/name);links+=f'<li><a href="reports/{name}">{name}</a></li>'

page='''<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tech-Guy · RUN-EXP-06</title>
<style>:root{color-scheme:dark;--bg:#0b1117;--panel:#142027;--paper:#eee6d2;--muted:#a8b8ba;--accent:#e2a36d;--teal:#81c8c3}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--bg);color:var(--paper);font:17px/1.65 system-ui,sans-serif}main{max-width:1450px;margin:auto;padding:40px clamp(18px,4vw,65px) 90px}a{color:var(--teal)}nav{display:flex;gap:22px;flex-wrap:wrap;padding:16px 0;border-block:1px solid #314149}nav a{text-decoration:none;font-size:14px}h1{font-size:clamp(36px,5vw,72px);line-height:1.1;letter-spacing:-2px;max-width:1050px;margin:15px 0 24px}h2{font-size:31px;line-height:1.25;margin:0 0 20px}h3{font-size:22px;margin-top:32px}section{padding:55px 0;border-bottom:1px solid #314149}.eyebrow{font-size:13px;letter-spacing:3px;text-transform:uppercase;color:var(--accent)}.lead{max-width:1000px;font-size:21px;color:#c7d2cf}.tags{display:flex;gap:12px;flex-wrap:wrap}.tags span{border:1px solid #36504e;background:#183132;padding:8px 14px;font-size:14px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,350px),1fr));gap:20px}figure{margin:0;background:var(--panel);border:1px solid #314149;overflow:hidden}img{display:block;width:100%;height:auto}figcaption{font-size:14px;color:var(--muted);padding:14px 18px}.meta,code{font-size:12px;overflow-wrap:anywhere;color:var(--muted)}.note{padding:18px 22px;background:#18242a;border-left:3px solid var(--accent)}table{border-collapse:collapse;width:100%;display:block;overflow-x:auto;margin:22px 0;font-size:14px}td,th{text-align:left;border-bottom:1px solid #34434a;padding:12px 16px;vertical-align:top}th{color:var(--accent)}.columns{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:25px}.card{padding:25px;background:var(--panel)}li{margin:8px 0}footer{color:var(--muted);font-size:14px;padding-top:30px}</style>
<main><header><p class="eyebrow">Tech-Guy / RUN-EXP-06 / 03 NOV 2026</p><h1>A apresentação aprovada entrou na run.</h1><p class="lead">SínteseC05 nos tickets normais. Inimigos e salas05 preservados. Evidência atual de três armas, catálogo completo, interação e desempenho — com os limites de cada prova declarados.</p><div class="tags"><span>01–05 aprovados pelo desenvolvedor</span><span>32 IDs alcançáveis auditados</span><span>07 apenas especificado</span></div></header>
<nav><a href="#run">Fluxo produtivo</a><a href="#before">Antes/depois</a><a href="#actors">Inimigos</a><a href="#catalogue">Catálogo</a><a href="#proof">Validação/custo</a><a href="#future">Próximos mapas</a><a href="#reports">Relatórios</a></nav>
<section id="run"><p class="eyebrow">01 / Fluxo real</p><h2>Nexus → salas → C05 → boss → Nexus</h2><p>As rotas usam diretor, roster, RNG e tickets normais. Warp, dano letal, imunidade e acesso temporário ao loadout pertencem à fixture; não são sobrevivência humana ou desbloqueios conquistados. Não há grants de boons nem ofertas forçadas. Uma quarta rota sem assistência usou seed aleatória.</p>'''+game+'''</section>
<section id="before"><p class="eyebrow">02 / Publicação</p><h2>Uma implementação compartilhada, sob o owner nativo</h2><p>RunRewardUI cria uma única viewC05; RunBoons.Choose continua autoridade. Comparadores preservados como ferramentas de desenvolvimento. Os números de dano não atravessam mais a tela de recompensas.</p>'''+comparisons+'''</section>
<section id="actors"><p class="eyebrow">03 / Consolidação</p><h2>Arte05 já ativa, sem novos registros ou tuning</h2><p>Fragmento:18 renderers/5772triângulos rígidos; Emissor:7/2172. Física, cápsulas, ataque, reação e navegação permanecem nativos. As salas mantêm duas circulações e bloqueio/readquisição de LOS. Não é rig ou animaçãoDCC final.</p>'''+actors+'''</section>
<section id="catalogue"><p class="eyebrow">04 / Conteúdo alcançável</p><h2>Arte correta quando existe; fallback claro quando falta</h2><p>40 linhas por arma,32 IDs com elegibilidade nativa demonstrada emfixture isolada.8 IDs usam7 ilustrações dedicadas;24 IDs usam marcas de família. Piercing é imagem preservada deID fora do pool. O mostruário abaixo é assistido/somente leitura, não RNG nem aquisição de run. Continuação diz SEM AQUISIÇÃO.</p>'''+families+'''<details><summary>24 ilustrações dedicadas ainda incompletas</summary>'''+missing+'''</details></section>
<section id="proof"><p class="eyebrow">05 / Provas e limites</p><h2>Seleções executadas nesta entrega</h2><table><thead><tr><th>Seleção</th><th>Passaram</th><th>Job</th><th>Duração</th></tr></thead><tbody>'''+tests+'''</tbody></table><p>Sem alegação de suíte global verde. Falhas intermediárias e correções de fixture estão preservadas no06.4. O checker ainda aponta um missing-script histórico emPlayground e um aviso de filename. Console final0erros;47 preferências existentes iguais nos escopos observados. Valores privados não entram no pacote.</p>
<h3>Standalone atual ·1920×1080</h3><p>Mesmo executável06 com o harness de comparação04/05 já existente, AI/nav/dano/poses ativos, jogador imune/ocioso. Variações de fase/posição impedem atribuir diferenças de tempo exclusivamente à arte. SóMainThread esteve disponível como marcadorCPU de release. Memória alocadaUnity não éVRAM. ModalC05 e cadaVFX não foram isolados.</p><table><thead><tr><th>Ativos</th><th>Frame05 p95</th><th>GPU05 p95</th><th>Alocação05 p95</th></tr></thead><tbody>'''+perfrows+'''</tbody></table>'''+standalone+'''<p class="note">Não é uma otimização comprovada nem orçamento final de hardware. O relatório06.4 também compara oEditor ao baseline atual e conserva amostras brutas/contagensGPU válidas.</p></section>
<section id="future"><p class="eyebrow">06 / Preparação, sem conversão</p><h2>Universos diferentes, leitura de combate consistente</h2><div class="columns"><div class="card"><h3>Arquivo Estratificado</h3><p>Camadas de dados, volumes retangulares, rupturas localizadas e bordas pintadas. Laços curtos entre massas de cobertura.</p></div><div class="card"><h3>Metrópole de Letreiros Recortados</h3><p>Silhuetas arquitetônicas e luz de cartaz. Avenidas abertas, esquinas e travessas, com néon contido fora dos sinais de perigo.</p></div><div class="card"><h3>Palimpsesto Corrompido</h3><p>Pinceladas, estratos arredondados e versões de superfície. Clareiras/rotas curvas com fronteiras físicas legíveis.</p></div></div><p>Propostas textuais originais, sem imagens finais, lore/IDs/ordem de campanha ou implementação aprovados. Primeiro experimento futuro: uma fachada e uma cobertura,3D versus híbrido na mesma sala/câmera/luz/encontro. Imagens serão revisadas antes da aplicação e o resultado será revisto no jogo. A câmera real é perspectiva e acompanha o jogador; plano fixo não equivale automaticamente a volume convincente.</p><p class="note">STOP06. Nenhum renderer, WorldStyleManager, nova hierarquia de dados ou mapa2.5D foi implementado. O brief07 não é autorização para iniciá-lo.</p></section>
<section id="reports"><p class="eyebrow">07 / Handoff</p><h2>Documentos, reprodução e dados locais</h2><p>O relatório consolidado contém o procedimento de playtest seguro noNexus/FirstSector e os filtros doTestRunner. Os arquivos abaixo são cópias locais para leitura, sem dependência de servidor ou internet.</p><ul>'''+links+'''</ul></section><footer>Capturas reaisUnity/standalone e comparadores explicitamente identificados. Imagens não retocadas. Clique em qualquer imagem para abrir sua resolução original. Este pacote não contém os executáveis ou valores de saves.</footer></main></html>'''
(REVIEW/'index.html').write_text(page,encoding='utf-8')
for value in re.findall(r'(?:href|src)="([^"]+)"',page):
    if value.startswith('#'):continue
    if not (REVIEW/value).is_file():raise FileNotFoundError(value)
(OUT/'review-manifest.json').write_text(json.dumps({'images':manifest,'localLinksMissing':0},indent=2,ensure_ascii=False),encoding='utf-8')
archive=OUT/'RUN_EXP_06_REVIEW.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(REVIEW.rglob('*')):
        if p.is_file():z.write(p,p.relative_to(REVIEW))
with zipfile.ZipFile(archive) as z:
    if z.testzip():raise RuntimeError('ZIP CRC failure')
print('Review:',len(manifest),'images; missing local links0; ZIP CRC OK;',archive.stat().st_size,'bytes')
