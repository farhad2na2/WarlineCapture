# Match GC Allocation Call-Stack Capture

Date: 2026-06-11 21:34:15 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..0
- Scanned frames with data: 1
- Scanned thread views: 89
- GC.Alloc samples: 969
- GC.Alloc bytes from hierarchy column: 39180
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Editor live conversion systems disabled before warmup: 2
- Runtime allocation probe:
  - `UIShellEcsPresentationSystem.Update`: 0 bytes / 0 allocating updates / 300 total updates.
  - `MenuBootstrapView.Update`: 0 bytes / 0 allocating updates / 300 total updates.

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 14352 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 2 | 12000 | 300 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate > GC.Alloc |
| 3 | 9568 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 5 | 400 | 10 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > HierarchyWindow.Tick > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallHierarchyHasChanged() [Invoke] > EditorApplication.hierarchyChanged: MapSurfacePreviewOverlaySystem.ClearPreview > GC.Alloc |
| 6 | 280 | 7 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitAttackSystem > GC.Alloc |
| 7 | 240 | 6 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AICombatOrderSystem > GC.Alloc |
| 8 | 240 | 5 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World GroundMissileFlyingRocketVisualSystem > Mono.JIT > GC.Alloc |
| 9 | 224 | 7 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > MonoCompiler.Tick > GC.Alloc |
| 10 | 168 | 6 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.Scripts.UI::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc |
| 11 | 144 | 3 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AirMissileLauncherTargetAcquisitionSystem > Mono.JIT > GC.Alloc |
| 12 | 140 | 7 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc |
| 13 | 140 | 7 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc |
| 14 | 88 | 2 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > MainMenuPlayUI.MinimapUpdate > GC.Alloc |
| 15 | 48 | 1 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update () | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 39180 | 969 |

## Call Stacks

### 1. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 14352
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 12000
Samples: 300
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 9568
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 1076
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 400
Samples: 10
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > HierarchyWindow.Tick > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallHierarchyHasChanged() [Invoke] > EditorApplication.hierarchyChanged: MapSurfacePreviewOverlaySystem.ClearPreview > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 280
Samples: 7
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitAttackSystem > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 240
Samples: 6
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AICombatOrderSystem > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 240
Samples: 5
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World GroundMissileFlyingRocketVisualSystem > Mono.JIT > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 224
Samples: 7
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 168
Samples: 6
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!Game.Scripts.UI::MenuDiagnosticsView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 144
Samples: 3
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AirMissileLauncherTargetAcquisitionSystem > Mono.JIT > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 140
Samples: 7
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.CoreModule.dll!UnityEditor.Scripting.ScriptCompilation::EditorCompilationInterface.TickCompilationPipeline() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 140
Samples: 7
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > MonoCompiler.Tick > UnityEditor.Android.Extensions.dll!UnityEditor.Android::AndroidPlatformBuildSettings.get_androidBuildSubtarget() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 88
Samples: 2
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GameplayRuntimeUpdate.MainMenu > MainMenuPlayUI.MinimapUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()

Bytes: 48
Samples: 1
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [MatchGcAllocationCallstackCapture.cs:245] MatchGcAllocationCallstackCapture:Update ()
 #1  (Mono JIT Code) [EditorApplication.cs:401] UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()
 #2  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
