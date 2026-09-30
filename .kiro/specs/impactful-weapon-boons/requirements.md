# Requirements Document

## Introduction

This feature adds a set of **impactful, playstyle-changing weapon boons** to Tech-Guy's per-run reward system. The goal is fun over numbers: today many boons are flat stat nudges (movement speed, cooldown reduction) or small geometry tweaks (Q advance distance, ability range), which do not meaningfully change how a weapon *feels* to play. This feature introduces boons that reshape each weapon family's rhythm and decision-making, so that picking one visibly changes the way the player fights rather than just inflating a number.

The work is grounded in the systems already in the codebase and reuses their extension seams rather than inventing new architecture:

- `Assets/_Project/Scripts/Weapons/WeaponRunModifiers.cs` — the `WeaponBoon` enum, the family `Catalog`, per-cast `Plan(ArsenalAbility, slot)` and `GauntletSteps(BreakerGauntletAbility, slot)` snapshot builders, `MeleeScale`, and `DirectDamageMultiplier`. This is where family boons turn ranks into behavior on a per-cast runtime snapshot (`ArsenalCastPlan`), never on the source asset.
- `Assets/_Project/Scripts/Core/RunBoons.cs` — the reward pool, offer selection, `Choose` application, single-use rules, and run-scoped teardown. Weapon boons appear automatically through the family gate in `OfferReward`.
- `Assets/_Project/Scripts/Core/HookBus.cs` — the per-run combat-event bus (`OnCrit`, `OnKill`, `OnFreeze`, `OnBurn`, `OnStanceBreak`, `OnDash`, `OnExplosion`), owned by `RunBoons.Hooks`, exception-isolated, cleared on run end.
- `Assets/_Project/Scripts/Characters/Combat/PlayerOnHitEffects.cs` and `RunSynergyEffects.cs` — the on-hit element registry (burn/chill/Thermal Shock) and the bounded cascade engine (`MaxDepth = 4`, `MaxSecondaryHits = 32`).
- `Assets/_Project/Scripts/Abilities/AbilityHolder.cs` — the Q/W/E/R slot driver, cooldown/active state, `RefreshLoadout`, and dash.
- `Assets/_Project/Scripts/Characters/Player/Stats/PlayerArpgStats.cs` — the additive/multiplicative stat model that `RunBoons.AddStat` hooks into (used only for the conditional/reactive stat behavior in this feature, not for flat stat picks).

The three weapon families are `Bow`, `Spear`, and `Gauntlet` (`RunWeaponFamily`), resolved by `WeaponRunModifiers.Identify`. Each new boon in this feature targets exactly one family and is designed to reinforce that family's identity: the Bow as kiting burst and projectile mastery, the Spear as spacing and reach control, the Gauntlet as aggressive momentum and combo pressure.

Numeric balancing is not the objective; the objective is behavior, feel, and clear on-screen feedback. All values in the acceptance criteria are the intended starting points and may be tuned during implementation.

## Glossary

- **Run_Modifier_System**: The per-run reward system owned by `RunBoons`. Boons last only for the current incursion, stack by pick, and never write to original weapon/ability/status assets.
- **Weapon_Boon**: A family-specific boon defined by a `WeaponBoon` enum value and a `WeaponRunModifiers.Definition` in the `Catalog`, tracked as a per-boon rank in `[1, MaxRank]`.
- **Cast_Plan**: The fresh, mutable per-cast `ArsenalCastPlan` snapshot produced by `WeaponRunModifiers.Plan` (or the cloned `AreaHitStep` list from `GauntletSteps`). All boon behavior is expressed by mutating this snapshot only.
- **Run_Family**: The equipped weapon family (`Bow`, `Spear`, or `Gauntlet`) resolved by `WeaponRunModifiers.Identify`.
- **Direct_Hit**: A hit from a basic attack, area, or projectile. Discharges, explosions, ricochets, and cascade secondary hits are not direct hits.
- **Hook_System**: The per-run combat-event bus `HookBus`, owned by `RunBoons.Hooks`.
- **Element_System**: The on-hit burn/chill registry in `PlayerOnHitEffects`, plus the `BurnStatus` and `ChillStatus` components on enemies.
- **Cascade_System**: The bounded secondary-hit engine in `RunSynergyEffects`, constrained to `MaxDepth = 4` and `MaxSecondaryHits = 32`, at most one secondary hit per target per original hit.
- **Player**: The `PlayerActor` that owns the run.
- **Enemy**: Any non-player `Actor` that can take damage.
- **Impactful_Boon**: A boon whose effect changes what an ability *does* or how the family *plays*, as opposed to a flat stat increase.

