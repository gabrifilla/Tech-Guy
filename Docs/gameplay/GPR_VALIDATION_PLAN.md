# Gameplay Rework — plano de validação

## Estado vigente — auditoria GPR-01.5, 29/10/2026

**GPR-01.5 concluída como auditoria, com ressalva de estabilidade e aceite físico pendente. STOP após01.5.** O desenvolvedor aceita01.4 como implementada/demonstrada no foco documentado; não aceitou fisicamente as novas camadas fogo/eletricidade ou a tripla. Gameplay01.3 já foi aceito. O histórico199/203 +4/4 isolados de01.4 não foi reclassificado como203/203.

A+B+C foi obtida por tickets reais, sem grants de boons, nos quatro percursos publicados deFirstSector, com assistênciaA+B desligada/restaurada no scope dos testes. Combate/conclusão dos encontros foram automatizados, não humanos. A rota atual usa geração e tem umaMutation e2–7 oportunidadesProtocol na amostra; a antiga conclusão de uma única oportunidade limitava-se à fixture. Nenhum peso/frequência/gate/sala/economia foi alterado.

Modelo determinístico64 seeds×4 caminhos×3 políticas, sorteio/commit de produção:ABC253/256 priorizandoB,252/256 priorizandoC,42/256 escolhendo uniformemente entre cards sorteados. Não são probabilidades universais nem garantia de sobrevivência/diversão. Quatro comparadores reais na câmera normal, com vários inimigos/telegraphs, mostram vento/fogo/eletricidade e feedback causal separado; leitura física continua pendente.

Validação desta atividade:98/98 focados;3/3 EditMode de amostra;8/8 produção (4rotas+4visuais);5/5 correções de ambiente/probe;10/10 checks finais;90/90 EditMode final. Regressão ampliada **203/204**: caso de pausa/recovery falhou emIsCasting após0,4s reais de pausa, passando inalterado no diagnóstico posterior. Causa ainda não confirmada, risco temporal aberto. Não declarar a bateria totalmente verde ouGPR-01 encerrada.

Somente testes/documentação mudaram: diretórios de evidência criados pelos writers; helperAsura exige recovery ainda estacionária, porque a espera móvel também reportaRecovery/progress1 e era admitida antes. Probe reproduziu a premissa incorreta; não prova retrospectivamente toda falha histórica. Nenhum timer/TTL/buffer/cadência/animação ou owner de produção alterado. Medições8/32/64 mantêm dois tornados/pool2 e cap32, com limites de Editor/GC agregado.

Antes de encerrarGPR-01 ou autorizar01.6: diagnosticar pausa/recovery e obter revisão física das camadas/tripla/telegraphs. Auditoria futura dedicada de recompensas/ritmo, layouts, identidade visual de inimigos e bosses é apenas proposta. Singularity, tuning, economia nova, novasMutations/especiais/skills e redesign não foram executados.

[Relatório consolidado e evidências](../decisions/GPR-01.5_RELATORIO.md). Relatórios01.5a/b/c/d preservam comandos, jobs, falhas e limites.

## Histórico anterior — não sobrepõe GPR-01.5


## Estado vigente — GPR-01.4, 29/10/2026

O pedido direto aceita fisicamente01.2c/01.3 e autoriza sequencialmente01.4a–e, incluindo transformação visual obrigatória. **GPR-01.4 implementada, com validação integrada e ressalva legada; STOP após01.4.** GPR-01.5, Singularity, novos especiais, troca modular, tuning, inimigos e salas não foram executados. Todo estado anterior abaixo é histórico onde divergir deste registro.

A=mutation_gauntlet_titan_fists; B=conductor; C=detonation, mesmos IDs/catálogos/tickets. Base/B/C/B+C mantêm execução nativa e não criam tornado. A viaja como vento; A+B acrescenta identidade elétrica e uma oportunidade Conductor; A+C acrescenta fogo e uma oportunidade Detonation em morte confirmada; A+B+C compõe vento, eletricidade e fogo, permitindo morte pelo Conductor derivado autorizar Detonation. Fogo visual não aplica burn/DoT.

Raiz e budget32 compartilhados; uma emissão Conductor e uma Detonation por tornado. Sem Conductor→Conductor, Detonation→Detonation/Conductor ou tornado→tornado no ramo novo. Native B+C preservado, inclusive suas cascatas e dedupe: não aplicar a whitelist nova globalmente. Soco e vento podem conduzir separadamente quando existem alvos elegíveis e capacidade.

Ownership visual no GauntletExecutionSnapshot aceito; ranks mecânicos na raiz aceita. A reserva intersecta ownership/snapshot e inventário, e o visual recebe essa mesma política. Aquisição durante antecipação/voo não altera projétil anterior. Cosmetic Completed conserva identidade aceita, sem root, HP, arco externo ou explosão. Pool/leases limpam fogo e eletricidade em release/clear/swap/disable/run/cena/contexto. Geometria, velocidade3,5m/s, alcance5,5m, TTL2s, raio0,65m e dano0,60× preservados.

Validação real Unity6000.5.10f1/MCP no checkoutD:/Dev/Personal/My_games/Tech-Guy:98/98 focados,12/12 visual/lifecycle,7/7 integração e90/90 EditMode. Regressão ampliada199/203; três falhas de diretório corrigidas na preparação e4/4 no diagnóstico isolado. Uma ocorrência de Asura cadenciada permanece sem causa confirmada; diagnóstico verde não é correção ou bateria ampliada inteiramente verde.368 nomes distintos com resultado aprovado observado (278 PlayMode+90 EditMode), sem ignorados nas seleções finais; não suíte global nem novo aceite humano.

