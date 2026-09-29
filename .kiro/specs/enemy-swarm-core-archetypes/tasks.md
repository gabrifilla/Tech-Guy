# Implementation Plan: Enemy Swarm Core Archetypes

## Overview

This plan extends the existing Unity C# enemy stack (`EnemyAI`, `EnemyAttackPatterns`, `EnemyAttackKind`, `EnemyCombatActions`, `EnemyAttackExecution`, `EnemyProfile`, `EnemyVariant`, `CombatReactionController`, `Actor`, `PlayerActor`, `EnemyRespawnPoint`) to deliver the 16 core enemy archetypes. Nothing is replaced.

The build order mirrors the design's architecture: **foundation data types and additive primitives (Actor/Shield/projectile) first**, then **attack-pipeline extensions**, then **role behaviors and support components**, then **player-side pull and EnemyVariant wiring**, then **the 16 authored assets and per-archetype configuration**, and finally **integration and full property/unit/PlayMode coverage**.

Design decisions that shape the plan:
- Attack-shaped archetypes (Rush, Grunt, Heavy, Charger, Shooter, Spread_Shooter, Sniper, Bomber, Hooker, Hazard_Caster) flow through the widened `EnemyAttackKind` + `EnemyAttackPatterns.Select` + `EnemyCombatActions.Perform` path and need **no new MonoBehaviour**.
- Non-attack roles (Healer, Shield_Support, Spawner, Hazard_Caster's zone lifecycle) get dedicated `ArchetypeBehavior`-derived components.
- Universal invariants are extracted into **plain C# classes** (no live scene) so they can be property-tested with FsCheck/CsCheck at min 100 iterations.

New gameplay code lives under `Assets/_Project/Scripts/Characters/Enemy` (archetype behaviors, projectile, support components) and reuses `Assets/_Project/Scripts/Characters/Combat`. Additive changes to `Actor.cs` and `PlayerActor` stay in `Assets/_Project/Scripts/Characters` / `Characters/Player`. Tests go under `Assets/_Project/Scripts/Tests` (`EditMode`, `PlayMode`, `Support`).

> Note: every new `.cs` file requires its `.meta` to be generated through the Unity Editor so GUIDs stay stable; new `EnemyProfile`/`EnemyArchetype` assets must be authored as Unity assets, not hand-written. This is a mechanical Editor step, not a task.

## Tasks

- [x] 1. Foundation data identity types
  - [x] 1.1 Add `ArchetypeId` and `CombatRole` enums
    - Create `ArchetypeId` (Rush, Grunt, Heavy, Charger, Shooter, SpreadShooter, Sniper, Bomber, HazardCaster, Hooker, Healer, ShieldSupport, Swarm, Spawner, Fragile, Mirror) and `CombatRole` (MeleePressure, RangedPressure, TerritoryControl, PlayerDisplacement, AllySupport, SwarmFuel, PriorityThreat, ComboFodder, ProjectileDenial) under `Assets/_Project/Scripts/Characters/Enemy`
    - _Requirements: 1.5, 22.1, 22.2_
  - [x] 1.2 Extend `EnemyAttackKind` with new kinds
    - Add `AimedShot, SpreadShot, SniperShot, LobShot, HookLine, HazardPlace` to the existing `EnemyAttackKind` enum without removing existing values
    - _Requirements: 1.2_
  - [x] 1.3 Create `EnemyArchetype` ScriptableObject
    - `[CreateAssetMenu]` type holding `ArchetypeId`, `EnemyProfile`, `EnemyAttackTraits`, primary `CombatRole`, optional behavior template reference, and `IsPriorityTarget` computed from role (AllySupport / PriorityThreat)
    - _Requirements: 1.1, 1.4, 22.2_

- [x] 2. Actor additive primitives (Heal + Shield)
  - [x] 2.1 Add `Actor.Heal(float amount)` and shield hook in `TakeDamage`
    - Add `Heal(amount)` that sets health to `min(maxHealth, health + amount)`, clamped and no-op when dead; make `TakeDamage` offer incoming damage to an attached `Shield` (if present) before reducing health
    - _Requirements: 13.3, 14.3_
  - [x] 2.2 Create `Shield` companion component
    - Absorb buffer with capacity + expiry; `Absorb(amount)` returns leftover after depleting the buffer; self-removes at capacity 0 or on expiry
    - _Requirements: 14.3_
  - [x] 2.3 Write property test for Shield absorb + Heal clamp math
    - **Property 7: Shield absorbs before health and never increases health** — **Validates: Requirements 14.3**
    - **Property 8: Heal clamps to maximum and never exceeds it** — **Validates: Requirements 13.3**
    - Extract absorb/heal math into plain C#; FsCheck/CsCheck, min 100 iterations; tag `// Feature: enemy-swarm-core-archetypes, Property 7 / Property 8`
    - _Requirements: 13.3, 14.3_

- [x] 3. Telegraph clock and single-beat resolver (pure logic)
  - [x] 3.1 Extract telegraph windup-clock and single-beat resolver into plain C#
    - Plain classes modeling the windup floor (min 0.25s, controlling attacks ≥ non-controlling), monotonic intensity fraction 0→1, and a single-beat resolver over a union of areas, reusable by `EnemyAttackExecution` and behaviors
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6_
  - [x] 3.2 Write property tests for telegraph clock and beat resolver
    - **Property 1: Telegraph windup floor before any damage beat** — **Validates: Requirements 2.1, 2.2, 3.4, 7.2, 8.2, 9.3, 11.3, 14.4**
    - **Property 2: Damage beat is single per telegraphed area** — **Validates: Requirements 2.5, 4.3, 6.4**
    - **Property 4: Telegraph intensity is monotonic** — **Validates: Requirements 2.3, 2.6, 9.3, 10.4**
    - FsCheck/CsCheck, min 100 iterations; tagged per property
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6_

- [x] 4. Reusable EnemyProjectile
  - [x] 4.1 Implement `EnemyProjectile` (straight + arced) state machine
    - Multi-frame travel via clip/segment logic; single overlap damage then mark-for-destruction; range-exhaustion and geometry-block cleanup; same-frame destroy + runtime material release; owner `Died`/`OnDisable` marks all in-flight projectiles for destruction. Straight (Shooter/Spread/Sniper) and Arced (Bomber, ground impact area) paths
    - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6_
  - [x] 4.2 Write property tests for projectile state machine
    - **Property 5: Projectile applies damage at most once, then is destroyed** — **Validates: Requirements 7.4, 7.5, 8.4, 19.2, 19.3, 19.4, 19.5**
    - **Property 6: Owner death cascades to in-flight projectiles** — **Validates: Requirements 19.6**
    - Extract hit/travel/cleanup logic to plain C# with mocked collision; min 100 iterations; tagged per property
    - _Requirements: 19.2, 19.3, 19.4, 19.5, 19.6_

- [x] 5. Checkpoint — foundation primitives compile and pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Attack pipeline extensions for new attack shapes
  - [x] 6.1 Widen `EnemyAttackPatterns.Select` for archetype-scoped selection
    - Add archetype/attack-profile parameter and per-archetype `EngagementRange` bands so `Select` narrows the repertoire (e.g. Sniper → only `SniperShot`) while distance/traits still choose the concrete kind
    - _Requirements: 1.2, 1.3, 7.2, 9.2_
  - [x] 6.2 Add `AimedShot`, `SpreadShot`, `SniperShot` cases to `EnemyCombatActions.Perform`
    - Each reuses `EnemyAttackExecution` telegraph and fires straight `EnemyProjectile`(s): single (AimedShot), ≥3 equally-spaced across fixed cone locked at telegraph start (SpreadShot), aim-line ≥1.0s intensifying (SniperShot); cancel on control-lock / LoS break during windup
    - _Requirements: 7.2, 7.4, 7.6, 8.2, 8.3, 8.4, 8.5, 9.3, 9.5, 9.6_
  - [x] 6.3 Add `LobShot` and `HazardPlace` cases to `EnemyCombatActions.Perform`
    - LobShot: arced `EnemyProjectile` to a ground point with ground-area impact telegraph shown for full flight and intensifying, single area check on land, projectile+telegraph removed on land. HazardPlace: ≥0.25s placement telegraph then activates a `HazardZone`
    - _Requirements: 10.2, 10.3, 10.4, 10.5, 11.3, 11.4_
  - [x] 6.4 Add `HookLine` case to `EnemyCombatActions.Perform`
    - Line telegraph ≥ Grunt melee windup, locked direction; on connect within tolerance, invoke player-side pull (wired in task 9); miss when player beyond hook-line tolerance
    - _Requirements: 12.2, 12.4, 12.5_
  - [x] 6.5 Write unit/example tests for attack-shape edge cases
    - Windup boundary exactly at 0.25s; Sniper vs Shooter range/interval/damage ordering (R9.2/R9.4); Bomber player-outside-impact-area no damage (R10.3); Grunt player-leaves-melee no-damage + stance retained (R4.5)
    - _Requirements: 4.5, 9.2, 9.4, 10.3_

- [x] 7. ArchetypeBehavior base and control-lock gate
  - [x] 7.1 Implement abstract `ArchetypeBehavior` base
    - `[RequireComponent(Actor, EnemyAI)]`; resolves `Actor`, `EnemyAI`, `NavMeshAgent`, `CombatReactionController` in `Awake`; `LogMissingDependencies` naming missing deps; `CanAct` gate (alive, agent enabled + on NavMesh, not control-locked)
    - _Requirements: 1.6, 20.1, 20.2, 20.4, 20.5_
  - [x] 7.2 Write property test for the control-lock gate predicate
    - **Property 11: Control-locked archetype issues no move or attack commands** — **Validates: Requirements 20.1, 20.2, 20.4, 20.5**
    - Extract `CanAct` predicate to plain C#; generate agent/lock states; min 100 iterations; tagged
    - _Requirements: 20.1, 20.2, 20.4, 20.5_

- [x] 8. Role behaviors and support components
  - [x] 8.1 Implement `HazardZone` (fire / electric / slow)
    - Trigger volume + interval timer (not heavy Update); fire/electric damage once per 0.25–1.0s interval while occupied, stop on exit; slow reduces player speed 20–60% on enter and restores exact pre-slow value on exit; lifetime 3–15s removal
    - _Requirements: 11.5, 11.6, 11.7, 11.8, 11.9_
  - [x] 8.2 Write property test for hazard interval/slow model
    - **Property 10: Hazard periodic damage and slow round-trip** — **Validates: Requirements 11.5, 11.6, 11.7, 11.8**
    - Extract interval/slow logic to plain C#; enter-then-exit is speed identity; min 100 iterations; tagged
    - _Requirements: 11.5, 11.6, 11.7, 11.8_
  - [x] 8.3 Implement `HazardCasterBehavior`
    - On cadence, telegraph placement ≥0.25s (via pipeline `HazardPlace`), then activate a `HazardZone` of the serialized variant; gated on `CanAct`
    - _Requirements: 11.1, 11.2, 11.3, 11.4_
  - [x] 8.4 Implement `HealerBehavior`
    - Timer-driven heal (1.0–3.0s) of most-wounded ally within heal range (4–8m) via `Actor.Heal`; beam/tether while channeling; no channel when no wounded ally; flee from player within flee range (2–5m, strictly < heal range) to a NavMesh-reachable point away; stop channel on control-lock / stance break; ally discovery via serialized group refs / filtered overlap (no `FindObjectOfType`)
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 20.3_
  - [x] 8.5 Implement `ShieldSupportBehavior`
    - Channel ≥0.25s telegraph via `EnemyAttackExecution`, stationary during channel; on completion grant a `Shield` to each ally within support range (4–10m); interrupted channel grants nothing
    - _Requirements: 14.1, 14.2, 14.4, 14.5_
  - [x] 8.6 Implement `SpawnerBehavior`
    - Spawn-interval timer; NavMesh-sampled instantiation (Spawn_Point path) of Swarm up to living cap (1–100); track living via each spawned `Actor.Died`, decrement on death; stop on own death; NavMesh sample failure skips + retries next interval
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.8, 16.9_
  - [x] 8.7 Write property test for spawner living-count model
    - **Property 9: Spawner living count never exceeds its cap** — **Validates: Requirements 16.2, 16.3, 16.4, 16.5**
    - Extract living-count model to plain C#; generate spawn/death interleavings; count stays in [0, cap], no spawn at cap, one decrement per death; min 100 iterations; tagged
    - _Requirements: 16.2, 16.3, 16.4, 16.5_
  - [x] 8.8 Implement `FrontalReflector` (Mirror)
    - Holds protected arc (90–180°); cooperates with the damage/projectile path so a player projectile from within the arc applies ≤25% damage, outside-arc and area attacks apply full damage; disabled during stance-break CC, re-enabled on regaining control; persistent arc visual
    - _Requirements: 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8_
  - [x] 8.9 Write property test for frontal-arc reflection math
    - **Property 12: Mirror frontal-arc reflection** — **Validates: Requirements 18.3, 18.4, 18.5, 18.6, 18.7**
    - Extract arc/reflection math to plain C#; min 100 iterations; tagged
    - _Requirements: 18.3, 18.4, 18.5, 18.6, 18.7_
  - [x] 8.10 Implement `PriorityTargetMarker`
    - Persistent tooltip-free indicator added when archetype `IsPriorityTarget`; distinct role-action cue while healing/shielding/spawning; marker + cue removed within the same frame on death
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 13.8, 14.6, 16.7_
  - [x] 8.11 Write property test for priority-marker presence predicate
    - **Property 13: Priority marker exists exactly for alive priority archetypes** — **Validates: Requirements 13.8, 14.6, 16.7, 21.1, 21.2, 21.4**
    - Extract presence predicate to plain C#; min 100 iterations; tagged
    - _Requirements: 13.8, 14.6, 16.7, 21.1, 21.2, 21.4_

- [x] 9. Player-side hook pull
  - [x] 9.1 Add `PlayerActor.BeginExternalPull(target, maxDuration)`
    - Suspend `CharControlScript` agent steering, move the player agent toward the Hooker over a bounded ≤1.5s using `agent.Warp`/`Move` (never raw transform), then restore control; always restore via cleanup even if the Hooker dies mid-pull; end early on Hooker control-lock; wire `HookLine` (task 6.4) to call this on connect
    - _Requirements: 12.3, 12.6, 12.7, 20.1_
  - [x] 9.2 Write property test for hook pull bound/tolerance model
    - **Property 14: Hook pull is bounded and returns control** — **Validates: Requirements 12.3, 12.5, 12.6, 12.7**
    - Extract pull-bound/tolerance model to plain C#; pull ends ≤1.5s and returns control, ends early on control-lock, no displacement beyond tolerance; min 100 iterations; tagged
    - _Requirements: 12.3, 12.5, 12.6, 12.7_

- [x] 10. Checkpoint — pipeline, behaviors, and pull compile and pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. EnemyVariant archetype wiring
  - [x] 11.1 Extend `EnemyVariant` to resolve `EnemyArchetype` and configure
    - Serialized `EnemyArchetype` reference; apply its `EnemyProfile` through existing paths (`SetMaxHealth`, `ConfigureAttack`, `ConfigureAttackTraits`, `ConfigureStance`, `agent.speed`); record `Combat_Role`; ensure role-behavior component exists for non-attack roles; add `PriorityTargetMarker` when `IsPriorityTarget`
    - _Requirements: 1.3, 1.4, 1.5, 21.1_
  - [x] 11.2 Add unresolved/duplicate id safe-default fallback
    - Null/unresolvable/mismatched-id → log error naming the id (or "unassigned") and fall back to a Grunt-equivalent safe default without throwing
    - _Requirements: 1.7_

- [x] 12. Author the 16 EnemyProfile + EnemyArchetype assets
  - [x] 12.1 Author melee-group profiles and archetype assets
    - Create pairwise-distinct `EnemyProfile`s and `EnemyArchetype` assets for Rush, Grunt, Heavy, Charger (distinct movement/health/stagger/stance per design table); ensure no two melee profiles are identical
    - _Requirements: 3.2, 3.5, 4.2, 5.2, 22.3_
  - [x] 12.2 Author ranged/control/support/swarm/special profiles and archetype assets
    - Create `EnemyProfile` + `EnemyArchetype` assets for Shooter, Spread_Shooter, Sniper, Bomber, Hazard_Caster, Hooker, Healer, Shield_Support, Swarm, Spawner, Fragile, Mirror per the design profile shapes and roles
    - _Requirements: 1.1, 7.1, 8.1, 9.1, 9.2, 10.1, 11.1, 12.1, 13.1, 13.2, 14.1, 15.1, 15.2, 16.1, 17.1, 17.2, 18.1_
  - [x] 12.3 Write property test for melee profile distinctness
    - **Property 15: Melee profiles are pairwise distinct** — **Validates: Requirements 22.3**
    - Load the 4 authored melee assets; assert each pair differs in ≥1 defined attribute; min 100 iterations over generated attribute comparisons; tagged
    - _Requirements: 22.3_

- [x] 13. Per-archetype behavior wiring and tuning
  - [x] 13.1 Wire melee archetypes (Rush, Grunt, Heavy, Charger)
    - Rush: chase to ≤2.5m then `Punch` windup < Grunt, hold when no path; Grunt: `Punch` telegraph ≥0.4s baseline; Heavy: `HeavySlam` + `Shockwave` ground-pound ≥0.4s area, high maxStance; Charger: `Charge` locked dir single-hit + ≥1.0s recovery on miss/obstruction
    - _Requirements: 3.1, 3.3, 3.4, 3.6, 4.1, 4.3, 4.4, 5.1, 5.3, 5.4, 5.5, 5.6, 6.1, 6.2, 6.3, 6.5, 6.6_
  - [x] 13.2 Wire ranged archetypes (Shooter, Spread_Shooter, Sniper, Bomber)
    - Map each to its attack kind and engagement band; Shooter standoff reposition via NavMesh; Sniper long range/high damage/long interval; Bomber arced lob with ground telegraph
    - _Requirements: 7.1, 7.2, 7.3, 8.1, 8.2, 9.1, 9.2, 9.4, 10.1, 10.2_
  - [x] 13.3 Wire control, support, swarm, and special archetypes
    - Hazard_Caster → `HazardCasterBehavior`; Hooker → `HookLine`; Healer → `HealerBehavior`; Shield_Support → `ShieldSupportBehavior`; Spawner → `SpawnerBehavior`; Swarm chase retarget ≤0.5s + one-hit death; Fragile stance 1–60 + knockUpRes 0; Mirror → `FrontalReflector`
    - _Requirements: 11.1, 12.1, 13.1, 14.1, 15.1, 15.3, 15.4, 15.5, 16.1, 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 18.1_

- [x] 14. Property 3 interrupt coverage and remaining unit tests
  - [x] 14.1 Write property test for interrupt cancellation
    - **Property 3: Interrupt cancels telegraph and suppresses the beat** — **Validates: Requirements 2.4, 7.6, 8.5, 9.6, 13.7, 14.5**
    - Generate windups with control-lock/stance-break before the beat; assert no damage and same-frame cancel; min 100 iterations; tagged
    - _Requirements: 2.4, 7.6, 8.5, 9.6, 13.7, 14.5_
  - [x] 14.2 Write remaining unit/example tests
    - Charger obstruction recovery (R6.6); Swarm one-hit death and ≤1s removal (R15.3/R15.6); Fragile launch airborne-duration ordering (R17.4)
    - _Requirements: 6.6, 15.3, 15.6, 17.4_

- [x] 15. Integration wiring and PlayMode tests
  - [x] 15.1 Verify end-to-end archetype configuration path
    - Confirm an `EnemyVariant`-configured archetype acts through the real attack coroutine and role behaviors without direct-transform locomotion; resolve any wiring gaps found
    - _Requirements: 1.3, 1.4, 20.1_
  - [x] 15.2 Write PlayMode integration tests (representative examples)
    - Spawner instantiates on a baked NavMesh and caps concurrency; Hooker pull moves the real player agent and returns control; Priority marker appears in-scene and is removed on death; a configured archetype telegraphs then applies a single beat
    - _Requirements: 16.2, 16.8, 12.3, 12.6, 21.1, 21.4, 2.1, 2.5_

- [x] 16. Final checkpoint — full suite green
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP; they are property, unit, and integration tests plus test-only extraction. Core archetype implementation and asset authoring are never optional.
- The MonoBehaviour coordination stays thin; universal invariants are extracted into plain C# so they are testable without a live scene, matching the design's testing strategy.
- Each property test uses FsCheck or CsCheck (do not hand-roll), runs a minimum of 100 iterations, and is tagged `// Feature: enemy-swarm-core-archetypes, Property {n}: {property text}`.
- Every new `.cs` file needs its `.meta` created through the Unity Editor; new `EnemyProfile`/`EnemyArchetype` assets must be authored as Unity assets.
- If Unity's Test Framework cannot be run from the CLI, validation falls back to structure inspection, reference searches, and `git diff` per AGENTS.md, and any unrun tests are reported clearly.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "2.1", "3.1"] },
    { "id": 2, "tasks": ["2.2", "3.2", "7.1"] },
    { "id": 3, "tasks": ["2.3", "4.1", "7.2"] },
    { "id": 4, "tasks": ["4.2", "6.1", "8.1", "8.8", "8.10"] },
    { "id": 5, "tasks": ["6.2", "6.3", "6.4", "8.2", "8.9", "8.11"] },
    { "id": 6, "tasks": ["6.5", "8.3", "8.4", "8.5", "8.6", "9.1"] },
    { "id": 7, "tasks": ["8.7", "9.2", "11.1"] },
    { "id": 8, "tasks": ["11.2", "12.1", "12.2"] },
    { "id": 9, "tasks": ["12.3", "13.1", "13.2", "13.3"] },
    { "id": 10, "tasks": ["14.1", "14.2", "15.1"] },
    { "id": 11, "tasks": ["15.2"] }
  ]
}
```
