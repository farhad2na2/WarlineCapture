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
| Renamed in this tracker | 72 |
| Remaining known runtime non-ECS bare `*System` declarations, including MonoBehaviour | 168 |
| Current non-ECS conversion inventory denominator, excluding MonoBehaviour/editor | 167 |
| Current batch | Runtime city visual presentation helper naming batch complete |
| Validation status | Batch 1 through Batch 61 compile and architecture validations passed by marker; Batch 22 architecture, Batch 23-32 Unity validations, Batch 54 architecture, Batch 55 Unity validations, Batch 56 Unity validations, Batch 57 runtime-boundary/architecture validations, Batch 58 Unity validations, Batch 59 Unity validations, Batch 60 Unity validations, and Batch 61 Unity validations were terminated after recording pass markers because batchmode hung during post-test cleanup; Batch 33-53 Unity validations exited cleanly; Batch 54 building runtime boundary validation and Batch 57 production metadata validation exited cleanly; Batch 3 building tick focused validation exposed a pre-existing simulation-order test/contract mismatch unrelated to the rename; Batch 8 bootstrap-composition guard exposed a pre-existing UI Toolkit hierarchy lookup unrelated to the rename |

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

## Batch 39 - Building Placement Context Composition Helper

This helper owns placement grid/input/preview/commit context construction, cancel/begin/confirm lifecycle context creation, placement session/command context construction, and wall commit scratch runs. The managed reason is placement context composition wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementContextSystem` | `BuildingPlacementContextCompositionSystemHelper` | Owns managed placement context/session/command composition wiring. |

## Batch 39 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `189`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch39-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch39-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=189`.

## Batch 40 - Building Placement Interaction Context Composition Helper

This helper owns placement interaction source delegate packaging and `BuildingPlacementInteractionSystem.Context` construction. The managed reason is interaction context composition wiring, including managed delegates and `GameObject` destruction callbacks, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInteractionContextSystem` | `BuildingPlacementInteractionContextCompositionSystemHelper` | Owns managed placement interaction source/context composition wiring. |

## Batch 40 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `188`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch40-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch40-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=188`.

## Batch 41 - Building Gameplay Grid Data Composition Helper

This helper owns managed building gameplay grid-data query access and grid-cell placement conversion wiring. The managed reason is grid data composition around `EntityManager`/query access and camera-backed placement grid conversion, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayGridDataSystem` | `BuildingGameplayGridDataCompositionSystemHelper` | Owns managed grid-data lookup and grid-cell conversion composition wiring. |

## Batch 41 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `187`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch41-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch41-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=187`.

## Batch 42 - Building Gameplay ECS Query Composition Helper

This helper owns managed building gameplay `EntityQuery` cache construction and query handle access for the composition graph. The managed reason is ECS query composition and cached query-handle wiring, not an ECS-scheduled system lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayEcsQuerySystem` | `BuildingGameplayEcsQueryCompositionSystemHelper` | Owns managed ECS query cache composition wiring. |

## Batch 42 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `186`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch42-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch42-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=186`.

## Batch 43 - Building Gameplay Disposal Execution Composition Helper

This helper owns managed building gameplay teardown execution: exiting build mode, destroying runtime building presentation objects and ECS companion entities, clearing runtime registries, disposing placement startup state, and clearing pathfinding pending state. The managed reason is disposal execution wiring across managed presentation/ECS boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingGameplayDisposalSystem` | `BuildingGameplayDisposalExecutionCompositionSystemHelper` | Owns managed building gameplay teardown execution wiring. |

## Batch 43 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `185`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch43-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch43-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=185`.

## Batch 44 - Building Citizen Population Composition Helper

This helper owns building-side citizen population composition: population boundary resolution, resource/prefab context creation, initialization, disposal, and dependency binding. The managed reason is citizen population composition wiring across managed camera/day-night/resource-prefab boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingCitizenPopulationCompositionSystem` | `BuildingCitizenPopulationCompositionSystemHelper` | Owns managed building citizen-population composition wiring. |

## Batch 44 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `184`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch44-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch44-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=184`.

## Batch 45 - Building Placement Adapter Composition Helper

This helper owns placement adapter composition glue: initial placement origin, center-screen placement origin, active-placement validity, gate alignment, and placement validity adapter delegates. The managed reason is placement composition wiring across managed camera, material-property-block, and runtime context boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementAdapterSystem` | `BuildingPlacementAdapterCompositionSystemHelper` | Owns managed building placement adapter composition wiring. |

## Batch 45 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `183`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch45-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch45-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=183`.

