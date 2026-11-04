# RUN-EXP-08 — sala pictórica implementada, limitada e comparável

**DONE:** protótipo Unity isolado, uma sala e enquadramento de seus acessos, dois albedos originais, fachada fixa 2.5D e coberturas com faces pintadas, gameplay/oclusão/fluxo validados tecnicamente. **PENDING DEVELOPER APPROVAL:** resultado estético e sensação de fluxo. **NOT DONE:** conversão produtiva de FirstSector, novos universos/boons/inimigos. **NOT TESTED:** GPU/standalone08 e playtest humano.

## Abrir o resultado jogável

Na Unity do projeto D:, usar Tools/Tech Guy/RUN-EXP-08/Open isolated room, em Edit Mode, depois Play. A cópia usa seed 63 e começa no fluxo normal da rota; selecionar destino 1, seguir acesso, entrar no encontro. Não altera a FirstSector publicada nem precisa habilitar uma cena no build. A estação FirstSector do Nexus continua indo à produção normal. No Play, Tools/Tech Guy/RUN-EXP-08/Toggle baseline and painterly in Play alterna só as representações; não muda as regras físicas. Salvar trabalho próprio antes de trocar de cena.

Cena: Assets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity. O autorador Build é opcional para reconstrução das referências08 e exige cena atual limpa; não executar builders05/FirstSector para obter as imagens. O protótipo usa o loadout/save atual se você jogar manualmente; os testes automatizados isolaram/restauraram suas preferências correntes.

## Pacote e recomendação

[Revisão offline](../validation/artifacts/run-exp-08/review/index.html) · [ZIP](../validation/artifacts/run-exp-08/RUN_EXP_08_REVIEW.zip). Imagens de Unity, conceito, antes/depois, porta corrigida, três armas, tripla, custo bruto e relatórios estão incluídos. A recomendação é continuar lapidando **essa mesma sala**, após sua avaliação. O manto pictórico e o pórtico provam um caminho de autoria viável, sem aprovar conversão global. A planta física é a mesma e isso fica explícito no pacote.

## Estado auditado, arquivos e sistemas

Projeto real D:/Dev/Personal/My_games/Tech-Guy, branch gameplayOverhaul, HEAD 766afa66d93a58afc1b9de1f2eafa15ed8a5504b, Unity 6000.5.10f1, URP 17.5, espaço Linear/Ultra. Cena inicial NexusBlockout limpa/Edit. Alterações/documentos07 anteriores foram preservados. Nenhum relatório07B foi encontrado em Docs/.agents/Tools; a referência pictórica verificável é C07, aprovada descritivamente pelo pedido08. Não se inventou uma referência07B.

AGENTS/README, skills locais/referências, visão, arte/pipeline, HUD, inimigos, progressão, masterplan, ledger/matriz,05/06/07 e plano2.5D foram consultados nesta conversa; nesta etapa foram relidos os contratos locais, seções atuais e o sistema real de conexão. Fonte: ManualStageEnvironment.BuildGenerated, ProceduralRunGeometryPlan, ApplyGateState/EncounterGates, FirstSectorEnvironmentArt/RoomPresentation, FirstSectorContentDefinition, TG_TopDown_Camera, CameraOcclusionFader, testes produtivos05/06 eGpr015. Não há AGENTS aninhado em Assets/_Project descoberto.

## Implementação e arquivos criados

Assets/_Project/Scripts/Prototypes/PainterlyRoomPrototype.cs é uma apresentação referenciada somente pela cena nova; espera o ambiente nativo pronto, decora nó 1 e seus acessos, oferece toggle A/B e limpa meshes/objetos próprios. RunExp08Authoring.cs cria/reabre somente a cópia Assets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity e materiais novos. Seed63 fica no RunSeedSource da cópia, sem mexer na cena FirstSector, BuildSettings, pacote ou PlayerPrefs. Fonte de gameplay não foi modificada.

Arte nova em Assets/_Project/Art/RunExp08: PigmentStrata.png, QuietSlate.png e cinco materiais MineralPigment/QuietSlate/ForwardIvory/ClosedInk/CoverTop, com metas criadas pela Unity. Métodos de teste novos nas partials RunExp03FirstSectorTests.Painterly08.cs e Painterly08Evidence.cs. Tools/RunExp08 guarda snapshot, pacote e QA. Documentos mestres/arte recebem bloco08 acima do histórico07. Fontes07, modelos05 e pacote07 não foram sobrescritos. Sem commit/push.

## Skills aplicadas e ferramentas

