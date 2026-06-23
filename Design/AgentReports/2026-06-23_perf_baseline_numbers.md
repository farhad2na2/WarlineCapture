# Performance Baseline Numbers

Date: 2026-06-23
Lane: Support / Architecture Performance
Capture: `/Users/farhad/Projects/WarlineCapture/ProfilerCaptures/WarlineCapture_2026-06-23_12-09-21.data`
Parsed summary: `/private/tmp/warline-profiler-capture-summary.md`

## Capture Summary

- Profiler frames scanned: 2000
- Thread views scanned: 196001
- Capture type: live Unity Editor Profiler capture from Menu -> Deploy -> Match session
- Android validation: not run, user-triggered only

## Main Findings

- The largest stalls are Editor/profiler overhead, not gameplay:
  - Frame 1901: 3825.9 ms main thread, almost entirely `EditorLoop`
  - Frame 1999: 1409.4 ms main thread, mostly `EditorLoop`, asset preload/profiler shutdown work
  - Frame 171: 1021.6 ms main thread, almost entirely `EditorLoop`
- Real gameplay spikes are smaller but actionable:
  - Frame 489: 85.3 ms main thread
    - `UnitPathfindingSystem`: 30.0 ms
    - `GameplayRuntimeUpdate.BuildingPlacement`: 26.1 ms
    - `BuildingPlacementRuntimeTick.UpdateActiveProductionTransports`: 25.9 ms
  - Frame 531: 83.6 ms main thread
    - `UnitAttackVfxRequestSystem`: 42.8 ms
    - `UnitDeathSystem`: 4.4 ms
  - Frame 1589: 85.0 ms main thread
    - `GameplayRuntimeUpdate.EndUpdate`: 26.0 ms
    - `BuildingPlacementRuntimeTick.UpdateInput`: 19.4 ms
    - `GameplayRuntimeUpdate.Selection`: 14.4 ms

## Focused Marker Totals

| Marker | Total ms | Max ms | Max frame | GC bytes | Notes |
|---|---:|---:|---:|---:|---|
| `GameplayRuntimeUpdate.Selection` | 1637.696 | 16.342 | 1598 | 6422229 | Hot managed runtime marker over capture |
| `GameplayRuntimeUpdate.BuildingPlacement` | 869.741 | 26.118 | 489 | 10487591 | Includes building production/transport/input work |
| `UnitPathfindingSystem` | 72.309 | 30.036 | 489 | 78263 | Biggest ECS pathing spike in capture |
| `DynamicOccupancyRebuildSystem` | 29.672 | 0.079 | 970 | 0 | Not a top spike in this capture |
| `Canvas.RenderOverlays` | 365.373 | 0.441 | 1971 | 0 | UI rendering is not the main bottleneck here |
| `Camera.Render` | 17.539 | 0.073 | 1805 | 0 | Camera render marker is low in this capture |
| `Gfx.WaitForGfxCommandsFromMainThread` | 30561.149 | 70.465 | 489 | 0 | Render thread waiting on main thread during gameplay spike |

## Baseline Interpretation

- Do not treat the huge `EditorLoop` frames as gameplay performance regressions.
- Use frames 489, 531, and 1589 as the initial optimization reference points.
- Stage 1 can proceed with low-risk allocation fixes, but any claimed FPS improvement needs a later compare capture.
- First runtime optimization candidates after low-risk cleanup are `UnitPathfindingSystem`, `BuildingPlacementRuntimeTick.UpdateActiveProductionTransports`, `UnitAttackVfxRequestSystem`, and selection/update end allocations.

## Validation Result

- Capture parsed successfully using Unity Editor profiler APIs.
- Temporary parser file was removed after report generation.
- No Android build was triggered.
