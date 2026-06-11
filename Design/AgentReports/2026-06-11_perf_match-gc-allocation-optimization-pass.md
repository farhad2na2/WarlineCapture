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
- `Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs`
- `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs`
- `Assets/Tests/Editor/MatchHudMinimapProjectionSystemTests.cs`
- `Assets/Tests/Editor/PerformanceDiagnosticsSystemAllocationTests.cs`
- `Assets/Tests/Editor/PerformanceDiagnosticsSystemAllocationTests.cs.meta`
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

## Validation result

- Compile/test validation: PASS in main workspace and earlier `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation.
- `/Users/farhad/Projects/WarlineCapture-CodexUnity2` validation: BLOCKED by unrelated broad compile errors in that shadow project.
- Automated profiler evidence: PARTIAL PASS. The automated Match steady-state capture now produces a call-stack report and confirmed two top sites removed from the top of the capture. The current measured top site is `RuntimeCityCompositionSystem.CreateStartupContext`.
- Final profiler proof: PENDING. Battle/spike captures are still required to prove steady-state 0 B/frame or document remaining allocation sites across combat frames.

## Known gaps

- The first main-project validation attempt was blocked by an open Unity instance, then rerun successfully once the lock was gone.
- `dotnet build` is not a reliable substitute for this Unity project:
  - `Game.Runtime.csproj` failed inside Unity package RenderGraph generated code.
  - `WarlineCapture.Runtime.csproj --no-dependencies` references stale moved source paths.
- This pass now includes automated steady-state call-stack captures, but still does not include the required battle/spike Profiler capture.
- Automated profiler capture currently loads the raw profile as one aggregate frame in batchmode, so the report is useful for ranking call stacks but not yet a trustworthy per-frame steady-state byte average.
- `MatchGcAllocationCallstackCapture.RunSteadyState` logs a shutdown-only Unity `NullReferenceException` after the success line when exiting batchmode. The report is written and the process exits 0, but the harness exit path should be cleaned up before treating the capture command as CI-ready.
- `UnitAttackTraceSystem.LateUpdate` and `UnitImpostorRenderSystem.DrawQuery` still use ECS query snapshots; only a GC call-stack reprofile should decide whether those need a deeper chunk-iteration pass.

## Cross-lane impacts

- None expected for UI, Art, or QA assets.
- QA should treat the shadow compile/tests as code validation only, not as final performance proof.

## Next recommended task

Fix the next confirmed automated-capture site, `RuntimeCityCompositionSystem.CreateStartupContext`, then rerun `MatchGcAllocationCallstackCapture.RunSteadyState`. After that, run the required Match battle/spike Profiler capture with `GC.Alloc` call stacks enabled and fix the next measured site only.
