# Requirements Document

## Introduction

This feature defines the **core set of 16 enemy archetypes** for Tech-Guy's Diablo / Lost Ark-style swarm combat. The design intent is that enemies function as **complementary groups**, not identical mobs: every archetype fulfills at least one clear combat role, is readable at a glance, and creates a distinct kind of problem for the player (melee pressure, ranged pressure, territory control, priority pressure, combo fodder, etc.).

Each archetype is defined as the combination of **a distinct AI behavior + a stat/stance profile + a telegraph + a combat role**. The implementation **extends** the existing enemy systems (`EnemyAI`, `EnemyAttackPatterns`, `EnemyAttackKind`, `EnemyProfile`, `EnemyVariant`, `EnemyCombatActions`, `CombatReactionController`, `EnemyRespawnPoint`) rather than replacing them, and follows the existing telegraph-before-damage model already used by `EnemyAttackExecution` (cancellable warning outline → single damage beat).

Three cross-cutting design principles from the design doc (seções 15 & 18) constrain every archetype:

1. **Telegraph before damage** — the more dangerous an attack or crowd-control effect is, the clearer and earlier the read must be.
2. **Never remove player control without warning** — any pull, hook, or root must be telegraphed, avoidable, predictable, and brief.
3. **Priority must be readable** — healers, shields, and spawners must be visually identifiable as priority targets without tooltips, and the swarm as a whole must generate varied problems rather than N copies of one enemy.

### Scope

**In scope:** the 16 archetypes listed in the Glossary below, their AI behaviors, telegraphs, stat/stance profiles, projectile handling, NavMesh/positioning behavior, target-priority legibility, and interaction with the existing stagger/launch/stance system.

**Out of scope (explicitly excluded):** visual art, models, and lore; "second wave" archetypes; the elite affix system; and full encounter/composition authoring tooling. Composition examples are context only.

## Glossary

- **Archetype**: A named enemy definition consisting of a distinct AI behavior, a stat/stance profile, one or more telegraphed actions, and a declared combat role. The 16 archetypes are the 16 listed below.
- **Archetype_System**: The collection of new and extended components that select and drive archetype behavior (extensions to `EnemyAI`, `EnemyAttackPatterns`, `EnemyAttackKind`, `EnemyProfile`, `EnemyVariant`, plus new per-archetype behavior classes).
- **Enemy_AI**: The existing `EnemyAI` MonoBehaviour that performs NavMeshAgent chase/patrol/perception and runs the telegraphed attack coroutine.
- **Attack_Selector**: The existing `EnemyAttackPatterns` static class (and its extensions) that chooses an `EnemyAttackKind` from distance, traits, and archetype.
- **Combat_Actions**: The existing `EnemyCombatActions` component that executes body-driven attacks using `EnemyAttackExecution` warnings.
- **Attack_Execution**: The existing `EnemyAttackExecution` helper that draws a cancellable ground warning during a windup and resolves a single damage beat on impact.
- **Reaction_Controller**: The existing `CombatReactionController` that manages stance, stance break, and the hard crowd-control effects Stagger / Stun / KnockUp / Knockback.
- **Enemy_Profile**: The existing `EnemyProfile` ScriptableObject holding stat multipliers, stance pool, stance recovery, and per-CC resistances.
- **Enemy_Variant**: The existing `EnemyVariant` component that applies a profile (and affixes) to an enemy and configures stance.
- **Spawn_Point**: The existing `EnemyRespawnPoint` component used to instantiate enemies.
- **Actor**: The existing `Actor` component holding `health` / `maxHealth`, `TakeDamage`, `SetMaxHealth`, `SetDamageTakenMultiplier`, and the `Died` event.
- **Telegraph**: A perceptible pre-damage signal (ground outline, aim line, projectile arc indicator, channel beam/tether, or windup animation) shown before an attack or crowd-control effect resolves.
- **Player_Actor**: The `Actor` representing the player character.
- **Combat_Role**: The tactical function an archetype performs. Defined roles: Melee-Pressure, Ranged-Pressure, Territory-Control, Player-Displacement, Ally-Support, Swarm-Fuel, Priority-Threat, Combo-Fodder, Projectile-Denial.
- **Priority_Target**: An archetype whose Combat_Role is Ally-Support or Priority-Threat (Healer, Shield Support, Spawner) that the design requires to be visually distinguishable as a high-value kill.
- **Melee archetypes**: **Rush**, **Grunt**, **Heavy**, **Charger**.
- **Ranged archetypes**: **Shooter**, **Spread_Shooter**, **Sniper**, **Bomber**.
- **Control archetypes**: **Hazard_Caster**, **Hooker**.
- **Support archetypes**: **Healer**, **Shield_Support**.
- **Swarm archetypes**: **Swarm**, **Spawner**.
- **Special archetypes**: **Fragile**, **Mirror**.
- **Hazard_Zone**: A persistent ground area created by a Hazard_Caster that applies damage or a slow to the Player_Actor while occupied.
- **Shield**: A temporary, depletable damage-absorbing buffer granted to an Actor that is consumed before the Actor's health.

