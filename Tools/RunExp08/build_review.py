"""Offline evidence package. Original PNGs are copied, never painted over or relabelled as concept captures."""
from pathlib import Path
import json,csv,statistics,math,hashlib,struct,shutil,runpy,html,re
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/validation/artifacts/run-exp-08';REVIEW=OUT/'review';DEC=ROOT/'Docs/decisions'
helpers=runpy.run_path(str(ROOT/'Tools/RunExp07/build_review.py'),run_name='helpers_only')
CSS=helpers['CSS'];md=helpers['md'];figure=helpers['figure'];esc=html.escape
REVIEW.mkdir(parents=True,exist_ok=True)
images=REVIEW/'images';images.mkdir(exist_ok=True)

def copy(src,dst):
    dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
    return dst.relative_to(REVIEW).as_posix()

gameplay={}
for weapon in range(3):
    candidates=sorted(p for p in (OUT/'gameplay').glob('*-weapon-'+str(weapon)) if (p/'validation.txt').exists())
    if not candidates:raise RuntimeError('No passing gameplay evidence for '+str(weapon))
    source=candidates[-1];gameplay[weapon]=source
    for p in source.glob('*'):
        if p.is_file():copy(p,REVIEW/'gameplay'/('weapon-'+str(weapon))/p.name)
performance=sorted((OUT/'performance').glob('*'))[-1]
triples=sorted((OUT/'triple').glob('*'))[-1]
for folder,label in ((performance,'performance'),(triples,'triple')):
    for p in folder.iterdir():
        if p.is_file():copy(p,REVIEW/label/p.name)
copy(ROOT/'Docs/validation/artifacts/run-exp-07/review/images/C-overview.png',images/'approved-C-concept.png')
copy(ROOT/'Docs/validation/artifacts/run-exp-07/review/images/C-sheet.png',images/'approved-C-sheet.png')
for p in (ROOT/'Assets/_Project/Art/RunExp08').glob('*.png'):copy(p,REVIEW/'materials'/p.name)
for p in (OUT/'prompts').glob('*.txt'):copy(p,REVIEW/'prompts'/p.name)
# Keep the first rejected framing for an honest visual correction comparison.
copy(OUT/'gameplay/20261103-224920-8678117-weapon-0/08-open-door-transition.png',images/'iteration-1-occluded-door.png')

rows=list(csv.DictReader((performance/'frames.csv').open()))
def percentile(a,q):
    a=sorted(a);return a[min(len(a)-1,math.ceil((len(a)-1)*q))]
cost={}
for state in ('0','1'):
    r=[x for x in rows if x['painterly']==state];frame=[float(x['unscaled_ms']) for x in r];cpu=[int(x['main_thread_ns'])/1e6 for x in r if int(x['main_thread_ns'])>=0]
    cost[state]={'samples':len(r),'frame_median_ms':statistics.median(frame),'frame_p95_ms':percentile(frame,.95),'cpu_median_ms':statistics.median(cpu) if cpu else None,'cpu_p95_ms':percentile(cpu,.95) if cpu else None,'drawcalls_median':statistics.median(int(x['draw_calls']) for x in r),'allocated_mib_median':statistics.median(int(x['allocated_bytes'])/1048576 for x in r),'enemy_counts':sorted(set(int(x['active_enemies']) for x in r))}
(REVIEW/'performance-summary.json').write_text(json.dumps(cost,indent=2),encoding='utf-8')
cost_table='| Versão | Amostras | Quadro mediana/p95 ms | Main Thread mediana/p95 ms | Draw calls mediana | Memória alocada MiB |\n|---|---|---|---|---|---|\n'
for state,label in [('0','Baseline3D'),('1','Protótipo pictórico')]:
    c=cost[state];cost_table+=f'| {label} | {c["samples"]} | {c["frame_median_ms"]:.3f}/{c["frame_p95_ms"]:.3f} | {c["cpu_median_ms"]:.3f}/{c["cpu_p95_ms"]:.3f} | {c["drawcalls_median"]:.0f} | {c["allocated_mib_median"]:.2f} |\n'

skills='''| Skill realmente lida/aplicada | Decisão influenciada |
|---|---|
| tech-guy-game-vision + referências | DireçãoC selecionada pelo pedido08 prevalece sobre recomendaçãoA; identidade, C05 e armas preservadas |
| tech-guy-combat-unity + combat-matrix | Física/LOS/dano e boons permanecem nos owners existentes; nenhuma mecânicaBurn criada |
| tech-guy-unity-acceptance + editor-validation | Evidência MCP/UTF atual, scopes de prefs, captura real e separação entre teste e aceite humano |
| router / unity-mcp-orchestrator | ProjetoD: e instância6000.5 confirmados, resources-first e compilação antes de executar |
| unity-csharp-scripting | Componente local com referências serializadas, lifecycle/cleanup e cena isolada reaberta |
| create-game-assets + art-direction/raster/3D/provenance / imagegen | Dois albedos originais, prompts preservados, UV por face, piso quieto e diferenciação de conceito/runtime |
| tech-guy-world-identity + world-card | Pinceladas minerais e silhueta periférica próprias; nenhum novo universo ou lore |
| level-design / tech-guy-level-layout + level-brief | Mesmas métricas de combate, corredor4m existente, pórtico aberto no centro, sem tempo extra de caminhada |
| camera-systems | Corrigir oclusão do lintel; renderers sob colliders físicos, câmera/follow intactos |
| shader-programming | Reutilizar shader Illustrated Toon com opacity dither e outline0; sem shader ou feature global novos |
| performance-optimization | ABBA com AI ativa e métricas brutas, Editor separado de standalone, sem inferir ganho deFPS |

Descobertas: quatro skills locais em .agents; boon-design não aplicada pois catálogo está fora do escopo. Instaladas acima já lidas nesta conversa e referências pertinentes consultadas; não se instalaram pacotes/skills. Não existe skill local dedicada a Blender/URP2.5D. BlenderMCP consultado08 sem conexão: **BLOCKED** paraDCC, não usado; malhas de revisão foram autoradas emC#/Unity. ImageGen integrado executado duas vezes; não forneceu modelo/seed nem camadas editáveis. PNG é a fonte raster; prompts/C#/materiais são fontes editáveis reais, sem PSD/blend fictício.
'''

