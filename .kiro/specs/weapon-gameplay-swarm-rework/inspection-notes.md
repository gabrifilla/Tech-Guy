# Registro de Inspeção — Weapon Gameplay Swarm Rework (Tarefa 1.1)

> Pré-requisito de qualquer alteração (R1). Documenta o estado atual dos sistemas de
> combate/arma/inimigo **antes** de tocar em qualquer arquivo, para orientar uma abordagem
> aditiva (estender, não recriar).

## 0. Método de inspeção e ressalvas de execução (R1.2, R1.7)

- **Unity_MCP indisponível.** O power `kiro-unity-accelerator` está instalado, mas **não expõe
  ferramentas** neste ambiente. A inspeção via Unity_MCP prevista em R1.1 **não pôde ser executada**.
- **Fallback aplicado (R1.2):** análise da estrutura de `Assets/_Project` + leitura direta dos `.cs`
  relevantes + busca de referências (`grep`) em `.cs`, `.unity`, `.prefab`, `.asset` e `.meta`.
- **Verificações NÃO executadas (relatadas, não marcadas como validadas) — R1.7 / R13.9:**
  - Inspeção/automação via Unity_MCP (ferramentas ausentes).
  - Abertura e validação de referências em cenas (`.unity`), prefabs (`.prefab`) e ScriptableObjects
    (`.asset`) dentro do Editor do Unity. Confirmou-se apenas por busca textual que os termos de
    enxame não aparecem nesses arquivos; a **integridade de referências/GUID no Editor não foi
    validada** por esta via.
  - Execução de testes EditMode/PlayMode via CLI.
- **Working tree:** `git status` mostra arquivos já modificados/não rastreados pelo usuário
  (feature `enemy-swarm-core-archetypes` em andamento). Esta tarefa **apenas adiciona** este
  documento de notas e **não altera** nenhum arquivo existente.

## 1. Componentes e assets inspecionados

Todos os caminhos são relativos à raiz do workspace `Tech-Guy`.

### 1.1 Sistema de reação / postura (`Assets/_Project/Scripts/Characters/Combat`)

| Item | Arquivo | Tipo | Observações-chave para a rework |
| --- | --- | --- | --- |
| `CombatReactionController` | `CombatReactionController.cs` | `MonoBehaviour` | Núcleo Lost Ark-style de postura/stagger/quebra. Já processa reação imediata (Push/Stagger) separada da quebra (Stun/KnockUp/Knockback). Tem `ConfigureRank` + `ConfigureStance`, `ApplyRankDefaults` (defaults por `EnemyRank`), `breakImmunityDuration`, `stanceRecoveryDelay`, resistências 0–1 por CC, `StanceBroken` event, `IsControlLocked`. **Ponto de extensão central** para R2/R3/R4. |
| `HitReactionRequest` | `HitReactionRequest.cs` | `readonly struct` | Campos: `Attacker`, `HitPoint`, `HitDirection`, `ReactionType`, `Strength`, `StanceDamage`, `BreakEffect`, `PushDistance`, `StunDuration`, `KnockUpHeight`, `KnockbackDistance`. **Já cobre os campos exigidos por R4.4** (nenhum canal paralelo necessário). Construtor faz `Mathf.Max(0, …)` em todas as distâncias/durações. |
| `HitReactionType` | `HitReactionType.cs` | `enum` | `None`, `Push`, `Stagger`. Contém também `StanceBreakEffect` (`None`, `Stun`, `KnockUp`, `Knockback`) no mesmo arquivo. |
| `StanceBreakEffect` | `HitReactionType.cs` | `enum` | Ver acima. Escada de severidade implícita KnockUp→Stun já existe em `TriggerStanceBreak` (KnockUp imune recai em Stun). |
| `HitStrength` | `HitStrength.cs` | `enum` | `Light`, `Medium`, `Heavy`, `Breaker`. |
| `EnemyRank` | `EnemyRank.cs` | `enum` | `Normal`, `Elite`, `Legendary`, `Boss`. |

**Constatações relevantes no `CombatReactionController`:**
- Quatro canais já independentes na prática (vida fica fora deste componente; reação imediata, dano
  de postura e quebra são tratados separadamente em `ApplyReaction`/`ApplyStanceDamage`).
- `ApplyStanceDamage` já faz `Mathf.Max(0, current − dano × multiplicador)` e resolve a quebra no mesmo
  passo em que a reserva zera; restaura a reserva ao máximo e aplica `breakImmuneUntil`.
- Janela de imunidade pós-quebra: `breakImmunityDuration` (default 1,5s). **Não há clamp explícito de
  legibilidade (≤0,5 m / ≤15°) na reação imediata** — R2.2 exigirá extensão.
- **Não existe** `VulnerabilityWindow` / rebaixamento de raridade efetiva (R3.3) — a implementar.
- **Não existe** supressão de Micro/Push durante travamento além do gate `if (!controlLocked)` no Push
  imediato — R4.5 exigirá tratamento explícito de deslocamento adicional.

