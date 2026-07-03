# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 10:20:38 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27692
- GC.Alloc samples: 30874
- GC.Alloc bytes from hierarchy column: 1644915
- GC.Alloc samples excluding editor compiler threads: 30874
- GC.Alloc bytes excluding editor compiler threads: 1644915
- Editor compiler-thread GC.Alloc samples: 0
- Editor compiler-thread GC.Alloc bytes: 0
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.

## Top Allocation Sites Excluding Editor Compiler Threads

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 478400 | 6877 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.FixedWingRunwayHomeInitializationSystem > GC.Alloc |
| 2 | 296608 | 6578 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 3 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 4 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIProductionSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 8 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 10 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 11 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 12 | 26072 | 617 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 13 | 23972 | 428 | 36 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 15 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 478400 | 6877 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.FixedWingRunwayHomeInitializationSystem > GC.Alloc |
| 2 | 296608 | 6578 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 3 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 4 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIProductionSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 8 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 10 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 11 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 12 | 26072 | 617 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 13 | 23972 | 428 | 36 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 15 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 132 | 85167 | 1112 |
| 2 | 133 | 34902 | 312 |
| 3 | 30 | 19723 | 382 |
| 4 | 86 | 7294 | 102 |
| 5 | 31 | 5365 | 101 |
| 6 | 213 | 5342 | 105 |
| 7 | 193 | 5342 | 105 |
| 8 | 150 | 5314 | 103 |
| 9 | 241 | 5270 | 102 |
| 10 | 298 | 5270 | 102 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 478400
Samples: 6877
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.FixedWingRunwayHomeInitializationSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 296608
Samples: 6578
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 143520
Samples: 3588
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 88504
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIProductionSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 35880
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 26072
Samples: 617
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 23972
Samples: 428
Frames: 36
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionSelectAllCommandSystem.cs:30] Game.Runtime.RtsSelectionSelectAllCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:252] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessSelectAllCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:331] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