shared='''## Estado auditado, arquivos e sistemas

Projeto realD:/Dev/Personal/My_games/Tech-Guy, branchgameplayOverhaul, HEAD766afa66d93a58afc1b9de1f2eafa15ed8a5504b, Unity6000.5.10f1,URP17.5, espaçoLinear/Ultra. Cena inicialNexusBlockout limpa/Edit. Alterações/documentos07 anteriores foram preservados. Nenhum relatório07B foi encontrado emDocs/.agents/Tools; a referência pictórica verificável éC07, aprovada descritivamente pelo pedido08. Não se inventou uma referência07B.

AGENTS/README, skills locais/referências, visão, arte/pipeline, HUD, inimigos, progressão, masterplan, ledger/matriz,05/06/07 e plano2.5D foram consultados nesta conversa; nesta etapa foram relidos os contratos locais, seções atuais e o sistema real de conexão. Fonte: ManualStageEnvironment.BuildGenerated, ProceduralRunGeometryPlan, ApplyGateState/EncounterGates, FirstSectorEnvironmentArt/RoomPresentation, FirstSectorContentDefinition, TG_TopDown_Camera, CameraOcclusionFader, testes produtivos05/06 eGpr015. Não háAGENTS aninhado emAssets/_Project descoberto.

## Implementação e arquivos criados

Assets/_Project/Scripts/Prototypes/PainterlyRoomPrototype.cs é uma apresentação referenciada somente pela cena nova; espera o ambiente nativo pronto, decora nó1 e seus acessos, oferece toggleA/B e limpa meshes/objetos próprios. RunExp08Authoring.cs cria/reabre somente a cópiaAssets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity e materiais novos. Seed63 fica no RunSeedSource da cópia, sem mexer na cenaFirstSector, BuildSettings, pacote ou PlayerPrefs. Fonte de gameplay não foi modificada.

Arte nova emAssets/_Project/Art/RunExp08: PigmentStrata.png, QuietSlate.png e cinco materiaisMineralPigment/QuietSlate/ForwardIvory/ClosedInk/CoverTop, com metas criadas pelaUnity. Métodos de teste novos nas partialsRunExp03FirstSectorTests.Painterly08.cs ePainterly08Evidence.cs. Tools/RunExp08 guarda snapshot, pacote e QA. Documentos mestres/arte recebem bloco08 acima do histórico07. Fontes07, modelos05 e pacote07 não foram sobrescritos. Semcommit/push.

## Skills aplicadas e ferramentas

SKILLS

## Testes executados e resultados

Seleção finalPlayMode jobe5a04d55c2a54260b5bb9c165eca7d52: **33/33Passed**,0falhas/skipped,98,3223341s. Inclui26casos da classeCameraOcclusionFadeTests, cinco casos08 (3armas, custo e tripla), básico/cover nativo das três armas e rota normal aleatória06. Não é suíte global. Custo repetido isoladamente após o checker sair, jobe0560bcf55ec4c21b032fc65e793852b:1/1Passed,12,0665082s, mesmo caso já contado nos33.

História preservada: primeiro discovery441bbb... encontrou0testes (não contar comoPASS de cenário), corrigido por import/compilação do arquivo. PrimeiroGauntlet3f605... passou1/1. Seleção9d9bd... passou30/31, comBow parado emGameplayInputReady; fixture passou a isolar/restaurar dispositivos comPublishedInput06 existente. Não se alterou controle para fazer teste passar. Asserções deReady, casts e passagem continuam. Jobs JSON brutos emevidence.

Fixture três armas: roster/AI/nav/dano nativos ativos, jogador imune; mana/Asura de teste, warps para pontos de cenário e morte letal automatizada ao concluir declarados. C05 usa ticket real eChoose legal. A transição5m é percorrida pelo agente real após o gate nativo abrir; warp posterior para próximo nó é explícito. A rota normal06 até boss/Nexus também é automatizada, não sobrevivência humana. Tripla usa grants emmemória eAI desligada para composição controlada, HP de um alvo reduzido para detonation, executor e recibosHP nativos; **não** comprova obtenção natural. Nenhum Burn/DoT novo.

## Evidência antes/depois e regressões

CapturasUnityGameView reais emreview/gameplay/weapon-0,1,2:08-baseline-room/08-painterly-room na mesma posição e frame congelado; door-transition; C05; next-room; native-skill0..3; painted-cover-occlusion; telegraphs eLOS. Metadata de câmera do frame emcamera-and-room.json, desta vez amostrada efetivamente. Antes/depois não usa imagem gerada como screenshot. ConceitoC separado emimages/approved-C-concept.png; material source emmaterials; fontes/prompts e proveniência rastreadas.

Regressões encontradas e corrigidas: lintel contínuo escondia o jogador na transição; removido vão superior central e piers associados aCollider de parede parafade. UVs do atlas05 esticavam a nova textura; cobertura visual recebeu domínio porface. Pintura no topo gerava ruído/achatamento; topo escuro próprio. Bloqueio de input na fixtureBow resolvido com isolamento corrente de dispositivos, sem tuning. Itens anteriores foram preservados para comparação. Sem nova regressão bloqueante nos33 casos finais.

## Custo observado e limitações

COSTTABLE

Última execução de custo isolada: ABBA,2s porbloco após0,5s de acomodação; mesmo mapa/câmera/roster/qualidade, AI/nav/ataques ativos e jogador imune. Posições/fases evoluem, Editor e coletor acrescentam custo; os números são observação local, não prova de otimização emquadros idênticos. GetComponentsInChildren/UTF e coletaCSV têm overhead. Condições/hardware emperformance/conditions.txt, brutos emframes.csv. GPU/VRAM e standalone08 **NOT TESTED**; não derivarGPU deFPS. Não existe metaFPS universal aprovada.

InspectorUnity: PigmentStrata2048×1024,BC7,12mips; QuietSlate1024²,BC7,11mips; Read/Writefalse. GetRuntimeMemorySizeLong observou5.593.408 e2.797.184 bytes, respectivamente (custo Unity reportado, nãoVRAM). ShaderIllustratedToon atual é opaco/dither, semalpha margins ou transparências emcamadas; materiais de cobertura com dois slots aumentam custo potencial. Sem normais novas, luzes adicionais, névoa ou bloom global.

## Integridade, pendências e aprovação

**DONE:**2368/2368 arquivos preexistentes dos escoposAssets/_Project/ProjectSettings/Packages/SourceArt idênticos;25arquivos novos Unity,0removidos;47/47prefsTechGuy idênticas,0adições/remoções. JSON antes/depois emevidence. Cena nova salva/reaberta:MCP encontrou0scripts ausentes e6referências novas válidas. Prefs restauradas pelos scopes correntes dos testes, sem snapshot antigo. Nenhuma substituição deC05, input, boons, combate, spawns, física ou gerador.

Checker genérico da skillC# **BLOCKED**: ValueError emTripo externo noC: com projetoD:, antes de concluir a análise. Não é um resultado verde; alternativas: compilação/Console/InspectorUnity, igualdade de hashes antigos e GUID/metas novos. Blender desconectado; fonteDCC **NOT DONE**. HTMLvalidado estaticamente; inspeção emnavegador **NOT TESTED**, pois a ferramenta desta conversa bloqueou file: e não houve contorno. PNGs deoverview/porta/tripla inspecionados diretamente.

**PENDING DEVELOPER APPROVAL:** beleza, singularidade, leitura física e sensação de fluxo na sala implementada. O retângulo navegável28×24m foi preservado deliberadamente; a singularidade implementada é de silhueta/material/enquadramento, não uma topologia física nova. Não se promete que todos os universos usarão a mesma câmera. Próximo passo: revisar essa mesma sala na cópia jogável e indicar no máximo duas correções concretas; sem conteúdo novo, conversão global ou boonrework. **STOP após08.**
'''.replace('SKILLS',skills).replace('COSTTABLE',cost_table)
shared=shared.replace('Seleção finalPlayMode jobe5a04d55c2a54260b5bb9c165eca7d52:', 'Seleção anterior ao ajuste final de sombras, PlayMode jobe5a04d55c2a54260b5bb9c165eca7d52:')
shared=shared.replace('Última execução de custo isolada:', 'Última execução de custo da revisão final:')
shared=shared.replace('Sem nova regressão bloqueante nos33 casos finais.', 'Após retirar sombras decorativas duplicadas e desativar o passe Ink nos materiais08 com outline zero, a revisão final passou **31/31**, job ae31e105586849429415febfb5411713, 75,8859632s. Inclui os cinco casos08 e26 de câmera. Os dois casos adicionais de produção da seleção33 anterior continuam sobre os mesmos arquivos protegidos; não somar reexecuções como testes novos. Sem nova regressão bloqueante nessas seleções.')
shared=shared.replace('ShaderIllustratedToon atual é opaco/dither', 'ShaderIllustratedToon atual é opaco/dither; passe Ink desativado nos materiais08 e sombras desativadas somente nas peças decorativas duplicadas. As massas físicas visuais principais continuam projetando sombra. A mediana de drawcalls do protótipo caiu de587 para418 entre iterações; baseline366. Isso reduz custo desnecessário sem alegar vitória deFPS. O shader')
shared+='\nContagens ativas de Actor no custo: baseline '+str(cost['0']['enemy_counts'])+', protótipo '+str(cost['1']['enemy_counts'])+'. A variação inclui estado vivo/produzido pelo roster nativo; por isso a tabela não isola o efeito da arte sobre CPU ou FPS. Comparações de drawcalls e tempos devem ser lidas com essa ressalva.\n'

