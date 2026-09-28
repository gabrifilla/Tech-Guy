# Requirements Document

## Introduction

This feature implements the modifier and synergy roadmap from `Docs/Tech-Guy_Modificadores_e_Sinergias.md`, following the implementation priorities defined in section 17 ("Prioridades sugeridas de implementação"). The goal is to make Tech-Guy's per-run rewards ("modificadores de run") *converse* with one another — generating emergent behavior rather than raw numbers — while preserving the existing bounded-cascade guarantees and the runtime-copy isolation already in place.

The work is grounded in the currently implemented catalog documented in `Docs/Modificadores.md` (46 rewards: 16 general + 30 weapon-specific across Bow/Spear/Gauntlets) and its source files:

- `Assets/_Project/Scripts/Core/RunBoons.cs` — reward pool, offer selection, application, general limits.
- `Assets/_Project/Scripts/Weapons/WeaponRunModifiers.cs` — weapon catalog, ranks, and cast/step plans.
- `Assets/_Project/Scripts/Weapons/ArsenalProjectile.cs` — arrow behavior for Piercing/Ricochet/Homing/TwinShot.
- `Assets/_Project/Scripts/Characters/Combat/PlayerOnHitEffects.cs` — on-hit element registry (burn/chill) and dispatch to synergies.
- `Assets/_Project/Scripts/Characters/Combat/RunSynergyEffects.cs` — bounded hit cascades (Conductor/Detonation/Reactor/Resonance).
- `Assets/_Project/Scripts/Characters/Combat/BurnStatus.cs`, `ChillStatus.cs` — status effects.
- `Assets/_Project/Scripts/Characters/Player/PlayerActor.cs`, `Assets/_Project/Scripts/Abilities/AbilityHolder.cs` — damage pipeline and the `OnAfterAttackHits` dispatch.
- `Assets/_Project/Scripts/UI/RunModifierPresentation.cs` — reward display metadata and combination hints.

The scope is ordered so that **Priority 1 (interactions between existing modifiers)** is the foundational requirement group, with Priorities 2–5 as subsequent groups. Per the design document's philosophy, numeric balancing is explicitly out of scope; the objective is behavior, visibility, and avoiding accidental anti-synergies.

## Glossary

- **Run_Modifier_System**: The overall per-run reward system owned by `RunBoons`. Modifiers last only for the current incursion, stack by pick, and never write to original weapon/ability/status assets.
- **Arrow_System**: The projectile behavior in `ArsenalProjectile` that governs a fired arrow's travel, hits, piercing, ricochet, and homing.
- **Cascade_System**: The bounded secondary-hit engine in `RunSynergyEffects` (Conductor discharges and Detonation explosions), constrained to `MaxDepth = 4` generations and `MaxSecondaryHits = 32` secondary hits, with at most one secondary hit per target per original hit.
- **Element_System**: The on-hit burn/chill registry in `PlayerOnHitEffects`, plus the `BurnStatus` and `ChillStatus` components applied to enemies.
- **Thermal_Shock**: A new reaction that fires when fire and frost meet on the same enemy, instead of one element removing the other.
- **Spear_Plan_System**: The spear cast-plan generation in `WeaponRunModifiers.Plan` and its consumption by the spear abilities.
- **Hook_System**: A new generic combat-event infrastructure that raises named events (OnCrit, OnKill, OnFreeze, OnBurn, OnStanceBreak, OnDash, OnExplosion) and lets run modifiers subscribe with little per-modifier code.
- **Rewrite_Category**: A reward classification for modifiers that fundamentally replace an ability (e.g., the existing `transform`), presented as rare and visually distinct.
- **System_Break_Feedback**: Escalating "system is breaking" presentation (glitch visuals, weapon comments, fake system messages) that intensifies as a build accumulates more interacting modifiers.
- **Direct_Hit**: A hit from a basic attack, area, or projectile, as defined in `Docs/Modificadores.md`. Discharges and explosions are secondary impacts, not direct hits.
- **Run_Family**: The equipped weapon family (`Bow`, `Spear`, or `Gauntlet`) resolved by `WeaponRunModifiers.Identify`.
- **Player**: The `PlayerActor` that owns the run.
- **Enemy**: Any non-player `Actor` that can take damage.

## Requirements

### Requirement 1: Piercing + Ricochet ordering (pierce first, then ricochet)

**User Story:** As a player combining Piercing and Ricochet on the bow, I want an arrow to pierce every enemy in its path first and only then begin ricocheting, so that the two modifiers reinforce each other instead of one canceling the other.

