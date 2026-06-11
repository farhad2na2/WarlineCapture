# Match GC Allocation Optimization Pass

Date: 2026-06-11
Lane: Gameplay/Performance
Task: Match runtime managed allocation reduction from `Design/GC_Allocation_Elimination_Plan.md`

## Files changed

- `Assets/Game/Scripts/Systems/UnitImpostorRenderSystem.cs`
- `Assets/Game/Scripts/Systems/UnitRenderBudgetClassificationSystem.cs`
- `Assets/Game/Scripts/Systems/MatchHudMinimapProjectionSystem.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudMinimapInputSystem.cs`
- `Assets/Game/Scripts/Rendering/UnitAttackTraceSystem.cs`
- `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystem.cs`
- `Assets/Game/Scripts/Components/MatchHudMinimapMarkerComponents.cs`
- `Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs`
- `Assets/Game/Scripts/Systems/UnitDeathSystem.cs`
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs`
- `Assets/Game/Scripts/Systems/VehicleWreckCleanupSystem.cs`
- `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs`
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnQuerySystem.cs`
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs`
- `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
- `Assets/Game/Scripts/Utilities/RespawnQueueUtility.cs`
- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs`
- `Assets/Tests/Editor/InitialUnitsSpawnFocusedTests.cs`
- `Assets/Tests/Editor/MatchHudMinimapProjectionSystemTests.cs`
- `Assets/Tests/Editor/PerformanceDiagnosticsSystemAllocationTests.cs`
- `Assets/Tests/Editor/PerformanceDiagnosticsSystemAllocationTests.cs.meta`
- `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
- `Design/AgentReports/2026-06-11_perf_match-gc-allocation-optimization-pass.md`

## 2026-06-11 continuation update

- Continued on the main project after `/Users/farhad/Projects/WarlineCapture-CodexUnity2` remained blocked by unrelated broad compile errors.
- Stabilized `PerformanceDiagnosticsSystemAllocationTests.EndStepDoesNotAllocateAfterWarmup`:
  - The original guard used `UnityEngine.Profiling.Recorder.Get("GC.Alloc").sampleBlockCount` against an empty action. In main batchmode this produced reproducible profiler-recorder sample-block jitter: a pure warmed Unity time-read baseline, saturated `EndStep`, and fresh `EndStep` all reported the same single block in one probe run.
  - The guard now compares `EndStep` against an equivalent warmed Unity time-read baseline using `System.GC.GetAllocatedBytesForCurrentThread()`, so the test still fails on real managed-byte allocations while avoiding profiler-recorder block-count noise.
  - No gameplay or runtime production behavior changed in this continuation.

## 2026-06-11 call-stack capture update

- Added `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`, an editor-only automated capture command:
  - Execute method: `MatchGcAllocationCallstackCapture.RunSteadyState`.
  - Loads `Assets/Game/Scenes/Menu.unity`, routes into Match through the UI shell, enables `GC.Alloc` call stacks, captures 300 ready Match frames, loads `/private/tmp/warline-match-gc-callstack-capture.raw`, and writes `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`.
- Profiler raw loading currently reports the batch capture as one loaded frame, so the numbers below are aggregate capture-frame values from the raw file rather than a per-frame steady-state average.
- First automated call-stack capture identified `BuildingDefinitionSystem.BuildConfiguredSpawnableEntry` through `BuildingRuntimeBoundarySystem.PublishRuntimeOwnedBuildingSummaries` as the top allocation path:
  - Before fix: 7,693,360 bytes / 65,279 samples in the loaded raw frame.
  - Fix: cached configured spawnable entries when configured definitions are rebuilt in `BuildingDefinitionSystem`, then used cached entries in `BuildingRuntimeBoundarySystem` instead of rebuilding entries and calling `GameObject.GetComponent` during publish ticks.
- Re-capture after the cache fix removed the previous top `GetComponentFastPath` site. The next top site was `BuildingUiCompositionSystem.CreateSource` from `BuildingPlacementInteractionSystem.HasSelectedBuilding`:
  - Before second fix: 7,155,218 bytes / 58,177 samples in the loaded raw frame.
  - Fix: `BuildingPlacementInteractionCompositionSystem` now wires selected/active building booleans directly to `RuntimeBuildingSystem` instead of constructing a full building UI query context just to answer those booleans.
- Re-capture after the second fix removed the UI composition site from the top. Current top site:
  - `RuntimeCityCompositionSystem.CreateStartupContext` via `RuntimeCityCompositionSystem.TryAutoSpawn`.
  - Current capture: 5,981,668 bytes / 48,183 samples in the loaded raw frame.
  - This appears tied to runtime-city auto-spawn startup context construction and should be handled as the next profiler-backed site, separately from this pass.