## Requirements

### Requirement 1: Archetype Definition Model

**User Story:** As a combat designer, I want each archetype defined as a data-plus-behavior unit that extends the existing enemy system, so that I can author distinct enemies without duplicating or replacing the current AI, profile, and stance code.

#### Acceptance Criteria

1. THE Archetype_System SHALL represent each of the 16 archetypes as the combination of one AI behavior, one Enemy_Profile stat/stance configuration, at least 1 and at most 8 Telegraphs, and exactly one primary Combat_Role.
2. THE Archetype_System SHALL extend the existing `EnemyAttackKind` enumeration and Attack_Selector rather than replacing them.
3. THE Archetype_System SHALL drive per-archetype behavior through the existing Enemy_AI attack coroutine and Combat_Actions execution path.
4. WHERE an archetype requires stance or crowd-control tuning, THE Enemy_Variant SHALL apply the archetype's Enemy_Profile through the existing `ConfigureStance` path on the Reaction_Controller.
5. THE Archetype_System SHALL assign each archetype a unique identifier that Enemy_Variant resolves at Enemy_Variant configuration time without using `GameObject.Find` or `FindObjectOfType`.
6. WHEN a required dependency (Actor, Enemy_AI, NavMeshAgent, Reaction_Controller) for an archetype is missing during `Awake` or `OnValidate`, THE Archetype_System SHALL log an error message naming the missing dependency.
7. IF an archetype identifier cannot be resolved or resolves to more than one archetype definition at Enemy_Variant configuration time, THEN THE Archetype_System SHALL log an error naming the identifier and SHALL fall back to a safe default archetype configuration without throwing an exception.

### Requirement 2: Telegraph Before Damage

**User Story:** As a player, I want every damaging attack and every crowd-control effect to be telegraphed before it lands, so that I can read and react to threats fairly.

#### Acceptance Criteria

1. WHEN an archetype begins a damaging attack, THE Archetype_System SHALL display a Telegraph for a minimum windup of 0.25 seconds before the damage beat resolves, using the existing Attack_Execution warning path.
2. WHERE an attack applies hard crowd-control (Stun, KnockUp, Knockback) or player displacement (pull, hook), THE Archetype_System SHALL make the Telegraph windup at least as long as the equivalent non-controlling attack's windup, and no shorter than 0.25 seconds.
3. WHILE a Telegraph is active, THE Archetype_System SHALL monotonically progress the Telegraph's appearance from its initial appearance toward its impact appearance as the windup fraction advances from 0 to 1.
4. IF the acting enemy's control is locked (stun or air-juggle) or its stance is broken during a windup, THEN THE Archetype_System SHALL cancel the Telegraph and suppress its pending damage beat within the same frame the interrupt is applied.
5. THE Archetype_System SHALL resolve at most one damage beat per telegraphed attack area per attack, consistent with the existing single-beat rule.
6. WHILE a telegraphed attack's windup has not completed and has not been cancelled, THE Archetype_System SHALL keep the Telegraph continuously visible.

### Requirement 3: Rush Archetype (Melee)

**User Story:** As a player, I want fast fragile melee enemies that apply pressure, so that I must keep moving and cannot ignore approaching threats.

#### Acceptance Criteria

