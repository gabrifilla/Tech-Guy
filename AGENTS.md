# Project Rules

## Context

* Unity 3D project using C#.
* Always preserve `.meta` files when moving, renaming, or deleting assets.
* Treat `Assets/Resources` as Unity-managed. Move assets from it only after confirming they are not used by `Resources.Load`.

## Structure

* Game code belongs in `Assets/_Project/Scripts`, organized by domain: `Characters`, `Abilities`, `Weapons`, `Items`, `UI`, `Effects`, `Camera`, and `Core`.
* Game-owned assets belong in `Assets/_Project`, using folders such as `Scenes`, `Prefabs`, `Materials`, `Settings`, `Art`, `Input`, `Resources`, and `ScriptableObjects` as needed.
* Third-party assets belong in `Assets/_ThirdParty`. Preserve their original internal structure.
* Keep samples, editor tools, and Asset Store plugins separate from gameplay code.

## C# / Unity

* Use PascalCase for types, methods, and public properties; camelCase for locals and parameters; `_camelCase` for private serialized fields.
* Prefer `[SerializeField] private` over public Inspector fields.
* Avoid `FindObjectOfType`, `GameObject.Find`, and magic strings in gameplay code. Prefer serialized references, prefab injection, or explicitly resolved components.
* Keep responsibilities separated: `MonoBehaviour` coordinates Unity/scene behavior; move growing game logic into smaller plain C# classes.
* Avoid heavy `Update` logic. Prefer events, coroutines, timers, or state machines when appropriate.
* Validate required dependencies in `Awake` or `OnValidate` when missing references would break behavior.
* Never change GUIDs, referenced asset names, or special Unity paths without checking scenes, prefabs, and ScriptableObjects that depend on them.

## Safety

* Never manually edit Unity GUID references unless explicitly required.
* Move existing assets instead of recreating them during reorganization.
* Do not modify third-party code unless explicitly requested.
* Avoid introducing new global state or singletons when local references are sufficient.

## Changes

* Before editing, check `git status` and preserve existing user changes.
* When moving Unity assets outside the Editor, always move the asset and its `.meta` together.
* Keep changes scoped to the current task. Avoid unrelated large refactors.
* After structural changes, run available CLI checks and inspect references in `.unity`, `.prefab`, `.asset`, and `.meta` files when relevant.

## Testing

* If Unity tests exist, run the relevant EditMode and/or PlayMode tests.
* If Unity cannot be run from the CLI, validate through project structure inspection, reference searches, and `git diff`.
* Clearly report any validation or tests that were not run.