stages=[
('RUN_EXP_08_0_BASELINE.md','08.0 — auditoria e baseline','Estabelecer a sala/base e comparação segura.','Sala escolhida:nó1 deFirstSector seed63, arena28×24m, centro(500;0;43), duas coberturas4,2×2,2×4,2m noscentros locais(-3;1,1;-1,5)/(3;1,1;1,5). Conexões e corredores já gerados peloowner nativo; não faltava topologia entre as salas, faltava enquadramento ambiental. Câmera perspectivaFOV40,follow16/16/yaw0, sem zoom/órbita normais encontrados. BaselineA é o mesmo nó com a apresentação08 desligada, não outro mapa.','Baseline capturado na cópia, incluindo GameVieweHUD real; o pacote inclui o conceito separado para julgar a distância entre ideia e resultado.'),
('RUN_EXP_08_1_ROOM_SHAPE.md','08.1 — identidade espacial','Dar identidade visual à sala preservando métricas de combate.','Manto de pigmento estratificado, coroamentos assimétricos sobre parede física, costuras minerais verticais e uma fachada fixa recortada. Racks05 ocultos apenas nesse nó; piso mais contínuo e quieto. Duas coberturas têm faces pintadas, quinas cortadas e topo escuro. Não há maze, rotas extras nem obstáculo decorativo novo.','Trade-off deliberado: não cortar cantos do chão/collider nesta prova. A forma arquitetônica recebe assimetria sem inventar chão acessível onde a física bloqueia. Redesenho da planta física continua fora desta entrega.'),
('RUN_EXP_08_2_PORTAS_E_FLUXO.md','08.2 — portas e fluxo','Deixar entrada, saída e transição reconhecíveis pela arquitetura.','Pórticos abertos emcima preservam a silhueta do player. Entrada usa acento desaturado escuro; saída tem aresta marfim/sálvia e quatro travessas de piso no trecho existente deaproximadamente5m. Piers dentro das paredes laterais deixam4m livres. A malha pictórica escura do gate é filha daBarreira nativa: some/retorna exatamente comEncounterGates.','A transição é espaço já navegável que recebeu composição, não corredor extra nem espera. Nada reescreve grafo/porta. Estado fechado é massa opaca; aberto mostra o piso contínuo. Labels nativos, commit de rota e menu permanecem compatíveis. Não há medição humana de tempo de procura da saída.'),
('RUN_EXP_08_3_HYBRID_2_5D.md','08.3 — híbrido implementado','Provar um tratamento2.5D real no renderer existente.','Fachada fixa emmesh recortada opaca, UVs deilustração e semmargens transparentes; coberturas emvolume3D de baixa complexidade comfaces pintadas e topo separado. Planos não giram para câmera. Renderers físicos nativos e caixas de colisão não são substituídos por sprites. Câmera, LOS/nav/projéteis/dash continuam3D.','ReutilizaTech Guy/Illustrated Toon comoutline0/_OcclusionOpacity eURP/Lit no piso; nenhum shader/feature nova. A malha associada aoCollider permite o caminho dedither dofader. Só afaixa atual de follow foi exercitada; novas órbitas/zooms não são certificados. ToggleA/B é manual no menu de revisão e mudaapenas apresentação; não é frameworks de mundos.'),
('RUN_EXP_08_4_ATMOSFERA_E_POLISH.md','08.4 — atmosfera e polish','Tornar a sala pictórica com hierarquia de combate.','Albedo original de gouache mineral nas faces verticais; lilás/giz/sálvia contra ardósia escura. Piso com pincelada discreta e sem fendas falsas; topo decover escuro deixa altura e lados legíveis. Acentos de progressão não usam vermelho de telegraph. Luz/exposição existente preservadas; sem partículas ambientais sobre o combate.','A referência éC07, não imitação de uma pintura específica. O protótipo ainda usa volumes simples e não tem detalhe finalDCC. A impressão de beleza não é um número do teste; fontesconceituais e capturas reais são mostradas para revisão. Pintura detalhada não foi espalhada por todas as superfícies.'),
('RUN_EXP_08_5_VALIDACAO.md','08.5 — gameplay e custo','Validar funções preservadas e caracterizar o custo honestamente.','Três armas emcena isolada, Fragmento/Emissor naturais, duas rotas, movimento real, oclusão com collider intacto, LOS bloqueado/retomado, básicos/especial/QER/dash, ticketsC05, porta nativa, travessia real5m e volta à apresentação regular no próximo nó. Tripla nativa comtelegraph/recibosHP emfixture separada.','33/33 na seleção final;1/1 custo repetido isolado, semsomar como teste novo. Nenhuma avaliação humana ou benchmarkstandalone produzido; evidências/limites são parte doresultado, não resultado deconteúdo extra.')]
reports={}
for filename,title,objective,implementation,tradeoff in stages:
    text=f'# RUN-EXP-{title}\n\n03/11/2026, America/Sao_Paulo. **DONE no recorte técnico; PENDING DEVELOPER APPROVAL visual/gameplay.**\n\n## Objetivo\n\n{objective}\n\n## Resultado desta etapa e trade-offs\n\n{implementation}\n\n{tradeoff}\n\n'+shared
    reports[filename]=text
