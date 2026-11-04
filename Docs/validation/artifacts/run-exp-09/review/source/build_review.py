"""Build an offline review from actual Unity evidence; never retouch screenshots."""
from pathlib import Path
import json,csv,statistics,math,hashlib,shutil,html,runpy,sys
R=Path(__file__).resolve().parents[2];O=R/'Docs/validation/artifacts/run-exp-09';V=O/'review';D=R/'Docs/decisions'
V.mkdir(parents=True,exist_ok=True);esc=html.escape
families={'AsymmetricCourtyard':'Pátio assimétrico','LChamber':'Câmara em L','ElongatedGallery':'Galeria com bolsões','FragmentedRing':'Anel fragmentado'}
manifest=[]
def cp(p,target,category):
    dest=V/target;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    manifest.append({'file':target,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'original':str(p.resolve()),'category':category});return target
def folder(p,label,category):
    for f in p.iterdir():
        if label=='edges' and f.name.startswith('09-scene-view-'):continue # Rejected Camera.Render previews; valid viewport captures live separately.
        if f.is_file():cp(f,label+'/'+f.name,category)
def latest(folder,pattern='*',required=None):
    a=sorted(p for p in folder.glob(pattern) if p.is_dir() and (not required or (p/required).exists()))
    if not a:raise RuntimeError('Missing evidence '+str(folder)+' '+pattern)
    return a[-1]
def fig(src,caption):return '<figure><a href="'+esc(src)+'"><img loading="lazy" src="'+esc(src)+'" alt="'+esc(caption)+'"></a><figcaption>'+esc(caption)+'</figcaption></figure>'
selected={}
for f in families:
    for weapon in range(3):
        p=latest(O/'gameplay','*-'+f+'-'+str(weapon),'passed.txt');selected[f,weapon]=p;folder(p,'gameplay/'+f+'/'+str(weapon),'isolated comparison, native executors, controlled six-role roster')
perf=latest(O/'performance',required='frames.csv');folder(perf,'performance','Editor measured, distinct seeded decks')
triple=latest(O/'triple',required='fixture.txt');folder(triple,'triple','assisted native triple, controlled targets')
edges=latest(O/'edges',required='reinforcements.csv');folder(edges,'edges','native Spawner sampler/timer, Hook recovery and concave dash; controlled fixture')
folder(O/'scene-viewport','scene-viewport','actual Unity Scene View viewport with NavMesh overlay, captured by MCP')
occlusion=latest(O/'occlusion',required='passed.txt');folder(occlusion,'occlusion','normal camera and native mass occlusion; collider/NavMesh unchanged')
struct=latest(O/'structural',required='fifty-decks.csv');folder(struct,'structural','50 real generated decks, NavMesh paths and holes')
baseline=R/'Docs/validation/artifacts/run-exp-03/firstsector/20261103-232338-0725195';folder(baseline,'baseline','RUN09 before geometry, production legacy')
# Exactly the four production route runs of RUN09 (three fixed weapons + one random seed).
prod=R/'Docs/validation/artifacts/run-exp-06/gameplay'
routes=[]
for p in sorted(prod.glob('20261103-2345*')):
    if (p/'route06.txt').exists():folder(p,'production/'+p.name,'production FirstSector after09, native route/rewards; automated warp/kill completion');routes.append(p)
if len(routes)!=4:raise RuntimeError('Expected four passing production route logs')