## Requirements

### Requirement 1: Design guardrails for impactful boons (cross-cutting)

**User Story:** As a designer, I want every new boon in this feature to change playstyle and reuse the existing run-scoped, asset-isolated systems, so that the rewards feel fun and never leak between runs or corrupt shared assets.

*Grounding:* `WeaponRunModifiers.Plan`/`GauntletSteps` document immutability, monotonicity, and order-independence contracts; `RunBoons.OnDestroy`/`ClearRunState` tear down run state; `HookBus.Clear` drops subscribers per run.

#### Acceptance Criteria

1. THE Run_Modifier_System SHALL express every new Weapon_Boon's behavior only within the per-cast Cast_Plan snapshot, the per-run element/cascade registries, or run-scoped stat modifiers tagged with `RunBoons` as their source, and SHALL NOT write any new value into a source `WeaponScript`, `Ability`, or status `ScriptableObject` asset.
2. WHEN a Weapon_Boon's rank increases from 1 toward its `MaxRank`, THE Run_Modifier_System SHALL make the boon's primary named quantity (added count, multiplier, radius, or duration) non-decreasing, so that a higher rank is never weaker than a lower rank on that quantity.
3. THE Run_Modifier_System SHALL produce byte-identical Cast_Plan output for any two acquisition orderings that result in the same set of boon ranks, so that boon behavior depends only on the final rank multiset and not on pick order.
4. WHEN a run ends, THE Run_Modifier_System SHALL clear every new Weapon_Boon's run-scoped state (ranks, `HookBus` subscriptions, element/cascade contributions, and stat modifiers) so that no effect from this feature persists into a later run.
5. THE Run_Modifier_System SHALL register each new Weapon_Boon in the `WeaponRunModifiers.Catalog` for exactly one Run_Family, so that it is only ever offered while that family is equipped.
6. THE Run_Modifier_System SHALL NOT require any new flat movement-speed, flat cooldown-reduction, or ability-distance-only boon to satisfy this feature; every new boon SHALL qualify as an Impactful_Boon per the glossary.

### Requirement 2: Bow — Split Arrow on kill (fork into the swarm)

**User Story:** As a bow player, I want my arrows to split into fresh arrows when they kill an enemy, so that clearing a swarm chains into more projectiles and rewards good target selection.

*Grounding:* `HookBus.OnKill` fires at most once per enemy; `ArsenalCastPlan.Arrows` and the projectile system already fan multiple arrows; `RunSynergyEffects` provides the bounded-cascade precedent (`MaxSecondaryHits`).

#### Acceptance Criteria

1. WHILE this Bow boon is active at rank R, WHEN a Direct_Hit from a bow arrow kills an Enemy, THE Run_Modifier_System SHALL spawn R additional arrows from the kill position, each fired outward on a distinct heading.
2. WHEN split arrows are spawned, THE Run_Modifier_System SHALL set each split arrow's damage to 50% of the killing arrow's damage.
3. THE Run_Modifier_System SHALL prevent a split arrow from itself producing further split arrows, so that a single original arrow's kill produces at most one generation of splits.
4. WHILE resolving split arrows, THE Run_Modifier_System SHALL respect the existing bounded-cascade guarantees so that a single game frame cannot exceed the Cascade_System's `MaxSecondaryHits` budget for split-arrow spawns.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Bow`, with a `MaxRank` of 3.

### Requirement 3: Bow — Charged Shot (hold to overpower the next arrow)

**User Story:** As a bow player, I want holding my basic attack to charge a heavier arrow, so that I choose between rapid fire and deliberate high-impact shots instead of just tapping.

*Grounding:* Basic attacks flow through `CharControlScript`/`PlayerActor`; `PlayerArpgStats.RollAttackDamage` and the arrow spawn path already carry a damage value; `AttackSpeedMultiplier` governs basic cadence.

#### Acceptance Criteria

