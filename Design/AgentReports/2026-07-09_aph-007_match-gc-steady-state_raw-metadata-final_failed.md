# Match GC Allocation Call-Stack Capture

Date: 2026-07-09 21:59:14 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27692
- GC.Alloc samples: 4826
- GC.Alloc bytes from hierarchy column: 269482
- Raw allocation samples resolved: 4793 (268816 bytes)
- Raw allocation samples conservatively unresolved: 33 across 33 hierarchy items (666 bytes)
- Raw attribution failure reasons: `rawSampleCallstackUnavailable:33`
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 4826
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 269482
- Steady-state player-relevant GC budget: Failed (269482 / 1024 bytes)
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 0
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 0
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
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
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 9 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 9 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 0 create calls. pooled=0, wrappers=0, prefabInstantiates=0, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 2 total updates. activeUpdates=0, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling/Diagnostic Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:26] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 2 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs:9] Game.Runtime.dll!Game.Runtime::SelectionStateCompositionSystemHelper..ctor() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 3 | 16744 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:281] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 4 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 5 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs:211] Game.Runtime.dll!Game.Runtime::RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 6 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1376] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiCampItemCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 7 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:175] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1409] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiProductionCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs:438] Game.Runtime.dll!Game.Runtime::BuildingPlacementCommandRequestCompositionSystemHelper.TryGetUiPlacementCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:407] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.HasValidTacticalFollowPose() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 11 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:396] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.IsTacticalFollowPanLocked() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 12 | 8104 | 40 | 8 | Main Thread | GC.Alloc | #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 13 | 7400 | 8 | 8 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 14 | 6248 | 32 | 8 | Main Thread | GC.Alloc | #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 15 | 5720 | 8 | 8 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 0 | 0 | 0 | 0 | n/a | n/a | No GC.Alloc samples found in this automated capture. | n/a |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:26] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 2 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs:9] Game.Runtime.dll!Game.Runtime::SelectionStateCompositionSystemHelper..ctor() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 3 | 16744 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:281] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 4 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 5 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs:211] Game.Runtime.dll!Game.Runtime::RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 6 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1376] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiCampItemCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 7 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:175] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1409] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiProductionCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs:438] Game.Runtime.dll!Game.Runtime::BuildingPlacementCommandRequestCompositionSystemHelper.TryGetUiPlacementCommandEntity() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:407] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.HasValidTacticalFollowPose() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 11 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:396] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.IsTacticalFollowPanLocked() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 12 | 8104 | 40 | 8 | Main Thread | GC.Alloc | #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 13 | 7400 | 8 | 8 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 14 | 6248 | 32 | 8 | Main Thread | GC.Alloc | #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 15 | 5720 | 8 | 8 | Main Thread | GC.Alloc | #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 269 | 10779 | 144 |
| 2 | 95 | 10779 | 144 |
| 3 | 179 | 10577 | 140 |
| 4 | 13 | 10577 | 140 |
| 5 | 194 | 7448 | 104 |
| 6 | 286 | 7246 | 100 |
| 7 | 29 | 7174 | 98 |
| 8 | 108 | 7174 | 98 |
| 9 | 270 | 5031 | 68 |
| 10 | 180 | 4495 | 62 |

## Call Stacks

