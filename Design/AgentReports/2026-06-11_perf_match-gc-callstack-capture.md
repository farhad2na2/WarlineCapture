# Match GC Allocation Call-Stack Capture

Date: 2026-07-13 17:51:57 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 32508
- GC.Alloc samples: 494
- GC.Alloc bytes from hierarchy column: 30111
- Raw allocation samples resolved: 453 (29183 bytes)
- Raw allocation samples conservatively unresolved: 41 across 41 hierarchy items (928 bytes)
- Raw attribution failure reasons: `rawSampleCallstackUnavailable:41`
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 25
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 930
- Steady-state player-relevant GC budget: Passed (930 / 1024 bytes)
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 469
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 29181
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
    - `Selection.FocusedReadModel`: 0 bytes / 0 allocating updates / 21 total updates.
    - `Selection.Panel`: 0 bytes / 0 allocating updates / 21 total updates.
    - `Selection.TacticalCamera`: 0 bytes / 0 allocating updates / 600 total updates.
    - `Selection.MarkerPreview`: 0 bytes / 0 allocating updates / 300 total updates.
    - `Selection.Camera`: 0 bytes / 0 allocating updates / 300 total updates.
  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: 0 bytes / 0 allocating calls / 0 create calls. pooled=0, wrappers=0, prefabInstantiates=0, prefabInstantiateBytes=0 / 0 allocating prefab instantiates. Diagnostic only; not a gate yet.
  - `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`: 0 bytes / 0 allocating updates / 4 total updates. activeUpdates=0, acquireBytes=0 / 0 allocating acquire calls, acquireCalls=0, pooledHits=0, createdInstances=0, createBytes=0 / 0 allocating create calls, createCalls=0, dropVisualAcquireBytes=0 / 0 allocating drop-visual acquire calls, dropVisualAcquireCalls=0, pooledDropVisualHits=0, createdDropVisuals=0, dropVisualCreateBytes=0 / 0 allocating drop-visual create calls, dropVisualCreateCalls=0. Diagnostic only; not a gate yet.
  - `TransportBoardingCommandSystem`: 0 bytes / 0 allocating updates / 300 total updates. handledUpdates=0, commandBytes=0 / 0 allocating command calls, commandCalls=0, handledCommandCalls=0. Diagnostic only; not a gate yet.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling/Diagnostic Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 216 | 8 | 8 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:192] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.SetFpsText() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc |
| 2 | 152 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1390] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryFindFirstFactionProducerBuilding() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > Mono.JIT > GC.Alloc |
| 3 | 136 | 8 | 4 | Thread Pool Worker | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | GC.Alloc |
| 4 | 84 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 5 | 84 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 6 | 84 | 1 | 1 | Main Thread | GC.Alloc | #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1510] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryBuildConfiguredUnit() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 7 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::Thread.GetMutableExecutionContext() | GC.Alloc |
| 8 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 9 | 28 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:252] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.LogPresentationDiagnostic() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |
| 10 | 26 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:192] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.SetFpsText() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 5120 | 40 | 20 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 3 | 2560 | 20 | 20 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 4 | 1305 | 18 | 8 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.RunSchedulerLoop() | GC.Alloc |
| 5 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 6 | 768 | 8 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.Delay() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 7 | 640 | 8 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 8 | 512 | 4 | 4 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.FireTimer() | GC.Alloc |
| 9 | 352 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 10 | 288 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 11 | 288 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 12 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 220 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 14 | 220 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 15 | 192 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::CancellationTokenSource.InternalRegister() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 16 | 192 | 2 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.Delay() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 17 | 160 | 4 | 4 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper() | GC.Alloc |
| 18 | 160 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.SetContinuationForAwait() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 19 | 160 | 2 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 20 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 21 | 72 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 22 | 72 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 23 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::CancellationTokenSource.InternalRegister() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 24 | 40 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.SetContinuationForAwait() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 5120 | 40 | 20 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 3 | 2560 | 20 | 20 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 4 | 1305 | 18 | 8 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.RunSchedulerLoop() | GC.Alloc |
| 5 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 6 | 768 | 8 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.Delay() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 7 | 640 | 8 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 8 | 512 | 4 | 4 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.FireTimer() | GC.Alloc |
| 9 | 352 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 10 | 288 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 11 | 288 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 12 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 220 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 14 | 220 | 11 | 11 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 15 | 216 | 8 | 8 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:192] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.SetFpsText() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc |
| 16 | 192 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::CancellationTokenSource.InternalRegister() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 17 | 192 | 2 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.Delay() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 18 | 160 | 4 | 4 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper() | GC.Alloc |
| 19 | 160 | 4 | 4 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.SetContinuationForAwait() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 20 | 160 | 2 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 21 | 152 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1390] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryFindFirstFactionProducerBuilding() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > Mono.JIT > GC.Alloc |
| 22 | 136 | 8 | 4 | Thread Pool Worker | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | GC.Alloc |
| 23 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 24 | 84 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 25 | 84 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 26 | 84 | 1 | 1 | Main Thread | GC.Alloc | #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1510] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryBuildConfiguredUnit() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 27 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::Thread.GetMutableExecutionContext() | GC.Alloc |
| 28 | 72 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 29 | 72 | 1 | 1 | Main Thread | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc |
| 30 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 1187 | 15 |
| 2 | 58 | 1124 | 17 |
| 3 | 300 | 1076 | 8 |
| 4 | 86 | 836 | 9 |
| 5 | 99 | 777 | 11 |
| 6 | 271 | 777 | 11 |
| 7 | 172 | 777 | 11 |
| 8 | 171 | 467 | 8 |
| 9 | 142 | 432 | 4 |
| 10 | 282 | 432 | 4 |