1. WHILE this Bow boon is active, WHEN the Player holds the basic-attack input for a charge time of at least 0.6 seconds before releasing, THE Run_Modifier_System SHALL fire a charged arrow instead of a normal arrow.
2. WHEN a charged arrow is fired at rank R, THE Run_Modifier_System SHALL multiply that arrow's damage by (1 + 0.75 × R) and mark it as piercing for that shot.
3. IF the Player releases the basic-attack input before the charge time elapses, THEN THE Run_Modifier_System SHALL fire a normal (uncharged) arrow with unmodified damage.
4. WHILE a charged arrow is being held, THE Run_Modifier_System SHALL present a visible charge indicator that reaches full state exactly when the charged arrow becomes available.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Bow`, with a `MaxRank` of 3.

### Requirement 4: Spear — Perfect Spacing zone (reward controlled distance)

**User Story:** As a spear player, I want to be rewarded for holding the ideal mid-range distance from enemies, so that spacing becomes an active, skill-expressing part of my playstyle rather than a passive stat.

*Grounding:* `WeaponRunModifiers.DirectDamageMultiplier(distance, targetHealthRatio, playerHealthRatio, elements)` already scales direct-hit damage by distance for `Sniper`/`SpearTip`; the spear identity centers on reach.

#### Acceptance Criteria

1. WHILE this Spear boon is active at rank R, WHEN a Direct_Hit lands on an Enemy whose distance from the Player is within the reward band of 3.5m to 6.5m, THE Run_Modifier_System SHALL multiply that hit's damage by (1 + 0.35 × R).
2. WHEN a Direct_Hit lands on an Enemy outside the 3.5m to 6.5m band, THE Run_Modifier_System SHALL apply no Perfect-Spacing bonus to that hit.
3. WHILE the Player has landed Perfect-Spacing hits on consecutive Direct_Hits, THE Run_Modifier_System SHALL present escalating on-screen feedback that communicates the active spacing reward, and SHALL reset that feedback when a Direct_Hit lands outside the band.
4. THE Run_Modifier_System SHALL derive this bonus within the direct-hit damage path only and SHALL NOT write the multiplier back to the source spear ability or weapon assets.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Spear`, with a `MaxRank` of 3.

### Requirement 5: Spear — Impaling Line (pin and pull the frontline)

**User Story:** As a spear player, I want my thrust to skewer enemies in a line and drag them, so that a single well-aimed thrust reorganizes the battlefield instead of just poking one target.

*Grounding:* `ArsenalCastPlan` carries thrust fields (`Hits`, `Range`, `Width`, `Damage`) consumed by the spear thrust path; `AreaHitStep.pushDistance` demonstrates the displacement precedent used by `StanceCrusher`.

#### Acceptance Criteria

