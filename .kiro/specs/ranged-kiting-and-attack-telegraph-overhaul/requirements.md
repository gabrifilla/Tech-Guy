# Requirements Document

## Introduction

This feature improves two aspects of enemy combat in the top-down action game (style reference: Hades, Lost Ark enemy/boss telegraphs). It is an **improvement of existing systems**, not a re-architecture.

1. **Fairer ranged kiting/standoff.** Ranged enemies (currently only the Shooter archetype kites) retreat at full chase speed, which makes it very hard for melee to close in and pressure them. This feature keeps ranged enemies at distance but slows and bounds their retreat so melee gets a fair chance to engage. Overall difficulty must be preserved — this is about the fairness of the melee-vs-ranged interaction, not making the game easier. Movement stays NavMesh-only, and the existing attack-skip-while-repositioning behavior is preserved.

2. **Consistent red, growing attack telegraphs.** Attacks are not signaled clearly enough. Today the telegraph draws only an **outline** (`CombatGroundRing` + `LineRenderer`), lerps toward **white** (not red), uses per-attack base colors, and some attacks (e.g. the Shooter straight shot) show **no** ground warning at all. This feature makes **every** enemy attack display a **red** telegraph that **grows/fills** within the attack's area during windup until impact, extending the existing `EnemyAttackExecution` / `CombatGroundRing` / `TelegraphBeat` systems. The existing single-beat damage rule, minimum windup floor, cancellability, and projectile telegraph behavior are all preserved.

3. **More natural, less robotic enemy behavior.** Enemies currently snap-rotate instantly toward the player, chase in a dead-straight beeline, react the same frame they first see the player, attack in deterministic synchronized lockstep, patrol with no idle pauses, and switch animation on a hard Idle/Walk binary. This feature adds smoothed turning, a short perception reaction delay, attack-cadence jitter, bounded chase-approach variation, patrol idle pauses, and speed-based animation blending — tuned so enemies read as more lifelike while overall difficulty is preserved. It is scoped as an improvement of `EnemyAI`, not a re-architecture.

This document scopes **requirements only** (what, not how). Specific tuning values and visual implementation choices are deferred to design.

## Glossary

- **Ranged_Enemy**: An enemy whose `CombatRole` keeps distance from the player and attacks from range (e.g. `RangedPressure`, `ProjectileDenial`, `TerritoryControl`). The Shooter archetype is the current kiting Ranged_Enemy.
- **Kiting**: Behavior where a Ranged_Enemy moves away from the player to re-establish its standoff distance while declining to attack that frame.
- **Standoff_Distance**: The minimum distance a Ranged_Enemy tries to keep from the player before it retreats, currently derived from the engagement band (`StandoffFraction = 0.45`, overshot by `StandoffMargin = 1.15`).
- **Retreat_Speed**: The NavMesh agent movement speed used specifically while a Ranged_Enemy is Kiting, as distinct from its chase/approach speed.
- **Retreat_Window**: A bounded, cooldown-governed period during which a Ranged_Enemy is permitted to Kite before it must pause retreating.
- **Enemy_Attack**: Any damaging enemy action routed through `EnemyAttackExecution.Execute`, including melee strikes, cones/waves, hazards, auras, and projectile launches.
- **Telegraph**: The world-space ground visual drawn within an Enemy_Attack's area(s) during windup that communicates the danger zone before the hit lands.
- **Windup**: The cancellable pre-impact phase of an Enemy_Attack, floored at `MinWindupSeconds = 0.25`.
- **Impact**: The single moment a resolved Enemy_Attack applies damage (governed by `SingleBeatResolver`).
- **Attack_Area**: An `EnemyAttackArea` (Circle, Ring, or Cone) used for both `Contains()` damage checks and `Outline()` points.
- **Telegraph_System**: The combined `EnemyAttackExecution`, `CombatGroundRing`, `EnemyProjectile`, and `TelegraphBeat` logic that renders and times telegraphs.
- **Fill_Progress**: A 0->1 value tracking how much of the Attack_Area is visually filled, advancing monotonically with Windup elapsed fraction.
- **Enemy_Behavior**: The `EnemyAI` MonoBehaviour and its supporting logic that coordinate perception, patrol, chase, facing, attack cadence, and movement animation for a single enemy.
- **Facing_Turn**: The per-frame rotation applied to orient an enemy toward the player, currently an instant snap (`Quaternion.LookRotation`) and targeted by this feature for gradual turning.
- **Angular_Speed**: The maximum rate, in degrees per second, at which an enemy rotates its Facing_Turn toward the player.
- **Reaction_Delay**: A short period between an enemy first detecting the player (perception transitioning to in-sight) and the enemy beginning to chase or attack.
- **Sight_Loss_Reset**: The continuous duration the player must remain out of sight before an enemy's Reaction_Delay re-arms for the next detection.
- **Attack_Cadence**: The time between consecutive attacks for an enemy, currently `Mathf.Max(_attackRecovery, timeBetweenAttacks)` with no variation.
- **Cadence_Jitter**: A bounded random variation applied to the Attack_Cadence so enemies do not attack in synchronized lockstep.
- **Approach_Offset**: A bounded lateral/angled displacement added to the chase destination so enemies do not all converge on a dead-straight beeline toward the player.
- **Patrol_Pause**: A brief randomized idle period inserted between patrol walk points, during which the enemy holds position.
- **Movement_Blend**: The data driving the movement animation from the NavMesh agent's current speed, replacing the hard Idle/Walk binary switch.
- **Behavior_Logic**: The pure C# classes (seeded where randomness is involved) that compute Facing_Turn steps, Reaction_Delay timing, Cadence_Jitter, Approach_Offset, and Patrol_Pause timing, separable from the `EnemyAI` MonoBehaviour and mirroring the existing `PreferredDistanceResolver` / `TelegraphBeat` pattern.

