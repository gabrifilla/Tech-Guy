# Implementation Plan: Ranged Kiting and Attack Telegraph Overhaul

## Overview

This plan converts the design into incremental, test-driven coding steps. It follows the project's established separation: all decision/math lives in pure, seeded C# classes (mirroring `PreferredDistanceResolver`/`TelegraphBeat`) that are built and property-tested **before** the `EnemyAI` / `EnemyAttackExecution` / `EnemyProjectile` MonoBehaviour integration that drives them. This guarantees the project always compiles, no code is orphaned, and each integration step wires previously built logic into the existing flow.

Pure behavior classes live under `Assets/_Project/Scripts/Characters/Enemy`; the telegraph fill/color types extend the existing `Assets/_Project/Scripts/Characters/Enemy/TelegraphBeat.cs` in place; `CombatGroundFill` lives under `Assets/_Project/Scripts/Effects` beside `CombatGroundRing`. Property tests go in `Assets/_Project/Scripts/Tests/EditMode/Editor` (asmdef/namespace `TechGuy.Tests.EditMode`, NUnit `[Test]`), using the seeded `PropertyCheck.ForAll` harness (≥100 cases, counterexample reporting) — FsCheck/CsCheck are **not** available on this machine. Example/edge files use `*ExampleTests.cs` / `*Tests.cs`. `.meta` files are preserved on existing files; new files receive new `.meta` from Unity. Movement stays NavMesh-only; all existing perception gates and the attack-animation guard are preserved.

Implementation language: **C#** (the design is fully specified in C#; no pseudocode).

## Tasks