1. THE Rush SHALL declare Combat_Role Melee-Pressure.
2. THE Rush SHALL use an Enemy_Profile whose movement multiplier is greater than 1.0 and whose health multiplier is less than 1.0, both relative to the 1.0 baseline.
3. WHILE the Player_Actor is within the Rush's configured sight range, THE Rush SHALL set the NavMeshAgent destination toward the Player_Actor until the distance to the Player_Actor is at or below melee range (at most 2.5 meters).
4. WHEN the distance between the Rush and the Player_Actor is at or below melee range (at most 2.5 meters), THE Rush SHALL start a melee attack through Combat_Actions whose windup duration is shorter than the Grunt's windup duration.
5. THE Rush SHALL use an Enemy_Profile whose stagger resistance equals 0, so that any hit requesting a Stagger interrupts its current action.
6. IF the NavMeshAgent has no valid path to the Player_Actor while the Player_Actor is within sight range, THEN THE Rush SHALL hold position without starting a melee attack until a valid path exists.

### Requirement 4: Grunt Archetype (Melee)

**User Story:** As a player, I want a standard baseline melee enemy, so that I have a readable reference point against which other archetypes contrast.

#### Acceptance Criteria

1. THE Grunt SHALL declare Combat_Role Melee-Pressure.
2. THE Grunt SHALL use an Enemy_Profile whose health, damage, movement, and attack-speed multipliers each equal 1.0.
3. WHEN the distance between the Grunt and the Player_Actor is at or below melee range (at most 2.5 meters), THE Grunt SHALL start a melee attack through Combat_Actions that displays a Telegraph for at least 0.4 seconds before its damage window opens.
4. WHEN a hit requesting a crowd-control effect reduces the Grunt's stance pool to 0, THE Reaction_Controller SHALL apply that requested crowd-control effect at full duration, because the Grunt's Enemy_Profile crowd-control resistances each equal 0.
5. IF the Player_Actor leaves melee range after the Grunt starts its telegraphed attack but before the damage window opens, THEN THE Grunt SHALL complete the current attack's damage window without applying damage and SHALL retain its remaining stance.

### Requirement 5: Heavy Archetype (Melee)

**User Story:** As a player, I want a slow tank enemy with large telegraphed attacks and high poise, so that I have to time my openings and cannot stagger it freely.

#### Acceptance Criteria

