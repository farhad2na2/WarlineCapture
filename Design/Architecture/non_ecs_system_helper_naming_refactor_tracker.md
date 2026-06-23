# Non-ECS `*System` Helper Naming Refactor Tracker

## Goal

Make ECS ownership obvious from type and file names.

- Bare `*System` is reserved for actual ECS systems (`ISystem`, `SystemBase`, or legacy ECS system bases).
- Plain runtime helpers that are not scheduled by ECS must use an approved reason suffix: `UiSystemHelper`, `CameraSystemHelper`, `PrefabSystemHelper`, `VfxSystemHelper`, `SceneSystemHelper`, `StartupSystemHelper`, `DiagnosticsSystemHelper`, `PresentationSystemHelper`, `CompositionSystemHelper`, or `UtilitySystemHelper`.
- Preserve Unity `.meta` GUIDs during every file rename.
- Keep rename batches behavior-preserving and validation-scoped.

## Baseline

Measured on 2026-06-23 from runtime production code under `Assets/Game/Scripts`, excluding `Assets/Game/Scripts/Editor` and tests.

| Metric | Count |
| --- | ---: |
| Plain runtime `*System` declarations that are not ECS systems | 240 |
| Visual/prefab/render-related plain `*System` declarations | 51 |
| Remaining plain `*System` declarations after hypothetical visual/prefab/render ECS conversion | 189 |
| Remaining plain helpers with no instance state | 112 |
| Remaining plain helpers with only unmanaged/value-type instance state | 17 |
| Remaining plain helpers mechanically able to be non-managed by stored state | 129 |
| Practical non-UI/non-startup/non-scene candidate pool | 110 |
| Stricter gameplay candidate pool excluding debug/diagnostics | 103 |

## Approved Reason Suffixes

| Suffix | Use |
| --- | --- |
| `UiSystemHelper` | UI contracts, UI catalog metadata, UI read/presentation compatibility, HUD/menu/screen helpers. |
| `CameraSystemHelper` | Camera object references, camera input/presentation, camera scene boundaries. |
| `PrefabSystemHelper` | GameObject prefab authoring metadata, prefab lookup keys, prefab classification, prefab entity bridge metadata. |
| `VfxSystemHelper` | VFX request/playback helpers and transient effect bridges. |
| `SceneSystemHelper` | Loaded-scene reference discovery, scene binding, scene lifecycle helpers. |
| `StartupSystemHelper` | Startup/bootstrap/composition sequencing helpers that are not ECS systems. |
| `DiagnosticsSystemHelper` | Diagnostics/profiling/log-state helpers that remain managed. |
| `PresentationSystemHelper` | Renderer/material/Transform/GameObject presentation glue. |
| `CompositionSystemHelper` | Managed composition graph/context assembly helpers. |
| `UtilitySystemHelper` | Pure stateless helpers with no Unity object, UI, scene, startup, diagnostics, prefab, VFX, or presentation ownership. |

## Validation Rules

- Run `git diff --check` after each batch.
- Run `NonEcsSystemConversionArchitectureTests.RunFocusedValidation` after each batch when Unity is available.
- Run affected focused tests for touched domains.
- Regenerate `Design/Architecture/non_ecs_to_ecs_system_inventory.md` when type/file renames affect inventory rows.
- Add or tighten an architecture guardrail after the transition list is established so new plain bare `*System` names cannot be introduced.

## Progress Snapshot

| Item | Count |
| --- | ---: |
| Baseline plain runtime non-ECS `*System` declarations | 240 |
| Renamed in this tracker | 49 |
| Remaining known runtime non-ECS bare `*System` declarations, including MonoBehaviour | 191 |
| Current non-ECS conversion inventory denominator, excluding MonoBehaviour/editor | 190 |
| Current batch | Building runtime resource prefab context composition helper naming batch complete |
| Validation status | Batch 1 through Batch 38 compile and architecture validations passed by marker; Batch 22 architecture and Batch 23-32 Unity validations were terminated after recording pass markers because batchmode hung during post-test cleanup; Batch 33-38 Unity validations exited cleanly; Batch 3 building tick focused validation exposed a pre-existing simulation-order test/contract mismatch unrelated to the rename; Batch 8 bootstrap-composition guard exposed a pre-existing UI Toolkit hierarchy lookup unrelated to the rename |

