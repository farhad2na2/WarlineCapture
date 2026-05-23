# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Extract the produced-unit spawn slice from `BuildingPlacementSystem` into `BuildingSpawnSystem` as step 1 of the next building architecture shrink pass.

## Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingProductionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/BuildingProductionSystemTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-spawn-system-extraction.md`

## Contracts touched
- Produced-unit spawn placement, recent spawn reservations, strict spawn-cell search, dynamic occupancy reservation, helipad spawn fallback, and spawned ECS unit initialization now belong to `BuildingSpawnSystem`.
- `BuildingPlacementSystem` keeps only the compatibility wrapper and context wiring for this slice.
- `GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedProductionSlice` now rejects moving the extracted spawn methods back into `BuildingPlacementSystem`.

## User-visible behavior
No intended behavior change. Produced units should still spawn from completed building production using the same cell search, helipad fallback, reservation, occupancy, and ECS component initialization behavior.

## Validation run
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity EditMode `BuildingProductionSystemTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Line count check for `BuildingPlacementSystem`, `BuildingSpawnSystem`, `BuildingProductionTransportSystem`, and `BuildingProductionSystem`

## Validation result
- `GameplayArchitectureContractTests`: passed, 70/70
- `BuildingProductionSystemTests`: passed, 11/11
- `BuildingPlacementSystem.cs`: 7002 lines after this slice
- Extracted production/spawn support files now hold 2178 lines combined:
  - `BuildingSpawnSystem.cs`: 898
  - `BuildingProductionTransportSystem.cs`: 769
  - `BuildingProductionSystem.cs`: 511

## Known gaps
- `BuildingPlacementSystem` is still large at 7002 lines.
- Spawn prefab entity resolution and production slot discovery still use callbacks into `BuildingPlacementSystem`; those are good next extraction seams.
- This was an architecture/refactor validation pass, not a visual gameplay capture pass.

## Cross-lane impacts
- No scene, prefab, art, UI layout, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for focused Unity test execution.

## Next recommended task
Extract the spawn prefab/entity resolution seam next, then extract production slot discovery/reservation view data after that. Those two slices should shrink `BuildingPlacementSystem` further without changing runtime behavior.
