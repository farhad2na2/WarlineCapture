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
| Renamed in this tracker | 237 |
| Remaining known runtime non-ECS bare `*System` declarations, including MonoBehaviour | 3 |
| Current non-ECS conversion inventory denominator, excluding MonoBehaviour/editor | 3 |
| Current batch | Selection UI command helper naming batch complete |
| Validation status | Batch 1 through Batch 226 compile and architecture validations passed by marker; Batch 226 selection/input and HUD command-control validations exited cleanly and architecture reported denominator 3; Batch 226 UI shell content validation reached pre-existing missing `statusChipSprite` prefab reference before helper binding, and command request/result validation reached the documented pre-existing `UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea` assertion; Batch 225 selection-state and selection/input validations exited cleanly and architecture reported denominator 4; Batch 224 camera and selection/input validations exited cleanly and architecture reported denominator 5; Batch 223 selection/input validation exited cleanly and architecture reported denominator 6; Batch 222 selection/input validation exited cleanly and architecture reported denominator 7; Batch 221 selection/input validation exited cleanly and architecture reported denominator 8; Batch 220 selected-order snapshot validation exited cleanly and architecture reported denominator 9; Batch 219 scene lifecycle validation exited cleanly and architecture reported denominator 10; Batch 218 architecture reported denominator 11; Batch 217 building gameplay composition validation exited cleanly and architecture reported denominator 12; Batch 216 runtime-grid validation exited cleanly and architecture reported denominator 13; Batch 215 road-build command validation exited cleanly and architecture reported denominator 14; Batch 214 road-build command validation exited cleanly and architecture reported denominator 15; Batch 213 road-build command validation exited cleanly after a serialized rerun and architecture reported denominator 16; Batch 212 resource-hauler validation exited cleanly and architecture reported denominator 17; Batch 211 performance-diagnostics validation exited cleanly and architecture reported denominator 18; Batch 210 match-HUD squad-tray validation exited cleanly and architecture reported denominator 19; Batch 209 map-vehicle placement validation exited cleanly and architecture reported denominator 20; Batch 208 building runtime boundary validation exited cleanly and architecture reported denominator 21; Batch 207 managed gameplay startup validation exited cleanly and architecture reported denominator 22; Batch 206 gameplay runtime update validation exited cleanly and architecture reported denominator 23; Batch 205 unit-transport validation exited cleanly and architecture reported denominator 24; Batch 204 selection-state validation exited cleanly and architecture reported denominator 25; Batch 203 focusable-unit lookup validation exited cleanly and architecture reported denominator 26; Batch 202 custom game startup validation exited cleanly and architecture reported denominator 27; Batch 201 faction resource validation exited cleanly and architecture reported denominator 28; Batch 200 citizen visible-unit validation exited cleanly and architecture reported denominator 29; Batch 199 citizen visible-unit validation exited cleanly and architecture reported denominator 30; Batch 198 citizen visible-unit validation exited cleanly and architecture reported denominator 31; Batch 197 citizen visible-unit validation exited cleanly and architecture reported denominator 32; Batch 196 citizen visible-unit validation exited cleanly and architecture reported denominator 33; Batch 195 citizen visible-unit validation exited cleanly and architecture reported denominator 34; Batch 194 citizen visible-unit validation exited cleanly and architecture reported denominator 35; Batch 193 citizen visible-unit validation exited cleanly and architecture reported denominator 36; Batch 192 citizen-visible-unit validation exited cleanly and architecture reported denominator 37; Batch 191 citizen population event validation exited cleanly and architecture reported denominator 38; Batch 190 citizen ECS projection validation exited cleanly and architecture reported denominator 39; Batch 189 citizen household registration validation exited cleanly and architecture reported denominator 40; Batch 188 citizen danger validation exited cleanly and architecture reported denominator 41; Batch 187 citizen building read validation exited cleanly and architecture reported denominator 42; Batch 186 building UI query validation and building gameplay composition smoke validation exited cleanly; Batch 185 building UI query validation and building gameplay composition smoke validation exited cleanly; Batch 184 building UI query validation and building gameplay composition smoke validation exited cleanly; Batch 183 selection/input Unity validation exited cleanly; Batch 182 selection/input Unity validation exited cleanly; Batch 181 selection/input Unity validation exited cleanly; Batch 180 selection/input Unity validation exited cleanly; Batch 179 selection/input Unity validation exited cleanly; Batch 178 selection/input Unity validation exited cleanly, while the broad command request/result runner reached the documented pre-existing `UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea` assertion unrelated to the rename; Batch 84 through Batch 118 Unity validations and Batch 124 through Batch 177 Unity validations exited cleanly, except the first Batch 146 road validation launch was retried serially because Unity rejected concurrent project access and Batch 147 architecture was rerun after removing stale road-command transition allowlist entries; Batch 119 architecture validation, Batch 120 Unity validations, Batch 121 building-placement/architecture Unity validations, Batch 122 Unity validations, and Batch 123 placement/architecture Unity validations were terminated after recording pass markers because batchmode hung during post-test cleanup; Batch 123 runtime-tick focused validation failed on the documented pre-existing `SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary` ordering assertion (`Expected: "mapBuildings"`, actual: `"boundary"`); Batch 121 selection command contract validation repeatedly failed in `UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea`, which does not reference the renamed helper and is tracked as an unrelated validation blocker; Batch 22 architecture, Batch 23-32 Unity validations, Batch 54 architecture, Batch 55 Unity validations, Batch 56 Unity validations, Batch 57 runtime-boundary/architecture validations, Batch 58 Unity validations, Batch 59 Unity validations, Batch 60 Unity validations, Batch 61 Unity validations, Batch 62 Unity validations, Batch 63 Unity validations, Batch 64 Unity validations, Batch 65 Unity validations, Batch 66 Unity validations, Batch 67 Unity validations, Batch 68 Unity validations, Batch 69 Unity validations, and Batch 70 Unity validations were terminated after recording pass markers because batchmode hung during post-test cleanup; Batch 71 through Batch 118 Unity validations exited cleanly; Batch 33-53 Unity validations exited cleanly; Batch 54 and Batch 102 building runtime boundary validations and Batch 57 production metadata validation exited cleanly; Batch 103 had no dedicated menu-diagnostics focused runner, so compile plus architecture validation covered the naming-only UI helper slice; Batch 3 building tick focused validation exposed a pre-existing simulation-order test/contract mismatch unrelated to the rename; Batch 8 bootstrap-composition guard exposed a pre-existing UI Toolkit hierarchy lookup unrelated to the rename |

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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`; inventory marker reports `runtimeNonEcsDenominator=225`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementRuntimeTickCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch3-building-runtime-tick-rerun.log`: failed on pre-existing `SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary` expectation mismatch. Current `HEAD` and the renamed working tree both run `UpdateSimulation` boundary first; the test expects map placement queues first. This is not caused by the diagnostics-helper rename and should be handled in a dedicated Agent D behavior/test-contract slice.

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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch5-selection-order-marker.log`: passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch12-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch13-selection-order-marker.log`: passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch14-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch15-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch17-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch18-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch19-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch20-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch21-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch22-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch23-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch24-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch25-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch26-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch27-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch28-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch29-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch30-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch31-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch32-building-gameplay-composition.log`: recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch33-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch34-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch35-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch36-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch37-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch38-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch39-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch39-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=189`.

## Batch 40 - Building Placement Interaction Context Composition Helper

This helper owns placement interaction source delegate packaging and `BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context` construction. The managed reason is interaction context composition wiring, including managed delegates and `GameObject` destruction callbacks, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInteractionContextSystem` | `BuildingPlacementInteractionContextCompositionSystemHelper` | Owns managed placement interaction source/context composition wiring. |

## Batch 40 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `188`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch40-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch41-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch42-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch43-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch43-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=185`.

## Batch 44 - Building Citizen Population Composition Helper

This helper owns building-side citizen population composition: population boundary resolution, resource/prefab context creation, initialization, disposal, and dependency binding. The managed reason is citizen population composition wiring across managed camera/day-night/resource-prefab boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingCitizenPopulationCompositionSystemHelper` | `BuildingCitizenPopulationCompositionSystemHelper` | Owns managed building citizen-population composition wiring. |

## Batch 44 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `184`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch44-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch45-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch47-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch48-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch49-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch50-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch51-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch52-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch53-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch55-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch56-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
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
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionMetadataValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch57-building-production-metadata.log`: passed with `[BuildingProductionMetadataValidation] result=Passed tests=3`.
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

## Batch 62 - Runtime City Surface Integration Utility Helper

This helper owns runtime-city surface integration utility logic: configured `MapSurfaceComponent` state, building footprint surface checks, runtime-city visual grounding, road path surface validation, and primary surface sampling. It has no Unity object lifecycle; the managed reason is reusable surface-integration utility behavior for city visual/planning boundaries, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCitySurfaceIntegrationSystem` | `RuntimeCitySurfaceIntegrationUtilitySystemHelper` | Owns runtime-city surface integration utility behavior. |

## Batch 62 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `166`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch62-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch62-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=166`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 63 - Runtime Decoration Spawner Presentation Helper

This helper owns managed runtime decoration presentation: decoration prefab selection, `GameObject` instantiation under a `Transform` root, transform placement/rotation/scale, spawned-child cleanup, combined-mesh bake timing, and read-only ECS grid/blocker sampling for placement. The managed reason is Unity object decoration presentation, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeDecorationSpawnerSystem` | `RuntimeDecorationSpawnerPresentationSystemHelper` | Owns managed runtime decoration prefab presentation. |

## Batch 63 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `165`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch63-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch63-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=165`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 64 - Runtime Grid Blocker Presentation Helper

This helper owns managed runtime blocker presentation: blocker prefab selection/metadata, `GameObject` root creation and cleanup, runtime blocker footprint bookkeeping for gameplay queries, blocker removal under roads/buildings, and ECS dependency-state publication for dependent startup placement. The managed reason is Unity object blocker presentation with a narrow runtime dependency bridge, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeGridBlockerSystem` | `RuntimeGridBlockerPresentationSystemHelper` | Owns managed runtime blocker prefab presentation and dependency bridge state. |

## Batch 64 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `164`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch64-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch64-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=164`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 65 - Runtime City Archway Spawn Prefab Helper

This helper wraps central archway prefab placement state for runtime-city generation: archway prefab cycling, hall-distance filtering, random plot selection, plot spacing, footprint lookup, spawn/reserve request construction, and used-plot recording. The managed reason is runtime-city prefab placement orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityArchwaySpawnSystem` | `RuntimeCityArchwaySpawnPrefabSystemHelper` | Owns runtime-city archway prefab placement orchestration. |

## Batch 65 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `163`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch65-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch65-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=163`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 66 - Runtime City Building Placement Prefab Helper

This helper wraps shared runtime-city building prefab placement state: request/result payloads, cached prefab footprint lookup, spawn/delete-on-failed-validation, road and required-rect checks, reserved-footprint updates, placement anchors, and plot-driven placement loops. The managed reason is runtime-city prefab placement orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityBuildingPlacementSystem` | `RuntimeCityBuildingPlacementPrefabSystemHelper` | Owns runtime-city building prefab placement and spawn/reserve orchestration. |

## Batch 66 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `162`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch66-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch66-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=162`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 67 - Runtime City Building Plot Utility Helper

This helper wraps runtime-city plot planning state: plot candidate data, roadside and entry plot collection, corridor plot generation, adjacent-origin building, scatter plot selection, plot spacing checks, and plot-to-origin centering math. The managed reason is reusable runtime-city plot utility logic, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityBuildingPlotSystem` | `RuntimeCityBuildingPlotUtilitySystemHelper` | Owns runtime-city building plot planning utility behavior. |

## Batch 67 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `161`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch67-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch67-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=161`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 68 - Runtime City Building Spawn Context Composition Helper