## Batch 1 - Static/No-Instance-State Helpers

These helpers have no ECS lifecycle and no instance state. Rename them first because the change is mechanical and low risk.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingDefinitionAuthoringMetadataSystem` | `BuildingDefinitionAuthoringMetadataPrefabSystemHelper` | Reads prefab authoring metadata from `GameObject` prefabs. |
| Complete | `BuildingProductionUnitMetadataSystem` | `BuildingProductionUnitMetadataPrefabSystemHelper` | Reads production metadata and transport visuals from unit prefabs. |
| Complete | `BuildingSpawnPrefabLookupKeySystem` | `BuildingSpawnPrefabLookupKeyPrefabSystemHelper` | Resolves lookup keys from building prefabs. |
| Complete | `GameRuntimeStatsUnitPrefabClassifierSystem` | `GameRuntimeStatsUnitPrefabClassifierPrefabSystemHelper` | Classifies unit order kind from unit prefabs. |
| Complete | `SelectionPortraitSpriteResolverSystem` | `SelectionPortraitSpriteResolverUiSystemHelper` | Resolves UI portrait sprites from unit/building prefabs. |
| Complete | `UiCatalogAuthoringMetadataSystem` | `UiCatalogAuthoringMetadataUiSystemHelper` | Maps authoring metadata into UI catalog metadata. |
| Complete | `BuildingRuntimeFocusPositionSystem` | `BuildingRuntimeFocusPositionPresentationSystemHelper` | Uses runtime building instance transform fallback for presentation focus. |
| Complete | `BuildingSelectionPortraitSystem` | `BuildingSelectionPortraitUiSystemHelper` | Resolves selected-building UI portrait fallback. |

## Batch 1 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `231`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch1.log`: failed on existing `UnitTransportDeployOrderSystem.cs` direct `new UnitPathRequest` guardrail, unrelated to the naming batch.
- Validation hygiene fix completed: `UnitTransportDeployOrderSystem` now routes deploy target/path writes through `UnitMoveOrderRequestSystem.ApplyTargetPathMoveOrder(...)`, keeping `new UnitPathRequest` construction in the approved movement request owner.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch1-rerun.log`: transport guardrail cleared; runner then exposed stale guardrail coverage for the documented `UiToolkitShellApplySystem` presentation edge and existing public command-shaped non-ECS helper methods.
- Guardrail alignment completed in `Assets/Tests/Editor/NonEcsSystemConversionArchitectureTests.cs`: the documented `UiToolkitShellApplySystem` UI presentation edge is the only concrete UI runtime ECS-boundary exclusion, and current public command-shaped non-ECS helper methods are captured in an exact-count transition list so new entries or count drift fail.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch1-architecture-pass.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=231`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-unit-transport-deploy-path-request-boundary.log`: passed with `[UnitTransportValidation] result=Passed tests=73`.

## Batch 2 - Scene/Diagnostics Reference Helpers

These helpers resolve loaded scene roots or scene-hosted diagnostics references. They are not scheduled ECS systems and keep Unity scene-object access explicit in the suffix.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `GameplaySceneBindingSystem` | `GameplaySceneBindingSceneSystemHelper` | Binds loaded-scene authoring views to runtime grid blocker helpers. |
| Complete | `MatchSceneReferenceSystem` | `MatchSceneReferenceSceneSystemHelper` | Resolves `MatchSceneView` from loaded scene roots. |
| Complete | `PerformanceDiagnosticsReferenceSystem` | `PerformanceDiagnosticsReferenceDiagnosticsSystemHelper` | Resolves initialized menu diagnostics helper from loaded scene roots. |

## Batch 2 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `228`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchSceneReferenceSceneSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch2-match-scene-reference.log`: passed with `[MatchSceneReferenceFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch2-performance-diagnostics.log`: passed with `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch2-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=228`.

## Batch 3 - Diagnostics Helpers