## Requirements

### Requirement 1: Reduced retreat speed while kiting

**User Story:** As a melee player, I want ranged enemies to retreat more slowly than they chase, so that I have a fair chance to close the gap and pressure them.

#### Acceptance Criteria

1. WHILE a Ranged_Enemy is Kiting, THE movement layer SHALL set the NavMesh agent speed to a configurable Retreat_Speed_Multiplier applied to the Ranged_Enemy's chase/approach speed, where Retreat_Speed_Multiplier is in the range 0.1 to 0.9 inclusive.
2. WHEN a Ranged_Enemy stops Kiting, THE movement layer SHALL restore the NavMesh agent speed to the Ranged_Enemy's chase/approach speed within 1 physics frame of the Kiting state ending.
3. THE Retreat_Speed_Multiplier SHALL be exposed as a serialized configurable value with a default of 0.5 and SHALL be clamped to the range 0.1 to 0.9 inclusive on load.
4. WHILE a Ranged_Enemy is not Kiting, THE movement layer SHALL set the NavMesh agent speed equal to the Ranged_Enemy's chase/approach speed with no retreat reduction applied.
5. IF the configured Retreat_Speed_Multiplier is outside the range 0.1 to 0.9 inclusive, THEN THE movement layer SHALL clamp the value to the nearest bound and apply the clamped value.

### Requirement 2: Tunable standoff margin

**User Story:** As a designer, I want the standoff distance and margin to be tunable, so that I can keep ranged enemies at distance without making them unreachable.

#### Acceptance Criteria

1. THE Standoff_Distance SHALL be derived from the engagement band multiplied by a configurable standoff fraction in the range 0.0 to 1.0 (default 0.45), such that the fraction being below 1.0 keeps the Standoff_Distance inside the engagement band and therefore reachable.
2. THE repositioning target SHALL be derived from the Standoff_Distance multiplied by a configurable standoff margin in the range 1.0 to 2.0 (default 1.15).
3. WHEN the player is within the Standoff_Distance, THE Ranged_Enemy SHALL attempt to reposition to the Standoff_Distance multiplied by the standoff margin.
4. WHEN the player is at or beyond the Standoff_Distance, THE Ranged_Enemy SHALL NOT reposition for standoff that frame.
5. WHILE a Ranged_Enemy is repositioning for standoff, THE Ranged_Enemy SHALL NOT fire that frame.
6. WHERE the standoff fraction or margin is adjusted, THE Ranged_Enemy SHALL apply the adjusted values without requiring code changes to the attack path.

### Requirement 3: Bounded retreat window and cooldown

