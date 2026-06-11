# Match GC Allocation Call-Stack Capture

Date: 2026-06-11 19:43:31 UTC
Lane: Gameplay/Performance
Capture type: automated Match battle-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Warm-up frames before capture: 180
- Profiler frame range: 0..0
- Scanned frames with data: 1
- Scanned thread views: 87
- GC.Alloc samples: 7579
- GC.Alloc bytes from hierarchy column: 415072
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture-battle.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture-battle.raw`

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |
| ---: | ---: | ---: | ---: | --- | --- | --- | --- |
| 1 | 183586 | 2093 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > InitializationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.InitializationSystemGroup > Default World Unity.Scenes.Editor.LiveConversionEditorSystemGroup > Default World Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem > GC.Alloc |
| 2 | 91024 | 1797 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GC.Alloc |
| 3 | 33488 | 897 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MenuBootstrapView.Update() [Invoke] > GC.Alloc |
| 4 | 28106 | 897 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc |
| 5 | 14352 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc |
| 6 | 12000 | 300 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate > GC.Alloc |
| 7 | 11960 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World InitialUnitsSpawnSystem > GC.Alloc |
| 8 | 11960 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AICombatOrderSystem > GC.Alloc |
| 9 | 11960 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitDeathSystem > GC.Alloc |
| 10 | 11960 | 299 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitRespawnSystem > GC.Alloc |
| 11 | 1076 | 8 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc |
| 12 | 614 | 7 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > InitializationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.InitializationSystemGroup > Default World Unity.Scenes.Editor.LiveConversionEditorSystemGroup > Default World Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem > GC.Alloc |
| 13 | 560 | 14 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > HierarchyWindow.Tick > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallHierarchyHasChanged() [Invoke] > EditorApplication.hierarchyChanged: MapSurfacePreviewOverlaySystem.ClearPreview > GC.Alloc |
| 14 | 360 | 9 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitAttackSystem > GC.Alloc |
| 15 | 304 | 6 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode) | Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GC.Alloc |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 415072 | 7579 |

## Call Stacks

### 1. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 183586
Samples: 2093
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > InitializationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.InitializationSystemGroup > Default World Unity.Scenes.Editor.LiveConversionEditorSystemGroup > Default World Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 2. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 91024
Samples: 1797
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 3. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 33488
Samples: 897
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MenuBootstrapView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 4. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 28106
Samples: 897
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellLoadingProgressView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 5. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 14352
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.UI.Runtime.dll!::UIShellEcsPresentationSystem.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 6. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 12000
Samples: 300
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: UnityEditor.PackageManager.UI.Internal.ApplicationProxy.OnUpdate > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 7. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 11960
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World InitialUnitsSpawnSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 8. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 11960
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World AICombatOrderSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 9. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 11960
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitDeathSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 10. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 11960
Samples: 299
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitRespawnSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 11. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 1076
Samples: 8
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.TickSceneTracker > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallUpdateFunctions() [Invoke] > EditorApplication.update: MatchGcAllocationCallstackCapture.Update > Mono.JIT > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 12. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 614
Samples: 7
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > InitializationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.InitializationSystemGroup > Default World Unity.Scenes.Editor.LiveConversionEditorSystemGroup > Default World Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 13. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 560
Samples: 14
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > HierarchyWindow.Tick > UnityEditor.CoreModule.dll!UnityEditor::EditorApplication.Internal_CallHierarchyHasChanged() [Invoke] > EditorApplication.hierarchyChanged: MapSurfacePreviewOverlaySystem.ClearPreview > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 14. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 360
Samples: 9
Frames: 1
Thread: Main Thread
Hierarchy path: Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > SimulationSystemGroup > UnityEngine.CoreModule.dll!::UpdateFunction.Invoke() [Invoke] > Default World Unity.Entities.SimulationSystemGroup > Default World UnitAttackSystem > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

### 15. #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)

Bytes: 304
Samples: 6
Frames: 1
Thread: Main Thread
Hierarchy path: Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate > Game.Composition.dll!::MatchSceneView.Update() [Invoke] > GC.Alloc

```
 #0  (Mono JIT Code) [LiveConversionConnection.cs:354] Unity.Scenes.Editor.LiveConversionConnection:Update (System.Collections.Generic.List`1<Unity.Scenes.LiveConversionChangeSet>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Collections.NativeList`1<Unity.Entities.Hash128>,Unity.Scenes.LiveConversionMode)
 #1  (Mono JIT Code) [EditorSubSceneLiveConversionSystem.cs:62] Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem:OnUpdate ()
 #2  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #3  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #4  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #5  (Mono JIT Code) [LiveConversionEditorSystemGroup.cs:15] Unity.Scenes.Editor.LiveConversionEditorSystemGroup:OnUpdate ()
 #6  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #7  (Mono JIT Code) [ComponentSystemGroup.cs:734] Unity.Entities.ComponentSystemGroup:UpdateAllSystems ()
 #8  (Mono JIT Code) [ComponentSystemGroup.cs:687] Unity.Entities.ComponentSystemGroup:OnUpdate ()
 #9  (Mono JIT Code) [DefaultWorld.cs:169] Unity.Entities.InitializationSystemGroup:OnUpdate ()
 #10  (Mono JIT Code) [SystemBase.cs:404] Unity.Entities.SystemBase:Update ()
 #11  (Mono JIT Code) [ScriptBehaviourUpdateOrder.cs:523] Unity.Entities.ScriptBehaviourUpdateOrder/DummyDelegateWrapper:TriggerUpdate ()
 #12  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.
- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
