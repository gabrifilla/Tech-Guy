<!-- Pedido direto do desenvolvedor, recebido em texto colado. Não é evidência de execução. -->

# TECH GUY — GPR-01.1 Approval and GPR-01.2 Authorization

## 1. Approval

I have reviewed `GPR-01.1_RELATORIO.md`.

**GPR-01.1 is accepted as technically completed within its authorized scope.**

The reported validation includes:

- 90/90 EditMode tests.
- 120/120 PlayMode tests.
- 210 distinct selected test cases.
- No ignored cases in the final selected runs.

This is not approval of the entire global test suite.

The Basic boon targeting extension, confirmed HP receipts, child reservations, Protocol lineage policy, safe boon acquisition, and compatibility protections are accepted as the foundation for the experimental Gauntlet mutation.

No Titanic Fists gameplay has been human-approved yet.

## 2. Authorization — GPR-01.2 ONLY

Implement the first playable mutation:

**Titanic Fists / Punhos Titânicos**

Do not start GPR-01.3 or later phases.

The goal is to demonstrate a meaningful change in the Gauntlet's existing LMB combo without changing its base timing or introducing a parallel combat executor.

## 3. Mandatory preparation

Before coding:

1. Read AGENTS.md and canonical project documentation.
2. Read the GPR-01.1 report and current vertical-slice design.
3. Read relevant Tech Guy skills: game vision, combat, boon design and Unity acceptance.
4. Apply additional installed gamedev skills when actually available.
5. Verify the actual Unity Editor and project through MCP.
6. Record current Git state, protected assets and prior modifications.
7. Preserve NexusBlockout, gameplay scenes, GUIDs, prefabs, serialized data, player preferences, and run progression.

Reuse the existing combat, Boon, Protocol, damage, reward, snapshot and visual systems wherever possible.

## 4. Titanic Fists — gameplay behavior

Create a Gauntlet-only Basic Mutation boon called **Punhos Titânicos**.

The mutation affects the existing LMB basic combo.

The intended behavior:

- Basic attacks retain their current inputs and combo order.
- The final authored basic combo hit becomes the mutation trigger.
- After the native terminal impact and confirmed-HP batch, an eligible terminal may generate one forward shockwave.
- The shockwave extends beyond the native punch area and can hit additional valid enemies.
- The effect must respect direction, range, hit geometry, target identity and existing combat rules.
- The original native punch remains unchanged.
- The mutation must not introduce extra automatic LMB requests, combo stages or animation changes.
- The shockwave must not become a new Q/E/R or RMB ability.

Use the existing GPR-01.1 receipt/reservation/lineage contracts.

Do not authorize damaging child effects based only on visual impact events.

Preserve the existing documented requirement for confirmed terminal damage. An unsuccessful native terminal must not generate damage through an unauthorized child.

## 5. Provisional prototype parameters

Initial experimental values:

- Shockwave range/width multiplier: 1.5x relative to an explicitly documented reference geometry.
- Shockwave damage coefficient: 0.60x relative to the documented eligible source damage reference.
- Maximum one authorized child reservation per eligible terminal.
- No repeated damage to native victims.
- No repeated damage to the same pooled target generation from the same child.
- No additional resource generation.
- No independent Protocol root.
- Existing root budget and caps must apply.

Avoid applying the 0.60 coefficient more than once.

Do not treat these numbers as final balance approval.

Make tuning parameters explicit, easy to inspect and limited to this mutation.

## 6. Visual transformation

Titanic Fists must produce an identifiable and reversible visual difference.

At minimum:

- A visible alteration or overlay on the Gauntlet when the boon is active.
- A distinct forward shockwave visual during the mutated terminal attack.
- Readable timing corresponding to the actual gameplay effect.
- Clear differentiation between the original punch impact and the secondary shockwave.
- No misleading damage indication when the child has not been authorized.

Prefer reusing existing VFX/materials/visual components and established pooling/lifecycle conventions.

Temporary prototype visuals are acceptable, but they must remain readable during combat.

Do not replace the character model, overhaul animations, alter rigs or redesign the production art direction.

Avoid permanent scene or prefab modifications unless proven necessary and explicitly documented.

## 7. Boon catalog and natural acquisition

Register the production Titanic Fists boon using the existing reward/catalog system.