def svg(data):
    # Every rectangle below is taken from the actual BoxCollider export. Overlay is actual baked triangulation.
    w,h=data['size']['x'],data['size']['y'];scale=16;pad=45
    def xy(v):return (pad+(v['x']+w/2)*scale,pad+(h/2-v['z'])*scale)
    out=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w*scale+2*pad}" height="{h*scale+2*pad}" viewBox="0 0 {w*scale+2*pad} {h*scale+2*pad}"><rect width="100%" height="100%" fill="#111b24"/><g>']
    for group,color in [('floors','#344653'),('walls','#ad8c64'),('covers','#775741')]:
        for b in data[group]:
            x,y=xy(b['center']);sx,sz=b['size']['x']*scale,b['size']['z']*scale
            out.append(f'<rect x="{x-sx/2}" y="{y-sz/2}" width="{sx}" height="{sz}" fill="{color}" stroke="#dde6eb" stroke-width=".6"/>')
    vert=data['navVertices'];ind=data['navIndices']
    out.append('<g fill="#59c9ad" fill-opacity=".14" stroke="#59c9ad" stroke-opacity=".22" stroke-width=".5">')
    for i in range(0,len(ind),3):out.append('<polygon points="'+' '.join(','.join(map(str,xy(vert[ind[i+j]]))) for j in range(3))+'"/>')
    out.append('</g>')
    for i,p in enumerate(data['spawns']):
        x,y=xy(p);out.append(f'<circle cx="{x}" cy="{y}" r="6" fill="#ff8673"/><text x="{x+8}" y="{y+4}" font-size="11" fill="white">{esc(data["roster"][i])}</text>')
    for p in data['sockets']:
        x,y=xy(p);out.append(f'<circle cx="{x}" cy="{y}" r="9" fill="#74caff" stroke="white"/>')
    x,y=xy({'x':0,'z':0});out.append(f'<path d="M{x-7},{y}h14M{x},{y-7}v14" stroke="white" stroke-width="3"/>')
    out.append(f'</g><text x="12" y="24" fill="white" font-size="15">{esc(families[data["family"]])} · {int(w)} × {int(h)} m · seed {data["seed"]}</text></svg>');return ''.join(out)
for f in families:
    data=json.loads((selected[f,0]/'actual-geometry-navmesh.json').read_text());p=V/'diagrams'/f'{f}.svg';p.parent.mkdir(exist_ok=True);p.write_text(svg(data),encoding='utf-8')

rows=list(csv.DictReader((perf/'frames.csv').open()));cost={}
for f in ['Rectangle',*families]:
    a=[x for x in rows if x['family']==f]
    med=lambda k:round(statistics.median(float(x[k]) for x in a),3)
    fr=sorted(float(x['frame_ms']) for x in a);cpu=[int(x['cpu_ns'])/1e6 for x in a if int(x['cpu_ns'])>0]
    cost[f]={'samples':len(a),'seed':a[0]['seed'],'frame_median_ms':med('frame_ms'),'frame_p95_ms':round(fr[math.ceil((len(fr)-1)*.95)],3),'cpu_median_ms':round(statistics.median(cpu),3) if cpu else None,'nav_bake_ms':med('nav_bake_ms'),'load_bake_ms':med('load_bake_ms'),'draw_calls':med('draw_calls'),'renderers':med('renderers'),'colliders':med('colliders'),'nav_triangles':med('nav_triangles'),'enemies':sorted(set(int(x['active_enemies']) for x in a))}
(V/'performance-summary.json').write_text(json.dumps(cost,indent=2),encoding='utf-8')
samples=list(csv.DictReader((struct/'fifty-decks.csv').open()));counts={f:sum(x['family']==f for x in samples) for f in families}
(V/'structural-summary.json').write_text(json.dumps({'seeds':50,'spatial_rooms':len(samples),'families':counts},indent=2),encoding='utf-8')
costmd='| Família | Inimigos | Bake deck ms | Load+bake ms | Frame med/p95 ms | CPU med ms | Draws | Renderers / colliders / triângulos deck |\n|---|---|---|---|---|---|---|---|\n'
for f,c in cost.items():costmd+=f'| {families.get(f,"Fallback retangular")} | {c["enemies"]} | {c["nav_bake_ms"]} | {c["load_bake_ms"]} | {c["frame_median_ms"]} / {c["frame_p95_ms"]} | {c["cpu_median_ms"]} | {c["draw_calls"]} | {c["renderers"]} / {c["colliders"]} / {c["nav_triangles"]} |\n'
(O/'evidence/performance-summary.md').write_text(costmd,encoding='utf-8')