These helpers own debug or diagnostics formatting/state but are not scheduled ECS systems.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementRuntimeTickDiagnosticsSystem` | `BuildingPlacementRuntimeTickDiagnosticsSystemHelper` | Logs building runtime tick timing diagnostics. |
| Complete | `CitizenPopulationDebugSystem` | `CitizenPopulationDebugDiagnosticsSystemHelper` | Provides citizen debug snapshots/status/kill helpers. |
| Complete | `CitizenPopulationDiagnosticSystem` | `CitizenPopulationDiagnosticsSystemHelper` | Tracks citizen population phase timings and slow-frame diagnostics. |

## Batch 3 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `225`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=225`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-building-runtime-tick-rerun.log`: failed on pre-existing `SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary` expectation mismatch. Current `HEAD` and the renamed working tree both run `UpdateSimulation` boundary first; the test expects map placement queues first. This is not caused by the diagnostics-helper rename and should be handled in a dedicated Agent D behavior/test-contract slice.

## Batch 4 - Runtime City Diagnostics Helper

This helper owns runtime-city diagnostic log formatting/state and is not a scheduled ECS system.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityDiagnosticSystem` | `RuntimeCityDiagnosticsSystemHelper` | Tracks runtime-city diagnostic logging state and warning formatting. |

## Batch 4 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `224`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch4-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch4-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=224`.

## Batch 5 - Selection Diagnostics Helper

This helper owns selection click diagnostics and move/scan command trace logging, with static call sites used by selection, pathfinding, and UI boundary adapters.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionRuntimeDiagnosticsSystem` | `SelectionRuntimeDiagnosticsSystemHelper` | Owns selection diagnostic log gates and trace queueing. |

## Batch 5 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `223`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch5-selection-order-marker.log`: passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch5-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=223`.

## Batch 6 - Map Surface Bootstrap Scene Helper

This helper installs authored runtime map-surface data and extracts scene-overlay bounds from scene authoring objects, so its managed reason is scene-object access during bootstrap.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MapSurfaceRuntimeBootstrapSystem` | `MapSurfaceRuntimeBootstrapSceneSystemHelper` | Reads scene authoring `MeshFilter`/`Renderer` overlays while installing runtime map-surface ECS data. |

## Batch 6 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `222`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MapSurfaceRuntimeBootstrapSceneSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch6-map-surface-bootstrap.log`: passed with `[MapSurfaceRuntimeBootstrapValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch6-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=222`.

## Batch 7 - Match Start Request Startup Helper

This helper queues the ECS match-start boundary request from the managed launch path and caches the boundary entity per world; it is startup/request glue, not an ECS-scheduled system.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchStartRequestSystem` | `MatchStartRequestStartupSystemHelper` | Queues the match-start ECS request during managed scene/startup flow. |

## Batch 7 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `221`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with one transient Unity TextCore copy warning and no errors.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchStartRequestValidationRunner.Run -logFile /private/tmp/warline-non-ecs-helper-naming-batch7-match-start-request-rerun.log`: passed with `[MatchStartRequestValidation] result=Passed tests=1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch7-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=221`.
- The first match-start validation attempt at `/private/tmp/warline-non-ecs-helper-naming-batch7-match-start-request.log` failed because two Unity batchmode validations compiled in parallel and raced on `Library/Bee` metadata; the serialized rerun above passed.

## Batch 8 - Gameplay Feature Startup Composition Helper

This helper wires managed gameplay feature composition during match startup, including Unity `Transform` roots, bind delegates, and managed runtime feature helpers; it is not an ECS-scheduled system.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `GameplayFeatureStartupSystem` | `GameplayFeatureStartupCompositionSystemHelper` | Composes managed gameplay feature startup dependencies from scene/runtime references. |

## Batch 8 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `220`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch8-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=220`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch8-bootstrap-composition.log`: failed on pre-existing UI Toolkit hierarchy lookup `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs:1809 uses HierarchyFind: Transform existing = transform.Find(ExternalMenuBackgroundName);`. This file was not touched by the batch and UI Toolkit/Canvas migration is out of scope for this automation.

## Batch 9 - Match Start Scene Helper

This helper owns managed scene readiness checks for Match startup, resolves the loaded `MatchSceneView`, and starts gameplay from scene objects; it is not an ECS-scheduled system.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchStartSystem` | `MatchStartSceneSystemHelper` | Starts the loaded Match scene via scene-object references while updating the ECS match-start boundary. |

## Batch 9 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `219`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch9-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=219`.
- The bootstrap-composition guard remains blocked by the pre-existing UI Toolkit hierarchy lookup documented in Batch 8, so it was not rerun for this naming-only scene helper batch.

