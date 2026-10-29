# Autorização direta do desenvolvedor — GPR-01.2b

Fonte: texto colado enviado nesta conversa em 28/10/2026. Reprodução da solicitação, não evidência de teste.

# TECH GUY — GPR-01.2 Human Feedback and GPR-01.2b Authorization

## 1. Developer feedback

I have physically tested the Titanic Fists mutation delivered in GPR-01.2.

The current implementation is technically correct:

- The shockwave is visible.
- The effect is noticeable.
- It damages enemies correctly.
- The mutation triggers according to the previously approved implementation.
- The developer confirms that the technical functionality matches the original specification.

However, the gameplay result feels **underwhelming**.

The developer wants a more transformative mutation, closer to the philosophy of The Binding of Isaac.

The previous implementation is accepted as a technical foundation, but its gameplay behavior is not accepted as the final Titanic Fists design.

## 2. New approved gameplay direction

**Replace the instantaneous shockwave with a slow-moving, ranged wind tornado/projectile.**

The desired behavior:

1. The player performs the existing Gauntlet basic combo using LMB.
2. Every successfully executed final combo attack releases one wind tornado.
3. The tornado launches in the committed direction of that attack.
4. It travels forward through the combat arena at a deliberately readable speed.
5. It can damage valid enemies encountered along its trajectory.
6. It disappears after completing its travel distance or reaching a valid termination condition.
7. It must remain visually distinct from the original punch.
8. It must work even if the original finishing punch hits no enemy.

**Critical requirement: tornado creation must NOT depend on confirmed HP damage from the original punch.**

The trigger is successful execution of the authored final combo attack, not damage confirmation.

Do not trigger from an uncommitted or canceled attack, an intermediate combo stage, or a merely requested LMB input.

Do not introduce hold-to-repeat or additional basic attacks.

## 3. Authorization — GPR-01.2b ONLY

Implement the revised Titanic Fists behavior.

Do not start GPR-01.3 or later activities.

Do not implement Conductor, Detonation, modular skill swapping, new weapon specials, or general combat balancing.

Before editing, read AGENTS.md, the GPR-01.1/GPR-01.2 reports, canonical design documents, and applicable Tech Guy/gamedev skills.

Verify the actual Unity Editor and project through MCP.

Preserve existing worktree changes, protected Nexus scenes, assets, prefabs, GUIDs, saved preferences, and player progression.

## 4. Preserve the current combat foundation

The following must remain unchanged:

- WASD movement.
- Independent mouse aiming.
- LMB combo sequence and timing.
- Q/E/R active skills.
- RMB Asura special.
- Existing attack animations, recovery, damage and cancellation rules.
- Existing weapon identities.
- Existing GI combat reactions and analytics.
- Existing reward acquisition and mutation ownership.

Do not create a second basic-attack executor or a parallel damage pipeline.

Modify the current Titanic Fists implementation using the smallest compatible extension.

## 5. Separate emission eligibility from damage confirmation

GPR-01.1 introduced confirmed-HP receipts and child-impact reservations to prevent invalid damage.

That architecture must remain safe.

However, the new design requires a deliberate distinction:

**Projectile emission authorization**
- Granted by the actually committed terminal combo attack with Titanic Fists present in its accepted snapshot.
- Does not require native punch damage.
- Supports legitimate whiffed terminal attacks.
- Does not generate HP damage, statistics, resource gains, Protocol triggers, or additional roots merely by spawning.

**Projectile impact authorization**
- Occurs only when the traveling tornado encounters a valid target.
- Must use the existing combat damage ownership, effective-HP receipts, root/parent provenance, budgets, caps, encounter identity, deduplication, and cancellation rules.
- Must not permit damage from stale or revoked work.
- Must not grant resources based on visual-only events.

Review the current ReserveConfirmedBasicChild contract.

Do not weaken or reinterpret existing confirmed-damage guarantees globally to make the new trigger work.

Prefer a narrowly scoped, explicitly named emission contract with separate impact authorization.

Preserve previous confirmed-HP behavior for unrelated Protocol effects.

## 6. Traveling tornado gameplay

Implement an actual moving combat effect, not an instantaneous overlap query disguised as a projectile.

Requirements:

- Spawn from an appropriate forward offset relative to the committed punch pose.
- Travel along the committed attack direction, independent of later mouse movement or player rotation.
- Use scaled gameplay time and respect existing pause/hitstop conventions.
- Maintain a bounded travel distance and lifetime.
- Move through the world progressively, checking swept or otherwise reliable collision coverage between positions.
- Prevent tunneling through enemies at lower frame rates.
- Respect valid enemy identity, encounter membership, damage eligibility, and solid-world occlusion.
- Avoid damaging enemies through walls.
- Apply damage at most once per target generation per projectile.
- Handle multiple targets along the trajectory, within existing root budget and impact caps.
- Terminate safely when the player changes weapon/context, dies, leaves the encounter, finishes the run, or changes scenes.
- Release all retained root/child reservations and pooled runtime/VFX state on termination.

Do not keep a Protocol root alive indefinitely.