## Call Stacks

### 1. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

### 2. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 5120
Samples: 40
Frames: 20
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 3. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 2560
Samples: 20
Frames: 20
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 4. #0 mscorlib.dll!::Scheduler.RunSchedulerLoop()

Bytes: 1305
Samples: 18
Frames: 8
Thread: Timer-Scheduler
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!::Scheduler.RunSchedulerLoop()
 #1 mscorlib.dll!::Scheduler.SchedulerThread()
 #2 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart_Context()
 #3 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #4 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #5 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #6 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart()
```

### 5. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()

Bytes: 1076
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:244] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()
 #1 [/Users/bokken/build/output/unity/unity/Editor/Mono/EditorApplication.cs:399] UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions()
```

### 6. #0 mscorlib.dll!System.Threading.Tasks::Task.Delay()

Bytes: 768
Samples: 8
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #1 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #2 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #3 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #4 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #5 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #6 mscorlib.dll!::MoveNextRunner.Run()
 #7 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #8 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #10 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 7. #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()

Bytes: 640
Samples: 8
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #1 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #2 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #3 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #4 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #5 mscorlib.dll!::MoveNextRunner.Run()
 #6 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #7 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #8 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 8. #0 mscorlib.dll!::Scheduler.FireTimer()

Bytes: 512
Samples: 4
Frames: 4
Thread: Timer-Scheduler
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!::Scheduler.FireTimer()
 #1 mscorlib.dll!::Scheduler.RunSchedulerLoop()
 #2 mscorlib.dll!::Scheduler.SchedulerThread()
 #3 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart_Context()
 #4 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #5 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #6 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #7 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart()
```

### 9. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 352
Samples: 11
Frames: 11
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 10. #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()

Bytes: 288
Samples: 4
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()
 #1 mscorlib.dll!::Reader.get_LogicalCallContext()
 #2 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #3 mscorlib.dll!System.Threading::ExecutionContext.FastCapture()
 #4 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #6 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #7 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #8 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #9 mscorlib.dll!::MoveNextRunner.Run()
 #10 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #12 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #13 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 11. #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()

Bytes: 288
Samples: 4
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #1 mscorlib.dll!System.Threading::ExecutionContext.FastCapture()
 #2 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #3 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #4 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #5 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #6 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #7 mscorlib.dll!::MoveNextRunner.Run()
 #8 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #10 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 12. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 256
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 13. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 220
Samples: 11
Frames: 11
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 14. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 220
Samples: 11
Frames: 11
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 15. #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:192] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.SetFpsText()

Bytes: 216
Samples: 8
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System::Number.UInt32ToDecStr()
 #1 mscorlib.dll!System::Number.FormatInt32()
 #2 mscorlib.dll!System::Int32.ToString()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:192] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.SetFpsText()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs:48] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsUiSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/MenuDiagnosticsView.cs:53] Game.UI.Runtime.dll!Game.UI.Runtime::MenuDiagnosticsView.Update()
```

### 16. #0 mscorlib.dll!System.Threading::CancellationTokenSource.InternalRegister()