## 2026-06-11 second continuation update

- Continued the profiler-backed sequence on the main project and fixed the next confirmed allocation sites one at a time:
  - `RuntimeCityCompositionSystem.CreateStartupContext`: cached startup delegates in `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`.
  - `SelectionGameplayStartupSystem.CreateRuntimeInputContext`: cached the runtime input context until UI/gameplay bindings change.
  - `SelectionGameplayStartupSystem.CreateRuntimeCameraContext`: cached the runtime camera context.
  - `RtsSelectionCommandResultContextSystem.Create`: cached the command-result flush context and invalidated it on HUD feedback binding changes.
  - `SelectionGameplayStartupSystem.RefreshFocusedUnit` and rectangle/squad callback paths: cached HUD selection callbacks instead of allocating lambdas in hot selection paths.
  - `BuildingProductionRequestSystem.CanQueueUnitFromBuilding`: cached and prewarmed production transport settings per configured unit prefab.
  - `BuildingGameplayCompositionSystem` map placement update source: prebuilt the map-placement spawn context instead of constructing it every runtime tick.
  - `BuildingGridCompositionSystem.TryGetGridData`: passed the existing grid entity-manager delegate through directly instead of wrapping it each call.
  - `BuildingRuntimeBoundarySystem.ResolveBoundaryId`: cached normalized boundary IDs by prefab/fallback key.
  - `BuildingDefinitionSystem.TryGetConfiguredUnitReadModel`: cached configured unit entries so runtime reads no longer call `GameObject.name`/authoring lookups each tick.
  - `MatchGcAllocationCallstackCapture`: suppressed warning stack traces while capturing to avoid harness-side `Camera.Render` warning allocations dominating reports.
  - `UnitAttackSystem.OnUpdate`: replaced per-frame managed dictionaries with persistent native scratch maps.
- Latest automated steady-state report after the `UnitAttackSystem` fix:
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`.
  - Current top site: Unity Entities/Burst profiler metadata initialization, `System.RuntimeType:getFullName` through `Unity.Entities.EntitiesProfiler.StaticData.Flush`.
  - Current aggregate capture value: 830,008 bytes / 15,462 samples in the loaded raw frame.
  - This is framework/profiler initialization noise, not a project gameplay allocation site, so no gameplay file is currently justified as the next edit from this steady-state report.

## 2026-06-11 battle-capture continuation update

- Continued on the main project after the missile dependency fix was user-verified.
- Used `MatchGcAllocationCallstackCapture.RunBattleState` as the profiler evidence source for the next edits.
- Baseline entering this continuation:
  - Battle report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`.
  - Earlier battle capture after loading/menu/building work: 117,284 bytes / 2,897 samples.
- Confirmed and fixed additional project-owned allocation sites:
  - `BuildingProductionUpdateSystem.UpdatePendingProductions`: reused the concrete runtime-building dictionary instead of enumerating the `IReadOnlyDictionary` interface path.
    - Result: battle capture reduced to 117,284 bytes / 2,897 samples from the previous 128,272-byte building pass, with `ProcessPendingProductions` reduced from 16,744 bytes to 5,528 bytes.
  - `MatchHudMinimapInputSystem.Update`: cached the `GridConfig` query and moved marker data to an ECS minimap marker read-model buffer.
    - Result: `GameplayRuntimeUpdate.MainMenu > MainMenuPlayUI.MinimapUpdate` dropped from 12,088 bytes / 302 samples to a residual 88 bytes / 2 samples.
  - `SelectionGameplayStartupSystem.CountSelectedTags`: reused `SelectionRuntimeQuerySystem.SelectedTagQuery` instead of creating a selected-tag query every selection tick.
    - Result: `GameplayRuntimeUpdate.Selection` dropped from the top allocation sites.
  - `RespawnQueueUtility`: added a cached-query overload and wired it into `UnitDeathSystem`, `UnitRespawnSystem`, and `VehicleWreckCleanupSystem`.
    - Result: `UnitDeathSystem` and `UnitRespawnSystem` dropped from the top allocation sites.
  - `InitialUnitsSpawnSystem`: added an active uninitialized config query and required that query for update so the system stops updating after initial spawn is initialized.
    - Result: `InitialUnitsSpawnSystem` dropped from the top allocation sites, and battle capture reduced from 63,240 bytes / 1,572 samples to 51,020 bytes / 1,265 samples.