reports['RUN_EXP_08_RELATORIO.md']='''# RUN-EXP-08 — sala pictórica implementada, limitada e comparável

**DONE:** protótipoUnity isolado, uma sala e enquadramento de seus acessos, dois albedos originais, fachada fixa2.5D e coberturas comfaces pintadas, gameplay/oclusão/fluxo validados tecnicamente. **PENDING DEVELOPER APPROVAL:** resultado estético e sensação defluxo. **NOT DONE:** conversão produtiva deFirstSector, novos universos/boons/inimigos. **NOT TESTED:** GPU/standalone08 e playtest humano.

## Abrir o resultado jogável

Na Unity do projetoD:, usar Tools/Tech Guy/RUN-EXP-08/Open isolated room, emEditMode, depoisPlay. A cópia usa seed63 e começa no fluxo normal da rota; selecionar destino1, seguir acesso, entrar noencontro. Não altera aFirstSector publicada nem precisa habilitar uma cena nobuild. A estaçãoFirstSector doNexus continua indo àprodução normal. NoPlay, Tools/Tech Guy/RUN-EXP-08/Toggle baseline and painterly in Play alterna sóasrepresentações; não muda as regras físicas. Salvar trabalho próprio antes detrocar escena.

Cena:Assets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity. O autoradorBuild éopcional para reconstrução das referências08 e exige cenaatual limpa; nãoexecutar builders05/FirstSector para obter asimagens. O protótipo usa o loadout/save atual se você jogar manualmente; os testes automatizados isolaram/restauraram suas preferências correntes.

## Pacote e recomendação

[Revisão offline](../validation/artifacts/run-exp-08/review/index.html) · [ZIP](../validation/artifacts/run-exp-08/RUN_EXP_08_REVIEW.zip). Imagens deUnity, conceito, antes/depois, porta corrigida, trêsarmas, tripla, custo bruto e relatórios estão incluídos. A recomendação é continuar lapidando **essa mesma sala**, após sua avaliação. O manto pictórico eopórtico provam um caminho deautoria viável, sem aprovar conversão global. Aplanta física éamesma e isso fica explícito no pacote.

'''+shared

