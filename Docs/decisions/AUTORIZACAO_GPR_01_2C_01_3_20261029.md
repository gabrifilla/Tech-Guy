# TECH GUY — GPR-01.2b Human Acceptance, GPR-01.2c Correction and GPR-01.3 Authorization

## 1. Developer feedback and acceptance

The developer has physically tested the GPR-01.2b traveling tornado.

**GPR-01.2b is accepted as functionally successful and enjoyable.**

Confirmed:

- Titanic Fists launches a real traveling tornado.
- The effect is visible and noticeable.
- Enemy damage works as expected.
- The gameplay direction is approved.

The following feedback must be recorded:

### A. Room-clear discrepancy

After completing a combat room, the tornado no longer appears when the player performs the combo. Upon entering a new room containing enemies, it works again.

Investigate this behavior before adding Conductor.

### B. Future tornado game-feel tuning

The tornado currently feels:

- Too fast.
- Too small.
- Difficult to aim accurately.
- More like a small projectile than a broad moving wind vortex.

The developer explicitly accepts deferring these changes.

Do not modify tornado speed, size, distance, damage, attack timing or native combat balance during this activity.

### C. Future content review

The developer also wants dedicated future reviews of:

- Room reward frequency and quality.
- Room and stage design.
- Encounter pacing.
- Boss mechanics and presentation.
- Enemy visual identity and mesh variety.

All enemies currently appearing visually similar is a known concern.

Document these as future design activities, not part of the current implementation.

## 2. Authorization and execution order

Authorize these activities sequentially:

1. **GPR-01.2c — Room-clear tornado lifecycle investigation and narrowly scoped correction.**
2. **GPR-01.3 — Titanic Fists + Conductor synergy.**

GPR-01.3 may begin only after GPR-01.2c has a documented technical result with no blocking regressions.

If the room-clear issue requires a major change to encounter lifetime, combat ownership, or persistence semantics, STOP and report alternatives rather than making an unauthorized architecture change.

Do not implement Detonation integration, Singularity, new specials, modular skill replacement, room redesign or enemy redesign.

## 3. Mandatory preparation

Before modifying code:

- Read AGENTS.md and canonical Tech Guy documentation.
- Read GPR-01.1, GPR-01.2 and GPR-01.2b reports.
- Read the current GPR vertical-slice specification.
- Read current Protocol lineage and budget contracts.
- Discover and apply relevant Tech Guy and installed game-development skills.
- Verify the exact Unity project and Editor through MCP.
- Record branch, HEAD, working tree, existing changes, scenes and protected assets.
- Preserve user saves, preferences and run progression.
- Never overwrite historical validation evidence.

## 4. GPR-01.2c — Room-clear behavior

### Investigate first

Reproduce the reported behavior using the current production encounter lifecycle:

1. Start a valid encounter with Titanic Fists.
2. Execute a final basic combo and verify tornado emission.
3. Complete the room.
4. Attempt another legitimate final combo attack while remaining in that room.
5. Determine whether the attack is accepted and the finalizer's contact callback occurs.
6. Determine whether emission authorization, root ownership, encounter membership or visual spawning rejects the event.
7. Enter another room and confirm the effect resumes.

Distinguish between:

- An attack input being unavailable.
- A finalizer not being committed.
- A committed finalizer failing to request emission.
- An encounter-scoped reservation being invalid.
- A valid projectile not being presented.

### Desired behavior

If normal basic attacks remain playable after a room is completed, Titanic Fists should retain consistent visual feedback for legitimate terminal combo attacks.

However:

- Do not revive or reopen the completed encounter.
- Do not permit damage against invalid or completed encounter targets.
- Do not create an unauthorized Protocol root.
- Do not grant resources, statistics or reward progress from cosmetic-only activity.
- Do not let inactive-encounter visual work survive room transitions improperly.
- Do not weaken global cancellation or session-lifetime contracts.

If necessary, introduce a narrow presentation-only path for legitimate noncombat terminal attacks, clearly separate from combat-damage authorization.

Use existing presentation infrastructure. Do not duplicate the entire tornado combat runtime.

### Validation

Test:

- Active encounter tornado emission and damage.
- Last-enemy kill and completion transition.
- Finalizer executed after the room is completed.
- No enemies present.
- Transition into the next room.
- Completed encounter targets remaining ineligible for damage.
- Cleanup on scene change, run end, disable and weapon swap.
- No leaked root reservations or pooled VFX.

Generate `GPR-01.2c_RELATORIO.md` with confirmed diagnosis, changes, Unity evidence and regression results.

STOP if a major design decision is necessary.

## 5. GPR-01.3 — First composed boon synergy

Implement the interaction:

**Titanic Fists + Conductor**

Use the existing Titanic Fists Mutation and existing Conductor Protocol.

Do not create duplicate reward definitions unless an existing catalog compatibility issue makes that strictly necessary.

### Required behavior

With Titanic Fists alone:

- The traveling tornado behaves exactly as in the accepted GPR-01.2b, subject to the narrowly scoped 01.2c lifecycle fix.
- No new Conductor branch is authorized.

With Conductor alone:

- Preserve all existing native Conductor behavior.
- No new Titanic Fists effect appears.

With both:

- The tornado remains an independently traveling wind projectile.
- Valid damaging tornado impacts become eligible to produce the approved Conductor effect.
- The electricity originates from the tornado's actual impact context, not automatically from the player position.
- Electric effects must interact with valid targets under the existing Protocol constraints.
- The combined behavior must be visually and mechanically distinguishable from either boon alone.