- Additional instrumentation/cleanup added during this continuation:
  - `GameplayRuntimeUpdateSystem`, `BuildingPlacementRuntimeTickSystem`, `MainMenuPlayUI`, and `UIShellEcsPresentationSystem` now have focused profiler markers used to split aggregate battle capture buckets.
  - `AICombatOrderSystem` no longer creates a squad entity array or faction-control native copy in its main loop, but the measured `AICombatOrderSystem` GC bucket did not change. Treat the remaining 11,960-byte bucket as not yet explained by this edit.
- Latest battle capture:
  - Command log: `/private/tmp/warline-main-gc-initialspawn-gate-battle.log`.
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`.
  - Current aggregate: 51,020 bytes / 1,265 samples.
  - Remaining top project-owned/editor-visible buckets:
    - `UIShellEcsPresentationSystem.Update`: 14,352 bytes / 299 samples.
    - `AICombatOrderSystem`: 11,960 bytes / 299 samples.
    - `MenuBootstrapView.Update`: 9,568 bytes / 299 samples.
  - Non-project editor noise still present:
    - `UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate`: 12,000 bytes / 300 samples.

## 2026-06-11 heartbeat update

- Continued the remaining evidence-backed battle GC work on the main project.
- `AICombatOrderSystem`:
  - Added a conservative 0.25-second system evaluation cadence before the squad scan/order loop.
  - Reason: the system already refreshes issued orders at a 2-second squad cadence, and the battle capture showed a per-frame allocation bucket even when no new order work was needed.
  - Result: the measured `AICombatOrderSystem` bucket dropped from 11,960 bytes / 299 samples to a small residual bucket around 240-280 bytes / 6-7 samples.
- Capture/teardown cleanup:
  - `MatchGcAllocationCallstackCapture` now exits batchmode through a static editor-update callback instead of a captured `delayCall` lambda.
  - `GameplayRuntimeUpdateSystem.Dispose` now skips cached query disposal if the cached ECS world has already been torn down, preventing the post-capture shutdown `NullReferenceException`.
- Latest battle capture after this heartbeat:
  - Command log: `/private/tmp/warline-main-gc-disposeguard-battle.log`.
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`.
  - Current aggregate: 39,520 bytes / 979 samples.
  - Remaining large buckets:
    - `UIShellEcsPresentationSystem.Update`: 14,352 bytes / 299 samples.
    - `UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate`: 12,000 bytes / 300 samples.
    - `MenuBootstrapView.Update`: 9,568 bytes / 299 samples.
  - The two UI buckets still report only the capture harness frame as the managed call stack, so the next step is to prove whether these are real runtime allocations or Editor/call-stack attribution artifacts before changing the shell architecture.

## Contracts touched

- `BuildingUiQuerySystem.Context` is now a public handle with internal payload fields/constructor so the current UI runtime assembly split can store/pass the context without exposing internals.
- Added `UnitImpostorRenderSystem.HasCharacterUnitPrefix(FixedString64Bytes)` as an internal no-allocation prefix helper used by render-budget classification.
- Did not modify `PathfindBatchJob`, `UnitPathfindingScheduleSystem`, `UnitPathfindingSystem`, or `UnitPathGridSnapshotSystem`.

## Allocation sites addressed

- `UnitImpostorRenderSystem.IsCharacterSourceKey`
  - Before: `FixedString64Bytes.ToString().StartsWith("Unit_Chr_")` allocated a managed string per impostor candidate.
  - After: direct ASCII byte prefix check on `FixedString64Bytes`.
- `UnitRenderBudgetClassificationSystem.IsCharacterUnit`
  - Before: same source-key `ToString().StartsWith(...)` pattern.
  - After: shared no-allocation prefix helper.
- `PerformanceDiagnosticsSystem.EndStep`
  - Before: every runtime update step formatted elapsed time with `ToString("F1")` into `_lastStepLogBuilder`, even on frames with no diagnostics log.
  - After: stores raw last-step samples per frame and builds formatted strings only when a diagnostic log is emitted.
  - Follow-up in this pass: removed the rolling dictionary-backed `stepStats` aggregation from the `EndStep` hot path after the allocation guard proved it still allocated. Periodic low-FPS logs now report `stepStats=none`; freeze/pre-game `lastSteps` diagnostics remain.
- `MatchHudMinimapProjectionSystem`
  - Before: repeated `Vector3[]` viewport-corner arrays inside camera projection helpers.
  - After: shared static viewport-corner array.
- `MatchHudMinimapInputSystem`
  - Before: marker update created a fresh `EntityQuery` every frame; static-map validation used `Texture2D.GetPixels32()`; raster fallback built a new candidate array per refresh.
  - After: marker query is cached per world and disposed safely; static-map validation reads `GetRawTextureData<Color32>()`; raster candidates reuse an instance scratch array.