### 1.2 Inimigos e IA (`Assets/_Project/Scripts/Characters/Enemy`)

| Item | Arquivo | Tipo | Observações-chave |
| --- | --- | --- | --- |
| `EnemyProfile` | `EnemyProfile.cs` | `ScriptableObject` | Fonte de dados de postura/resistências (`MaxStance`, `StanceDamageMultiplier`, `StanceRecovery*`, `*Resistance`). Alimenta `ConfigureStance`. `EnemyRarity` (Normal/Magic/Rare) é distinto de `EnemyRank`. |
| `EnemyVariant` | `EnemyVariant.cs` | `MonoBehaviour` | Orquestra `ApplyProfile`→`ApplyStanceProfile`→`ConfigureStance`/`ConfigureRank`; sem profile mantém defaults por rank. `ApplyArchetype` adiciona `PriorityTargetMarker` a arquétipos de prioridade. Resolução por campo serializado (sem `Find`). |
| `EnemyAI` | `EnemyAI.cs` | `MonoBehaviour` | Percepção/locomoção/ataque. Expõe `InterruptAttack()` (→ `CancelAttack`), `TelegraphedAttack` (coroutine), `EngagementRange`/`StandoffDistance` (via `EnemyAttackPatterns`), gate `CanAttack`. **Ponto de extensão** para slots de ataque (R5), distância preferida (R6) e interrupção (R7.6–7.8). |
| `PriorityTargetMarker` | `PriorityTargetMarker.cs` | `MonoBehaviour` | Marcador de prioridade world-space (ring persistente + cue de ação). Lógica pura de presença em `PriorityMarkerPresence`. **Reusável** pela Marca do Arco (R10) via `WeaponMark`. |
| `ArchetypeId` | `ArchetypeId.cs` | `enum` | 16 arquétipos: Rush, Grunt, Heavy, Charger, Shooter, SpreadShooter, Sniper, Bomber, HazardCaster, Hooker, Healer, ShieldSupport, Swarm, Spawner, Fragile, Mirror. Base da matriz Arma×Inimigo (R11). |
| `CombatRole` | `CombatRole.cs` | `enum` | MeleePressure, RangedPressure, TerritoryControl, PlayerDisplacement, AllySupport, SwarmFuel, PriorityThreat, ComboFodder, ProjectileDenial. Base do fallback por role (R6.4/R11.7). |

### 1.3 Sistema de armas / habilidades (`Assets/_Project/Scripts/Abilities/Weapon` e `.../Weapons`)

| Item | Arquivo | Tipo | Observações-chave |
| --- | --- | --- | --- |
| `ArsenalCombat` | `Abilities/Weapon/ArsenalCombat.cs` | `MonoBehaviour` | Executa habilidades de Arco/Lança via `Use(ArsenalAbility)` → `Execute` (coroutine). Usa `WeaponRunModifiers.Plan` e roteia dano por `PlayerActor.TryApplyAreaDamage`. Tem `FindNearbyEnemy`, `TryChainThrust`, `FireMoonShards`, `SpectralThrust`. **Ponto de injeção** para SweetSpot/SpearFlow (R9) e Mark (R10) sem engordar o MonoBehaviour. |
| `ArsenalAbility` | `Abilities/Weapon/ArsenalAbility.cs` | `ScriptableObject` (`Ability`) | Dados de skill: `ArsenalSkillKind { Arrow, Volley, Rain, Thrust, Sweep }`, `Windup/Hits/Interval/Range/Width/DamageMultiplier/Piercing`. `ConfigureRunTransformation` só muta cópia runtime. |
| `ArsenalSkillKind` | `Abilities/Weapon/ArsenalAbility.cs` | `enum` | Arrow, Volley, Rain, Thrust, Sweep. |
| `AreaHitStep` | `Abilities/Weapon/AreaHitStep.cs` | `[Serializable]` classe | Passo de dano em área com bloco de **Reaction** (`reactionType`, `hitStrength`, `pushDistance`) e **Stance** (`stanceDamage`, `breakEffect`, `stunDuration`, `knockUpHeight`, `knockbackDistance`). **Já é o canal de dados de deslocamento** exigido por R4.4. Shapes: Box/Sphere. |
| `BreakerGauntletCombat` | `Abilities/Weapon/BreakerGauntletCombat.cs` | `MonoBehaviour` | Cast das Manoplas. Hospeda `AsuraMomentum`. `Use`/`Execute`, avanço clampado à NavMesh (`agent.Raycast`/`NavMeshHit`), aplica `AreaHitStep` via `SequencedAreaAttackAbility.ApplyHitStep`, `FinishForDodge`. **Ponto de extensão** do loop das Manoplas (R8). |
| `AsuraMomentum` | `Abilities/Weapon/AsuraMomentum.cs` | classe plana | `Maximum=100`, `AddEnergy` (clamp), `RegisterSkill` (25 se categoria difere, 10 se igual), `TryConsume`, `IsReady`, `Reset`. **Já satisfaz** a lógica base de R8.6/8.7/8.10. |
| `BreakerGauntletAbility` | `Abilities/Weapon/BreakerGauntletAbility.cs` | `ScriptableObject` | (Inspecionado por referência via `BreakerGauntletCombat`/`WeaponRunModifiers`: expõe `HitSteps`, `AsuraBurst`, `ShockSkill`, `AdvanceDistance`, `AdvanceDuration`, `activeTime`, `EffectColor`.) |
| `WeaponScript` | `Weapons/WeaponScript.cs` | `ScriptableObject` | `FiresArrows` (→ família Bow), `abilities[]`, stats de ataque, `hitbox`. |
| `WeaponLoadout` | `Weapons/WeaponLoadout.cs` | classe estática | 3 armas via `Resources` (`Gauntlet`, `Bow`, `Spear`), unlock por moedas, seleção/equipar. Confirma o kit de 3 armas (R13.1). |
| `WeaponRunModifiers` | `Weapons/WeaponRunModifiers.cs` | classe plana | `RunWeaponFamily { Gauntlet, Bow, Spear }`, `WeaponBoon` (34 boons catalogados), `Catalog` (imutável), `Rank`, `Add`, `Identify`, `DirectDamageMultiplier`, `Plan(ArsenalAbility, slot)` → `ArsenalCastPlan`, `GauntletSteps(BreakerGauntletAbility, slot)`. **Muta apenas snapshots por conjuração** (`ArsenalCastPlan`; `GauntletSteps` clona via `JsonUtility`). Base de R12. |
| `ArsenalCastPlan` | `Weapons/WeaponRunModifiers.cs` | classe plana | Snapshot por cast: `Windup/Interval/Range/Width/Damage/WaveMultiplier/Hits/Arrows/Directions/TrackCursor/Travel/PhantomDelay/ShardCount/ReturnWave/ChainThrust`. |
| `PlayerActor.TryApplyAreaDamage` | `Characters/Player/PlayerActor.cs` | método (várias sobrecargas) | Pipeline compartilhado de dano em área (Box/Sphere), aceita `HitReactionRequest?`. Já é o **ponto único** por onde as habilidades aplicam dano/reação. Chamado por `ArsenalCombat`, `SequencedAreaAttackAbility`, `CharControlScript` (nova/básicos). |