*Grounding:* In `ArsenalProjectile.Update`, the ricochet branch (`if (_bounces > 0)`) currently executes and `return`s before the `if (_piercing) continue;` line, so with both modifiers a ricochet redirects the arrow on the first hit and piercing never applies. Section 15 of the design doc names this exact anti-synergy.

#### Acceptance Criteria

1. WHILE an Arrow_System projectile has both piercing enabled and one or more remaining ricochet bounces, WHEN the projectile hits an un-hit Enemy whose collider lies on the projectile's current straight-line heading, THE Arrow_System SHALL apply damage, add that Enemy to the `_hit` set, and continue traveling straight through without consuming a ricochet bounce.
2. WHEN an Arrow_System projectile with one or more remaining ricochet bounces hits an Enemy, no other un-hit live Enemy lies on its current straight-line heading, and an un-hit live Enemy is found within a 6-unit radius, THE Arrow_System SHALL redirect the projectile toward the nearest such Enemy and consume exactly one ricochet bounce.
2a. IF an Arrow_System projectile with one or more remaining ricochet bounces hits an Enemy, no other un-hit live Enemy lies on its current straight-line heading, and no un-hit live Enemy is found within the 6-unit radius, THEN THE Arrow_System SHALL NOT redirect the projectile and SHALL NOT consume a ricochet bounce, and the projectile SHALL continue on its current heading and be destroyed per criterion 5 when applicable.
3. WHEN an Arrow_System projectile redirects via ricochet, THE Arrow_System SHALL multiply its current damage multiplier by 0.75.
4. WHEN an Arrow_System projectile finishes piercing (no further un-hit Enemy on its straight-line heading) and has one or more remaining ricochet bounces, THE Arrow_System SHALL enter the ricochet phase and target remaining un-hit Enemies within the 6-unit search radius.
5. IF an Arrow_System projectile has zero remaining ricochet bounces and piercing is disabled after resolving a hit, THEN THE Arrow_System SHALL destroy the projectile.
6. THE Arrow_System SHALL never apply damage to the same Enemy more than once from a single projectile, using the existing `_hit` set.

### Requirement 2: Ignite + Frost → Thermal Shock reaction

**User Story:** As a player running both Ignite and Frost, I want applying one element to an enemy already carrying the other to trigger a Thermal Shock reaction, so that mixing fire and ice is the reward instead of the elements deleting each other.

*Grounding:* Today `PlayerOnHitEffects.ApplyElements` applies burn and chill independently; nothing coordinates them. `BurnStatus` and `ChillStatus` are `[DisallowMultipleComponent]` components already discoverable via `GetComponent`. Sections 11 and 15 define the reaction.

#### Acceptance Criteria

1. WHEN the Element_System applies fire to an Enemy that already carries a ChillStatus with remaining duration greater than 0, THE Element_System SHALL trigger Thermal_Shock on that Enemy exactly once for that application.
2. WHEN the Element_System applies frost to an Enemy that already carries a BurnStatus with remaining duration greater than 0, THE Element_System SHALL trigger Thermal_Shock on that Enemy exactly once for that application.
3. IF an element application does not result in that element becoming active on the Enemy (for example, the chill chance roll does not succeed and no ChillStatus is added or refreshed), THEN THE Element_System SHALL NOT trigger Thermal_Shock for that application.
4. WHEN Thermal_Shock triggers, THE Element_System SHALL apply a single instantaneous damage instance to the Enemy, in addition to and without interrupting the ongoing burn and chill status damage, whose magnitude is a configured multiple of the triggering hit's damage that is greater than 0.
5. WHEN Thermal_Shock triggers, THE Element_System SHALL leave both the BurnStatus and the ChillStatus present on the Enemy with their remaining durations unchanged by the trigger, removing neither element.
6. WHERE the Cascade_System has one or more Resonance ranks active, WHEN Thermal_Shock triggers, THE Cascade_System SHALL report the Thermal_Shock instantaneous damage instance through the same impact path that Resonance amplifies, so the burst is eligible for Resonance amplification.
7. WHEN the Element_System processes a single `ApplyElements` call for one Enemy, THE Element_System SHALL trigger Thermal_Shock at most once PER qualifying element application within that call, so a single call that applies both fire (onto an existing chill) and frost (onto an existing burn) MAY trigger Thermal_Shock up to twice, once per qualifying element application, and SHALL NOT chain beyond one trigger per qualifying element application.