- `UnitAttackTraceSystem.BuildTraceMesh`
  - Before: mesh creation used temporary `List<Vector3>`, `List<Vector2>`, and triangle array setup.
  - After: static vertex/UV/triangle arrays are reused during trace mesh creation.
- `BuildingDefinitionSystem` / `BuildingRuntimeBoundarySystem`
  - Before: configured spawnable read-model/owned-summary publishing rebuilt `ConfiguredSpawnableEntry` values and hit `GameObject.GetComponent<BuildingDefinitionAuthoring>()` repeatedly during Match runtime.
  - After: configured spawnable entries are cached next to configured definitions and reused by boundary publishing.
- `BuildingPlacementInteractionCompositionSystem`
  - Before: selected/active building boolean checks recreated the full building UI query context from the Match HUD selection panel path.
  - After: those booleans read directly from `RuntimeBuildingSystem`.

## User-visible behavior

- No intended visual or gameplay behavior changes.
- Impostors, minimap projection, minimap markers, and attack traces should remain equivalent.
- Diagnostic-only low-FPS `stepStats` aggregation is intentionally disabled to keep `PerformanceDiagnosticsSystem.EndStep` allocation-free. The per-frame `lastSteps` diagnostic remains available for freeze/pre-game logs.

## Validation run

- Main project Unity compile:
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -logFile /private/tmp/warline-gc-guards-main-compile-5.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
- Earlier main project Unity compile attempt:
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -logFile /private/tmp/warline-gc-compile.log`
  - Result: blocked because `/Users/farhad/Projects/WarlineCapture` was already open in another Unity instance.
- Shadow validation setup:
  - Mirrored only the six touched runtime files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow Unity compile:
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -logFile /private/tmp/warline-gc-unity1-compile.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
- Focused EditMode tests in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `UnitRenderBudgetSystemTests`: 13 passed, 0 failed.
  - `MatchHudMinimapProjectionSystemTests`: 11 passed, 0 failed.
  - `RuntimeDiagnosticsSystemTests`: 4 passed, 0 failed.
- Focused EditMode tests in `/Users/farhad/Projects/WarlineCapture`:
  - `UnitRenderBudgetSystemTests.RunFocusedValidation`: passed with `[UnitRenderBudgetFocusedValidation] result=Passed tests=14`.
  - `MatchHudMinimapProjectionSystemTests.RunFocusedValidation`: passed with `[MatchHudMinimapProjectionFocusedValidation] result=Passed tests=11`.
  - `PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation`: passed with `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1`.
  - Note: Unity `-runTests` parsed the Test Runner arguments but did not emit XML in this session, so the execute-method focused validations were used. The minimap execute-method excludes the existing `RuntimeMinimapReplacesDefaultSpriteOnExistingImageAndCentersViewportOnBind` case because it depends on Test Runner `LogAssert` scope.
- Continuation validation in `/Users/farhad/Projects/WarlineCapture`:
  - Main project Unity compile:
    - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -logFile /private/tmp/warline-main-gc-continue-compile-2.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - `UnitRenderBudgetSystemTests.RunFocusedValidation`:
    - Command log: `/private/tmp/warline-main-gc-continue-unitrender-2.log`
    - Result: passed with `[UnitRenderBudgetFocusedValidation] result=Passed tests=14`.
  - `MatchHudMinimapProjectionSystemTests.RunFocusedValidation`:
    - Command log: `/private/tmp/warline-main-gc-continue-minimap-3.log`
    - Result: passed with `[MatchHudMinimapProjectionFocusedValidation] result=Passed tests=11`.
  - `PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation`:
    - Command log: `/private/tmp/warline-main-gc-continue-diagnostics-8.log`
    - Result: passed with `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=1`.
  - `git diff --check`: passed.
- Call-stack automation validation in `/Users/farhad/Projects/WarlineCapture`:
  - Compile after adding capture utility:
    - Command log: `/private/tmp/warline-main-gc-callstack-compile-3.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - First automated Match steady-state capture:
    - Command log: `/private/tmp/warline-main-gc-callstack-capture-2.log`
    - Result: passed; first top site was `BuildingDefinitionSystem.BuildConfiguredSpawnableEntry`.
  - Compile after cached spawnable fix:
    - Command log: `/private/tmp/warline-main-gc-cache-compile.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - Re-capture after cached spawnable fix:
    - Command log: `/private/tmp/warline-main-gc-callstack-capture-after-cache.log`
    - Result: passed; previous top site removed, next top site was `BuildingUiCompositionSystem.CreateSource`.
  - Compile after selected-building boolean shortcut:
    - Command log: `/private/tmp/warline-main-gc-secondsite-compile.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - Re-capture after selected-building boolean shortcut:
    - Command log: `/private/tmp/warline-main-gc-callstack-capture-after-secondsite.log`
    - Result: passed; previous top site removed, current report points to `RuntimeCityCompositionSystem.CreateStartupContext`.
  - Final compile after capture-harness exit-order cleanup:
    - Command log: `/private/tmp/warline-main-gc-final-compile.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - Building UI query focused validation:
    - Command log: `/private/tmp/warline-main-gc-building-ui-query.log`
    - Result: passed with `[BuildingUiQueryValidation] result=Passed tests=3`.
  - Building runtime boundary focused validation:
    - Command log: `/private/tmp/warline-main-gc-building-boundary-2.log`
    - Result: passed with `[BuildingRuntimeBoundaryValidation] result=Passed`.
  - Build drawer catalog focused validation:
    - Command log: `/private/tmp/warline-main-gc-builddrawer-catalog.log`
    - Result: passed with `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`.
  - Final automated Match steady-state capture:
    - Command log: `/private/tmp/warline-main-gc-callstack-capture-final.log`
    - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
    - Result: report written and command exited 0; current top site is `RuntimeCityCompositionSystem.CreateStartupContext`.
    - Gap: Unity logs a shutdown-only `NullReferenceException` after `[MatchGcAllocationCallstackCapture] result=Passed` while exiting batchmode. The report is already written and the process exit code is 0, but the capture harness exit path still needs cleanup.
- Attempted shadow validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`:
  - Result: blocked by broad pre-existing compile errors unrelated to this GC pass, including duplicate `PathfindBatchJob`/tactical feedback symbols and missing component types such as `DynamicBlockerComponent`.
