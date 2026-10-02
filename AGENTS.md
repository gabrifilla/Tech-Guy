# Project Rules

## Context and Scope

* Unity 3D project using C#.
* Read the applicable project instructions and current requirements, design, and tasks before changing the affected system.
* Follow the agreed implementation stages. Do not pull future-stage features into the current task without a task requirement or user instruction.
* Preserve existing behavior outside the requested change. Report conflicts between code and specifications instead of silently redefining the contract.
* Fix defects demonstrated by tests within the current scope. Do not weaken a correct test merely to match faulty implementation.

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

