# File Naming Architecture Contract

Project-source filenames must not start with the product or project name. This applies to scripts, prefabs, animation clips, sprite atlases, textures, configs, test files, design documents, generated design media, and source-control tracked support files.

Use the feature/domain as the filename prefix instead:

- `UI*` for shell, menu, HUD, popup, widget, and UI test files.
- `Gameplay*`, `Unit*`, `Building*`, `Vehicle*`, `Map*`, `Terrain*`, `Selection*`, or another gameplay domain word for runtime systems and data.
- `Config*`, `Save*`, `Audio*`, `Brand*`, `Balance*`, `Visual*`, `Monetization*`, `Saga*`, or the document topic for assets and design files.

Rationale: project renames should not require sweeping source-file renames, and project-prefixed files tend to pile up as unrelated catch-all buckets.

When renaming Unity assets, move the `.meta` file with the asset so serialized references keep the same GUID. Do not recreate the asset just to change its filename.

## Runtime System Naming

Runtime C# filenames and top-level type names must make ECS ownership obvious:

- Bare `*System` is reserved for Unity ECS systems: `ISystem`, `SystemBase`, `ComponentSystemBase`, `ComponentSystem`, or `JobComponentSystem`.
- Plain runtime C# classes or structs that are not scheduled by ECS must not use a bare `*System` name.
- Plain non-ECS helpers that remain by design must use one approved reason suffix: `UiSystemHelper`, `CameraSystemHelper`, `PrefabSystemHelper`, `VfxSystemHelper`, `SceneSystemHelper`, `StartupSystemHelper`, `DiagnosticsSystemHelper`, `PresentationSystemHelper`, `CompositionSystemHelper`, or `UtilitySystemHelper`.
- The suffix should name the reason the type is outside ECS, not merely the domain. Example: a helper reading prefab authoring data should use `PrefabSystemHelper`; a helper binding loaded scene references should use `SceneSystemHelper`; a pure static conversion helper should use `UtilitySystemHelper`.
- Rename the `.cs.meta` file with the `.cs` file during Unity script renames.

The active migration is tracked in `Design/Architecture/non_ecs_system_helper_naming_refactor_tracker.md`.

New exceptions require an explicit note in the owning architecture document. Player-facing product names may appear in in-game text, store copy, bundle identifiers, namespaces, and final exported deliverables, but not as the starting token of tracked source asset filenames.