- [x] 1. Natural-behavior pure logic (facing, reaction, cadence, approach, patrol, blend)
  - [x] 1.1 Implement `EnemyFacing` pure turn-step resolver
    - Create `Assets/_Project/Scripts/Characters/Enemy/EnemyFacing.cs` (global namespace, `static class`)
    - Add `MinAngularSpeed=90`, `MaxAngularSpeed=1440`, `FacingEpsilonDegrees=0.5` constants and `ClampAngularSpeed`
    - Implement `StepTowards(currentForward, targetDir, angularSpeed, dt)` using `Vector3.RotateTowards`-style clamping so the step never exceeds the remaining angle (no overshoot, no sign flip); return `currentForward` unchanged when `targetDir.sqrMagnitude <= Mathf.Epsilon`
    - Implement `IsFacing(currentForward, targetDir)` true once remaining signed angle ≤ `FacingEpsilonDegrees`
    - _Requirements: 10.1, 10.2, 10.3, 10.5, 10.6, 16.1_

  - [x] 1.2 Write property test for `EnemyFacing`
    - File `EnemyFacingConvergencePropertyTests.cs`, namespace `TechGuy.Tests.EditMode`
    - **Property 1: Facing converges without overshoot**
    - **Validates: Requirements 10.1, 10.3, 10.4, 10.6, 16.3**
    - Use `PropertyCheck.ForAll` (≥100 cases): assert `|angle|` non-increasing, no sign flip, and `IsFacing` true at ≤0.5°

  - [x] 1.3 Implement `ReactionGate` pure reaction-delay/sight-loss-reset state
    - Create `Assets/_Project/Scripts/Characters/Enemy/ReactionGate.cs` (`sealed class`)
    - Expose `Reacted`, constants `MinReactionDelay=0`/`MaxReactionDelay=2`/`MinSightLossReset=0`/`MaxSightLossReset=10`, `Tick(inSight, dt, reactionDelay, sightLossReset)`, and `Reset()`
    - Accrue reaction time while continuously in sight; re-arm after continuous out-of-sight ≥ `sightLossReset`; `reactionDelay==0` reacts on the first in-sight frame; defensively clamp inputs
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 16.1, 16.2_

  - [x] 1.4 Write property test for `ReactionGate`
    - File `ReactionGateDelayPropertyTests.cs`
    - **Property 2: Reaction gate blocks until delay, then opens, and re-arms after sight-loss-reset**
    - **Validates: Requirements 11.1, 11.3, 11.4, 11.5, 11.6, 16.1, 16.2**

  - [x] 1.5 Implement `CadenceJitter` pure jittered-interval resolver
    - Create `Assets/_Project/Scripts/Characters/Enemy/CadenceJitter.cs` (`static class`)
    - Add `MinJitter=0`/`MaxJitter=0.5` constants, `ClampJitter`, and `Effective(baseInterval, jitterFraction, floor, System.Random rng)`
    - Sample uniformly in `[base*(1-j), base*(1+j)]`, floor at `max(floor, Epsilon)`, always `> 0`; return `baseInterval` exactly when `jitterFraction==0`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 16.1, 16.2_

  - [x] 1.6 Write property test for `CadenceJitter`
    - File `CadenceJitterBandPropertyTests.cs`
    - **Property 3: Cadence jitter stays in band, respects floor, is positive, and is deterministic per seed**
    - **Validates: Requirements 12.2, 12.3, 12.4, 12.5, 16.2, 16.4**

  - [x] 1.7 Implement `ApproachOffset` pure bounded-displacement resolver
    - Create `Assets/_Project/Scripts/Characters/Enemy/ApproachOffset.cs` (`sealed class`)
    - Add `MinMagnitude=0`/`MaxMagnitude=4`/`MinRefresh=0.1`/`MaxRefresh=10` constants, `Tick(dt, magnitude, refreshInterval, System.Random rng)`, `Reset()`
    - Refresh to a new seeded ground-plane sample when the interval elapses, otherwise hold the prior sample; magnitude of returned vector ≤ configured magnitude; `Vector3.zero` when `magnitude==0`; defensively clamp inputs
    - _Requirements: 13.2, 13.3, 13.8, 16.1, 16.2_

  - [x] 1.8 Write property test for `ApproachOffset`
    - File `ApproachOffsetBoundsPropertyTests.cs`
    - **Property 4: Approach offset is bounded, holds between refreshes, suppressed at zero, and NavMesh-sampled**
    - **Validates: Requirements 13.2, 13.3, 13.6, 13.8, 16.2** (off-mesh fallback covered by the `ChasePlayer` example test in 8.3)

  - [x] 1.9 Implement `PatrolPause` pure idle-timer
    - Create `Assets/_Project/Scripts/Characters/Enemy/PatrolPause.cs` (`sealed class`)
    - Expose `IsPaused`, `Begin(minSeconds, maxSeconds, System.Random rng)` (clamp `min>=0`, `max>=min`), `Tick(dt)` (true while pausing), `Clear()`; `max==0` never pauses
    - _Requirements: 14.1, 14.2, 14.3, 14.5, 14.6, 16.1, 16.2_

  - [x] 1.10 Write property test for `PatrolPause`
    - File `PatrolPauseRangePropertyTests.cs`
    - **Property 5: Patrol pause duration lies within the configured range and exits on demand**
    - **Validates: Requirements 14.1, 14.2, 14.3, 14.5, 14.6, 16.2**

  - [x] 1.11 Implement `MovementBlend` pure speed-normalization
    - Create `Assets/_Project/Scripts/Characters/Enemy/MovementBlend.cs` (`static class`)
    - Implement `Normalize(currentSpeed, maxSpeed)` = `currentSpeed/maxSpeed` clamped to `[0,1]`; `maxSpeed<=0` returns 0
    - _Requirements: 15.2, 15.6, 16.1_

  - [x] 1.12 Write property test for `MovementBlend`
    - File `MovementBlendRangePropertyTests.cs`
    - **Property 6: Movement blend is in [0,1]**
    - **Validates: Requirements 15.2, 15.6, 16.5**

- [x] 2. Checkpoint - Ensure all tests pass
  - Ensure all natural-behavior pure-logic tests pass, ask the user if questions arise.

