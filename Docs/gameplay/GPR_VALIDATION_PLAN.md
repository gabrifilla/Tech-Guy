# Gameplay Rework — plano de validação

27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](../decisions/GPR-C01.3a_RELATORIO.md).

## Evidência C01.3a —102 casos distintos

19 novos PlayMode: clique/hold/whiff/0-1-2/contact/dedupe/aim/root/buffer/expiração/cancel/swap/UI/foco/hitstop, Bow sem/com ChargedShot, Spear, RMB Asura custo único/sem básico, UI/pausa, Cadenciada em3 presses e finisher prioritário. Mais23 integração+7 contextos+30 GI/legado/Bow/Spear/Asura+8 dash+15 EditMode. IDs/filtros/durações no relatório e artifacts/gameplay-rework/c01-3a/*.request.json; todos102 passaram, sem ignorados. Não suíte global.

Para preservar artefatos históricos, SessionState TechGuy.Tests.GprPhase=c01-3a durante os filtros compartilhados; remover no fim. Scope virtual isola/restaura dispositivos e políticas Input System. Cliques de fixture duram25ms reais; usar clock real para input durante hitstop, não WaitForSeconds escalado. Intermediários com falhas de fixture/compilação estão preservados; não são verdes.

Driver c01_3a_nexus_driver.cs.txt em Play/Nexus usa player publicado, mouse/foco virtuais e energia100 concedida somente em memória para Asura. Resultado observado:1372 amostras/3 básicos0-1-2/1 Asura, X/Z constantes;4 capturas abertas. Não equivale a humano físico. Critérios físicos e expansão3b continuam sujeitos a revisão. Incidente histórico recovery/TTL sob hitch não foi declarado corrigido por esta bateria.

## Evidência C01.2c e reprodução

Bateria final83/83 normais:23 integração,7 UI/contextos,30 GI/legado/Bow/Spear,8 dash e15 EditMode. Filtros, resultados e IDs no [relatório](../decisions/GPR-C01.2c_RELATORIO.md) e em artifacts/gameplay-rework/c01-2c/*.request.json. Executar cada request com `python Tools/GameplayObservation/c01_2c_validation.py <request> <output>`, MCP conectado ao checkout D: e Editor correto; não executar jobs simultâneos.

Diagnóstico recovery preserva o teste original. Editor SessionState `TechGuy.C01.2c.RecoveryHitchMs` (0 normal,150 ou500 controlado) e `TechGuy.C01.2c.RecoveryRun` rotulam CSV.150 passa;500 reproduz falha da premissa de validade após TTL, não conta como verde; reset obrigatório0 antes de gameplay. Novo caso negativo verifica descarte legítimo da intenção vencida. Histórico intermitente não declarado corrigido.

Reproduzir NexusBlockout em Play Mode com c01_2c_nexus_driver.cs.txt via execute_code: WASD/mouse/foco virtuais,4,5s e757 amostras observadas nesta revisão, speed5; UI screenshots de pausa/results complementadas com Game View focada. Nunca descrever isso como controle humano físico. Próxima revisão física: paredes/quinas/bordas, retomada com teclas mantidas, walking/facing e direção do dash. Crowd/overlap/obstáculos móveis/todos os mapas e orçamento de build continuam pendentes.

Atualização vigente27/10/2026: C01.2a aceita; C01.2b e teclas necessárias implementadas com autorização direta. [Relatório/limites](../decisions/GPR-C01.2b_RELATORIO.md).66/66 casos finais e smoke virtual na Nexus; sem aceite físico/global. Próximo recorte proposto C01.2c; demais fases não iniciadas.

### Protocolo da integração C01.2b

Executar os13 métodos GPRDirect de DirectMovementIntegrationPlayModeTests na assembly PlayMode existente, preservando as asserções de contato/dano e preparando foco/teclado/mouse **virtuais** explicitamente. Validar migração pura, conflito/round-trip sem alterar legado, cardinal/diagonal/parada/mira, root/cast três armas, Kiting Step, parede não baked, pull/dash/hitstop/posse e sincronização de contato. Regressões:21 baseline+5GI+4Bow/Spear,8DashCancelResolver e15AsuraFinishControl EditMode. Filtros exatos nos resultados c01-2b.

O primeiro grupo legado teve29/30, com falha do buffer. Sem mudanças de código/tolerância, isolado1/1 e grupo final30/30 passaram; repetir no próximo QA sob carga, sem afirmar causa ou resolução definitiva. Não contar reruns como casos adicionais. Checker global de metas falhou novamente pelo pacote em C: e projeto em D:; guardar stderr, conferir novos GUIDs/importação sem apresentar como PASS global.

A ativação nas4 cenas habilitadas deve manter somente `_directKeyboardMovement:1` no diff de cada cena, sem rebuild. Builders apenas preservam o modo em geração futura. Reabrir/importar cenas, consultar componentes e restaurar setup/seleção. Conferir1789 hashes protegidos:22 mudanças intencionais,1767 iguais,0 ausências; dados persistentes antes/depois iguais.

Para reprodução visual usar c01_2b_nexus_driver.cs.txt em Play Mode na Nexus real. Mede2,5s/461 amostras com input/foco virtuais, sem recursos injetados; deve remover dispositivos/delegates e sair de Play Mode. Capturas reais abertas; na borda da plataforma Origem Terra, diagonal parou. Não comparar esse trecho bloqueado como diagonal livre; a fixture demonstrou normalização. Sem humano físico, crowd, plataformas/escadas/OffMeshLinks ou benchmark de build.

Os contextos completos de menu/reward/results, remap WASD/segundários e separação Interact ficam nos recortes seguintes. LMB/RMB e X não foram implementados. Não presumir GO físico a partir do aceite da ideia do protótipo.

### Protocolo reproduzível do experimento C01.2a

1. Confirmar instância/projeto, Edit Mode e ausência de jogo humano/cena suja. Importar assembly `TechGuy.Tests.GprMovement` com force refresh quando os arquivos novos ainda não constarem no AssetDatabase; **zero casos não valida nada**.
2. Rodar PlayMode filtrando essa assembly: dez testes, arena/transient NavMesh novos por caso, limpeza em UnityTearDown. CSVs registram dt pedido, posição final, velocidade planar, agent.velocity, owner e custo Step. Comparar cardinal/diagonal, câmera yaw90/mundo, parada, paredes baked/unbaked, canto/escape, obstáculo/borda, slope20°, clocks/gates, dash cursor e alternativa WASD. CC é testado com agente/motor desativados.
3. O teste de Keyboard/Mouse usa dispositivos virtuais e o leitor de preview; marca virtual/API nos PNGs, remove dispositivos e restaura os anteriores. Nunca preencher aceite físico. Capturas Game View são abertas/inspecionadas. Passos 30/60/144 Hz sintéticos não são benchmark de FPS.
4. Menu `Tech-Guy/Experiments/C01.2a/Open isolated WASD arena` abre cena não salva e restaura a anterior ao sair de Play Mode. WASD/mouse/Space(cursor)/Tab(câmera-mundo) são controles **só da fixture**, sem GamePreferences/Settings. Não anexar a prefab/scene publicado.
5. Regressões focadas: 21 casos baseline +5 GI de impacto/causa/cadência/hitstop,8 DashCancelResolver e15 AsuraFinishControl EditMode, com evidências JSON no relatório. Preservar falhas de iteração e warnings. Executar `python Tools/GameplayObservation/c01_2a_analyze.py`; checar hashes/prefs/summary e Editor restaurado.

Pendente após revisão: mapa real/portas/crowd/overlap inicial/plataformas/escadas, rig/animadores reais, adaptação dos produtores de deslocamento e consumidores de agent.velocity. Recolher físico com câmera/mundo e cursor/WASD dash sem adotar default pela simulação. Asura/X/Bow-Spear continuam abertos, sem bloquear a prova do motor. Checker global Python incompatível com pacote em C: e projeto em D:; usar evidência focada de importação/GUIDs sem rotular checker como PASS.

| ID | Verificação futura | Evidência e gate |
|---|---|---|
| V00 | Git/diff/hashes incluindo untracked próprios, .meta/GUID/exclusões; prefs/loadout/moedas/unlocks/summary antes/depois; Editor limpa e restaurado | JSON e status; 0 alteração fora de escopo, nunca restaurar save humano posterior |
| V01 | 8 direções/cancelamento de opostos/soltura, mesma distância cardinal/diagonal;30/60/120fps controlados; paredes/cantos/NavMesh borda/desnível/obstáculo/sem câmera | Deslocamento por tempo e vídeo; medir velocidade base antes; tolerância numérica proposta a partir da repetição, sem buff |
| V02 | Andar em sentido oposto à mira; cursor no HUD/fora da janela/sem chão; ataque parado/em movimento; trocar alvo após commit | Vídeo+timeline aim/posição/contato; nenhum homing de swing aceito |
| V03 | CliqueLMB→um pedido, busy/recovery/press rápido/hold sem repetição automática default; RMB sóespecial, simultâneo com básico e sem recurso | Sem combo automático, básico por RMB ou duplicatecost; hold legado só como política explicitamente aprovada |
| V04 | Quatro slots modularesQERX independentes de especial; Asura RMB cheia/vazia/Cadenciada/F; único estado/cooldown e fonte históricaR; X vazio ou conteúdo aprovado | Testes API/virtual/físico separados; Array.IndexOf não pode retornar Basic por extração nem duplicar Skill4/Asura |
| V05 | Aproximar arsenal/terminal/portal, treino e apresentação compacta; G só interage, R só skill3, F só finaliza quando permitido | NexusLobby e NexusBlockout existentes, sem rebuild; prompts usam binding atual |
| V06 | Rebind 4 partes Move, primário/alternativo, Mouse0–4, inválido/reservado/duplicado, colisão com movimento, cancel/capture frame/reset; prefs ausente/corrompida/custom F11/legacy W | EditMode migração pura + UI/input simulado + físico; persistência Stop/Play, scene reload e processo/build separado |
| V07 | Pausa/settings/reward/results/detalhe/mapa/rebind/loss of focus, pressionar/soltar durante overlay, fechar segurando teclas/botões | Sem movimento/ataque/custo oculto ou click-through; Esc/back/foco sempre alcançável |
| V08 | Básico/skills/dash antes/depois do contato, hitstop, pausa, CC/pull, morte/disable/troca; projétil já emitido | Timeline startup/active/recovery, ImpactEvent/HP/pose; comparar baseline sem alterar janelas |
| V09 | Base/A em Common/Heavy/Elite/Boss; terminal confirmado/whiff/imune/LOS/geração reciclada/cancel reentrante; alvo lateral e alvo já tocado | Só confirmação válida cria uma onda, sem dupla vítima e sem Stagger universal |
| V10 | A/B/A+B e B+A; budgets saturados, filho negado, nova raiz mesmo frame, descendente com depth cap; repetição/pooling | HP/postura/recurso não mudam por filho suprimido; total/queries/fila/VFX dentro de limites efetivos |
| V11 | Swap seguro/ocupado/cooldown/com dependente incompatível, ticket stale/replay, falha injetada antes/depois commit/notificação | Estado anterior restaurado ou Faulted explícito sem retry; outros3 timers iguais; não cobrar mana pela equipagem |
| V12 | Base/A/A+B/reset em720p/1080p com mesmo rig/câmera/seed; attachments/materials em troca/morte/disable/pooling/2ª run | PNG reais abertos + clipe de contato, perigo hostil/HUD visíveis; sem resíduos/instâncias crescendo |
| V13 | Simular seeds com tickets atuais e todos ordinais; reproduzir caminho real de oportunidade; Mutation→Elite/boss pelo gate vigente | Simulação não prova percurso natural; não matar por API para alegar gameplay natural |
| V14 | HP efetivo/overkill/immune e causalidade Basic→onda→Conductor; snapshot QWER histórico com QERX físico; duas runs e resultados | Partições reconciliadas ao total, 0 receipts duplicados; preferência summary preservada por fixtures |
| V15 | Gauntlet/Bow/Spear, reação/resistência/energia/cancel/summary/projétil/rotas | Unity Test Framework por filtros; falhas históricas isoladas e conjunto, não alegar suite global verde |
| V16 | Sessão humana8–12min: andar/mirar em sentidos opostos, combo LMB/RMB, esquiva, QERX, Nexus, reward, remap, restart; depois base/A/A+B/swap | Comentários físicos “tentei/esperei/ocorreu”, clipe/timecode, APROVAR/REFAZER/PENDENTE por feel/visual; nenhuma resposta preenchida pelo agente |

## Filtros existentes e cobertura nova

Reusar AsuraFinishControlTests (identidade/binding), BasicAttackR1PlayModeTests (selecionar casos GI de contato/cancel/dedupe/movimento), BasicAttackCursorFluidityTests, BasicAttackR1MovementFluidityTests, DashCancelResolverPlayModeTests, CancelNoDuplicationPlayModeTests, AsuraEarlyFinishPlayModeTests, CombatResponsivenessPlayModeTests, GauntletExpansionPlayModeTests, ProtocolRuntimeDamageTests, ProtocolPublishedCompositionTests, EnemyProjectileLifecyclePlayModeTests, SniperAimCommitmentPlayModeTests, RunStatisticsPlayModeTests, RunSummaryIsolationPlayModeTests, RunResultsModelTests e testes de tickets/compatibilidade/reward. Nomes de **arquivos** não garantem nome de classe: listar/discover antes de compor filtro; GauntletBasicImpactPlayModeTests.cs é partial BasicAttackR1PlayModeTests.

Novos casos deverão cobrir vetor/câmera/inputs/contextos/migração e swap seletivo/onda/filhos; testes comportamentais que falham na implementação anterior quando possível. Antes de rodar fixtures que finalizam run, inspecionar isolamento de PlayerPrefs/RunSummary; execução pode alterar dado humano mesmo sem editar script. Nunca enfraquecer assertions ou restaurar conteúdo aposentado para obter verde.

## Baseline e capturas — planejamento histórico de GPR-00/C01.1

Este parágrafo descrevia o gate antes de C01.1:15 EditMode +4 PlayMode, sem baseline visual/físico novo. A baseline virtual/visual C01.1 posterior está em GPR_C01_BASELINE.md; as quatro capturas do protótipo autorizado C01.2a estão no relatório atual. GI1.4/1.8 não são reobservação atual por menção histórica. V16 físico continua pendente; nenhum PNG de fixture equivale a aceite físico de controle ou rig.

Usar Tools/GameplayObservation/observe_gameplay.py e cenários existentes, após discovery/readiness e inspeção dos efeitos de restauração; seeded para comparação controlada, current para sessão física preparada pelo desenvolvedor. Conferir que o teardown não sobrescreve RunSummary humano. Registrar requested/effective seed, arma, nó/roster, câmera, resolução, cadência, fase, hashes/config e assistência; abrir PNGs realmente e comparar timelines por frame/tempo, não apenas totals. No máximo clipes representativos20s e imagens base/A/A+B/reset; sem coleta massiva.

## Dívida histórica

Mutation/boon fixtures aposentadas, release/hold sensível e BurnTick saturado têm falhas anteriores documentadas. GPR-00 reexecutou somente filtro de finalização e quatro básicos. Reprodutibilidade atual **não aferida** desses outros casos. Antes de alterar seu owner, executar cada caso afetado isolado e no grupo, registrar erro/assert/catálogo/tempo e distinguir regressão nova. Incidente ranged sem HP da GI1.9 não está reproduzido; não atribuir a Hazard/LobShot sem evidência. Se bloquear aceite, abrir tarefa própria por aprovação.

## Baseline novaC01.1a e gates revisados

[Baseline atual](GPR_C01_BASELINE.md):21 PlayMode+15 EditMode executados, input virtual e capturas reais de Nexus/dummy/Asura/UI. Supera a pendência visual de GPR-00 somente nesses cenários; humano físico, encontro hostil/benchmark e motor WASD continuam pendentes. Não inferir regressão verde global ou prontidão do Agent.Move por mediana5 m/s de path legado.

LMB clique/RMB especial são decisões aprovadas posteriores ao planoWASD; terminalRMB/fatores1,25/0,8 e aliasBow/Spear estão cancelados. Piloto em memória/EditMode da migração deve exercitar overrides legados F11, defaults, teclas de movimento conflitantes e ausência/corrupção, sem aplicar no perfil humano. Só depois de decisão e novo gate pode substituir backend. Ver ADRD15–18 e Interact pendentes.

## Gates

GPR-00: design entregue, STOP. GPR-C01: V00–08,14–16 e aprovação de controle/feel; sem mutações. GPR-01: V00,08–16 com base/A/A+B/swap e dois ciclos; humano aceita mecânica/visual antes de expansão. Build/processo reiniciado, gamepad, performance de swarm64 e todas as Mutations não estão cobertos por19 testes focados.