1. THE Heavy SHALL declare Combat_Role Melee-Pressure.
2. THE Heavy SHALL use an Enemy_Profile whose health multiplier is greater than 1.0, whose movement multiplier is less than 1.0, and whose stagger resistance is greater than 0, all relative to the 1.0 baseline.
3. WHEN the distance between the Heavy and the Player_Actor is at or below engagement range (at most 2.5 meters), THE Heavy SHALL perform a telegraphed slam attack followed by a ground-pound area attack through Combat_Actions.
4. WHEN the Heavy begins each slam or ground-pound, THE Heavy SHALL display a ground-area Telegraph covering that attack's impact area for at least 0.4 seconds before the impact occurs.
5. WHILE the Heavy is performing its slam or ground-pound windup, THE Reaction_Controller SHALL require the Heavy's configured maximum stance to be depleted to 0 to interrupt it, where the Heavy's maximum stance is greater than the Grunt's maximum stance.
6. WHEN a hit reduces the Heavy's stance pool to 0, THE Reaction_Controller SHALL apply the hit's requested crowd-control effect with its duration scaled by (1 minus the Heavy's configured resistance for that effect), and SHALL apply no hard crowd-control effect for any resistance equal to 1.

### Requirement 6: Charger Archetype (Melee)

**User Story:** As a player, I want an enemy that commits to a locked charge direction, so that I can bait and dodge its dash and punish the miss.

#### Acceptance Criteria

1. THE Charger SHALL declare Combat_Role Melee-Pressure.
2. WHEN the Charger begins a charge, THE Charger SHALL fix its charge direction toward the Player_Actor's position at that moment and SHALL display a lane Telegraph covering the charge path for at least 0.4 seconds before dashing.
3. WHEN the charge windup completes, THE Charger SHALL dash along the fixed charge direction using the existing Charge execution in Combat_Actions, without updating the direction toward the Player_Actor during the dash.
4. WHILE the Charger is dashing, THE Charger SHALL apply its charge damage to the Player_Actor at most one time per dash.
5. WHEN the Charger's dash ends without applying damage to the Player_Actor, THE Charger SHALL enter a recovery window of at least 1.0 second during which it does not begin a new attack.
6. IF the Charger's dash path is obstructed by level geometry before reaching its dash endpoint, THEN THE Charger SHALL stop at the obstruction and enter the same recovery window without applying charge damage.

### Requirement 7: Shooter Archetype (Ranged)

**User Story:** As a player, I want a ranged enemy that kites at medium range, so that I am pressured to close distance while dodging simple projectiles.

#### Acceptance Criteria

1. THE Shooter SHALL declare Combat_Role Ranged-Pressure.
2. WHILE the Player_Actor is between the Shooter's minimum standoff distance and its firing range, and the Shooter has line of sight to the Player_Actor, THE Shooter SHALL fire a single projectile toward the Player_Actor's position after a Telegraph windup of at least 0.25 seconds.
3. WHEN the distance between the Player_Actor and the Shooter drops below the Shooter's minimum standoff distance, THE Shooter SHALL select a NavMesh-reachable destination that increases that distance to at least the minimum standoff distance and move to it using the NavMeshAgent.
4. WHEN a Shooter projectile reaches the Player_Actor's position along its travel path, THE Archetype_System SHALL apply the projectile's damage to the Player_Actor exactly once and then remove the projectile.
5. IF a Shooter projectile's travel path is blocked by static geometry before reaching the Player_Actor, THEN THE Archetype_System SHALL stop the projectile at the blocking point and remove it without applying damage.
6. IF the Shooter's line of sight to the Player_Actor is broken or the Shooter's control is locked during the Telegraph windup, THEN THE Archetype_System SHALL cancel the shot without firing a projectile or applying damage.

### Requirement 8: Spread Shooter Archetype (Ranged)

**User Story:** As a player, I want an enemy that fires a cone of projectiles, so that dodging directly sideways is punished and I must dodge through or around the spread.

#### Acceptance Criteria

1. THE Spread_Shooter SHALL declare Combat_Role Ranged-Pressure.
2. WHEN the Spread_Shooter fires, THE Spread_Shooter SHALL emit at least 3 projectiles distributed at equal angular spacing across a forward cone, after a Telegraph windup of at least 0.25 seconds.
3. WHEN the Telegraph begins, THE Spread_Shooter SHALL orient the center of its cone toward the Player_Actor's position at that moment and SHALL keep that orientation fixed for the remainder of the windup and firing.
4. WHEN any Spread_Shooter projectile reaches the Player_Actor along its travel path, THE Archetype_System SHALL apply that projectile's damage to the Player_Actor exactly once per projectile and then remove that projectile.
5. IF the Spread_Shooter's control is locked during the Telegraph windup, THEN THE Archetype_System SHALL cancel the volley without emitting projectiles or applying damage.

### Requirement 9: Sniper Archetype (Ranged)

**User Story:** As a player, I want a long-range enemy with a clearly intensifying aim line and a heavy, infrequent shot, so that I always have time to break line of sight or dodge a high-damage hit.

#### Acceptance Criteria

1. THE Sniper SHALL declare Combat_Role Ranged-Pressure.
2. THE Sniper SHALL use a firing range greater than the Shooter's firing range.
3. WHILE the Sniper is aiming, THE Sniper SHALL display an aim-line Telegraph from itself toward the Player_Actor for an aim windup of at least 1.0 second, and SHALL intensify the Telegraph toward its impact appearance as the windup progresses.
4. THE Sniper SHALL use an attack whose per-hit damage exceeds the Grunt's per-hit damage and whose time between attacks exceeds the Shooter's time between attacks.
5. WHEN the Sniper's aim windup completes, THE Sniper SHALL fire along the aim line established at the moment the windup completes and SHALL apply its damage to the Player_Actor at most once.
6. IF the Sniper's control is locked or its line of sight to the Player_Actor is broken during the aim windup, THEN THE Archetype_System SHALL cancel the shot without firing or applying damage.

### Requirement 10: Bomber Archetype (Ranged)

**User Story:** As a player, I want an enemy that lobs arced projectiles with a ground impact indicator, so that I can see where a delayed area hit will land and vacate the space.

#### Acceptance Criteria

1. THE Bomber SHALL declare Combat_Role Ranged-Pressure.
2. WHEN the Bomber fires, THE Bomber SHALL launch an arced projectile toward a target ground position and SHALL display a ground-area impact Telegraph, covering the configured impact radius centered on that position, before the projectile lands.
3. WHEN the arced projectile lands, THE Archetype_System SHALL apply area damage to the Player_Actor exactly once if the Player_Actor is within the telegraphed impact area at the moment of impact, and SHALL apply no damage otherwise.
4. WHILE the arced projectile is in flight, THE Bomber SHALL display the ground-area impact Telegraph for the full travel duration and SHALL intensify it toward its impact appearance as the projectile approaches its landing.
5. WHEN the arced projectile lands, THE Archetype_System SHALL remove the projectile and its impact Telegraph.

### Requirement 11: Hazard Caster Archetype (Control)

**User Story:** As a player, I want an enemy that places persistent ground damage zones, so that my safe space is reshaped over time rather than by burst damage.

#### Acceptance Criteria

1. THE Hazard_Caster SHALL declare Combat_Role Territory-Control.
2. THE Hazard_Caster SHALL support fire, electric, and slow Hazard_Zone variants, selectable per instance through serialized configuration.
3. WHEN the Hazard_Caster creates a Hazard_Zone, THE Hazard_Caster SHALL display a Telegraph at the zone position for a windup of at least 0.25 seconds before the zone becomes active.
4. WHEN a Hazard_Zone's pre-activation Telegraph windup completes, THE Archetype_System SHALL activate the Hazard_Zone at the telegraphed position.
5. WHILE the Player_Actor occupies an active fire or electric Hazard_Zone, THE Archetype_System SHALL apply damage to the Player_Actor once per fixed interval in the range 0.25 to 1.0 seconds.
6. WHEN the Player_Actor leaves an active fire or electric Hazard_Zone, THE Archetype_System SHALL stop applying that zone's periodic damage to the Player_Actor.
7. WHILE the Player_Actor occupies an active slow Hazard_Zone, THE Archetype_System SHALL reduce the Player_Actor's movement speed by a configured factor in the range 20% to 60% for the duration of occupancy.
8. WHEN the Player_Actor leaves an active slow Hazard_Zone, THE Archetype_System SHALL restore the Player_Actor's movement speed to its pre-slow value.
9. WHEN a Hazard_Zone's configured lifetime in the range 3 to 15 seconds elapses, THE Archetype_System SHALL remove the Hazard_Zone.

### Requirement 12: Hooker Archetype (Control / Displacement)

**User Story:** As a player, I want a hook enemy whose pull is always telegraphed and avoidable, so that losing control feels fair and readable.

#### Acceptance Criteria

1. THE Hooker SHALL declare Combat_Role Player-Displacement.
2. WHEN the Hooker begins a hook, THE Hooker SHALL lock the hook's straight-line direction and display a line Telegraph before firing.
3. WHEN the hook connects with the Player_Actor, THE Archetype_System SHALL pull the Player_Actor toward the Hooker along the hook line, completing the pull within a bounded duration not exceeding 1.5 seconds.
4. THE Hooker's hook Telegraph windup SHALL be at least as long as the Grunt's melee windup.
5. IF the Player_Actor's position is farther than a configured hook-line tolerance in the range 0.5 to 1.5 meters from the hook line at the moment the hook fires, THEN THE Archetype_System SHALL resolve the hook without displacing the Player_Actor.
6. WHILE the Player_Actor is being pulled by a hook, THE Archetype_System SHALL end the pull within the bounded duration not exceeding 1.5 seconds and return movement control to the Player_Actor.
7. IF the Hooker's control is locked by the Reaction_Controller while it is pulling the Player_Actor, THEN THE Archetype_System SHALL end the pull and return movement control to the Player_Actor.

### Requirement 13: Healer Archetype (Support)

**User Story:** As a player, I want a low-health enemy that visibly heals its allies and flees when approached, so that I learn to prioritize killing it.

#### Acceptance Criteria

1. THE Healer SHALL declare Combat_Role Ally-Support and SHALL declare itself a Priority_Target.
2. THE Healer SHALL use an Enemy_Profile whose maximum-health multiplier is below the baseline of 1.0 (in the range 0.3 to 0.7 inclusive).
3. WHILE at least one allied Actor whose current health is below its maximum health is within the Healer's heal range (a fixed radius in the range 4 to 8 meters), THE Healer SHALL restore a fixed amount of health to the most-wounded such ally once every fixed heal interval (in the range 1.0 to 3.0 seconds), clamping the ally's resulting health so it does not exceed that ally's maximum health.
4. WHILE the Healer is channeling a heal, THE Healer SHALL display a visible beam or tether connecting the Healer to the ally currently being healed.
5. WHILE no allied Actor is below its maximum health within the Healer's heal range, THE Healer SHALL NOT channel a heal.
6. WHEN the Player_Actor comes within the Healer's flee range (a fixed radius strictly less than the Healer's heal range, in the range 2 to 5 meters), THE Healer SHALL move away from the Player_Actor along the NavMeshAgent until the Player_Actor is beyond the flee range.
7. IF the Healer's control is locked (stun or air-juggle) or its stance is broken while channeling a heal, THEN THE Archetype_System SHALL stop that heal channel and SHALL apply no further health restoration from that channel.
8. WHEN the Healer is spawned, THE Archetype_System SHALL display a persistent visual marker identifying the Healer as a Priority_Target that is visible without the Player_Actor selecting or targeting it, and WHEN the Healer dies THE Archetype_System SHALL remove that marker.