Bytes: 192
Samples: 4
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading::CancellationTokenSource.InternalRegister()
 #1 mscorlib.dll!System.Threading::CancellationToken.Register()
 #2 mscorlib.dll!System.Threading::CancellationToken.InternalRegisterWithoutEC()
 #3 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #4 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #6 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #7 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #8 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #9 mscorlib.dll!::MoveNextRunner.Run()
 #10 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #12 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #13 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 17. #0 mscorlib.dll!System.Threading.Tasks::Task.Delay()

Bytes: 192
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #1 mscorlib.dll!System.Threading.Tasks::Task.Delay()
 #2 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #3 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #4 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #5 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #6 mscorlib.dll!::MoveNextRunner.Run()
 #7 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #8 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #10 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 18. #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper()

Bytes: 160
Samples: 4
Frames: 4
Thread: Timer-Scheduler
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper()
 #1 mscorlib.dll!System.Threading::ThreadPool.UnsafeQueueUserWorkItem()
 #2 mscorlib.dll!::Scheduler.FireTimer()
 #3 mscorlib.dll!::Scheduler.RunSchedulerLoop()
 #4 mscorlib.dll!::Scheduler.SchedulerThread()
 #5 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart_Context()
 #6 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #7 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #8 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #9 mscorlib.dll!System.Threading::ThreadHelper.ThreadStart()
