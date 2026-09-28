# Implementation Plan: Modifier Synergies (Theme 17)

## Overview

This plan implements the modifier-and-synergy roadmap in incremental, integrated steps grounded in the design document. Work is ordered by the theme-17 priorities: the foundational Priority 1 interactions (R1–R6) come first because they reuse existing systems and eliminate the anti-synergies, followed by Priority 2 spear modifiers (R7), Priority 3 the `HookBus` infrastructure (R8), Priority 4 the Rewrite category (R9), Priority 5 the "system breaking" feedback (R10), and the cross-cutting guarantees (R11) that constrain every layer.

Language: C# for Unity (the design is grounded in the existing C#/Unity codebase; no pseudocode).

Conventions applied throughout: preserve `.meta` files on any asset move/rename/create; no `FindObjectOfType`/`GameObject.Find`/magic strings in gameplay code; keep `MonoBehaviour`s thin and put decision logic in plain C# classes; apply all modifiers to runtime copies only and never mutate source `ScriptableObject` assets.

Property tests reference the numbered Correctness Properties in `design.md`. Test sub-tasks are marked optional with `*`. Property-based tests use a .NET PBT library compatible with the Unity test runner (FsCheck via NUnit EditMode, or CsCheck) — do not hand-roll an engine; each property test runs at least 100 generated cases and carries a `Feature: modifier-synergies-theme17, Property {n}` comment.

## Tasks

- [x] 1. Set up the feature test assembly and shared test seams
  - Create an EditMode test assembly definition under `Assets/_Project/Scripts/Tests/EditMode` (new `.asmdef` referencing the gameplay assembly, NUnit, and the chosen PBT library) and a PlayMode test assembly under `Assets/_Project/Scripts/Tests/PlayMode` for physics-dependent projectile tests
  - Add the PBT library package/reference so property tests can run under the Unity test runner
  - Preserve/create the corresponding `.meta` files for every new folder and `.asmdef`
  - Introduce injectable/seedable randomness seams for the chill-chance roll (`PlayerOnHitEffects`) and crit roll (`PlayerActor`) so RNG-dependent properties are deterministic under test
  - _Requirements: 11.5, 11.6_

- [x] 2. R1 — Piercing + Ricochet ordering fix in `ArsenalProjectile`
  - [x] 2.1 Add the `HasInlineTarget()` helper and reorder hit resolution in `ArsenalProjectile.Update`
    - Add `private bool HasInlineTarget()` that sphere-casts (radius `.12f`) along `transform.forward` over `_remaining` and returns true when an un-hit live `Actor` (not the owner) lies on the straight-line heading, false when solid non-`Actor` scenery blocks the line
    - Reorder the hit block so piercing continuation (`if (_piercing && HasInlineTarget()) continue;`) is evaluated before the ricochet redirect branch; enter ricochet only when nothing remains in-line; keep the `if (_piercing) continue;` fallback after the ricochet branch
    - On ricochet redirect: consume exactly one bounce, multiply `_multiplier` by `0.75f`, redirect toward the nearest un-hit live enemy within 6 units
    - Preserve the existing `_hit.Add(actor)` guard as the single source of truth for no-double-hit, and the terminal `Destroy` path when `_bounces == 0` and `_piercing` is false
    - _Requirements: 1.1, 1.2, 1.2a, 1.3, 1.4, 1.5, 1.6_

  - [x] 2.2 Write property test for pierce-first ordering
    - **Property 1: Pierce-first ordering** — arrow damages and continues straight through every in-line un-hit enemy without consuming a bounce, and redirects (one bounce) only when none remain in-line
    - **Validates: Requirements 1.1, 1.2, 1.4**
    - PlayMode test with generated enemy layouts (physics sweep required)

  - [x] 2.3 Write property test for ricochet damage decay
    - **Property 2: Ricochet damage decay** — after k redirects the multiplier equals initial × 0.75^k
    - **Validates: Requirements 1.3**

  - [x] 2.4 Write property test for no double hit per projectile
    - **Property 3: No double hit per projectile** — each enemy is damaged at most once from a single projectile, for any pierce/ricochet combination
    - **Validates: Requirements 1.6**

  - [x] 2.5 Write example test for terminal destroy
    - Verify the projectile is destroyed when it has zero remaining bounces and piercing is disabled after resolving a hit
    - _Requirements: 1.5_

