# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 17:37:31 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 30702
- GC.Alloc samples: 21883
- GC.Alloc bytes from hierarchy column: 1907835
- GC.Alloc samples excluding editor/tooling rows: 21629
- GC.Alloc bytes excluding editor/tooling rows: 1880085
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 254
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 27750
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 343768 | 1523 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 2 | 321531 | 3028 | 70 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | GC.Alloc |
| 3 | 196416 | 2817 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 78936 | 1794 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 8 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 11 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 12 | 38850 | 382 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 13 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 14 | 30910 | 12 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |
| 15 | 30558 | 6 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 343768 | 1523 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 2 | 321531 | 3028 | 70 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | GC.Alloc |
| 3 | 196416 | 2817 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 78936 | 1794 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 8 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 9 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 64482 | 962 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 11 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 12 | 38850 | 382 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 13 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 14 | 30910 | 12 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |
| 15 | 30558 | 6 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 108 | 292971 | 1418 |
| 2 | 258 | 287732 | 3369 |
| 3 | 259 | 272495 | 3240 |
| 4 | 109 | 148875 | 649 |
| 5 | 107 | 81350 | 473 |
| 6 | 142 | 52624 | 439 |
| 7 | 67 | 46746 | 459 |
| 8 | 260 | 32198 | 372 |
| 9 | 41 | 28794 | 115 |
| 10 | 164 | 27493 | 233 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 343768
Samples: 1523
Frames: 2
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 321531
Samples: 3028
Frames: 70
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 196416
Samples: 2817
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitGridMovementSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 78936
Samples: 1794
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 64482
Samples: 962
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 38850
Samples: 382
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 30910
Samples: 12
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #3  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)

Bytes: 30558
Samples: 6
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsSelectionAttackTargetModeCommandSystem.cs:69] Game.Runtime.RtsSelectionAttackTargetModeCommandSystem:ProcessPendingRequests (Unity.Entities.EntityManager,int,Unity.Entities.Entity,Game.Components.RtsSelectionCommandIntentKind&,bool&,bool&,Game.Tactical.Contracts.TacticalCommandReasonCode&)
 #1  (Mono JIT Code) [RtsSelectionCommandResultFlushCompositionSystemHelper.cs:471] Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper:ProcessAttackTargetModeCommandRequests (Game.Runtime.RtsSelectionCommandResultFlushCompositionSystemHelper/Context,int,Unity.Entities.Entity)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:317] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
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
