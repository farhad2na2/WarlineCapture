# Match GC Allocation Call-Stack Capture

Date: 2026-07-17 05:15:54 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route
Exact commit: `cd6e764bd878c6d7cedcbaa3c5034f0f105825b6`
Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
Dirty at capture start: `false`
Quality: `Mobile` (index `1`)
Resolution: `640x480`
Instrumentation: `Unity Profiler binary log; allocation callstacks enabled; Scripts and Memory categories enabled; deep profiling disabled; capture logging suppressed`

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 28595
- GC.Alloc samples: 646
- GC.Alloc bytes from hierarchy column: 34666
- Raw allocation samples resolved: 623 (34120 bytes)
- Raw allocation samples conservatively unresolved: 23 across 23 hierarchy items (546 bytes)
- Raw attribution failure reasons: `rawSampleCallstackUnavailable:23`
- GC.Alloc samples excluding editor/tooling/diagnostic rows: 10
- GC.Alloc bytes excluding editor/tooling/diagnostic rows: 262
- Steady-state player-relevant GC budget: Passed (262 / 1024 bytes)
- Editor/tooling/diagnostic GC.Alloc samples excluded from player-relevant rows: 636
- Editor/tooling/diagnostic GC.Alloc bytes excluded from player-relevant rows: 34404
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
| 1 | 186 | 8 | 4 | Thread Pool Worker | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | GC.Alloc |
| 2 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 3 | 28 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |

## Top Editor/Tooling/Diagnostic Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 10356 | 206 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces() | GC.Alloc |
| 3 | 2048 | 16 | 8 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 4 | 1368 | 15 | 1 | Thread Pool Worker | GC.Alloc | #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces() | GC.Alloc |
| 5 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:288] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 6 | 1024 | 8 | 8 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 7 | 796 | 18 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation.MacOsStructs::sockaddr_dl.Read() | GC.Alloc |
| 8 | 720 | 18 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface..ctor() | GC.Alloc |
| 9 | 624 | 13 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net::IPAddress..ctor() | GC.Alloc |
| 10 | 544 | 8 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface.AddAddress() | GC.Alloc |
| 11 | 256 | 2 | 2 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.FireTimer() | GC.Alloc |
| 12 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 160 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 14 | 128 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync() | GC.Alloc |
| 15 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 16 | 100 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 17 | 100 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 18 | 80 | 2 | 2 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper() | GC.Alloc |
| 19 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.EnsureContingentPropertiesInitializedCore() | GC.Alloc |
| 20 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | GC.Alloc |
| 21 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | GC.Alloc |
| 22 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew() | GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 10356 | 206 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces() | GC.Alloc |
| 3 | 2048 | 16 | 8 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 4 | 1368 | 15 | 1 | Thread Pool Worker | GC.Alloc | #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces() | GC.Alloc |
| 5 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:288] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update() | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 6 | 1024 | 8 | 8 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 7 | 796 | 18 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation.MacOsStructs::sockaddr_dl.Read() | GC.Alloc |
| 8 | 720 | 18 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface..ctor() | GC.Alloc |
| 9 | 624 | 13 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net::IPAddress..ctor() | GC.Alloc |
| 10 | 544 | 8 | 3 | Thread Pool Worker | GC.Alloc | #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface.AddAddress() | GC.Alloc |
| 11 | 256 | 2 | 2 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!::Scheduler.FireTimer() | GC.Alloc |
| 12 | 256 | 2 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 13 | 186 | 8 | 4 | Thread Pool Worker | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | GC.Alloc |
| 14 | 160 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 15 | 128 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync() | GC.Alloc |
| 16 | 128 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases\|8() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 17 | 100 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 18 | 100 | 5 | 5 | Main Thread | GC.Alloc | (raw allocation attribution unavailable: rawSampleCallstackUnavailable) | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 19 | 80 | 2 | 2 | Timer-Scheduler | GC.Alloc | #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper() | GC.Alloc |
| 20 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.EnsureContingentPropertiesInitializedCore() | GC.Alloc |
| 21 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.Capture() | GC.Alloc |
| 22 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext() | GC.Alloc |
| 23 | 72 | 1 | 1 | Thread Pool Worker | GC.Alloc | #0 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew() | GC.Alloc |
| 24 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 25 | 28 | 1 | 1 | Main Thread | GC.Alloc | #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log() | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 120 | 9632 | 204 |
| 2 | 121 | 3425 | 37 |
| 3 | 119 | 2615 | 56 |
| 4 | 300 | 1076 | 8 |
| 5 | 279 | 432 | 4 |
| 6 | 249 | 432 | 4 |
| 7 | 219 | 432 | 4 |
| 8 | 191 | 432 | 4 |
| 9 | 161 | 432 | 4 |
| 10 | 82 | 432 | 4 |

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

### 2. #0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()

Bytes: 10356
Samples: 206
Frames: 3
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()
 #1 System.dll!System.Net.NetworkInformation::SystemNetworkInterface.GetNetworkInterfaces()
 #2 System.dll!System.Net.NetworkInformation::NetworkInterface.GetAllNetworkInterfaces()
 #3 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #4 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #5 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #6 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #7 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #8 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #9 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #10 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #11 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #12 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #13 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #14 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 3. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 2048
