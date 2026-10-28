# Tech-Guy — Repository Instructions for Codex

## Context and Scope

* Unity 3D project using C#.
* Read the applicable project instructions and current requirements, design, and tasks before changing the affected system.
* Follow the agreed implementation stages. Do not pull future-stage features into the current task without a task requirement or user instruction.
* Preserve existing behavior outside the requested change. Report conflicts between code and specifications instead of silently redefining the contract.
* Fix defects demonstrated by tests within the current scope. Do not weaken a correct test merely to match faulty implementation.

## Product Vision and Source of Truth

* **Game identity:** Tech-Guy is a Unity 3D single-player action-RPG roguelite. A software developer is trapped inside his own computer, explores interconnected digital worlds and attempts to escape an unknown system. The **Nexus** is the safe hub between expeditions; an equipped weapon is an intelligent, expressive companion with its own personality, humor, and combat identity. Use this fiction when designing abilities, names, effects, interfaces, and future dialogue, without inventing unrequested narrative features.
* **Inspirations are complementary, not blueprints:** Diablo IV and Lost Ark primarily inform combat feel, readable large-scale encounters, player abilities, and enemy reactions. Hades informs runs and parts of the stylized visual language; Path of Exile 2, Shape of Dreams and The Binding of Isaac inform skill transformation and emergent synergies; Aion 2 may inform combat/build exploration. Spider-Verse inspires visual dynamism and variety across digital worlds. High on Life informs weapon personality, **not** a shift to first-person shooter design. Do not copy any referenced game's complete HUD, gameplay system, proprietary assets, or presentation wholesale.
* **Document roles:** `README.md` is authoritative for high-level story, product identity and intended HUD, but parts of its gameplay/boon lists may describe an older prototype. Read the current approved `requirements.md` / `design.md` / `tasks.md` and phase handoffs wherever they actually exist; do not assume locations or filenames. `BoonReworkExpandedDesign.md` captures boon vision and candidate content; `BoonReworkExecutionAudit.md` is a dated **reported** implementation snapshot; `TechGuy_BoonRework_NextIteration.md` outlines proposed validation and next iterations. The old `BoonReworkProposal.md` is background, not a mandate to restore a smaller catalog. Verify files exist and check the current code, Inspector, tests and latest user decision before acting.
* **Resolve discrepancies deliberately:** direct user scope and explicit approved stage decisions govern requested changes; inspect runtime/assets to learn what is really implemented; use design documents for intended behavior and the audit only for claims that still need verification. Write down `specified / implemented / Unity-verified / human-accepted` separately. If two sources conflict materially, state the conflict and avoid silently choosing a different gameplay contract.
* **Do not confuse roadmap with shipping content:** a named boon, ability, procedural feature, enemy behavior, or ending screen in a design document is not evidence that it exists in the build. Conversely, do not reintroduce retired stat-only boons merely because the README still lists them.

## Core Gameplay and Combat Feel

* Preserve a satisfying **boon-free base kit** for each weapon. Boons should transform viable skills, not repair dysfunctional baseline attacks. Runs should yield visibly different decisions, without demanding one exact lucky sequence of rewards.
* **Gauntlet / Manopla:** close-range boxing inspired by Breaker—recognizable jab/cross/uppercut, gap-closing, timed counters, combo cadence, posture breaks, conditional air follow-ups, and the Asura resource/finisher. Contact, animation, impact timing and enemy reaction matter more than indiscriminate visual spectacle.
* **Bow / Arco:** precision and control of distance, marks, deliberate charge, valid projectile interactions, mobility and traps. Keep meaningful aim and positional choices; do not turn every build into passive auto-targeting turrets.
* **Spear / Lança:** reach, thrust/sweep spacing, trajectories, return paths, lines and timing. Differentiate it from Gauntlet AoE and Bow traps; preserve direct weapon handling and positioning.
* Encounter design supports **swarms** of common and differentiated enemies alongside Heavy/Elite/Boss behaviors. Telegraph wind-ups and danger zones must be readable and avoid unavoidable hits. Keep distinct threat roles and fair recovery windows. Boss posture breaks may interrupt an attack according to their profile; they must not be permanently stun-locked.
* **Hit reaction is not generic knockback:** basic hits may stagger/interrupt eligible common enemies; launch/air juggle, knockdown, push, guard break, freeze or pull require an expressly compatible skill and enemy control profile. Heavy, Elite and Boss enemies have resistance/fallback behavior. Never force movement by bypassing collision, navigation, immunity or enemy action-state ownership.
* Use intentional attack phases (startup/active/recovery), buffering, cancellation and committed actions. Distinguish player invulnerability, damage reduction, super armor and stagger immunity rather than granting any accidentally. Pause, hitstop and timescale must suspend gameplay timers consistently.
* Keep player threats, hitboxes, targets and boss telegraphs legible under simultaneous player procs, crowd effects and many enemies. Prefer expressive VFX/SFX/camera feedback without hiding incoming attacks or destabilizing framerate. Profile scenes with 8/32/64 enemies when relevant rather than assuming a performance budget.