- [x] 3. R2 — Ignite + Frost → Thermal Shock in `PlayerOnHitEffects`
  - [x] 3.1 Expose `ChillStatus.FreezeThreshold`
    - Promote the freeze threshold in `ChillStatus` from `private const` to `internal const` (or add a static getter) so freeze/shock checks read it without a magic number
    - _Requirements: 2.1, 11.5_

  - [x] 3.2 Add `ReportImpact` to `RunSynergyEffects`
    - Add `public void ReportImpact(Actor target, float damage, PlayerOnHitEffects elements)` that routes a single bounded impact through the existing Resonance amplification math (`1 + 0.4·Resonance·statusCount`) without starting a new cascade and without exceeding `MaxSecondaryHits`; no-op when Resonance rank is 0, target invalid, damage ≤ 0, or already resolving
    - _Requirements: 2.6_

  - [x] 3.3 Implement Thermal Shock coordination in `ApplyElements`
    - Sample pre-existing `BurnStatus`/`ChillStatus` (remaining duration > 0) before applying elements; track whether fire and/or frost actually became active this call
    - Add configurable `_thermalShockMultiplier` (> 0) with `ConfigureThermalShock(float)`; compute `shock = (appliedFire && hadChill) || (appliedFrost && hadBurn)` and call `TriggerThermalShock` at most once per qualifying element application (up to twice per call when both qualify), never chaining beyond that
    - `TriggerThermalShock` applies one instantaneous `TakeDamage(damageDealt × multiplier)`, leaves both statuses present with unchanged durations, then calls `RunSynergyEffects.ReportImpact` for the dealt amount; add a re-entrancy guard so cascade-driven `ApplyElements` calls do not spawn additional shocks
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

  - [x] 3.4 Write property test for at-most-one Thermal Shock per qualifying application
    - **Property 4: Opposite element triggers at most one Thermal Shock**
    - **Validates: Requirements 2.1, 2.2, 2.7**

  - [x] 3.5 Write property test for no shock without an active opposite element
    - **Property 5: No shock without an active opposite element** (drive the chill roll via the seeded RNG seam)
    - **Validates: Requirements 2.3**

  - [x] 3.6 Write property test for Thermal Shock burst magnitude
    - **Property 6: Thermal Shock burst magnitude** — exactly one instantaneous instance of magnitude d·m (> 0), distinct from DoT
    - **Validates: Requirements 2.4**

  - [x] 3.7 Write property test for status preservation
    - **Property 7: Thermal Shock preserves both statuses** — both statuses remain with unchanged durations
    - **Validates: Requirements 2.5**

  - [x] 3.8 Write property test for Resonance amplification of the shock burst
    - **Property 8: Thermal Shock is Resonance-amplified** — burst amplified by 1 + 0.4·r·s as a single bounded impact
    - **Validates: Requirements 2.6**