## 2. Confirmação: os sistemas de enxame ainda NÃO existem (R1.2)

Busca de referências pelos termos-chave dos serviços de enxame a implementar:

- `grep "AttackSlot|PreferredDistance|SoftGroup"` em `**/*.cs` → **nenhum resultado**.
- `grep "AttackSlot|PreferredDistance|SoftGroup|Preferred_Distance"` em `**/*.{unity,prefab,asset}`
  → **nenhum resultado**.

Conclusão: **não existem** `AttackSlotPool`/`AttackSlotConfig`/`SwarmAttackCoordinator`,
`PreferredDistanceProfile`/`PreferredDistanceLayer`, nem `SoftGroupingService`/`SoftGroupingConfig`.
Também **não existem** `DisplacementTier`, `VulnerabilityWindow`, `RankReactionDefaults`, `SweetSpot`,
`SpearFlow`, `WeaponMark`/`MarkConfig` nem `WeaponEnemyMatrix`. Todos serão **novos**, criados de forma
desacoplada (R5.4) e reusando a infraestrutura acima.

## 3. Mapa de reutilização (aditivo, não recriar) — R1.3/R1.4

- **Reação/Postura/Raridade (R2/R3/R4):** estender `CombatReactionController` + dados via
  `EnemyProfile`/`RankReactionDefaults`; expressar deslocamento só pelos campos de `HitReactionRequest`
  / `AreaHitStep` (sem canal paralelo).
- **Enxame (R5/R6/R7):** novos serviços desacoplados conectados a `EnemyAI`
  (`InterruptAttack`/`TelegraphedAttack`/`EngagementRange`/`StandoffDistance`) e à locomoção existente
  (`NavMeshAgent`/`CharacterController`).
- **Manoplas (R8):** estender `BreakerGauntletCombat`/`AreaHitStep`; preservar `AsuraMomentum`.
- **Lança (R9):** injetar `SweetSpot`/`SpearFlow` em `ArsenalCombat`; configurar `ArsenalAbility`/`AreaHitStep`.
- **Arco (R10):** `WeaponMark` reusando `PriorityTargetMarker`; configurar via dados.
- **Modificadores (R12):** estender `WeaponRunModifiers.Plan`/`GauntletSteps` mantendo imutabilidade dos assets.
- **Matriz (R11):** novo `WeaponEnemyMatrix` (ScriptableObject) sobre `ArchetypeId × RunWeaponFamily` com fallback por `CombatRole`.

## 4. Preservação de `.meta`/GUID (R1.5)

Esta tarefa não move, renomeia nem exclui assets — apenas adiciona este documento em
`.kiro/specs/...` (fora de `Assets/`, portanto sem `.meta`). Nenhum GUID/caminho especial foi alterado.
