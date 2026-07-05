# Match GC Allocation Call-Stack Capture

Date: 2026-07-05 02:15:07 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27993
- GC.Alloc samples: 7756
- GC.Alloc bytes from hierarchy column: 502467
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 6805
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 435981
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 951
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 66486
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`
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
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 31 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 31 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 1 create calls. pooled=0, wrappers=1, prefabInstantiates=1, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 51 total updates. activeUpdates=50, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=1, pooledHits=1, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling/Diagnostic Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 112236 | 1594 | 58 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | GC.Alloc |
| 2 | 86291 | 1131 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 3 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 5 | 27440 | 686 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 6 | 16400 | 156 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 7 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 10 | 11232 | 143 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.ProcessPendingProductions > BuildingProductionRuntimeTick.ProcessPendingProductions > GC.Alloc |
| 11 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 12 | 7936 | 62 | 31 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 4800 | 100 | 50 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 14 | 4736 | 128 | 48 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 15 | 4240 | 10 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > Instantiate > Instantiate.Copy > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 65768 | 941 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 2 | 718 | 10 | 10 | Burst-CompilerThread-9 | GC.Alloc | (no managed call stack captured) | GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 112236 | 1594 | 58 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | GC.Alloc |
| 2 | 86291 | 1131 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 3 | 65768 | 941 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 5 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 6 | 27440 | 686 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 7 | 16400 | 156 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 8 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 11 | 11232 | 143 | 2 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.ProcessPendingProductions > BuildingProductionRuntimeTick.ProcessPendingProductions > GC.Alloc |
| 12 | 10177 | 25 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 13 | 7936 | 62 | 31 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 14 | 4800 | 100 | 50 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 15 | 4736 | 128 | 48 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 235 | 82180 | 1055 |
| 2 | 238 | 77763 | 638 |
| 3 | 59 | 23872 | 263 |
| 4 | 292 | 12313 | 234 |
| 5 | 237 | 7950 | 108 |
| 6 | 276 | 7854 | 106 |
| 7 | 120 | 7772 | 109 |
| 8 | 258 | 7636 | 112 |
| 9 | 164 | 7178 | 103 |
| 10 | 182 | 6934 | 98 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 112236
Samples: 1594
Frames: 58
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 86291
Samples: 1131
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 65768
Samples: 941
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 27440
Samples: 686
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 16400
Samples: 156
Frames: 2
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 11232
Samples: 143
Frames: 2
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.ProcessPendingProductions > BuildingProductionRuntimeTick.ProcessPendingProductions > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 10177
Samples: 25
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 7936
Samples: 62
Frames: 31
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 4800
Samples: 100
Frames: 50
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)

Bytes: 4736
Samples: 128
Frames: 48
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) [RtsCameraRequestSystem.cs:319] Game.Runtime.RtsCameraRequestSystem:TryGetGridConfig (Unity.Entities.EntityManager,Game.Components.GridConfig&)
 #1  (Mono JIT Code) [RtsCameraRequestSystem.cs:304] Game.Runtime.RtsCameraRequestSystem:SyncGroundBoundary (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,bool)
 #2  (Mono JIT Code) [RtsCameraRequestSystem.cs:253] Game.Runtime.RtsCameraRequestSystem:ProcessPendingRequests (Unity.Entities.EntityManager,Game.Runtime.RtsCameraSystem,UnityEngine.Camera,System.Action)
 #3  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:190] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:ProcessCameraRequests (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context,Unity.Entities.EntityManager)
 #4  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:548] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateZoom (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #5  (Mono JIT Code) [RtsSelectionRuntimeCameraSystemHelper.cs:181] Game.Runtime.RtsSelectionRuntimeCameraSystemHelper:UpdateRuntimeCameraTick (Game.Runtime.RtsSelectionRuntimeCameraSystemHelper/Context)
 #6  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:482] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:160] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling/diagnostic rows include Burst compiler threads, Unity AI/MCP/Tracing frames, and diagnostic logging from `PerformanceDiagnosticsSystemHelper.LogNoStackTrace`. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