Do not bypass impact caps by spawning a fresh root for each tornado.

Do not generate a new tornado from damage caused by another tornado.

## 7. Experimental tuning

Begin with prototype values, adjustable from one clear definition:

- Maximum travel distance: approximately 5–6 meters.
- Travel speed: approximately 3–4 meters per second.
- Damage: provisionally 0.60x the authored eligible terminal-attack damage reference.
- Per-target hits: once per projectile.
- Projectile visual width: large enough to communicate its damaging corridor.

Treat these as initial tuning hypotheses.

Do not change the existing native punch damage or global balance.

Because the tornado can now spawn after a whiff, determine a stable damage reference from the committed attack snapshot or equivalent authored combat damage source.

Do not derive damage from a receipt that may not exist.

Do not reroll critical hits or duplicate attacker multipliers.

Document precisely which damage value is used and how the coefficient is applied once.

## 8. Visual design

The current angular overlay and amber instant-wave presentation should be revised.

The intended effect is a stylized traveling wind vortex, similar to a small forward-moving tornado launched by a heavy punch.

Requirements:

- Clear wind rotation or swirling motion.
- Noticeable forward movement.
- A readable silhouette during combat.
- Distinct presentation from the original basic impact.
- Appropriate scale and colors for Tech Guy's stylized visual language.
- Readable hit feedback without overwhelming enemy attack telegraphs.
- Reversible active-mutation presentation on the Gauntlet.
- Correct spawning, pooling, reuse and cleanup.

Use reasonable prototype VFX based on existing project systems.

Do not overhaul the character model, rig, animations or scene art.

Avoid unnecessary new assets and permanent prefab modifications.

## 9. Mutation acquisition and lifecycle

Preserve the existing Titanic Fists boon identity and legal reward acquisition.

Verify:

- No boon: unchanged Gauntlet.
- Boon acquired: every committed final combo attack emits one tornado.
- Whiffed final attack: emits tornado.
- Canceled or interrupted terminal before commitment: no tornado.
- Other combo stages: no tornado.
- Weapon swap: no stale tornado or mutation application.
- Return to Gauntlet: mutation remains available according to current run rules.
- End of run: all tornadoes and visual state cleaned.
- New run: no stale mutation.
- Scene change, pause, disable and pooled reuse: safe cleanup.
- No unrelated cooldown or resource changes.

Keep reward tickets, seed determinism, rank limits and inventory compatibility intact.

## 10. Protocol composition remains disabled

For this phase, retain:

`ProtocolBasicEffects.None`

Do not enable Conductor or Detonation on tornado impacts.

The future GPR-01.3 will evaluate the interaction between the traveling tornado and Conductor.

Preserve native Protocol behavior outside this branch.

## 11. Required Unity validation

Run focused EditMode and PlayMode tests through the real Unity MCP connection.

Add red-to-green regression coverage for:

- Every committed terminal producing exactly one tornado.
- Terminal whiff producing a tornado.
- Intermediate combo stages producing none.
- Canceled/uncommitted terminals producing none.
- Progressive travel across multiple frames.
- Correct committed-direction behavior after mouse movement.
- Stable damage after a whiffed native punch.
- Frame-rate-independent distance and reliable swept detection.
- Target generation deduplication.
- No repeated damage to the same target from one projectile.
- Budget exhaustion and cancellation.
- No damage beyond solid occlusion.
- Correct pause/hitstop behavior.
- No Protocol root leakage.
- End-of-run/scene-change/weapon-swap cleanup.
- No extra mana or Asura energy.
- No new Conductor/Detonation cascades.
- Existing GI contracts and combat regressions.
- Existing Bow, Spear, Q/E/R, RMB, movement and input behavior.

Capture actual Game View evidence of:

1. Titanic Fists equipped.
2. A final combo whiff creating a tornado.
3. The tornado visibly traveling across the arena.
4. The tornado damaging an enemy encountered later.
5. Correct cleanup.

If possible, include multiple time-separated frames demonstrating real movement.

Do not represent an instantaneous hitbox or static VFX as a traveling projectile.

Report all failed intermediate attempts and preserve relevant evidence.

Track existing intermittent regression cases separately; do not hide them with retries or weakened assertions.

## 12. Mandatory reporting

Create:

`GPR-01.2b_RELATORIO.md`

Include:

- Exact implementation status.
- Developer feedback addressed.
- Architectural changes and justification.
- Modified/created files.
- Skills actually applied.
- Tornado emission and damage contracts.
- Experimental tuning values.
- Unity test jobs and exact results.
- Game View evidence.
- Lifecycle and persistence checks.
- Regression findings and known limitations.
- Readiness recommendation for GPR-01.3.

Update canonical requirements, technical design, task plan, validation matrix, progress ledger and vertical-slice specification.

Preserve historical GPR-01.2 evidence instead of rewriting the original experiment as though the new tornado had always been planned.

All developer-facing reports must be in Brazilian Portuguese.

## 13. STOP condition

**STOP after GPR-01.2b.**

Do not implement Conductor or Detonation integration until the developer evaluates the revised moving tornado in actual gameplay.