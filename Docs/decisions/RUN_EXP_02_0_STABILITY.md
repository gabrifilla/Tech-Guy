# RUN-EXP-02.0 — estabilidade e triagem

Escopo: revalidar o pull/fogo de RUN-EXP-01 e classificar as 31 falhas históricas. Não há aceite físico novo nem retuning de combate.

## Trabalho e arquitetura

Preservados o bloqueio de renovação do pull ativo e os 0,6 s de recuperação exclusiva contra outro pull. Sem imunidade adicional a dano. Tornado/fogo e executores publicados não foram alterados nesta frente.

A fixture de composição reutilizava o mesmo destino de evidência. Um teste novo reproduziu a colisão (0/1, job `b63a27422a2348d7a346c187302e2550`). A correção mantém o diretório-base do chamador, reinicia o cache por teste e cria subdiretório único. Asserções de aquisição natural permanecem. A fixture GPR015 passou a respeitar o diretório de evidência da fase, preservando o default histórico.

Arquivos: `TitanicConductorPlayModeTests.cs`, `Gpr015ProductionAuditTests.cs`, `RunExp02StabilityTests.cs`; cliente fino `Tools/RunExp02/mcp_call.py` reutiliza o cliente MCP existente. Não há servidor/runner alternativo.

## Unity e testes

MCP confirmou `Tech-Guy@fccafb2b47cd7671`, Unity `6000.5.10f1`, projeto `D:/Dev/Personal/My_games/Tech-Guy`. Import/Console e UTF consultados pelo cliente existente; tool execute_code usa CodeDOM.

Job `b007fc8e1c3940cd8cc95cc4fd59dc00`: **13/13 PlayMode**, seleção exata abaixo. Resultado integral: [stability-green.json](../validation/artifacts/run-exp-02/stability-green.json).