### Requirement 14: Shield Support Archetype (Support)

**User Story:** As a player, I want an enemy that grants temporary shields to allies and is vulnerable while doing so, so that I have a window to punish it.

#### Acceptance Criteria

1. THE Shield_Support SHALL declare Combat_Role Ally-Support and SHALL declare itself a Priority_Target.
2. WHEN the Shield_Support completes a channel, THE Shield_Support SHALL grant a Shield to each allied Actor within its support range (a fixed radius in the range 4 to 10 meters) at the moment the channel completes.
3. THE granted Shield SHALL absorb incoming damage to the allied Actor, being consumed before the ally's health, until either its absorption capacity (a fixed amount) is depleted or its duration (in the range 3 to 10 seconds) expires, after which incoming damage SHALL again reduce the ally's health.
4. WHILE the Shield_Support is channeling, THE Shield_Support SHALL display a visible channel Telegraph for a minimum windup of 0.25 seconds before the channel completes and SHALL remain stationary for the duration of the channel.
5. IF the Shield_Support's channel is interrupted by a stance break or control lock before it completes, THEN THE Archetype_System SHALL grant no Shield from that channel.
6. WHEN the Shield_Support is spawned, THE Archetype_System SHALL display a persistent visual marker identifying the Shield_Support as a Priority_Target that is visible without the Player_Actor selecting or targeting it, and WHEN the Shield_Support dies THE Archetype_System SHALL remove that marker.