Do not implement automatic Conductor activation merely when the tornado spawns or when it visually overlaps a target without a valid damage event.

## 6. Protocol lineage and safety

Reuse the existing `ProtocolRuntime`, `ProtocolBudget`, root/parent attribution, effective-HP receipts and damage queue.

Extend only the specific Titanic Fists branch policy necessary for Conductor.

Requirements:

- `ProtocolBasicEffects.None` remains the default.
- Explicit Conductor authorization requires legitimate ownership of both effects.
- Do not grant Detonation in this activity.
- No Conductor-to-Conductor infinite recursion.
- No tornado spawning another tornado.
- No new Protocol root for each electric arc.
- No root-budget renewal.
- No repeated electric effects from duplicate colliders or repeated frames.
- Respect target identity and pooled generation.
- Respect impact caps, line of sight and encounter ownership.
- No unauthorized additional mana, Asura energy or resource generation.
- No double-counting of HP or damage statistics.
- Pause, hitstop, cancellation, room completion and scene transitions must invalidate or suspend work correctly according to existing contracts.

The moving tornado may create several legitimate damage events over time. Define explicitly when and how many Conductor emissions are permitted per projectile, victim and root.

Do not silently expand the existing 32-impact budget.

If the current budget causes effects to be skipped in dense crowds, report the limitation rather than increasing it without approval.

## 7. Visual composition

The combination must have a readable visual identity:

- Titanic Fists alone: existing wind tornado.
- Conductor alone: existing electric effect.
- Both: visible electrical arcs or conduction associated with the moving tornado's valid impacts.

Use existing VFX/materials/pooling systems where practical.

Avoid changing the tornado's size, movement speed, native damage or animation timing in this phase.

Do not obscure enemy telegraphs or create visual damage indicators unsupported by real gameplay events.

The result should feel like a composed effect, not two unrelated particle systems.

## 8. Required comparisons

Provide reproducible tests and observations for:

1. Base Gauntlet.
2. Titanic Fists only (A).
3. Conductor only (B).
4. Titanic Fists + Conductor (A+B).
5. A+B with a whiffed native finisher but valid later tornado impact.
6. A+B when the tornado never hits an enemy.
7. A+B when the target is immune or takes zero effective HP damage.
8. A+B when root impact capacity is exhausted.
9. A+B after encounter completion and during room transitions.

Prove that A+B introduces an actual new interaction not already provided by native Conductor effects.

Test legal acquisition through the existing reward/ticket system where feasible. Clearly distinguish a natural reward path from assisted fixture grants.

## 9. Preserve gameplay

Do not modify:

- Current WASD movement.
- Independent mouse aiming.
- LMB combo timing.
- Q/E/R skills.
- RMB Asura.
- Existing Bow/Spear behavior.
- Current tornado travel speed, size, distance or damage.
- Native Protocol balance.
- Enemy AI, spawn systems or room layouts.
- Boss mechanics, assets or enemy meshes.
- Player progression and saved settings.

Basic attack speed, directional moonwalk, tornado aiming/size/speed, room rewards and visual diversity remain separate future tasks.

## 10. Unity validation

Use real Unity MCP and Unity Test Framework.

Create focused red-to-green coverage for the new behavior.

Run relevant existing regressions for:

- Basic attacks and finalizers.
- Titanic Fists alone.
- Conductor alone.
- Protocol roots, budgets and cancellations.
- Damage/HP/statistics.
- Reward compatibility and acquisition.
- Movement, input, Q/E/R and RMB Asura.
- Bow/Spear.
- Encounter lifecycle and room clear.
- Pause, focus, disable and scene transitions.

Capture Game View evidence, ideally with time-separated frames of a moving electrically enhanced tornado.

Record exact test counts, failing intermediate attempts, Unity Console and limitations.

Do not call selected passing filters a global-suite approval.

Do not substitute automated input for physical human playtesting.

## 11. Mandatory reporting

For each activity, generate a separate Brazilian Portuguese Markdown report:

- `GPR-01.2c_RELATORIO.md`
- `GPR-01.3_RELATORIO.md`

Each must contain:

- Scope and status.
- Confirmed findings and technical decisions.
- Modified files.
- Skills applied.
- Exact tests, results and job identifiers.
- Unity MCP and Game View evidence.
- Protocol lineage and damage guarantees.
- Encounter lifecycle and cleanup results.
- Regressions, unresolved concerns and performance limitations.
- Next activity and approval requirements.

Update the canonical validation matrix, progress ledger, requirements, technical design, tasks and vertical-slice specification.

Preserve historical documents and evidence from previous phases.

## 12. Future design backlog

Record dedicated follow-up planning tasks for:

- Tornado movement speed, width, reach, readability and aiming.
- Basic attack cadence.
- Directional locomotion animations.
- Room rewards and reward economy.
- Room/stage layouts and encounter pacing.
- Enemy model and visual variety.
- Elite and boss identity.
- Future Singularity mutation:
  traveling tornado + gravitational attraction + bounded damage + final collapse/launch, respecting enemy CC immunity and stagger rules.

These are not implementation authorizations.

## 13. STOP condition

**STOP after GPR-01.3.**

Do not begin GPR-01.4/Detonation, Singularity, new abilities, enemy redesign or level-design implementation.

Present both reports and request developer review before the next stage.