| Skill realmente lida/aplicada | Decisão influenciada |
|---|---|
| tech-guy-game-vision + referências | DireçãoC selecionada pelo pedido08 prevalece sobre recomendaçãoA; identidade, C05 e armas preservadas |
| tech-guy-combat-unity + combat-matrix | Física/LOS/dano e boons permanecem nos owners existentes; nenhuma mecânicaBurn criada |
| tech-guy-unity-acceptance + editor-validation | Evidência MCP/UTF atual, scopes de prefs, captura real e separação entre teste e aceite humano |
| router / unity-mcp-orchestrator | ProjetoD: e instância6000.5 confirmados, resources-first e compilação antes de executar |
| unity-csharp-scripting | Componente local com referências serializadas, lifecycle/cleanup e cena isolada reaberta |
| create-game-assets + art-direction/raster/3D/provenance / imagegen | Dois albedos originais, prompts preservados, UV por face, piso quieto e diferenciação de conceito/runtime |
| tech-guy-world-identity + world-card | Pinceladas minerais e silhueta periférica próprias; nenhum novo universo ou lore |
| level-design / tech-guy-level-layout + level-brief | Mesmas métricas de combate, corredor4 m existente, pórtico aberto no centro, sem tempo extra de caminhada |
| camera-systems | Corrigir oclusão do lintel; renderers sob colliders físicos, câmera/follow intactos |
| shader-programming | Reutilizar shader Illustrated Toon com opacity dither e outline0; sem shader ou feature global novos |
| performance-optimization | ABBA com AI ativa e métricas brutas, Editor separado de standalone, sem inferir ganho deFPS |

Descobertas: quatro skills locais em .agents; boon-design não aplicada pois catálogo está fora do escopo. Instaladas acima já lidas nesta conversa e referências pertinentes consultadas; não se instalaram pacotes/skills. Não existe skill local dedicada a Blender/URP2.5D. BlenderMCP consultado08 sem conexão: **BLOCKED** paraDCC, não usado; malhas de revisão foram autoradas emC#/Unity. ImageGen integrado executado duas vezes; não forneceu modelo/seed nem camadas editáveis. PNG é a fonte raster; prompts/C#/materiais são fontes editáveis reais, sem PSD/blend fictício.


## Testes executados e resultados

Seleção anterior ao ajuste final de sombras, PlayMode job e5a04d55c2a54260b5bb9c165eca7d52: **33/33 Passed**,0 falhas/skipped,98,3223341s. Inclui26 casos da classeCameraOcclusionFadeTests, cinco casos 08 (3 armas, custo e tripla), básico/cover nativo das três armas e rota normal aleatória06. Não é suíte global. Custo repetido isoladamente após o checker sair, job e0560bcf55ec4c21b032fc65e793852b:1/1 Passed,12,0665082s, mesmo caso já contado nos33.

História preservada: primeiro discovery441bbb... encontrou0 testes (não contar comoPASS de cenário), corrigido por import/compilação do arquivo. PrimeiroGauntlet3f605... passou1/1. Seleção9d9bd... passou30/31, comBow parado emGameplayInputReady; fixture passou a isolar/restaurar dispositivos comPublishedInput06 existente. Não se alterou controle para fazer teste passar. Asserções deReady, casts e passagem continuam. Jobs JSON brutos emevidence.

Fixture três armas: roster/AI/nav/dano nativos ativos, jogador imune; mana/Asura de teste, warps para pontos de cenário e morte letal automatizada ao concluir declarados. C05 usa ticket real eChoose legal. A transição5 m é percorrida pelo agente real após o gate nativo abrir; warp posterior para próximo nó é explícito. A rota normal06 até boss/Nexus também é automatizada, não sobrevivência humana. Tripla usa grants em memória e AI desligada para composição controlada, HP de um alvo reduzido para detonation, executor e recibos HP nativos; **não** comprova obtenção natural. Nenhum Burn/DoT novo.

## Evidência antes/depois e regressões

CapturasUnityGameView reais emreview/gameplay/weapon-0,1,2:08-baseline-room/08-painterly-room na mesma posição e frame congelado; door-transition; C05; next-room; native-skill0..3; painted-cover-occlusion; telegraphs e LOS. Metadata de câmera do frame emcamera-and-room.json, desta vez amostrada efetivamente. Antes/depois não usa imagem gerada como screenshot. ConceitoC separado emimages/approved-C-concept.png; material source emmaterials; fontes/prompts e proveniência rastreadas.

