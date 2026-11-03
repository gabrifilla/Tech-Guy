# RUN-EXP-06.4 — integração, regressões, custo e integridade

03/11/2026, America/Sao_Paulo. **Concluído no escopo06.** Projeto realD:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1/MCP Tech-Guy@fccafb2b47cd7671; mesma branchgameplayOverhaul, sem descarte do trabalho02–05. O pedido06 aprova a direção/gameplay/UI01–05 e autoriza publicaçãoC05. A instrução posterior do usuário também autoriza commit/push de todas as alterações e desligamento somente após salvar e confirmar o push. O registroGit de entrega é separado dos resultados abaixo.

## Seleções finais executadas nesta etapa

| Seleção | Job | Resultado | Duração |
|---|---|---|---|
| FirstSector + UI produtiva e catálogo | `d775c4fd01864708999ab03077facbb4` | 27/27, falhas0, skipped0 | 286.705s |
| Regressões de combate/input/boons | `ae7e05f0965342e4b9cb3eb2a4dc703a` | 33/33, falhas0, skipped0 | 43.775s |
| HP/atribuição e efeitos de habilidades | `258b26f015304b1699935dc316556811` | 10/10, falhas0, skipped0 | 11.171s |
| Básicos/combos e câmera/oclusão | `8901f2dd89e743258749252a41d40513` | 29/29, falhas0, skipped0 | 3.373s |
| Conteúdo/geometria/encontros/arte | `111fc22c5f2845729644ab2edfe1c47f` | 29/29, falhas0, skipped0 | 1.598s |


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

| Ativos | Frame p95 baseline06 → atual ms | BehaviourUpdate atual p95 ms | Nav p95 ms | Pose p95 ms | Skinning.Update p95 ms | GC p95 bytes |
|---|---|---|---|---|---|---|
| 8 | 6.66 → 4.99 | 0.971 | 0.109 | 0.063 | 0.065 | 397920 |
| 32 | 10.22 → 6.76 | 1.080 | 0.220 | 0.217 | 0.114 | 609260 |
| 64 | 14.97 → 12.38 | 2.057 | 0.361 | 0.486 | 0.188 | 1046568 |


| Ativos | Renderers rígidos | Triângulos rígidos | Skinned/triângulos | Drawcalls globaisEditor p95 | Spawn / cleanup decorrido ms |
|---|---|---|---|---|---|
| 8 | 75 | 23832 | 14/54944 | 505 | 5.72 / 423.25 |
| 32 | 275 | 87384 | 70/274720 | 1570 | 16.37 / 789.19 |
| 64 | 543 | 172596 | 147/576912 | 3173 | 35.04 / 448.69 |


Mesma fixture nativa de população1:1:1 Fragmento/Emissor/Grunt pelo diretor,1s de aquecimento e5s de amostragem. AI/nav/dano/poses ativos; jogador imune e ocioso, moedas suprimidas somente na fixture. BehaviourUpdate é agregado; GC incluiEditor/UTF/LINQ/IMGUI. Drawcalls são globais doEditor, não da câmera do jogador; batches indisponíveis não são zero. Skinning.Update é agregado dos atores que usam esse caminho, não rig novo dos candidatos. Cleanup inclui espera deliberada0,4s, não CPU deDestroy. Renderers/triângulos rígidos e skinned permaneceram iguais ao baseline06. Frame/GPU/modalC05 e cada VFX não foram isolados aqui.

A diferença de frame não comprova otimização: horário, fases de AI, carga doEditor, posição e callbacks variam. Não houve mudança de geometria/AI de inimigos06. O baseline já contém a arte05; não confundir com geometria04. Os testes de vento/fogo/eletricidade/teto de procs são regressão funcional separada, não benchmarks de todas as builds.

## Standalone atual realmente executado

BuildWindows64 releaseMono2x: 202093971bytes, 34.890s,0erros/9avisos. `standalone-build.json` preserva mensagens: umCS0108ScenePortal, seis APIs de busca deprecadas no harness05 e dois avisos de transporteMCP. São dívida/avisos preservados, não build semwarnings.

Reutilizado o harness05 **sem alterar sua fonte/cenas**, compilado contra o runtime atual06. Exige `--runexp05-profile`; labels04/05 significam variantes de arte no mesmo executável, não builds históricos antes/depois da UI. Produto isoladoTechGuyReviewLocal/RUNEXP06Profile, conferido emapp.info; company/product/frameTiming do projeto restaurados emfinally. Nenhuma segundaUnity foi aberta no checkout. CaminhoBuilds/RunExp06Profile, ignorado porGit.

HardwareAMD Ryzen 5 5600X 6-Core Processor  / NVIDIA GeForce RTX 3060 Ti. Resolução efetiva1920×1080, conferida nos relatórios, embora os argumentos solicitassem1280×800; não apresentar a resoluçãoCLI como a medida real. Vsync0/framecap-1. Cada par mantém a mesma coorte8/32/64,1,5s de aquecimento e4s de amostragem por variante, ordem invertida em32. AI/ataques continuam evoluindo, portanto posições/windups não são os mesmos instantes. Todas as amostras conservaram a população ativa; impactos reais foram registrados. O aplicativo terminou sozinho após seisJSONs/PNGs ecomplete.txt.

| Ativos | Quadros arte04/05 | Frame p95 arte04 → arte05 ms | MainThread05 p95 ms | GPU p95 arte04 → arte05 ms | GPU válidos04/05 | Memória alocada05 p95 MiB |
|---|---|---|---|---|---|---|
| 8 | 2805/2821 | 1.81 → 1.78 | 1.78 | 0.77 → 0.79 | 2355/2268 | 265.59 |
| 32 | 1251/1281 | 4.11 → 4.03 | 4.03 | 1.07 → 1.10 | 970/992 | 270.67 |
| 64 | 701/788 | 7.31 → 7.68 | 7.63 | 2.68 → 2.67 | 653/704 | 275.66 |


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