- [x] 4. R3 — Detonation reacts to elements (combustion and shatter) in `RunSynergyEffects.Resolve`
  - [x] 4.1 Implement combustion and shatter in the detonation branch
    - In the death-explosion branch, read `BurnStatus`/`ChillStatus` presence: frozen (and not the both-case) → shatter effect emitting 3–6 fragments with a distinct visual; burning and not frozen → combustion with radius × 1.3; both present → shatter only, applied exactly once (guarded by `_visited.Add`)
    - Keep the bounded `while` loop, `impact.Depth >= MaxDepth` guard, and one-secondary-hit-per-target rule untouched; every fragment hit still increments `LastSecondaryHits`
    - Confirm Reactor amplification stays in `BurnScaling` (no separate DoT) and that isolated burn ticks in `BurnStatus.Tick` are not wired to `Resolve`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 4.2 Write property test for element-death detonation classification
    - **Property 9: Element-death detonation classification** — combustion (×1.3) vs shatter (3–6 fragments) vs both→shatter-once
    - **Validates: Requirements 3.1, 3.2, 3.3**

  - [x] 4.3 Write property test for cascade bounds invariant (detonation path)
    - **Property 10: Cascade bounds invariant** — ≤ 32 secondary hits, depth ≤ 4, at most one secondary hit per target per original hit, across combustion/shatter
    - **Validates: Requirements 3.4, 7.9, 8.11, 11.3**

  - [x] 4.4 Write property test for Reactor scaling without a new DoT
    - **Property 11: Reactor scales burn without a new DoT** — exactly one `BurnStatus` remains, no extra DoT
    - **Validates: Requirements 3.5**

  - [x] 4.5 Write property test for isolated burn ticks not detonating
    - **Property 12: Isolated burn ticks do not detonate** — `LastSecondaryHits` unchanged by a lone burn tick
    - **Validates: Requirements 3.6**

- [x] 5. Checkpoint — element/cascade interactions
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. R4 — Homing + WideVolley fan-then-home in `ArsenalProjectile` and `ArsenalCombat`
  - [x] 6.1 Refine homing seek and launch-frame behavior in `ArsenalProjectile`
    - Add seek constants (`SeekInterval = .1f`, `SeekRadius = 7f`, `SeekConeDeg = 50f`, `TurnRateDeg = 180f`); initialize `_nextSeek = Time.time + SeekInterval` at spawn so the launch frame applies zero homing correction and the initial heading equals the assigned fan heading
    - At each re-seek use `FindNextTarget(transform.position, 7f, true)` (7-unit radius, 50° forward half-angle) evaluated per-arrow from its own position/forward; steer with `RotateTowards(..., 180 × Time.deltaTime)`; when no target is found, keep the current heading
    - Confirm the sweep sphere cast (radius `.12f`) destroys the arrow at non-`Actor` solid scenery (no wall pass-through), and that TwinShot multiplication in `Fire` spawns arrows that each run their own independent seek
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 6.2 Wire fan headings from `ArsenalCombat` so homing does not override them on launch
    - Ensure each WideVolley arrow is launched along its assigned fan-spread heading in `ArsenalCombat`, with the projectile's first-frame seek suppressed per 6.1
    - _Requirements: 4.1, 4.6_

  - [x] 6.3 Write property test for homing steering constraints
    - **Property 13: Homing steering constraints** — zero launch-frame correction, 7-unit/50° gate, keep heading when no target, ≤ 180°·Δt turn
    - **Validates: Requirements 4.1, 4.2, 4.3**

  - [x] 6.4 Write property test for independent per-arrow seeking
    - **Property 14: Independent per-arrow seeking** — each arrow (incl. TwinShot copies) seeks from its own position/cone and may lock different enemies
    - **Validates: Requirements 4.4, 4.6**

  - [x] 6.5 Write property test for no scenery pass-through
    - **Property 15: No scenery pass-through** — arrow destroyed at scenery impact, never damages an enemy beyond it (PlayMode, physics sweep)
    - **Validates: Requirements 4.5**

- [x] 7. R5 — TripleMoon + Orbit orbital core in `WeaponRunModifiers.Plan` and `RunModifierPresentation`
  - [x] 7.1 Formalize the spear slot-1 plan branches
    - In `WeaponRunModifiers.Plan` slot 1: `Hits += 2 × Rank(TripleMoon)`, `Width *= 1 + .25f × Rank(Orbit)`, `Travel = Rank(Orbit) > 0`; verify each branch (both / TripleMoon-only / Orbit-only) matches the design and that the plan is a fresh per-cast `ArsenalCastPlan` snapshot never assigned back to source assets
    - _Requirements: 5.1, 5.2, 5.3, 5.5_

  - [x] 7.2 Confirm/extend the Orbit combination hint in `RunModifierPresentation`
    - `Hint("weapon_Orbit")` returns "COMBINAÇÃO ATIVA · luas avançam a cada pulso." when TripleMoon is owned, extended to also fire when both appear together in the same offer set
    - _Requirements: 5.4_

  - [x] 7.3 Write property test for spear plan correctness and isolation
    - **Property 16: Spear plan correctness and isolation** — Hits = base + 2·tm, Width = base·(1 + 0.25·o), Travel = (o > 0); repeated generation leaves source assets unchanged
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.5**

  - [x] 7.4 Write example test for the Orbit combination hint
    - Verify hint text appears when both owned or both offered together, and not otherwise
    - _Requirements: 5.4_