### Requirement 3: Detonation reacts to elements (combustion and shatter)

**User Story:** As a player pairing Detonation with fire or frost, I want burning enemies to combust into larger explosions and frozen enemies to shatter, so that elemental preparation visibly changes how deaths cascade.

*Grounding:* `RunSynergyEffects.Resolve` already computes `explode` on death and reads `BurnStatus`/`ChillStatus` presence for Resonance amplification. Sections 11 and 4 (detonation/reactor) define combustion (bigger explosion for burning enemies) and shatter (fragmenting for frozen enemies).

#### Acceptance Criteria

1. WHEN an Enemy that has an active `BurnStatus` dies under the Detonation effect, THE Cascade_System SHALL trigger a combustion explosion whose radius equals the base Detonation explosion radius multiplied by 1.3.
2. WHEN an Enemy that has an active `ChillStatus` dies under the Detonation effect and is not also burning, THE Cascade_System SHALL trigger a shatter effect that presents a distinct shatter visual and emits between 3 and 6 fragment projectiles.
3. IF an Enemy that dies under the Detonation effect has both an active `BurnStatus` and an active `ChillStatus`, THEN THE Cascade_System SHALL apply only the shatter effect (shatter takes precedence over combustion) and SHALL apply it exactly once.
4. WHILE resolving detonation cascades, THE Cascade_System SHALL enforce the cascade bounds MaxDepth=4 and MaxSecondaryHits=32, and SHALL apply at most one secondary hit per Enemy per original hit.
5. WHEN the Reactor modifier amplifies an Enemy's burn, THE Cascade_System SHALL apply the amplification through the existing burn-scaling rule and SHALL NOT create a separate damage-over-time effect.
6. IF a detonation candidate results only from isolated burn ticks (no qualifying death event), THEN THE Cascade_System SHALL NOT initiate a detonation.

### Requirement 4: Homing + WideVolley (fan out, then curve to different enemies)

**User Story:** As a player using WideVolley with Homing, I want the volley to leave in a wide fan and then curve toward different enemies, so that a shotgun-like burst becomes a swarm of tracking arrows.

*Grounding:* WideVolley adds arrows via `plan.Arrows` (fired as a fan in `ArsenalCombat`), and Homing sets `arrow._homing` in `ArsenalProjectile`. Each homing arrow currently seeks the nearest un-hit target via `FindNextTarget`. Sections 5 (Homing + WideVolley) and 12 (Swarm) define the interaction.

#### Acceptance Criteria

1. WHEN a volley is fired with WideVolley active, THE Arrow_System SHALL launch every arrow along its assigned fan-spread heading and apply zero homing correction on the launch frame, so the initial travel direction of each arrow equals its fan heading.
2. WHILE Homing is active on a fanned volley, THE Arrow_System SHALL, at a re-seek interval of 0.1 seconds, select for each arrow the nearest un-hit, live Enemy within a 7-unit radius whose direction lies within a 50-degree half-angle of that arrow's current forward direction, and steer the arrow toward that Enemy at a maximum turn rate of 180 degrees per second.
3. IF a homing arrow finds no un-hit, live Enemy within the 7-unit radius and 50-degree forward cone at a re-seek, THEN THE Arrow_System SHALL continue the arrow along its current heading without applying any homing correction until a valid target is found.
4. WHEN two or more homing arrows from the same volley are in flight, THE Arrow_System SHALL evaluate each arrow's target independently from its own position and forward cone, without forcing all arrows to share one target, so that when two or more distinct qualifying Enemies exist within range the arrows may lock onto different Enemies.
5. WHEN a homing arrow's swept path (sphere cast, radius 0.12 units) intersects non-Enemy solid scenery before reaching an Enemy, THE Arrow_System SHALL stop and destroy that arrow at the impact point without passing through the scenery, consistent with the documented "sem atravessar paredes" behavior.
6. WHERE TwinShot is also active, THE Arrow_System SHALL apply the fan-then-home behavior defined in criteria 1 through 5 to every arrow produced by TwinShot multiplication, with each multiplied arrow performing its own independent seek.

### Requirement 5: TripleMoon + Orbit (orbital core)

**User Story:** As a spear player combining TripleMoon and Orbit, I want the W sweeps to multiply and travel outward around me, so that the two modifiers together form a visible orbital core of attacks.

