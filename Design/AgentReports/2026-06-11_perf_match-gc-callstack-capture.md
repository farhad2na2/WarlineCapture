# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 13:14:24 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 30401
- GC.Alloc samples: 30260
- GC.Alloc bytes from hierarchy column: 2009184
- GC.Alloc samples excluding editor compiler threads: 30260
- GC.Alloc bytes excluding editor compiler threads: 2009184
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
| 1 | 393365 | 4011 | 106 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | GC.Alloc |
| 2 | 196416 | 2817 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 143520 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 4 | 131560 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 72776 | 1095 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 8 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 11 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 12 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 13 | 58480 | 1020 | 85 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 14 | 46770 | 426 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 15 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 393365 | 4011 | 106 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | GC.Alloc |
| 2 | 196416 | 2817 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 143520 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 4 | 131560 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 72776 | 1095 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 8 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 11 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 12 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 13 | 58480 | 1020 | 85 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 14 | 46770 | 426 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 15 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 147 | 299006 | 3420 |
| 2 | 148 | 263475 | 3237 |
| 3 | 85 | 70646 | 522 |
| 4 | 213 | 46624 | 480 |
| 5 | 149 | 43842 | 433 |
| 6 | 106 | 34701 | 308 |
| 7 | 31 | 34655 | 306 |
| 8 | 30 | 31496 | 312 |
| 9 | 1 | 30124 | 146 |
| 10 | 278 | 24735 | 287 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 393365
Samples: 4011
Frames: 106
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 196416
Samples: 2817
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 143520
Samples: 3289
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 131560
Samples: 3289
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 72776
Samples: 1095
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 64482
Samples: 962
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 58480
Samples: 1020
Frames: 85
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 46770
Samples: 426
Frames: 9
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionBoardTargetModeCommandSystem.cs:52] Game.Runtime.RtsSelectionBoardTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,bool&,bool&,Game.Components.BoardCommandModeDirection&,Unity.Entities.Entity&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:635] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessBoardTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:326] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
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