Samples: 16
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:125] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 4. #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()

Bytes: 1368
Samples: 15
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #1 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #2 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #3 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #4 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #5 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #6 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #7 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #8 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #9 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #10 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #11 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 5. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:288] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()

Bytes: 1076
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Game.Editor.MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs:288] Game.Editor.dll!Game.Editor::MatchGcAllocationCallstackCapture.Update()
 #1 [/Users/bokken/build/output/unity/unity/Editor/Mono/EditorApplication.cs:399] UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions()
```

### 6. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 1024
Samples: 8
Frames: 8
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:125] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 7. #0 System.dll!System.Net.NetworkInformation.MacOsStructs::sockaddr_dl.Read()

Bytes: 796
Samples: 18
Frames: 3
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 System.dll!System.Net.NetworkInformation.MacOsStructs::sockaddr_dl.Read()
 #1 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()
 #2 System.dll!System.Net.NetworkInformation::SystemNetworkInterface.GetNetworkInterfaces()
 #3 System.dll!System.Net.NetworkInformation::NetworkInterface.GetAllNetworkInterfaces()
 #4 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #6 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #7 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #8 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #9 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #10 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #11 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #12 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #13 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #14 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #15 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 8. #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface..ctor()

Bytes: 720
Samples: 18
Frames: 3
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface..ctor()
 #1 System.dll!System.Net.NetworkInformation::MacOsNetworkInterface..ctor()
 #2 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()
 #3 System.dll!System.Net.NetworkInformation::SystemNetworkInterface.GetNetworkInterfaces()
 #4 System.dll!System.Net.NetworkInformation::NetworkInterface.GetAllNetworkInterfaces()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #6 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #7 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #8 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #9 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #10 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #11 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #12 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #13 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #14 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #15 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #16 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 9. #0 System.dll!System.Net::IPAddress..ctor()

Bytes: 624
Samples: 13
Frames: 3
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 System.dll!System.Net::IPAddress..ctor()
 #1 System.dll!System.Net::IPAddress..ctor()
 #2 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()
 #3 System.dll!System.Net.NetworkInformation::SystemNetworkInterface.GetNetworkInterfaces()
 #4 System.dll!System.Net.NetworkInformation::NetworkInterface.GetAllNetworkInterfaces()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #6 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #7 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #8 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #9 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #10 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #11 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #12 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #13 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #14 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #15 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #16 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 10. #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface.AddAddress()

Bytes: 544
Samples: 8
Frames: 3
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 System.dll!System.Net.NetworkInformation::UnixNetworkInterface.AddAddress()
 #1 System.dll!System.Net.NetworkInformation::MacOsNetworkInterfaceAPI.GetAllNetworkInterfaces()
 #2 System.dll!System.Net.NetworkInformation::SystemNetworkInterface.GetNetworkInterfaces()
 #3 System.dll!System.Net.NetworkInformation::NetworkInterface.GetAllNetworkInterfaces()
 #4 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:127] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.GetActiveNetworkInterfaces()
 #5 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:94] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.<PollNetworkAsync>b__46_0()
 #6 mscorlib.dll!System.Threading.Tasks::Task.InnerInvoke()
 #7 mscorlib.dll!System.Threading.Tasks::Task.Execute()
 #8 mscorlib.dll!System.Threading.Tasks::Task.ExecutionContextCallback()
 #9 mscorlib.dll!System.Threading::ExecutionContext.RunInternal()
 #10 mscorlib.dll!System.Threading::ExecutionContext.Run()
 #11 mscorlib.dll!System.Threading.Tasks::Task.ExecuteWithThreadLocal()
 #12 mscorlib.dll!System.Threading.Tasks::Task.ExecuteEntry()
 #13 mscorlib.dll!System.Threading.Tasks::Task.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #14 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #15 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 11. #0 mscorlib.dll!::Scheduler.FireTimer()

Bytes: 256
Samples: 2
Frames: 2
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

### 12. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 256
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:125] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 13. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 186
Samples: 8
Frames: 4
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 14. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 160
Samples: 5
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 15. #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()

Bytes: 128
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()
 #1 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:82] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkOnTimerTick()
 #2 mscorlib.dll!::Scheduler.TimerCB()
 #3 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #4 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #5 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 16. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()

Bytes: 128
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs:348] Game.Runtime.dll!::<>c__DisplayClass9_0.<Initialize>g__UpdateSelectionRuntimePhases|8()
 #1 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs:59] Game.Runtime.dll!Game.Runtime::GameplayRuntimeUpdateCompositionSystemHelper.Update()
 #2 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:546] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.UpdateRuntime()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs:175] Game.Composition.dll!Game.Composition::MatchBootstrapCompositionSystemHelper.Update()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Composition/MatchSceneView.cs:125] Game.Composition.dll!Game.Composition::MatchSceneView.Update()
```

### 17. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 100
Samples: 5
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 18. (raw allocation attribution unavailable: rawSampleCallstackUnavailable)