- `TechGuy.Tests.BasicAttackR1PlayModeTests.CompositionNaturalTicketsAcquireAAndCWithoutFixtureGrants` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00OverlappingHooksCannotRestartTheControlClock` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00PauseAndHitstopPreservePullAndRecoveryClocks` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00PlayerDeathCannotLeavePullOrRestoreDeadInput` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00ProductionHooksAllowMovementAndDashBetweenConnections` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00SingleAttackerRepeatsWithARealControlWindow` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUN00SourceDeathAndSceneUnloadCannotRetainPullOwnership` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.RUNEXP02RepeatedEvidenceRootNeverReusesCaptureDestination` — Passed
- `TechGuy.Tests.BasicAttackR1PlayModeTests.TitanicConductorNaturalTicketsAcquireBothWithoutFixtureGrants` — Passed
- `TechGuy.Tests.Gpr015ProductionAuditTests.GPR015GameViewElectricWindInPublishedCombat` — Passed
- `TechGuy.Tests.Gpr015ProductionAuditTests.GPR015GameViewFireWindInPublishedCombat` — Passed
- `TechGuy.Tests.Gpr015ProductionAuditTests.GPR015GameViewTripleInPublishedCombat` — Passed
- `TechGuy.Tests.Gpr015ProductionAuditTests.GPR015GameViewWindInPublishedCombat` — Passed

Capturas A, A+B, A+C e A+B+C: `Docs/validation/artifacts/gameplay-rework/run-exp-02/stability/`, produzidas em combate publicado pela fixture GPR015. São evidência assistida de composição, não probabilidade de recompensa nem aceite humano. As imagens devem ser revistas junto às limitações do relatório consolidado.

## Ledger das 31 ocorrências

Classificação inicial por comparação RUN-EXP-01 antes/depois e contratos atuais. “Histórico” identifica reprodução anterior, não prova que o comportamento seja correto. “Expectativa obsoleta” identifica uma asserção incompatível com scheduler/catálogo; o caso continua aberto até migração e nova execução. Não foi confirmado defeito novo de produção entre esses 31.

| Caso | Classe | Evidência/limite |
|---|---|---|
| `BasicAttackR1PlayModeTests.AnalyticsBasicUsesAndHitsComeFromProductionCallbacks` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.CompositionNaturalTicketsAcquireAAndCWithoutFixtureGrants` | Ambiente/preparação de evidência | Recusa correta de sobrescrever diretório reutilizado; regressão nova vermelha reproduziu destino repetido. |
| `BasicAttackR1PlayModeTests.FourPathsAcquireMutationAtC4AndExecuteItsRealShapeInNextCombat` | Expectativa obsoleta da fixture | Pressupõe catálogo reduzido ou presença garantida de card em sorteio; RNG não será alterado. |
| `BasicAttackR1PlayModeTests.GauntletPhase3AuthoredZeroChildDelaysKeepProducerFirst` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.GauntletPhase3BasicWithoutRunPaysOnceForMultipleAndLethalTargets` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.GauntletPhase3CadencedWaitRejectsUnavailableSkillsAndAcceptsQWEOrBasic` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.GauntletPhase3DemolishingAdvanceTraversesBodiesAndLaunchesOnlyOnBreak` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.GauntletPhase3LegacyShockwaveKeepsItsEffectEventsAndDistinctProducer` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.GauntletPhase3PublicEventOrderSeesCompleteGainAndNoDuplicateFist` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.GauntletPhase3RConsumesOnceReservesRanksAndPublishesAtomicResources` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.GauntletPhase3ReentrantRequestWaitsUntilImpactAndRevalidatesMana` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.GauntletPhase3RewardsOfferExecutableMutationsAndPreserveLegacyQ` | Expectativa obsoleta da fixture | Pressupõe catálogo reduzido ou presença garantida de card em sorteio; RNG não será alterado. |
| `BasicAttackR1PlayModeTests.GauntletPhase3SharedHudGroupsEffectiveGainsShowsMutationsAndUnbinds` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualBossBatchDeathWinsAndNeverCreatesPortal` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualDeathBeforePortalRejectsResidualContactAndOldSessionRequests` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualExtractionClaimsOnceAndRetryPreservesResultAndTeardown` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualImpossibleFirstMutationKeepsBossBlockedAndAllowsAbortWithoutVictory` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualLastBasicBatchWaitsForFeedbackCloseAndPreservesLethalEnergyHitStopAndEpoch` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualLastEchoClosesItsOwnBatchAndNeverPaysNativeEnergyAgain` | Caso não resolvido | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualLastSkillCancelsFutureChildrenAfterConfirmedProducerWithoutResettingCategory` | Teste não determinístico | Falhou na seleção353 e passou em ambos os comparadores; causa temporal/ambiental não isolada. |
| `BasicAttackR1PlayModeTests.ManualPlayerDeathInLastLethalBatchWinsWithoutRewardPublication` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.ManualRepairUsesRealHealthEventAndHudWithoutChangingManaAsuraOrCooldowns` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.MigratedGauntletBasicInputsUseWeaponReactionWithoutLeakingToBow` | Expectativa obsoleta da fixture | Asserção de HP/recursos/eventos antes do contato efetivo do scheduler vigente; não mudar timing de combate. |
| `BasicAttackR1PlayModeTests.PauseConfirmedAbortWaitsForResumeFrameAndReturnsFromUnavailableMutationWithoutVictory` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.RunFocusDiscountsEffectiveQCostOnceForEachWeaponFamily` | Comportamento histórico falhando | Reproduzido antes e depois do fix; não estabelece causa de produção. |
| `BasicAttackR1PlayModeTests.SpearBoonContactStakeDamageCanEndEncounterInsideItsCallback` | Teste não determinístico | Falhou na seleção353 e passou em ambos os comparadores; causa temporal/ambiental não isolada. |
| `BasicAttackR1PlayModeTests.SpearBoonHorizonUsesOneCostAndCrossingConsumesItsOriginalRootOnce` | Teste não determinístico | Falhou na seleção353 e passou em ambos os comparadores; causa temporal/ambiental não isolada. |
| `BasicAttackR1PlayModeTests.SpearBoonPathStakeDamageCanDisableTheExecutorInsideItsCallback` | Teste não determinístico | Falhou na seleção353 e passou em ambos os comparadores; causa temporal/ambiental não isolada. |
| `BasicAttackR1PlayModeTests.TargetClickDuringHitStopKeepsOneAttackAfterRelease` | Teste não determinístico | Falhou na seleção353 e passou em ambos os comparadores; causa temporal/ambiental não isolada. |
| `BasicAttackR1PlayModeTests.TitanicConductorGameViewShowsMovingWindAndImpactBoundArc` | Ambiente/preparação de evidência | Recusa correta de sobrescrever diretório reutilizado; regressão nova vermelha reproduziu destino repetido. |
| `BasicAttackR1PlayModeTests.TitanicConductorNaturalTicketsAcquireBothWithoutFixtureGrants` | Ambiente/preparação de evidência | Recusa correta de sobrescrever diretório reutilizado; regressão nova vermelha reproduziu destino repetido. |

Os dois casos de aquisição natural com destino repetido passaram na seleção acima. O terceiro caso de evidência, TitanicConductorGameViewShowsMovingWindAndImpactBoundArc, passou no job11/11 `7eb548c0fd354f1da745694aa4ade0b2`. Os demais casos históricos não são declarados corrigidos. A comparação anterior registrou 24 falhando nas duas versões, 5 aprovados nas duas e 2 variáveis; isso não equivale a uma suíte global verde.

## Preservação, skills e gates

Skills aplicadas: tech-guy-combat-unity, tech-guy-unity-acceptance, unity-mcp-orchestrator, unity-csharp-scripting e game-feel. Baseline de 306 arquivos protegidos, preferências reais e estado do Editor em `../validation/artifacts/run-exp-02/baseline-*.json`; ver comparação final no consolidado. RunSummary é isolado pelas fixtures existentes. Nenhum GUID histórico foi recriado.

- Técnico desta triagem: aprovado na seleção 13/13, sem bloqueador novo identificado; dívida histórica permanece.
- Gameplay e visual de pull/fogo: aguardam revisão física.
- Arte de produção: não se aplica à alteração de fixture; fogo continua sem aceite físico.

Próximo gate autorizado: somente os candidatos isolados de 02.1. Não se infere autorização para migração de produção.

Conferência final dos arquivos do persistentDataPath: [save-files-final.json](../validation/artifacts/run-exp-02/save-files-final.json). Quatro dos cinco hashes permanecem iguais; somente TestResults.xml mudou pela execução UTF. Isso não é alteração de save de gameplay. Console final sem erros;306/306 hashes protegidos e três preferências conferidas, com os limites de cobertura já registrados.