**User Story:** As a melee player, I want a ranged enemy's retreat to be bounded in duration, so that it cannot kite me indefinitely across the arena.

#### Acceptance Criteria

1. WHILE a Ranged_Enemy has been continuously Kiting for a duration equal to or greater than its configurable Retreat_Window (default 3 seconds, configurable range 0.5 to 30 seconds), THE Ranged_Enemy SHALL exit the Kiting state and stop increasing its distance from the player.
2. WHEN a Ranged_Enemy exits the Kiting state because its Retreat_Window elapsed, THE Ranged_Enemy SHALL enter a Retreat_Cooldown state for a duration equal to its configurable Retreat_Cooldown (default 2 seconds, configurable range 0.1 to 30 seconds).
3. WHILE a Ranged_Enemy is in the Retreat_Cooldown state, THE Ranged_Enemy SHALL NOT initiate or perform Kiting.
4. WHEN a Ranged_Enemy's Retreat_Cooldown duration elapses, THE Ranged_Enemy SHALL exit the Retreat_Cooldown state and become eligible to Kite again.
5. THE Ranged_Enemy SHALL expose Retreat_Window and Retreat_Cooldown as serialized Inspector-configurable duration values, each measured in seconds and constrained to be greater than 0.

### Requirement 4: Preserved difficulty and existing behavior

**User Story:** As a designer, I want the kiting changes to preserve overall difficulty and existing combat behavior, so that only the melee-vs-ranged fairness improves.

#### Acceptance Criteria

1. WHEN a Ranged_Enemy begins Kiting and the measured distance to the player is less than the Standoff_Distance, THE Ranged_Enemy SHALL set its NavMesh agent destination to a point directly along the player-to-enemy vector such that the resulting position restores the distance to the player to within plus or minus 0.5 units of the standoff reposition target.
2. WHILE a Ranged_Enemy is Kiting, THE Ranged_Enemy SHALL skip its attack action for that frame, resulting in zero attacks initiated while the Kiting state is active.
3. THE Ranged_Enemy movement SHALL be issued exclusively through NavMesh.SamplePosition followed by agent.SetDestination, and SHALL NOT modify the enemy Transform position directly.
4. WHERE an archetype does not report a standoff value, THE Ranged_Enemy SHALL leave that archetype's movement destination, attack cadence, and attack-skip behavior identical to the pre-change behavior, producing no observable difference in movement path or attack timing.
5. IF NavMesh.SamplePosition fails to return a valid point for a Ranged_Enemy in the current frame, THEN THE Ranged_Enemy SHALL retain its current position without issuing a new destination AND SHALL skip firing for that frame.
6. IF a Ranged_Enemy's NavMesh agent is unavailable (null, disabled, or not placed on a NavMesh), THEN THE Ranged_Enemy SHALL fall through to its existing non-standoff behavior without raising an error that interrupts the combat loop.

### Requirement 5: Red telegraph color for every attack

**User Story:** As a player, I want every enemy attack's danger zone to be red, so that I instantly recognize incoming damage regardless of which enemy or attack it is.

#### Acceptance Criteria

1. WHEN an Enemy_Attack begins its Windup, THE Telegraph_System SHALL render the attack's danger-zone Telegraph using a single predefined red base color, independent of the attack type.
2. THE Telegraph_System SHALL apply the same red base color to all Enemy_Attack types (replacing the previous per-attack base colors: Warm orange, Frost cyan, hook purple, hazard orange, and shot red), such that no danger-zone Telegraph displays a non-red base color.
3. WHILE an Enemy_Attack is winding up, THE Telegraph_System SHALL compute the displayed Telegraph color by linearly interpolating from the red base color toward white by a factor of 0.65 multiplied by the Windup elapsed fraction (0.0 to 1.0), so that at elapsed fraction 0.0 the color is the pure red base and as the fraction approaches 1.0 the color is the red base lightened toward white by up to 65 percent.
4. THE Telegraph_System SHALL leave non-telegraph combat visuals (frost boundaries, protection auras, and chilled-player rings) unchanged from their existing colors, and SHALL apply the red standardization only to danger-zone attack Telegraphs.

### Requirement 6: Growing/filling telegraph within the attack area

