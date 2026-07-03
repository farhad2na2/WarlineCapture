# Match GC Allocation Call-Stack Capture

Date: 2026-07-03 18:15:33 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..300
- Scanned frames with data: 301
- Scanned thread views: 31304
- GC.Alloc samples: 12779
- GC.Alloc bytes from hierarchy column: 903267
- GC.Alloc samples excluding editor/tooling rows: 3
- GC.Alloc bytes excluding editor/tooling rows: 272
- Editor/tooling GC.Alloc samples excluded from player-relevant rows: 12776
- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: 902995
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
| 1 | 272 | 3 | 3 | Timer-Scheduler | GC.Alloc | (no managed call stack captured) | GC.Alloc |

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 273212 | 2723 | 66 | Thread Pool Worker | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | GC.Alloc |
| 2 | 84979 | 1122 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc |
| 3 | 76544 | 2093 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc |
| 4 | 76544 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc |
| 5 | 65472 | 939 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc |
| 6 | 45448 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 7 | 38272 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc |
| 8 | 32991 | 301 | 7 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc |
| 9 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc |
| 10 | 26312 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc |
| 11 | 25040 | 626 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc |
| 12 | 23920 | 598 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc |
| 13 | 14352 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 14 | 13156 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc |
| 15 | 11960 | 299 | 299 | Main Thread | GC.Alloc | #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 122 | 85475 | 622 |
| 2 | 120 | 82118 | 1052 |
| 3 | 141 | 21252 | 263 |
| 4 | 168 | 19044 | 121 |
| 5 | 22 | 18998 | 119 |
| 6 | 147 | 18972 | 118 |
| 7 | 2 | 18956 | 118 |
| 8 | 140 | 18876 | 116 |
| 9 | 289 | 17842 | 101 |
| 10 | 290 | 16839 | 198 |

## Call Stacks

### 1. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 273212
Samples: 2723
Frames: 66
Thread: Thread Pool Worker
Hierarchy path: GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 84979
Samples: 1122
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.BuildingPlacement > BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 76544
Samples: 2093
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 76544
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Panel > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 65472
Samples: 939
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.AIBuildPlannerSystem > Burst.Compiler.IL.dll!Burst.Compiler.IL.Jit::JitCompilerService.CompileInternal() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 45448
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 38272
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.FocusedReadModel > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 32991
Samples: 301
Frames: 7
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 26312
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.CommandFlush > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 26312
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.TacticalCamera > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 25040
Samples: 626
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.Selection > GameplayRuntimeUpdate.Selection.Camera > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 23920
Samples: 598
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World Game.Runtime.TransportBoardingCommandSystem > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 14352
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 13156
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.UI.Runtime::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)

Bytes: 11960
Samples: 299
Frames: 299
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!Game.Composition::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.RoadBuild > GC.Alloc

```
 #0  (Mono JIT Code) System.Text.StringBuilder:.ctor (int,int)
 #1  (Mono JIT Code) System.Text.StringBuilder:.ctor (int)
 #2  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObjectInternal (object,System.Type,Newtonsoft.Json.JsonSerializer)
 #3  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object,System.Type,Newtonsoft.Json.JsonSerializerSettings)
 #4  (Mono JIT Code) Newtonsoft.Json.JsonConvert:SerializeObject (object)
 #5  (Mono JIT Code) [Bridge.cs:1730] Unity.AI.MCP.Editor.Bridge/<ExecuteCommandAsync>d__75:MoveNext ()
 #6  (Mono JIT Code) Unity.AI.MCP.Editor.Bridge:ExecuteCommandAsync (Unity.AI.MCP.Editor.Models.Command,Unity.AI.MCP.Editor.Connection.IConnectionTransport,System.Threading.CancellationToken)
 #7  (Mono JIT Code) [Bridge.cs:1537] Unity.AI.MCP.Editor.Bridge:ProcessCommands ()
 #8  (Mono JIT Code) [EditorApplication.cs:403] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #9  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