Bytes: 100
Samples: 5
Frames: 5
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc

```
(raw allocation attribution unavailable: rawSampleCallstackUnavailable)
```

### 19. #0 mscorlib.dll!System.Threading::ThreadPool.QueueUserWorkItemHelper()

Bytes: 80
Samples: 2
Frames: 2
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

### 20. #0 mscorlib.dll!System.Threading.Tasks::Task.EnsureContingentPropertiesInitializedCore()

Bytes: 72
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading.Tasks::Task.EnsureContingentPropertiesInitializedCore()
 #1 mscorlib.dll!System.Threading.Tasks::Task.EnsureContingentPropertiesInitialized()
 #2 mscorlib.dll!System.Threading.Tasks::Task.set_CapturedContext()
 #3 mscorlib.dll!System.Threading.Tasks::Task.TaskConstructorCore()
 #4 mscorlib.dll!System.Threading.Tasks::Task..ctor()
 #5 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew()
 #6 mscorlib.dll!System.Threading.Tasks::Task.Run()
 #7 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()
 #8 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:82] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkOnTimerTick()
 #9 mscorlib.dll!::Scheduler.TimerCB()
 #10 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #11 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #12 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 21. #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()

Bytes: 72
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #1 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #2 mscorlib.dll!System.Threading.Tasks::Task.TaskConstructorCore()
 #3 mscorlib.dll!System.Threading.Tasks::Task..ctor()
 #4 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew()
 #5 mscorlib.dll!System.Threading.Tasks::Task.Run()
 #6 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()
 #7 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:82] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkOnTimerTick()
 #8 mscorlib.dll!::Scheduler.TimerCB()
 #9 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #10 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #11 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 22. #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()

Bytes: 72
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading::ExecutionContext.get_LogicalCallContext()
 #1 mscorlib.dll!::Reader.get_LogicalCallContext()
 #2 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #3 mscorlib.dll!System.Threading::ExecutionContext.Capture()
 #4 mscorlib.dll!System.Threading.Tasks::Task.TaskConstructorCore()
 #5 mscorlib.dll!System.Threading.Tasks::Task..ctor()
 #6 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew()
 #7 mscorlib.dll!System.Threading.Tasks::Task.Run()
 #8 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()
 #9 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:82] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkOnTimerTick()
 #10 mscorlib.dll!::Scheduler.TimerCB()
 #11 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #12 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #13 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 23. #0 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew()

Bytes: 72
Samples: 1
Frames: 1
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0 mscorlib.dll!System.Threading.Tasks::Task.InternalStartNew()
 #1 mscorlib.dll!System.Threading.Tasks::Task.Run()
 #2 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:92] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkAsync()
 #3 [./Library/PackageCache/com.unity.ai.assistant@6fee27370e6a/Modules/Unity.AI.Toolkit.Accounts/Services/States/SettingsState.cs:82] Unity.AI.Toolkit.Accounts.dll!Unity.AI.Toolkit.Accounts.Services.States::SettingsState.PollNetworkOnTimerTick()
 #4 mscorlib.dll!::Scheduler.TimerCB()
 #5 mscorlib.dll!System.Threading::QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
 #6 mscorlib.dll!System.Threading::ThreadPoolWorkQueue.Dispatch()
 #7 mscorlib.dll!System.Threading::_ThreadPoolWaitCallback.PerformWaitCallback()
```

### 24. #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()

Bytes: 48
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/UI/Shell/UIShellEcsPresentationSystem.cs:50] Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update()
```

### 25. #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log()

Bytes: 28
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update() [Invoke] > GC.Alloc

```
 #0 mscorlib.dll!System::String.Ctor()
 #1 mscorlib.dll!System::String.CreateString()
 #2 [./Library/PackageCache/com.unity.collections@a43cabe808ca/Unity.Collections/FixedString.gen.cs:2443] Unity.Collections.dll!Unity.Collections::FixedString32Bytes.ToString()
 #3 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationDiagnostics.cs:21] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationDiagnostics.Log()
 #4 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationBridgeSystemHelper.cs:45] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationBridgeSystemHelper.DrainAcceptedRequests()
 #5 [/Users/farhad/Projects/WarlineCapture/Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationRuntimeView.cs:27] Game.Runtime.dll!Game.Runtime::AudioPlaybackPresentationRuntimeView.Update()
```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Allocation bytes come from per-instance GC metadata; hierarchy ownership comes from the allocation item path; managed stacks are resolved from each item's raw profiler sample index.
- Missing or malformed raw sample metadata is recorded as an unresolved hierarchy allocation and remains inside the player-relevant budget unless its hierarchy/thread independently proves editor tooling ownership.
- Probe-backed exclusions are limited to the exact 48-byte shell callback signature and the exact 256-byte selection-panel refresh signature proven by controlled marker A/B captures. Resolved Timer-Scheduler rows are excluded only when every frame is framework-only and the repository has no matching timer API owner. Every changed, unresolved, incomplete, or unrelated gameplay row remains player-relevant.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