### Requirement 15: Swarm Archetype (Swarm)

**User Story:** As a player, I want tiny, extremely fragile enemies that appear in large numbers, so that they occupy space and feed on-kill and explosion builds.

#### Acceptance Criteria

1. THE Swarm SHALL declare Combat_Role Swarm-Fuel.
2. THE Swarm SHALL use an Enemy_Profile whose maximum health is at most 25 percent of the Grunt's configured maximum health.
3. WHEN a single light player hit is applied to the Swarm, THE Swarm SHALL have its health reduced to 0 or below and transition to its death state.
4. WHILE the Player_Actor is within the Swarm's sight range, THE Swarm SHALL update its NavMeshAgent destination toward the Player_Actor at least once every 0.5 seconds.
5. IF the Player_Actor leaves the Swarm's sight range, THEN THE Swarm SHALL fall back to its default idle/patrol behavior.
6. WHEN a Swarm enemy dies, THE Actor SHALL raise its existing Died event exactly once, and THE Archetype_System SHALL remove the Swarm from the scene within 1 second.

### Requirement 16: Spawner Archetype (Swarm)

**User Story:** As a player, I want an enemy that continuously produces Swarm enemies, so that the encounter worsens the longer I leave it alive and I am pressured to prioritize it.

#### Acceptance Criteria

