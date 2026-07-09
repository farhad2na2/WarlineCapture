# Match GC Allocation Call-Stack Capture

Date: 2026-07-09 21:03:56 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27993
- GC.Alloc samples: 4444
- GC.Alloc bytes from hierarchy column: 239639
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 4444
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 239639
- Steady-state player-relevant GC budget: Failed (239639 / 1024 bytes)
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
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 10 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 10 total updates.
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
| 1 | 45918 | 590 | 7 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 2 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 3 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 5 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 6 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 7 | 12044 | 302 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 3883 | 50 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 11 | 2304 | 18 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 12 | 2020 | 40 | 10 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | GC.Alloc |
| 13 | 1728 | 50 | 14 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 14 | 1488 | 24 | 3 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 15 | 1152 | 9 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 0 | 0 | 0 | 0 | n/a | n/a | No GC.Alloc samples found in this automated capture. | n/a |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 45918 | 590 | 7 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 2 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 3 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 5 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 6 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 7 | 12044 | 302 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |
| 8 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 9 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 10 | 3883 | 50 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc |
| 11 | 2304 | 18 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 12 | 2020 | 40 | 10 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | GC.Alloc |
| 13 | 1728 | 50 | 14 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 14 | 1488 | 24 | 3 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 15 | 1152 | 9 | 9 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 139 | 10807 | 148 |
| 2 | 222 | 10537 | 140 |
| 3 | 72 | 7274 | 101 |
| 4 | 241 | 7246 | 100 |
| 5 | 156 | 7246 | 100 |
| 6 | 0 | 5083 | 70 |
| 7 | 223 | 4479 | 62 |
| 8 | 140 | 4475 | 62 |
| 9 | 296 | 1330 | 22 |
| 10 | 88 | 1128 | 18 |

## Call Stacks

### 1. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 45918
Samples: 590
Frames: 7
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 12044
Samples: 302
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 3883
Samples: 50
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 2304
Samples: 18
Frames: 9
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 2020
Samples: 40
Frames: 10
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 1728
Samples: 50
Frames: 14
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 1488
Samples: 24
Frames: 3
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)

Bytes: 1152
Samples: 9
Frames: 9
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) string:FastAllocateString (int)
 #1  (Mono JIT Code) string:Concat (string,string,string,string)
 #2  (Mono JIT Code) [UnitPathResultApply.cs:85] Game.Runtime.UnitPathResultApply:Apply (Unity.Entities.SystemState&,Unity.Entities.Entity,Game.Components.PathPoolComponent&,Unity.Collections.NativeArray`1<Unity.Entities.Entity>,Unity.Collections.NativeArray`1<Game.Components.UnitPathRequest>,Unity.Collections.NativeArray`1<Unity.Mathematics.int2>,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<byte>,Game.Runtime.MapSurfacePathfindingSnapshot/Context,Unity.Collections.NativeStream,Unity.Collections.NativeArray`1<byte>,Unity.Collections.NativeArray`1<int>,Unity.Collections.NativeArray`1<int>,int&,int&,int&,int&,int&,int&,int&)
 #3  (Mono JIT Code) [UnitPathfindingApply.cs:86] Game.Runtime.UnitPathfindingApply:Apply (Unity.Entities.SystemState&,Game.Runtime.UnitPathfindingEntitySets&,Game.Runtime.UnitPathRequestBuffer&,Game.Runtime.UnitPathResultApply&,Game.Runtime.UnitPathValidationMetrics&,Game.Runtime.UnitPathfindingBudget&,Game.Runtime.UnitPathfindingDiagnostics&,Game.Runtime.UnitPathLiveUnitSnapshot&,Unity.Jobs.JobHandle&,Unity.Collections.NativeStream&,bool&,int&,int&,int&,int&,int&,int&,double&,bool,double)
 #4  (Mono JIT Code) [UnitPathfindingSystem.cs:100] Game.Runtime.UnitPathfindingSystem:OnUpdate (Unity.Entities.SystemState&)
 #5  (Mono JIT Code) Game.Runtime.UnitPathfindingSystem:__codegen__OnUpdate (intptr,intptr)
 #6  (Mono JIT Code) [SystemBaseRegistry.cs:251] Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #7  (Mono JIT Code) (wrapper native-to-managed) Unity.Entities.SystemBaseRegistry/<>c__DisplayClass9_0:<SelectBurstFn>b__0 (intptr,intptr)
 #8  (Mono JIT Code) (wrapper managed-to-native) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:wrapper_native_indirect_0xb51c2d018 (intptr&,void*)
 #9  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl/Unity.Entities.UnmanagedUpdate_000015A0$BurstDirectCall:Invoke (void*)
 #10  (Mono JIT Code) Unity.Entities.WorldUnmanagedImpl:UnmanagedUpdate (void*)
 #11  (Mono JIT Code) [WorldUnmanaged.cs:942] Unity.Entities.WorldUnmanagedImpl:UpdateSystem (Unity.Entities.SystemHandle)
 #12  (Mono JIT Code) [ComponentSystemGroup.cs:725] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #13  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #14  (Mono JIT Code) [SystemBase.cs:205] Unity.Entities.SystemBase:Update ()
 #15  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #16  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling/diagnostic rows include Burst compiler threads, Unity AI/MCP/Tracing frames, diagnostic logging from `PerformanceDiagnosticsSystemHelper.LogNoStackTrace`, and probe-contradicted Mono JIT attribution rows. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