- [x] 3. Kiting / standoff pure logic (window/cooldown + speed/standoff math)
  - [x] 3.1 Implement `KiteController` pure window/cooldown state machine
    - Create `Assets/_Project/Scripts/Characters/Enemy/KiteController.cs` (`sealed class`, `enum Phase { Eligible, Kiting, Cooldown }`)
    - Expose `Current`, `Tick(wantsToKite, dt, retreatWindow, retreatCooldown)`, `Reset()`
    - Enter `Kiting` when `wantsToKite` and `Eligible`; at continuous kite time ≥ `retreatWindow` go `Cooldown` and return `false`; stay `Cooldown` until elapsed ≥ `retreatCooldown` then `Eligible`; `wantsToKite==false` resets to `Eligible` and zeroes the kite timer; deterministic for identical inputs
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 9.1, 9.4_

  - [x] 3.2 Write property test for `KiteController`
    - File `KiteWindowCooldownPropertyTests.cs`
    - **Property 7: Retreat window bounds continuous kiting and cooldown blocks then re-arms**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 9.4**

  - [x] 3.3 Implement `RetreatCadence` pure speed + standoff resolver
    - Create `Assets/_Project/Scripts/Characters/Enemy/RetreatCadence.cs` (`static class`)
    - Add `MinRetreatMultiplier=0.1`/`MaxRetreatMultiplier=0.9`, `ClampRetreatMultiplier`, `RetreatSpeed(chaseSpeed, retreatMultiplier)`, `StandoffDistance(engagementBand, standoffFraction)` = `band*clamp01(fraction)`, `RepositionTarget(standoffDistance, standoffMargin)` = `standoff*clamp(margin,1,2)`
    - _Requirements: 1.1, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 9.1_

  - [x] 3.4 Write property test for `RetreatCadence` speed multiplier
    - File `RetreatSpeedMultiplierPropertyTests.cs`
    - **Property 8: Retreat speed multiplier is clamped and applied only while kiting**
    - **Validates: Requirements 1.1, 1.3, 1.4, 1.5**

  - [x] 3.5 Write property test for `RetreatCadence` standoff distance/margin
    - File `StandoffDistanceBandPropertyTests.cs`
    - **Property 9: Standoff distance stays inside the engagement band and the reposition target scales by margin**
    - **Validates: Requirements 2.1, 2.2, 2.3**

- [x] 4. Telegraph pure logic (fill progress + shared red base)
  - [x] 4.1 Extend `TelegraphBeat.cs` with `TelegraphFill`
    - In `Assets/_Project/Scripts/Characters/Enemy/TelegraphBeat.cs`, add `readonly struct TelegraphFill` beside `TelegraphWindupClock`/`TelegraphIntensity` (preserve existing contents and `.meta`)
    - Add `static readonly Color RedBase = new Color(0.9f, 0.1f, 0.1f, 1f)` and `static float ProgressAt(float fraction)` that clamps input to `[0,1]`, is non-decreasing, equals the clamped fraction, and equals 1 when fraction ≥ 1
    - _Requirements: 5.1, 5.2, 6.2, 6.4, 8.4, 8.5, 9.2, 9.3, 9.5_

  - [x] 4.2 Write property test for `TelegraphFill.ProgressAt`
    - File `TelegraphFillProgressPropertyTests.cs`
    - **Property 10: Fill progress is monotonic in [0,1], reaches 1 at impact, and clamps out-of-range input**
    - **Validates: Requirements 6.2, 6.4, 8.4, 8.5, 9.3, 9.5**

  - [x] 4.3 Write property test for red base color, windup floor, and single-beat preservation
    - File `TelegraphRedBasePropertyTests.cs`
    - **Property 11: Telegraph red base is applied for every attack type, with the single-beat rule and min-windup floor preserved**
    - **Validates: Requirements 5.1, 5.2, 5.3, 8.1, 8.2, 8.6**
    - Assert `TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction)` lerps red→white by `0.65*clamp01(fraction)`, `TelegraphWindupClock(authored).Duration == max(0.25, authored)`, and `SingleBeatResolver` resolves at most one beat

  - [x] 4.4 Write determinism property test across all pure logic
    - File `PureLogicDeterminismPropertyTests.cs`
    - **Property 13: Determinism of all pure logic for identical inputs and seeds**
    - **Validates: Requirements 9.4, 16.1, 16.2**
    - Cover `EnemyFacing`, `ReactionGate`, `CadenceJitter`, `ApproachOffset`, `PatrolPause`, `KiteController`, `RetreatCadence`, `TelegraphFill` with identical (inputs, seed) producing identical outputs

- [x] 5. Checkpoint - Ensure all tests pass
  - Ensure all kiting and telegraph pure-logic tests pass, ask the user if questions arise.

