# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 11:04:25 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 28294
- GC.Alloc samples: 25245
- GC.Alloc bytes from hierarchy column: 1413444
- GC.Alloc samples excluding editor compiler threads: 25245
- GC.Alloc bytes excluding editor compiler threads: 1413444
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
| 1 | 169832 | 3887 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 2 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 3 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 7 | 66036 | 1021 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 8 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 11 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 12 | 48626 | 906 | 81 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | GC.Alloc |
| 13 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 14 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 15 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 169832 | 3887 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 2 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 3 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 7 | 66036 | 1021 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 8 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 11 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 12 | 48626 | 906 | 81 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | GC.Alloc |
| 13 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 14 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 15 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 188 | 289092 | 3402 |
| 2 | 102 | 70910 | 527 |
| 3 | 17 | 30398 | 151 |
| 4 | 282 | 19006 | 372 |
| 5 | 187 | 18784 | 132 |
| 6 | 251 | 18722 | 129 |
| 7 | 101 | 18706 | 131 |
| 8 | 97 | 18173 | 358 |
| 9 | 54 | 5366 | 72 |
| 10 | 189 | 5308 | 103 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 169832
Samples: 3887
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 143520
Samples: 3588
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 88504
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 66036
Samples: 1021
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 64482
Samples: 962
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 48626
Samples: 906
Frames: 81
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)

Bytes: 35880
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:37] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Components.RtsSelectionCommandIntentKind&)
 #1  (Mono JIT Code) [RtsSelectionCancelActiveCommandModeSystem.cs:30] Game.Runtime.RtsSelectionCancelActiveCommandModeSystem:ProcessPendingRequests (Unity.Entities.EntityManager)
 #2  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:408] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessCancelActiveCommandModeRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context)
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:329] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