1. THE Spawner SHALL declare Combat_Role Priority-Threat and SHALL declare itself a Priority_Target.
2. WHILE the Spawner is alive and its count of living produced Swarm enemies is below its configured maximum, THE Spawner SHALL spawn a Swarm enemy once per configured spawn interval (a positive value in seconds) using the existing Spawn_Point instantiation path.
3. THE Spawner SHALL track the number of concurrently living Swarm enemies it has produced and SHALL cap that number at a configured maximum in the range 1 to 100 inclusive.
4. IF the Spawner's count of living produced Swarm enemies has reached the configured maximum, THEN THE Spawner SHALL not spawn additional Swarm enemies until that count falls below the maximum.
5. WHEN a Swarm enemy the Spawner produced dies, THE Spawner SHALL decrement its count of living produced Swarm enemies via that Swarm's Died event.
6. WHEN the Spawner dies, THE Spawner SHALL stop producing new Swarm enemies within 1 second and SHALL not resume producing while dead.
7. WHILE the Spawner is alive, THE Archetype_System SHALL display a persistent visual marker identifying the Spawner as a Priority_Target that is visible without the Player_Actor selecting or targeting it.
8. WHEN spawning a Swarm enemy, THE Spawner SHALL place it on the NavMesh within a configured radius of the Spawner using the existing NavMesh sampling behavior.
9. IF NavMesh sampling fails to find a valid spawn position within the configured radius, THEN THE Spawner SHALL skip that spawn attempt, retain its living count, and retry at the next spawn interval.

### Requirement 17: Fragile Archetype (Special)

**User Story:** As a player, I want an enemy that launches easily and hangs in the air, so that I can use it as combo fodder and knock it into other enemies.

#### Acceptance Criteria

1. THE Fragile SHALL declare Combat_Role Combo-Fodder.
2. THE Fragile SHALL use an Enemy_Profile whose maximum stance is in the range 1 to 60 and whose KnockUp resistance equals 0.
3. WHEN the Fragile's stance pool is reduced to 0 by a KnockUp attack, THE Reaction_Controller SHALL launch the Fragile into the air.
4. WHILE the Fragile is airborne from a KnockUp, THE Fragile SHALL remain airborne for a duration equal to the launch's stun duration scaled by (1 minus its KnockUp resistance), which for the Fragile's zero KnockUp resistance exceeds the airborne duration of an otherwise-identical enemy with nonzero KnockUp resistance.
5. WHILE the Fragile is airborne and displaced into another enemy, THE Archetype_System SHALL not suppress the resulting collision.
6. WHEN the Fragile's airborne crowd-control ends, THE Archetype_System SHALL return the Fragile to its grounded controllable state.

### Requirement 18: Mirror Archetype (Special)

**User Story:** As a player, I want an enemy with a frontal reflective shield, so that I am forced to flank it, break its stance, or use area attacks instead of shooting it head-on.

#### Acceptance Criteria

1. THE Mirror SHALL declare Combat_Role Projectile-Denial.
2. THE Mirror's frontal shield SHALL protect a fixed frontal arc in the range 90 to 180 degrees centered on the Mirror's forward direction.
3. WHILE the Mirror's frontal shield is active, WHEN a player projectile strikes the Mirror from within its frontal arc, THE Archetype_System SHALL apply at most 25 percent of that projectile's damage to the Mirror.
4. WHEN a player attack strikes the Mirror from outside its frontal arc, THE Archetype_System SHALL apply that attack's full damage to the Mirror.
5. WHEN an area attack overlaps the Mirror, THE Archetype_System SHALL apply that attack's full damage to the Mirror regardless of the frontal shield.
6. WHEN the Mirror's stance is broken, THE Mirror SHALL disable its frontal shield for the duration of the resulting crowd-control effect.
7. WHEN the Mirror's crowd-control effect ends and it regains control, THE Mirror SHALL re-enable its frontal shield.
8. WHILE the Mirror is alive, THE Archetype_System SHALL present the frontal shield with a visual communicating the protected arc that is visible without the Player_Actor selecting the Mirror.

### Requirement 19: Projectile Handling for Ranged Archetypes

**User Story:** As a combat designer, I want ranged projectiles handled consistently, so that all ranged archetypes share predictable travel, collision, and cleanup behavior.

#### Acceptance Criteria