**User Story:** As a player, I want the red telegraph to grow and fill the attack's area as it winds up, so that I can read the exact danger zone and time my dodge before the hit lands.

#### Acceptance Criteria

1. WHILE an Enemy_Attack is winding up, THE Telegraph_System SHALL render a filled Telegraph surface covering the Attack_Area in addition to the existing outline, such that the filled surface is visible every rendered frame of the Windup.
2. WHILE an Enemy_Attack is winding up, THE Telegraph_System SHALL set Fill_Progress to a normalized value in the range 0.0 to 1.0, equal to 0.0 at Windup start and reaching 1.0 at Impact, and SHALL ensure Fill_Progress is non-decreasing between any two consecutive frames.
3. WHILE an Enemy_Attack is winding up, THE Telegraph_System SHALL confine every point of the filled Telegraph to the resolved Attack_Area, such that any sampled point of the filled surface returns true from the Attack_Area shape test (Contains) for the active shape (Circle, Ring, or Cone), and no filled point lies outside that shape.
4. WHEN Fill_Progress reaches 1.0 at Impact, THE Telegraph_System SHALL display the filled Telegraph covering the full resolved Attack_Area, with the filled extent matching the same shape parameters (center, forward, reach, inner radius, angle) used by the damage test at Impact.
5. IF the Windup duration is zero or non-positive, THEN THE Telegraph_System SHALL render the filled Telegraph at Fill_Progress 1.0 for a single frame before removal.
6. WHEN an Enemy_Attack ends, whether Impact is resolved or the attack is cancelled, THE Telegraph_System SHALL remove the filled Telegraph and its outline within the same frame the attack ends, leaving no residual Telegraph visual.

### Requirement 7: Telegraph coverage for currently-unwarned attacks

**User Story:** As a player, I want attacks that currently show no ground warning to also display a telegraph, so that no enemy attack is unreadable.

#### Acceptance Criteria

1. WHEN an Enemy_Attack that previously suppressed its ground warning begins its Windup, THE Telegraph_System SHALL render a red, filling Telegraph covering the attack's Attack_Area, with the fill progressing from 0% at Windup start to 100% at Windup end.
2. WHEN an Enemy_Attack is routed through the attack execution path, THE Telegraph_System SHALL render a Telegraph for that attack, including straight-shot and projectile-launch attack types, regardless of the attack's prior warning-suppression setting.
3. WHILE a launched projectile travels toward its target, THE Telegraph_System SHALL increase the visual intensity of the existing projectile approach telegraph as the remaining distance to the target decreases, reaching maximum intensity at the moment of impact.
4. IF an Enemy_Attack routed through the attack execution path cannot resolve a valid Attack_Area, THEN THE Telegraph_System SHALL skip Telegraph rendering for that attack and allow the attack to proceed without interrupting execution.

### Requirement 8: Preserved telegraph timing and damage rules

**User Story:** As a designer, I want the new telegraphs to respect existing timing and damage guarantees, so that readability improves without changing how attacks resolve.

#### Acceptance Criteria

1. THE Telegraph_System SHALL enforce a minimum Windup duration equal to MinWindupSeconds with a floor of 0.25 seconds, such that no Impact resolves before the elapsed Windup time reaches that duration.
2. THE Telegraph_System SHALL resolve at most one damage beat per Enemy_Attack across the union of its telegraphed Attack_Areas, preserving the existing single-beat rule.
3. WHEN an Enemy_Attack is cancelled before the Windup duration elapses, THE Telegraph_System SHALL remove the associated Telegraph within 1 render frame and SHALL NOT resolve a damage beat for that Enemy_Attack.
4. WHILE an Enemy_Attack is in Windup, THE Telegraph_System SHALL set Fill_Progress and red-intensity to the Windup elapsed fraction (elapsed Windup time divided by total Windup duration), clamped to the range 0.0 to 1.0.
5. WHEN the Windup elapsed fraction reaches 1.0 at Impact, THE Telegraph_System SHALL set Fill_Progress and red-intensity to 1.0.
6. IF a configured MinWindupSeconds value is below the 0.25 second floor, THEN THE Telegraph_System SHALL use 0.25 seconds as the effective minimum Windup duration.

### Requirement 9: Separable, testable logic

