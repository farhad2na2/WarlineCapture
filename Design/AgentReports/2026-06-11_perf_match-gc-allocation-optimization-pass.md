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
- Attempted shadow validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`:
  - Result: blocked by broad pre-existing compile errors unrelated to this GC pass, including duplicate `PathfindBatchJob`/tactical feedback symbols and missing component types such as `DynamicBlockerComponent`.
- Static checks:
  - `git diff --check`: passed.
  - Confirmed no pathfinding files are modified.

## Validation result

- Compile/test validation: PASS in main workspace and earlier `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation.
- `/Users/farhad/Projects/WarlineCapture-CodexUnity2` validation: BLOCKED by unrelated broad compile errors in that shadow project.
- Final profiler proof: PENDING. Interactive Unity Profiler re-capture with `GC.Alloc` call stacks is still required to prove steady-state 0 B/frame or document remaining allocation sites.

## Known gaps

- The first main-project validation attempt was blocked by an open Unity instance, then rerun successfully once the lock was gone.
- `dotnet build` is not a reliable substitute for this Unity project:
  - `Game.Runtime.csproj` failed inside Unity package RenderGraph generated code.
  - `WarlineCapture.Runtime.csproj --no-dependencies` references stale moved source paths.
- This pass removed concrete static allocation sites matching the existing profiler baseline, but did not produce a fresh before/after Profiler capture.
- `UnitAttackTraceSystem.LateUpdate` and `UnitImpostorRenderSystem.DrawQuery` still use ECS query snapshots; only a GC call-stack reprofile should decide whether those need a deeper chunk-iteration pass.

## Cross-lane impacts

- None expected for UI, Art, or QA assets.
- QA should treat the shadow compile/tests as code validation only, not as final performance proof.

## Next recommended task

Run the required interactive Match battle Profiler capture with `GC.Alloc` call stacks enabled and compare the remaining steady-state allocation list against this pass. If allocations remain, fix the next measured site only.