- [x] 8. R6 — Haste feeds ComboNova hardening in `CharControlScript` and `RunModifierPresentation`
  - [x] 8.1 Harden the ComboNova proc gate in `CharControlScript.PerformAttack`
    - Keep the proc gated solely on `_runBasicCount % 3 == 0` with `Rank(ComboNova) > 0`; nova applies area damage in a 2.5 m radius equal to `0.6 × rank × weaponDamage`; rank-0 skips the nova and leaves the basic outcome otherwise unchanged
    - Confirm the basic interval divides by `AttackSpeedMultiplier` (via `PlayerActor.GetAttackInterval`) so Haste changes only proc frequency, never which basic procs
    - _Requirements: 6.1, 6.2, 6.3, 6.5_

  - [x] 8.2 Add the Haste→ComboNova combination hint in `RunModifierPresentation`
    - Add a `haste`/`weapon_ComboNova` hint stating Haste accelerates ComboNova, shown only when both are owned or both appear in the same offer
    - _Requirements: 6.4_

  - [x] 8.3 Write property test for the ComboNova proc gate
    - **Property 17: ComboNova proc gate** — nova (radius 2.5, damage 0.6·rank·weaponDamage) fires on exactly every third basic and no other, for any attack-speed multiplier ≥ 0.01
    - **Validates: Requirements 6.1, 6.3**

  - [x] 8.4 Write property test for Haste scaling proc frequency proportionally
    - **Property 18: Haste scales proc frequency proportionally** — interval = base/m, basics-per-window (and procs) scale with m
    - **Validates: Requirements 6.2**

  - [x] 8.5 Write example tests for the hint and rank-0 skip
    - Verify the Haste→ComboNova hint gating and that rank-0 ComboNova fires no nova
    - _Requirements: 6.4, 6.5_

- [x] 9. Checkpoint — Priority 1 complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. R7 — New transformative Spear modifiers (Priority 2)
  - [x] 10.1 Add new `WeaponBoon` members and catalog definitions
    - Add `PhantomSpear`, `MoonShard`, `ReturnWave`, `ChainThrust` to the `WeaponBoon` enum; register catalog `Definition`s (family = Spear) with title, description, max rank, and presentation scope via `RunModifierPresentation.ScopeFor`; inherit offer eligibility from the existing `definition.Family == WeaponModifiers.Family` gate
    - _Requirements: 7.1, 7.7_

  - [x] 10.2 Add `ArsenalCastPlan` fields and consume them in `WeaponRunModifiers.Plan`
    - Add `PhantomDelay` (0 off; 0.2–0.5 when active), `ShardCount` (0 off; clamp 2–6), `ReturnWave` (bool), `ChainThrust` (bool); set them in `Plan` for Thrust/Sweep/slot-3 as designed
    - _Requirements: 7.2, 7.3, 7.4, 7.5, 7.6, 7.8_

  - [x] 10.3 Execute the new spear behaviors in `ArsenalCombat`
    - PhantomSpear: schedule a delayed spectral thrust repeat after `PhantomDelay`
    - MoonShard: fire `ShardCount` (2–6) projectiles from sweep extremities via `ArsenalProjectile.Fire`
    - ReturnWave: add a projectile `_returning` flag that flips the wave heading toward the player at max range
    - ChainThrust: on a thrust hit, search within 6 m for a *different* enemy and create exactly one short chain thrust; create nothing when none exists
    - Route all secondary hits produced here through the existing damage/cascade path so they stay within `MaxDepth`/`MaxSecondaryHits`; write to runtime copies only
    - _Requirements: 7.2, 7.3, 7.4, 7.5, 7.6, 7.8, 7.9_

  - [x] 10.4 Write property test for the MoonShard fragment bound
    - **Property 19: MoonShard fragment bound** — shard count within [2,6] from sweep extremities
    - **Validates: Requirements 7.3**

  - [x] 10.5 Write property test for the ChainThrust conditional
    - **Property 20: ChainThrust conditional** — exactly one chain thrust to a different enemy when one is within 6 m, none otherwise
    - **Validates: Requirements 7.5, 7.6**

  - [x] 10.6 Write property test for cascade bounds across spear secondaries
    - **Property 10: Cascade bounds invariant** applied to phantom/shard/chain secondaries
    - **Validates: Requirements 7.9, 11.3**

  - [x] 10.7 Write example tests for catalog membership and metadata
    - Verify each new modifier has title, description, family = Spear, max rank, presentation scope, and Spear-gated offer eligibility
    - _Requirements: 7.1, 7.2, 7.4, 7.7_

