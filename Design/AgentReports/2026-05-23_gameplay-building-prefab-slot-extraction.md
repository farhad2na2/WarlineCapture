# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Extract spawn prefab/entity resolution and production slot discovery/reservation seams out of `BuildingPlacementSystem`.

## Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionSlotSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingProductionSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/BuildingProductionSystemTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-23_gameplay-building-prefab-slot-extraction.md`

## Contracts touched
- Spawn prefab registry lookup, prefab entity resolution, and live-unit prefab fallback lookup now belong in `BuildingSpawnPrefabSystem`.
- Production slot discovery, pending-slot reservation checks, slot occupancy cleanup, and production slot reservation now belong in `BuildingProductionSlotSystem`.
- `GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedProductionSlice` now checks for both new systems and rejects moving those private methods back into `BuildingPlacementSystem`.

## User-visible behavior
No intended behavior change. Building production should still resolve unit prefab entities through the same registry/query/live-unit fallback order and reserve/use production spawn slots with the same occupancy cleanup behavior.

## Validation run
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity EditMode `BuildingProductionSystemTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- `git diff --check` on the touched files
- Line count check for the building production/spawn system files

## Validation result
- `GameplayArchitectureContractTests`: passed, 70/70
- `BuildingProductionSystemTests`: passed, 10/10
- `git diff --check`: passed
- `BuildingPlacementSystem.cs`: 6861 lines after this slice

## Known gaps
- `BuildingPlacementSystem` is still large and still owns production queue orchestration.
- `BuildingSpawnPrefabSystem` intentionally uses internal runtime contexts rather than reflection or singleton access.
- The slot-system direct behavior test was not kept because testing internal runtime nested types by reflection would violate the architecture direction; the contract test guards the ownership boundary instead.

## Cross-lane impacts
- No scene, prefab, art, UI layout, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for focused Unity test execution.

## Next recommended task
Extract production queue orchestration from `BuildingPlacementSystem` into a narrow `BuildingProductionQueueSystem`, leaving `BuildingPlacementSystem` as the caller that supplies time, entity manager, and grid context.