- [x] 6. Implement `CombatGroundFill` filled-surface renderer
  - [x] 6.1 Create `CombatGroundFill` MonoBehaviour helper
    - Create `Assets/_Project/Scripts/Effects/CombatGroundFill.cs` beside `CombatGroundRing.cs`
    - Mirror `CombatGroundRing.Create(parent, label, color)`; render a procedural mesh (Lane rect, Cone fan incl. apex, Circle/Ring segments) beneath the ring with a URP/Unlit transparent material
    - Implement `SetArea(in EnemyAttackArea area, float progress)` building the fill mesh from `EnemyAttackArea.Outline()` scaled by progress, `SetColor(Color)`, and `Clear()` (removes within same frame)
    - At `progress==1` the fill extent matches the area's exact shape parameters used by the damage test
    - _Requirements: 6.1, 6.3, 6.4, 7.1_

  - [x] 6.2 Write example test for `CombatGroundFill` mesh-per-shape
    - File `CombatGroundFillMeshExampleTests.cs`
    - **Property 12: Filled telegraph is confined to the attack area** (validated by example: build a mesh for Circle, Ring, and Cone; assert vertex count/bounds and that sampled interior/boundary points return true from `EnemyAttackArea.Contains`, none outside)
    - Include zero-windup single-frame fill at `Fill_Progress==1`
    - **Validates: Requirements 6.1, 6.3, 6.4, 6.5, 7.1**

- [x] 7. `EnemyAI` configuration and per-enemy state
  - [x] 7.1 Add serialized `KitingConfig`/`NaturalBehaviorConfig` and `OnValidate` clamping
    - In `Assets/_Project/Scripts/Characters/Enemy/EnemyAI.cs`, add `[Serializable] struct KitingConfig` (RetreatSpeedMultiplier 0.5, StandoffFraction 0.45, StandoffMargin 1.15, RetreatWindow 3, RetreatCooldown 2) and `[Serializable] struct NaturalBehaviorConfig` (AngularSpeed 540, ReactionDelay 0.3, SightLossReset 1.0, CadenceJitter 0.15, ApproachOffset 1.5, ApproachRefresh 1.0, PatrolPauseMin 0.5, PatrolPauseMax 1.5) as `[SerializeField] private` instances
    - Convert the existing `private const StandoffFraction = 0.45f` / `StandoffMargin = 1.15f` into reads from `KitingConfig` (no changes to the attack path signatures)
    - Clamp every field in `OnValidate` with `[Range]`/`[Min]`, enforcing `PatrolPauseMax >= PatrolPauseMin`
    - _Requirements: 1.3, 2.1, 2.2, 2.6, 3.1, 3.2, 3.5, 10.2, 11.2, 11.5, 12.2, 13.2, 13.3, 14.2, 16.6_

  - [x] 7.2 Add per-enemy runtime state and seeded RNG in `Awake`
    - Add `KiteController _kite`, `ReactionGate _reaction`, `ApproachOffset _approach`, `PatrolPause _patrolPause`, and `System.Random _rng` fields
    - Seed `_rng` in `Awake` from a stable per-instance seed (e.g. `GetInstanceID()`); validate required references in `Awake`/`OnValidate`, logging missing config once per enemy (mirror `PreferredDistanceLayer._loggedMissingConfig`)
    - _Requirements: 16.2_

  - [x] 7.3 Store chase speed on `EnemyVariant` for retreat restore
    - In `Assets/_Project/Scripts/Characters/Enemy/EnemyVariant.cs`, store the resolved chase speed (`_chaseSpeed`) in `ApplyProfile` so the kiting layer can restore `agent.speed` on kite exit (preserve existing public signatures)
    - _Requirements: 1.2, 1.4_

