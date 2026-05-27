Lane
Gameplay

Task
UnitPathfindingSystem refactor roadmap step 2: establish a conservative performance baseline before code extraction.

Files changed
- Design/Architecture/unit_pathfinding_system_refactor_roadmap.md
- Design/AgentReports/2026-05-27_gameplay-unit-pathfinding-refactor-baseline.md

Contracts touched
- UnitPathfindingSystem refactor roadmap performance validation gate.

User-visible behavior
- None. Runtime pathfinding code was not changed.

Validation run
- Existing runtime FPS probe in validation clone:
  - Project: /Users/farhad/Projects/WarlineCapture-CodexUnity1
  - Unity: 6000.4.0f1 batchmode
  - Method: RuntimeFpsPlayButtonProbe.Run
  - Log: /private/tmp/warlinecapture-unit-pathfinding-fps-baseline-step2.log
  - Report: /private/tmp/warlinecapture-runtime-fps-probe.json

Validation result
- Probe result: completed.
- Game button clicked: true.
- FPS samples: 13760.
- Average FPS: 309.04.
- Minimum FPS: 85.82.
- Maximum FPS: 327.59.
- FrameRateDiag logs: 0.
- Captured PerfDiag samples included `Default World UnitPathfindingSystem=0.0ms`.
- Captured unit counts in slow-update samples: 240-259.

Known gaps
- This is an editor batchmode smoke baseline, not Android/device rendering acceptance.
- The existing probe does not issue a manual group move or long-distance move.
- Current always-on diagnostics do not emit path request count, adaptive budget, pending wall time, or apply time without enabling disabled path logs. Those values must be exposed later through structured metrics, not by turning on hot-path console logging.
- Captured slow frames were from BuildingPlacement and RuntimeCity startup work, not UnitPathfindingSystem.

Cross-lane impacts
- None. This baseline is for Gameplay refactor sequencing only.

Next recommended task
- Continue UnitPathfindingSystem roadmap step 3: freeze public/static surface and inventory external reads of `UnitPathfindingSystem.HasPendingPathJob`.