*Grounding:* In `WeaponRunModifiers.Plan`, spear slot 1 already adds `2 * Rank(TripleMoon)` hits, widens by Orbit, and sets `plan.Travel = Rank(Orbit) > 0`. `RunModifierPresentation.Hint` already surfaces a "COMBINAÇÃO ATIVA" hint for this pair. Section 7 (TripleMoon/Orbit) and 12 (Lunar Engine) define the core.

#### Acceptance Criteria

1. WHILE both TripleMoon (rank 1 to 3) and Orbit (rank 1 to 3) are active, WHEN the spear W ability (slot 1) is cast, THE Spear_Plan_System SHALL produce a cast plan whose sweep count equals the base sweep count plus 2 per TripleMoon rank AND whose sweeps advance outward 1.5m per pulse with sweep width multiplied by (1 + 0.25 per Orbit rank).
2. WHEN the spear W ability (slot 1) is cast WHILE only TripleMoon (rank 1 to 3) is active, THE Spear_Plan_System SHALL set the cast plan sweep count to the base count plus 2 per TripleMoon rank AND SHALL leave outward travel disabled and sweep width unchanged from base.
3. WHEN the spear W ability (slot 1) is cast WHILE only Orbit (rank 1 to 3) is active, THE Spear_Plan_System SHALL enable outward travel of 1.5m per pulse AND set sweep width to base multiplied by (1 + 0.25 per Orbit rank) AND SHALL leave the sweep count equal to the base count.
4. WHILE both TripleMoon and Orbit are owned, or while both are offered together in the same selection, THE Run_Modifier_System SHALL present the combination hint text "COMBINAÇÃO ATIVA · luas avançam a cada pulso." for the Orbit modifier.
5. THE Spear_Plan_System SHALL derive the combined behavior only within the per-cast runtime plan snapshot AND SHALL NOT write TripleMoon or Orbit values back to the source spear ability or weapon assets.

### Requirement 6: Haste indirectly feeds ComboNova

**User Story:** As a gauntlet player who picks Haste, I want faster basic attacks to reach ComboNova's third-hit proc sooner, so that attack speed meaningfully accelerates my combo payoff without a dedicated coupling.

*Grounding:* Haste adds to `AttackSpeedMultiplier` (basic attack cadence); ComboNova fires on `_runBasicCount % 3 == 0` in `CharControlScript`. The relationship is emergent: more basics per second → more third-hits per second. Section 4 (haste) and 9 (ComboNova) describe this.

#### Acceptance Criteria

1. WHILE the Player owns ComboNova at rank 1 or higher, WHEN the Player's running basic-attack count reaches a multiple of 3 (`_runBasicCount % 3 == 0`), THE Run_Modifier_System SHALL trigger exactly one ComboNova nova that applies area damage within a 2.5-meter radius equal to 60% of the current weapon's basic attack damage per ComboNova rank.
2. WHEN Haste raises the basic attack speed multiplier by 0.25 per Haste rank, THE Run_Modifier_System SHALL shorten the basic attack interval proportionally (interval = base interval / attack speed multiplier), such that the number of basic attacks completed per fixed time window increases by the same proportion and, consequently, the count of ComboNova procs per that window increases proportionally.
3. THE Run_Modifier_System SHALL evaluate the ComboNova proc solely on the every-third-basic-attack rule (`_runBasicCount % 3 == 0`), so that changing the attack speed multiplier to any value at or above 0.01 changes only proc frequency over time and never changes the requirement that a nova fires on exactly every third basic attack and no other basic attack.
4. WHEN both Haste and ComboNova are simultaneously owned by the Player, or both are presented together in the same offer, THE Run_Modifier_System SHALL display a combination hint stating that Haste accelerates ComboNova, and SHALL not display that hint when only one of the two is owned and neither offer contains both.
5. IF the Player's running basic-attack count reaches a multiple of 3 WHILE ComboNova is owned at rank 0 (not owned), THEN THE Run_Modifier_System SHALL not trigger a ComboNova nova and SHALL leave the basic attack outcome otherwise unchanged.

### Requirement 7: More transformative Spear modifiers (Priority 2)

**User Story:** As a spear player, I want additional behavior-changing modifiers comparable to the bow's transformative options, so that spear builds can become as visually absurd as bow builds.

*Grounding:* Section 17 Priority 2 asks for 2–4 of PhantomSpear, MoonShard, ReturnWave, ChainThrust (defined in sections 7–8, 12). The spear catalog lives in `WeaponRunModifiers.Catalog` with plans in `Plan`; new entries reuse `WeaponBoon`, `Definition`, and `ScopeFor`/`RunModifierPresentation`.

#### Acceptance Criteria