### 1. #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:26] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView()

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/bokken/build/output/unity/unity/Runtime/Export/SceneManager/Scene.cs:110] UnityEngine.CoreModule.dll!UnityEngine.SceneManagement::Scene.GetRootGameObjects()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:26] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:15] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneReferenceSceneSystemHelper.cs:10] Game.Composition.dll!Game.Composition::MatchSceneReferenceSceneSystemHelper.TryGetLoadedMatchSceneView()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs:436] Game.Composition.dll!Game.Composition::MenuBootstrapCompositionSystemHelper.BindMatchRuntimeUi()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs:110] Game.Composition.dll!Game.Composition::MenuBootstrapCompositionSystemHelper.Update()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MenuBootstrapView.cs:106] Game.Composition.dll!Game.Composition::MenuBootstrapView.Update()
```

### 2. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs:9] Game.Runtime.dll!Game.Runtime::SelectionStateCompositionSystemHelper..ctor()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs:9] Game.Runtime.dll!Game.Runtime::SelectionStateCompositionSystemHelper..ctor()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:281] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:118] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.OnUpdate()
 #3 Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.__codegen__OnUpdate()
 #4 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #5 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #6 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #7 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #8 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #9 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #10 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #11 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

### 3. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:281] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests()

Bytes: 16744
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:281] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.ProcessPreResolvedTransportRequests()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs:118] Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.OnUpdate()
 #2 Game.Runtime.dll!Game.Runtime::TransportBoardingCommandSystem.__codegen__OnUpdate()
 #3 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #4 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #5 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #6 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #7 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #8 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #9 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #10 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

### 4. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

### 5. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs:211] Game.Runtime.dll!Game.Runtime::RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs:211] Game.Runtime.dll!Game.Runtime::RoadBuildCommandCompositionSystemHelper.EnsureRoadBuildCommandEntity()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs:104] Game.Runtime.dll!Game.Runtime::RoadBuildCommandCompositionSystemHelper.ProcessPendingRoadBuildCommands()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildRuntimeActionCompositionSystemHelper.cs:83] Game.Runtime.dll!Game.Runtime::RoadBuildRuntimeActionCompositionSystemHelper.ProcessCommandQueue()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildRuntimeActionCompositionSystemHelper.cs:66] Game.Runtime.dll!Game.Runtime::RoadBuildRuntimeActionCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RoadBuildCompositionSystemHelper.cs:59] Game.Runtime.dll!::<>c__DisplayClass2_0.<Initialize>b__0()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 6. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1376] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiCampItemCommandEntity()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1376] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiCampItemCommandEntity()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:663] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.ProcessPendingUiCampItemCommandsIfPresent()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:180] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessUiProductionRequests()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 7. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:175] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:175] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:44] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.DrainAcceptedRequests()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs:27] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update()
```

### 8. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1409] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiProductionCommandEntity()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1409] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryGetUiProductionCommandEntity()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:266] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.ProcessPendingUiProductionCommandsIfPresent()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:180] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessUiProductionRequests()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 9. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs:438] Game.Runtime.dll!Game.Runtime::BuildingPlacementCommandRequestCompositionSystemHelper.TryGetUiPlacementCommandEntity()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs:438] Game.Runtime.dll!Game.Runtime::BuildingPlacementCommandRequestCompositionSystemHelper.TryGetUiPlacementCommandEntity()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementCommandRequestCompositionSystemHelper.HasPendingUiPlacementCommands()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementInputTickCompositionSystemHelper.cs:44] Game.Runtime.dll!Game.Runtime::BuildingPlacementInputTickCompositionSystemHelper.ProcessPendingPlacementCommands()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementInputTickCompositionSystemHelper.cs:32] Game.Runtime.dll!::<>c__DisplayClass0_0.<Create>b__7()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickUiSystemHelper.cs:93] Game.Runtime.dll!Game.Runtime::BuildingPlacementInputRuntimeTickUiSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeTickCompositionSystemHelper.cs:46] Game.Runtime.dll!::<>c__DisplayClass0_0.<Create>b__5()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 10. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:407] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.HasValidTacticalFollowPose()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:407] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.HasValidTacticalFollowPose()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:388] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.TacticalFollowOwnsCamera()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:121] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.UpdateRuntimeCameraTick()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|7()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 11. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:396] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.IsTacticalFollowPanLocked()

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:396] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.IsTacticalFollowPanLocked()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:388] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.TacticalFollowOwnsCamera()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/RtsSelectionRuntimeCameraSystemHelper.cs:121] Game.Runtime.dll!Game.Runtime::RtsSelectionRuntimeCameraSystemHelper.UpdateRuntimeCameraTick()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|7()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:574] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:208] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:103] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 12. #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule()

