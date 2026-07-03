# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 14:33:00 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 33712
- GC.Alloc samples: 30197
- GC.Alloc bytes from hierarchy column: 2142439
- GC.Alloc samples excluding editor compiler threads: 30197
- GC.Alloc bytes excluding editor compiler threads: 2142439
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
| 1 | 718173 | 6817 | 123 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | GC.Alloc |
| 2 | 131560 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 3 | 105248 | 2392 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 4 | 94256 | 1644 | 137 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 5 | 91740 | 832 | 13 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 6 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 7 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 8 | 72094 | 1085 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 9 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 11 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 12 | 63330 | 938 | 287 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 13 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 14 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 15 | 40574 | 8 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 718173 | 6817 | 123 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | GC.Alloc |
| 2 | 131560 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 3 | 105248 | 2392 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 4 | 94256 | 1644 | 137 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 5 | 91740 | 832 | 13 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 6 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 7 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 8 | 72094 | 1085 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 9 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 10 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 11 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 12 | 63330 | 938 | 287 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 13 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 14 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 15 | 40574 | 8 | 5 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 82 | 326670 | 3758 |
| 2 | 13 | 85764 | 577 |
| 3 | 210 | 68667 | 586 |
| 4 | 271 | 55353 | 505 |
| 5 | 153 | 46689 | 317 |
| 6 | 30 | 45734 | 384 |
| 7 | 152 | 43019 | 315 |
| 8 | 209 | 37826 | 239 |
| 9 | 270 | 36986 | 229 |
| 10 | 86 | 36347 | 219 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 718173
Samples: 6817
Frames: 123
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 131560
Samples: 3289
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 105248
Samples: 2392
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 94256
Samples: 1644
Frames: 137
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 91740
Samples: 832
Frames: 13
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 72094
Samples: 1085
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 63330
Samples: 938
Frames: 287
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)

Bytes: 40574
Samples: 8
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:302] Game.Runtime.RuntimeGameplayStateSystem:TryGetStateEntity (Unity.Entities.EntityManager&,Unity.Entities.Entity&)
 #1  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:212] Game.Runtime.RuntimeGameplayStateSystem:ReadCameraInput ()
 #2  (Mono JIT Code) [RuntimeGameplayStateSystem.cs:113] Game.Runtime.RuntimeGameplayStateSystem:get_ZoomOutHeld ()
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:596] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ResolveZoomDirection (Game.Runtime.RuntimeGameplayStateSystem)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:552] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:403] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
