# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Extract active placement input and wall-run state mutation from `BuildingPlacementSystem` into `BuildingPlacementInputSystem`.

## Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingPlacementInputSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-placement-input-system-extraction.md`

## Contracts touched
- Active placement drag state, pointer-to-cell placement movement, wall drag axis/origin expansion, committed wall-run input state, and active-placement hit testing now belong in `BuildingPlacementInputSystem`.
- `GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedInputSlice` verifies the new system boundary and rejects moving wall drag axis mutation, wall origin expansion, wall-run commit mutation, or active-placement hit testing back into `BuildingPlacementSystem`.

## User-visible behavior
No intended behavior change. Dragging placement previews, re-anchoring wall placement on click, committing valid wall runs on pointer release, idle camera follow behavior, and placement UI pointer suppression should behave the same.

## Validation run
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- `git diff --check` on the touched files
- Line count check for `BuildingPlacementSystem` and `BuildingPlacementInputSystem`

## Validation result
- `GameplayArchitectureContractTests`: passed, 73/73
- `git diff --check`: passed
- `BuildingPlacementSystem.cs`: 6353 lines after this slice
- `BuildingPlacementInputSystem.cs`: 306 lines

## Known gaps
- `BuildingPlacementSystem` still owns the private `PlacementState` facade object and adapts it into preview, validation, input, and commit systems.
- Runtime placement input was validated by focused EditMode architecture contracts, not by a PlayMode drag/placement capture.

## Cross-lane impacts
- No scene, prefab, art, UI layout, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for focused Unity test execution.

## Next recommended task
Extract selected-building and camp production request orchestration into `BuildingProductionRequestSystem`, keeping `BuildingPlacementSystem` as the UI-facing facade until the request path can move to ECS components.