Bytes: 8104
Samples: 40
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0 mscorlib.dll!System.Text::StringBuilder.ToString()
 #1 mscorlib.dll!System.Text::StringBuilderCache.GetStringAndRelease()
 #2 mscorlib.dll!System::String.FormatHelper()
 #3 mscorlib.dll!System::String.Format()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule()
 #5 [/Users/farhad/Projects/WarlineCapture/Library/Bee/artifacts/1300b0aEDbg.dag/SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Game.Runtime/UnitPathfindingSystem__System_6834964750.g.cs:15] Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.OnUpdate()
 #6 Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.__codegen__OnUpdate()
 #7 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #8 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #9 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #10 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #11 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #12 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #13 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #14 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

### 13. #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule()

Bytes: 7400
Samples: 8
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0 mscorlib.dll!System::String.Concat()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs:50] Game.Runtime.dll!Game.Runtime::UnitPathfindingScheduler.Schedule()
 #2 [/Users/farhad/Projects/WarlineCapture/Library/Bee/artifacts/1300b0aEDbg.dag/SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Game.Runtime/UnitPathfindingSystem__System_6834964750.g.cs:15] Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.OnUpdate()
 #3 Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.__codegen__OnUpdate()
 #4 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #5 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #6 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #7 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #8 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #9 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #10 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #11 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

### 14. #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()

Bytes: 6248
Samples: 32
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0 mscorlib.dll!System.Text::StringBuilder.ToString()
 #1 mscorlib.dll!System.Text::StringBuilderCache.GetStringAndRelease()
 #2 mscorlib.dll!System::String.FormatHelper()
 #3 mscorlib.dll!System::String.Format()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingApply.cs:43] Game.Runtime.dll!Game.Runtime::UnitPathfindingApply.Apply()
 #6 [/Users/farhad/Projects/WarlineCapture/Library/Bee/artifacts/1300b0aEDbg.dag/SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Game.Runtime/UnitPathfindingSystem__System_6834964750.g.cs:15] Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.OnUpdate()
 #7 Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.__codegen__OnUpdate()
 #8 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #9 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #10 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #11 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #12 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #13 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #14 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #15 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

### 15. #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()

Bytes: 5720
Samples: 8
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0 mscorlib.dll!System::String.Concat()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/UnitPathfindingApply.cs:43] Game.Runtime.dll!Game.Runtime::UnitPathfindingApply.Apply()
 #3 [/Users/farhad/Projects/WarlineCapture/Library/Bee/artifacts/1300b0aEDbg.dag/SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Game.Runtime/UnitPathfindingSystem__System_6834964750.g.cs:15] Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.OnUpdate()
 #4 Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.__codegen__OnUpdate()
 #5 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBaseRegistry.cs:248] Unity.Entities.dll!::<>c__DisplayClass9_0.<SelectBurstFn>b__0()
 #6 Unity.Entities.dll!Unity.Entities::UnmanagedUpdate_000015A0$BurstDirectCall.Invoke()
 #7 Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UnmanagedUpdate()
 #8 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/WorldUnmanaged.cs:924] Unity.Entities.dll!Unity.Entities::WorldUnmanagedImpl.UpdateSystem()
 #9 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:699] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.UpdateAllSystems()
 #10 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ComponentSystemGroup.cs:682] Unity.Entities.dll!Unity.Entities::ComponentSystemGroup.OnUpdate()
 #11 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/SystemBase.cs:178] Unity.Entities.dll!Unity.Entities::SystemBase.Update()
 #12 [./Library/PackageCache/com.unity.entities@bab66ffaba49/Unity.Entities/ScriptBehaviourUpdateOrder.cs:520] Unity.Entities.dll!::DummyDelegateWrapper.TriggerUpdate()
```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Allocation bytes come from per-instance GC metadata; hierarchy ownership comes from the allocation item path; managed stacks are resolved from each item's raw profiler sample index.
- Missing or malformed raw sample metadata is recorded as an unresolved hierarchy allocation and remains inside the player-relevant budget unless its hierarchy/thread independently proves editor tooling ownership.
- Editor/tooling/diagnostic rows include only Burst compiler threads, Unity AI/MCP/Tracing hierarchy paths, and diagnostic logging hierarchy paths. Gameplay allocations are not excluded by direct-probe results.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