**User Story:** As a developer, I want the new kiting and telegraph logic to live in pure, testable classes, so that behavior can be verified with property-based tests consistent with the existing codebase pattern.

#### Acceptance Criteria

1. THE Retreat_Speed_Multiplier selection, Retreat_Window, and Retreat_Cooldown decision logic SHALL be implemented in pure C# classes separable from MonoBehaviour, mirroring the existing PreferredDistanceResolver pattern.
2. THE Fill_Progress and red-intensity mapping from the Windup fraction SHALL be implemented in pure C# logic separable from rendering, mirroring the existing TelegraphBeat and TelegraphIntensity pattern.
3. THE Fill_Progress mapping SHALL produce output in the range 0.0 to 1.0 and SHALL be monotonically non-decreasing as the Windup fraction increases from 0.0 to 1.0.
4. THE retreat decision logic SHALL be a pure function of elapsed Kiting time, Retreat_Cooldown state, and configured durations, producing identical results for identical inputs.
5. IF the Windup fraction supplied to the Fill_Progress or red-intensity mapping is outside the range 0.0 to 1.0, THEN THE mapping SHALL clamp the input to the nearest bound before computing its output.

### Requirement 10: Smooth turning toward the player

**User Story:** As a player, I want enemies to rotate to face me over time instead of snapping instantly, so that their movement reads as natural rather than robotic.

#### Acceptance Criteria

1. WHILE an enemy is orienting toward the player, THE Enemy_Behavior SHALL rotate its Facing_Turn toward the player-to-enemy horizontal direction at a rate no greater than a configurable Angular_Speed measured in degrees per second, instead of setting the rotation instantly.
2. THE Angular_Speed SHALL be exposed as a serialized configurable value with a default of 540 degrees per second and SHALL be clamped to the range 90 to 1440 degrees per second inclusive on load.
3. WHILE the player's horizontal direction remains fixed relative to the enemy, THE Enemy_Behavior SHALL reduce the signed angle between the enemy forward direction and the player direction toward 0 degrees on each subsequent frame until the remaining angle is 0.5 degrees or less, at which point the enemy forward direction SHALL be treated as facing the player.
4. WHEN the enemy forward direction is within 0.5 degrees of the player direction, THE Enemy_Behavior SHALL permit any facing-gated action (including attack initiation and standoff repositioning) to proceed exactly as it does under the existing behavior.
5. IF the player-to-enemy horizontal direction has a squared magnitude at or below Mathf.Epsilon, THEN THE Enemy_Behavior SHALL leave the enemy rotation unchanged for that frame.
6. THE Facing_Turn step computation (producing the next rotation from the current rotation, the target direction, the Angular_Speed, and the frame delta time) SHALL be implemented in Behavior_Logic separable from the MonoBehaviour and SHALL converge the forward direction to the target direction without overshooting past it.

### Requirement 11: Perception reaction delay

**User Story:** As a player, I want enemies to take a brief moment to notice me before reacting, so that they feel like they are perceiving me rather than reacting like a machine.

#### Acceptance Criteria

1. WHEN an enemy's perception transitions from the player not being in sight to the player being in sight, THE Enemy_Behavior SHALL wait for a configurable Reaction_Delay before beginning to chase or attack, holding its pre-detection behavior during the delay.
2. THE Reaction_Delay SHALL be exposed as a serialized configurable value with a default of 0.3 seconds and SHALL be clamped to the range 0.0 to 2.0 seconds inclusive on load.
3. WHILE the Reaction_Delay timer is elapsing and the player remains in sight, THE Enemy_Behavior SHALL NOT initiate chase or attack actions.
4. WHEN the Reaction_Delay has elapsed and the player remains in sight, THE Enemy_Behavior SHALL begin chase or attack behavior governed by the existing in-sight and in-attack-range gates with no further reaction delay for as long as the player stays continuously in sight.
5. WHEN the player remains continuously out of sight for a duration equal to or greater than a configurable Sight_Loss_Reset (default 1.0 seconds, configurable range 0.0 to 10.0 seconds inclusive), THE Enemy_Behavior SHALL re-arm the Reaction_Delay so the next detection incurs the delay again.
6. WHERE the Reaction_Delay is configured to 0.0 seconds, THE Enemy_Behavior SHALL begin chase or attack behavior on the same frame the player enters sight, matching the existing pre-change reaction timing.
7. THE Enemy_Behavior SHALL preserve all existing perception gates (sight range, attack range, ranged-archetype engagement band, melee proximity, control-lock, and death checks) unchanged, applying the Reaction_Delay only as an additional gate on beginning chase or attack.

