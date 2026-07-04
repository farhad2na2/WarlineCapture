# Match GC Allocation Call-Stack Capture

Date: 2026-07-04 11:59:45 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27692
- GC.Alloc samples: 6630
- GC.Alloc bytes from hierarchy column: 420990
- GC.Alloc samples excluding editor/tooling rows: 5691
- GC.Alloc bytes excluding editor/tooling rows: 355518
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 939
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 65472
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 84391 | 1115 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 2 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 3 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 4 | 28449 | 540 | 51 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 5 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 6 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 7 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 10240 | 80 | 40 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 10 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 11 | 7680 | 160 | 80 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 12 | 7384 | 73 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 13 | 6392 | 173 | 61 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 14 | 6328 | 1 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |
| 15 | 6156 | 2 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > LogStringToConsole > GC.Alloc |

## Top Editor/Tooling Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 84391 | 1115 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 2 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 5 | 28449 | 540 | 51 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 6 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 7 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 10 | 10240 | 80 | 40 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 11 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 12 | 7680 | 160 | 80 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 13 | 7384 | 73 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 14 | 6392 | 173 | 61 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 15 | 6328 | 1 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 86 | 96649 | 1334 |
| 2 | 87 | 94309 | 702 |
| 3 | 98 | 1723 | 34 |
| 4 | 0 | 1720 | 27 |
| 5 | 75 | 1656 | 34 |
| 6 | 272 | 1584 | 24 |
| 7 | 36 | 1555 | 26 |
| 8 | 43 | 1490 | 26 |
| 9 | 134 | 1435 | 25 |
| 10 | 240 | 1435 | 25 |

## Call Stacks

### 1. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 84391
Samples: 1115
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 28449
Samples: 540
Frames: 51
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 10240
Samples: 80
Frames: 40
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 10177
Samples: 25
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 7680
Samples: 160
Frames: 80
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 7384
Samples: 73
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 6392
Samples: 173
Frames: 61
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 6328
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:400] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling rows include Burst compiler threads plus Unity AI/MCP/Tracing frames. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