- [x] 11. R8 — Generic combat-event `HookBus` infrastructure (Priority 3)
  - [x] 11.1 Create the `HookBus` plain C# class
    - New `Assets/_Project/Scripts/Core/HookBus.cs` (with `.meta`): events `OnCrit(Actor,float)`, `OnKill(Actor)`, `OnFreeze(Actor)`, `OnBurn(Actor)`, `OnStanceBreak(Actor)`, `OnDash()`, `OnExplosion(Vector3)`; a `HashSet<Actor> _killed` dedupe; `Raise*` methods; per-subscriber exception isolation by iterating `GetInvocationList()` and wrapping each call in try/catch (`Debug.LogException`), never re-raising; `Clear()` nulls every delegate and clears `_killed`
    - Not a `MonoBehaviour`; obtained by combat scripts via `RunBoons` ownership (no `FindObjectOfType`/`GameObject.Find`)
    - _Requirements: 8.1, 8.3, 8.9, 8.10_

  - [x] 11.2 Own, create, and clear the `HookBus` in `RunBoons`
    - `RunBoons` creates the bus per run, exposes it to collaborators, and calls `Clear()` on `OnDestroy` so a previous run's subscribers never fire in a later run
    - _Requirements: 8.9, 11.4_

  - [x] 11.3 Wire OnCrit and OnKill raise-sites in `PlayerActor`
    - In `DealResolvedAttackDamage`, capture `AttackDamageRoll.IsCritical` and raise `OnCrit(enemy, dealt)` after damage is applied; raise `OnKill(enemy)` when `enemy.IsDead` after damage (deduped by the bus)
    - _Requirements: 8.2, 8.3_

  - [x] 11.4 Wire OnBurn and OnFreeze raise-sites in `PlayerOnHitEffects`
    - Raise `OnBurn` when a burn is applied/refreshed; raise `OnFreeze` when chill is applied at/above `FreezeThreshold`
    - _Requirements: 8.4, 8.5_

  - [x] 11.5 Wire OnExplosion, OnStanceBreak, and OnDash raise-sites
    - Raise `OnExplosion(origin)` in the `RunSynergyEffects.Resolve` detonation branch; subscribe to `CombatReactionController.StanceBroken` for player-caused breaks and raise `OnStanceBreak(Actor)` after resolving the owning `Actor`; raise `OnDash()` once per dash from the dash pathway (`AbilityHolder`/`CharControlScript`)
    - Route any hook-triggered secondary hits through `RunSynergyEffects` so totals stay within cascade limits
    - _Requirements: 8.6, 8.7, 8.8, 8.11_

  - [x] 11.6 Write property test for OnKill dedupe
    - **Property 21: OnKill fires once per enemy** — even with repeat notifications
    - **Validates: Requirements 8.3**

  - [x] 11.7 Write property test for hook exception isolation
    - **Property 22: Hook exception isolation** — every subscriber invoked; no exception propagates to the raiser
    - **Validates: Requirements 8.10**

  - [x] 11.8 Write example tests for hook wiring
    - Verify each event exposes 0..n subscribers and each raise-site fires with the correct payload
    - _Requirements: 8.1, 8.2, 8.4, 8.5, 8.6, 8.7, 8.8_

