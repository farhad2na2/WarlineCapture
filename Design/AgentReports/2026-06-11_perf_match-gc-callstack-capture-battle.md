# Match GC Allocation Call-Stack Capture

Date: 2026-07-04 21:11:08 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 28294
- GC.Alloc samples: 16783
- GC.Alloc bytes from hierarchy column: 1758350
- GC.Alloc samples excluding editor/tooling rows: 8740
- GC.Alloc bytes excluding editor/tooling rows: 530819
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 8043
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 1227531
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`: 0 bytes / 0 allocating updates / 300 total updates. Diagnostic only; not a gate yet.
    - `Selection.CommandFlush`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Input`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 86 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 86 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 0 create calls. pooled=0, wrappers=0, prefabInstantiates=0, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 115 total updates. activeUpdates=110, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 135536 | 2364 | 197 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 2 | 53002 | 533 | 6 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 3 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 5 | 33902 | 421 | 110 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 6 | 24200 | 602 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 7 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 8 | 21760 | 170 | 85 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 9 | 19806 | 58 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc |
| 10 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 11 | 13728 | 372 | 109 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 12 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateInput > GC.Alloc |
| 13 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |
| 14 | 10880 | 85 | 85 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 15 | 10240 | 140 | 20 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |

## Top Editor/Tooling Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 989677 | 4764 | 222 | Burst-CompilerThread-2 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | GC.Alloc |
| 2 | 134646 | 1926 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 3 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 29298 | 270 | 199 | Burst-CompilerThread-2 | GC.Alloc | (no managed call stack captured) | GC.Alloc |
| 5 | 2612 | 63 | 24 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Mono.JIT > GC.Alloc |
| 6 | 374 | 9 | 9 | Burst-CompilerThread-7 | GC.Alloc | (no managed call stack captured) | Mono.JIT > GC.Alloc |
| 7 | 308 | 3 | 2 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Mono.JIT > mscorlib.dll!System.Buffers::ArrayPool`1..cctor() > GC.Alloc |
| 8 | 296 | 3 | 1 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Mono.JIT > mscorlib.dll!System.Buffers::ArrayPool`1..cctor() > mscorlib.dll!System.Buffers::TlsOverPerCoreLockedStacksArrayPool`1..cctor() > GC.Alloc |
| 9 | 288 | 1 | 1 | Burst-CompilerThread-7 | GC.Alloc | (no managed call stack captured) | mscorlib.dll!System.Security.Cryptography::SHA256Managed..cctor() > GC.Alloc |
| 10 | 160 | 4 | 2 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | mscorlib.dll!System::EmptyArray`1..cctor() > GC.Alloc |
| 11 | 120 | 3 | 1 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | mscorlib.dll!System.IO::EnumerationOptions..cctor() > GC.Alloc |
| 12 | 116 | 3 | 1 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Burst.Compiler.IL.dll!Burst.Compiler.IL.CodeSign::MachOSigner..cctor() > GC.Alloc |
| 13 | 96 | 2 | 2 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Mono.JIT > Mono.JIT > GC.Alloc |
| 14 | 48 | 1 | 1 | Burst-CompilerThread-7 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Mono.JIT > mscorlib.dll!System.Buffers::ArrayPool`1..cctor() > Mono.JIT > GC.Alloc |
| 15 | 48 | 1 | 1 | Burst-CompilerThread-7 | GC.Alloc | (no managed call stack captured) | Mono.JIT > mscorlib.dll!System.Buffers::ArrayPool`1..cctor() > Mono.JIT > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 989677 | 4764 | 222 | Burst-CompilerThread-2 | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | GC.Alloc |
| 2 | 135536 | 2364 | 197 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc |
| 3 | 134646 | 1926 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 4 | 69278 | 985 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 5 | 53002 | 533 | 6 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |
| 6 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 7 | 40664 | 897 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 8 | 33902 | 421 | 110 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 9 | 29298 | 270 | 199 | Burst-CompilerThread-2 | GC.Alloc | (no managed call stack captured) | GC.Alloc |
| 10 | 24200 | 602 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 11 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 12 | 21760 | 170 | 85 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 19806 | 58 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc |
| 14 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 15 | 13728 | 372 | 109 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 103 | 285075 | 129 |
| 2 | 37 | 282639 | 3698 |
| 3 | 31 | 279130 | 36 |
| 4 | 98 | 31372 | 184 |
| 5 | 279 | 20704 | 269 |
| 6 | 0 | 19903 | 195 |
| 7 | 96 | 19276 | 146 |
| 8 | 71 | 16359 | 66 |
| 9 | 174 | 15195 | 294 |
| 10 | 249 | 14210 | 179 |

## Call Stacks

### 1. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 989677
Samples: 4764
Frames: 222
Thread: Burst-CompilerThread-2
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 135536
Samples: 2364
Frames: 197
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 134646
Samples: 1926
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 69278
Samples: 985
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 53002
Samples: 533
Frames: 6
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 40664
Samples: 897
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 33902
Samples: 421
Frames: 110
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > BuildingProductionRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. (no managed call stack captured)

Bytes: 29298
Samples: 270
Frames: 199
Thread: Burst-CompilerThread-2
Hierarchy path: GC.Alloc

```
(no managed call stack captured)
```

### 10. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 24200
Samples: 602
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 21760
Samples: 170
Frames: 85
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 19806
Samples: 58
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.UnitLookAtTargetSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 13728
Samples: 372
Frames: 109
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:14] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Debug.bindings.cs:147] UnityEngine.Debug:LogFormat (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Object,string,object[])
 #4  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:563] Game.Runtime.PerformanceDiagnosticsSystemHelper:LogNoStackTrace (string)
 #5  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:541] Game.Runtime.PerformanceDiagnosticsSystemHelper:UpdateFrameRateDiagnostics (bool,double,int,bool,bool)
 #6  (Mono JIT Code) [PerformanceDiagnosticsSystemHelper.cs:246] Game.Runtime.PerformanceDiagnosticsSystemHelper:EndUpdate (bool,bool,int,bool,bool,bool)
 #7  (Mono JIT Code) [GameplayRuntimeUpdateCompositionSystemHelper.cs:204] Game.Runtime.GameplayRuntimeUpdateCompositionSystemHelper:Update (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #8  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:560] Game.Composition.MatchBootstrapCompositionSystemHelper:UpdateRuntime (bool,Game.Runtime.RuntimeGameplayStateSystem,Game.Runtime.PerformanceDiagnosticsSystemHelper,System.Action,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper,Game.Runtime.BuildingRuntimeUpdateCompositionSystemHelper/Context,System.Action,UnityEngine.Camera,Game.Runtime.RuntimeCityCompositionSystemHelper,Game.Runtime.RuntimeGridBlockerPresentationSystemHelper,Game.Runtime.RuntimeDecorationSpawnerPresentationSystemHelper,Game.Runtime.DayNightSystem,System.Action,Game.UI.Contracts.IMatchRuntimeUi,Game.Rendering.Contracts.IUnitImpostorRenderer,bool&)
 #9  (Mono JIT Code) [MatchBootstrapCompositionSystemHelper.cs:210] Game.Composition.MatchBootstrapCompositionSystemHelper:Update ()
 #10  (Mono JIT Code) [MatchSceneView.cs:98] Game.Composition.MatchSceneView:Update ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling rows include Burst compiler threads plus Unity AI/MCP/Tracing frames. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