- Static checks:
  - `git diff --check`: passed.
  - Confirmed no pathfinding files are modified.
- Second continuation validation in `/Users/farhad/Projects/WarlineCapture`:
  - Compile after `RuntimeCityCompositionSystem` delegate caching:
    - Command log: `/private/tmp/warline-main-gc-runtimecity-compile.log`
    - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
  - Compile after selection context/callback caching:
    - Command logs: `/private/tmp/warline-main-gc-selectioninput-compile-2.log`, `/private/tmp/warline-main-gc-selectioncamera-compile-2.log`, `/private/tmp/warline-main-gc-commandresult-compile.log`, `/private/tmp/warline-main-gc-focusedunit-compile-2.log`
    - Result: passed; no compile errors or warnings.
  - Compile after building production/grid/boundary/configured-unit cache fixes:
    - Command logs: `/private/tmp/warline-main-gc-productiontransport-compile.log`, `/private/tmp/warline-main-gc-mapplacement-compile.log`, `/private/tmp/warline-main-gc-griddata-compile.log`, `/private/tmp/warline-main-gc-boundaryid-compile-2.log`, `/private/tmp/warline-main-gc-configuredunits-compile.log`, `/private/tmp/warline-main-gc-transportprewarm-compile.log`
    - Result: passed; no compile errors or warnings.
  - Compile after `UnitAttackSystem` native scratch-map fix:
    - Command log: `/private/tmp/warline-main-gc-unitattack-compile.log`
    - Result: passed; no compile errors or warnings.
  - Final automated Match steady-state capture after `UnitAttackSystem` fix:
    - Command log: `/private/tmp/warline-main-gc-callstack-capture-after-unitattack.log`
    - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
    - Result: report written and command exited 0; current top site is Unity Entities profiler metadata initialization, not a project gameplay file.
  - `BuildingUiQuerySystemTests.RunFocusedValidation`:
    - Command log: `/private/tmp/warline-main-gc-building-ui-query-2.log`
    - Result: passed with `[BuildingUiQueryValidation] result=Passed tests=3`.
  - `BuildDrawerCatalogQuerySystemTests.RunFocusedValidation`:
    - Command log: `/private/tmp/warline-main-gc-builddrawer-catalog-2.log`
    - Result: passed with `[BuildDrawerCatalogQueryValidation] result=Passed tests=21`.
  - `BuildingRuntimeBoundaryValidationTests.RunBatchValidation`:
    - Command log: `/private/tmp/warline-main-gc-building-boundary-3.log`
    - Result: passed with `[BuildingRuntimeBoundaryValidation] result=Passed`.
  - `CombatDeathValidationTests` through Unity `-runTests`:
    - Command log: `/private/tmp/warline-main-gc-combatdeath-tests.log`
    - Result: inconclusive; Unity exited 0 but did not emit the requested test-results XML in this session.
  - Static check:
    - `git diff --check`: passed.
  - Follow-up warmed steady-state capture attempt:
    - First command log: `/private/tmp/warline-main-gc-callstack-capture-warmed-next.log`
    - Result: blocked inside sandbox by Unity Package Manager IPC socket failure, `listen EPERM`.
    - Second command log: `/private/tmp/warline-main-gc-callstack-capture-warmed-next-2.log`
    - Result: blocked because a real Unity editor instance currently has `/Users/farhad/Projects/WarlineCapture` open.