- [x] 8. `EnemyAI` natural-behavior integration
  - [x] 8.1 Wire `EnemyFacing` into `FacePlayer()` and `ReactionGate` into `Update()`
    - Replace the instant `transform.rotation = Quaternion.LookRotation(direction)` snap with `EnemyFacing.StepTowards(transform.forward, direction, angularSpeed, Time.deltaTime)`; gate facing-dependent actions on `EnemyFacing.IsFacing`
    - Tick `ReactionGate` right after `UpdatePerception()`; additionally gate chase/attack dispatch on `Reacted`, holding pre-detection behavior during the delay; preserve all existing perception gates
    - _Requirements: 10.1, 10.3, 10.4, 10.5, 11.1, 11.3, 11.4, 11.6, 11.7_

  - [x] 8.2 Write example tests for reaction-delay==0 and facing gate pass-through
    - File `EnemyReactionFacingExampleTests.cs`
    - Reaction-delay==0 reacts same frame; facing within 0.5° permits attack/standoff exactly as instant-snap did
    - _Requirements: 10.4, 11.6_

  - [x] 8.3 Wire `ApproachOffset` into `ChasePlayer()`
    - Compute `desired = player.position + _approach.Tick(dt, magnitude, refresh, _rng)`; `NavMesh.SamplePosition` then `agent.SetDestination(hit.position)`; fall back to `player.position` when off-mesh; NavMesh-only (never write Transform)
    - Suppress the offset while the enemy is kiting/repositioning for standoff (only reached when `TryMaintainStandoff` returned false)
    - _Requirements: 13.1, 13.4, 13.5, 13.6, 13.7, 13.8_

  - [x] 8.4 Write example test for approach magnitude==0 beeline and off-mesh fallback
    - File `ApproachOffsetChaseExampleTests.cs`
    - magnitude==0 chases directly to player; off-mesh refresh falls back to player position
    - _Requirements: 13.6, 13.8_

  - [x] 8.5 Wire `PatrolPause` into `Patrol()`
    - On reaching a walk point, `Begin(...)` then hold position while `Tick` is true; on elapse, select the next walk point via the existing random/ground-check path unchanged; `Clear()` when the player enters sight
    - _Requirements: 14.1, 14.3, 14.4, 14.5, 14.6_

  - [x] 8.6 Write example test for patrol max==0 no pause
    - File `PatrolPauseIntegrationExampleTests.cs`
    - PatrolPauseMax==0 patrols with no idle pause (continuous motion)
    - _Requirements: 14.6_

  - [x] 8.7 Wire `CadenceJitter` into `TelegraphedAttack()` and `MovementBlend` into `SetMovementAnimation()`
    - Set `nextAttackTime = Time.time + CadenceJitter.Effective(Max(_attackRecovery, timeBetweenAttacks), jitter, Max(_attackRecovery, timeBetweenAttacks), _rng)` so the floor preserves the existing recovery guarantee
    - In `SetMovementAnimation()`, preserve the attack-animation guard; otherwise set the animator blend parameter to `MovementBlend.Normalize(agent.velocity.magnitude, agent.speed)`; fall back to the existing Idle/Walk binary when the blend parameter is missing; skip when animator/agent is null
    - _Requirements: 12.1, 12.4, 12.5, 15.1, 15.2, 15.3, 15.4, 15.5_

  - [x] 8.8 Write example tests for jitter==0 deterministic cadence and animation guard
    - File `CadenceAnimationGuardExampleTests.cs`
    - jitter==0 schedules at base cadence; movement animation does not override attack animation during windup/recovery; missing blend param falls back to Idle/Walk
    - _Requirements: 12.5, 15.3, 15.4_

- [x] 9. `EnemyAI` kiting / standoff integration
  - [x] 9.1 Wire `KiteController` + `RetreatCadence` + `EnemyVariant` speed into `TryMaintainStandoff()`
    - Compute `engagementBand` as today; derive `standoff`/`repositionTarget` from `RetreatCadence` (replacing the removed consts and inline distance); consult `_kite.Tick(wantsToKite, dt, window, cooldown)` before issuing a retreat destination
    - On entering `Kiting` set `agent.speed = RetreatCadence.RetreatSpeed(chase, mult)`; on exit restore `agent.speed = chase` within one frame from the stored `EnemyVariant` chase speed; skip firing while kiting; destination is a point along the player→enemy vector restoring distance to within ±0.5 of the reposition target
    - NavMesh-only via `NavMesh.SamplePosition`→`SetDestination`; on sample failure retain position and skip firing; on unavailable agent fall through to existing non-standoff behavior; non-ranged archetypes untouched
    - _Requirements: 1.1, 1.2, 1.4, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 9.2 Write example tests for kiting speed swap/restore and non-ranged pass-through
    - File `KitingStandoffIntegrationExampleTests.cs`
    - Entering kiting sets retreat speed; exiting restores chase speed within one frame; non-ranged archetype produces identical movement/attack timing
    - _Requirements: 1.2, 1.4, 4.4_

- [x] 10. Checkpoint - Ensure all tests pass
  - Ensure all natural-behavior and kiting integration tests pass, ask the user if questions arise.

