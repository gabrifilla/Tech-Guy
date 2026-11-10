# Tech Guy — STATUS

**Atualizado:** 09/11/2026 · **Branch:** `gameplayOverhaul` · **Fase atual:** 1b — Fragmentos, NPC e terminal (Etapa 0: plano aguardando aprovação). 1a aprovada em playtest em 09/11/2026. ([plano](plano/TECH_GUY_PLANO_RESET_DE_LOOP.md))

> Ponto de entrada único. `02_ESTADO_E_LACUNAS.md` e `03_PRIORIDADES.md` ficam como histórico e não são mais atualizados.

## Onde estamos
- **Combate base:** divertido, segundo o desenvolvedor. A Manopla é a única arma em foco.
- **Run:** `NexusBlockout → FirstSector → NexusBlockout`. Uma run é um setor. Boss morto → hit-stop curto + inimigos e perigos restantes da sala removidos → faixa "UNIVERSO 1 CONCLUÍDO" (~2,5 s) → portal (o jogador entra quando quiser) → resultado "VITÓRIA" no Nexus.
- **Meta-progressão:** base invisível `MetaProgress.v1` (JSON em `Application.persistentDataPath`) conta runs, vitórias, mortes, abandonos e universos concluídos. A vitória e o universo concluído contam **na morte do boss**, não na entrada do portal. Sem UI; moedas inalteradas.

## Jogável agora (o que testar)
| O quê | Como |
|---|---|
| Run completa com a Manopla | MainMenu → Nexus → portal First Sector → boss → faixa → portal → resultado "VITÓRIA" |
| Atalho do boss (Editor/Development) | Na escolha de rota do FirstSector, **F9** limpa as salas pela política real e entra na sala do boss |
| Recompensas C05 | Qualquer sala de combate |
| Combo protegido | Titanic Fists → Conductor → Detonation |

## Congelado / pausado
Lança, Arco, arte 2.5D/pictórica e prólogo. Os protótipos G1/G2/G3 (RUN-EXP-10) estão só no branch `proto/run-exp-10-g123`, fora do `gameplayOverhaul`.
Preservar: Titanic → Conductor → Detonation, salas 09/09B, C05 e os controles WASD/mouse/Q/E/R.

## Saúde técnica
- EditMode: 274/274 (09/11/2026, job `b43c561d`, Fase 1a).
- PlayMode completo: último 828/879 (09/11/2026, job `861b5b32`, antes da 1a). Suítes focais da 1a: 43 testes passando (job `da07be45` 26/28; as 2 falhas, um erro do teste novo e um reload do Fast Script Reload, passaram após correção no job `17fa570c`; resultados 15/15 no `07e7e185`).
- Atenção: a suíte completa grava em `Docs/validation/artifacts/` (caminhos fixos). Confira o `git status` antes de commitar depois de rodá-la.

### Quarentena F0 — 44 testes PlayMode
`[Explicit("QUARENTENA-F0: motivo")]`: ficam fora da execução padrão e rodam quando selecionados pelo nome. `grep -r QUARENTENA-F0 Assets` lista todos, com o motivo. Fase 1a triou o grupo de rota/salas: 6 saíram da quarentena (reescritos pela regra 09B ou ambiente corrigido) e 6 seguem com motivo atualizado. Suíte padrão após a quarentena: **não rodada** (meta: verde).

| Motivo | Testes |
|---|---|
| Navegam por clique (removido no C01) — 9 | NexusBlockout/NexusSpatialJourney `Journey720`/`Journey1080`; NexusUsability `CombatHudIsReserved…`, `PedestalKeyEquips…`, `PointerWalkAndInteraction…`, `ResultsBlockPedestalKey…`; ManualRunPlayerFeedback `PublishedFloorClickActuallyMoves…` |
| Esperam efeito síncrono (triagem 10.2B) — 9 | BasicAttackR1 `GauntletPhase3{BasicWithoutRun, CadencedWait, PublicEventOrder, RConsumesOnce, ReentrantRequest}…`, `ManualLastBasicBatch…`, `ManualPlayerDeathInLastLethalBatch…`, `MigratedGauntletBasicInputs…`; ManualRunPlayerFeedback `FirstTwoRooms…` |
| Causa não determinada, Manopla — 7 | BasicAttackR1 `GauntletPhase3{AuthoredZeroChildDelays, DemolishingAdvance, LegacyShockwave, RewardsOfferExecutable, SharedHudGroups}…`, `ManualLastEcho…`, `ManualLastSkillCancels…` |
| Anteriores ao catálogo atual — 5 | BasicAttackR1 `FourPathsAcquireMutationAtC4…`, `RunFocusDiscounts…`, `ManualImpossibleFirstMutation…`, `PauseConfirmedAbortWaits…`; ProtocolPublishedComposition `FiveCompositions…` |
| Salas retangulares fixas / modo fora da produção (triagem 1a) — 5 | RunExp03FirstSector `Archive05Comparison…`, `ConsolidatedRoles…`, `RefinedRoomComparisons…`; ManualAuthoredNavigation `FourAuthoredPaths…`; BoonPublishedEncounter `Gauntlet…` (seed aleatória, causa não determinada) |
| Ambiente de teste vazando / Lança pausada — 4 | BasicAttackR1 `SpearBoon{ContactStake, PathStake, Horizon}…`; ManualAuthoredNavigation `AuthoredClosedGate…` |
| Dependem da ordem (passam em seleção menor) — 5 | `TornadoKeepsCommittedDirection…`, BreakerKitEmission `ConsecutiveNativeSkills…`, Gpr015 `GameViewTriple…`, SpacingRecoil `Recoil_NeverCrossesSolidScenery`, TitanicRoomClear `RoomClearElementalLastKill…` |

Saíram na 1a: `{Gauntlet,Spear,Bow}NaturalSeed…`, `GeneratedPaths…`, BoonPublishedEncounter `Bow…`/`Spear…`.

## Próxima etapa
**Fase 1b** ([prompt](plano/PROMPT_FASE_1B_FRAGMENTOS_NPC_TERMINAL.md)): Fragmentos de Dados, primeiro NPC resgatável, terminal com 6 melhorias e progresso na tela de fim de run.

## Backlog
- Boss do Universo 1 aprovado tecnicamente, mas sem graça no playtest; redesign pendente.

## Decisões em aberto (do desenvolvedor)
- Identidade do primeiro NPC resgatável.
- Terminal fechado até o 1º resgate ou versão mínima desde a 1ª run.

## Regras de processo
Uma frente por vez · cada etapa termina jogável · relatório de 1 página (Feito / Não feito / Não testado / O que jogar) · sem novos arquivos em `Docs/decisions/` por etapa · boon que não aparece em 10 s de vídeo não entra.