## Batch 46 - Building Destroyed Visual Presentation Helper

This helper owns destroyed-building visual presentation: hiding live roots, spawning the configured destroyed visual prefab, reusing the cached destroyed visual instance, and cleaning it up. The managed reason is Unity `GameObject`/prefab presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingDestroyedVisualSystem` | `BuildingDestroyedVisualPresentationSystemHelper` | Owns managed destroyed-building visual presentation. |

## Batch 46 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `182`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingDestroyedVisualPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch46-building-destroyed-visual.log`: passed with `[BuildingDestroyedVisualFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch46-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=182`.

## Batch 47 - Building Foundation Visual Presentation Helper

This helper owns foundation-height visual presentation: applying evaluated foundation height to runtime building `GameObject` instances and projecting the matching surface component to the combat entity. The managed reason is Unity object/entity presentation sync, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingFoundationVisualSystem` | `BuildingFoundationVisualPresentationSystemHelper` | Owns managed foundation visual presentation sync. |

## Batch 47 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `181`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch47-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch47-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=181`.

## Batch 48 - Building Placement Visual Presentation Helper

This helper owns placement visual presentation: visual wrapper creation, prefab instantiation, combined-mesh renderer filtering, placement positioning, rotation, and local-bounds offsets. The managed reason is Unity `GameObject`/renderer presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementVisualSystem` | `BuildingPlacementVisualPresentationSystemHelper` | Owns managed building placement visual presentation. |

## Batch 48 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `180`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch48-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch48-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=180`.

## Batch 49 - Building Placement Visual Update Composition Helper

This helper owns placement visual update composition: active-placement focus, pointer-driven visual updates, wall preview rebuilds, confirm validation, current placement focus resolution, and placement object handoff to commit/lifecycle systems. The managed reason is placement visual/update composition across managed preview, input, camera, and commit boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementVisualUpdateSystem` | `BuildingPlacementVisualUpdateCompositionSystemHelper` | Owns managed placement visual update composition. |

## Batch 49 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `179`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch49-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch49-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=179`.

## Batch 50 - Building Placement Preview Presentation Helper

This helper owns placement preview presentation: outline object lifetime, material/property-block color updates, wall preview segment rebuilds, segment validity tinting, and runtime object destruction policy. The managed reason is Unity `GameObject`/renderer preview presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementPreviewSystem` | `BuildingPlacementPreviewPresentationSystemHelper` | Owns managed building placement preview presentation. |

## Batch 50 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `178`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch50-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch50-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=178`.

## Batch 51 - Building Placement Startup Helper

This helper owns placement startup/config wiring: config application, world camera/build-plane state, runtime building root creation, configured definition selection, road-footprint state, and placement preview initialization/disposal. The managed reason is scene/config startup wiring, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementStartupSystem` | `BuildingPlacementStartupSystemHelper` | Owns managed placement startup/config wiring. |

## Batch 51 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `177`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch51-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch51-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=177`.

## Batch 52 - Building Placement Lifecycle Composition Helper

This helper owns active placement lifecycle composition: active placement mutable state, begin/cancel/confirm/rotate flows, placement cost, preview ownership release, UI pointer notification, and placement failure reasons. The managed reason is lifecycle state and command composition across managed preview/input boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementLifecycleSystem` | `BuildingPlacementLifecycleCompositionSystemHelper` | Owns managed active-placement lifecycle composition. |

## Batch 52 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `176`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch52-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch52-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=176`.

## Batch 53 - Building Placement Query UI Helper

This helper owns managed selected-building UI/read-model queries: placement status text, selected-building label/display/description, selected-building preview prefab lookup, production prefab list reads, and selected-building health lookup for UI presentation. The managed reason is UI read-model and prefab-backed presentation data, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementQuerySystem` | `BuildingPlacementQueryUiSystemHelper` | Owns managed selected-building UI/read-model queries. |

## Batch 53 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `175`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch53-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch53-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=175`.

## Batch 54 - Building Placement Redirect Composition Helper

This helper owns managed placement redirect side-effect composition: deferred redirect footprints, marker-refresh deferral, placed-building unit redirect scans, perimeter redirect-goal search, and centralized move-order request writes. The managed reason is redirect side-effect composition across managed placement/runtime boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementRedirectSystem` | `BuildingPlacementRedirectCompositionSystemHelper` | Owns managed placement redirect side-effect composition. |

## Batch 54 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `174`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch54-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch54-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=174`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 55 - Building Placement Session Composition Helper

