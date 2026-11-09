# Tech Guy — STATUS

**Atualizado:** 09/11/2026 · **Branch:** `gameplayOverhaul` · **Fase atual:** 0 — Arrumar a casa ([plano](plano/TECH_GUY_PLANO_RESET_DE_LOOP.md))

> Ponto de entrada único. `02_ESTADO_E_LACUNAS.md` e `03_PRIORIDADES.md` ficam como histórico e não são mais atualizados.

## Onde estamos
- **Combate base:** divertido, segundo o desenvolvedor. A Manopla é a única arma em foco.
- **Run:** `NexusBlockout → FirstSector → NexusBlockout`. Uma run é um setor. O boss fica no fim; vencê-lo abre o portal de volta ao Nexus.
- **Meta-progressão:** praticamente inexistente (só moedas para desbloquear Arco e Lança). **É a próxima frente.**

## Jogável agora (o que testar)
| O quê | Como |
|---|---|
| Run completa com a Manopla | MainMenu → Nexus → portal First Sector → boss → portal |
| Recompensas C05 | Qualquer sala de combate |
| Combo protegido | Titanic Fists → Conductor → Detonation |

## Congelado / pausado
Lança, Arco, arte 2.5D/pictórica e prólogo. Os protótipos G1/G2/G3 (RUN-EXP-10) estão só no branch `proto/run-exp-10-g123`, fora do `gameplayOverhaul`.
Preservar: Titanic → Conductor → Detonation, salas 09/09B, C05 e os controles WASD/mouse/Q/E/R.

## Saúde técnica
- EditMode: 267/267 (09/11/2026, job `b0e34a2d`, com G1/G2/G3 ainda no working tree).
- PlayMode completo: 828/879, 50 falhas, 1 Explicit (09/11/2026, job `861b5b32`, idem).
- Atenção: a suíte completa grava em `Docs/validation/artifacts/` (caminhos fixos). Confira o `git status` antes de commitar depois de rodá-la.

### Quarentena F0 — 50 testes PlayMode
`[Explicit("QUARENTENA-F0: motivo")]`: ficam fora da execução padrão e rodam quando selecionados pelo nome. `grep -r QUARENTENA-F0 Assets` lista todos, com o motivo. Nenhum corpo de teste foi alterado. Suíte padrão após a quarentena: **não rodada** (meta: verde).

| Motivo | Testes |
|---|---|
| Navegam por clique (removido no C01) — 9 | NexusBlockout/NexusSpatialJourney `Journey720`/`Journey1080`; NexusUsability `CombatHudIsReserved…`, `PedestalKeyEquips…`, `PointerWalkAndInteraction…`, `ResultsBlockPedestalKey…`; ManualRunPlayerFeedback `PublishedFloorClickActuallyMoves…` |
| Esperam efeito síncrono (triagem 10.2B) — 8 | BasicAttackR1 `GauntletPhase3{BasicWithoutRun, CadencedWait, PublicEventOrder, RConsumesOnce, ReentrantRequest}…`, `ManualLastBasicBatch…`, `ManualPlayerDeathInLastLethalBatch…`, `MigratedGauntletBasicInputs…` |
| Causa não determinada, Manopla — 7 | BasicAttackR1 `GauntletPhase3{AuthoredZeroChildDelays, DemolishingAdvance, LegacyShockwave, RewardsOfferExecutable, SharedHudGroups}…`, `ManualLastEcho…`, `ManualLastSkillCancels…` |
| Anteriores ao catálogo atual — 5 | BasicAttackR1 `FourPathsAcquireMutationAtC4…`, `RunFocusDiscounts…`, `ManualImpossibleFirstMutation…`, `PauseConfirmedAbortWaits…`; ProtocolPublishedComposition `FiveCompositions…` |
| Rota/salas 09/09B — 12 | RunExp03FirstSector `{Gauntlet,Spear,Bow}NaturalSeed*ReachesBoss…`, `Archive05Comparison…`, `ConsolidatedRoles…`, `RefinedRoomComparisons…`; ManualAuthoredNavigation `FourAuthoredPaths…`, `GeneratedPaths…`; BoonPublishedEncounter `Bow…`, `Gauntlet…`, `Spear…`; ManualRunPlayerFeedback `FirstTwoRooms…` |
| Ambiente de teste vazando / Lança pausada — 4 | BasicAttackR1 `SpearBoon{ContactStake, PathStake, Horizon}…`; ManualAuthoredNavigation `AuthoredClosedGate…` |
| Dependem da ordem (passam em seleção menor) — 5 | `TornadoKeepsCommittedDirection…`, BreakerKitEmission `ConsecutiveNativeSkills…`, Gpr015 `GameViewTriple…`, SpacingRecoil `Recoil_NeverCrossesSolidScenery`, TitanicRoomClear `RoomClearElementalLastKill…` |

O grupo "Rota/salas" cobre o fluxo da Fase 1a: deve ser triado lá, não esquecido.

## Próxima etapa
**Fase 1a — fim claro do Universo 1:** boss vencível no fluxo normal, tela de vitória e volta ao Nexus. Critério jogável: o desenvolvedor vence o boss com a Manopla numa run normal e vê a vitória.

## Decisões em aberto (do desenvolvedor)
- Identidade do primeiro NPC resgatável.
- Terminal fechado até o 1º resgate ou versão mínima desde a 1ª run.

## Regras de processo
Uma frente por vez · cada etapa termina jogável · relatório de 1 página (Feito / Não feito / Não testado / O que jogar) · sem novos arquivos em `Docs/decisions/` por etapa · boon que não aparece em 10 s de vídeo não entra.
