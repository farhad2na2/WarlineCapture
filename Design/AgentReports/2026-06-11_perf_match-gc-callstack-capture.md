# Match GC Allocation Call-Stack Capture

Date: 2026-06-11 17:14:29 UTC
Lane: Gameplay/Performance
Capture type: automated Match steady-state after Menu -> Match route

## Capture Summary

- Requested frames: 300
- Profiler frame range: 0..0
- Scanned frames with data: 1
- Scanned thread views: 90
- GC.Alloc samples: 15462
- GC.Alloc bytes from hierarchy column: 830008
- Raw load status: `rawLoaded path=/private/tmp/warline-match-gc-callstack-capture.raw`
- Raw capture: `/private/tmp/warline-match-gc-callstack-capture.raw`

## Top Allocation Sites

| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame |
| ---: | ---: | ---: | ---: | --- | --- | --- |
| 1 | 830008 | 15462 | 1 | Main Thread | GC.Alloc | #0  (Mono JIT Code) (wrapper managed-to-native) System.RuntimeType:getFullName (System.RuntimeType,bool,bool) |

## Highest Allocation Frames

| Rank | Profiler frame | Bytes | Samples |
| ---: | ---: | ---: | ---: |
| 1 | 0 | 830008 | 15462 |

## Call Stacks

### 1. #0  (Mono JIT Code) (wrapper managed-to-native) System.RuntimeType:getFullName (System.RuntimeType,bool,bool)

Bytes: 830008
Samples: 15462
Frames: 1
Thread: Main Thread

```
 #0  (Mono JIT Code) (wrapper managed-to-native) System.RuntimeType:getFullName (System.RuntimeType,bool,bool)
 #1  (Mono JIT Code) System.RuntimeType:get_AssemblyQualifiedName ()
 #2  (Mono JIT Code) [BurstRuntime.cs:104] Unity.Burst.BurstRuntime/HashCode64`1<Unity.Collections.NativeList`1<Unity.Entities.EntityArchetype>>:.cctor ()
 #3  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)
 #4  (Mono JIT Code) [SharedStatic.cs:52] Unity.Burst.SharedStatic`1<int>:GetOrCreate<Unity.Collections.NativeList`1<Unity.Entities.EntityArchetype>> (uint)
 #5  (Mono JIT Code) [NativeList.cs:87] Unity.Collections.NativeList`1<Unity.Entities.EntityArchetype>:.cctor ()
 #6  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)
 #7  (Mono JIT Code) [EntitiesProfiler+StaticData.cs:142] Unity.Entities.EntitiesProfiler/StaticData:ResetSessionMetaData ()
 #8  (Mono JIT Code) [EntitiesProfiler+StaticData.cs:87] Unity.Entities.EntitiesProfiler/StaticData:Flush ()
 #9  (Mono JIT Code) [EntitiesProfiler.cs:100] Unity.Entities.EntitiesProfiler:Update ()
 #10  (Mono JIT Code) [RuntimeApplication.cs:21] Unity.Entities.RuntimeApplication:InvokePostFrameUpdate ()
 #11  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)

```

## Coverage Notes

- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.
- Battle and spike-frame call stacks still require a deterministic battle-driver capture or an interactive Profiler capture with Call Stacks -> GC.Alloc enabled.
- Do not use this report to edit unrelated files unless they appear in the call stacks above.