common='''
## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.
'''
sections={
'1_ROOM_FAMILIES':('Famílias espaciais reais','''## Objetivo e estado auditado

DONE: substituir parte das arenas retangulares por quatro footprints distintos, preservando retângulo e arenas especiais. Antes09, variação aberta/coberta mantinha o mesmo envelope retangular; o protótipo08 era apresentação isolada.

| Família | Envelope / piso | Decisão de jogo e papéis |
|---|---|---|
| Pátio assimétrico | Envelope nativo arredondado a metros pares; recortes NW 4×5 e SE 5×3 sem piso | Rotação aberta entre coberturas e recortes. Melee aproxima pelo interior; ranged mantém distância no lado oposto. Mais próximo do baseline para transição. |
| L | 28×26 m, piso 602 m²; quadrante NE 14×9 ausente | Dois braços largos e canto que rompe visão. Ranged valoriza o braço superior; melee pressiona a curva. Jogador pode escolher abrir ângulo ou recolher-se. |
| Galeria | 28×30 m; eixo de 12 m e bolsões laterais desencontrados; piso 504 m² | Avanço longitudinal, cobertura escalonada e refúgios laterais. Ranged tem eixo longo; acesso pelos bolsões quebra a aproximação frontal. |
| Anel | 28×26 m; massa 7×10 m deslocada a leste e piso ausente sob ela; piso 658 m² | Dois contornos reconectam o jogador. Faixa leste 5 m, norte/sul 8 m e oeste 16 m antes do inset. Massa interrompe ataques físicos/LOS; rota oeste é mais ampla. |

São quatro famílias implementadas, não quatro skins. Portas em fronteira externa, centro válido e densidade de roster nativa limitam a autoria. As duas coberturas continuam condicionadas pela seleção existente. Massas e materiais são05; pintura08 não foi distribuída.

## Arquivos, decisões e trade-offs

FirstSectorRoomLayout.cs define células de 1 m, retângulos fundidos, fronteiras, portais e posições. A leitura métrica é intencional, sem DCC nem geometria orgânica. O anel pode dominar o quadro perto da massa: precisa de aceite humano; usa material05 e o fader existente. O termo “fragmentado” descreve circulação desviada e coberturas, sem saltos/ilhas desconectadas. Não há salto novo.
'''),
'2_PLAYABLE_GEOMETRY':('Geometria jogável e navegação','''## Objetivo e implementação

DONE: pisos, paredes e colisão seguem o footprint. ManualStageEnvironment.Spatial.cs materializa somente os retângulos caminháveis fundidos. Ausências não recebem piso invisível. Contornos recebem paredes físicas com cortes de 4 m; a massa do anel é collider NotWalkable de 3,5 m de altura, sem piso sob ela. Molduras de sockets são somente visuais e não têm collider. Nenhum collider por célula.

ManualNodeDefinition preserva a referência ao plano imutável em Copy. ManualStageEnvironment guarda o plano por nó e ContainsPlayer acrescenta inset físico de 0,55 m ao teste do trigger. Isso evita ativar uma sala só por estar dentro do envelope de um vazio. O trigger retangular permanece uma aproximação larga, refinada pelo plano. NavMeshSurface continua usando PhysicsColliders e um bake por run.

## Validação e correções

DONE: 50 seeds em EditMode e outros 50 decks materializados em PlayMode. BFS do piso, centro/spawns/portas, soma de áreas dos pisos fundidos, determinismo e RNG preservados. No runtime: amostras de piso com caminho completo, pontos proibidos sem NavMesh e conexões do grafo transitáveis com gates abertos para o ensaio estrutural. Abertura de gate neste teste não é evidência de lifecycle; a progressão é coberta pelos testes de gameplay/produção.

Agentes auditados têm raio0,5 m/altura2 m; corredores de4 m e menor faixa do anel5 m antecedem inset de bake. Dash nativo nominal5 m não exige corredor reto5 m: o motor deve parar no contorno. Os testes de reentrância verificam o motor real; não alteramos raio, velocidade, dano ou câmera para encaixar a sala.

Arquivos: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, ManualStageEnvironment.Procedural.cs, ManualStageEnvironment.cs, ManualRunDefinition.cs e testes Spatial09*. Uma montagem inicial de ombreiras físicas foi removida antes dos gates: a decoração final não decide circulação. Falha de compilação ao montar a fixture negativa foi corrigida antes de qualquer resultado contar como aprovação.
'''),
'3_DOORS_AND_FLOW':('Portas, conectores e fluxo','''## Objetivo e contrato preservado

DONE: adjacências, IDs e destinos continuam os do snapshot de run. ProceduralRunGeometryPlan resolve o socket no contorno externo real, mantendo normal cardinal e projetando a posição lateral solicitada. O socket não pode abrir para a massa central. A projeção mantém separados destinos de branches/merges; usar sempre o meio da borda poderia juntar portas.

Conectores mantêm trechos ortogonais, largura4 m e prolongamento4 m antes de cotovelos. Barreiras físicas, carving e anchors continuam os do materializador existente. Estado vem de ProgressionDirector/ManualSession: fechar no encontro, resolver ordinais de recompensa, commit válido e só então abrir. Ombreiras indicam abertura, sem um segundo dono de progressão.

## Testes e trade-offs

DONE: 50 decks verificaram segmentos ortogonais e caminhos reais por aresta. Doze comparações verificaram gate fechado, ticketsC05, gate aberto, deslocamento real do NavMeshAgent de1 m dentro a3 m fora do socket e entrada no próximo nó. O restante da distância usa Warp na fixture, explicitamente. Quatro rotas de produção atravessaram estados até chefe/extrair/Nexus com o owner atual; também usam Warp e kills controladas.

A grade cardinal restringe curvas e arquitetura, em troca de uma integração limitada. Não se desenhou uma porta de destino fictício. Arquivos alterados: ProceduralRunGeometryPlan.cs, materializador parcial, ManualStageEnvironment e cena FirstSector. O grafo/UI de escolha não foi redesenhado. PENDING DEVELOPER APPROVAL: perceber e escolher a saída com a câmera normal, sobretudo junto à massa do anel.
'''),
'4_ENCOUNTERS':('Encontros e ocupação espacial','''## Objetivo e estado atual

DONE: os mesmos prefabs/ranks e ordem do roster são redistribuídos em posições seguras. Arrange reserva centro com raio4 m, sockets com raio5 m, clearance1,25 m e separação2,4 m. Backline usa a classificação existente; scores por família favorecem braço superior do L, eixo/bolsões da galeria e lado oposto do anel. Não altera pesos, ondas, budget, HP, dano, rewards ou inimigos.

Doze fixtures usam Fragmento, Emissor, Grunt, Heavy, Hooker e Healer reais, seis por sala. IA/nav/poses ficam ativos na observação, circulação e LOS. O alvo do teste de básico tem HP/posição/IA temporariamente controlados; habilidades usam mana1500 e Asura para exercitar executores nativos. Isso verifica compatibilidade e legibilidade capturada, não dificuldade natural.

## Testes e correções

Ataque básico causa HP nas três armas; Q/E/R e especial são usados e encerram casting; dash recupera NavMesh. O Emissor não resolve impacto atrás da cobertura física e readquire após mudança de ângulo. A asserção identifica o collider obstrutor. Fragmento/Emissor registram poses/pés. O Spawner e Hooker são exercitados separadamente junto aos contornos, com fixtures declaradas.

A tripla TitanicFists+Conductor+Detonation foi aplicada em memória e produziu recibos nativos de dano e telegraph HeavySlam junto à massa do anel. A primeira posição copiada do08 estava atrás de uma cobertura e falhou corretamente; mudou-se a fixture para uma faixa livre, sem modificar protocolos. O caso negativo usa a massa real como bloqueio. NOT DONE: balanceamento novo, nova IA, novos arquétipos e campanha de dificuldade. Arquivos de executor e catálogos permanecem intactos.
'''),
'5_FIRSTSECTOR_INTEGRATION':('Integração FirstSector','''## Objetivo e implementação produtiva

DONE: _useSpatialLayouts está habilitado por override em Assets/_Project/Scenes/FirstSector.unity. ManualRunRandomizer.Generate recebe opção vinda do ambiente atual. A seleção usa SplitMix64(seed XOR nodeId XOR salt espacial) módulo5: quatro novas famílias e retângulo fallback, apenas em Combat com conteúdo compatível. Boss/Elite/especiais conservam caminho anterior. Os streams existentes de recompensas/inimigos/layout executam na ordem anterior; a nova escolha ocorre depois e não consome esses RNGs.

ManualNodeDefinition guarda o plano; ProgressionDirector passa a opção, sem novo diretor/framework. Review09 é uma cópia para comparar. Review08 e seu flag padrãofalse ficam no caminho antigo. Não há runtime painting08 nas salas produtivas.

## Reprodução e validação

Abra NexusBlockout e Play para seed normal. O portal normal carrega FirstSector com seleção espacial antes do chefe. Para comparar uma forma no primeiro Combat, fora de Play use Tools → Tech Guy → RUN-EXP-09 → Next run → Courtyard / L Chamber / Gallery / Ring e entre no portal. O override de seed é SessionState de uso único, não save. Tools → Tech Guy → RUN-EXP-09 → Open spatial comparison abre a cena isolada09; a08 continua disponível para comparação histórica.

DONE: três rotas fixas, uma por arma, e uma aleatória via Nexus/portal, roster/rewards nativos, C05 com cancel/reabrir/confirmar uma vez, chefe/extraction e retornoNexus. Mais um teste de foco/modal/Enter remapeado sem vazar comando para combate. Os logs06 com timestamp2345 são execução desta09, copiados em production do pacote; o nome histórico do helper não muda a origem das capturas. Imunidade, acesso temporário a loadout, Warps e kills são somente da automação.

Arquivos: ManualRunRandomizer.cs, ManualRunDefinition.cs, ProgressionDirector.Manual.cs, ambiente/planos e override de cena. RunExp09Authoring.cs fornece comparação/reprodução, sem alteração de prefs persistentes. Chance por família é regra do protótipo, sujeita a aceite; não há pesos novos de loot.
'''),
'6_VALIDATION':('Validação técnica','''## Resultado e escopo

DONE: auditoria anterior; 1/1 EditMode de50 seeds; 1/1 PlayMode de50 decks; 12/12 famílias×armas; 5/5 produção (três armas, rota aleatória e foco/C05); custo Editor e tripla com recibos. DONE: 1/1 borda/Spawner/HookLine nas quatro famílias, 1/1 tripla com caso negativo da massa e 1/1 inspeção Scene View. As regressões estão nos jobs finais anexados, sem transformar uma execução que não iniciou em “passou”. O pacote contém resultados iniciais e finais para auditoria.

Falhas: foco da fixture no arco exigiu restabelecer foco e release no próprio teste; não se afrouxou gameplay. Tripla inicial bloqueada pela cobertura exigiu posição livre. Um erro local de ordem de variável impediu compilar a fixture negativa; corrigido, aguardou-se compilação limpa. Jobs sem inicialização foram descartados, sem contagem de testes aprovados. A amostra de50 decks não cobre todas as seeds possíveis.

## Performance observada

As condições e amostras estão em performance/frames.csv e conditions.txt. Cinco decks com seeds distintas, 3s por família após0,6s; inimigos e geometria do deck variam. Timer de bake mede somente NavMeshSurface.BuildNavMesh. Load+bake inclui carregamento e frames iniciais. Frame/CPU/draws medidos no Editor; coleta tem overhead e não é A/B de densidade igual. Alocação total é memória residente, não GC/frame. Sem alegação de otimização ou FPS standalone.

'''+costmd+'''
O agrupamento de pisos evita um collider por metro. Nenhuma grande regressão pode ser excluída em GPU/campanha por estes dados curtos. NOT TESTED: GPU, GC/frame e custo exclusivo de agentes/fader. Browser do HTML: BLOCKED pelo file:// no ambiente; QA estática de referências locais, SHA256, PNG/CRC, sintaxeJS e ZIP. Sem contorno dessa política por servidor local.
''')}
for key,(title,body) in sections.items():(D/f'RUN_EXP_09_{key}.md').write_text(f'# RUN-EXP-09.{key[0]} — {title}\n\n'+body+common,encoding='utf-8')
summary='''# RUN-EXP-09 — FirstSector com variação estrutural

DONE: quatro famílias físicas foram implementadas e integradas na seleção normal de Combat do FirstSector: pátio assimétrico, L, galeria com bolsões e anel. Cada uma altera piso, paredes, navegação, sockets e ocupação, mantendo retângulo como fallback e preservando chefe/Elite/especiais. _useSpatialLayouts é um override produtivo; o tratamento painterly08 continua isolado.

## O que está comprovado

50 seeds de dados e50 decks com NavMesh real; 12 combinações de armas×famílias; três rotas fixas e uma aleatória Nexus→salas→chefe→Nexus; C05/foco/remapeamento; ataque básico com HP, quatro habilidades, dash, bloqueio/readquisição do Emissor e travessia física do trecho de porta. Há testes de Spawner/Hook e tripla com fixtures declaradas e jobs finais anexados. Ver RUN_EXP_09_6_VALIDATION.md e evidence/*.json para resultados exatos e tentativas iniciais.

Plantas SVG derivam de BoxColliders, anchors e triangulação real exportados em PlayMode. Game View normal e vista ortográfica técnica são categorias separadas. Scene View aparece como captura técnica do Editor. A forma só é considerada publicada porque a cena produtiva foi habilitada e as rotas normais foram executadas; screenshots da comparação permanecem identificados como comparação.

## Como revisar

Use o HTML portátil em Docs/validation/artifacts/run-exp-09/review/index.html ou extraia RUN_EXP_09_REVIEW.zip e abra review/index.html. No Unity, rode NexusBlockout para distribuição normal ou escolha Tools → Tech Guy → RUN-EXP-09 → Next run para uma forma determinística no primeiro Combat. Open spatial comparison abre Review09; Review08 preserva a apresentação painterly histórica.

O avanço aqui é estrutural e mensurável. “Mais bonita”, ritmo, dificuldade, conforto da câmera e identidade artística continuam PENDING DEVELOPER APPROVAL em playtest físico. NOT DONE: novos mundos/inimigos/boons, rework de economia/UI, pintura global, chefe novo e etapa posterior. STOP após09; sem commit/push.

## Relatórios

Auditoria0; famílias1; geometria2; portas3; encontros4; integração5; validação6. Cada relatório registra fontes, mudanças, decisões, trade-offs, evidência e limites. Performance tem medidas curtas de Editor e não prova de GPU/standalone. Integridade é fechada por hashes/prefs comparados ao snapshot anterior.
'''
(D/'RUN_EXP_09_RELATORIO.md').write_text(summary+common,encoding='utf-8')
if '--finalize' in sys.argv:runpy.run_path(str(R/'Tools/RunExp09/finalize_docs.py'),run_name='__main__')