- [x] 12. Checkpoint — spear modifiers and HookBus
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. R9 — Rewrite reward category (Priority 4)
  - [x] 13.1 Add the Rewrite category flag and presentation in `RunModifierPresentation`
    - Add `IsRewrite` and classify ability-replacing ids (currently `transform`) as `Rewrite`; reuse the "TRANSFORMAÇÃO" label/accent for the category presentation
    - _Requirements: 9.1, 9.2_

  - [x] 13.2 Implement Rewrite offer rules in `RunBoons.OfferReward`
    - At most one Rewrite per offer set; gate appearance behind a ≤ 20% roll per offer set; exclude the Rewrite when no valid target ability exists; grant via the existing runtime `ArsenalAbility` copy (`ConfigureRunTransformation`) with no asset mutation; keep single-use exclusion after taken via `IsSingleUse("transform")`
    - _Requirements: 9.3, 9.4, 9.5, 9.6_

  - [x] 13.3 Write property test for the Rewrite offer constraint
    - **Property 23: Rewrite offer constraint** — at most one Rewrite per set; empirical appearance ≤ 20% within tolerance
    - **Validates: Requirements 9.3**

  - [x] 13.4 Write example tests for Rewrite classification, presentation, target-gating, and single-use
    - Verify classification/label, no-target exclusion, runtime-copy grant, and post-take exclusion
    - _Requirements: 9.1, 9.2, 9.5, 9.6_

- [x] 14. R10 — "System breaking" escalating feedback (Priority 5)
  - [x] 14.1 Create the `SystemBreakState` plain C# class
    - New `Assets/_Project/Scripts/UI/SystemBreakState.cs` (with `.meta`): thresholds `{3,6,9}`, `Tier` (0..3), `Evaluate(int interactingCount)` that sets the tier and presents feedback on crossing (glitch visuals / weapon comments / fake system messages) with higher intensity/frequency and a defined ordering at higher tiers, `Reset()`; cosmetic only (never touches damage/cascade/reward rules); null-guard missing tier assets so a missing bundle is skipped without throwing; keep gameplay-critical UI legible
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.6, 10.7_

  - [x] 14.2 Own, drive, and reset `SystemBreakState` in `RunBoons`
    - `RunBoons` creates it per run, recomputes interacting-modifier count from `RunBoons.Acquired` on `RewardChosen`, calls `Evaluate`, and calls `Reset()` on `OnDestroy`
    - _Requirements: 10.5, 11.4_

  - [x] 14.3 Write property test for tier mapping and monotonicity
    - **Property 24: Escalation tier mapping and monotonicity** — tier = count of thresholds ≤ c; intensity/frequency non-decreasing with tier
    - **Validates: Requirements 10.1, 10.3**

  - [x] 14.4 Write example tests for feedback presentation and missing-asset skip
    - Verify feedback selection, cosmetic-only behavior, and that a missing tier bundle is skipped without error
    - _Requirements: 10.2, 10.4, 10.7_