This helper packages runtime-city building-spawn context and child-system dependency bundles for composition. The managed reason is runtime-city composition/context assembly, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityBuildingSpawnContextSystem` | `RuntimeCityBuildingSpawnContextCompositionSystemHelper` | Owns runtime-city building-spawn context and dependency composition. |

## Batch 68 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `160`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch68-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch68-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=160`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 69 - Runtime City Bulk Building Spawn Routine Prefab Helper

This helper owns the runtime-city bulk building spawn coroutine sequence and callback payloads for managed building/decor prefab placement. The managed reason is runtime-city prefab spawn orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityBulkBuildingSpawnRoutineSystem` | `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper` | Owns runtime-city bulk building prefab spawn coroutine orchestration. |

## Batch 69 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `159`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch69-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch69-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=159`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 70 - Runtime City Bulk Plot Plan Utility Helper

This helper assembles runtime-city bulk plot plans and shuffles central, outer, and entry plot candidates. The managed reason is reusable runtime-city plot planning utility logic, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityBulkPlotPlanSystem` | `RuntimeCityBulkPlotPlanUtilitySystemHelper` | Owns runtime-city bulk plot plan utility behavior. |

## Batch 70 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `158`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch70-runtime-city-generation.log`: recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch70-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=158`; Unity was terminated after the pass marker because batchmode hung during post-test cleanup.

## Batch 71 - Runtime City Chain Utility Helper

This helper owns runtime-city chain next-city planning, travel direction selection, city spacing checks, and autobahn path validation. The managed reason is reusable runtime-city planning utility logic, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityChainSystem` | `RuntimeCityChainUtilitySystemHelper` | Owns runtime-city chain planning utility behavior. |

## Batch 71 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `157`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch71-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch71-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=157`.

## Batch 72 - Runtime City Cloth-Cover Spawn Prefab Helper

This helper places cloth-cover decoration prefabs adjacent to shop and house footprints. The managed reason is runtime-city prefab spawn placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityClothCoverSpawnSystem` | `RuntimeCityClothCoverSpawnPrefabSystemHelper` | Owns runtime-city cloth-cover prefab placement behavior. |

## Batch 72 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `156`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch72-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch72-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=156`.

## Batch 73 - Runtime City Decoration Group Prefab Helper

This helper groups runtime-city decoration prefabs into cloth-cover, archway, and free-scatter buckets. The managed reason is prefab classification/grouping, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityDecorationPrefabGroupSystem` | `RuntimeCityDecorationGroupPrefabSystemHelper` | Owns runtime-city decoration prefab grouping behavior. |

## Batch 73 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `155`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch73-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch73-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=155`.

## Batch 74 - Runtime City Free-Scatter Decoration Prefab Helper

This helper places free-scatter decoration prefabs around runtime-city plots. The managed reason is prefab placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityFreeScatterDecorationSystem` | `RuntimeCityFreeScatterDecorationPrefabSystemHelper` | Owns runtime-city free-scatter decoration prefab placement behavior. |

## Batch 74 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator remains `155` because the removed free-scatter row was offset by an existing composition passive-boundary row surfaced during regeneration.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch74-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch74-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=155`.

## Batch 75 - Runtime City Corridor Building Spawn Prefab Helper

This helper places runtime-city corridor entrance shop and house prefabs from corridor-side plots. The managed reason is prefab placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityCorridorBuildingSpawnSystem` | `RuntimeCityCorridorBuildingSpawnPrefabSystemHelper` | Owns runtime-city corridor entrance building prefab placement behavior. |

## Batch 75 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `154`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch75-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch75-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=154`.

## Batch 76 - Runtime City Decoration Building Spawn Prefab Helper

This helper sequences runtime-city decoration prefab placement through cloth-cover, archway, and free-scatter placement helpers. The managed reason is prefab spawn orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityDecorationBuildingSpawnSystem` | `RuntimeCityDecorationBuildingSpawnPrefabSystemHelper` | Owns runtime-city decoration building prefab spawn orchestration. |

## Batch 76 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `153`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch76-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch76-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=153`.

## Batch 77 - Runtime City Entry Building Spawn Prefab Helper

This helper places runtime-city entry shop and house prefabs from entry plots. The managed reason is prefab placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityEntryBuildingSpawnSystem` | `RuntimeCityEntryBuildingSpawnPrefabSystemHelper` | Owns runtime-city entry shop and house prefab placement behavior. |

## Batch 77 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `152`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch77-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch77-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=152`.

## Batch 78 - Runtime City Roadside Building Spawn Prefab Helper

This helper places runtime-city roadside market, shop, gas-station, and house prefabs and owns the related placement plan payload. The managed reason is prefab placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityRoadsideBuildingSpawnSystem` | `RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper` | Owns runtime-city roadside building prefab placement behavior. |

## Batch 78 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `151`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch78-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch78-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=151`.

## Batch 79 - Runtime City Rural Building Spawn Prefab Helper

This helper samples rural plots and spawns/reserves rural building prefabs. The managed reason is prefab placement, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityRuralBuildingSpawnSystem` | `RuntimeCityRuralBuildingSpawnPrefabSystemHelper` | Owns runtime-city rural building prefab placement behavior. |

## Batch 79 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `150`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch79-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch79-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=150`.

## Batch 80 - Runtime City Spawn Bridge Prefab Helper

This helper bridges runtime-city generated building prefab spawn/delete calls and deferred side effects over `BuildingRuntimeCitySpawnBridgeCompositionSystemHelper`. The managed reason is prefab spawn bridging, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCitySpawnBridgeSystem` | `RuntimeCitySpawnBridgePrefabSystemHelper` | Owns runtime-city generated building prefab spawn/delete bridge behavior. |

## Batch 80 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `149`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch80-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch80-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=149`.

## Batch 81 - Runtime City Road-Build Bridge Composition Helper

This helper bridges runtime-city road commit requests to `RoadRuntimeGenerationCompositionSystemHelper`, including deferred ECS sync hooks, road-cell-size fallback, and standalone connector handoff. The managed reason is composition bridge state around an existing runtime road-generation boundary, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityRoadBuildBridgeSystem` | `RuntimeCityRoadBuildBridgeCompositionSystemHelper` | Owns runtime-city to road-runtime-generation bridge composition state. |

## Batch 81 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `148`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch81-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch81-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=148`.

## Batch 82 - Runtime City Road Commit Composition Helper

This helper owns runtime-city road commit coordination, including city road network commit, road-cell population, source-exit/autobahn commit, standalone connector handoff, occupied-road-cell mutation, and road commit failure result shaping. The managed reason is composition state around the runtime-city road commit workflow, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityRoadCommitSystem` | `RuntimeCityRoadCommitCompositionSystemHelper` | Owns runtime-city road commit composition state and road-generation bridge calls. |

## Batch 82 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `147`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with one transient Unity XML copy warning and no errors.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch82-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch82-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=147`.

## Batch 83 - Runtime City Road Layout Utility Helper

This helper owns runtime-city road layout/path planning over value data, including town road strokes, straight road paths, city-to-city autobahn paths, autobahn anchor selection, and low-level stroke segment helpers. The managed reason is pure runtime-city utility planning, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityRoadLayoutSystem` | `RuntimeCityRoadLayoutUtilitySystemHelper` | Owns runtime-city road layout and path-planning utility methods. |

## Batch 83 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `146`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch83-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch83-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=146`.

## Batch 84 - Runtime City Startup Helper

This helper owns runtime-city startup and manual-generation gating, including spawn-on-start readiness, play-request checks, mission exclusion policy, dependency availability checks, required prefab readiness, initial-unit readiness gating, diagnostic wait throttling, and startup gate result shaping. The managed reason is startup sequencing, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityStartupSystem` | `RuntimeCityStartupSystemHelper` | Owns runtime-city startup/manual-generation gating and diagnostics cadence. |

## Batch 84 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `145`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch84-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch84-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=145`.

## Batch 85 - Runtime City Config Composition Helper

This helper owns runtime-city config snapshot projection and fallback state, including spawn-on-start flags, generation counts, density/spacing policy values, default health, and prefab-category list references. The managed reason is composition-owned config state and prefab reference projection, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityConfigSystem` | `RuntimeCityConfigCompositionSystemHelper` | Owns runtime-city config snapshot projection and prefab-list fallback state. |

## Batch 85 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `144`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch85-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch85-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=144`.

## Batch 86 - Runtime City Lifecycle Composition Helper

This helper owns runtime-city generation lifecycle state, including spawned/generating flags, coroutine routine ownership, generation frame counters, generation diagnostic cadence, and yield cadence. The managed reason is composition-owned coroutine/lifecycle state, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityLifecycleSystem` | `RuntimeCityLifecycleCompositionSystemHelper` | Owns runtime-city generation lifecycle state and coroutine routine ownership. |

## Batch 86 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `143`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch86-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch86-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=143`.

## Batch 87 - Runtime City Readiness Query Composition Helper

This helper owns runtime-city readiness ECS query access for composition, including grid-data query caching, grid config lookup, initial-unit readiness checks, and initial base exclusion road-rect collection. The managed reason is composition-owned ECS readiness query boundary state, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityReadinessQuerySystem` | `RuntimeCityReadinessQueryCompositionSystemHelper` | Owns runtime-city readiness query caching and initial-unit/base-exclusion lookups. |

## Batch 87 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `142`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch87-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch87-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=142`.

## Batch 88 - Runtime City Minimap Event UI Helper

This helper owns queued runtime-city static minimap invalidation and the UI-facing `IMatchRuntimeUi.NotifyStaticMinimapChanged` flush. The managed reason is UI notification state at the composition edge, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityMinimapEventSystem` | `RuntimeCityMinimapEventUiSystemHelper` | Owns runtime-city static minimap invalidation queue and UI-facing flush. |

## Batch 88 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `141`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch88-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch88-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=141`.

## Batch 89 - Runtime City Prefab Selection Helper

This helper owns runtime-city prefab membership checks, random prefab selection, list shuffling, renderer-based footprint estimation, and prefab footprint caching. The managed reason is GameObject prefab/renderer metadata, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityPrefabSelectionSystem` | `RuntimeCityPrefabSelectionPrefabSystemHelper` | Owns runtime-city GameObject prefab selection and footprint metadata cache. |

## Batch 89 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `140`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch89-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch89-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=140`.

## Batch 90 - Runtime City Landmark Offset Utility Helper

This helper owns runtime-city landmark offset tables and hall-distance filtering over value data. The managed reason is pure runtime-city utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityLandmarkOffsetSystem` | `RuntimeCityLandmarkOffsetUtilitySystemHelper` | Owns runtime-city landmark offset arrays and hall-distance filtering. |

## Batch 90 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `139`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch90-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch90-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=139`.

## Batch 91 - Runtime City Yard Gate Utility Helper

This helper owns runtime-city yard gate side policy and centered opening math over value data. The managed reason is pure runtime-city utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityYardGateSystem` | `RuntimeCityYardGateUtilitySystemHelper` | Owns yard gate side selection and centered opening math. |

## Batch 91 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `138`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch91-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch91-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=138`.

## Batch 92 - Runtime City Yard Wall Plan Utility Helper

This helper owns runtime-city yard-wall house planning, target-count calculation, and yard-rect fit candidate ordering over value data. The managed reason is runtime-city planning utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityYardWallPlanSystem` | `RuntimeCityYardWallPlanUtilitySystemHelper` | Owns yard-wall house-plan shuffling, target counts, and yard-rect fit candidate ordering. |

## Batch 92 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `137`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch92-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch92-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=137`.

## Batch 93 - Runtime City Walkability Utility Helper

This helper owns runtime-city reserved-footprint data, entrance-corridor reservation, reserved-footprint clearance checks, road-overlap checks, yard-rect checks, rectangle expansion, touch checks, and plot-origin math over value data. The managed reason is runtime-city walkability utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityWalkabilitySystem` | `RuntimeCityWalkabilityUtilitySystemHelper` | Owns reserved-footprint and walkability value policy. |

## Batch 93 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `136`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch93-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch93-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=136`.