helper=runpy.run_path(str(R/'Tools/RunExp07/build_review.py'),run_name='helpers_only');md=helper['md']
pages=[]
for p in sorted(D.glob('RUN_EXP_09_*.md')):
    cp(p,'reports/'+p.name,'Portuguese report');dest='reports/'+p.stem+'.html';(V/dest).write_text('<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="../style.css"><main><a href="../index.html">← Revisão09</a>'+md(p.read_text(encoding='utf-8'))+'</main>',encoding='utf-8');pages.append('<a href="'+dest+'">'+esc(p.stem)+'</a>')
for glob in ('Assets/_Project/Scripts/Core/Run/*Spatial*.cs','Assets/_Project/Scripts/Core/Run/FirstSectorRoomLayout.cs','Assets/_Project/Scripts/Editor/RunExp09Authoring.cs','Assets/_Project/Scripts/Tests/**/*Spatial09*.cs','Assets/_Project/Scripts/Tests/**/RunExp09SpatialTests.cs'):
    for p in R.glob(glob):cp(p,'source/'+p.relative_to(R).as_posix(),'actual09 repository source snapshot; inspect in full Unity project to run')
(V/'style.css').write_text('''*{box-sizing:border-box}body{background:#111b24;color:#e2e7e9;font:16px/1.6 system-ui;margin:0}main{max-width:1200px;margin:auto;padding:30px}a{color:#8ed5ff}h1,h2{line-height:1.2;color:#f5dfb4}section{margin:38px 0;padding-top:18px;border-top:1px solid #506272}figure{margin:16px 0}img{width:100%;height:auto;border:1px solid #546676;border-radius:5px}figcaption{font-size:14px;color:#b7c9d3}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:18px}table{border-collapse:collapse;display:block;overflow-x:auto}th,td{padding:8px;border:1px solid #526474;text-align:left}nav{display:flex;gap:14px;flex-wrap:wrap}.note{border-left:4px solid #e5b962;padding:14px;background:#22313e}details{padding:12px;background:#1c2a36;margin:10px 0}summary{cursor:pointer}pre{overflow:auto}''',encoding='utf-8')
body='''<h1>RUN-EXP-09 · Espaço que muda a luta</h1><p>Quatro famílias físicas no FirstSector. Piso, sólidos, portas, spawn e NavMesh reais; materiais05 preservados.</p><p class="note">Implementado e testado tecnicamente. Aceite de beleza, fluxo e gameplay físico: PENDING DEVELOPER APPROVAL. Automação usa fixtures explicitadas; não é uma run humana.</p><nav>'''+''.join('<a href="#'+f+'">'+name+'</a>' for f,name in families.items())+'''<a href="#production">Produção</a><a href="#tests">Testes/custo</a><a href="#docs">Relatórios</a></nav>'''
body+='<section><h2>Antes09: o mesmo retângulo</h2><div class="grid">'+fig('baseline/05-room-open-after-normal-camera.png','Baseline capturado nesta09 antes da geometria — sala aberta05')+fig('baseline/05-room-cover-after-normal-camera.png','Baseline antes09 — mesma forma retangular, duas coberturas05')+'</div><p>A comparação abaixo muda o contorno físico. Não é uma troca de paleta nem implantação global da pintura08.</p></section>'
for f,name in families.items():
    data=json.loads((selected[f,0]/'actual-geometry-navmesh.json').read_text());label='gameplay/'+f+'/0/'
    body+='<section id="'+f+'"><h2>'+name+'</h2><p>Envelope '+str(data['size']['x'])+' × '+str(data['size']['y'])+' m. Seed '+data['seed']+'. Comparação controlada com seis papéis nativos, câmera normal FOV40.</p><div class="grid">'+fig(label+'09-gameplay-overview.png','Game View normal — IA ativa, jogador imune na fixture')+fig('diagrams/'+f+'.svg','Planta de colliders reais; verde = triangulação NavMesh, azul = sockets, salmão = spawns; cruz = centro')+'</div><details><summary>Vista técnica, circulação, portas e três armas</summary><div class="grid">'+fig(label+'09-technical-top-down.png','Câmera ortográfica temporária de inspeção — não é a câmera de gameplay')+fig('scene-viewport/09-scene-view-'+f+'.png','Scene View do Editor — inspeção técnica')+fig(label+'09-actual-player-circulation.png','Deslocamento real do jogador')+fig(label+'09-door-open-physical-traversal.png','Gate aberto após C05; travessia física do trecho do socket')+fig('edges/09-concave-dash-'+f+'.png','Dash nativo recuperado junto à reentrância')+'</div>'
    for w,weapon in enumerate(['Manopla','Arco','Lança']):
        base='gameplay/'+f+'/'+str(w)+'/'
        body+='<h3>'+weapon+'</h3><div class="grid">'+fig(base+'09-native-skill-3.png','Especial nativo — recurso de teste declarado')+fig(base+'09-emissor-cover-blocked.png','Emissor bloqueado pela cobertura física')+'</div><p><a href="'+base+'passed.txt">Condições da fixture</a> · <a href="'+base+'actual-geometry-navmesh.json">Geometria e NavMesh exportados</a></p>'
    body+='</details></section>'