Requirements:

- Gauntlet Basic must be its valid mutation target.
- Preserve the existing reward eligibility rules and caps.
- Preserve deterministic seed and reward semantics.
- Avoid duplicate or invalid offers.
- Do not expand legacy `Any` targeting to Basic.
- Do not introduce a parallel reward service.
- Verify that Titanic Fists can be offered and acquired through legitimate gameplay reward flow.
- Verify that the mutation is activated only when legally acquired.

Using a debug grant is allowed for isolated testing, but it is not evidence of natural reward acquisition.

If the existing reward pool cannot offer Titanic Fists without changing unrelated reward policy, document the blocker and request approval rather than silently expanding eligibility.

## 8. Mutation lifecycle

Validate the entire lifecycle:

- No boon: normal Gauntlet behavior.
- Boon acquired: mutation becomes active.
- Weapon context changes: no unauthorized mutation effects.
- Return to Gauntlet: correct state restored according to existing run rules.
- End of run: mutation and visuals are removed.
- New run: no stale mutation state.
- Scene changes, pause, disable and pooled target reuse: clean state.
- Existing cooldowns and special resources remain unchanged by acquisition.

Do not implement new skill acquisition, skill replacement or universal rollback behavior.

## 9. Protocol policy

For GPR-01.2, keep:

`ProtocolBasicEffects.None`

The shockwave may deal its own authorized damage under the existing lineage contract.

Do not enable new Conductor or Detonation grants on this branch.

Do not implement A+B or A+B+C compositions yet.

Preserve existing native Protocol behavior outside the new branch.

Do not create extra Protocol roots or alter the existing budgets.

## 10. Preserve existing gameplay

Preserve:

- WASD movement.
- Independent mouse aiming.
- Current LMB combo cadence and timing.
- RMB existing Asura.
- Q/E/R existing skills.
- Existing enemy impact/reaction rules.
- Current base damage and authored ability tuning.
- Pause, focus, UI and input-release behavior.

The developer has previously mentioned:

- Basic attacks may feel slightly too fast.
- Locomotion can look like moonwalking during strafing/backpedaling.

Both remain explicitly deferred game-feel refinements.

Do not retune them now.

## 11. Required validation

Execute relevant Unity EditMode and PlayMode tests through MCP.

Create focused regression coverage for:

- Base Gauntlet without mutation.
- Titanic Fists acquired legitimately.
- Correct final-hit triggering.
- No shockwave damage from an unauthorized whiff or immunity.
- Correct shockwave forward geometry.
- Correct additional target selection.
- Exclusion of native punch victims.
- Effective HP receipts and correct attribution.
- Damage coefficient applied once.
- One child per eligible terminal.
- Budget exhaustion and child cancellation.
- Target pooling and deduplication.
- Correct behavior after pause, scene transitions and run end.
- No persistence of mutation into a new run.
- No cooldown/resource reset on acquisition.
- Existing Q/E/R, RMB Asura, Bow and Spear behavior.
- Existing GI combat, damage, analytics and input regressions.

Capture actual Game View evidence of:

1. Base Gauntlet.
2. Titanic Fists active.
3. A shockwave affecting an additional target.
4. The mutation removed after run cleanup.

Distinguish automated runtime verification from human physical playtesting.

Do not claim the mutation is fun or visually approved without developer gameplay feedback.

## 12. Mandatory report

Create `GPR-01.2_RELATORIO.md` containing:

- Exact scope and status.
- Implementation details.
- Modified and created files.
- Actual skills applied.
- Architectural decisions.
- Mutation data and provisional parameters.
- Reward acquisition evidence.
- Combat and damage test evidence.
- Unity MCP jobs, Console, PlayMode and EditMode outcomes.
- Visual evidence and its limitations.
- Integrity and save/progression observations.
- Known risks and unresolved issues.
- Readiness assessment for GPR-01.3.

Update the master progress ledger, canonical validation matrix, requirements, technical design and tasks.

All reports must be written in Brazilian Portuguese.

## 13. STOP condition

**STOP after GPR-01.2.**

Do not start GPR-01.3, Conductor integration, Detonation integration, weapon-special redesign, or modular skill replacement.

Wait for developer review of the first playable mutation before moving on.