### Requirement 12: Attack cadence jitter

**User Story:** As a player, I want enemies to vary their attack timing slightly, so that groups of enemies do not attack in mechanical synchronized lockstep.

#### Acceptance Criteria

1. WHEN an enemy computes the next allowed attack time after completing an attack, THE Enemy_Behavior SHALL apply a Cadence_Jitter to the base Attack_Cadence before scheduling the next attack.
2. THE Cadence_Jitter SHALL be expressed as a configurable fractional variation of the base Attack_Cadence, exposed as a serialized value with a default of 0.15 (plus or minus 15 percent) and clamped to the range 0.0 to 0.5 inclusive on load.
3. WHEN the Cadence_Jitter is applied, THE Enemy_Behavior SHALL select the effective interval uniformly within the base Attack_Cadence scaled by (1 minus Cadence_Jitter) through the base Attack_Cadence scaled by (1 plus Cadence_Jitter).
4. THE Enemy_Behavior SHALL enforce the existing recovery and minimum-interval floor, such that the effective interval is never less than Mathf.Max(_attackRecovery, timeBetweenAttacks) is intended to guarantee today, and the effective interval SHALL always be greater than 0.
5. WHERE the Cadence_Jitter is configured to 0.0, THE Enemy_Behavior SHALL schedule the next attack at the base Attack_Cadence with no variation, matching the existing deterministic timing.
6. THE Cadence_Jitter computation (mapping a base interval, a jitter fraction, a floor, and a random sample to an effective interval) SHALL be implemented in Behavior_Logic separable from the MonoBehaviour and SHALL never return a negative or zero interval.

### Requirement 13: Natural chase approach variation

**User Story:** As a player, I want chasing enemies to approach along slightly varied paths rather than all stacking on one straight line, so that a group closing in reads as a crowd rather than a formation.

#### Acceptance Criteria

1. WHILE an enemy is chasing the player, THE Enemy_Behavior SHALL set the NavMesh agent destination to the player position displaced by a bounded Approach_Offset, instead of the raw player position.
2. THE Approach_Offset magnitude SHALL be exposed as a serialized configurable value with a default of 1.5 units and SHALL be clamped to the range 0.0 to 4.0 units inclusive on load.
3. THE Approach_Offset SHALL be refreshed on a configurable interval, exposed as a serialized value with a default of 1.0 seconds and clamped to the range 0.1 to 10.0 seconds inclusive on load, and SHALL remain fixed between refreshes so the enemy does not jitter frame to frame.
4. WHILE an enemy is chasing with an Approach_Offset applied, THE Enemy_Behavior SHALL continue to reduce the distance to the player over time such that the enemy still reaches its attack range.
5. THE Enemy_Behavior SHALL issue chase movement exclusively through the NavMesh agent destination and SHALL NOT modify the enemy Transform position directly when applying the Approach_Offset.
6. IF applying the Approach_Offset would place the destination off the NavMesh, THEN THE Enemy_Behavior SHALL fall back to the player position as the chase destination for that refresh.
7. THE Enemy_Behavior SHALL NOT apply the Approach_Offset while a Ranged_Enemy is Kiting or repositioning for standoff, so the approach variation does not fight the standoff and kiting behavior defined in Requirements 1 through 4.
8. WHERE the Approach_Offset magnitude is configured to 0.0, THE Enemy_Behavior SHALL chase directly toward the player position, matching the existing beeline behavior.

### Requirement 14: Patrol idle pauses

**User Story:** As a player, I want patrolling enemies to occasionally pause between destinations, so that patrol movement looks less mechanical than constant motion.

#### Acceptance Criteria