def polish(text):
    # Editorial spacing only. Identifiers and file paths stay literal.
    words={
        'projetoD:':'projeto D:', 'branchgameplayOverhaul':'branch gameplayOverhaul', 'HEAD766':'HEAD 766',
        'Unity6000':'Unity 6000','URP17':'URP 17','espaçoLinear':'espaço Linear','cenaFirstSector':'cena FirstSector',
        'nó1':'nó 1','seed63':'seed 63','câmeraFOV40':'câmera FOV 40','FOV40':'FOV 40','follow16':'follow 16',
        '5m':'5 m','4m':'4 m','28×24m':'28 × 24 m','4,2m':'4,2 m',
        '33/33Passed':'33/33 Passed','1/1Passed':'1/1 Passed','26casos':'26 casos','3armas':'3 armas',
        '0falhas':'0 falhas','0removidos':'0 removidos','0adições':'0 adições','0scripts':'0 scripts','6referências':'6 referências',
        '2368arquivos':'2368 arquivos','2368/2368arquivos':'2368/2368 arquivos','25arquivos':'25 arquivos','47preferências':'47 preferências','47prefs':'47 prefs','25novos':'25 novos',
        'doEditor':'do Editor','noEditor':'no Editor','noPlay':'no Play','emEditMode':'em Edit Mode','deFirstSector':'de FirstSector',
        'naUnity':'na Unity','deUnity':'de Unity','pelaUnity':'pela Unity','dadosUnity':'dados Unity','arquivosUnity':'arquivos Unity',
        'oautoradorBuild':'o autorador Build','daBarreira':'da Barreira','aoCollider':'ao Collider','emCollider':'em Collider','faderexistente':'fader existente',
        'somentea':'somente a','apenasapresentação':'apenas apresentação','dofoi':'do foi','estãoincluídos':'estão incluídos',
        'emmemória':'em memória','eAI':'e AI','eHUD':'e HUD','eLOS':'e LOS','eURP':'e URP','eseus':'e seus','eos':'e os','eopórtico':'e o pórtico','eoprÓximo':'e o próximo','eum':'e um',
        'oprotótipo':'o protótipo','opróximo':'o próximo','oresultado':'o resultado','osapoios':'os apoios','ostestes':'os testes','oponto':'o ponto',
        'apintura':'a pintura','acâmera':'a câmera','aarte':'a arte','abase':'a base','apresentação08':'apresentação 08','apresentação05':'apresentação 05',
        'atransição':'a transição','Atransição':'A transição','Aplanta':'A planta','Aassimetria':'A assimetria','afachada':'a fachada',
        'umespaço':'um espaço','umvolume':'um volume','umponto':'um ponto','umbloco':'um bloco','umanova':'uma nova','amesma':'a mesma','amesma':'a mesma',
        'dedetalhe':'de detalhe','dedither':'de dither','debaixo':'de baixo','deaproximadamente':'de aproximadamente',
        'defluxo':'de fluxo','deidentidade':'de identidade','deautoria':'de autoria','deleitura':'de leitura','decusto':'de custo','decolisão':'de colisão',
        'deinput':'de input','deuma':'de uma','detransição':'de transição','deboons':'de boons','debaseline':'de baseline','deconteúdo':'de conteúdo',
        'dopiso':'do piso','docorredor':'do corredor','dojogo':'do jogo','docombate':'do combate','doprojeto':'do projeto','dopórtico':'do pórtico','docorpo':'do corpo','dofluxo':'do fluxo',
        'daferramenta':'da ferramenta','dapintura':'da pintura','daprimeira':'da primeira','dasala':'da sala','dareferência':'da referência',
        'naresoluçãooriginal':'na resolução original','namesmo':'no mesmo','nomesmo':'no mesmo','namesma':'na mesma','naslaterais':'nas laterais',
        'noencontro':'no encontro','naofinal':'no final','noframe':'no frame','notopo':'no topo','nobuild':'no build','novermelho':'no vermelho',
        'semGPU':'sem GPU','semDCC':'sem DCC','semnormais':'sem normais','semalpha':'sem alpha','semrolloutglobal':'sem rollout global','semexpansão':'sem expansão','Semexpansão':'Sem expansão',
        'semadicionar':'sem adicionar','semmargens':'sem margens','nãoreproduz':'não reproduz','nãomedidos':'não medidos','nãotestada':'não testada','nãotopologia':'não topologia','nãohá':'não há',
        'comjogador':'com jogador','comtelegraph':'com telegraph','comfaces':'com faces','compintura':'com pintura','comarte':'com arte','porface':'por face',
        'porbloqueio':'por bloqueio','porrecibosHP':'por recibos HP','àprodução':'à produção','àfrente':'à frente','dosescopos':'dos escopos',
        'duasarmas':'duas armas','trêsarmas':'três armas','doisalbedos':'dois albedos','cincomateriais':'cinco materiais',
        'Pngs':'PNGs','fonteblend':'fonte blend','semDCC':'sem DCC','nenhumboonfoi':'nenhum boon foi',
        'imagenssãolocais':'imagens são locais','Todasasimagenssãolocais':'Todas as imagens são locais','screenshotssãocategorias':'screenshots são categorias',
        'eclicáveis':'e clicáveis','umshader':'um shader','evidênciaMCP':'evidência MCP','focada':'focada',
        'nafronteira':'na fronteira','racks05':'racks 05','topoescuro':'topo escuro','doisPNG':'dois PNG','doisPNG':'dois PNG',
        'Seleçãofinal':'Seleção final','casos08':'casos 08','física/roster':'física / roster','conversãoglobal':'conversão global',
        'protótipoUnity':'protótipo Unity','fixa2.5D':'fixa 2.5D','tratamento2.5D':'tratamento 2.5D',
        'depoisPlay':'depois Play','destino1':'destino 1','aFirstSector':'a FirstSector','estaçãoFirstSector':'estação FirstSector','doNexus':'do Nexus',
        'NoPlay':'No Play','sóasrepresentações':'só as representações','detrocar escena':'de trocar de cena','Cena:Assets':'Cena: Assets',
        'O autoradorBuild':'O autorador Build','éopcional':'é opcional','referências08':'referências08','cenaatual':'cena atual','nãoexecutar':'não executar','asimagens':'as imagens',
        'éa mesma':'é a mesma','realD:':'real D:','inicialNexusBlockout':'inicial NexusBlockout','emDocs/':'em Docs/','éC07':'é C07',
        'háAGENTS':'há AGENTS','emAssets/':'em Assets/','cópiaAssets/':'cópia Assets/','materiaisMineralPigment':'materiais MineralPigment',
        'partialsRunExp03':'partials RunExp03','ePainterly08':'e Painterly08','Semcommit':'Sem commit','comoutline0':'com outline0',
        'emmesh':'em mesh','deilustração':'de ilustração','emvolume3D':'em volume3D','continuam3D':'continuam 3D','ReutilizaTech':'Reutiliza Tech',
        'dofader':'do fader','afaixa':'a faixa','mudaapenas':'muda apenas','toggleA/B':'toggle A/B','ToggleA/B':'Toggle A/B',
        'Seleção anterior':'Seleção anterior','jobe5':'job e5','jobe0':'job e0','joba':'job a','jobe':'job e','job9d':'job 9d',
        'jobae':'job ae','comRecorder':'com Recorder','emframes':'em frames','seisreferências':'seis referências',
        'focadas08':'focadas08','àsmétricas':'às métricas','centroslocais':'centros locais','noscentros':'nos centros',
        'emcena':'em cena','emfixture':'em fixture','semsomar':'sem somar','benchmarkstandalone':'benchmark standalone',
        'ticketsC05':'tickets C05','real5 m':'real 5 m','recibosHP':'recibos HP','deRecorder':'de Recorder',
        'baseline366':'baseline 366','de587':'de 587','para418':'para 418','documentos07':'documentos07',
        'semtempo':'sem tempo','fixa2':'fixa 2','dequadros':'de quadros','coroamentosassimétricos':'coroamentos assimétricos',
    }
    for old,new in words.items():text=text.replace(old,new)
    text=re.sub(r',(?=[A-Za-zÀ-ÿ])',', ',text)
    text=re.sub(r'(?<=\d)(?=(?:casos|testes|arquivos|referências|preferências|materiais|imagens|mips|falhas|novos)\b)',' ',text)
    return text