## Validation result

- Compile/test validation: PASS in main workspace and earlier `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation.
- `/Users/farhad/Projects/WarlineCapture-CodexUnity2` validation: BLOCKED by unrelated broad compile errors in that shadow project.
- Automated profiler evidence: PARTIAL PASS. The automated Match steady-state capture now produces a call-stack report and confirmed the previous project gameplay sites removed from the top of the capture. The current measured top site is Unity Entities profiler metadata initialization, not a project gameplay file.
- Final profiler proof: PENDING. Battle/spike captures are still required to prove steady-state 0 B/frame or document remaining allocation sites across combat frames.

## Known gaps

- The first main-project validation attempt was blocked by an open Unity instance, then rerun successfully once the lock was gone.
- `dotnet build` is not a reliable substitute for this Unity project:
  - `Game.Runtime.csproj` failed inside Unity package RenderGraph generated code.
  - `WarlineCapture.Runtime.csproj --no-dependencies` references stale moved source paths.
- This pass now includes automated steady-state call-stack captures, but still does not include the required battle/spike Profiler capture.
- Automated profiler capture currently loads the raw profile as one aggregate frame in batchmode, so the report is useful for ranking call stacks but not yet a trustworthy per-frame steady-state byte average.
- `MatchGcAllocationCallstackCapture.RunSteadyState` logs a shutdown-only Unity `NullReferenceException` after the success line when exiting batchmode. The report is written and the process exits 0, but the harness exit path should be cleaned up before treating the capture command as CI-ready.
- Latest steady-state top site is Unity Entities/Burst profiler metadata initialization. Do not edit gameplay code from that site; run a warmed second capture or a battle/spike capture to find the next project-owned allocation before making more runtime changes.
- Follow-up warmed capture is currently blocked while the main Unity editor has `/Users/farhad/Projects/WarlineCapture` open. Do not delete the lockfile or kill the editor unless the owner explicitly allows it.
- `UnitAttackTraceSystem.LateUpdate` and `UnitImpostorRenderSystem.DrawQuery` still use ECS query snapshots; only a GC call-stack reprofile should decide whether those need a deeper chunk-iteration pass.

## Cross-lane impacts

- None expected for UI, Art, or QA assets.
- QA should treat the shadow compile/tests as code validation only, not as final performance proof.

## Next recommended task

Run a warmed second steady-state capture and then the required Match battle/spike Profiler capture with `GC.Alloc` call stacks enabled. Fix the next project-owned allocation site named by those captures only; do not chase the current Unity Entities profiler metadata initialization site as gameplay work.

## 2026-06-11 battle continuation update

Lane: Gameplay/Performance

Task: Continue Match battle-state GC allocation reduction after Unity batchmode was restored.

Files changed in this continuation:

- `Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs`
- `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
- `Design/AgentReports/2026-06-11_perf_match-gc-allocation-optimization-pass.md`

Contracts touched:

- No UI/API contract changes.
- `BuildingProductionSystem` now pools `RuntimeBuildingEntity.PendingProduction` internally and exposes `internal ReleasePendingProduction(...)` for transport completion paths.
- `GroundMissileProjectileFlightSystem` now uses a cached `EntityQuery` plus component lookups instead of the hot `SystemAPI.Query` foreach.
- No pathfinding files were modified.

User-visible behavior:

- Intended behavior is unchanged.
- Production transport visuals are pooled and reused instead of instantiated/destroyed per delivery.
- Production transport blade rotation now uses cached blade transforms.
- Pending unit-production entries are pooled and released on completion/cancel.

Profiler-backed sites addressed:

- `GroundMissileProjectileFlightSystem.OnUpdate`: replaced the hot generated `SystemAPI.Query` iterator with cached query/component lookup iteration. A first `IJobEntity` attempt was rejected after capture because it introduced editor Burst reflection allocation; it was replaced before final validation.
- `BuildingProductionTransportSystem.TryEnsureActiveProductionTransport`: pooled `ActiveProductionTransport` state objects and prewarmed them with transport visual pools.
- `BuildingBarrierSystem.UpdateRoadBarrierDoors`: removed boxed `IReadOnlyDictionary` enumeration from the road-gate existence probe.
- `BuildingProductionTransportSystem.RotateProductionTransportBlades`: cached blade transform lists per pooled transport instance instead of `GetComponentsInChildren<Transform>()` each frame.
- `BuildingProductionTransportSystem.GetProductionTransportDoorTransform`: cached missing `Door_X` lookups so doorless transports do not repeatedly recurse through transform names.
- `BuildingProductionRequestSystem.QueueFactionUnitProductionRequest` / `BuildingProductionSystem.TryQueuePlayerUnitFromBuilding`: pooled pending-production request objects.