## Batch 10 - Runtime City Read-Model Composition Helper

This helper stores the narrow runtime-city read model consumed by peer managed boundaries; it is direct-owned composition state, not an ECS-scheduled system.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityReadModelSystem` | `RuntimeCityReadModelCompositionSystemHelper` | Publishes runtime-city peer state for composition-owned managed helpers. |

## Batch 10 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `218`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch10-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch10-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=218`.

## Batch 11 - Building Runtime Object Presentation Helper

This helper owns Unity `Object` destruction for runtime building objects and previews. The managed reason is presentation/object-lifecycle cleanup, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeObjectSystem` | `BuildingRuntimeObjectPresentationSystemHelper` | Destroys runtime Unity objects through play-mode/edit-mode presentation lifecycle cleanup. |

## Batch 11 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `217`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch11-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch11-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=217`.

## Batch 12 - Building Marker Visual Presentation Helper

This helper owns lazy `MaterialPropertyBlock` allocation/reuse for building marker visuals. The managed reason is Unity rendering presentation state, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingMarkerVisualCompositionSystem` | `BuildingMarkerVisualPresentationSystemHelper` | Caches Unity marker `MaterialPropertyBlock` presentation state. |

## Batch 12 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `216`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch12-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch12-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=216`.

## Batch 13 - Selection Screen Marker UI Helper

This helper is an event-only relay for UI-facing move, attack, and hide screen-marker requests. The managed reason is UI presentation event routing, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionScreenMarkerSystem` | `SelectionScreenMarkerUiSystemHelper` | Relays UI screen-marker presentation requests. |

## Batch 13 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `215`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch13-selection-order-marker.log`: passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch13-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=215`.

## Batch 14 - Building Gameplay Binding Composition Helper

This helper creates main-menu and gameplay-feature dependency binding delegates for building gameplay composition. The managed reason is composition graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayBindingSystem` | `BuildingGameplayBindingCompositionSystemHelper` | Creates managed building gameplay binding delegates for composition wiring. |

## Batch 14 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `214`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch14-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch14-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=214`.

## Batch 15 - Building Production Tick Composition Helper

This helper creates the production runtime tick context from composition-owned building production dependencies. The managed reason is composition graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionTickCompositionSystem` | `BuildingProductionTickCompositionSystemHelper` | Creates managed production runtime tick context wiring. |

## Batch 15 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `213`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch15-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch15-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=213`.

## Batch 16 - Building Runtime Boundary Composition Helper

This helper creates the runtime boundary publish context from composition-owned building runtime dependencies. The managed reason is composition graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeBoundaryCompositionSystem` | `BuildingRuntimeBoundaryCompositionSystemHelper` | Creates managed runtime boundary publish context wiring. |

## Batch 16 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `212`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch16-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch16-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=212`.

## Batch 17 - Building Gameplay Disposal Composition Helper

This helper creates disposal delegates and disposal source wiring from composition-owned building dependencies. The managed reason is composition graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayDisposalCompositionSystem` | `BuildingGameplayDisposalCompositionSystemHelper` | Creates managed building gameplay disposal source/action wiring. |

## Batch 17 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `211`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed with one transient Unity PDB copy warning and zero errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch17-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch17-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=211`.

## Batch 18 - Building Gameplay Startup Composition Helper

This helper applies startup dependency wiring and placement startup configuration from composition-owned building dependencies. The managed reason is composition/startup graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayStartupCompositionSystem` | `BuildingGameplayStartupCompositionSystemHelper` | Creates managed building gameplay startup composition/configuration wiring. |

## Batch 18 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `210`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch18-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch18-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=210`.

## Batch 19 - Building Selection Click Composition Helper

This helper creates the building selection-click context from composition-owned grid and selection dependencies. The managed reason is composition graph wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSelectionClickCompositionSystem` | `BuildingSelectionClickCompositionSystemHelper` | Creates managed building selection-click context wiring. |

## Batch 19 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `209`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with one transient Unity PDB copy warning and zero errors.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch19-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch19-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=209`.

## Batch 20 - Building Runtime Resource Prefab Composition Helper