## Boon Design and Run Progression

* The design target is **meaningful transformations**, not a catalog dominated by flat damage/health/haste/cooldown bonuses. Change input sequences, origins, geometry, travel, targeting, risk, timing, on-hit reactions or inter-skill interactions. Every standalone Mutation should be playable and worthwhile without its ideal Duo.
* Families and responsibilities: **Mutation** transforms one base skill/slot; **Augment** modifies a specific compatible skill/producer; **Protocol** applies approved universal phenomena (currently Ignite, Frost, Conductor and Detonation per the audit); **Resonance/Duo** rewards a verified combination; **Mythic/Keystone** changes structural rules and stays disabled until its explicit validation/telemetry gate is satisfied.
* Existing graph constraints reported by the audit are **one active Mutation per Q/W/E/R slot, up to two Augments per ability, up to two Duos per run**. Preserve these until an approved change and migration modifies them; these are not permanent creative limits on the number of alternatives available *across* runs.
* Compatibility follows verified **capabilities of the current executor** (projectile, melee, mark, charge, trap, line, return path, proxy, persistent zone, multi-press, etc.), not an old slot label or just a shared name. A replaced ability must not silently invalidate an already acquired Augment/Protocol. Disallow invalid acquisition orders or implement an explicitly approved migration; test either order of compatible acquisitions.
* Each use has a traceable **causal root**; distinguish native hits from derived effects, echoes, DoT ticks and secondary explosions. Preserve the shared proc budget, stable ordering, caps and recursion depth. Derived hits must not mint unauthorized mana/Asura, refresh budgets, duplicate persistent entities, bypass enemy protections, or outlive their encounter. A denied damage reservation cannot still deal unintended HP/posture damage.
* Keep ScriptableObject authoring and runtime inventory distinct. Never mutate original skill assets when adding a run-only boon. Preserve immutable boon IDs, serialization, runtime snapshot order, seed-driven deterministic offers and consistent eligibility across manual/procedural reward paths. Do not enable disabled catalog candidates simply because their declarations compile.
* Make runs diverse **across each weapon and slot**. Avoid making Singularidade Errante or `W pulls -> E explodes` the only successful build. Favor alternative tactical loops such as counters, aimed charged shots, trap setups, return trajectories and conditional aerial combos.
* When working on boon rewards, test offer eligibility, prerequisites, item weights, acquired targets and the probability of reaching a coherent build. The audit's reported 2:1 weighting is a hypothesis, **not** proof that Duos are obtainable or that the resulting runs feel different. Measure via representative seeds and actual gameplay.
* Before changing or expanding the boon runtime, follow the validated phase order in the latest approved plan. In the 2026-10-17 audit, later Bow/Spear/Augment/Duo code lacked full Unity validation; **re-check current evidence** instead of assuming this remains true forever. Keep Mythics gated until the required acceptance evidence is produced.

## HUD, Input and Presentation