- [x] 15. R11 — Cross-cutting guarantees and atomic run-end clear
  - [x] 15.1 Implement atomic run-end clear in `RunBoons.OnDestroy`
    - Clear `PlayerOnHitEffects`, `RunSynergyEffects` ranks, `HookBus` subscriptions, and `SystemBreakState` together as a single operation so all run-scoped state is cleared at once
    - _Requirements: 11.4_

  - [x] 15.2 Confirm runtime-copy isolation and cascade bounds across all new paths
    - Verify every new/modified reward writes only to `WeaponRunModifiers` ranks, per-cast `ArsenalCastPlan` snapshots, runtime `PlayerOnHitEffects`/`RunSynergyEffects` state, or the `HookBus`; confirm `MaxDepth = 4` and `MaxSecondaryHits = 32` remain `const`; confirm dependencies resolve via serialized references / `TryGetComponent` / `RunBoons` ownership with no `FindObjectOfType`/`GameObject.Find`/magic strings
    - _Requirements: 11.1, 11.3, 11.5_

  - [x] 15.3 Extend editor asset-isolation validation
    - Extend the existing validation editor tooling (pattern from `ArsenalValidation`/`WeaponModifierValidation`) to assert no code path assigns into a source weapon/ability/status `ScriptableObject`; preserve `.meta` files for any touched editor script
    - _Requirements: 11.2, 11.6_

  - [x] 15.4 Write property test for asset isolation
    - **Property 25: Asset isolation** — applying any new/modified reward leaves every original weapon/ability/status asset unchanged
    - **Validates: Requirements 11.1**

  - [x] 15.5 Write property test for run-end residual state
    - **Property 26: Run-end leaves no residual state** — element registry emptied, cascade ranks zeroed, HookBus subscriptions removed, escalation tier reset; no prior-run subscriber fires afterward
    - **Validates: Requirements 8.9, 10.5, 11.4**

  - [x] 15.6 Write property test for the global cascade bounds invariant
    - **Property 10: Cascade bounds invariant** — end-to-end across any modifier combination, hook subscriber, and spear/element effect
    - **Validates: Requirements 3.4, 7.9, 8.11, 11.3**

- [x] 16. Integration and smoke coverage
  - [x] 16.1 Write integration tests for combined interactions
    - Detonation-with-elements end-to-end (combustion/shatter), a full fan-then-home WideVolley, and pierce-then-ricochet on a generated layout
    - _Requirements: 1.1, 3.1, 3.2, 4.1, 4.6_

  - [x] 16.2 Run EditMode/PlayMode tests via the Unity CLI test runner; if unavailable, validate structurally
    - Attempt the Unity batch-mode test runner. Where the CLI cannot run tests, validate via reference searches, project-structure inspection, and `git diff` (inspecting `.unity`, `.prefab`, `.asset`, and `.meta` references where relevant), and explicitly report every test that was not run per AGENTS.md
    - _Requirements: 11.5, 11.6_

- [x] 17. Final checkpoint — full feature
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional test sub-tasks and can be skipped for a faster MVP; core implementation sub-tasks are never marked optional.
- Each task references specific requirement clauses (not just user stories) for traceability, and each property test names the design Property it implements.
- Property-based tests use a Unity-compatible .NET PBT library (FsCheck via NUnit EditMode, or CsCheck) at ≥ 100 cases per property, tagged `Feature: modifier-synergies-theme17, Property {n}`; RNG-dependent logic is driven through seedable seams, and physics-dependent properties run in PlayMode.
- Where the Unity CLI cannot execute tests, validation falls back to reference searches, structure inspection, and `git diff`, with any un-run tests reported explicitly (per AGENTS.md).
- All work stays on runtime copies; `.meta` files are preserved for every created/moved asset; no `FindObjectOfType`/`GameObject.Find`/magic strings in gameplay code.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "3.1", "3.2", "7.1", "8.1", "10.1", "13.1", "14.1"] },
    { "id": 1, "tasks": ["2.2", "2.3", "2.4", "2.5", "3.3", "7.2", "8.2", "10.2", "11.1", "13.2", "14.2"] },
    { "id": 2, "tasks": ["3.4", "3.5", "3.6", "3.7", "3.8", "4.1", "6.1", "7.3", "7.4", "8.3", "8.4", "8.5", "10.3", "11.2", "13.3", "13.4", "14.3", "14.4"] },
    { "id": 3, "tasks": ["4.2", "4.3", "4.4", "4.5", "6.2", "10.4", "10.5", "10.6", "10.7", "11.3", "11.4"] },
    { "id": 4, "tasks": ["6.3", "6.4", "6.5", "11.5", "15.1", "15.3"] },
    { "id": 5, "tasks": ["11.6", "11.7", "11.8", "15.2"] },
    { "id": 6, "tasks": ["15.4", "15.5", "15.6"] },
    { "id": 7, "tasks": ["16.1"] },
    { "id": 8, "tasks": ["16.2"] }
  ]
}
```