- [x] 11. Telegraph integration (red fill in execution + projectile recolor)
  - [x] 11.1 Wire `CombatGroundFill` + red base into `EnemyAttackExecution.Execute(...)`
    - In `Assets/_Project/Scripts/Characters/Enemy/EnemyAttackExecution.cs`, create one `CombatGroundFill` per telegraphed area alongside each existing `CombatGroundRing`
    - In the windup loop call `SetArea(area, TelegraphFill.ProgressAt(fraction))` and `SetColor(TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction))`; add `[SerializeField] private bool _showFill = true`
    - Force warning rendering effectively on for all routed attacks so previously-suppressed attacks (e.g. straight shot) now render; skip telegraph rendering (and proceed) when no valid `EnemyAttackArea` resolves
    - Clear both ring and fill in the same frame in `ClearVisuals()` on impact or cancel; preserve `SingleBeatResolver`, the min-windup floor, and cancellability
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 6.4, 6.6, 7.1, 7.2, 7.4, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [x] 11.2 Write example test for forced telegraph coverage and same-frame clear
    - File `TelegraphExecutionIntegrationExampleTests.cs`
    - A previously-unwarned straight shot now renders a red fill; ring+fill are cleared in the same frame on impact and on cancel; no-area attack proceeds without rendering
    - _Requirements: 6.6, 7.2, 7.4, 8.3_

  - [x] 11.3 Recolor `EnemyProjectile` approach telegraph to the shared red base
    - In `Assets/_Project/Scripts/Characters/Enemy/EnemyProjectile.cs`, recolor the `StepArced` approach telegraph base to `TelegraphFill.RedBase`; keep the existing intensify-toward-impact lerp (max intensity at impact)
    - _Requirements: 5.1, 5.2, 7.3_

- [x] 12. Final verification
  - [x] 12.1 Compile and run EditMode tests
    - Refresh/compile the project (resolve any compile errors) and run the `TechGuy.Tests.EditMode` suite; confirm all new property and example tests pass; inspect `git diff` and references in affected `.cs`/`.prefab`/`.asset` files; report any validation or tests that could not be run
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 16.1, 16.2_

  - [x] 12.2 Optional PlayMode smoke test
    - File under `Assets/_Project/Scripts/Tests/PlayMode`; verify the kiting layer sets/restores `agent.speed` on a live `NavMeshAgent` and that a telegraphed attack creates then clears both ring and fill
    - _Requirements: 1.2, 6.6_

  - [x] 12.3 Optional visual telegraph validation
    - Capture a play-mode screenshot confirming every attack type renders a red growing fill confined to its area
    - _Requirements: 5.1, 6.1_

## Notes

- Tasks marked with `*` are optional (tests / purely-visual validation) and can be skipped for a faster MVP; core implementation tasks are never optional.
- Pure logic + its property tests are built before the MonoBehaviour integration that drives it, so the project always compiles and no code is orphaned.
- Each task references specific requirement sub-clauses and, where applicable, the design Correctness Property number it implements.
- Property tests use the seeded `PropertyCheck.ForAll` harness (≥100 cases, counterexample reporting); each carries `// Feature: ranged-kiting-and-attack-telegraph-overhaul, Property {n}` and `// Validates: Requirements X.Y` comments.
- Movement stays NavMesh-only; existing perception gates, the attack-animation guard, the single-beat rule, and the min-windup floor are preserved throughout.
- `.meta` files on existing files are preserved; new files receive new `.meta` from Unity.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.3", "1.5", "1.7", "1.9", "1.11", "3.1", "3.3", "4.1"] },
    { "id": 1, "tasks": ["1.2", "1.4", "1.6", "1.8", "1.10", "1.12", "3.2", "3.4", "3.5", "4.2", "4.3", "4.4", "6.1", "7.1", "7.3"] },
    { "id": 2, "tasks": ["6.2", "7.2"] },
    { "id": 3, "tasks": ["8.1", "8.3", "8.5", "8.7", "9.1", "11.1", "11.3"] },
    { "id": 4, "tasks": ["8.2", "8.4", "8.6", "8.8", "9.2", "11.2", "12.1"] },
    { "id": 5, "tasks": ["12.2", "12.3"] }
  ]
}
```