* The functional **combat HUD follows Diablo-style readability**: visible life and mana orbs, Q/W/E/R skills, dash indicator, costs, cooldowns and state feedback; show Asura when Gauntlet is equipped. Hades may inform color and atmosphere, not replace the life/mana HUD. Use an original visual identity consistent with Tech-Guy's digital worlds.
* Preserve context-specific, **rebindable** input. The README describes click-to-target/move, Shift + click for stationary attacks, Q/W/E/R skills, Space dash, Esc pause and E for Nexus interaction; later executions may add an optional finisher/return action, reported as `F` by default. Inspect the current input bindings, conflicts and HUD prompts—never hardcode `F` or assume the README's bindings are immutable.
* Reward cards must explain replaced skill, changed input, gameplay effect, supported interactions, relevant restrictions and tradeoffs. Indicate why a Duo or Augment is unavailable; do not offer dead choices.
* A **run-end statistics screen** showing damage contributions, most damaging skill, acquired boons and run outcomes is a future product direction inspired by Diablo/Hades presentation and data-rich roguelites; it is **not** authorized implementation scope unless the current task explicitly includes it.

## Project-Local Skills for Codex

* When present, consult only the relevant project-local skill instructions under `.agents/skills/` before significant related changes; do not claim a skill is installed without checking its `SKILL.md`.
* `tech-guy-game-vision`: narrative, identity, gameplay priorities and visual direction.
* `tech-guy-combat-unity`: combat feel, enemy control, attacks, posture and reactions.
* `tech-guy-boon-design`: abilities, boon families, dependencies, capabilities and emergent builds.
* `tech-guy-unity-acceptance`: Unity Editor / Unity MCP inspections, tests and acceptance evidence.
* Skills supplement these repository instructions and current approved specs. They do not themselves connect Unity MCP, implement code or verify a test.

## Structure

* Game code belongs in `Assets/_Project/Scripts`, organized by domain: `Characters`, `Abilities`, `Weapons`, `Items`, `UI`, `Effects`, `Camera`, and `Core`.
* Game-owned assets belong in `Assets/_Project`, using folders such as `Scenes`, `Prefabs`, `Materials`, `Settings`, `Art`, `Input`, `Resources`, and `ScriptableObjects` as needed.
* Third-party assets belong in `Assets/_ThirdParty`. Preserve their original internal structure.
* Keep samples, editor tools, and Asset Store plugins separate from gameplay code.
* Apply this structure to new work. Do not reorganize unrelated existing folders solely to enforce these rules.
* Treat every `Resources` folder as a runtime loading boundary. Before moving or renaming its contents, inspect `Resources.Load` usage, including dynamically constructed keys and configuration-driven paths. An empty literal-string search does not establish that an asset is unused.

## C# / Unity

* Use PascalCase for types, methods, and public properties; camelCase for locals and parameters; `_camelCase` for private serialized fields.
* Prefer `[SerializeField] private` over public Inspector fields.
* Preserve serialized data when renaming fields. Use an appropriate migration, such as `FormerlySerializedAs`, where needed.
* Avoid `FindObjectOfType`, `GameObject.Find`, and magic strings in gameplay code. Prefer serialized references, prefab injection, or explicitly resolved components.
* Keep responsibilities separated: `MonoBehaviour` coordinates Unity/scene behavior; move growing game logic into smaller plain C# classes.
* Avoid heavy `Update` logic. Prefer events, coroutines, timers, or state machines when appropriate. Use `Update` when the system requires frame-based input or behavior.
* Validate required dependencies in `Awake` or `OnValidate` when missing references would break behavior. Keep `OnValidate` free of gameplay execution and unrelated asset mutations.
* Use serialization-compatible authoring data for Inspector configuration. Verify that values survive saving and reloading; do not assume a runtime data model is also a valid serialized authoring model.
* For numeric contracts, explicitly handle invalid values such as NaN and infinity where relevant. Keep validation of raw input separate from constructor normalization or clamping.
* Choose clocks explicitly for gameplay timing. Verify how pause, hitstop, and time scaling affect the changed system.
* Never change GUIDs, referenced asset names, or special Unity paths without checking scenes, prefabs, ScriptableObjects, and runtime loaders that depend on them.

## Safety and Asset Changes