body+='<section id="production"><h2>FirstSector de produção</h2><p>Entradas pelo Nexus/portal e retornos após chefe. Roster, rewards e tickets nativos; Warps, imunidade e kills controladas explicitados no log. Sem pintura08 global.</p><div class="grid">'
for p in routes:
    files=sorted(p.glob('06-normal-node-*.png'));base='production/'+p.name+'/'
    body+=fig(base+files[0].name,'Game View da cena produtiva — '+p.name)+fig(base+next(p.glob('06-returned-Nexus*.png')).name,'Retorno real ao Nexus')+'<p><a href="'+base+'route06.txt">Log de rota e assistência</a></p>'
body+='</div><h3>Tripla junto à massa</h3><div class="grid">'+fig('triple/09-triple-warning.png','Telegraph HeavySlam com vento/eletricidade/fogo nativos')+fig('triple/09-triple-confirmed.png','Dano confirmado por recibos nativos')+fig('triple/09-triple-real-mass-blocks-wind.png','Caso negativo: massa física bloqueia vento')+fig('occlusion/09-mass-occlusion-normal-camera.png','Oclusão da massa: câmera normal FOV40, collider ativo e vazio sem NavMesh')+'</div><a href="triple/native-damage.json">Recibos de dano</a></section>'
body+='<section id="tests"><h2>Validação e custos</h2><p>50 seeds, '+str(len(samples))+' salas espaciais materializadas. '+esc(str(counts))+'</p><p>12/12 famílias × armas; 5/5 testes produtivos. Resultados adicionais e falhas iniciais em evidence. Browser: NOT TESTED por bloqueio file://; QA estática acompanha o pacote.</p>'+md(costmd)+'<p class="note">Editor, amostras curtas e decks/seeds/rosters diferentes. Não é A/B de densidade igual. GPU, GC/frame, custo exclusivo de oclusão e navegação de agentes, standalone e campanha longa NOT TESTED.</p><p><a href="structural/fifty-decks.csv">50 decks</a> · <a href="performance/frames.csv">Frames</a> · <a href="performance/conditions.txt">Condições</a> · <a href="edges/reinforcements.csv">Reforços/bordas</a> · <a href="evidence/integrity-comparison.json">Integridade</a> · <a href="package-qa.json">QA do pacote</a></p></section>'
body+='<section id="docs"><h2>Relatórios completos em português</h2><nav>'+''.join(pages)+'</nav><p>STOP após09. Próxima decisão: jogar as quatro formas com as três armas, avaliar curvas/portas, visibilidade junto à massa, pressão ranged e fadiga. Não há aceite artístico ou de dificuldade inferido de um teste automatizado.</p></section>'
(V/'index.html').write_text('<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Tech Guy · RUN-EXP-09</title><link rel="stylesheet" href="style.css"></head><body><main>'+body+'</main></body></html>',encoding='utf-8')
(V/'asset-manifest.json').write_text(json.dumps({'assets':manifest},indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'families':counts,'spatial_rooms':len(samples),'captures':sum(x['file'].endswith('.png') for x in manifest),'performance':cost},ensure_ascii=False))