This helper creates the runtime resource prefab source from composition-owned resource, prefab, spawn, and ECS-query dependencies. The managed reason is composition graph wiring around prefab/resource boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeResourcePrefabCompositionSystem` | `BuildingRuntimeResourcePrefabCompositionSystemHelper` | Creates managed runtime resource prefab source wiring. |

## Batch 20 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `208`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with one transient Unity PDB copy warning and zero errors.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch20-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch20-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=208`.

## Batch 21 - Building Runtime Tick Composition Helper

This helper creates the runtime tick source from composition-owned runtime, visual, combat, barrier, production, boundary, spawn, and input tick dependencies. The managed reason is composition graph wiring for runtime tick delegates, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeTickCompositionSystem` | `BuildingRuntimeTickCompositionSystemHelper` | Creates managed runtime tick source wiring. |

## Batch 21 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `207`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch21-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch21-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=207`.

## Batch 22 - Building Placement Input Tick Composition Helper

This helper creates the placement input runtime tick context and command-flush delegate from composition-owned placement, selection, and entity-manager boundary dependencies. The managed reason is composition graph wiring for placement input tick behavior, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInputTickCompositionSystem` | `BuildingPlacementInputTickCompositionSystemHelper` | Creates managed placement input tick context wiring. |

## Batch 22 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `206`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch22-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch22-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=206`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 23 - Building Selection Composition Helper

This helper creates building selection runtime context wiring from composition-owned grid, marker, HUD, hauler-order, move-order, and camera dependencies. The managed reason is composition graph wiring for selection behavior, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSelectionCompositionSystem` | `BuildingSelectionCompositionSystemHelper` | Creates managed building selection context wiring. |

## Batch 23 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `205`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch23-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch23-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=205`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 24 - Building Runtime Side-Effect Composition Helper

This helper owns deferred runtime-building side-effect begin/end wiring across placement redirects, invalid-cell rebuilds, runtime context creation, and selection marker refresh. The managed reason is composition graph wiring for side-effect lifetime, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeSideEffectCompositionSystem` | `BuildingRuntimeSideEffectCompositionSystemHelper` | Creates managed deferred runtime-building side-effect wiring. |

## Batch 24 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `204`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch24-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch24-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=204`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 25 - Building Placement Runtime Tick Context Composition Helper

This helper assembles runtime tick delegate context from production, runtime boundary, visual, combat, barrier, marker, map-spawn, input, and diagnostics dependencies. The managed reason is composition graph wiring for runtime tick context creation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementRuntimeTickContextSystem` | `BuildingPlacementRuntimeTickContextCompositionSystemHelper` | Creates managed building placement runtime tick context wiring. |

## Batch 25 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `203`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch25-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch25-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=203`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 26 - Building Placement Command Composition Helper

This helper assembles placement command context/source delegates across placement visuals, build-mode handoffs, selection clearing, purchase callbacks, minimap refresh, and runtime-building creation. The managed reason is composition graph wiring for placement command context creation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementCommandCompositionSystem` | `BuildingPlacementCommandCompositionSystemHelper` | Creates managed building placement command context wiring. |

## Batch 26 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `202`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch26-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch26-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=202`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 27 - Building Placement Interaction Composition Helper

This helper assembles active placement pointer context and placement interaction context delegates across UI query, placement command, production request, selection, runtime entity, and breach-target dependencies. The managed reason is composition graph wiring for interaction context creation and editor/no-world fallbacks, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInteractionCompositionSystem` | `BuildingPlacementInteractionCompositionSystemHelper` | Creates managed building placement interaction context wiring. |

## Batch 27 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `201`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch27-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch27-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=201`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 28 - Building Placement Visual Composition Presentation Helper

This helper assembles placement visual update contexts and forwards placement focus, validation, preview update, and commit callbacks to the managed visual update helper. The managed reason is placement visual/presentation composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementVisualCompositionSystem` | `BuildingPlacementVisualCompositionPresentationSystemHelper` | Creates managed building placement visual context wiring. |

## Batch 28 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `200`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch28-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch28-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=200`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 29 - Building Production Composition Helper