1. WHEN a ranged projectile is fired, THE Archetype_System SHALL move it along its configured travel path across one or more frames at its configured travel speed, and SHALL NOT resolve its outcome within the same frame it is fired.
2. WHEN a projectile's collision volume overlaps the Player_Actor's collision volume, THE Archetype_System SHALL apply the projectile's configured damage to the Player_Actor exactly once and mark the projectile for destruction.
3. WHEN a projectile travels a cumulative distance equal to its configured travel range without overlapping the Player_Actor, THE Archetype_System SHALL mark the projectile for destruction without applying damage.
4. WHEN a projectile's collision volume overlaps non-Player world geometry or an obstacle before reaching the Player_Actor, THE Archetype_System SHALL mark the projectile for destruction without applying damage to the Player_Actor.
5. WHEN a projectile is marked for destruction, THE Archetype_System SHALL destroy the projectile within the same frame and release any runtime materials or effect instances it created, leaving no orphaned runtime material or effect instance.
6. IF a projectile's owning enemy dies or is disabled while that projectile is in flight, THEN THE Archetype_System SHALL mark all of that enemy's in-flight projectiles for destruction without applying further damage.

### Requirement 20: NavMesh Positioning Behavior

**User Story:** As a combat designer, I want positioning behaviors driven by the existing NavMeshAgent, so that kiting, fleeing, and repositioning stay on the walkable surface and respect the existing control-lock rules.

#### Acceptance Criteria

1. THE Archetype_System SHALL issue all archetype movement through the existing NavMeshAgent used by Enemy_AI, and SHALL NOT set an archetype's transform position directly for locomotion.
2. WHILE an archetype's control is locked by the Reaction_Controller, THE Archetype_System SHALL NOT issue new movement destinations or attack commands for that archetype.
3. WHEN a kiting or fleeing archetype (Shooter, Healer) chooses a reposition destination, THE Archetype_System SHALL select a destination that lies on the NavMesh and is reachable by that archetype's NavMeshAgent from its current position.
4. IF no reachable NavMesh destination is found for a repositioning archetype during a decision attempt, THEN THE Archetype_System SHALL leave that archetype at its current destination for that frame without issuing an invalid move command and without error.
5. IF an archetype's NavMeshAgent is disabled or the agent is not positioned on the NavMesh, THEN THE Archetype_System SHALL skip that archetype's movement commands for that frame, leaving the archetype's state unchanged and raising no error.

### Requirement 21: Target-Priority Legibility

**User Story:** As a player, I want to identify high-value enemies at a glance, so that I can prioritize healers, shields, and spawners without reading tooltips.

#### Acceptance Criteria

1. WHILE a Priority_Target (Healer, Shield_Support, Spawner) is alive and active in the scene, THE Archetype_System SHALL display a persistent visual indicator on that enemy that is present without the player selecting, targeting, or hovering the enemy.
2. THE Archetype_System SHALL render the Priority_Target indicator using a presentation that is not applied to any non-priority archetype, so that a priority enemy and a non-priority enemy are visually distinguishable at the same on-screen distance.
3. WHILE a Priority_Target is performing its role action (healing, shielding, or spawning), THE Archetype_System SHALL display an additional visual cue that begins when the action begins and ends when the action ends, and that is visually distinct from the persistent priority indicator of criterion 1.
4. WHEN a Priority_Target dies or is removed from the scene, THE Archetype_System SHALL remove its persistent priority indicator and any active role-action cue within the same frame.

### Requirement 22: Swarm Problem Diversity

**User Story:** As a combat designer, I want the archetype set to generate distinct kinds of pressure, so that a group of enemies is a puzzle of complementary roles rather than N identical mobs.

#### Acceptance Criteria

1. THE Archetype_System SHALL provide at least one archetype for each of the following Combat_Roles: Melee-Pressure, Ranged-Pressure, Territory-Control, Player-Displacement, Ally-Support, Swarm-Fuel, Priority-Threat, Combo-Fodder, and Projectile-Denial.
2. THE Archetype_System SHALL assign each of the 16 archetypes exactly one primary Combat_Role drawn from the set defined in criterion 1, such that no archetype has zero or more than one primary Combat_Role.
3. THE Archetype_System SHALL ensure that each of the four Melee archetypes has an Enemy_Profile that differs from every other Melee archetype's Enemy_Profile in at least one defined profile attribute, so that no two Melee archetypes share an identical Enemy_Profile.
