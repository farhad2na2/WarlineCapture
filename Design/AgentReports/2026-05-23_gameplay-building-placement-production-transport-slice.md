# Lane
Gameplay

# Task
Step 2: Keep shrinking `BuildingPlacementSystem.cs` by moving production duration/transport policy into `BuildingProductionSystem` and active production transport visual/update behavior into `BuildingProductionTransportSystem`.

# Files changed
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs.meta`
- `Assets/Tests/Editor/BuildingProductionSystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Updated the gameplay SOLID/ECS contract so production duration, transport settings/fallback policy, transport unit classification, and transport launch delay math belong in `BuildingProductionSystem`.
- Updated the gameplay SOLID/ECS contract so active production transport visual state, arrival/drop/departure updates, transport lanes, transport drop visuals, and transport visual helpers belong in `BuildingProductionTransportSystem`.
- Extended `GameplayArchitectureContractTests.BuildingPlacementSystemMustDelegateExtractedProductionSlice` to reject production duration, transport policy, active transport update, transport lane, and drop-visual helpers returning to `BuildingPlacementSystem`.

# User-visible behavior
- No intended gameplay behavior change.
- Production duration, configured transport pickup, helicopter/plane fallback, large-vehicle plane fallback, and helicopter classification are now resolved by `BuildingProductionSystem`.
- Active production transport creation/update, aircraft arrival/departure, drop visuals, lane offsets, rope visuals, door animation, and temporary drop pose setup are now delegated to `BuildingProductionTransportSystem`.
- ECS unit spawn placement remains in `BuildingPlacementSystem` through callbacks passed to the transport system.

# Validation run
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs Assets/Game/Scripts/Systems/BuildingProductionTransportSystem.cs.meta Assets/Game/Scripts/Systems/BuildingProductionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/Editor/BuildingProductionSystemTests.cs Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity EditMode in `WarlineCapture-CodexUnity1`: `BuildingProductionSystemTests`
- Unity EditMode in `WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`

# Validation result
- `git diff --check`: passed.
- `BuildingProductionSystemTests`: passed, 11/11.
- `GameplayArchitectureContractTests`: passed, 70/70.

# Known gaps
- `BuildingPlacementSystem.cs` is still large: 7692 lines after this continued slice.
- Spawn placement and ECS unit instantiation are still in `BuildingPlacementSystem` and should be extracted in later slices.
- `BuildingProductionTransportSystem` currently uses callbacks into the legacy facade for runway lookup, spawn placement, produced-unit movement target, and produced-unit rotation. Those callbacks should shrink as spawn/ECS instantiation moves to a dedicated system.
- Main worktree contains unrelated UI/art/editor dirty files from other lanes; they were not touched.

# Cross-lane impacts
- None expected. This was a gameplay architecture/internal refactor with focused EditMode validation.

# Next recommended task
Continue shrinking `BuildingPlacementSystem.cs` by extracting production spawn placement and ECS produced-unit instantiation into a narrower gameplay `*System`, leaving the facade to delegate.
