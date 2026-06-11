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

## Contracts touched

- No public gameplay contract changes.
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
  - After: stores raw step samples per frame and builds formatted strings only when a diagnostic log is emitted.
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
- Impostors, minimap projection, minimap markers, attack traces, and diagnostic output format should remain equivalent.

## Validation run

- Main project Unity compile attempt:
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
- Static checks:
  - `git diff --check`: passed.
  - Confirmed no pathfinding files are modified.

## Validation result

- Compile/test validation: PASS in shadow workspace.
- Final profiler proof: PENDING. Interactive Unity Profiler re-capture with `GC.Alloc` call stacks is still required to prove steady-state 0 B/frame or document remaining allocation sites.

## Known gaps

- The main project could not be batchmode-validated because it was open in Unity.
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