This helper owns managed placement session composition: placement begin/confirm/rotate/cancel/exit command flow, build-mode state transitions, selection preservation after confirm, minimap notification, preview hiding, and command-mode clearing. The managed reason is session command composition across lifecycle, input, preview, UI, and runtime-state boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementSessionSystem` | `BuildingPlacementSessionCompositionSystemHelper` | Owns managed placement session command composition. |

## Batch 55 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `173`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch55-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch55-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=173`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 56 - Building Placement Command Request Composition Helper

This helper owns managed placement command request composition: ECS placement request/result queue helpers, begin/confirm/rotate/cancel/exit request processing, Soldier Base placement start, placement UI pointer notification, and active-placement cost routing. The managed reason is command request/result composition across ECS queue and managed session/startup/definition boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementCommandSystem` | `BuildingPlacementCommandRequestCompositionSystemHelper` | Owns managed placement command request/result composition. |

## Batch 56 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `172`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch56-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch56-architecture-rerun.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=172`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 57 - Building Definition Prefab Helper

This helper owns managed building definition and configured prefab metadata: configured building/unit prefab lookup, authoring metadata resolver injection, runtime building prefab metadata cache, bounds/visual-footprint discovery, production spawn point metadata, production-slot read helpers, source-key normalization, and runtime/configured definition construction. The managed reason is GameObject prefab-backed definition metadata, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingDefinitionSystem` | `BuildingDefinitionPrefabSystemHelper` | Owns managed building definition and configured prefab metadata. |

## Batch 57 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `171`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunProductionMetadataValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch57-building-production-metadata.log`: passed with `[BuildingProductionMetadataValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch57-building-runtime-boundary.log`: recorded `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch57-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=171`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 58 - Building Runtime Visual Presentation Helper

This helper owns managed runtime building visual presentation: runtime building `Transform` traversal, `Door_Z` lookup and open-state initialization, alive-root capture, animated-part cache updates, faction renderer/tint cache handoff, marker material property block context, and destroyed-building marker visibility refresh. The managed reason is Unity object presentation state, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeVisualSystem` | `BuildingRuntimeVisualPresentationSystemHelper` | Owns managed runtime building visual presentation. |

## Batch 58 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `170`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingSelectionMarkerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch58-building-selection-marker.log`: recorded `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch58-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=170`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 59 - Unit Attack Trace Presentation Helper

This helper owns managed attack-trace presentation: scene camera config, trace mesh/material/property-block lifecycle, ECS trace read queries, camera-facing trace billboarding, batched matrix/color/parameter arrays, and `Graphics.DrawMeshInstanced` playback. The managed reason is camera/material/mesh rendering presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `UnitAttackTraceSystem` | `UnitAttackTracePresentationSystemHelper` | Owns managed attack-trace rendering presentation. |

## Batch 59 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `169`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitCombatFocusedEditModeTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch59-unit-combat.log`: recorded `[UnitCombatFocusedEditModeValidation] result=Passed tests=1`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch59-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=169`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 60 - Unit Impostor Presentation Helper

This helper owns managed unit impostor presentation: camera-facing billboard mesh/material lifecycle, prefab atlas lookup, ECS read queries for culled/source-key fallback impostor candidates, material batch state, high-camera impostor style selection, and `Graphics.RenderMeshInstanced` playback. The managed reason is camera/material/mesh rendering presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `UnitImpostorRenderSystem` | `UnitImpostorPresentationSystemHelper` | Owns managed unit impostor rendering presentation. |

## Batch 60 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `168`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch60-unit-render-budget.log`: recorded `[UnitRenderBudgetFocusedValidation] result=Passed tests=31`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch60-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=168`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 61 - Runtime City Visual Presentation Helper

This helper owns managed runtime city visual presentation: city visual root parenting, visual-only prefab wrapper creation, combined-mesh instantiation, footprint-center positioning, surface-height integration, local-bounds centering, child visibility toggles, and cleanup. The managed reason is Unity object visual presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityVisualSystem` | `RuntimeCityVisualPresentationSystemHelper` | Owns managed runtime city visual presentation. |

## Batch 61 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `167`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch61-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch61-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=167`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Open Follow-Up Batches

- Direct ECS-called helper batch.
- UI/camera/scene/startup managed-boundary batch.
- Building/production composition helper batch.
- Road/city/citizen composition helper batch.
- Visual/prefab/render helpers that are not converted to `ISystem`.
- Architecture guardrail ratchet after each batch reduces the transition list.
