# PM Dispatch: Gameplay Fixed-Road Alignment

Date: 2026-05-07

## Trigger

The user flagged that previous random city generation and road-generation behavior is not aligned with the current WarlineCapture direction: tactical maps should use fixed/authored roads.

## Task Update

`Design/AgentTasks/gameplay_current.md` now includes a fixed-road alignment audit in the active gameplay task.

Gameplay must audit:

- `RuntimeCitySpawnerSystem`
- `RoadBuildSystem`
- `GridAuthoring.FillRoadCells`
- Game bootstrap paths that start runtime city/road systems
- M01 tactical binder road ownership

## Required Direction

For M01 and fixed tactical missions, road cells must come from authored metadata, especially:

- `TacticalMapDefinition`
- `Chapter01MissionTacticalRuntimeBinder.MarkSurfaceCells(...)`

Random/procedural city road chains must not overwrite or redefine tactical mission roads.

## Cross-Lane State

- Gameplay owns runtime road/city generation guards and validation.
- UI should continue visual target work.
- Support/FTUE can continue assistant runtime integration using fixed M01 anchors.
- QA/HCI should treat random/procedural road drift as a gameplay-readability and balance-risk finding.