1. WHILE this Spear boon is active, WHEN the spear thrust ability is cast, THE Run_Modifier_System SHALL cause the thrust to damage every Enemy along its line rather than stopping at the first Enemy.
2. WHEN a thrust cast with this boon at rank R connects with one or more enemies, THE Run_Modifier_System SHALL pull each connected Enemy toward the Player by a displacement that increases with R.
3. WHILE displacing pulled enemies, THE Run_Modifier_System SHALL move enemies only through their existing locomotion/displacement channel and SHALL NOT teleport an Enemy through solid scenery.
4. THE Run_Modifier_System SHALL apply this behavior only to the per-cast thrust Cast_Plan snapshot and SHALL NOT modify the source ability asset.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Spear`, with a `MaxRank` of 3.

### Requirement 6: Gauntlet — Momentum Stacks (aggression builds power)

**User Story:** As a gauntlet player, I want landing hits without taking damage to build a stacking power bonus, so that staying aggressive and untouched is the core of my playstyle.

*Grounding:* `HookBus` exposes combat events; `PlayerArpgStats` supports run-scoped stacking modifiers via `AddModifier`/`RemoveModifiersFrom`; the gauntlet identity is aggressive close-range pressure.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active at rank R, WHEN the Player lands a Direct_Hit, THE Run_Modifier_System SHALL add one Momentum stack, up to a maximum of 10 stacks.
2. WHILE the Player holds one or more Momentum stacks, THE Run_Modifier_System SHALL increase the Player's outgoing damage by (2 × R)% per stack, applied through the run-scoped stat pipeline.
3. WHEN the Player takes damage from an Enemy, THE Run_Modifier_System SHALL reset the Player's Momentum stacks to zero.
4. WHILE Momentum stacks are held, THE Run_Modifier_System SHALL present an on-screen stack indicator that reflects the current stack count and clears to zero when stacks are lost.
5. WHEN a run ends, THE Run_Modifier_System SHALL remove all Momentum-related stat modifiers so that no bonus persists into a later run.
6. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 7: Gauntlet — Shockwave Finisher (combos end in an area burst)

**User Story:** As a gauntlet player, I want my combo finisher to release a shockwave, so that landing a full combo pays off with crowd control and area damage instead of a single last hit.

*Grounding:* `GauntletSteps` clones and appends `AreaHitStep`s (used by `FlurryEcho`/`AsuraEcho`/`ShockRing`), including `hitShape = Sphere`, `sphereRadius`, `stanceDamage`, and `pushDistance`; the gauntlet finisher already runs as the final authored step.

#### Acceptance Criteria

1. WHILE this Gauntlet boon is active, WHEN the final step of a gauntlet combo resolves, THE Run_Modifier_System SHALL append a spherical shockwave step centered on the Player.
2. WHEN the shockwave resolves at rank R, THE Run_Modifier_System SHALL set its radius to a base of 3m scaled by (1 + 0.2 × R) and deal a fraction of the finisher's damage to every Enemy within the radius.
3. WHEN the shockwave connects with an Enemy, THE Run_Modifier_System SHALL apply stance damage and outward push consistent with the existing `AreaHitStep` displacement channel.
4. THE Run_Modifier_System SHALL produce the shockwave by appending a cloned `AreaHitStep` to the per-cast step list and SHALL NOT mutate the source ability's authored steps.
5. THE Run_Modifier_System SHALL offer this boon only WHILE the Run_Family is `Gauntlet`, with a `MaxRank` of 3.

### Requirement 8: Cross-family — Elemental Overflow (spend status for a burst)

**User Story:** As any player who has picked an element, I want to consume the fire or frost stacked on an enemy for an immediate burst, so that elemental setup pays off in a satisfying release that works across all weapons.

*Grounding:* `PlayerOnHitEffects` tracks `BurnStatus`/`ChillStatus` and already fires Thermal Shock when elements meet; `RunSynergyEffects.ReportImpact` routes a single bounded burst through the Resonance-amplified path without starting a new cascade.

#### Acceptance Criteria

1. WHILE this boon is active and the Player has an element (Ignite or Frost) enabled, WHEN a Direct_Hit lands on an Enemy that already carries an active `BurnStatus` or `ChillStatus`, THE Run_Modifier_System SHALL trigger an Elemental Overflow burst on that Enemy.
2. WHEN an Elemental Overflow burst triggers at rank R, THE Run_Modifier_System SHALL deal instantaneous damage equal to (0.5 × R) times the triggering hit's damage as a single bounded impact, without removing the underlying `BurnStatus` or `ChillStatus`.
3. THE Run_Modifier_System SHALL route the Elemental Overflow burst through the existing bounded-impact path so that it is eligible for Resonance amplification and cannot exceed the Cascade_System's per-frame secondary-hit budget.
4. IF the struck Enemy carries neither an active `BurnStatus` nor an active `ChillStatus`, THEN THE Run_Modifier_System SHALL NOT trigger an Elemental Overflow burst for that hit.
5. THE Run_Modifier_System SHALL offer this boon regardless of Run_Family, with a `MaxRank` of 3.

### Requirement 9: Presentation and offering integration

**User Story:** As a player choosing rewards, I want these impactful boons to appear in the reward cards with clear, evocative descriptions and combination hints, so that I understand how each one changes my playstyle before I pick it.

*Grounding:* `RunBoons.OfferReward` builds offers from the family `Catalog` via the family/rank gate and formats rank text as `"Nível X/Y\n<description>"`; `RunModifierPresentation` supplies display metadata and combination hints.

#### Acceptance Criteria

1. WHEN the Run_Modifier_System offers a reward set WHILE a Run_Family is equipped, THE Run_Modifier_System SHALL make the new boons for that family eligible to appear through the existing family/rank offer gate.
2. WHERE a new Weapon_Boon has a current rank below its `MaxRank`, THE Run_Modifier_System SHALL present its reward card with a Portuguese title and description and a rank indicator in the existing `"Nível X/Y"` format.
3. WHEN a new boon has a meaningful synergy with an already-owned or co-offered boon, THE Run_Modifier_System SHALL surface a combination hint consistent with the existing hint presentation.
4. THE Run_Modifier_System SHALL NOT alter the number of choices presented per reward set as a result of adding these boons.