## Batch 94 - Runtime City Ingress Utility Helper

This helper owns runtime-city city-layout creation, incoming-anchor stroke wiring, inner connection-cell math, city connection offset math, and ingress-corridor pruning over value data. The managed reason is runtime-city ingress utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityIngressSystem` | `RuntimeCityIngressUtilitySystemHelper` | Owns ingress layout stitching, connection math, and corridor pruning value policy. |

## Batch 94 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `135`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch94-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch94-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=135`.

## Batch 95 - Runtime City Layout Utility Helper

This helper owns runtime-city town radius calculation, city-chain axis selection, center planning, buildable-road bounds, base-exclusion avoidance, and city-spacing checks over value data. The managed reason is runtime-city layout utility policy, not ECS scheduling or Unity-object presentation.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityLayoutSystem` | `RuntimeCityLayoutUtilitySystemHelper` | Owns city layout math, center planning, bounds, and base-exclusion value policy. |

## Batch 95 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `134`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch95-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch95-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=134`.

## Batch 96 - Runtime City Hall Spawn Prefab Helper

This helper owns runtime-city hall prefab candidate shuffling, footprint lookup, centered-origin search, spawn-and-reserve requests, clearance reservation, and hall failure diagnostics. The managed reason is GameObject prefab spawning and placement boundary orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityHallSpawnSystem` | `RuntimeCityHallSpawnPrefabSystemHelper` | Owns hall prefab selection, spawn/reserve requests, and placement diagnostics. |

## Batch 96 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `133`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch96-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch96-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=133`.

## Batch 97 - Runtime City Landmark Spawn Prefab Helper

This helper owns clock tower, fountain, monument, and pillar prefab selection, offset iteration, hall-distance rejection, spawn-and-reserve requests, and landmark clearance reservation. The managed reason is GameObject prefab spawning and placement boundary orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityLandmarkSpawnSystem` | `RuntimeCityLandmarkSpawnPrefabSystemHelper` | Owns landmark prefab selection, validation, spawn/reserve requests, and placement labels. |

## Batch 97 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `132`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch97-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch97-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=132`.

## Batch 98 - Runtime City Yard-Wall Visual Presentation Helper

This helper owns yard-wall, gate, and pillar visual-only prefab spawning for house-yard boundaries, including wall run splitting, gate rotation choices, pillar footprint lookup, and `RuntimeCityVisualPresentationSystemHelper.SpawnVisualOnlyPrefab` calls. The managed reason is Unity-object presentation spawning, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityYardWallVisualSystem` | `RuntimeCityYardWallVisualPresentationSystemHelper` | Owns visual-only wall, gate, and pillar prefab presentation for runtime-city yard boundaries. |

## Batch 98 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `131`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch98-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch98-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=131`.

## Batch 99 - Runtime City House Yard-Wall Prefab Helper

This helper owns house yard-wall prefab orchestration, including wall/gate/pillar prefab preconditions, house-plan iteration, successful-wall target counting, wall prefab choice, visual spawn handoff, and footprint reservation. The managed reason is GameObject prefab placement orchestration, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityHouseYardWallSystem` | `RuntimeCityHouseYardWallPrefabSystemHelper` | Owns house yard-wall prefab orchestration and visual handoff for runtime-city yards. |

## Batch 99 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `130`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch99-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch99-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=130`.

## Batch 100 - Runtime City Composition Helper

This helper owns the runtime-city child-system graph, startup wiring, bridge and visual/minimap configuration, context factory creation, update orchestration, read-model publication, and disposal. The managed reason is composition orchestration at the runtime-city boundary, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityCompositionSystem` | `RuntimeCityCompositionSystemHelper` | Owns runtime-city composition graph, context factories, update orchestration, and disposal. |

## Batch 100 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `129`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch100-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch100-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=129`.

## Batch 101 - Runtime City Generation Composition Helper

This helper owns runtime-city generation coroutine orchestration, lifecycle gating, deferred road/building side-effect ordering, city-list and RNG lifetime, bulk-building routine stepping, minimap event publication, and generation completion. The managed reason is coroutine and side-effect composition at the runtime-city generation boundary, not ECS scheduling.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeCityGenerationSystem` | `RuntimeCityGenerationCompositionSystemHelper` | Owns runtime-city generation coroutine orchestration and side-effect composition. |

## Batch 101 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `128`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch101-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch101-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=128`.

## Batch 102 - Match Building Runtime Boundary Bootstrap Startup Helper

This helper is a static startup/composition boundary that ensures the building runtime ECS boundary entity and explicit read-model/request buffers exist. It does not own gameplay policy or a frame update loop.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchBuildingRuntimeBoundaryBootstrapSystem` | `MatchBuildingRuntimeBoundaryBootstrapStartupSystemHelper` | Ensures the building runtime boundary entity and buffers during match startup. |

## Batch 102 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `127`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch102-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch102-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=127`.

## Batch 103 - Menu Diagnostics UI Helper

This helper binds menu FPS/log diagnostics UI, subscribes to Unity runtime log callbacks, and formats diagnostic text for a view. It is a UI presentation helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MenuDiagnosticsSystem` | `MenuDiagnosticsUiSystemHelper` | Binds and updates menu diagnostics UI presentation. |

## Batch 103 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `126`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- Dedicated menu-diagnostics focused runner: not present; this naming-only UI helper slice was covered by compile plus the architecture guardrail.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch103-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=126`.

## Batch 104 - Armory Catalog Query UI Helper

This helper collects Armory catalog display rows from UI-prefab metadata resolvers and sorts the results for presentation. It reads prefab metadata for UI display and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `ArmoryCatalogQuerySystem` | `ArmoryCatalogQueryUiSystemHelper` | Queries Armory catalog prefab metadata for UI display rows. |

## Batch 104 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `125`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ArmoryCurrentContentPrefabTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch104-armory-current-content.log`: passed with `[ArmoryCurrentContentValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch104-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=125`.

## Batch 105 - Build Drawer Catalog Query UI Helper

This helper collects Build Drawer catalog rows from UI-prefab metadata resolvers, resolves requestable building/unit prefabs, and sorts presentation rows for the Build Drawer. It is a UI query/presentation helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildDrawerCatalogQuerySystem` | `BuildDrawerCatalogQueryUiSystemHelper` | Queries Build Drawer prefab metadata for UI display and command rows. |

## Batch 105 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `124`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildDrawerCatalogQueryUiSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch105-build-drawer-catalog.log`: passed with `[BuildDrawerCatalogQueryValidation] result=Passed tests=22`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch105-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=124`.

## Batch 106 - Match HUD Minimap Input UI Helper

This helper binds minimap UI input, viewport/zoom/focus interactions, static-map capture resources, and marker/viewport presentation refresh. It is a UI input/presentation helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchHudMinimapInputSystem` | `MatchHudMinimapInputUiSystemHelper` | Binds minimap UI input and presentation refresh. |

## Batch 106 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `123`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudMinimapProjectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch106-minimap-projection.log`: passed with `[MatchHudMinimapProjectionFocusedValidation] result=Passed tests=11`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch106-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=123`.

## Batch 107 - Match HUD Minimap Projection UI Helper

This helper owns minimap world/normalized projection math, viewport rect helpers, camera-centered grid selection, and capture-camera configuration. It is a static UI projection helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchHudMinimapProjectionSystem` | `MatchHudMinimapProjectionUiSystemHelper` | Computes minimap UI projection and capture-camera geometry. |

## Batch 107 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `122`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudMinimapProjectionUiSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch107-minimap-projection.log`: passed with `[MatchHudMinimapProjectionFocusedValidation] result=Passed tests=11`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch107-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=122`.

## Batch 108 - Match Overlay Command Input UI Helper

This helper binds Match HUD command buttons, command-wheel fallback listeners, Build Drawer open/close callbacks, and typed command feedback routing to the UI command/read-model contracts. It is a UI input helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchOverlayCommandInputSystem` | `MatchOverlayCommandInputUiSystemHelper` | Binds Match HUD command button input to UI command contracts. |

## Batch 108 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `121`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch108-command-controls.log`: passed with `[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch108-command-feedback.log`: passed with `[MatchHudCommandFeedbackValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch108-shell-content.log`: passed with `[UIShellCurrentContentLoadValidation] result=Passed tests=10`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch108-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=121`.

## Batch 109 - Match Overlay Command Tab Feedback UI Helper

This helper applies command-mode feedback to explicit Match HUD command-tab groups and clears selected UI objects through `EventSystem`. It is a UI presentation helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchOverlayCommandTabFeedbackSystem` | `MatchOverlayCommandTabFeedbackUiSystemHelper` | Applies command-mode tab feedback to Match HUD UI groups. |

## Batch 109 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `120`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch109-command-feedback.log`: passed with `[MatchHudCommandFeedbackValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch109-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=120`.

## Batch 110 - Match Overlay Command Tab Visual UI Helper

This helper applies selected/normal sprites to explicit Match HUD command tabs. It is a UI presentation helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchOverlayCommandTabVisualSystem` | `MatchOverlayCommandTabVisualUiSystemHelper` | Applies selected/normal command-tab sprites for Match HUD UI groups. |

## Batch 110 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `119`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandFeedbackPanelTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch110-command-feedback.log`: passed with `[MatchHudCommandFeedbackValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch110-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch110-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=119`.

## Batch 111 - Quick Custom Screen Flow UI Helper

This helper binds Quick Custom screen defaults/current config, applies control state into the runtime config store, and forwards launch requests. It is a UI flow helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `QuickCustomScreenFlowSystem` | `QuickCustomScreenFlowUiSystemHelper` | Binds Quick Custom UI config flow and launch forwarding. |

## Batch 111 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `118`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed with one transient Unity XML copy warning and no errors.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch111-shell-content.log`: passed with `[UIShellCurrentContentLoadValidation] result=Passed tests=10`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch111-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=118`.

## Batch 112 - Settings Screen Flow UI Helper

This helper loads, saves, resets, and applies settings for the Settings screen through `SettingsService`. It is a UI flow helper and does not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SettingsScreenFlowSystem` | `SettingsScreenFlowUiSystemHelper` | Binds Settings UI persistence, reset, and runtime preference application. |

## Batch 112 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `117`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch112-shell-content.log`: passed with `[UIShellCurrentContentLoadValidation] result=Passed tests=10`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch112-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=117`.

## Batch 113 - Match Bootstrap Composition Helper

This helper composes match-scene startup, runtime update delegation, and shutdown boundaries around serialized `MatchSceneView` references. It is a composition boundary and must not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchBootstrapSystem` | `MatchBootstrapCompositionSystemHelper` | Composes match-scene lifecycle and delegates gameplay work to narrower runtime/ECS systems. |

## Batch 113 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `116`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch113-bootstrap-composition.log`: passed with `[BootstrapCompositionGuardrailValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch113-architecture-rerun.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=116`.

## Batch 114 - Menu Bootstrap Composition Helper

This helper composes persistent menu/UI shell startup, loading progress, match-load handoff, and diagnostics boundaries around serialized `MenuBootstrapView` references. It is a composition boundary and must not own gameplay policy.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MenuBootstrapSystem` | `MenuBootstrapCompositionSystemHelper` | Composes persistent menu/UI shell lifecycle and delegates scene/load work to narrower systems. |

## Batch 114 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `115`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch114-bootstrap-composition.log`: passed with `[BootstrapCompositionGuardrailValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch114-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=115`.

## Batch 115 - Building Barrier Utility Helper

This helper owns barrier/gate utility behavior for base-breach memory, wall/gate classification, breach target lookup, road barrier doors, gate alignment, and expanded selection checks. It remains a plain helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingBarrierSystem` | `BuildingBarrierUtilitySystemHelper` | Provides barrier/gate utility logic used by building runtime, placement, selection, and AI helpers. |

## Batch 115 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `114`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed with one transient Unity PDB copy warning and no errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingBarrierUtilitySystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch115-building-barrier.log`: passed with `[BuildingBarrierFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch115-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=114`.