reports={name:polish(t.replace('33/33 na seleção final','31/31 na revisão final; 33/33 na seleção anterior ao ajuste de sombras')) for name,t in reports.items()}

for filename,text in reports.items():
    (DEC/filename).write_text(text,encoding='utf-8');(REVIEW/'reports').mkdir(exist_ok=True)
    portable=text.replace('../validation/artifacts/run-exp-08/review/index.html','../index.html').replace('[ZIP](../validation/artifacts/run-exp-08/RUN_EXP_08_REVIEW.zip)','ZIP na pasta original da entrega')
    (REVIEW/'reports'/filename).write_text(portable,encoding='utf-8')
    (REVIEW/'reports'/filename.replace('.md','.html')).write_text('<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+esc(filename)+'</title><style>'+CSS+'</style><main><a href="../index.html">← Revisão08</a>'+md(portable)+'</main></html>',encoding='utf-8')

body='''<header><span class="badge pending">IMPLEMENTADO · REVISÃO HUMANA PENDENTE</span><p>TECH GUY / RUN-EXP-08</p><h1>Pigmento, volume<br>e passagem.</h1><p>Uma sala pictórica jogável na Unity. Mesma física e combate; um teste limitado de identidade, portas e fluxo.</p></header><nav><a href="#sala">Sala A/B</a><a href="#fluxo">Portas e transição</a><a href="#conceito">Conceito × implementado</a><a href="#combate">Gameplay</a><a href="#custo">Custo</a><a href="#relatorios">Relatórios</a></nav><aside class="note">31/31 na revisão final; 33/33 antes do ajuste de sombras. Beleza e clareza percebida do fluxo aguardam sua avaliação. Nenhuma outra sala foi convertida; a planta navegável continua 28×24m. A assimetria é arquitetônica/visual.</aside><section id="sala"><h2>Mesma sala. Mesma câmera. Dois tratamentos.</h2><p>Capturas reais com jogador/roster e HUD. Antes/depois congelados no mesmo ponto. A referência A é a arte05 produtiva; B é o protótipo08. A cópia isolada deixa o próximo nó com arte normal.</p><div class="pair comparison">'''
body+=figure('gameplay/weapon-0/08-baseline-room.png','Unity baseline capture · seed63/nó1')+figure('gameplay/weapon-0/08-painterly-room.png','Implemented prototype · sala08 pictórica')+'</div></section>'
body+='<section id="fluxo"><h2>O jogador atravessa; a arquitetura não o esconde.</h2><p>O lintel daprimeira versão ocultavaocorpo. A revisão abriuocentro superior dopórtico, preservou4m delargura física e associou osapoios ao fader existente. Atransição usa5m docorredor nativo, semadicionar tempo morto.</p><div class="pair comparison">'+figure('images/iteration-1-occluded-door.png','Technical comparison · iteração rejeitada: lintel ocultava jogador')+figure('gameplay/weapon-0/08-open-door-transition.png','Implemented prototype · travessia revisada, corpo visível')+'</div><div class="pair">'+figure('gameplay/weapon-0/08-next-room-regular-flow.png','Implemented prototype · próximo nó continua comapresentação regular')+figure('gameplay/weapon-0/08-painted-cover-occlusion.png','Implemented prototype · fade visual, collider preservado')+'</div></section>'
body+='<section id="conceito"><h2>Da referência aprovada ao jogo real</h2><div class="pair comparison">'+figure('images/approved-C-concept.png','Concept art · direçãoC07 aprovada pelo pedido08')+figure('gameplay/weapon-0/08-painterly-room.png','Implemented prototype · sem retoque sobre aGameView')+'</div><details><summary>Fontes de pintura e limite de acabamento</summary>'+figure('materials/PigmentStrata.png','Original generated material source · não é screenshot')+figure('materials/QuietSlate.png','Original generated material source · piso debaixo contraste')+'<p>DoisPNG originais doImageGen, prompts incluídos. O protótipo nãoreproduz todo detalhe/curvatura da pintura conceitual; mantém volumes/rotas físicos aprovados. SemDCC, não háfonteblend/PSD fictícia.</p></details></section>'
body+='<section id="combate"><h2>Combate na sala implementada</h2><div class="grid three">'
for weapon,label in [(0,'Manopla'),(1,'Arco'),(2,'Lança')]:
    body+=figure(f'gameplay/weapon-{weapon}/08-native-skill-0.png',f'Implemented prototype · {label}, habilidade nativa')
body+='</div><div class="pair">'+figure('gameplay/weapon-0/natural-Fragmento-windup-node-1.png','Implemented prototype · windup natural deFragmento')+figure('gameplay/weapon-0/emissor-reacquired-los-node-1.png','Implemented prototype · Emissor readquireLOS apóscontorno')+'</div><div class="pair">'+figure('triple/08-triple-warning.png','Implemented prototype · tripla eHeavySlam, fixture assistida')+figure('triple/08-triple-confirmed.png','Implemented prototype · efeitos nativos confirmados porrecibosHP')+'</div><p>Gameplay automatizado: imunidade/recursos/warps declarados. Tripla usa composição controlada egrants emmemória; nenhumboonfoi refeito. Não é playthrough humano ouprova deobtenção natural.</p></section>'
body+='<section id="custo"><h2>Custo: observação local, sem promessa de otimização</h2>'+md(cost_table)+'<p>Editor, ABBA, mesmo roster inicial e AI ativa; agentes e população evoluem entre blocos. Baseline teve '+str(cost['0']['enemy_counts'])+' atores ativos; protótipo '+str(cost['1']['enemy_counts'])+'. GPU, VRAM e Standalone08 não medidos. O ajuste de sombras/passe Ink reduziu o protótipo de587 para418 draw calls de mediana; baseline366. Amostras e condições incluídas.</p><a href="performance/frames.csv">CSV bruto</a> · <a href="performance/conditions.txt">Condições</a> · <a href="performance-summary.json">Resumo calculado</a></section>'
body+='<section id="relatorios"><h2>Reprodução, auditoria e decisão</h2><p>NaUnity: Tools/Tech Guy/RUN-EXP-08/Open isolated room; Play; selecionar nó1. ToggleA/B pelo menu08 durantePlay. Produção, input, economia eboons permanecem nosowners atuais.</p><ul>'
for name in reports:body+=f'<li><a href="reports/{name.replace(".md",".html")}">{name}</a> · <a href="reports/{name}">Markdown</a></li>'
body+='</ul><div class="note"><strong>Revisão solicitada:</strong> a sala implementada tem aidentidade desejada? Aporta eoprÓximo espaço ficammais claros? Qualelemento atrapalhaocombate ouparece aindagenérico? Apontar atéduas correções prioritárias na mesma sala. Semexpansão automática deconteúdo.</div><p>Integridade:2368arquivos antigos e47preferências idênticos;25novos arquivosUnity. HTMLverificado estaticamente; renderização emnavegador nãotestada porbloqueio defile: daferramenta. Fontes/conceitos/screenshotssãocategorias separadas.</p></section><footer>STOP apósRUN-EXP-08 · semrolloutglobal,commit,pushouboonrework. Todasasimagenssãolocais eclicáveis naresoluçãooriginal.</footer>'
# Normalize recurring word boundaries in generated prose, keeping identifiers/paths intact.
for a,b in [('Unity6000','Unity 6000'),('URP17','URP 17'),('projetoD:','projeto D:'),('branchgameplay','branch gameplay'),('HEAD766','HEAD 766'),('33/33Passed','33/33 Passed'),('1/1Passed','1/1 Passed')]:body=body.replace(a,b)
body=polish(body)
(REVIEW/'index.html').write_text('<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tech Guy / RUN-EXP-08</title><style>'+CSS+'</style></head><body><main>'+body+'</main></body></html>',encoding='utf-8')

# Images are originals; provenance names and hashes prevent concepts being passed off as runtime.
assets=[]
for p in REVIEW.rglob('*.png'):
    raw=p.read_bytes();w,h=struct.unpack('>II',raw[16:24]);rel=p.relative_to(REVIEW).as_posix()
    category='Concept art' if 'approved-C' in rel else 'Original generated material source' if rel.startswith('materials/') else 'Unity baseline capture' if 'baseline' in p.name else 'Technical comparison' if 'iteration-1' in p.name else 'Implemented prototype'
    assets.append({'id':rel,'file':rel,'category':category,'width':w,'height':h,'sha256':hashlib.sha256(raw).hexdigest(),'edits':'None; original PNG copied','approval':'PENDING DEVELOPER APPROVAL of implemented result','provenance':'ImageGen integrated' if category in ('Concept art','Original generated material source') else 'Unity ScreenCapture / declared UTF fixture'})
(REVIEW/'asset-manifest.json').write_text(json.dumps({'assets':assets,'material_prompts':['prompts/PigmentStrata.txt','prompts/QuietSlate.txt'],'reference':'RUN-EXP-07 direction C, not an undocumented07B','runtime_methods':'Fixed opaque facade mesh + painted face cover volumes; native physics preserved'},indent=2),encoding='utf-8')
for target,link in [('Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md','RUN_EXP_08_RELATORIO.md'),('Docs/gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md','../decisions/RUN_EXP_08_RELATORIO.md'),('Docs/validation/01_MATRIZ_DE_VALIDACAO.md','../decisions/RUN_EXP_08_RELATORIO.md'),('Docs/art/01_DIRECAO_VISUAL.md','../decisions/RUN_EXP_08_RELATORIO.md'),('Docs/art/02_PIPELINE_DE_ASSETS.md','../decisions/RUN_EXP_08_RELATORIO.md')]:
    p=ROOT/target;t=p.read_text(encoding='utf-8');marker='<!-- RUNEXP08 CURRENT -->'
    if marker in t:
        t=re.sub(r'<!-- RUNEXP08 CURRENT -->.*?<!-- /RUNEXP08 CURRENT -->\s*','',t,flags=re.S)
        p.write_text(t,encoding='utf-8')
    if marker not in t:
        at=t.find('\n')+1;block=f'\n{marker}\n## RUN-EXP-08 — protótipo pictórico isolado; STOP\n\n03/11/2026. Direção C/pictórica escolhida explicitamente no pedido08. Cópia de cena com nó1 e seus acessos: fachada fixa2.5D, coberturas pintadas, piso quieto e pórticos revisados; física/roster/combate/grafo intactos. Revisão final31/31; seleção anterior33/33. Custo Editor ABBA: drawcalls366→418, semGPU/standalone; tempos não isolam arte porque AI/população evoluem.2368 arquivos antigos e47prefs idênticos;25 arquivos novosUnity. Beleza/fluxo percebido **PENDING DEVELOPER APPROVAL**; sem conversão produtiva ou conteúdo novo. [Relatório, imagens e limites]({link}).\n<!-- /RUNEXP08 CURRENT -->\n\n';p.write_text(t[:at]+polish(block)+t[at:].lstrip('\n'),encoding='utf-8')
print(json.dumps({'reports':len(reports),'images':len(assets),'cost':cost},ensure_ascii=False))