Tickets reais A+B seed0 e A+C seed1; quatro ordens triplas assistidas explicitamente. Rota normal exercitada oferece somente uma oportunidade Protocol: não foi ampliada para fabricar aquisição tripla natural. Capturas Game View, receipts separados, desempenho controlado8/32/64 e todas as tentativas no consolidado e artefatosGPR-01.4. Playtest físico de leitura/feel desta revisão continua pendente.

Matriz obrigatória executada: base/A/B/C/AB/AC/BC/ABC; whiff/zeroHP/imunidade/LOS/colliders/geração/caps; dois produtores; morte por vento e eletricidade; não recursão; aquisição antes/depois de aceitação e durante voo; pausa/hitstop/cancel/reentrância/swap/disable/morte/run/cena/clear/próxima sala; legadosGI/armas/input. Quatro comparadores Game View com quadros separados, eventos confirmados e reutilização. Desempenho mede agregadoProtocol e GC do frame no Editor; não isola custo deRaycastAll/GPU e não certifica standalone. Preservar pastas históricas e registrar falhas intermediárias.

[Consolidado](../decisions/GPR-01.4_RELATORIO.md) · relatórios separados01.4a/b/c/d/e e evidências novas vinculadas nele.

## Histórico anterior — não sobrepõe o estado GPR-01.4


Fechamento técnico29/10/2026: **329 casos distintos aprovados nas seleções (239 PlayMode +90 EditMode)**; zero ignorados, sem certificação da suíte global. Inclui196/196 regressões e correção de preparação do caso de cancelamento, com falhas intermediárias preservadas. Evidência e limites no relatórioGPR-01.3. Revisão física da composição pendente; STOP após01.3.

## Execução01.2c /01.3 — 29/10/2026

As verificações atuais estão nos [relatórios01.2c](../decisions/GPR-01.2c_RELATORIO.md) e [01.3](../decisions/GPR-01.3_RELATORIO.md), com seletores, jobs, falhas intermediárias e contagens. Separar aceite físico de01.2b, correção técnica de lifecycle e composição ainda pendente de revisão humana. Não repetir writers antigos sobre evidências históricas: saídas de regressão desta atividade usam SessionState `TechGuy.Tests.GprPhase` em pasta própria.

Comparar base/A/B/A+B; soco em whiff com vento válido/vento sem impacto; imunidade do vento e do arco; quota inteira/último slot; LOS, dedupe/geração, cancelamento no receipt, ranks e aquisição tardia; morte final/clear/Traversal e próxima sala. Imagens não substituem asserts de HP, root/parent, estatísticas e recursos. Regredir controles, habilidades, três armas, Protocols, rewards e encontros. Manter limitação explícita para carga8/32/64, save/resume e standalone.

**STOP após01.3**, sem teste/implementação de Detonation composta ou Singularity.

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Gate atual e execução proporcional

C01.5 fechada com evidência histórica revisada + aceite físico direto; nenhum teste novo repetido nesta rodada. Limites físicos/standalone/crowd permanecem explícitos no relatório5 e não equivalem a rejeição do controle.

Para a fatia proposta, V09 prova A; V10 prova A+B e A+B+C em todas as ordens; V11 prova escopoBasic/restore/tickets/rollback e aquisição sem reset de timers (swap adiado); V12 compara base/A/A+B/A+B+C/reset; V13 prova seed/rota e duas runs; V14 reconcilia HP/causalidade/energia; V15 regressões dos owners afetados; V16 é aceite humano somente de mutação/sinergia/visual novos. Detalhes observáveis nos nove critérios da [fatia](GPR_01_VERTICAL_SLICE.md).

Antes de alterar owner de Protocols, selecionar casos históricos afetados (inclusive BurnTick se relevante), registrar baseline/falhas reais; não rodar tudo por rotina nem enfraquecer testes corretos. Usar MCP no projeto certo, save/reload de dados autorados, Console sem Clear e teardown/Prefs isolados. Não declarar readiness executável antes dessas provas futuras.

## Plano histórico — superado nos gates explicitados acima

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



## Histórico C01.3b — fechamento na data,28/10/2026

**CONCLUÍDA TECNICAMENTE:105/105 casos distintos finais, zero ignorados**, via Unity MCP no checkout correto/Editor6000.5.10f1. Aceite humano anterior das últimas correções recebido para WASD, básicos, Q/E/R e Asura/RMB; básicos rápidos ficam como refinamento futuro, sem mudar cadência. [Relatório](../decisions/GPR-C01.3b_RELATORIO.md).

Corrigidas fixtures que criavam dispositivos virtuais antes da política de foco, e o gate de soltura que descartava o primeiro comando novo no frame seguinte. Dois testes falharam0/2 antes da correção de produção;22 básicos/input+23 movimento+7 contextos+30 combate+8 dash+15 EditMode passaram após as correções. Ajustado teste Cadenciada para agir no frame de recovery observado, mantendo asserções/tempos. Outputs e falhas anteriores preservados em c01-3b/correcao e gate; não suíte global. Runtime publicado com input virtual:3 básicos0/1/2 e1 Asura por RMB,4 PNG; não novo teste físico.

F11 deixou de ativar Asura direta por decisão explícita; binding salva/executor/custos/estágios preservados. Frame de soltura continua bloqueado; held não atravessa UI/foco/pausa. Editor final NexusLobby limpa/Edit Mode; permanece erro de pacote Tripo3D preexistente. Causa física do incidente RMB antigo não confirmada retrospectivamente; aceite humano posterior inclui RMB funcional.

**Gate de C01.4 satisfeito;4a–4d já autorizadas, ainda não implementadas.** Não iniciar C01.5/GPR-01. Interact separado precisa respeitar F/finisher; X pode ficar vazio, nunca duplicar Asura. Autorizações e evidências3a abaixo são históricas onde conflitam com este fechamento.

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