## Batch 116 - Building Combat Utility Helper

This helper owns destroyed-building state, cleanup-id collection, runtime combat-state resolution, blocker destruction, runtime entity destruction sync, destroyed visual handoff, and marker/minimap callbacks. It remains a plain helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingCombatSystem` | `BuildingCombatUtilitySystemHelper` | Provides building combat/destruction utility behavior used by building runtime composition. |

## Batch 116 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `113`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingCombatUtilitySystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch116-building-combat.log`: passed with `[BuildingCombatFocusedValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch116-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=113`.

## Batch 117 - Building Placement Commit Composition Helper

This helper owns placement commit context/request data, wall-run expansion, wall segment footprint/rotation helpers, visual creation/position/register delegates, preview consumption, and post-placement auto-select policy. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementCommitSystem` | `BuildingPlacementCommitCompositionSystemHelper` | Composes building placement commit requests and delegates for managed placement flow. |

## Batch 117 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `112`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed with transient Unity PDB copy warnings and no errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch117-building-placement-command.log`: passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch117-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=112`.

## Batch 118 - Building Placement Grid Camera Helper

This helper owns placement grid math and screen-to-grid camera projection for managed building placement flow. It remains a plain camera-boundary helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementGridSystem` | `BuildingPlacementGridCameraSystemHelper` | Resolves placement grid positions and camera-based screen projection for managed placement flow. |

## Batch 118 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `111`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch118-building-placement-command.log`: passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch118-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=111`.

## Batch 119 - Building Placement Input Runtime Tick UI Helper

This helper owns managed pointer/UI gating for placement input runtime ticks, building selection release handling, placement preview hiding, and input timing results. It remains a plain UI/input boundary helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInputRuntimeTickSystem` | `BuildingPlacementInputRuntimeTickUiSystemHelper` | Handles managed placement pointer/UI tick flow outside ECS scheduling. |

## Batch 119 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `110`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch119-building-placement-command.log`: passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch119-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=110`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.

## Batch 120 - Building Placement Input UI Helper

This helper owns placement pointer drag state, UI pointer-down suppression, wall-run scratch lists, hover updates, and wall-run commit behavior. It remains a plain UI/input helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInputSystem` | `BuildingPlacementInputUiSystemHelper` | Handles managed placement pointer and wall-run input state outside ECS scheduling. |

## Batch 120 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `109`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch120-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch120-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=109`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.

## Batch 121 - Building Placement Interaction Boundary Composition Helper

This helper owns the shared managed placement/building interaction boundary for selected-building state queries, placement confirm/cancel/exit routing, Soldier Base placement start, runtime entity destroyed handling, base-breach target resolution, and selected-building label/status helpers. It remains a plain composition boundary helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInteractionSystem` | `BuildingPlacementInteractionBoundaryCompositionSystemHelper` | Routes managed placement/building interaction commands and state queries through a shared boundary. |

## Batch 121 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `108`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch121-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch121-selection-command-contract.log`: failed at `SelectionCommandRequestResultContractTests.UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea` before completing the batch; rerun at `/private/tmp/warline-non-ecs-helper-naming-batch121-selection-command-contract-rerun.log` failed at the same assertion. The failing test does not reference the renamed helper and is tracked as an unrelated validation blocker for this naming-only slice.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch121-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=108`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.

## Batch 122 - Building Placement Invalid-Cell Cache Composition Helper

This helper owns the managed placement invalid-cell prefix cache, road-footprint mask use, runtime blocker checks, and cached placement validation queries. It remains a plain stateful composition/cache helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementInvalidCellSystem` | `BuildingPlacementInvalidCellCacheCompositionSystemHelper` | Owns managed invalid-cell cache state and placement validity composition. |

## Batch 122 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `107`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch122-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch122-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=107`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.

## Batch 123 - Building Placement Runtime Tick Composition Helper

This helper owns managed building placement runtime tick orchestration, cadence timers, per-slice profiler markers, diagnostics timing, and callback fan-out for startup/simulation tick phases. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementRuntimeTickSystem` | `BuildingPlacementRuntimeTickCompositionSystemHelper` | Owns managed runtime tick orchestration and diagnostics timing composition. |

## Batch 123 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `106`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementRuntimeTickCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch123-building-runtime-tick.log`: failed on the documented pre-existing `SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary` ordering assertion (`Expected: "mapBuildings"`, actual: `"boundary"`); Unity then crashed during shutdown after writing the failure marker.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch123-building-placement-command.log`: recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch123-architecture.log`: recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=106`; Unity batchmode was terminated after the pass marker because it hung during post-test cleanup.

## Batch 124 - Building Placement Validation Utility Helper

This helper owns static placement footprint, wall-run, road/blocker, invalid-prefix, and overlap validation utilities. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingPlacementValidationSystem` | `BuildingPlacementValidationUtilitySystemHelper` | Provides static placement and wall validation utilities for managed placement flow. |

## Batch 124 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `105`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingPlacementValidationUtilitySystemHelperTests.RunPlacementCommandRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch124-building-placement-command.log`: passed with `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch124-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=105`.

## Batch 125 - Building Production Runtime Tick Composition Helper

This helper owns managed production tick orchestration across pending production, active transports, resource production, resource haulers, random-state propagation, metrics callbacks, and spawn-reservation cleanup. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionRuntimeTickSystem` | `BuildingProductionRuntimeTickCompositionSystemHelper` | Coordinates managed production runtime tick slices and delegates without owning simulation data. |

## Batch 125 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `104`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch125-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch125-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ResourceHaulerUtilitySystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch125-resource-hauler.log`: passed with `[ResourceHaulerFocusedValidation] result=Passed tests=9`; the first concurrent attempt aborted before running tests because Unity rejected a second project instance, then this serialized rerun passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch125-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=104`.

## Batch 126 - Building Production Slot Utility Helper

This helper owns managed production slot reservation, pending-slot checks, slot occupancy cleanup, spawn local-position lookup, and produced-unit liveness checks. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionSlotSystem` | `BuildingProductionSlotUtilitySystemHelper` | Provides production slot reservation and occupancy utilities for managed production flow. |

## Batch 126 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `103`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch126-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch126-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch126-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=103`.

## Batch 127 - Building Production Queue Composition Helper

This helper owns managed production queueing, pending-production pooling, production progress, transport setting resolution, unit production metadata, produced-unit pruning, and transport launch timing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionSystem` | `BuildingProductionQueueCompositionSystemHelper` | Coordinates managed building production queue, progress, metadata, and transport settings. |

## Batch 127 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `102`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch127-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionMetadataValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch127-building-production-metadata.log`: passed with `[BuildingProductionMetadataValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch127-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch127-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=102`.

## Batch 128 - Building Production Transport Bridge Composition Helper

This helper owns managed production transport bridge operations: helipad spawn resolution, produced-unit movement orders, rotation alignment, spawn-near-building routing, newest produced-unit lookup, and camera focus callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionTransportBridgeSystem` | `BuildingProductionTransportBridgeCompositionSystemHelper` | Bridges managed production transport requests to ECS movement/spawn/focus boundaries. |

## Batch 128 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `101`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionCameraFocusValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch128-building-production-camera-focus.log`: passed with `[BuildingProductionCameraFocusValidation] result=Passed tests=10`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch128-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch128-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch128-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=101`.

## Batch 129 - Building Production Transport Presentation Helper

This helper owns managed active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, prefab pooling, door animation, and transport visual helpers. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionTransportSystem` | `BuildingProductionTransportPresentationSystemHelper` | Owns managed production transport visual/presentation lifecycle and delegates ECS movement, spawn, and focus boundaries to bridge/composition helpers. |

## Batch 129 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `100`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionCameraFocusValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch129-building-production-camera-focus.log`: passed with `[BuildingProductionCameraFocusValidation] result=Passed tests=10`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch129-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch129-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch129-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=100`.

## Batch 130 - Building Production Update Composition Helper

This helper owns managed production update orchestration for pending production iteration, transport launch checks, active transport ticking, random-state mutation, spawn completion, and timeline rebuild routing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingProductionUpdateSystem` | `BuildingProductionUpdateCompositionSystemHelper` | Coordinates production queue updates and transport presentation helper calls across runtime buildings without owning ECS lifecycle state. |

## Batch 130 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `99`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch130-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch130-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch130-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=99`.

## Batch 131 - Building Resource Hauler Bridge Composition Helper

This helper owns managed resource-hauler bridge orchestration: hauler query collection, selected-hauler assignment, ECS move-order/path request bridging, runtime building approach checks, and approach-cell search. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingResourceHaulerBridgeSystem` | `BuildingResourceHaulerBridgeCompositionSystemHelper` | Bridges managed runtime building state to ECS resource-hauler query, move-order, path-request, and approach-cell boundaries. |

## Batch 131 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `98`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ResourceHaulerUtilitySystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch131-resource-hauler.log`: passed with `[ResourceHaulerFocusedValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch131-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch131-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=98`.

## Batch 132 - Building Runtime Boundary Processing Composition Helper

This helper owns managed runtime boundary processing: faction resource sell requests, UI production requests, production request draining, runtime spawn request processing, read-model publication, production/resource summaries, surface overlay publishing, and configured read-model publication. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeBoundarySystem` | `BuildingRuntimeBoundaryProcessingCompositionSystemHelper` | Processes ECS boundary buffers and publishes managed building runtime read models without owning ECS lifecycle state. |

## Batch 132 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `97`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch132-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch132-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch132-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch132-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=97`.

## Batch 133 - Building Runtime City Spawn Bridge Composition Helper

This helper owns managed runtime-city generated building spawn/delete bridging, ECS runtime spawn request routing, fallback runtime spawn, and deferred side-effect callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeCitySpawnSystem` | `BuildingRuntimeCitySpawnBridgeCompositionSystemHelper` | Bridges runtime-city prefab spawn/delete calls through building runtime spawn commands and fallback managed spawn boundaries. |

## Batch 133 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `96`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch133-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeCityGenerationFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch133-runtime-city-generation.log`: passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch133-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch133-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=96`.

## Batch 134 - Building Runtime Context Factory Composition Helper

This helper owns managed runtime context factory construction for spawn, creation, ownership, city-spawn, runtime entity, visual, selection-marker, redirect, combat, query, barrier, resource-hauler, and building-spawn contexts. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeContextSystem` | `BuildingRuntimeContextFactoryCompositionSystemHelper` | Builds narrow building runtime contexts across managed runtime domains without owning ECS lifecycle state. |

## Batch 134 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `95`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch134-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch134-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch134-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=95`.

## Batch 135 - Building Runtime Creation Composition Helper

This helper owns managed runtime building creation: runtime registry insertion, blocker/combat entity creation callbacks, link attachment, foundation visual adjustment, production collection initialization, redirect side effects, and marker refresh callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeCreationSystem` | `BuildingRuntimeCreationCompositionSystemHelper` | Composes managed runtime building creation and side-effect callbacks without owning ECS lifecycle state. |

## Batch 135 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `94`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch135-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch135-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch135-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=94`.

## Batch 136 - Building Runtime Entity Composition Helper

This helper owns managed runtime blocker/combat entity creation, runtime building delete and destroyed callbacks, path-blocking policy, and runtime combat component setup. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeEntitySystem` | `BuildingRuntimeEntityCompositionSystemHelper` | Composes runtime building blocker/combat entity creation and destruction callbacks without owning ECS lifecycle state. |

## Batch 136 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `93`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch136-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch136-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch136-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=93`.

## Batch 137 - Building Runtime Ownership Composition Helper