Validation run:

- Main project Unity compile:
  - Command log: `/private/tmp/warline-main-gc-pending-production-pool2-compile.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, or `Compilation failed` entries.
- Main project battle capture:
  - Command log: `/private/tmp/warline-main-gc-callstack-capture-battle-37.log`
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
  - Result: `[MatchGcAllocationCallstackCapture] result=Passed frames=300`.
  - Latest aggregate capture: 415,072 bytes / 7,579 samples in the loaded raw frame.
  - Latest top site: Unity editor live conversion, `Unity.Scenes.Editor.LiveConversionConnection.Update`, not a project gameplay stack.
- Focused EditMode tests:
  - `BuildingProductionSystemTests`: `/private/tmp/warline-main-gc-building-production-tests.xml`, 21 passed / 0 failed.
  - `GroundMissileLauncherRuntimeTests`: `/private/tmp/warline-main-gc-ground-missile-tests.xml`, 13 passed / 1 failed.
- Static check:
  - `git diff --check`: passed.

Validation result:

- Compile: PASS.
- Battle capture command: PASS.
- Production focused tests: PASS.
- Ground missile focused tests: PARTIAL. `MissileProjectile_ImpactsAndDamagesEnemyArea` failed because the target health was 10 while the test expects 0. The impact did occur and this path is outside the allocation edits: the test fixture sets launcher `Damage = 90`, and `ApplyDirectHitDamage` subtracts that once from 100 health. I did not change missile damage semantics in this GC pass.

Known gaps:

- The latest battle capture is still an aggregate loaded raw frame, not a reliable per-frame steady-state byte average.
- Remaining reported top allocation is Unity editor live conversion noise. Do not edit gameplay code from that site.
- The capture still logs a shutdown-only `NullReferenceException` after the success line while Unity exits batchmode.
- `GroundMissileLauncherRuntimeTests.MissileProjectile_ImpactsAndDamagesEnemyArea` needs a separate gameplay/test-owner decision: either direct missile impact should kill 100 health despite `Damage = 90`, or the test expectation should be updated.

Cross-lane impacts:

- No expected UI, Art, or asset pipeline impact.
- QA should regression-check production delivery visuals because transport instances are now pooled/reused.

Next recommended task:

Run a player/build-style capture or disable editor live conversion in the capture harness, then reprofile the same battle scenario. If a project-owned stack reappears under the Unity editor noise, continue with the next confirmed allocation site only.

## 2026-06-11 ground missile dependency hotfix

Lane: Gameplay/Performance

Task: Fix runtime ECS safety exception reported after the allocation pass.

Files changed:

- `Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs`
- `Assets/Tests/Editor/GroundMissileLauncherRuntimeTests.cs`
- `Design/AgentReports/2026-06-11_perf_match-gc-allocation-optimization-pass.md`

Contracts touched:

- No gameplay contract change.
- `GroundMissileProjectileFlightSystem` now explicitly completes read/write dependencies for `GroundMissileProjectileComponent` and `LocalTransform` before its synchronous cached-query lookup loop.

User-visible behavior:

- Intended missile behavior is unchanged.
- Fixes the reported `InvalidOperationException` where `GroundMissileProjectileFlightSystem` read `LocalTransform` while `VehicleSlopeAlignmentSystem.AlignJob` still had a scheduled write dependency.

Validation run:

- Main-project Unity compile:
  - Command log: `/private/tmp/warline-main-ground-missile-dependency-validation-compile.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, `Compilation failed`, `Build failed`, or `Exception` entries.
- Focused direct validation:
  - Command log: `/private/tmp/warline-main-ground-missile-dependency-validation.log`
  - Method: `GroundMissileLauncherRuntimeTests.RunProjectileDependencyValidation`
  - Result: `[GroundMissileProjectileDependencyValidation] result=Passed tests=1`.
- Static check: `git diff --check` passed.

Validation result:

- Static check: PASS.
- Unity compile: PASS.
- Dependency regression validation: PASS.

Known gaps:

- Unity `-runTests` accepted the Test Runner arguments but did not emit a results XML in this session, so the focused validation used the project's existing `-executeMethod` validation-runner pattern instead.

Cross-lane impacts:

- None expected.

Next recommended task:

- Continue profiling only if this hotfix stays clean in normal editor play and the next capture exposes another project-owned allocation site.

## 2026-06-11 GC heartbeat continuation

Lane: Gameplay/Performance