Regressões encontradas e corrigidas: lintel contínuo escondia o jogador na transição; removido vão superior central e piers associados aCollider de parede parafade. UVs do atlas05 esticavam a nova textura; cobertura visual recebeu domínio por face. Pintura no topo gerava ruído/achatamento; topo escuro próprio. Bloqueio de input na fixtureBow resolvido com isolamento corrente de dispositivos, sem tuning. Itens anteriores foram preservados para comparação. Após retirar sombras decorativas duplicadas e desativar o passe Ink nos materiais08 com outline zero, a revisão final passou **31/31**, job ae31e105586849429415febfb5411713, 75,8859632s. Inclui os cinco casos 08 e26 de câmera. Os dois casos adicionais de produção da seleção33 anterior continuam sobre os mesmos arquivos protegidos; não somar reexecuções como testes novos. Sem nova regressão bloqueante nessas seleções.

## Custo observado e limitações

| Versão | Amostras | Quadro mediana/p95 ms | Main Thread mediana/p95 ms | Draw calls mediana | Memória alocada MiB |
|---|---|---|---|---|---|
| Baseline3D | 747 | 5.158/7.334 | 5.175/7.370 | 366 | 1169.40 |
| Protótipo pictórico | 726 | 5.109/6.740 | 5.092/6.737 | 418 | 1169.42 |


Última execução de custo da revisão final: ABBA,2s porbloco após0,5s de acomodação; mesmo mapa/câmera/roster/qualidade, AI/nav/ataques ativos e jogador imune. Posições/fases evoluem, Editor e coletor acrescentam custo; os números são observação local, não prova de otimização emquadros idênticos. GetComponentsInChildren/UTF e coletaCSV têm overhead. Condições/hardware emperformance/conditions.txt, brutos em frames.csv. GPU/VRAM e standalone08 **NOT TESTED**; não derivarGPU deFPS. Não existe metaFPS universal aprovada.

InspectorUnity: PigmentStrata2048×1024, BC7,12 mips; QuietSlate1024², BC7,11 mips; Read/Writefalse. GetRuntimeMemorySizeLong observou5.593.408 e2.797.184 bytes, respectivamente (custo Unity reportado, nãoVRAM). ShaderIllustratedToon atual é opaco/dither; passe Ink desativado nos materiais08 e sombras desativadas somente nas peças decorativas duplicadas. As massas físicas visuais principais continuam projetando sombra. A mediana de drawcalls do protótipo caiu de 587 para 418 entre iterações; baseline 366. Isso reduz custo desnecessário sem alegar vitória deFPS. O shader, sem alpha margins ou transparências emcamadas; materiais de cobertura com dois slots aumentam custo potencial. Sem normais novas, luzes adicionais, névoa ou bloom global.

## Integridade, pendências e aprovação

**DONE:**2368/2368 arquivos preexistentes dos escoposAssets/_Project/ProjectSettings/Packages/SourceArt idênticos;25 arquivos novos Unity,0 removidos;47/47 prefsTechGuy idênticas,0 adições/remoções. JSON antes/depois emevidence. Cena nova salva/reaberta:MCP encontrou0 scripts ausentes e6 referências novas válidas. Prefs restauradas pelos scopes correntes dos testes, sem snapshot antigo. Nenhuma substituição deC05, input, boons, combate, spawns, física ou gerador.

Checker genérico da skillC# **BLOCKED**: ValueError emTripo externo noC: com projeto D:, antes de concluir a análise. Não é um resultado verde; alternativas: compilação/Console/InspectorUnity, igualdade de hashes antigos e GUID/metas novos. Blender desconectado; fonteDCC **NOT DONE**. HTMLvalidado estaticamente; inspeção emnavegador **NOT TESTED**, pois a ferramenta desta conversa bloqueou file: e não houve contorno. PNGs deoverview/porta/tripla inspecionados diretamente.

**PENDING DEVELOPER APPROVAL:** beleza, singularidade, leitura física e sensação de fluxo na sala implementada. O retângulo navegável28×24 m foi preservado deliberadamente; a singularidade implementada é de silhueta/material/enquadramento, não uma topologia física nova. Não se promete que todos os universos usarão a mesma câmera. Próximo passo: revisar essa mesma sala na cópia jogável e indicar no máximo duas correções concretas; sem conteúdo novo, conversão global ou boonrework. **STOP após08.**

Contagens ativas de Actor no custo: baseline [6, 7, 8], protótipo [6, 7]. A variação inclui estado vivo/produzido pelo roster nativo; por isso a tabela não isola o efeito da arte sobre CPU ou FPS. Comparações de drawcalls e tempos devem ser lidas com essa ressalva.