This helper owns managed runtime owner-faction assignment, combat `Faction` projection, gate friendly-pass blocker updates, and owner-faction visual projection. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeOwnershipSystem` | `BuildingRuntimeOwnershipCompositionSystemHelper` | Composes runtime building ownership, combat faction, gate pass, and faction visual updates without owning ECS lifecycle state. |

## Batch 137 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `92`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch137-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch137-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch137-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=92`.

## Batch 138 - Building Runtime Read-Model Composition Helper

This helper owns managed runtime building read/query APIs: faction counts, produced-unit and pending-production counts, building role/id lists, focus and combat reads, owner/destroyed/city/refugee flags, approach-cell queries, and base-breach target routing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeQuerySystem` | `BuildingRuntimeReadModelCompositionSystemHelper` | Composes runtime building read-model and query APIs without owning ECS lifecycle state. |

## Batch 138 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `91`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch138-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch138-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch138-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch138-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=91`.

## Batch 139 - Building Runtime Spawn Composition Helper

This helper owns managed runtime spawn orchestration: initial roster spawn, runtime building spawn, wall-run and wall-segment spawn, runtime footprint queries, placement validation, visual instantiation callbacks, registration callbacks, and owner-faction assignment. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeSpawnSystem` | `BuildingRuntimeSpawnCompositionSystemHelper` | Composes managed runtime building and wall spawn orchestration without owning ECS lifecycle state. |

## Batch 139 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `90`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch139-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch139-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch139-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch139-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=90`.

## Batch 140 - Building Runtime Update Composition Helper

This helper owns managed runtime update dispatch: startup tick callback, simulation tick callback, and runtime building entity-link synchronization. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingRuntimeUpdateSystem` | `BuildingRuntimeUpdateCompositionSystemHelper` | Dispatches managed building runtime startup/simulation callbacks and link synchronization without owning ECS lifecycle state. |

## Batch 140 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `89`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch140-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch140-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch140-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=89`.

## Batch 141 - Building Selection Click Utility Helper

This helper owns managed building selection click routing: pending path-job gating, grid lookup, optional screen-to-cell lookup, and the final cell-selection callback. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSelectionClickSystem` | `BuildingSelectionClickUtilitySystemHelper` | Routes building selection screen-click inputs through delegate-provided grid and cell-selection callbacks without owning ECS lifecycle state. |

## Batch 141 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `88`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch141-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch141-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch141-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=88`.

## Batch 142 - Building Selection Runtime Composition Helper

This helper owns managed building selection runtime composition: UI selection command queue/result processing, selected-building clear/delete helpers, screen-rect and grid-cell selection, selected-hauler order handoff, marker refresh callbacks, HUD selection callbacks, and camera-focus routing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSelectionSystem` | `BuildingSelectionRuntimeCompositionSystemHelper` | Composes managed building selection runtime behavior and UI selection command processing without owning ECS lifecycle state. |

## Batch 142 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `87`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeBuildingSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch142-runtime-building-selection.log`: passed with `[RuntimeBuildingSystemFocusedValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch142-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch142-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=87`.

## Batch 143 - Building Spawn Cell Utility Helper

This helper owns stateless building spawn-cell perimeter search and reservation helpers over grid/native-array occupancy data. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSpawnCellSystem` | `BuildingSpawnCellUtilitySystemHelper` | Finds and reserves adjacent building spawn cells using grid/native-array data without owning ECS lifecycle state. |

## Batch 143 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `86`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch143-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch143-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch143-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=86`.

## Batch 144 - Building Spawn Composition Helper

This helper owns managed building spawn composition and production spawn routing: produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad fallback, and spawned ECS unit initialization. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSpawnSystem` | `BuildingSpawnCompositionSystemHelper` | Coordinates building production spawn placement and ECS unit initialization through explicit runtime contexts without owning an ECS lifecycle. |

## Batch 144 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `85`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunProductionRequestValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch144-building-production-request.log`: passed with `[BuildingProductionRequestValidation] result=Passed tests=21`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch144-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch144-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=85`.

## Batch 145 - Building Surface Placement Utility Helper

This helper owns stateless building footprint map-surface sampling and placement-surface result projection. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSurfacePlacementSystem` | `BuildingSurfacePlacementUtilitySystemHelper` | Evaluates building footprint height/slope and projects placement surface data without owning ECS lifecycle state. |

## Batch 145 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `84`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MapSurfaceLayeredGridFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch145-map-surface.log`: passed with `[MapSurfaceLayeredGridFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch145-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=84`.

## Batch 146 - Road Build Building Placement Composition Helper

This helper owns managed road/build building-placement composition: drag state, preview instance creation and cancellation, footprint positioning, validity checks, and placement visual callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildBuildingPlacementSystem` | `RoadBuildBuildingPlacementCompositionSystemHelper` | Coordinates managed road/build placement preview composition and validation through explicit road-build contexts without owning an ECS lifecycle. |

## Batch 146 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `83`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch146-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch146-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=83`.

## Batch 147 - Road Build Command Composition Helper

This helper owns road-build command request/result queue helpers and managed road session command dispatch. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildCommandSystem` | `RoadBuildCommandCompositionSystemHelper` | Coordinates road-build command request/result buffers with the managed session context without owning an ECS lifecycle. |

## Batch 147 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `82`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch147-road-build-command-rerun.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch147-architecture-rerun.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=82` after stale road-command transition allowlist entries were removed.

## Batch 148 - Road Build Composition Context Helper

This helper owns road-build context factory composition across footprint, runtime generation, read-model, interaction, input, command, delete prompt, disposal, ECS, visual, mutation, and placement contexts. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildCompositionContextSystem` | `RoadBuildCompositionContextCompositionSystemHelper` | Creates road-build composition contexts for managed road/city boundaries without owning an ECS lifecycle. |

## Batch 148 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `81`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch148-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch148-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=81`.

## Batch 149 - Road Build Composition Lifecycle Helper

This helper owns road-build startup, dependency binding, disposal, and no-EntityManager exit-build-mode fallback sequencing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildCompositionLifecycleSystem` | `RoadBuildCompositionLifecycleCompositionSystemHelper` | Coordinates managed road-build lifecycle sequencing without owning an ECS lifecycle. |

## Batch 149 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `80`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch149-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch149-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=80`.

## Batch 150 - Road Build Composition Helper

This helper owns the road-build composition entry point used by managed gameplay startup, returning runtime update, GUI, disposal, generation, and footprint wiring delegates. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildCompositionSystem` | `RoadBuildCompositionSystemHelper` | Wires managed road-build composition startup and dependency binding without owning an ECS lifecycle. |

## Batch 150 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `79`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch150-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch150-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=79`.

## Batch 151 - Road Build Runtime Action Helper

This helper owns road-build runtime action state and routes command queue processing, input update, and delete-prompt GUI handling from the road composition boundary. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildRuntimeActionSystem` | `RoadBuildRuntimeActionCompositionSystemHelper` | Coordinates road-build runtime action dispatch without owning an ECS lifecycle. |

## Batch 151 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `78`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch151-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch151-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=78`.

## Batch 152 - Road Build ECS Boundary Helper

This helper owns the road-build managed ECS boundary for entity-manager access, blocker/combat entity creation, runtime link attachment, player unit spawning, and runtime building disposal. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildEcsBoundarySystem` | `RoadBuildEcsBoundaryCompositionSystemHelper` | Bridges managed road-build composition to ECS entity operations without owning an ECS lifecycle. |

## Batch 152 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `77`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch152-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch152-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=77`.

## Batch 153 - Road Preview Presentation Helper

This helper owns managed road preview presentation, including preview object pooling, material alpha copies, path preview rebuild, clear/update, and disposal. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadPreviewSystem` | `RoadPreviewPresentationSystemHelper` | Applies managed road preview presentation without owning an ECS lifecycle. |

## Batch 153 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `76`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch153-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch153-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=76`.

## Batch 154 - Citizen Population Composition Helper

This helper owns citizen population composition wiring, including child helper creation, initialization, visible-citizen cleanup, read-model refresh, event binding, disposal, and building composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationCompositionSystem` | `CitizenPopulationCompositionSystemHelper` | Coordinates citizen population composition without owning an ECS lifecycle. |

## Batch 154 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `75`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch154-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch154-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=75`.

## Batch 155 - Citizen Visible-Unit Presentation Helper

This helper owns visible citizen presentation bridging, including spawn/despawn visibility, same-frame entity instantiation/setup, movement-command enqueueing, visible record mutation, removal, clear, and arrival checks. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenVisibleUnitSystem` | `CitizenVisibleUnitPresentationSystemHelper` | Applies managed visible-citizen presentation bridging without owning an ECS lifecycle. |

## Batch 155 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `74`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch155-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch155-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=74`.

## Batch 156 - Road Build Input Composition Helper

This helper owns managed road-build pointer input composition, including build/delete gesture state, preview callbacks, building-placement drag handoff, and road composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildInputSystem` | `RoadBuildInputCompositionSystemHelper` | Coordinates managed road-build input composition without owning an ECS lifecycle. |

## Batch 156 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `73`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch156-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch156-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=73`.

## Batch 157 - Road Build Interaction Composition Helper

This helper owns managed road-build interaction composition for building placement commits, selection hit tests, building selection, deletion, ECS entity cleanup, and storage callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildInteractionSystem` | `RoadBuildInteractionCompositionSystemHelper` | Coordinates managed road-build interaction composition without owning an ECS lifecycle. |

## Batch 157 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `72`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch157-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch157-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=72`.

## Batch 158 - Road Build Placement Storage Composition Helper

This helper owns managed road-build placement storage, including runtime building collection state, active placement storage, building id allocation, selected-building state, and road composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildPlacementStorageSystem` | `RoadBuildPlacementStorageCompositionSystemHelper` | Coordinates managed road-build placement storage without owning an ECS lifecycle. |

## Batch 158 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `71`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch158-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch158-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=71`.

## Batch 159 - Road Build Read-Model Composition Helper

This helper owns managed road-build read-model composition for active road mode, dragging interaction, pending placement, selected building, placement labels, and camera/selection consumers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildReadModelSystem` | `RoadBuildReadModelCompositionSystemHelper` | Coordinates managed road-build read-model composition without owning an ECS lifecycle. |

## Batch 159 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `70`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch159-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch159-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=70`.

## Batch 160 - Road Build Session Composition Helper

This helper owns managed road-build session composition, including build tool state, road build snapshots, delete-prompt state, command-mode callbacks, and exit-build-mode flow. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildSessionSystem` | `RoadBuildSessionCompositionSystemHelper` | Coordinates managed road-build session composition without owning an ECS lifecycle. |

## Batch 160 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `69`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch160-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch160-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=69`.

## Batch 161 - Road Build Dependency Composition Helper

This helper owns managed road-build dependency composition, including building-interaction binding, command-mode calls, minimap UI configuration, runtime blocker dependency state, and road composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildDependencySystem` | `RoadBuildDependencyCompositionSystemHelper` | Coordinates managed road-build dependency composition without owning an ECS lifecycle. |

## Batch 161 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `68`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch161-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch161-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=68`.

## Batch 162 - Road Build Disposal Composition Helper

This helper owns managed road-build disposal composition, including runtime root cleanup, placement visual/cache cleanup, ECS boundary cleanup, minimap event cleanup, road tile clearing, and road composition lifecycle callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildDisposalSystem` | `RoadBuildDisposalCompositionSystemHelper` | Coordinates managed road-build disposal composition without owning an ECS lifecycle. |

## Batch 162 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `67`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch162-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch162-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=67`.

## Batch 163 - Road Build Mutation Composition Helper

This helper owns managed road-build mutation composition, including stroke creation/deletion, session snapshot capture/restore, dirty-cell refresh, and road composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildMutationSystem` | `RoadBuildMutationCompositionSystemHelper` | Coordinates managed road-build mutation composition without owning an ECS lifecycle. |

## Batch 163 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `66`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch163-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch163-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=66`.