1. THE Run_Modifier_System SHALL add between 2 and 4 new Spear-family behavior modifiers drawn from the set {PhantomSpear, MoonShard, ReturnWave, ChainThrust}.
2. WHERE PhantomSpear is active, WHEN the Player performs a thrust, THE Spear_Plan_System SHALL repeat the thrust with a spectral copy after a delay between 0.2 and 0.5 seconds.
3. WHERE MoonShard is active, WHEN the Player performs a sweep, THE Spear_Plan_System SHALL launch between 2 and 6 projectiles from the sweep extremities.
4. WHERE ReturnWave is active, WHEN the wave reaches its maximum range limit, THE Spear_Plan_System SHALL return the wave toward the Player.
5. WHERE ChainThrust is active, WHEN a thrust hits an Enemy and another Enemy exists within a search radius of 6 meters, THE Spear_Plan_System SHALL create exactly one automatic short thrust toward a different nearby Enemy within that radius.
6. IF ChainThrust is active and no other Enemy exists within the 6-meter search radius, THEN THE Spear_Plan_System SHALL NOT create a chain thrust.
7. THE Run_Modifier_System SHALL register each new modifier in the weapon catalog with a title, description, family=Spear, a maximum rank, a presentation scope, and offer eligibility gated to an equipped Spear weapon.
8. THE Run_Modifier_System SHALL apply each new modifier to runtime copies only and SHALL NOT mutate any original weapon or ability asset.
9. WHILE resolving secondary hits produced by these modifiers, THE Cascade_System SHALL keep all secondary hits within the cascade limits (MaxDepth=4, MaxSecondaryHits=32).

### Requirement 8: Generic combat-event Hook infrastructure (Priority 3)

**User Story:** As a developer extending the modifier system, I want a generic hook infrastructure for combat events, so that many future modifiers can be built with little event-plumbing code.

*Grounding:* `Actor` already exposes `Died` and `DamageReceived` events; `AbilityHolder.NotifyAttackHits` already dispatches post-hit to `AttackPassiveAbility.OnAfterAttackHits`. `ChillStatus` knows when a freeze applies; `BurnStatus` when a burn applies; `PlayerActor` resolves crits and applies stance/`StanceBreakEffect`. Section 13 (Hook) and 17 Priority 3 define the event set.

#### Acceptance Criteria

1. THE Hook_System SHALL expose subscribable events for OnCrit, OnKill, OnFreeze, OnBurn, OnStanceBreak, OnDash, and OnExplosion, and each event SHALL support zero or more subscribers.
2. WHEN the Player lands a critical Direct_Hit, THE Hook_System SHALL raise OnCrit with the affected Enemy and the resolved damage value after the damage has been applied to that Enemy.
3. WHEN an Enemy transitions to the dead state as a result of a Player-caused impact, THE Hook_System SHALL raise OnKill with that Enemy exactly once for that Enemy.
4. WHEN the Element_System applies the frozen state to an Enemy, THE Hook_System SHALL raise OnFreeze with that Enemy after the frozen state has been applied.
5. WHEN the Element_System applies a new burn or refreshes an existing burn on an Enemy, THE Hook_System SHALL raise OnBurn with that Enemy after the burn state has been applied or refreshed.
6. WHEN a Player attack transitions an Enemy's stance to the broken state, THE Hook_System SHALL raise OnStanceBreak with that Enemy after the stance-broken state has been applied.
7. WHEN the Player performs a dash, THE Hook_System SHALL raise OnDash once per dash action.
8. WHEN the Cascade_System produces a detonation explosion, THE Hook_System SHALL raise OnExplosion with the explosion origin position.
9. WHEN a run ends, THE Hook_System SHALL remove all subscriptions from every hook event such that subscribers registered during the ended run receive no further notifications in any subsequent run.
10. IF a subscriber invoked for any hook event raises an exception, THEN THE Hook_System SHALL invoke every remaining subscriber registered for that event and SHALL NOT re-raise the exception to the code that raised the hook.
11. WHEN a hook subscriber triggers secondary hits, THE Hook_System SHALL constrain the total number of resulting secondary hits so it does not exceed the limits already enforced by the Cascade_System.

### Requirement 9: Rewrite reward category (Priority 4)

**User Story:** As a player, I want ability-rewriting modifiers to be presented as a rare and dramatic category distinct from ordinary upgrades, so that fundamentally transforming an ability feels like a special moment.