1. WHEN an enemy reaches its current patrol walk point, THE Enemy_Behavior SHALL enter a Patrol_Pause for a randomized duration before selecting the next walk point.
2. THE Patrol_Pause duration SHALL be selected uniformly within a configurable minimum-to-maximum range, exposed as serialized values with a default range of 0.5 to 1.5 seconds, where the minimum is clamped to be greater than or equal to 0.0 and the maximum is clamped to be greater than or equal to the minimum on load.
3. WHILE an enemy is in a Patrol_Pause, THE Enemy_Behavior SHALL hold the enemy at its current position and SHALL NOT select or move toward a new walk point until the pause duration elapses.
4. WHEN a Patrol_Pause duration elapses, THE Enemy_Behavior SHALL resume patrol by selecting the next walk point using the existing random walk-point selection and ground check unchanged.
5. IF the player enters sight during a Patrol_Pause, THEN THE Enemy_Behavior SHALL exit the Patrol_Pause and proceed to the perception-gated chase or attack behavior without waiting for the remaining pause duration.
6. WHERE the Patrol_Pause maximum duration is configured to 0.0 seconds, THE Enemy_Behavior SHALL patrol with no idle pause between walk points, matching the existing continuous-motion behavior.
7. THE Patrol_Pause timer logic (deciding whether a pause is active and when it elapses from the configured range and a random sample) SHALL be implemented in Behavior_Logic separable from the MonoBehaviour.

### Requirement 15: Speed-based movement animation blending

**User Story:** As a player, I want enemy movement animation to reflect how fast the enemy is actually moving, so that motion reads naturally instead of flipping between a hard idle and walk.

#### Acceptance Criteria

1. WHILE an enemy is not in an attack windup or recovery window, THE Enemy_Behavior SHALL drive the movement animation from the NavMesh agent's current speed by setting a blend parameter, instead of performing a hard binary switch between the Idle and Walk animation states.
2. THE Enemy_Behavior SHALL compute the Movement_Blend value as the agent's current horizontal speed normalized against the agent's configured maximum speed, clamped to the range 0.0 to 1.0.
3. WHILE an enemy is in an attack windup or recovery window (an attack routine is active, or an attack was taken and the next attack time has not yet been reached), THE Enemy_Behavior SHALL NOT override the attack animation with the movement animation, preserving the existing attack-animation guard.
4. IF the configured animator does not expose the blend parameter used for Movement_Blend, THEN THE Enemy_Behavior SHALL fall back to the existing Idle/Walk state selection based on whether the agent velocity squared magnitude exceeds Mathf.Epsilon, without raising an error that interrupts the update loop.
5. IF the animator or agent reference is null, THEN THE Enemy_Behavior SHALL skip movement animation updates for that frame, matching the existing null-guard behavior.
6. THE Movement_Blend normalization (mapping a current speed and a maximum speed to a 0.0 to 1.0 value) SHALL be implemented in Behavior_Logic separable from the MonoBehaviour and SHALL clamp its output to the range 0.0 to 1.0.

### Requirement 16: Separable, testable natural-behavior logic

**User Story:** As a developer, I want the natural-behavior decision and math logic to live in pure, seeded, testable classes, so that behavior can be verified with property-based tests consistent with the existing codebase pattern.

#### Acceptance Criteria

1. THE Facing_Turn step, Reaction_Delay timer, Cadence_Jitter, Approach_Offset, and Patrol_Pause timer logic SHALL each be implemented in Behavior_Logic as pure C# classes separable from the MonoBehaviour, mirroring the existing PreferredDistanceResolver and TelegraphBeat patterns.
2. THE Behavior_Logic SHALL produce identical outputs for identical inputs, and WHERE randomness is involved, THE Behavior_Logic SHALL accept an explicit seed or random source so that a given seed and input produce a deterministic, repeatable result.
3. THE Facing_Turn step logic SHALL converge the enemy forward direction toward the target direction without overshooting, such that the signed angle to the target after a step has the same or smaller absolute value than before the step and never changes sign due to overshoot.
4. THE Cadence_Jitter logic SHALL return an effective interval that is strictly greater than 0 and never below the configured floor, for all jitter fractions within the configured range and all random samples.
5. THE Movement_Blend normalization logic SHALL return a value within the range 0.0 to 1.0 for all non-negative current speeds and all positive maximum speeds.
6. IF an input supplied to any Behavior_Logic mapping is outside its documented valid range, THEN THE Behavior_Logic SHALL clamp the input to the nearest bound before computing its output rather than producing an out-of-range or undefined result.