## Batch 164 - Road Delete Prompt UI Helper

This helper owns the managed IMGUI delete-road modal, including prompt rendering, delete/cancel actions, session prompt state, and road-build runtime GUI callers. It remains a plain UI helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadDeletePromptSystem` | `RoadDeletePromptUiSystemHelper` | Renders the managed road-delete IMGUI prompt without owning an ECS lifecycle. |

## Batch 164 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `65`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch164-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch164-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=65`.

## Batch 165 - Road Minimap Event UI Helper

This helper owns managed road-minimap UI invalidation, including `IMatchRuntimeUi` binding, static minimap change notification, clear/flush state, and road composition callers. It remains a plain UI helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadMinimapEventSystem` | `RoadMinimapEventUiSystemHelper` | Coordinates managed road-minimap UI invalidation without owning an ECS lifecycle. |

## Batch 165 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `64`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch165-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch165-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=64`.

## Batch 166 - Road Network Composition Helper

This helper owns managed road-network graph composition state, including stroke graph dictionaries, road tile data, autobahn metadata, snapshot/restore behavior, and road composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadNetworkSystem` | `RoadNetworkCompositionSystemHelper` | Owns managed road-network graph state without owning an ECS lifecycle. |

## Batch 166 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `63`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch166-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch166-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=63`.

## Batch 167 - Road Path Planning Utility Helper

This helper owns stateless road-path planning utility logic, including drag-axis resolution, L-shaped path construction, preview dirty-cell/proposed-edge planning, endpoint preview expansion, and preview mask construction. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadPathPlanningSystem` | `RoadPathPlanningUtilitySystemHelper` | Performs stateless road-path planning utility work without owning an ECS lifecycle. |

## Batch 167 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `62`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch167-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch167-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=62`.

## Batch 168 - Road Runtime Generation Context Composition Helper

This helper owns managed road runtime-generation context assembly, including road-cell-size callbacks, deferred ECS sync begin/end callbacks, stroke creation delegates, and special visual handoff. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadRuntimeGenerationContextSystem` | `RoadRuntimeGenerationContextCompositionSystemHelper` | Coordinates managed runtime road-generation context without owning an ECS lifecycle. |

## Batch 168 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `61`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch168-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch168-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=61`.

## Batch 169 - Road Runtime Generation Composition Helper

This helper owns managed runtime-city road generation composition, including road-cell-size queries, deferred ECS sync callbacks, runtime road stroke creation, special visual bridge calls, and runtime-city composition callers. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadRuntimeGenerationSystem` | `RoadRuntimeGenerationCompositionSystemHelper` | Coordinates managed runtime road generation without owning an ECS lifecycle. |

## Batch 169 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `60`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch169-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch169-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=60`.

## Batch 170 - Road Surface Placement Utility Helper

This helper owns road surface placement validation over baked map surface data, including path surface checks, primary sample evaluation, movement-mask checks, and road surface type resolution. It remains a plain utility helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadSurfacePlacementSystem` | `RoadSurfacePlacementUtilitySystemHelper` | Validates road placement against map surface data without owning an ECS lifecycle. |

## Batch 170 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `59`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MapSurfaceLayeredGridFocusedTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch170-map-surface.log`: passed with `[MapSurfaceLayeredGridFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch170-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch170-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=59`.

## Batch 171 - Road Runtime Root Scene Helper

This helper owns managed Unity scene hierarchy root creation and disposal for runtime roads, autobahns, connector roots, debug-straight roads, and runtime buildings. It remains a plain scene helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadRuntimeRootSystem` | `RoadRuntimeRootSceneSystemHelper` | Creates and disposes Unity scene roots for managed road runtime presentation boundaries. |

## Batch 171 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `58`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch171-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch171-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=58`.

## Batch 172 - Road Build Visual Context Presentation Helper

This helper owns managed road visual context construction for chunk, preview, special-road, and prefab visual boundaries. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildVisualContextSystem` | `RoadBuildVisualContextPresentationSystemHelper` | Builds managed road visual contexts and prefab lookup boundaries without owning an ECS lifecycle. |

## Batch 172 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `57`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch172-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch172-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=57`.

## Batch 173 - Road Visual Refresh Presentation Helper

This helper owns managed road visual refresh orchestration for road tiles, dirty chunks, ECS road sync requests, and special-road visual rebuilds. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadVisualRefreshSystem` | `RoadVisualRefreshPresentationSystemHelper` | Refreshes road visual presentation and road ECS sync boundaries without owning an ECS lifecycle. |

## Batch 173 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `56`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch173-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch173-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=56`.

## Batch 174 - RTS Selection Runtime Camera Helper

This helper owns managed runtime camera workflow coordination for selection startup, fullscreen/normal iso mode requests, drag/pan/zoom routing, initial focus consumption, and match-intro camera transitions. It remains a plain camera helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionRuntimeCameraSystem` | `RtsSelectionRuntimeCameraSystemHelper` | Coordinates managed RTS camera request flow and explicit `Camera` boundaries without owning an ECS lifecycle. |

## Batch 174 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `55`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsCameraSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch174-rts-camera.log`: passed with `[RtsCameraFocusedValidation] result=Passed tests=11`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch174-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=55`.

## Batch 175 - Selection UI Camera Helper

This helper owns managed selection/UI camera control requests, direct `Camera` zoom/framing calls, and RTS camera request forwarding. It remains a plain camera helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionUiCameraSystem` | `SelectionUiCameraSystemHelper` | Coordinates UI camera controls and explicit `Camera` request boundaries without owning an ECS lifecycle. |

## Batch 175 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `54`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsCameraSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch175-rts-camera.log`: passed with `[RtsCameraFocusedValidation] result=Passed tests=11`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch175-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=54`.

## Batch 176 - Building Selection Marker Presentation Helper

This helper owns managed building selection marker GameObject presentation, marker prefab instantiation, renderer property-block application, premium boundary view setup, object-outline setup, hide, and disposal. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingSelectionMarkerSystem` | `BuildingSelectionMarkerPresentationSystemHelper` | Applies building selection marker visuals through managed GameObject, renderer, material, and outline boundaries without owning an ECS lifecycle. |

## Batch 176 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `53`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingSelectionMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch176-building-selection-marker.log`: passed with `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch176-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=53`.

## Batch 177 - Selection Order Marker Presentation Helper

This helper owns managed move, attack, scan, attack-target, attack-preview, and board-preview marker GameObject presentation, marker prefab ownership, renderer property-block application, query-backed preview marker display, visibility expiry, and disposal. It remains a plain presentation helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionOrderMarkerSystem` | `SelectionOrderMarkerPresentationSystemHelper` | Applies selection order marker visuals through managed GameObject, renderer, line-renderer, material, and preview-marker boundaries without owning an ECS lifecycle. |

## Batch 177 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `52`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch177-selection-order-marker.log`: passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch177-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=52`.

## Batch 178 - RTS Selection Command-Result Flush Composition Helper

This helper owns managed command-result flush composition across ECS command-result buffers, HUD feedback callbacks, command-mode cleanup, marker presentation forwarding, selected-building fallback cleanup, and command-family request/result processors. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionCommandResultFlushSystem` | `RtsSelectionCommandResultFlushCompositionSystemHelper` | Coordinates command-result flush side effects across ECS result buffers, HUD callbacks, selection cleanup, and marker presentation without owning an ECS lifecycle. |

## Batch 178 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `51`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch178-selection-command-result.log`: reached the documented pre-existing `UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea` assertion and logged `[SelectionCommandRequestResultContractValidation] result=Failed`; the failure does not reference the renamed helper.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch178-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch178-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=51`.

## Batch 179 - RTS Selection Focus Command Composition Helper

This helper owns managed focus, select-all, deselect, external selection command branching, HUD selection callbacks, camera-drag cleanup, and focus command context coordination. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionFocusCommandSystem` | `RtsSelectionFocusCommandCompositionSystemHelper` | Coordinates focus/select-all command side effects across ECS state, HUD callbacks, camera-aware rectangle requests, and selection cleanup without owning an ECS lifecycle. |

## Batch 179 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `50`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch179-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch179-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=50`.

## Batch 180 - RTS Selection Input State Composition Helper

This helper owns default-world input-state singleton resolution, ECS request-buffer creation, cached state-entity reuse, pointer request enqueueing, and command request enqueueing. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionInputStateSystem` | `RtsSelectionInputStateCompositionSystemHelper` | Coordinates selection input state and request buffers across ECS state and manually constructed input/UI boundary helpers without owning an ECS lifecycle. |

## Batch 180 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `49`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch180-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch180-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=49`.

## Batch 181 - RTS Selection Input Composition Helper

This helper owns command-mode state, queued move-order state, selection drag state, pointer request enqueueing, command-intent request enqueueing, transport/scan/selection-rectangle request helpers, and compatibility accessors for selection UI/runtime boundaries. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionInputSystem` | `RtsSelectionInputCompositionSystemHelper` | Coordinates selection input state and command-intent request composition across ECS request buffers and manually constructed input/UI boundary helpers without owning an ECS lifecycle. |

## Batch 181 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `48`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch181-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch181-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=48`.

## Batch 182 - RTS Selection Pointer Target Command Composition Helper

This helper owns pointer-to-unit/cell resolution, camera-aware target lookup, resolved move/attack/scan/board request composition, selected-transport board orchestration, map-surface command target resolution, and click diagnostics. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionPointerTargetCommandSystem` | `RtsSelectionPointerTargetCommandCompositionSystemHelper` | Coordinates pointer target command requests and managed camera/map-surface target resolution across ECS request buffers and selection startup boundaries without owning an ECS lifecycle. |

## Batch 182 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `47`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch182-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch182-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=47`.

## Batch 183 - RTS Selection Runtime Input Composition Helper

This helper owns queued move-order consumption, normal pointer press/hold/release orchestration, UI click suppression, selection-hold triggering, live rectangle diffing, selection rectangle request queueing, and command-mode target dispatch. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RtsSelectionRuntimeInputSystem` | `RtsSelectionRuntimeInputCompositionSystemHelper` | Coordinates runtime pointer input and command-mode request composition across Unity pointer/time boundaries and ECS request buffers without owning an ECS lifecycle. |

## Batch 183 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `46`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch183-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch183-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=46`.

## Batch 184 - Building UI Composition Helper

This helper owns building UI source, command context, query context, live-unit preview prefab fallback, and placement command fallback wiring. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingUiCompositionSystem` | `BuildingUiCompositionSystemHelper` | Composes building UI command/query contexts and managed preview/presentation fallbacks without owning an ECS lifecycle. |

## Batch 184 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `45`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingUiQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch184-building-ui-query.log`: passed with `[BuildingUiQueryValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch184-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch184-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=45`.

## Batch 185 - Building UI Context Composition Helper

This helper owns building UI source packaging plus command/query context creation for production requests, placement commands, and building UI read models. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingUiContextSystem` | `BuildingUiContextCompositionSystemHelper` | Composes building UI command/query context data and production request callbacks without owning an ECS lifecycle. |

## Batch 185 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `44`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingUiQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch185-building-ui-query.log`: passed with `[BuildingUiQueryValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch185-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch185-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=44`.

## Batch 186 - Building UI Query Helper

This helper owns selected-building UI read models, produced-unit UI entries, pending-production UI entries, owner-faction filtering, visible-selectable checks, and managed preview prefab fallback reads. It remains a plain UI helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `BuildingUiQuerySystem` | `BuildingUiQueryUiSystemHelper` | Provides building UI query/read-model data and preview fallback reads without owning an ECS lifecycle. |

## Batch 186 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `43`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingUiQuerySystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch186-building-ui-query.log`: passed with `[BuildingUiQueryValidation] result=Passed tests=5`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch186-building-gameplay-smoke.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch186-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=43`.

## Batch 187 - Citizen Building Read Composition Helper

