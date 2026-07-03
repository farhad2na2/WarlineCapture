# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 11:45:47 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 29498
- GC.Alloc samples: 90197
- GC.Alloc bytes from hierarchy column: 9114591
- GC.Alloc samples excluding editor compiler threads: 24101
- GC.Alloc bytes excluding editor compiler threads: 1418878
- Editor compiler-thread GC.Alloc samples: 66096
- Editor compiler-thread GC.Alloc bytes: 7695713
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.

## Top Allocation Sites Excluding Editor Compiler Threads

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 205712 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 2 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 3 | 143520 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 4 | 136954 | 1730 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 5 | 91686 | 36 | 16 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |
| 6 | 91248 | 18 | 16 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |
| 7 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 8 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 9 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 10 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 11 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 12 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 13 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 14 | 30304 | 674 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 15 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 7531238 | 64519 | 292 | Burst-CompilerThread-1 | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | GC.Alloc |
| 2 | 205712 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 3 | 161845 | 1532 | 285 | Burst-CompilerThread-1 | GC.Alloc | (no managed call stack captured) | GC.Alloc |
| 4 | 143520 | 3588 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 5 | 143520 | 3289 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 6 | 136954 | 1730 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 7 | 91686 | 36 | 16 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc |
| 8 | 91248 | 18 | 16 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc |
| 9 | 88504 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 10 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 11 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 12 | 59800 | 1495 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 13 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 14 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 15 | 35880 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 39 | 255122 | 261 |
| 2 | 249 | 136340 | 1077 |
| 3 | 188 | 133878 | 541 |
| 4 | 256 | 112514 | 1009 |
| 5 | 270 | 111710 | 961 |
| 6 | 298 | 108733 | 857 |
| 7 | 276 | 104573 | 924 |
| 8 | 286 | 101934 | 921 |
| 9 | 284 | 94810 | 600 |
| 10 | 231 | 90668 | 121 |

## Call Stacks

### 1. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 7531238
Samples: 64519
Frames: 292
Thread: Burst-CompilerThread-1
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 205712
Samples: 3588
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. (no managed call stack captured)

Bytes: 161845
Samples: 1532
Frames: 285
Thread: Burst-CompilerThread-1
Hierarchy path: GC.Alloc

```
(no managed call stack captured)
```

### 4. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 143520
Samples: 3588
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 143520
Samples: 3289
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 136954
Samples: 1730
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 91686
Samples: 36
Frames: 16
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 91248
Samples: 18
Frames: 16
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > LogStringToConsole > UnityEngine.CoreModule.dll!UnityEngine::StackTraceUtility.ExtractStackTrace() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 88504
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 59800
Samples: 1495
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #4  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:119] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:556] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #6  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #7  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #8  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)

Bytes: 35880
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:1362] Game.Runtime.TacticalFollowCameraModeSystemHelper:EnsurePoseEntity (Unity.Entities.EntityManager)
 #1  (Mono JIT Code) [TacticalFollowCameraModeSystemHelper.cs:121] Game.Runtime.TacticalFollowCameraModeSystemHelper:TryReadPose (Unity.Entities.EntityManager,Game.Components.TacticalFollowCameraPoseComponent&)
 #2  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:510] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateTacticalFollowCameraPose|15 ()
 #3  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:408] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass8_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
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
