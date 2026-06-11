# Match GC Allocation Call-Stack Capture

Date: 2026-06-11 16:16:39 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Profiler frame range: 0..0
- Scanned frames with data: 1
- Scanned thread views: 88
- GC.Alloc samples: 50076
- GC.Alloc bytes from hierarchy column: 6094392
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame |
| ---: | ---: | ---: | ---: | --- | --- | --- |
| 1 | 6094392 | 50076 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) [RuntimeCityCompositionSystem.cs:178] RuntimeCityCompositionSystem:CreateStartupContext (int) |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 6094392 | 50076 |

## Call Stacks

### 1. #0  (Mono JIT Code) [RuntimeCityCompositionSystem.cs:178] RuntimeCityCompositionSystem:CreateStartupContext (int)

Bytes: 6094392  
Samples: 50076  
Frames: 1  
Thread: Main Thread

```
 #0  (Mono JIT Code) [RuntimeCityCompositionSystem.cs:178] RuntimeCityCompositionSystem:CreateStartupContext (int)
 #1  (Mono JIT Code) [RuntimeCityCompositionSystem.cs:144] RuntimeCityCompositionSystem:TryAutoSpawn (int)
 #2  (Mono JIT Code) [RuntimeCityCompositionSystem.cs:102] RuntimeCityCompositionSystem:Update (int)
 #3  (Mono JIT Code) [GameplayRuntimeUpdateSystem.cs:50] GameplayRuntimeUpdateSystem:Update (bool,RuntimeGameplayStateSystem,PerformanceDiagnosticsSystem,System.Action,BuildingRuntimeUpdateSystem,BuildingRuntimeUpdateSystem/Context,System.Action,UnityEngine.Camera,RuntimeCityCompositionSystem,RuntimeGridBlockerSystem,RuntimeDecorationSpawnerSystem,DayNightSystem,System.Action,IMatchRuntimeUi,IUnitImpostorRenderer,bool&)
 #4  (Mono JIT Code) [MatchBootstrapSystem.cs:606] MatchBootstrapSystem:UpdateRuntime (bool,RuntimeGameplayStateSystem,PerformanceDiagnosticsSystem,System.Action,BuildingRuntimeUpdateSystem,BuildingRuntimeUpdateSystem/Context,System.Action,UnityEngine.Camera,RuntimeCityCompositionSystem,RuntimeGridBlockerSystem,RuntimeDecorationSpawnerSystem,DayNightSystem,System.Action,IMatchRuntimeUi,IUnitImpostorRenderer,bool&)
 #5  (Mono JIT Code) [MatchBootstrapSystem.cs:188] MatchBootstrapSystem:Update ()
 #6  (Mono JIT Code) [MatchSceneView.cs:85] MatchSceneView:Update ()
 #7  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Battle and spike-frame call stacks still require a deterministic battle-driver capture or an interactive Profiler capture with Call Stacks -> GC.Alloc enabled.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