This Agent E helper owns the citizen population runtime-building read cache and role-specific building id lists used by citizen lifecycle, travel, household, refugee, danger, event, and visible-unit composition. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenBuildingReadSystem` | `CitizenBuildingReadCompositionSystemHelper` | Provides citizen building read/cache composition against building runtime read models without owning an ECS lifecycle. |

## Batch 187 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `42`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch187-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch187-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=42`.

## Batch 188 - Citizen Danger Composition Helper

This Agent E helper owns managed danger source registration, periodic Transform-position sampling, and citizen flee-target composition against the citizen building read cache. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenDangerSystem` | `CitizenDangerCompositionSystemHelper` | Provides citizen danger-source and flee-target composition with managed Transform references without owning an ECS lifecycle. |

## Batch 188 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `41`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch188-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch188-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=41`.

## Batch 189 - Citizen Household Registration Composition Helper

This Agent E helper owns citizen household assignment, removed-house displacement, rehousing, and household refugee/member counting against managed citizen state and building-read data. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenHouseholdRegistrationSystem` | `CitizenHouseholdRegistrationCompositionSystemHelper` | Provides citizen household registration and rehousing composition without owning an ECS lifecycle. |

## Batch 189 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `40`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed with one transient Unity `UnityEditor.UIElementsSamplesModule.dll` copy retry warning and no errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch189-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch189-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=40`.

## Batch 190 - Citizen Population ECS Projection Composition Helper

This Agent E helper owns citizen population EntityManager/query projection, summary publication, citizen/household entity creation, and grid-config reads for managed citizen composition. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationEcsProjectionSystem` | `CitizenPopulationEcsProjectionCompositionSystemHelper` | Provides citizen ECS projection composition without owning an ECS lifecycle. |

## Batch 190 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `39`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch190-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch190-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=39`.

## Batch 191 - Citizen Population Event Composition Helper

This Agent E helper owns citizen population event forwarding for destroyed visible citizens and destroyed home buildings, plus managed delegate handoff into citizen refugee/travel composition. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationEventSystem` | `CitizenPopulationEventCompositionSystemHelper` | Provides citizen population event composition without owning an ECS lifecycle. |

## Batch 191 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `38`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch191-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch191-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=38`.

## Batch 192 - Citizen Population Lifecycle Composition Helper

This Agent E helper owns citizen population lifecycle timing, path-job skip handling, danger refresh, visible sync cadence, and totals refresh callbacks. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationLifecycleSystem` | `CitizenPopulationLifecycleCompositionSystemHelper` | Provides citizen population lifecycle composition without owning an ECS lifecycle. |

## Batch 192 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `37`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch192-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch192-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=37`.

## Batch 193 - Citizen Population Read-Model Composition Helper

This Agent E helper owns citizen totals read-model state, refresh/reset API, ECS summary fallback reads, and managed read-model publication. It remains a plain composition helper and does not introduce an ECS lifecycle.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationReadModelSystem` | `CitizenPopulationReadModelCompositionSystemHelper` | Provides citizen population read-model composition without owning an ECS lifecycle. |

## Batch 193 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `36`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch193-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch193-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=36`.

## Batch 194 - Citizen Population Runtime Update Composition Helper

`CitizenPopulationRuntimeUpdateSystem` owned citizen population runtime update composition and did not expose an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping bind/reset, logical citizen updates, visible sync, and store/death callback behavior unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationRuntimeUpdateSystem` | `CitizenPopulationRuntimeUpdateCompositionSystemHelper` | Provides citizen population runtime update composition without owning an ECS lifecycle. |

## Batch 194 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `35`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch194-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch194-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=35`.

## Batch 195 - Citizen Population State Composition Helper

`CitizenPopulationStateSystem` owned managed citizen population dictionaries, scratch lists, and ID allocation without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping citizen population state access unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationStateSystem` | `CitizenPopulationStateCompositionSystemHelper` | Holds citizen population state for composition-owned citizen workflows without owning an ECS lifecycle. |

## Batch 195 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `34`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch195-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch195-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=34`.

## Batch 196 - Citizen Population Totals Composition Helper

`CitizenPopulationTotalsSystem` owned citizen population totals calculation and citizen/household data checks without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping totals read-model refresh callers unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenPopulationTotalsSystem` | `CitizenPopulationTotalsCompositionSystemHelper` | Provides citizen population totals calculation for composition-owned citizen workflows without owning an ECS lifecycle. |

## Batch 196 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `33`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch196-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch196-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=33`.

## Batch 197 - Citizen Refugee Composition Helper

`CitizenRefugeeSystem` owned managed citizen refugee displacement, tent assignment, upkeep policy, and delegate contracts without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping citizen population callers unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenRefugeeSystem` | `CitizenRefugeeCompositionSystemHelper` | Provides citizen refugee workflow composition without owning an ECS lifecycle. |

## Batch 197 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `32`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch197-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch197-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=32`.

## Batch 198 - Citizen Resource Composition Helper

`CitizenResourceSystem` owned citizen resource context delegates, configuration checks, and dollar spend clamping without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping citizen refugee/resource callers unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenResourceSystem` | `CitizenResourceCompositionSystemHelper` | Provides citizen resource spend/context composition without owning an ECS lifecycle. |

## Batch 198 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `31`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch198-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch198-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=31`.

## Batch 199 - Citizen Schedule Composition Helper

`CitizenScheduleSystem` owned weekday/weekend/refugee status policy, schedule phase calculation, target-building selection, and shopping cadence without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping citizen population runtime callers unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenScheduleSystem` | `CitizenScheduleCompositionSystemHelper` | Provides citizen schedule policy composition without owning an ECS lifecycle. |

## Batch 199 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `30`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch199-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch199-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=30`.

## Batch 200 - Citizen Status Transition Composition Helper

`CitizenStatusTransitionSystem` owned citizen status mutation, travel-status detection, desired-status travel mapping, arrival settling, and death/debug status transitions without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping citizen population, travel, visible-unit, and test callers unchanged.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CitizenStatusTransitionSystem` | `CitizenStatusTransitionCompositionSystemHelper` | Provides citizen status-transition policy composition without owning an ECS lifecycle. |

## Batch 200 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `29`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CitizenVisibleUnitPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch200-citizen-visible.log`: passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch200-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=29`.

## Batch 201 - Faction Resource Composition Helper

`FactionResourceSystem` owns resource economy snapshots, resource drains, storage capacity calculations, and oil/fuel production policy without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and updating building/resource call sites and focused tests.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `FactionResourceSystem` | `FactionResourceCompositionSystemHelper` | Provides faction resource economy and production policy composition without owning an ECS lifecycle. |

## Batch 201 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `28`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FactionResourceCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch201-faction-resource.log`: passed with `[FactionResourceFocusedValidation] result=Passed tests=6`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch201-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=28`.

## Batch 202 - Custom Game Startup Helper

`CustomGameStartupSystem` owns custom-game startup/config projection through a direct helper constructed by the match bootstrap composition boundary. This batch renamed it to an approved startup-helper suffix while preserving the Unity `.meta` GUID and updating bootstrap, focused tests, and architecture guardrails.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `CustomGameStartupSystem` | `CustomGameStartupSystemHelper` | Provides custom-game startup projection without owning an ECS lifecycle. |

## Batch 202 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `27`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CustomGameStartupSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch202-custom-game-startup.log`: passed with `[CustomGameStartupFocusedValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch202-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=27`.

## Batch 203 - Focusable Unit Lookup Camera Helper

`FocusableUnitLookupSystem` owns focusable-unit query caching and camera-backed screen-distance hit testing without an ECS lifecycle. This batch renamed it to an approved camera-helper suffix while preserving the Unity `.meta` GUID and updating selection startup, pointer-target, building-interaction, focused tests, and architecture validation references.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `FocusableUnitLookupSystem` | `FocusableUnitLookupCameraSystemHelper` | Provides focusable-unit lookup and camera-backed hit testing without owning an ECS lifecycle. |

## Batch 203 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `26`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FocusableUnitLookupCameraSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch203-focusable-unit-lookup.log`: passed with `[FocusableUnitLookupFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch203-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=26`.

## Batch 204 - Focused Unit Lifecycle Composition Helper

`FocusedUnitLifecycleSystem` owns selected/focused entity lifecycle composition, selected-tag synchronization, focus assignment, and HUD callback orchestration without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and updating selection/focus call sites and tests.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `FocusedUnitLifecycleSystem` | `FocusedUnitLifecycleCompositionSystemHelper` | Provides selected/focused entity lifecycle composition without owning an ECS lifecycle. |

## Batch 204 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `25`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionStateCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch204-selection-state.log`: passed with `[SelectionStateFocusedValidation] result=Passed tests=8`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch204-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=25`.

## Batch 205 - Focused Unit UI Read-Model Helper

`FocusedUnitUiReadModelSystem` owns focused-unit UI read-model publication and passenger scratch-list assembly without an ECS lifecycle. This batch renamed it to an approved UI-helper suffix while preserving the Unity `.meta` GUID and updating selection UI/HUD and transport validation call sites.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `FocusedUnitUiReadModelSystem` | `FocusedUnitUiReadModelUiSystemHelper` | Provides focused-unit UI read-model publishing without owning an ECS lifecycle. |

## Batch 205 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `24`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch205-unit-transport.log`: passed with `[UnitTransportValidation] result=Passed tests=73`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch205-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=24`.

## Batch 206 - Gameplay Runtime Update Composition Helper

`GameplayRuntimeUpdateSystem` owns managed runtime update composition, loading-gate diagnostics, and direct `Update`/`LateUpdate`/`OnGui` delegate sequencing without an ECS lifecycle. This batch renamed it to an approved composition-helper suffix while preserving the Unity `.meta` GUID and updating the dedicated editor validation runner.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `GameplayRuntimeUpdateSystem` | `GameplayRuntimeUpdateCompositionSystemHelper` | Composes managed runtime update delegates without owning an ECS lifecycle. |

## Batch 206 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `23`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod GameplayRuntimeUpdateValidationRunner.Run -logFile /private/tmp/warline-non-ecs-helper-naming-batch206-gameplay-runtime-update.log`: passed with `[GameplayRuntimeUpdateValidation] result=Passed tests=1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch206-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=23`.

## Batch 207 - Managed Gameplay Startup Helper

`ManagedGameplayStartupSystem` owns managed gameplay startup composition and returns explicit child-system references without an ECS lifecycle. This batch renamed it to an approved startup-helper suffix while preserving the Unity `.meta` GUID and updating the focused validation runner and active architecture contract references.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `ManagedGameplayStartupSystem` | `ManagedGameplayStartupSystemHelper` | Sequences managed gameplay startup/composition without owning an ECS lifecycle. |

## Batch 207 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `22`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ManagedGameplayStartupValidationRunner.Run -logFile /private/tmp/warline-non-ecs-helper-naming-batch207-managed-gameplay-startup.log`: passed with `[ManagedGameplayStartupValidation] result=Passed tests=1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch207-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=22`.

## Batch 208 - Map Building Placement Prefab Helper

`MapBuildingPlacementSpawnSystem` owns authored map-building visual cloning and runtime building registration from baked placement config without an ECS lifecycle. This batch renamed it to an approved prefab-helper suffix while preserving the Unity `.meta` GUID and keeping building composition call sites direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MapBuildingPlacementSpawnSystem` | `MapBuildingPlacementSpawnPrefabSystemHelper` | Clones authored map-building visuals and registers runtime building data from prefab-backed placement config. |

## Batch 208 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `21`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingRuntimeBoundaryValidationTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch208-building-runtime-boundary.log`: passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch208-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=21`.

## Batch 209 - Map Vehicle Placement Prefab Helper

`MapVehiclePlacementSpawnSystem` owns authored vehicle source-key projection, prefab-entity instantiation, placement progress, and blocker-clearance helpers without an ECS lifecycle. This batch renamed it to an approved prefab-helper suffix while preserving the Unity `.meta` GUID and keeping the existing split-decomposition tracker open for future ECS processor extraction.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MapVehiclePlacementSpawnSystem` | `MapVehiclePlacementSpawnPrefabSystemHelper` | Projects authored vehicle source keys and spawns configured prefab entities from map placement config. |