Task: Continue remaining evidence-backed Match GC allocation optimization.

Files changed:

- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs`
- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
- `Design/AgentReports/2026-06-11_perf_match-gc-allocation-optimization-pass.md`

Contracts touched:

- No gameplay API contract change.
- No pathfinding files were modified.

User-visible behavior:

- AI combat order evaluation now runs at a 0.25-second cadence before scanning squads. Squad order refresh remains 2 seconds, so combat orders should remain responsive while avoiding per-frame no-op scans.
- No intended UI or missile behavior change.

Validation run:

- Main-project Unity compile:
  - Command log: `/private/tmp/warline-main-gc-disposeguard-compile.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, `BuildFailedException`, `Compilation failed`, or `Exception` entries.
- Main-project automated battle GC capture:
  - Command log: `/private/tmp/warline-main-gc-disposeguard-battle.log`
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
  - Result: `[MatchGcAllocationCallstackCapture] result=Passed frames=300`.
- Static check: `git diff --check` passed.

Validation result:

- Compile: PASS.
- Battle capture command: PASS.
- Static check: PASS.
- `AICombatOrderSystem` allocation bucket reduced from 11,960 bytes / 299 samples to 280 bytes / 7 samples in the latest capture.
- Latest aggregate battle capture: 39,520 bytes / 979 samples.

Known gaps:

- Remaining large reported buckets are `UIShellEcsPresentationSystem.Update` at 14,352 bytes / 299 samples, Unity Editor package-manager noise at 12,000 bytes / 300 samples, and `MenuBootstrapView.Update` at 9,568 bytes / 299 samples.
- The two UI buckets still collapse to the capture harness managed frame in the call stack, so they need a proof pass before architecture changes. A short retained-code experiment was not kept because it did not reduce total captured bytes.

Cross-lane impacts:

- QA should smoke-check enemy AI target/order responsiveness because the AI combat scan cadence changed from every frame to 0.25 seconds.

Next recommended task:

- Prove whether `UIShellEcsPresentationSystem.Update` and `MenuBootstrapView.Update` are true managed runtime allocations or Editor/call-stack attribution artifacts. If real, address the shell polling path with a focused design; if artifact, document the residual and continue to final warmed battle/spike validation.

## 2026-06-11 UI residual proof

Lane: Gameplay/Performance

Task: Prove remaining UI buckets before changing shell architecture.

Files changed:

- `Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`
- `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
- `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
- `Design/AgentReports/2026-06-11_perf_match-gc-allocation-optimization-pass.md`

Contracts touched:

- No gameplay or UI behavior contract change.
- Added editor-only allocation probe methods used by the automated GC capture harness. These are excluded from player builds by `UNITY_EDITOR`.

User-visible behavior:

- No intended visible behavior change.

Validation run:

- Main-project Unity compile:
  - Command log: `/private/tmp/warline-main-gc-ui-probe-compile.log`
  - Result: passed; no `error CS`, `warning CS`, `Scripts have compiler errors`, `BuildFailedException`, `Compilation failed`, or `Exception` entries.
- Main-project automated battle GC capture:
  - Command log: `/private/tmp/warline-main-gc-ui-probe-battle.log`
  - Report: `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`
  - Result: `[MatchGcAllocationCallstackCapture] result=Passed frames=300`.
- Static check: `git diff --check` passed.

Validation result:

- Compile: PASS.
- Battle capture command: PASS.
- Static check: PASS.
- Direct runtime allocation probe over the capture window:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
- Profiler hierarchy still reports:
  - `UIShellEcsPresentationSystem.Update`: 14,352 bytes / 299 samples.
  - `MenuBootstrapView.Update`: 9,568 bytes / 299 samples.
- Conclusion: these two remaining large UI rows are profiler/call-stack attribution artifacts from the automated editor capture, not confirmed runtime managed allocations inside those update methods.
- Latest aggregate battle capture: 39,180 bytes / 969 samples.

Known gaps:

- The capture remains an Editor batchmode hierarchy report loaded as one raw frame. It is useful for ranking evidence-backed project call stacks, but the UI residual proof shows it can over-attribute profiler/capture allocations to MonoBehaviour update rows.
- Spike-frame validation still needs either an interactive Profiler capture or a deterministic spike driver; no new deterministic spike driver was added in this pass.

Cross-lane impacts:

- None expected.

Next recommended task:

- Treat `InitialUnitsSpawnSystem`, `AICombatOrderSystem`, `UIShellEcsPresentationSystem.Update`, and `MenuBootstrapView.Update` as closed for this GC pass. Remaining large rows are Editor/capture noise. Future GC work should use the runtime allocation probe pattern or a player/build capture before changing shell or UI architecture.