```

### 19. #0 mscorlib.dll!System.Threading.Tasks::Task.SetContinuationForAwait()

Bytes: 160
Samples: 4
Frames: 4
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading.Tasks::Task.SetContinuationForAwait()
 #1 mscorlib.dll!System.Runtime.CompilerServices::TaskAwaiter.OnCompletedInternal()
 #2 mscorlib.dll!System.Runtime.CompilerServices::TaskAwaiter.UnsafeOnCompleted()
 #3 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #4 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #5 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #6 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #7 mscorlib.dll!::MoveNextRunner.Run()
 #8 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #10 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 20. #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()

Bytes: 160
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #1 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #2 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #3 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #4 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #5 mscorlib.dll!::MoveNextRunner.Run()
 #6 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #7 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #8 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 21. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1390] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryFindFirstFactionProducerBuilding()

Bytes: 152
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > Mono.JIT > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1390] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryFindFirstFactionProducerBuilding()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:921] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.QueueFactionUnitProductionRequest()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:246] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessProductionRequests()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 22. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 136
Samples: 8
Frames: 4
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 23. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 128
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:308] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 24. #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey()

Bytes: 84
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0 mscorlib.dll!System.Globalization::TextInfo.ToLowerInternal()
 #1 mscorlib.dll!System.Globalization::TextInfo.ToLower()
 #2 mscorlib.dll!System::String.ToLowerInvariant()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs:615] Game.Runtime.dll!Game.Runtime::BuildingRuntimeReadModelCompositionSystemHelper.Normalize()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs:126] Game.Runtime.dll!Game.Runtime::BuildingRuntimeReadModelCompositionSystemHelper.CountRuntimeProducedUnitsForFaction()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:246] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessProductionRequests()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #14 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #15 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #16 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #17 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 25. #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey()

Bytes: 84
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0 mscorlib.dll!System.Globalization::TextInfo.ToLowerInternal()
 #1 mscorlib.dll!System.Globalization::TextInfo.ToLower()
 #2 mscorlib.dll!System::String.ToLowerInvariant()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:611] Game.Runtime.dll!Game.Runtime::BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs:615] Game.Runtime.dll!Game.Runtime::BuildingRuntimeReadModelCompositionSystemHelper.Normalize()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs:194] Game.Runtime.dll!Game.Runtime::BuildingRuntimeReadModelCompositionSystemHelper.CountPendingProductionsForFaction()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:246] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessProductionRequests()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #14 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #15 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #16 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #17 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 26. #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1510] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryBuildConfiguredUnit()

Bytes: 84
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0 mscorlib.dll!System::String.Ctor()
 #1 mscorlib.dll!System::String.CreateString()
 #2 [/Users/bokken/build/output/unity/unity/Runtime/Scripting/Marshalling/StringMarshalling.cs:75] UnityEngine.CoreModule.dll!UnityEngine.Bindings::OutStringMarshaller.GetStringAndDispose()
 #3 UnityEngine.CoreModule.dll!UnityEngine::Object.GetName()
 #4 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnityEngineObject.bindings.cs:409] UnityEngine.CoreModule.dll!UnityEngine::Object.get_name()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1510] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryBuildConfiguredUnit()
 #6 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:1455] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.TryResolveConfiguredUnit()
 #7 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs:921] Game.Runtime.dll!Game.Runtime::BuildingProductionRequestSystemHelper.QueueFactionUnitProductionRequest()
 #8 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:246] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessProductionRequests()
 #9 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:149] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.ProcessRequests()
 #10 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs:96] Game.Runtime.dll!Game.Runtime::BuildingRuntimeProcessingCompositionSystemHelper.Update()
 #11 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs:60] Game.Runtime.dll!Game.Runtime::BuildingRuntimePublishCompositionSystemHelper.Update()
 #12 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs:68] Game.Runtime.dll!::<>c__DisplayClass4_0.<Create>b__5()
 #13 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs:167] Game.Runtime.dll!Game.Runtime::BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation()
 #14 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs:685] Game.Runtime.dll!::<>c__DisplayClass15_0.<Initialize>g__UpdateBuildingSimulationTick|53()
 #15 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs:40] Game.Runtime.dll!Game.Runtime::BuildingRuntimeUpdateCompositionSystemHelper.UpdateSimulation()
 #16 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:65] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #17 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:584] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #18 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:214] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #19 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:107] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 27. #0 mscorlib.dll!System.Threading::Thread.GetMutableExecutionContext()

Bytes: 72
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading::Thread.GetMutableExecutionContext()
 #1 mscorlib.dll!System.Threading::SynchronizationContext.SetSynchronizationContext()
 #2 mscorlib.dll!System.Threading.Tasks::AwaitTaskContinuation.RunCallback()
 #3 mscorlib.dll!System.Threading.Tasks::SynchronizationContextAwaitTaskContinuation.Run()
 #4 mscorlib.dll!System.Threading.Tasks::Task.FinishContinuations()
 #5 mscorlib.dll!System.Threading.Tasks::Task.FinishStageThree()
 #6 mscorlib.dll!::DelayPromise.Complete()
 #7 mscorlib.dll!::<>c.<Delay>b__247_1()
 #8 mscorlib.dll!::Scheduler.TimerCB()
 #9 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #10 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #11 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 28. #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()

Bytes: 72
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #1 mscorlib.dll!System.Threading::ExecutionContext.FastCapture()
 #2 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #3 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #4 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #5 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #6 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #7 mscorlib.dll!::MoveNextRunner.Run()
 #8 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #9 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #10 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 29. #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()

Bytes: 72
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunDelayedTasks > UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()
 #1 mscorlib.dll!::Reader.get_LogicalCallContext()
 #2 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #3 mscorlib.dll!System.Threading::ExecutionContext.FastCapture()
 #4 mscorlib.dll!System.Runtime.CompilerServices::AsyncMethodBuilderCore.GetCompletionAction()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Editor/Assistant/Utils/TaskUtils.cs:0] Unity.AI.Assistant.Editor.dll!::<AwaitCondition>d__1.MoveNext()
 #6 mscorlib.dll!::MoveNextRunner.InvokeMoveNext()
 #7 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #8 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #9 mscorlib.dll!::MoveNextRunner.Run()
 #10 mscorlib.dll!::<>c.<.cctor>b__7_0()
 #11 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:156] UnityEngine.CoreModule.dll!::WorkRequest.Invoke()
 #12 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:73] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.Exec()
 #13 [/Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnitySynchronizationContext.cs:108] UnityEngine.CoreModule.dll!UnityEngine::UnitySynchronizationContext.ExecuteTasks()
```

### 30. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 48
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Allocation bytes come from per-instance GC metadata; hierarchy ownership comes from the allocation item path; managed stacks are resolved from each item's raw profiler sample index.
- Missing or malformed raw sample metadata is recorded as an unresolved hierarchy allocation and remains inside the player-relevant budget unless its hierarchy/thread independently proves editor tooling ownership.
- Probe-backed exclusions are limited to the exact 48-byte shell callback signature and the exact 256-byte selection-panel refresh signature proven by controlled marker A/B captures. Resolved Timer-Scheduler rows are excluded only when every frame is framework-only and the repository has no matching timer API owner. Every changed, unresolved, incomplete, or unrelated gameplay row remains player-relevant.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
