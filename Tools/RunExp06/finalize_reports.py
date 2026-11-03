"""Render06 reports from preserved, successful jobs; keep historical documents intact."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-06'
DOC=ROOT/'Docs/decisions'
read=lambda name:json.loads((OUT/name).read_text(encoding='utf-8'))
jobs=[]
for label,name in [('FirstSector + UI produtiva e catálogo','final-play'),('Regressões de combate/input/boons','regression'),('HP/atribuição e efeitos de habilidades','skill-effects'),('Básicos/combos e câmera/oclusão','extra-combo-camera-final'),('Conteúdo/geometria/encontros/arte','final-edit')]:
    d=read(name+'.json')['data'];s=d['result']['summary']
    if d['status']!='succeeded' or s['failed'] or s['skipped']:raise RuntimeError('Inspect unsuccessful final selection '+name)
    jobs.append((label,d['job_id'],s))
table='| Seleção | Job | Resultado | Duração |\n|---|---|---|---|\n'
for label,job,s in jobs:table+=f"| {label} | `{job}` | {s['passed']}/{s['total']}, falhas0, skipped0 | {s['durationSeconds']:.3f}s |\n"
integrity=read('integrity-final.json')
if integrity['missingFiles'] or integrity['baselineGameplayPreferencesChanged'] or integrity['addedGameplayPreferences']:raise RuntimeError('Review current06 integrity changes before finalizing')
perf=read('performance-summary.json')
if len(perf['standalone'])!=6:raise RuntimeError('Standalone six-sample proof missing')
build=read('standalone-build.json')
editor=read('editor-final.json')['data']['result'];editor=json.loads(editor) if isinstance(editor,str) else editor
if editor['play'] or editor['dirty'] or editor['assistance'] or editor['nextSeed'] or editor['temporaryRewardViews']:raise RuntimeError('Editor handoff state not restored')
console=read('console-final-errors.json')
if console['data']:raise RuntimeError('Inspect final Console errors')
edtable='| Ativos | Frame p95 baseline06 → atual ms | BehaviourUpdate atual p95 ms | Nav p95 ms | Pose p95 ms | Skinning.Update p95 ms | GC p95 bytes |\n|---|---|---|---|---|---|---|\n'
geotable='| Ativos | Renderers rígidos | Triângulos rígidos | Skinned/triângulos | Drawcalls globaisEditor p95 | Spawn / cleanup decorrido ms |\n|---|---|---|---|---|---|\n'
for count in (8,32,64):
    b=next(r for r in perf['editor'] if r['version']=='baseline06' and r['count']==count)
    r=next(r for r in perf['editor'] if r['version']=='current06' and r['count']==count);m=r['markers']
    edtable+=f"| {count} | {b['frameMs']['p95']:.2f} → {r['frameMs']['p95']:.2f} | {m['BehaviourUpdate']['p95']/1e6:.3f} | {m['NavMeshManager']['p95']/1e6:.3f} | {m['RUNEXP03.EnemyPose']['p95']/1e6:.3f} | {m['MeshSkinning.Update']['p95']/1e6:.3f} | {m['GC Allocated In Frame']['p95']} |\n"
    geotable+=f"| {count} | {r['renderers']} | {r['triangles']} | {r['skinned']}/{r['skinnedTriangles']} | {r['drawCallsEditor']['p95']} | {r['spawnMs']:.2f} / {r['cleanupMs']:.2f} |\n"
sttable='| Ativos | Quadros arte04/05 | Frame p95 arte04 → arte05 ms | MainThread05 p95 ms | GPU p95 arte04 → arte05 ms | GPU válidos04/05 | Memória alocada05 p95 MiB |\n|---|---|---|---|---|---|---|\n'
for count in (8,32,64):
    a=next(r for r in perf['standalone'] if r['population']==count and r['version']=='04');b=next(r for r in perf['standalone'] if r['population']==count and r['version']=='05')
    sttable+=f"| {count} | {a['frames']}/{b['frames']} | {a['frameMs']['p95']:.2f} → {b['frameMs']['p95']:.2f} | {b['markers']['Main Thread']['p95']/1e6:.2f} | {a['gpuMs']['p95']:.2f} → {b['gpuMs']['p95']:.2f} | {a['gpuValidFrames']}/{b['gpuValidFrames']} | {b['allocatedMemoryBytes']['p95']/1048576:.2f} |\n"

validation=f'''# RUN-EXP-06.4 — integração, regressões, custo e integridade

03/11/2026, America/Sao_Paulo. **Concluído no escopo06.** Projeto realD:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1/MCP Tech-Guy@fccafb2b47cd7671; mesma branchgameplayOverhaul, sem descarte do trabalho02–05. O pedido06 aprova a direção/gameplay/UI01–05 e autoriza publicaçãoC05. A instrução posterior do usuário também autoriza commit/push de todas as alterações e desligamento somente após salvar e confirmar o push. O registroGit de entrega é separado dos resultados abaixo.

## Seleções finais executadas nesta etapa

{table}

Seleções completas em `validation-selections-complete.json`, resultados integrais nosJSONs correspondentes. São seleções específicas, não a suíte global inteira. Reexecuções e seleções sobrepostas não somam testes únicos. Baseline atual antes da edição: `0d8453e3ded9498eac248ed69c7a4313`,13/13Play,85,144s. Primeira integração27/27: `5ed555724612428789c8e41cb2c440d5`,289,124s; a seleção final acima inclui as correções de apresentação e fixtures.

Os27 incluem19FirstSector e8UI: rotas naturais anteriores e quatro rotas novas Nexus→FirstSector→salas→C05/tickets→boss→extração→Nexus, três armas, rosters/matriz de12cenários, fases/apoios/dano, pausa/controle/reuso/despawn, cobertura/LOS/duas rotas, habilidades/especial/dash,8/32/64 ativos, catálogo integral, ranks/contexto e input. As novas rotas0/2/1 usam Manopla/Arco/Lança e resolvem tickets por C05 automaticamente instalada, sem menus/review/override. A quarta usa seed aleatória, assistência desligada; o valor real final foi4976199183887837982. Conclusão por warp/dano letal, imunidade do jogador e acesso temporário de loadout são declarados como fixture; não são sobrevivência humana nem desbloqueios conquistados. Sem injetar ofertas, conceder boons, alterar RNG/pools ou garantir sinergias.

As33 regressões cobrem HookLine repetido/sobreposto, janelas reais de movimento/dash, pausa/hitstop/morte/unload, vento/fogo/eletricidade, autoridades de input remapeável, recompensas, paredes, Frost e contexto/gerações/leases, Bow e Spear. As10 verificam HP real/atribuição de Q/W/E/R internos e efeitos Bow/Spear, custos e cleanup. Os29 adicionais são26 casos de câmera/oclusão mais dois de combo/cadência e um de estatísticas de básicos, sem modificar a câmera para publicar UI. Os29Edit validam conteúdo, geometria, encontros, metadados/arte e elegibilidade. Não afirmam validar toda combinação de executor/boon/inimigo.

## Recompensa e inspeção visual

C05 é instalada pelo ownerRunRewardUI normal e usa RunBoons.Choose/ticket nativos. Ver [06.1](RUN_EXP_06_1_REWARD_UI_PRODUCTION.md). Catálogo:40 linhas por arma/32 IDs com testemunho de elegibilidade legal em fixture isolada;8 IDs usam7 imagens dedicadas,24 usam fallback coerente de família. Piercing permanece fora do pool e sua imagem não foi aplicada indevidamente. Mostruário assistido é somente leitura, distinto das rotas produtivas.

Capturas atuais de produção1160×600/1280×800/1920×1080 mostram o fluxo real das três armas. O índice `final-capture-index.json` copia364 arquivos do intervalo efetivo do job final, com origem ehash. Capturas anteriores que mostravam popups sobre os cards permanecem como evidência intermediária. A correção mantém a profundidadeGUI do MonoBehaviour da view; popups/dano não mudaram. Confirmação em detalhes/duplicada não deixa a mensagem falsa de oferta indisponível.

Mouse físico via skillcomputer-use/sky no ticketFirstSector seed0: selecionar segunda oferta, abrir/fechar detalhes e incorporar concedeu só `augment_gauntlet_echoing_fist`, uma aquisição. A próxima oferta nativa abriu automaticamente; fechar manteve o ticket pendente e reabrir preservou conductor/detonation/ignite. Sem Lab. `mouse-*.json` registra seleção/posse/estado e capturasGameView. Uma capturaWindows teve erro de crop e foi recuperada por novo inventário da janela; a primeira pintura da janela estava vazia, antes dos cliques de jogo. Não houve coordenadas de jogo adivinhadas.

Teclado/gamepad são exercitados pelos testes existentes e novos. O novo caso de foco/remapeamento confirma: sem aquisição ao perder foco ou retornar com Enter segurado; detalhes bloqueiam confirmação; Esc fecha detalhes antes da pausa; Skill1 temporariamenteEnter não dispara ao confirmar ou entrar em combate segurando; nova pressão após liberar dispara normalmente. A contagem de tickets, ID correto, duploConfirm, cancelamento/reabertura e ranks permanecem nativos. Mouse/gamepad virtuais são evidência automatizada, separados do mouseWindows observado.

## Falhas preservadas e correções

- `3f72768d285f40c1819bb94a8f40746b`,0/4,3,383s: fixture nova supunhaScenePortal na entradaNexus. O fluxo real é LobbyInteraction.Station. Corrigida somente a fixture para acionar o binding atual de interação perto da estação nativa. `1d7235b8aef54cae930824552195cb79` passou5/5,24,863s, depois as rotas entraram no27 final.
- `2c790ad01b744171acab07ae70e2017f`,28/29,3,049s: estatísticas de básico consultadas imediatamente após aceitar o segundo ataque, antes do contato agendado. Fixture agora espera o fim nativo do básico e mantém a asserção de duas transações/uses e HP real. Reexecução adicional29/29 acima. Casos de câmera já passavam nessa execução.
- `4399bbf1f72849e68ec8a9b3e3b15331`,26/27,288,707s: lifecycle verificava lock0,25s depois de yields/captura que podiam consumir a janela noEditor ocupado. A fixture afirma o lock imediatamente e pausa o relógio antes de capturar; conserva asserções de lock durante pausa e recuperação após retomar. Final27/27 confirma a correção; nenhuma duração/AI/resistência de combate foi retunada.
- Compilação da primeira fixture tentou acessar uma constante privada de unlock e uma classeEditor da assemblyPlay. Corrigidas referências do teste para o contrato existente, sem alterar o runtime. Build devolveu erro de respostaMCP durante a desconexão da ponte; o relatório persistidoSucceeded e a execução do aplicativo confirmam conclusão, sem repetir o build por causa da resposta perdida.
- O agregador de evidência inicialmente não reconheceu frações de sete dígitos das pastasUTCUnity. Corrigiu-se o parser para a precisão de seis dígitosPython e verificaram-se364 cópias; nenhuma captura foi inventada/retocada.

## Desempenho noEditor — baseline desta entrada06

{edtable}

{geotable}

Mesma fixture nativa de população1:1:1 Fragmento/Emissor/Grunt pelo diretor,1s de aquecimento e5s de amostragem. AI/nav/dano/poses ativos; jogador imune e ocioso, moedas suprimidas somente na fixture. BehaviourUpdate é agregado; GC incluiEditor/UTF/LINQ/IMGUI. Drawcalls são globais doEditor, não da câmera do jogador; batches indisponíveis não são zero. Skinning.Update é agregado dos atores que usam esse caminho, não rig novo dos candidatos. Cleanup inclui espera deliberada0,4s, não CPU deDestroy. Renderers/triângulos rígidos e skinned permaneceram iguais ao baseline06. Frame/GPU/modalC05 e cada VFX não foram isolados aqui.

A diferença de frame não comprova otimização: horário, fases de AI, carga doEditor, posição e callbacks variam. Não houve mudança de geometria/AI de inimigos06. O baseline já contém a arte05; não confundir com geometria04. Os testes de vento/fogo/eletricidade/teto de procs são regressão funcional separada, não benchmarks de todas as builds.

## Standalone atual realmente executado

BuildWindows64 releaseMono2x: {build['bytes']}bytes, {build['seconds']:.3f}s,0erros/{build['warnings']}avisos. `standalone-build.json` preserva mensagens: umCS0108ScenePortal, seis APIs de busca deprecadas no harness05 e dois avisos de transporteMCP. São dívida/avisos preservados, não build semwarnings.

Reutilizado o harness05 **sem alterar sua fonte/cenas**, compilado contra o runtime atual06. Exige `--runexp05-profile`; labels04/05 significam variantes de arte no mesmo executável, não builds históricos antes/depois da UI. Produto isoladoTechGuyReviewLocal/RUNEXP06Profile, conferido emapp.info; company/product/frameTiming do projeto restaurados emfinally. Nenhuma segundaUnity foi aberta no checkout. CaminhoBuilds/RunExp06Profile, ignorado porGit.

Hardware{perf['standalone'][0]['cpu']} / {perf['standalone'][0]['gpu']}. Resolução efetiva1920×1080, conferida nos relatórios, embora os argumentos solicitassem1280×800; não apresentar a resoluçãoCLI como a medida real. Vsync0/framecap-1. Cada par mantém a mesma coorte8/32/64,1,5s de aquecimento e4s de amostragem por variante, ordem invertida em32. AI/ataques continuam evoluindo, portanto posições/windups não são os mesmos instantes. Todas as amostras conservaram a população ativa; impactos reais foram registrados. O aplicativo terminou sozinho após seisJSONs/PNGs ecomplete.txt.

{sttable}

GPU é FrameTimingManager, somente amostras positivas; valores ausentes foram excluídos e contados, não convertidos emzero. SóMainThread esteve disponível entre os marcadoresCPU de release desejados; GC, Nav, pose, drawcalls/batches individuais são **não medidos** nessa execução. Memória é totalUnity alocada, nãoVRAM nem custo exclusivo de texturas; ambos conjuntos de assets estão carregados. Não mede isoladamente custo do modalC05 ou cadaVFX. Em64, p95 de frame da arte05 ficou maior apesar da mediana menor: os resultados não sustentam alegação de otimização ou orçamento de hardware final. Não foi identificado bloqueador novo nesse recorte.

## Integridade e estado entregue

Baseline06:2739 hashes e47 preferências de gameplay existentes. Na inspeção antes da atualização documental final:2723 arquivos idênticos,16 fontesC# esperadamente diferentes,0 desaparecidos após mapear cincoCS e cinco metas movidos. As cinco metas movidas têm bytes/GUIDs iguais. Os16 incluem owners/view/model/artmap, referências mecânicasEditor/testes e as duas correções de fixture descritas. A atualização posterior de documentos canônicos aparece nominalmente emintegrity-final.json; não é mudança de assets. Ver contagens finais nesse artefato, que é refeito após os relatórios.

**47/47 preferências de gameplay existentes iguais;0 removidas/alteradas e0 novas nesses dois escopos observados.** Não restaurar snapshots04/05: as15 adições legítimas vistas noPlayer antes06 fazem parte deste baseline. Valores privados continuam sóLibrary; relatórios publicam hashes/nomes. Scopes atuais de moeda dos jobs33/10/adicional e currency/loadout/summary/seed/assist do mouse foram fechados. Player original não foi restaurado. Isso não garante todos os saves do disco, somente os escopos e arquivos observados.

Scenes/prefabs/materials/texturas/definições/input/ProjectSettings/Packages/fontfallback/fontesDCC permanecem byte a byte. FirstSector reabertaMCP: GUIDf62bb8b29264e014aa2e95e916388ed9, limpa,314dependências,missing0; prefabsFragmento/Emissor05 válidos,18/7renderers,5772/2172tris,cápsulas0,85/0,8,missing0. Cinco refsArchive/UVs/FBXnão-readable confirmados. Câmera serializada perspectivaFOV40/altura16/distância16/ângulo0 confirmou as hipóteses06.5. Não há renderer2.5D.

O testelegado regravou integration-movement.json, comprovadamente limpo na entrada06. Conteúdo novo foi arquivado emmovement-regression-current06.json e somente o arquivo histórico limpo voltou aos bytesHEAD; semreset/checkout do projeto. Trabalho anterior02–05 e a exclusão preexistenteDocs.zip preservados para o commit integral autorizado.

Checker:2877arquivos, um erro históricoPlayground missing-script GUID9d54880da6396da4c8cb3d4f4d1289a3 e um aviso históricoTitanicCompositionFrameSampler/filename; não reparados fora de escopo. Console final explicitamente consultado sóerros:0, semlimpeza manual. Há avisos conhecidos de compilação/runtime e seu registro foi preservado. NexusBlockout limpa/Edit, Time1, assistênciafalse, nextSeedvazio, nenhuma view temporária/missingScript/scope pendente.

## Gates, skills e limites

GatesA/B técnicos: caminho normal/C05 real e catálogo verificados. C/D: mesmos inimigos/ataques/LOS/rotas com evidência atual. E: nenhuma regressão bloqueante nova confirmada no escopo, falhas intermediárias preservadas e corrigidas. F:47prefs e assets protegidos preservados no escopo observado. G: direção/brief tecnicamente delimitados, sem implementação07. Aceite humano01–05/C05 foi recebido no pedido06; UTF não é um novo playtest humano nem afirma acabamento final.

Skills lidas/aplicadas: quatroTechGuy locais e referências; router,UnityMCP/C#/InputSystem/animation/NavMesh/build, game-ui-ux/input-systems/game-feel, create-game-assets/direção/3D/proveniência, performance-optimization, level-design/tech-guy-level-layout e tech-guy-world-identity/world-card/shader-programming para planejamento, computer-use para mouse. Nenhuma skill dedicadaBlender/rig/renderer2.5D foi inventada. Não houve nova operaçãoDCC06, pois a arte já estava integrada e aceita.

**Não concluído por escopo:**24 ilustrações dedicadas, rig/clipsDCC/LOD finais, reforma do gerador, novos mundos e conversão2.5D. **Não testado:** outros hardwares, GPU/VRAM de técnicas2.5D, custo individual deVFX/modal e todo produto cartesiano de boons. O próximo experimento requer revisão das imagens e do resultado no jogo; nada aqui inicia07.
'''
(DOC/'RUN_EXP_06_4_VALIDATION.md').write_text(validation,encoding='utf-8')

report=f'''# RUN-EXP-06 — integração produtiva concluída e próxima direção preparada

03/11/2026. C05 agora é a interface normal dos tickets de recompensa de FirstSector, sem laboratório, menu de desenvolvimento ou seed especial. Fragmento/Emissor e ambiente05 já estavam integrados; foram consolidados e verificados sem duplicar assets, spawn ou arquétipos. A aprovação01–05/C05 foi recebida expressamente no pedido06. O acabamento de arte ainda tem limitações descritas abaixo.

O planejamento2.5D está pronto em [06.5](RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md), com três linguagens originais exploratórias e contratos comuns de leitura/física. O [brief07](RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md) compara uma fachada e uma cobertura3D/híbrida na mesma câmera/sala, exige revisão das imagens antes da conversão e **não autoriza começar07**. Nenhum mapa foi convertido, renderer/worldmanager criado ou universo publicado06.

## Entrega para revisar

[Revisão visual portátilHTML](../validation/artifacts/run-exp-06/review/index.html) · [ZIP](../validation/artifacts/run-exp-06/RUN_EXP_06_REVIEW.zip) · [24 ilustrações dedicadas ainda faltantes](../validation/artifacts/run-exp-06/reward-illustrations-missing.md) · [Catálogo32 IDsCSV](../validation/artifacts/run-exp-06/reward-catalogue.csv).

O pacote distingue fluxo produtivo, comparadores assistidos e medições; contém capturas atuais nas três armas, fallback, detalhes, contextos/rank, salas/inimigos/LOS e aplicativo real. Arte aprovada não foi remodelada para fabricar uma reintegração. Cinco scripts de apresentação foram promovidos porAssetDatabase mantendoGUIDs; RunRewardUI continuaowner e RunBoons/ticket continuamautoridades. Detalhes/pausa/foco/cancelamento/reabertura/remapeamento e confirmação exata foram exercitados. A revisão visual detectou e corrigiu popups sobre a tela; não houve tuning de combate/RNG/economia.

{table}

São seleções reais desta execução, sem alegação de suíte global verde. [06.4](RUN_EXP_06_4_VALIDATION.md) registra jobs intermediários/falhas/correções, limites e números brutos. MouseWindows selecionou/incorporou uma oferta legal emFirstSector e preservou o próximo ticket ao fechar/reabrir. Os três percursosNexus→boss→Nexus são automatizados, com imunidade/warp/conclusãoletal declaradas; uma run sem assistência também usou seed aleatória. Não são um playthrough humano de sobrevivência.

## Como reproduzir com segurança

1. Abrir `D:/Dev/Personal/My_games/Tech-Guy` na Unity6000.5.10f1. Salvar trabalho próprio antes de trocar cena; não abrir uma segundaUnity no mesmo projeto. Cena normal inicialNexusBlockout.
2. Usar o arsenal e os loadouts já disponíveis na sua progressão. A cobertura automatizadaArco/Lança usa acesso temporário restaurado; não modifica seus desbloqueios para um playtest manual.
3. Manter assistênciaTitanicConductor desligada e limpar apenas um override de seed que você mesmo tenha configurado. Para reprodução determinística, usar o campo de seed existente com0Manopla/2Arco/1Lança e restaurá-lo ao final; não é requisito para C05 abrir.
4. Entrar emPlay, aproximar-se da estaçãoFirstSector noNexus e usar o binding de interação mostrado pelo jogo. Não esperar entrada automática por umScenePortal inexistente nessa estação.
5. Completar um encontro normal. No ticket real, testar selecionar com mouse/setas/direcional, abrir detalhes e voltar, fechar mantendo pendente e reabrir, conferir categoria/ordinal, incorporar e verificar a build pela pausa. Não usar mostruário de arte como prova de RNG. Seguir salas abertas/cobertas até boss, extração eNexus.
6. Emcobertura, contornar pelos dois lados, observarEmissor perder/readquirirLOS, Fragmento navegar e projéteis/dash respeitarem sólidos. Exercitar básicos, especial eQ/E/R com os prompts/bindings atuais. Preservar seus saves e sair dePlay ao terminar.
7. Para repetir a seleção automatizada, usar TestRunner com as classes/filtros de `validation-selections-complete.json`, ou `python Tools/RunExp06/run_validation.py` com a ponteMCP existente. Não executar junto de um playtest manual. As fixturesdeclaram suas manipulações temporárias e restauram os escopos atuais; o script não restaura saves históricos.

## Custo e integridade

8/32/64 ativos medidos noEditor contra baseline06 e emstandalone atualWindowsrelease1920×1080, AI/nav/dano/poses ativos. O harness05 foi reutilizado sem duplicação, emproduto de perfil isolado. CPU/GPU/memória e indisponibilidades estão no06.4; diferenças entre instantes de AI não demonstram otimização. Sem orçamento universal ou alegação de VRAM medida.

2739hashes comparados, cincoCS/metas movidos comGUIDs preservados; cenas/prefabs/arte/definições/input/settings/fontfallback/fontesDCC iguais.47 preferências existentes de gameplay iguais,0novas/alteradas/removidas nesses escopos. Nada foi restaurado de snapshots04/05. O checker mantém um erro/umaviso históricos fora das cenas produtivas; Console final0erros semlimpeza manual. EditorNexusBlockout limpa/Edit/Time1, sem assistência, seed ou scopes pendentes.

Limitações mantidas:24IDs sem ilustração dedicada, cobertos por marca de família; uma imagem autorada fora do pool é preservada. Rig/clipsDCC eLOD finais não foram feitos. Outras máquinas, custo isolado deVFX/modal e técnica2.5D no jogo não foram testados. Aceite05 não é declaração de arte finalAAA; integração06 fica disponível para sua revisão, sem inventar aceite humano novo.

## Documentos e encerramento

[06.0Baseline](RUN_EXP_06_0_BASELINE.md) · [06.1UI](RUN_EXP_06_1_REWARD_UI_PRODUCTION.md) · [06.2Inimigos](RUN_EXP_06_2_ENEMIES_PRODUCTION.md) · [06.3Ambiente](RUN_EXP_06_3_ENVIRONMENT_PRODUCTION.md) · [06.4Validação](RUN_EXP_06_4_VALIDATION.md) · [06.5Direção](RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md) · [Brief07](RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md).

Masterplan, design técnico, UI, arte/pipeline, matriz, ledger, estado, requisitos e tarefas receberam estado06 acima do histórico. A instrução posterior do usuário autoriza commit e push **de todas as alterações**, inclusive as anteriores; desligamento somente depois de push confirmado e trabalho salvo, semforçar perda de dados. Ver `RUN_EXP_06_DELIVERY.md` para o registroGit final. STOP06: implementação07 não iniciada.
'''
(DOC/'RUN_EXP_06_RELATORIO.md').write_text(report,encoding='utf-8')

entries={
'Docs/gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md':('RUN-EXP-06 — publicação concluída e STOP antes07','C05 é normal nos tickets reais. Arte05 de inimigos/ambiente já estava instalada e foi consolidada sem duplicações. Aceite01–05 recebido no pedido06. Direção multiverso e brief2.5D são somente propostas; não iniciar07.','../decisions/RUN_EXP_06_RELATORIO.md'),
'Docs/gameplay/GPR_TECHNICAL_DESIGN.md':('RUN-EXP-06 — UI compartilhada sob owner nativo','RunRewardUI cria/reutiliza RewardSelectionView/RewardChoicePresentation/RewardIllustrationCatalog emUI/Rewards; cincoGUIDs preservados. RunBoons/Choose/ticket ePauseMenuUI mantêm autoridades. Sem dependênciaLab, RNG/pool/peso novo ou alteração de inimigo/gerador. Foco/soltura/detalhes/duploConfirm validados.','../decisions/RUN_EXP_06_4_VALIDATION.md'),
'Docs/systems/06_HUD_E_UI.md':('RUN-EXP-06 — C05 aprovada e publicada','O aceite direto01–05/C05 substitui o gate pendente histórico. Três alternativas quando disponíveis, foco/detalhes/contexto/rank, categoria/ordinal e cancelamento com ticket pendente. 32IDs alcançáveis:8 com7 imagens e24 com fallback de família; Continuação não promete aquisição. Mouse/teclado/gamepad/foco/remapeamento e1160×600/1280×800/1920×1080 verificados no escopo06.','../decisions/RUN_EXP_06_1_REWARD_UI_PRODUCTION.md'),
'Docs/art/01_DIRECAO_VISUAL.md':('RUN-EXP-06 — direção05 aceita; universos futuros exploratórios','Fragmento quadrúpede, Emissor de conchas e arquivoFirstSector05 foram aceitos para produção; acabamento final continua limitado. Arquivo Estratificado, Metrópole de Letreiros Recortados e Palimpsesto Corrompido são hipóteses originais de linguagem/layout, sem IDs/lore/ordem de campanha aprovados. Física/leitura/telegraphs comuns.','../decisions/RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md'),
'Docs/art/02_PIPELINE_DE_ASSETS.md':('RUN-EXP-06 — consolidar autoria; imagem antes da conversão futura','FontesBlender/FBX/UV/materials05 preservadas e refs produtivas confirmadas. Sem nova modelagem06.32IDs/24 ilustrações faltantes auditados. Futuro07: imagem original revisada antes de aplicar emfachada/cobertura, depois revisãoUnity emmovimento. Sem renderer/stylemanager ou conversão atual; ganho de autoria/desempenho precisa ser medido.','../decisions/RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md'),
'Docs/validation/01_MATRIZ_DE_VALIDACAO.md':('RUN-EXP-06 — gates técnicos executados e aceite05 recebido','A produção/C05 e catálogo verificados; C/D inimigos/LOS/duas rotas preservados; E nenhuma regressão bloqueante nova confirmada; F47prefs existentes e assets protegidos iguais no escopo; G planejamento/brief sem07. Seleções finais27/27,33/33,10/10,29/29adicionais e29/29Edit. Falhas intermediárias corrigidas e checkerhistórico permanecem registrados. Não é suite global nem playthrough humano.','../decisions/RUN_EXP_06_4_VALIDATION.md'),
'Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md':('RUN-EXP-06 — integração encerrada no escopo','06.0–06.4 concluídos tecnicamente; C05 publicada após aceite01–05.06.5 ebrief07 entregues somente como preparação. Regressões e preservação47prefs registradas, sem novosboons/bosses/arquétipos/economia/renderer. Commit/push integral e desligamento seguro autorizados posteriormente pelo usuário; registro de entrega separado.','RUN_EXP_06_RELATORIO.md'),
'Docs/02_ESTADO_E_LACUNAS.md':('RUN-EXP-06 — estado produtivo atual','C05 é padrão, não prévia pendente. Arte05 já presente foi mantida/validada. Faltam24 ilustrações dedicadas, rig/clipsDCC/LOD finais e validação de outras máquinas; fallback cobre o catálogo atual.2.5D está previsto e especificado, ainda não implementado. O gate humano05 foi cumprido pelo pedido06, não inferido dos testes.','decisions/RUN_EXP_06_RELATORIO.md'),
'Docs/gameplay/GPR_REQUIREMENTS.md':('RUN-EXP-06 — escopo e autorização','IntegrarC05 aprovada ao caminho real; consolidar o que já existe emFirstSector; validar três armas/boss/Nexus, input/tickets/catálogo/custo/integridade. Preparar três linguagens e umbrief07, sem implementar2.5D ou retunar jogo. Escopo técnico concluído, limites eGit autorizado no relatório.','../decisions/RUN_EXP_06_RELATORIO.md'),
'Docs/gameplay/GPR_TASKS.md':('RUN-EXP-06 — tarefas concluídas','06.0 baseline atual;06.1 publicaçãoC05 ecatálogo32IDs;06.2/3 consolidação sem duplicação;06.4 seleções finais/perfil/integridade;06.5 direção ebrief07 de duas peças. DONE no escopo descrito. NOT DONE: implementação07,24 ilustrações e acabamentoDCC final.','../decisions/RUN_EXP_06_RELATORIO.md'),
'Docs/gameplay/GPR_VALIDATION_PLAN.md':('RUN-EXP-06 — evidências atuais e limites','Rotas comNexusInteraction real e C05auto, três armas e seed aleatória; manipulações de fixture declaradas. MouseWindows real mais teclado/gamepad/foco/remapeamento automatizados, rank/contexto/fallback32IDs. Editor8/32/64 e standalone atualcomAI ativa; marcadores ausentes não são zero. Baseline06/47prefs, não snapshots antigos.','../decisions/RUN_EXP_06_4_VALIDATION.md')}
for name,(title,body,link) in entries.items():
    path=ROOT/name;text=path.read_text(encoding='utf-8-sig');begin='<!-- RUNEXP06 CURRENT -->';end='<!-- /RUNEXP06 CURRENT -->'
    if begin in text:
        a=text.index(begin);b=text.index(end,a)+len(end);text=text[:a]+text[b:]
    first,rest=text.split('\n',1)
    section=f'\n\n{begin}\n## {title}\n\n03/11/2026. {body}\n\nEstado06 prevalece sobre gates pendentes/restrições de fases anteriores abaixo, preservados como histórico. [Evidência e limites]({link}).\n{end}\n'
    path.write_text(first+section+rest,encoding='utf-8')
for name,old,new in [
('RUN_EXP_06_2_ENEMIES_PRODUCTION.md','Validação integrada posterior de ataques, reações, pausa/controle, mixed encounters, navegação, morte/cleanup e três armas será vinculada em','Validação integrada final de ataques, reações, pausa/controle, mixed encounters, navegação, morte/cleanup e três armas está registrada em'),
('RUN_EXP_06_3_ENVIRONMENT_PRODUCTION.md','A sequência final06 também verificará','A sequência final06 também verificou'),
('RUN_EXP_06_3_ENVIRONMENT_PRODUCTION.md','Resultados exatos e imagens de fluxo produtivo serão ligados em','Resultados exatos e imagens de fluxo produtivo estão em'),
('RUN_EXP_06_3_ENVIRONMENT_PRODUCTION.md','sem antecipar um resultado final ainda não observado','com resultados finais efetivamente executados')]:
    path=DOC/name;path.write_text(path.read_text(encoding='utf-8').replace(old,new),encoding='utf-8')
p=DOC/'RUN_EXP_06_0_BASELINE.md';s=p.read_text(encoding='utf-8');marker='> Encerramento06:'
if marker not in s:s=s.replace('\n\n', '\n\n> Encerramento06: a instrução posterior do usuário autorizou commit/push integral e desligamento após push confirmado/trabalho salvo. O estado inicial abaixo é histórico. Validação final em06.4 e entrega consolidada emRUN_EXP_06_RELATORIO.md.\n\n',1)
p.write_text(s,encoding='utf-8')
print('Reports06.4/consolidated and11 canonical current-state sections written; history preserved.')
