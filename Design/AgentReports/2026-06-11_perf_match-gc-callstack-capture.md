# Match GC Allocation Call-Stack Capture

Date: 2026-07-15 12:38:25 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 31906
- GC.Alloc samples: 376
- GC.Alloc bytes from hierarchy column: 21600
- Raw allocation samples resolved: 355 (21096 bytes)
- Raw allocation samples conservatively unresolved: 21 across 21 hierarchy items (504 bytes)
- Raw attribution failure reasons: `rawSampleCallstackUnavailable:21`
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 6
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 292
- Steady-state player-relevant GC budget: Passed (292 / 1024 bytes)
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 370
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 21308
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Unity AI MCP editor bridge disabled before warmup: True
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
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 14 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 14 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 0 create calls. pooled=0, wrappers=0, prefabInstantiates=0, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 3 total updates. activeUpdates=0, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling/Diagnostic Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 2 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 3 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:537] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.FinalizeAutomaticAssignmentSignature() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 4 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 5 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Mono.JIT > GC.Alloc |
| 6 | 28 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 3328 | 26 | 13 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 3 | 1664 | 13 | 13 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 4 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 5 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 224 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 7 | 140 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 8 | 140 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 9 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 3328 | 26 | 13 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 3 | 1664 | 13 | 13 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 4 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 5 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 224 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 7 | 140 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 8 | 140 | 7 | 7 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 9 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 10 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 11 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 12 | 56 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:537] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.FinalizeAutomaticAssignmentSignature() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc |
| 13 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 14 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Mono.JIT > GC.Alloc |
| 15 | 28 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 300 | 1076 | 8 |
| 2 | 282 | 432 | 4 |
| 3 | 259 | 432 | 4 |
| 4 | 236 | 432 | 4 |
| 5 | 213 | 432 | 4 |
| 6 | 190 | 432 | 4 |
| 7 | 174 | 432 | 4 |
| 8 | 155 | 432 | 4 |
| 9 | 135 | 432 | 4 |
| 10 | 111 | 432 | 4 |

## Call Stacks

### 1. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

### 2. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 3328
Samples: 26
Frames: 13
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 3. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 1664
Samples: 13
Frames: 13
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 4. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()

Bytes: 1076
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()
 #1 [/Users/bokken/build/output/unity/unity/Editor/Mono/EditorApplication.cs:399] UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions()
```

### 5. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 256
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 6. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 224
Samples: 7
Frames: 7
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 7. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 140
Samples: 7
Frames: 7
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 8. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 140
Samples: 7
Frames: 7
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 9. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 128
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 10. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell()

Bytes: 56
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:63] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.ResolveAutomaticAssignmentBlockReason()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:305] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.TryAssignAutomaticHaulerOrder()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:107] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.UpdateResourceHaulers()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs:229] Game.Runtime.dll!Game.Runtime::BuildingProductionRuntimeTickCompositionSystemHelper.UpdateResourceHaulers()
 #5 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:59] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__3()
 #6 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #7 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:687] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #8 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #9 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #11 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #12 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 11. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell()

Bytes: 56
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:142] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindNearestAutomaticSourceToCell()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/ResourceHaulerAutomaticRoutePolicySystemHelper.cs:22] Game.Runtime.dll!Game.Runtime::ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindAutomaticHaulerRoute()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:305] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.TryAssignAutomaticHaulerOrder()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:107] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.UpdateResourceHaulers()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs:229] Game.Runtime.dll!Game.Runtime::BuildingProductionRuntimeTickCompositionSystemHelper.UpdateResourceHaulers()
 #5 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:59] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__3()
 #6 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #7 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:687] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #8 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #9 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #11 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #12 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 12. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:537] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.FinalizeAutomaticAssignmentSignature()

Bytes: 56
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateResourceHaulers > BuildingProductionRuntimeTick.UpdateResourceHaulers > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:537] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.FinalizeAutomaticAssignmentSignature()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:490] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.CalculateAutomaticAssignmentSignature()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:423] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.ShouldRunAutomaticAssignmentScan()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs:107] Game.Runtime.dll!Game.Runtime::BuildingResourceHaulerBridgeCompositionSystemHelper.UpdateResourceHaulers()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs:229] Game.Runtime.dll!Game.Runtime::BuildingProductionRuntimeTickCompositionSystemHelper.UpdateResourceHaulers()
 #5 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:59] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__3()
 #6 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #7 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:687] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #8 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #9 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #11 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #12 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Composition/MatchSceneView.cs:114] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 13. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 48
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

### 14. #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()

Bytes: 48
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Mono.JIT > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/UnitPathResultApply.cs:31] Game.Runtime.dll!Game.Runtime::UnitPathResultApply.Apply()
 #1 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Systems/UnitPathfindingApply.cs:43] Game.Runtime.dll!Game.Runtime::UnitPathfindingApply.Apply()
 #2 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Library/Bee/artifacts/1300b0aEDbg.dag/SystemGenerator/Unity.Entities.SourceGen.SystemGenerator.SystemGenerator/Temp/GeneratedCode/Game.Runtime/UnitPathfindingSystem__System_6273298910.g.cs:15] Game.Runtime.dll!Game.Runtime::UnitPathfindingSystem.OnUpdate()
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

### 15. #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log()

Bytes: 28
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System::String.Ctor()
 #1 mscorlib.dll!System::String.CreateString()
 #2 [./Library/PackageCache/com.unity.collections@a43cabe808ca/Unity.Collections/FixedString.gen.cs:2443] Unity.Collections.dll!Unity.Collections::FixedString32Bytes.ToString()
 #3 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log()
 #4 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:45] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.DrainAcceptedRequests()
 #5 [/Users/farhad/Projects/WarlineCapture-ArchitectureHardening/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs:27] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update()
```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Allocation bytes come from per-instance GC metadata; hierarchy ownership comes from the allocation item path; managed stacks are resolved from each item's raw profiler sample index.
- Missing or malformed raw sample metadata is recorded as an unresolved hierarchy allocation and remains inside the player-relevant budget unless its hierarchy/thread independently proves editor tooling ownership.
- Probe-backed exclusions are limited to the exact 48-byte shell callback signature and the exact 256-byte selection-panel refresh signature proven by controlled marker A/B captures. Resolved Timer-Scheduler rows are excluded only when every frame is framework-only and the repository has no matching timer API owner. Every changed, unresolved, incomplete, or unrelated gameplay row remains player-relevant.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
