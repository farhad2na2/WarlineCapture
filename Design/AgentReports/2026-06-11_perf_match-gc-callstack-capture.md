# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 18:30:06 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 30702
- GC.Alloc samples: 13241
- GC.Alloc bytes from hierarchy column: 1402131
- GC.Alloc samples excluding editor/tooling rows: 7
- GC.Alloc bytes excluding editor/tooling rows: 736
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 13234
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 1401395
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.
- Runtime allocation probe assertion: Passed.

## Top Allocation Sites Excluding Editor/Tooling Rows

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 736 | 7 | 4 | Thread Pool Worker | GC.Alloc | (no managed call stack captured) | GC.Alloc |

## Top Editor/Tooling Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 417183 | 1907 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 2 | 216018 | 968 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 3 | 190344 | 1647 | 36 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 7 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 8 | 36464 | 8 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc |
| 9 | 30080 | 752 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 10 | 27198 | 246 | 3 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 11 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 12 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 13 | 26072 | 617 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 15 | 18234 | 4 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc |

## Top Allocation Sites (Raw)

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 417183 | 1907 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 2 | 216018 | 968 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 3 | 190344 | 1647 | 36 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | GC.Alloc |
| 4 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 5 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 6 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 7 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 8 | 36464 | 8 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc |
| 9 | 30080 | 752 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 10 | 27198 | 246 | 3 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 11 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 12 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 13 | 26072 | 617 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 15 | 18234 | 4 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 1 | 479545 | 2020 |
| 2 | 0 | 244943 | 1008 |
| 3 | 257 | 67112 | 480 |
| 4 | 36 | 48081 | 545 |
| 5 | 35 | 46106 | 234 |
| 6 | 93 | 31546 | 242 |
| 7 | 256 | 18482 | 176 |
| 8 | 258 | 15901 | 185 |
| 9 | 34 | 8686 | 84 |
| 10 | 144 | 7116 | 111 |

## Call Stacks

### 1. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 417183
Samples: 1907
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 216018
Samples: 968
Frames: 1
Thread: Main Thread
Hierarchy path: EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 190344
Samples: 1647
Frames: 36
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

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
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 36464
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 30080
Samples: 752
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 27198
Samples: 246
Frames: 3
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 26312
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 26312
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 26072
Samples: 617
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 18234
Samples: 4
Frames: 1
Thread: Main Thread
Hierarchy path: EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > LogStringToConsole > GC.Alloc

```
 #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)
 #1  (Mono JIT Code) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #2  (Mono JIT Code) [DebugLogHandler.cs:9] UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
 #3  (Mono JIT Code) [Logger.cs:60] UnityEngine.Logger:Log (UnityEngine.LogType,object)
 #4  (Mono JIT Code) [Debug.bindings.cs:123] UnityEngine.Debug:Log (object)
 #5  (Mono JIT Code) [TraceSinks.cs:313] Unity.AI.Tracing.ConsoleSink:LogToConsole (string,string,System.Exception)
 #6  (Mono JIT Code) [TraceSinks.cs:277] Unity.AI.Tracing.ConsoleSink:Write (Unity.AI.Tracing.TraceEvent)
 #7  (Mono JIT Code) [TraceWriter.cs:373] Unity.AI.Tracing.TraceWriter:WriteEventInternal (string,string,Unity.AI.Tracing.TraceEventOptions,string,System.Nullable`1<int>)
 #8  (Mono JIT Code) [TraceWriter.cs:297] Unity.AI.Tracing.TraceWriter:Event (string,Unity.AI.Tracing.TraceEventOptions)
 #9  (Mono JIT Code) [TraceLogger.cs:105] Unity.AI.Tracing.TraceLogger:Emit (string,string,string,string,Unity.AI.Tracing.TraceEventOptions,object,System.Exception)
 #10  (Mono JIT Code) [TraceLogger.cs:35] Unity.AI.Tracing.TraceLogger:Info (string,string,string,Unity.AI.Tracing.TraceEventOptions)
 #11  (Mono JIT Code) [McpLog.cs:74] Unity.AI.MCP.Editor.Helpers.McpLog/<>c__DisplayClass8_0:<LogDelayed>b__0 ()
 #12  (Mono JIT Code) [EditorTask.cs:375] Unity.AI.Toolkit.EditorTask/<>c__DisplayClass21_0:<DelayCall>b__0 ()
 #13  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #14  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Editor/tooling rows include Burst compiler threads plus Unity AI/MCP/Tracing frames. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