*Grounding:* The existing `transform` reward already replaces slot 1 (`ArsenalAbility.ConfigureRunTransformation`) and `RunModifierPresentation` already labels it "TRANSFORMAÇÃO". Section 13 (Rewrite) asks to formalize this as a category with distinct presentation.

#### Acceptance Criteria

1. THE Run_Modifier_System SHALL classify all ability-replacing modifiers, including the existing `transform` reward, into a distinct Rewrite_Category.
2. WHEN a Rewrite_Category modifier is presented in an offer set, THE Run_Modifier_System SHALL display it with a distinct category label and accent visual that reuses the "TRANSFORMAÇÃO" presentation.
3. THE Run_Modifier_System SHALL include at most one Rewrite_Category modifier per offer set, with an appearance probability capped at 20 percent per offer set.
4. WHEN a Rewrite_Category modifier is granted, THE Run_Modifier_System SHALL apply it via a runtime copy and SHALL NOT mutate the original ability asset.
5. IF no valid target ability exists for a Rewrite_Category modifier, THEN THE Run_Modifier_System SHALL NOT offer that Rewrite.
6. WHEN a Rewrite_Category modifier has been taken in a run, THE Run_Modifier_System SHALL exclude it from all subsequent offer sets in that same run, consistent with existing `transform` single-use handling.

### Requirement 10: "System breaking" escalating feedback (Priority 5)

**User Story:** As a player assembling an increasingly absurd build, I want the game to react with glitch visuals, weapon comments, and fake system messages, so that pushing the modifiers feels like overrunning the game's own limits.

*Grounding:* Section 3 (thematic direction) and section 17 Priority 5 define escalating feedback (e.g., `WARNING: Projectile count exceeds expected parameters.`, weapon quips). Presentation metadata lives in `RunModifierPresentation`; run state is tracked in `RunBoons.Acquired`.

#### Acceptance Criteria

1. THE System_Break_Feedback SHALL define discrete escalation tiers keyed to the count of accumulated interacting modifiers, with tier thresholds at 3, 6, and 9 accumulated interacting modifiers.
2. WHEN the accumulated interacting modifier count crosses a tier threshold, THE System_Break_Feedback SHALL present feedback for that tier drawn from the set {glitch visuals, weapon comments, fake system messages}.
3. WHILE presenting escalation feedback, THE System_Break_Feedback SHALL present higher-tier feedback at greater intensity and frequency than lower-tier feedback, and SHALL apply a defined ordering when multiple feedback items fire concurrently.
4. THE System_Break_Feedback SHALL treat all escalation feedback as cosmetic only and SHALL NOT alter damage, cascade limits, or reward rules.
5. WHEN a run ends, THE System_Break_Feedback SHALL reset all escalation state.
6. WHILE presenting escalation feedback, THE System_Break_Feedback SHALL keep gameplay-critical information legible over the deliberate visual noise.
7. IF any asset for a tier's feedback bundle is missing, THEN THE System_Break_Feedback SHALL skip that tier's entire feedback bundle without raising an error.

### Requirement 11: Preserve existing guarantees across all changes

**User Story:** As a player and as a developer, I want every new interaction and modifier to respect the existing safety limits and asset isolation, so that emergent builds stay bounded and original assets are never mutated.

*Grounding:* `RunSynergyEffects` enforces cascade bounds; `RunBoons` instantiates runtime copies of weapons and abilities and destroys them on run end; `Docs/Modificadores.md` states rewards are per-run only and do not alter original assets. Project rules require preserving `.meta` files and avoiding `FindObjectOfType`/magic strings.

#### Acceptance Criteria

1. WHEN any new or modified reward is applied, THE Run_Modifier_System SHALL apply it to runtime copies only and SHALL NOT write to any original weapon, ability, or status asset.
2. IF a write to an original weapon, ability, or status asset is attempted, THEN THE Run_Modifier_System SHALL treat it as a defect and prevent the write.
3. THE Cascade_System SHALL preserve the cascade bounds MaxDepth=4 and MaxSecondaryHits=32 after all changes.
4. WHEN a run ends, THE Run_Modifier_System SHALL atomically clear all run-scoped state, including all new hooks and feedback state, as a single operation such that all such state is cleared together rather than independently or at different times.
5. THE Run_Modifier_System SHALL resolve dependencies via serialized references or explicitly resolved components, and SHALL NOT use `FindObjectOfType`, `GameObject.Find`, or magic strings in gameplay code.
6. WHEN an asset is moved, renamed, or created, THE feature SHALL preserve its corresponding `.meta` file so that Unity GUID references stay intact.
