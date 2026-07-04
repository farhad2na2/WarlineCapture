# Match GC Allocation Call-Stack Capture

Date: 2026-07-04 20:51:13 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 27993
- GC.Alloc samples: 52596
- GC.Alloc bytes from hierarchy column: 4196128
- GC.Alloc samples excluding editor/tooling rows: 7572
- GC.Alloc bytes excluding editor/tooling rows: 496120
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 45024
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 3700008
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`: 0 bytes / 0 allocating updates / 300 total updates. Diagnostic only; not a gate yet.
    - `Selection.CommandFlush`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Input`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 70 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 70 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 0 create calls. pooled=0, wrappers=0, prefabInstantiates=0, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 124 total updates. activeUpdates=124, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=1, pooledDropVisualHits=0, createdDropVisuals=1, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=1. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 68800 | 1200 | 100 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 2 | 60770 | 604 | 6 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 3 | 47830 | 613 | 123 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 4 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 5 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 6 | 24200 | 602 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 7 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 8 | 19806 | 58 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc |
| 9 | 17664 | 138 | 69 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 10 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 11 | 12132 | 2 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > MainMenuPlayUI.MinimapUpdate > GameObject.AddComponent > UnityEngine.UI.dll!UnityEngine.UI::Image.OnEnable() [Invoke] > GC.Alloc |
| 12 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 13 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 14 | 11940 | 31 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Instantiate > Instantiate.Copy > GC.Alloc |
| 15 | 10712 | 290 | 97 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |

## Top Editor/Tooling Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 3405749 | 40884 | 292 | Burst-CompilerThread-5 | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 2 | 134646 | 1926 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 65768 | 941 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 5 | 23941 | 278 | 272 | Burst-CompilerThread-5 | GC.Alloc | (no managed call stack captured) | GC.Alloc |
| 6 | 378 | 6 | 1 | Burst-CompilerThread-5 | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Mono.JIT > GC.Alloc |
| 7 | 248 | 4 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 3405749 | 40884 | 292 | Burst-CompilerThread-5 | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | GC.Alloc |
| 2 | 134646 | 1926 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 68800 | 1200 | 100 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 5 | 65768 | 941 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 6 | 60770 | 604 | 6 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 7 | 47830 | 613 | 123 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 8 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 9 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 10 | 24200 | 602 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 11 | 23941 | 278 | 272 | Burst-CompilerThread-5 | GC.Alloc | (no managed call stack captured) | GC.Alloc |
| 12 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 13 | 19806 | 58 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc |
| 14 | 17664 | 138 | 69 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 15 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases\|7 () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 99 | 635317 | 7472 |
| 2 | 100 | 130690 | 552 |
| 3 | 22 | 86234 | 956 |
| 4 | 0 | 41403 | 412 |
| 5 | 116 | 40034 | 392 |
| 6 | 204 | 35547 | 479 |
| 7 | 140 | 35445 | 373 |
| 8 | 296 | 35299 | 550 |
| 9 | 12 | 32051 | 329 |
| 10 | 252 | 32013 | 322 |

## Call Stacks

### 1. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 3405749
Samples: 40884
Frames: 292
Thread: Burst-CompilerThread-5
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 134646
Samples: 1926
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 68800
Samples: 1200
Frames: 100
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 65768
Samples: 941
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitPathfindingSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 60770
Samples: 604
Frames: 6
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 47830
Samples: 613
Frames: 123
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 24200
Samples: 602
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. (no managed call stack captured)

Bytes: 23941
Samples: 278
Frames: 272
Thread: Burst-CompilerThread-5
Hierarchy path: GC.Alloc

```
(no managed call stack captured)
```

### 12. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 19806
Samples: 58
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 17664
Samples: 138
Frames: 69
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [SelectionGameplayStartupSystemHelper.cs:423] Game.Runtime.SelectionGameplayStartupSystemHelper/<>c__DisplayClass9_0:<Initialize>g__UpdateSelectionRuntimePhases|7 ()
 #1  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:135] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #2  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #3  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #4  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #5  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling rows include Burst compiler threads plus Unity AI/MCP/Tracing frames. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
