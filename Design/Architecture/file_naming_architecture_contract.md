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

## Operation-Map EntityScene And Dense-City Naming

The operation-map EntityScene and dense-city implementation uses these domain prefixes:

- `OperationMap*` for accepted-map identity, configuration, authoring, baked ECS data, runtime loading/readiness, building state, packaging, and validation;
- `DenseCity*` for generated-city authoring, immutable generation records, semantic realization, candidate transactions, proxies, and dense-only validation;
- `Map*`, `Building*`, or another narrower existing domain prefix when ownership is not specific to the operation-map migration.

Suffixes declare the execution boundary:

- `*Authoring` is a Unity conversion-edge component and may exist only in the authoring assembly/source scenes;
- `*Component` and buffer element names are unmanaged ECS data in `Game.Components`;
- `*Blob` is reserved for immutable, non-component `BlobAssetReference` record/root structs such as the existing `OperationMapBlob`; an `IComponentData` or `IBufferElementData` may not use it to avoid the required `*Component` suffix;
- `*Config` is serialized configuration or ScriptableObject bake input and does not own a runtime update loop;
- a bare `*System` is allowed only for an actual ECS `ISystem` or ECS system base, including baking-world systems;
- runtime managed boundaries retain an approved reason suffix or a narrow non-system noun such as `Policy` or `Utility`;
- editor-only producers use explicit nouns such as `Builder`, `Planner`, `Transaction`, `Validator`, `Probe`, `Writer`, `Backfill`, `Factory`, `Realizer`, `Extractor`, or `Library`.

`OperationMapBuildingDestructionSystem` and
`OperationMapRenderMaterialBaseColorBakingSystem` are not naming exceptions: both are
actual `ISystem` implementations, and the latter is restricted to the baking world.
`OperationMapEntityScenePresentationPolicy` and
`OperationMapEntityPresentationReadinessUtility` are narrow runtime composition helpers
that do not claim ECS scheduling. Dense-city editor producers are editor-only and must
not be added to a runtime non-ECS `*System` allowlist.

The candidate-only virtualized render path follows the same closed rules:

- `OperationMapRenderDatabaseBakeConfig` is generated ScriptableObject bake input in `Game.Configs`;
- `OperationMapVirtualizedPresentationAuthoring` is a conversion-edge authoring component in `Game.Authoring`;
- `OperationMapRenderDatabaseComponent`, `OperationMapRenderProxySlotComponent`, `OperationMapRenderVirtualizationStateComponent`, `OperationMapVirtualizedBuildingPresentationComponent`, `OperationMapRenderStateChangeComponent`, and `OperationMapRenderVirtualizationMetricsComponent` are unmanaged ECS data in `Game.Components`;
- `OperationMapRenderVirtualizationBakingSystem` and the `OperationMapRender*System` runtime owners named by the implementation tracker use bare `*System` only because each is an actual ECS system;
- database/capacity/report producers remain editor-only `Builder` or `Validator` types.

Generated assets, reports, and captures use the operation-map or dense-city domain
prefix plus their stable owner/purpose. Candidate-only paths additionally use
`Candidate` or `Candidates`; this is an ownership marker, not a runtime presentation
kind. Every added or renamed Unity source/asset keeps a same-name tracked `.meta` file.

No new product-prefix, broad-shell, bare non-ECS `*System`, or top-level naming-escape
exception is approved for the EntityScene/dense-city lane.

New exceptions require an explicit note in the owning architecture document. Player-facing product names may appear in in-game text, store copy, bundle identifiers, namespaces, and final exported deliverables, but not as the starting token of tracked source asset filenames.
