# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 07:25:24 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27692
- GC.Alloc samples: 34567
- GC.Alloc bytes from hierarchy column: 1829437
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 828464 | 16861 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GC.Alloc |
| 2 | 478400 | 6877 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.FixedWingRunwayHomeInitializationSystem > GC.Alloc |
| 3 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 4 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIProductionSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 5 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 6 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 7 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 8 | 26072 | 617 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 9 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 10 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GC.Alloc |
| 11 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 12 | 23616 | 419 | 33 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 13 | 14784 | 308 | 154 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 14 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 15 | 13156 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 145 | 87455 | 1117 |
| 2 | 146 | 35070 | 315 |
| 3 | 173 | 16437 | 309 |
| 4 | 172 | 9107 | 183 |
| 5 | 116 | 6520 | 128 |
| 6 | 20 | 6494 | 127 |
| 7 | 58 | 6494 | 127 |
| 8 | 48 | 6494 | 127 |
| 9 | 87 | 6494 | 127 |
| 10 | 68 | 6494 | 127 |

## Call Stacks

### 1. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 828464
Samples: 16861
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 478400
Samples: 6877
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.FixedWingRunwayHomeInitializationSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIProductionSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 35880
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 26072
Samples: 617
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23616
Samples: 419
Frames: 33
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 14784
Samples: 308
Frames: 154
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 13156
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:332] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass1_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