* Before editing, check `git status` and preserve existing user changes. Never reset or discard unrelated changes.
* Prefer moving or renaming assets through the Unity Editor, using Unity MCP when supported.
* When moving or renaming Unity assets outside the Editor, always move the asset and its `.meta` together, including folder metadata where applicable.
* Keep existing GUIDs during moves and renames. Never manually edit GUID references unless explicitly required by the task.
* Move existing assets instead of recreating them during reorganization.
* When intentionally deleting an asset, first check its dependencies and then delete its associated `.meta` as part of the same change. Do not leave orphaned metadata.
* Do not modify third-party code unless explicitly requested.
* Avoid introducing new global state or singletons when local references are sufficient.
* Keep changes scoped to the current task. Avoid unrelated large refactors.
* After structural changes, inspect affected references in `.unity`, `.prefab`, `.asset`, and `.meta` files and validate the result in Unity.

## Mandatory Unity MCP Validation

* Always use Unity MCP for every necessary validation that it supports and that depends on Unity: asset import, script compilation, Console inspection, scene and prefab references, serialized Inspector data, EditMode/PlayMode tests, and runtime behavior.
* This requirement applies to relevant C# and asset changes, not only structural changes. Documentation-only changes do not require an Editor session unless they make claims that need Unity verification.
* At the beginning of Unity-dependent work, verify Unity MCP availability and confirm that it is connected to the intended project and Editor instance. Discover the available capabilities; do not assume tool names or support that has not been verified.
* After relevant changes, use Unity MCP to refresh or import affected assets when needed, wait for compilation to finish, and inspect the Console. Capture relevant existing errors before the change so new regressions can be distinguished.
* Never clear the Console just to make validation appear clean. Inspect errors and warnings relevant to the change and record unresolved findings.
* Run relevant Unity Test Framework tests through Unity MCP when its tools support them. Use other supported execution methods for tests that MCP cannot run, and still inspect the resulting Unity state through MCP.
* Use Unity MCP to inspect affected scenes, prefabs, components, and serialized references. For authoring changes, verify persistence after saving and reloading when relevant.
* For runtime changes, use Unity MCP to enter Play Mode, exercise representative scenarios, and inspect results when supported. Ensure Play Mode is exited afterward and preserve the user's scene state.
* CLI checks, source inspection, reference searches, and `git diff` supplement Unity MCP validation. They do not replace Unity checks that MCP can perform.
* If Unity MCP is unavailable, disconnected, or fails, report the attempted validation and the concrete limitation. Continue work that can be checked independently, using available alternatives, but mark remaining Unity checks as pending.
* Do not report a Unity-dependent task or stage as fully validated while required Unity checks remain pending. Do not claim that MCP, compilation, tests, or playtests ran without actual execution evidence.

## Tests and Acceptance

* If Unity tests exist, run the relevant EditMode and/or PlayMode tests. Choose checks based on the changed behavior and its dependencies.
* Add or update tests for meaningful behavior changes, contracts, and reproduced defects. Do not add tests that merely mirror implementation details or relax assertions to conceal regressions.
* Verify boundary cases relevant to the system. For combat changes, include applicable timing, input buffering, cancellation, interruptions, cleanup, and pause/hitstop behavior.
* Distinguish compilation success, automated test success, runtime verification, and gameplay acceptance. Passing technical checks alone does not establish that combat feels correct.
* Evaluate the task's acceptance criteria against observed results. A completed checklist is not evidence that those criteria were met.
* If a test cannot run, report why and what remains unverified. Static inspection must be described as static inspection.

## Completion and Handoff

* Review `git diff` before completing the task. Check for unintended changes, broken references, and unexpected serialized asset modifications.
* Report what changed, which checks actually ran, their results, and any remaining defects or pending validation.
* Separate current-stage fixes from proposed future-stage work. Do not silently expand the implementation roadmap.
* Generate a Markdown handoff document for impactful reviews, architectural decisions, changes to stage scope, or substantial validation findings. Include the decision, rationale, affected scope, and next actions.
* Do not generate an additional Markdown report for every minor question or routine fix unless the user requests it.
