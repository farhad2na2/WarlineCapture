# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Extract placement commit expansion and preview consumption responsibilities from `BuildingPlacementSystem` into `BuildingPlacementCommitSystem`.

## Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-placement-commit-system-extraction.md`

## Contracts touched
- Placement commit expansion, wall segment runtime creation, committed placement preview consumption, and post-placement auto-select policy now belong in `BuildingPlacementCommitSystem`.
- `GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedCommitSlice` verifies the new system boundary and rejects moving wall commit expansion, preview consumption, or auto-select policy back into `BuildingPlacementSystem`.

## User-visible behavior
No intended behavior change. Confirmed building placement should still spend dollars, record runtime stats, notify minimap refresh, exit build mode, create wall segments, consume previews, and auto-select production-capable buildings as before.

## Validation run
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- `git diff --check` on the touched files
- Line count check for `BuildingPlacementSystem` and `BuildingPlacementCommitSystem`

## Validation result
- `GameplayArchitectureContractTests`: passed, 72/72
- `git diff --check`: passed
- `BuildingPlacementSystem.cs`: 6554 lines after this slice
- `BuildingPlacementCommitSystem.cs`: 192 lines

## Known gaps
- `BuildingPlacementSystem` still adapts its private placement state into commit data; extracting active placement state/input movement would reduce that adapter code further.
- Direct editor behavior tests for `BuildingPlacementCommitSystem` were not kept because the system intentionally depends on the internal `RuntimeBuildingData` boundary; widening that runtime data just for tests would weaken the architecture.
- Runtime placement behavior was validated by focused EditMode architecture contracts, not by a PlayMode placement capture.

## Cross-lane impacts
- No scene, prefab, art, UI layout, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for focused Unity test execution.

## Next recommended task
Extract active placement input/state mutation into `BuildingPlacementInputSystem`, then revisit whether `PlacementState` can become a narrow ECS request/preview component instead of a private facade object.