## Batch 209 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `20`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMovementBlockerValidationTests.RunMapVehiclePlacementFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch209-map-vehicle-placement.log`: passed with `[MapVehiclePlacementValidation] result=Passed tests=4`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch209-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=20`.

## Batch 210 - Match HUD Squad Tray Selection UI Helper

`MatchHudSquadTraySelectionSystem` owned the direct HUD squad-tray quick-select bridge: it ranks selectable ECS units using the active camera, mutates selection state through existing selection helpers, and calls HUD selection callbacks without an ECS lifecycle. This batch renamed it to an approved UI-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `MatchHudSquadTraySelectionSystem` | `MatchHudSquadTraySelectionUiSystemHelper` | Keeps the squad-tray HUD/UI selection boundary explicit without claiming ECS scheduling ownership. |

## Batch 210 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `19`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionUiSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch210-match-hud-squad-tray.log`: passed with `[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch210-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=19`.

## Batch 211 - Performance Diagnostics Helper

`PerformanceDiagnosticsSystem` owns manually invoked diagnostics/profiler recorder state for menu/bootstrap composition. It is not scheduled by ECS and remains a diagnostics boundary, so this batch renamed it to the approved diagnostics-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `PerformanceDiagnosticsSystem` | `PerformanceDiagnosticsSystemHelper` | Owns FreezeDetect/FrameRateDiag/PerfDiag formatting and profiler recorder state as a manually invoked diagnostics helper. |

## Batch 211 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `18`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod PerformanceDiagnosticsSystemHelperAllocationTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch211-performance-diagnostics.log`: passed with `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch211-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=18`.

## Batch 212 - Resource Hauler Utility Helper

`ResourceHaulerSystem` owned pure managed hauler domain logic: source/destination classification, haul-order creation, phase/timer mutation, cargo capacity checks, and load/unload transfer mutation. It is not scheduled by ECS and has no Unity object, UI, scene, startup, diagnostics, prefab, VFX, or presentation ownership, so this batch renamed it to the approved utility-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `ResourceHaulerSystem` | `ResourceHaulerUtilitySystemHelper` | Owns stateless resource-hauler order, phase, timer, cargo, and load/unload transfer helper logic. |

## Batch 212 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `17`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ResourceHaulerUtilitySystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch212-resource-hauler.log`: passed with `[ResourceHaulerFocusedValidation] result=Passed tests=9`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch212-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=17`.

## Batch 213 - Road Build Composition Source Helper

`RoadBuildCompositionSourceSystem` owns road-build composition graph source state: child helper instances, state objects, default-world managed-system resolution, and road composition ownership. It is not scheduled by ECS and remains a managed composition boundary, so this batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildCompositionSourceSystem` | `RoadBuildCompositionSourceCompositionSystemHelper` | Owns road-build composition source fields, resolver state, and child helper instance wiring. |

## Batch 213 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `16`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch213-road-build-command-rerun.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`; the first concurrent attempt aborted before tests on transient Unity/Bee missing metadata, then this serialized rerun passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch213-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=16`.

## Batch 214 - Road Build Context Helper

`RoadBuildContextSystem` owns road-build ECS boundary context construction and delegate wiring without an ECS lifecycle. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping existing road composition callers direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildContextSystem` | `RoadBuildContextCompositionSystemHelper` | Creates the road-build ECS boundary context from managed road composition delegates. |

## Batch 214 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `15`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch214-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch214-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=15`.

## Batch 215 - Road Build Interaction Context Helper

`RoadBuildInteractionContextSystem` owns road-build session, input, command, and delete-prompt context construction from explicit managed delegates without an ECS lifecycle. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping existing road runtime action callers direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RoadBuildInteractionContextSystem` | `RoadBuildInteractionContextCompositionSystemHelper` | Creates road-build interaction contexts for session, input, command, and delete-prompt helpers. |

## Batch 215 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `14`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RoadBuildCommandCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch215-road-build-command.log`: passed with `[RoadBuildCommandRequestValidation] result=Passed tests=7`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch215-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=14`.

## Batch 216 - Runtime Grid Bootstrap Startup Helper

`RuntimeGridBootstrapSystem` owns one-shot runtime grid EntityManager bootstrap and ECS grid buffer/component projection without an ECS lifecycle. This batch renamed it to the approved startup-helper suffix while preserving the Unity `.meta` GUID and keeping match bootstrap callers direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeGridBootstrapSystem` | `RuntimeGridBootstrapStartupSystemHelper` | Bootstraps runtime grid config, buffers, blocker storage, occupancy storage, and path pool setup. |

## Batch 216 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `13`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RuntimeGridDeduplicationSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch216-runtime-grid.log`: passed with `[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=3`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch216-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=13`.

## Batch 217 - Runtime Resource Utility Helper

`RuntimeResourceSystem` owns direct runtime dollar/resource state and spend/add context construction without an ECS lifecycle. This batch renamed it to the approved utility-helper suffix while preserving the Unity `.meta` GUID and keeping building composition callers direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeResourceSystem` | `RuntimeResourceUtilitySystemHelper` | Owns runtime dollar/resource state and spend/add context construction for building gameplay. |

## Batch 217 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `12`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionQueueCompositionSystemHelperTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch217-building-gameplay-composition.log`: passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch217-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=12`.

## Batch 218 - Runtime Root Scene Helper

`RuntimeRootSystem` creates managed Unity `GameObject`/`Transform` roots for runtime blockers, city, transports, and UI under the match owner. This batch renamed it to the approved scene-helper suffix while preserving the Unity `.meta` GUID and keeping match bootstrap ownership direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `RuntimeRootSystem` | `RuntimeRootSceneSystemHelper` | Creates Unity scene roots for managed runtime presentation/object boundaries. |

## Batch 218 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `11`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch218-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=11`.

## Batch 219 - Scene Lifecycle Scene Helper

`SceneLifecycleSystem` owns managed Unity scene load/unload operations, active `AsyncOperation` state, and ECS lifecycle queue/result projection without being scheduled by ECS. This batch renamed it to the approved scene-helper suffix while preserving the Unity `.meta` GUID and keeping menu/match scene call sites direct.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SceneLifecycleSystem` | `SceneLifecycleSceneSystemHelper` | Owns managed scene lifecycle operations and ECS queue/result projection. |

## Batch 219 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `10`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SceneLifecycleValidationRunner.Run -logFile /private/tmp/warline-non-ecs-helper-naming-batch219-scene-lifecycle.log`: passed with `[SceneLifecycleValidation] result=Passed tests=1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch219-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=10`.

## Batch 220 - Selected Unit Order Snapshot Composition Helper

`SelectedUnitOrderSnapshotSystem` owns selected-unit order component preservation/restoration state for startup-composed selection flows without being scheduled by ECS. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID and keeping the focused preserve/restore validation entry point intact.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectedUnitOrderSnapshotSystem` | `SelectedUnitOrderSnapshotCompositionSystemHelper` | Owns selected-unit order snapshot/restore state used by selection startup composition. |

## Batch 220 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `9`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch220-selected-order-snapshot.log`: passed with `[SelectedUnitOrderSnapshotFocusedValidation] result=Passed tests=1`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch220-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=9`.

## Batch 221 - Selection Building Interaction Composition Helper

`SelectionBuildingInteractionSystem` owns manually composed building selection, focused-unit cleanup, HUD feedback, transport boarding click checks, and move-order-to-building routing. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID and removing the stale public command-mutator transition guardrail entry.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionBuildingInteractionSystem` | `SelectionBuildingInteractionCompositionSystemHelper` | Composes selection/building interaction helpers and camera-backed target lookup without being scheduled by ECS. |

## Batch 221 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `8`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch221-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch221-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=8`.

## Batch 222 - Selection Gameplay Startup Helper

`SelectionGameplayStartupSystem` is the manually constructed startup boundary that creates selection runtime helpers, builds selection contexts, wires UI callbacks, and exposes the composed runtime update/disposal delegates. This batch renamed it to the approved startup-helper suffix while preserving the Unity `.meta` GUID and updating the pointer-command boundary guardrail path.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionGameplayStartupSystem` | `SelectionGameplayStartupSystemHelper` | Owns selection gameplay startup composition and callback wiring without being scheduled by ECS. |

## Batch 222 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `7`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch222-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch222-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=7`.

## Batch 223 - Selection Rectangle Request Composition Helper

`SelectionRectangleRequestSystem` owns manually composed rectangle request draining, camera-backed visible-unit collection, selected-tag application, selected-move cache refresh, building fallback selection, HUD callbacks, and focus assignment. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID and removing the stale public command-mutator transition guardrail entry.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionRectangleRequestSystem` | `SelectionRectangleRequestCompositionSystemHelper` | Composes rectangle selection request processing and selection side effects without being scheduled by ECS. |

## Batch 223 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `6`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch223-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch223-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=6`.

## Batch 224 - Selection Runtime Config Startup Helper

`SelectionRuntimeConfigSystem` creates the managed selection runtime config state during startup, including camera fallback, marker prefab references, selection thresholds, and camera zoom/pitch/FOV defaults. This batch renamed it to the approved startup-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionRuntimeConfigSystem` | `SelectionRuntimeConfigStartupSystemHelper` | Creates managed selection runtime config state during startup without being scheduled by ECS. |

## Batch 224 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `5`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsCameraSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch224-rts-camera.log`: passed with `[RtsCameraFocusedValidation] result=Passed tests=11`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch224-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch224-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=5`.

## Batch 225 - Selection State Composition Helper

`SelectionStateSystem` owns manually constructed selected/focused state, selected-move cache helpers, cacheability filtering, and lifecycle debug recording. This batch renamed it to the approved composition-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionStateSystem` | `SelectionStateCompositionSystemHelper` | Stores selection/focus composition state without being scheduled by ECS. |

## Batch 225 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `4`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionStateCompositionSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch225-selection-state.log`: passed with `[SelectionStateFocusedValidation] result=Passed tests=8`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch225-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch225-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=4`.

## Batch 226 - Selection UI Command UI Helper

`SelectionUiCommandSystem` owns UI-facing selection command request publication through `ISelectionUiCommand`, Unity frame/screen reads, focused transport disembark request helpers, and gameplay-input lock checks. This batch renamed it to the approved UI-helper suffix while preserving the Unity `.meta` GUID.

| Status | Old type/file | New type/file | Reason |
| --- | --- | --- | --- |
| Complete | `SelectionUiCommandSystem` | `SelectionUiCommandUiSystemHelper` | UI command facade that queues ECS selection command intents without being scheduled by ECS. |

## Batch 226 Validation Log

- `python3 Tools/Architecture/generate_non_ecs_system_inventory.py`: completed; inventory denominator is now `3`.
- `git diff --check`: passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`: passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: passed.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch226-rts-selection-input.log`: passed with `[RtsSelectionInputSystemValidation] result=Passed tests=56`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudCommandControlsCurrentPrefabTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch226-hud-command-controls.log`: passed with `[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=6`.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch226-ui-shell-content.log`: failed before helper binding on pre-existing prefab reference debt, `statusChipSprite must be assigned on the placement bar prefab`; this slice did not touch prefabs or UI Toolkit/Canvas assets.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionCommandRequestResultContractTests.RunBatchValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch226-command-request-result.log`: reached the documented pre-existing `UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea` assertion unrelated to this rename.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-non-ecs-helper-naming-batch226-architecture.log`: passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=3`.

## Open Follow-Up Batches

- Direct ECS-called helper batch.
- UI/camera/scene/startup managed-boundary batch.
- Building/production composition helper batch.
- Road/city/citizen composition helper batch.
- Visual/prefab/render helpers that are not converted to `ISystem`.
- Architecture guardrail ratchet after each batch reduces the transition list.