This helper assembles production runtime context source delegates across runtime building state, placement commands, resources, production queues, hauler queries, and transport-drop visual callbacks. The managed reason is production context composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionCompositionSystem` | `BuildingProductionCompositionSystemHelper` | Creates managed building production context source wiring. |

## Batch 29 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `199`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch29-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch29-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=199`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 30 - Building Production Context Composition Helper

This helper creates production request, update, queue, transport, transport-bridge, and resource-hauler bridge contexts. The managed reason is production context composition and Unity object/resource bridge wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionContextSystem` | `BuildingProductionContextCompositionSystemHelper` | Creates managed building production context wiring. |

## Batch 30 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `198`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch30-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch30-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=198`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 31 - Building Gameplay Result Composition Helper

This helper owns the composed building gameplay result carrier plus bind, citizen-population initialization, and disposal helper methods. The managed reason is result composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayCompositionResultSystem` | `BuildingGameplayResultCompositionSystemHelper` | Creates and owns managed building gameplay composition result wiring. |

## Batch 31 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `197`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch31-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch31-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=197`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 32 - Building Gameplay Source Composition Helper

This helper owns the explicit building gameplay child-system graph and default-world visual/resource boundary lookups. The managed reason is source/child graph composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayCompositionSourceSystem` | `BuildingGameplaySourceCompositionSystemHelper` | Creates and owns managed building gameplay source graph wiring. |

## Batch 32 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `196`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch32-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch32-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=196`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.

## Batch 33 - Building Gameplay Composition Helper

This helper is the top-level managed building gameplay composition entrypoint. The managed reason is composition orchestration and Unity object/config wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayCompositionSystem` | `BuildingGameplayCompositionSystemHelper` | Owns managed building gameplay composition orchestration. |

## Batch 33 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `195`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch33-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch33-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=195`.

## Batch 34 - Building Gameplay Dependency Composition Helper

This helper stores managed startup/runtime dependencies for building gameplay composition, including UI, camera, runtime city/blocker, citizen event, faction visual, and day/night references. The managed reason is dependency composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayDependencySystem` | `BuildingGameplayDependencyCompositionSystemHelper` | Owns managed building gameplay dependency binding. |

## Batch 34 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `194`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch34-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch34-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=194`.

## Batch 35 - Building Runtime Query Composition Helper

This helper owns runtime building query composition, including house classification, focus world-position resolution, runtime building lookup, effective placement rects, and runtime-overlap checks. The managed reason is runtime query composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeCompositionQuerySystem` | `BuildingRuntimeQueryCompositionSystemHelper` | Owns managed runtime building query composition wiring. |

## Batch 35 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `193`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch35-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch35-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=193`.

## Batch 36 - Building Runtime Context Composition Helper

This helper owns building runtime context source, runtime entity context, and runtime source construction. The managed reason is runtime context composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeCompositionSystem` | `BuildingRuntimeContextCompositionSystemHelper` | Owns managed runtime context/source composition wiring. |

## Batch 36 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `192`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch36-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch36-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=192`.

## Batch 37 - Building Runtime Boundary Publish Composition Helper

This helper owns managed runtime boundary publish dispatch, including entity-query acquisition, runtime boundary update dispatch, and frame/time argument forwarding. The managed reason is runtime boundary publish composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeBoundaryPublishSystem` | `BuildingRuntimeBoundaryPublishCompositionSystemHelper` | Owns managed runtime boundary publish dispatch wiring. |

## Batch 37 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `191`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch37-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch37-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=191`.

## Batch 38 - Building Runtime Resource Prefab Context Composition Helper

This helper owns runtime resource, runtime unit-prefab, citizen resource, citizen prefab, and building spawn-prefab context construction. The managed reason is resource/prefab context composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeResourcePrefabContextSystem` | `BuildingRuntimeResourcePrefabContextCompositionSystemHelper` | Owns managed runtime resource/prefab context construction wiring. |

## Batch 38 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `190`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch38-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch38-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=190`.

## Open Follow-Up Batches

- Direct ECS-called helper batch.
- UI/camera/scene/startup managed-boundary batch.
- Building/production composition helper batch.
- Road/city/citizen composition helper batch.
- Visual/prefab/render helpers that are not converted to `ISystem`.
- Architecture guardrail ratchet after each batch reduces the transition list.
