# Match GC Allocation Call-Stack Capture

Date: 2026-07-06 12:40:35 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27391
- GC.Alloc samples: 6932
- GC.Alloc bytes from hierarchy column: 457049
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 2
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 232
- Steady-state player-relevant GC budget: Passed (232 / 1024 bytes)
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 6930
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 456817
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `GameplayRuntimeUpdateCompositionSystemHelper.Update` top-level phases. Diagnostic only; not a gate yet.
    - `GameplayRuntimeUpdate.RuntimeCity`: 0 bytes / 0 allocating updates / 0 total updates.
    - `GameplayRuntimeUpdate.RuntimeGridBlockers`: 0 bytes / 0 allocating updates / 0 total updates.
    - `GameplayRuntimeUpdate.RuntimeDecorations`: 0 bytes / 0 allocating updates / 0 total updates.
    - `GameplayRuntimeUpdate.RoadBuild`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.BuildingPlacement`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.Selection`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.DayNight`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.CitizenPopulation`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.MainMenu`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.LoadingGate`: 0 bytes / 0 allocating updates / 300 total updates.
    - `GameplayRuntimeUpdate.EndUpdate`: 0 bytes / 0 allocating updates / 300 total updates.
  - `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`: 0 bytes / 0 allocating updates / 300 total updates. Diagnostic only; not a gate yet.
    - `Selection.CommandFlush`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Input`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 35 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 35 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 1 create calls. pooled=0, wrappers=1, prefabInstantiates=1, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 69 total updates. activeUpdates=69, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling/Diagnostic Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 232 | 2 | 2 | Thread Pool Worker | GC.Alloc | (no managed call stack captured) | GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 147939 | 1676 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 2 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 5 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 6 | 23148 | 455 | 40 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 7 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 11 | 8704 | 68 | 34 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 12 | 8082 | 77 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 13 | 6528 | 136 | 68 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 14 | 5888 | 171 | 52 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 15 | 4352 | 34 | 34 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 147939 | 1676 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 2 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 5 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 6 | 23148 | 455 | 40 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 7 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 11 | 8704 | 68 | 34 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 12 | 8082 | 77 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 13 | 6528 | 136 | 68 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 14 | 5888 | 171 | 52 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 15 | 4352 | 34 | 34 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 124 | 144053 | 1212 |
| 2 | 122 | 81516 | 1042 |
| 3 | 253 | 15587 | 301 |
| 4 | 294 | 2254 | 43 |
| 5 | 123 | 2254 | 43 |
| 6 | 252 | 2197 | 46 |
| 7 | 0 | 1542 | 30 |
| 8 | 103 | 1480 | 26 |
| 9 | 163 | 1478 | 30 |
| 10 | 199 | 1408 | 28 |

## Call Stacks

### 1. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 147939
Samples: 1676
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23148
Samples: 455
Frames: 40
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 10177
Samples: 25
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 8704
Samples: 68
Frames: 34
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 8082
Samples: 77
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 6528
Samples: 136
Frames: 68
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 5888
Samples: 171
Frames: 52
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 4352
Samples: 34
Frames: 34
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:425] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling/diagnostic rows include Burst compiler threads, Unity AI/MCP/Tracing frames, diagnostic logging from `PerformanceDiagnosticsSystemHelper.LogNoStackTrace`, and probe-contradicted Mono JIT attribution rows. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
