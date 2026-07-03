# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 15:59:03 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 30401
- GC.Alloc samples: 15800
- GC.Alloc bytes from hierarchy column: 1293637
- GC.Alloc samples excluding editor/tooling rows: 5
- GC.Alloc bytes excluding editor/tooling rows: 510
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 15795
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 1293127
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
| 1 | 510 | 5 | 4 | Thread Pool Worker | GC.Alloc | (no managed call stack captured) | GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 241310 | 2456 | 61 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | GC.Alloc |
| 2 | 214989 | 945 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask > GC.Alloc |
| 3 | 85147 | 1124 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 4 | 78936 | 1794 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 5 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 6 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 7 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 8 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 9 | 41332 | 764 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc |
| 10 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 11 | 28278 | 258 | 6 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 12 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 13 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 14 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 15 | 22892 | 224 | 3 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 324969 | 2019 |
| 2 | 1 | 112930 | 819 |
| 3 | 189 | 69316 | 489 |
| 4 | 97 | 44131 | 314 |
| 5 | 216 | 36762 | 329 |
| 6 | 217 | 33573 | 279 |
| 7 | 215 | 21736 | 157 |
| 8 | 99 | 19232 | 123 |
| 9 | 62 | 17310 | 95 |
| 10 | 196 | 16214 | 311 |

## Call Stacks

### 1. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 241310
Samples: 2456
Frames: 61
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

### 2. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 214989
Samples: 945
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

Bytes: 85147
Samples: 1124
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

### 4. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 78936
Samples: 1794
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

### 5. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

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

### 6. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

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

### 7. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

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

### 9. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 41332
Samples: 764
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateActiveProductionTransports > GC.Alloc

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

### 11. #0  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log_Injected (UnityEngine.LogType,UnityEngine.LogOption,UnityEngine.Bindings.ManagedSpanWrapper&,intptr)

Bytes: 28278
Samples: 258
Frames: 6
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

Bytes: 23920
Samples: 598
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

Bytes: 22892
Samples: 224
Frames: 3
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.EndUpdate > GC.Alloc

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
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
