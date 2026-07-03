# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 15:10:57 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 32809
- GC.Alloc samples: 17267
- GC.Alloc bytes from hierarchy column: 1299916
- GC.Alloc samples excluding editor compiler threads: 17267
- GC.Alloc bytes excluding editor compiler threads: 1299916
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
| 1 | 422529 | 4093 | 76 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | GC.Alloc |
| 2 | 105248 | 2392 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 3 | 85147 | 1124 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 7 | 51011 | 465 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 8 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 9 | 41332 | 764 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 10 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 11 | 29588 | 290 | 4 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 12 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 13 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 15 | 20760 | 8 | 4 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 422529 | 4093 | 76 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | GC.Alloc |
| 2 | 105248 | 2392 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 3 | 85147 | 1124 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 7 | 51011 | 465 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 8 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 9 | 41332 | 764 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 10 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 11 | 29588 | 290 | 4 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 12 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 13 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 15 | 20760 | 8 | 4 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 8 | 105579 | 719 |
| 2 | 7 | 82478 | 1060 |
| 3 | 166 | 69506 | 492 |
| 4 | 33 | 53209 | 435 |
| 5 | 132 | 46779 | 455 |
| 6 | 261 | 46043 | 340 |
| 7 | 131 | 44413 | 292 |
| 8 | 262 | 32527 | 375 |
| 9 | 32 | 25620 | 169 |
| 10 | 260 | 24056 | 152 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 422529
Samples: 4093
Frames: 76
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 105248
Samples: 2392
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 85147
Samples: 1124
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 51011
Samples: 465
Frames: 9
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 41332
Samples: 764
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 29588
Samples: 290
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 26312
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)

Bytes: 20760
Samples: 8
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionScanTargetModeCommandSystem.cs:35] Game.Runtime.RtsSelectionScanTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:611] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessScanTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:323] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
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
