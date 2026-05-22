# Lane
Gameplay

# Task
Migrate the build-mode caller group from direct `InitialUnitsRuntimeState` access to the `RuntimeGameplayStateSystem` compatibility boundary.

# Files changed
- `Assets/Game/Scripts/UI/RoadBuildSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

# Contracts touched
- Extended `GameplayArchitectureContractTests.RtsSelectionSystemMustUseRuntimeGameplayStateBoundary` so `RoadBuildSystem`, `BuildingPlacementSystem`, and `GameBootstrap` must use `RuntimeGameplayStateSystem` for migrated runtime flags.
- Updated the RTS selection responsibility audit with the twelfth extraction slice.

# User-visible behavior
No intended gameplay behavior change. Build mode, road build mode, building placement, play start, and runtime HUD behavior should continue through the same public interactions while state access moves behind the ECS-compatible boundary.

# Validation run
- `git diff --check -- Assets/Game/Scripts/UI/RoadBuildSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- Unity EditMode `RuntimeGameplayStateSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `Chapter01M01PlayableRuntimeTests.RoadBuildSetBuildMode_RejectsM01WithoutEnteringBuildMode`
- Unity EditMode `BuildingPlacementValidationSystemTests`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests`
- Full Unity EditMode `Chapter01M01PlayableRuntimeTests`

# Validation result
- `git diff --check`: passed.
- `RuntimeGameplayStateSystemTests`: passed 4/4.
- `GameplayArchitectureContractTests`: passed 38/38.
- `Chapter01M01PlayableRuntimeTests.RoadBuildSetBuildMode_RejectsM01WithoutEnteringBuildMode`: passed 1/1.
- `BuildingPlacementValidationSystemTests`: passed 4/4.
- `BattleHudGameplayBridgeConnectionTests`: passed 6/6.
- Full `Chapter01M01PlayableRuntimeTests`: 8/9 passed. `RoadBuildSetBuildMode_RejectsM01WithoutEnteringBuildMode` passed; `Initialize_CreatesFriendlySquadAndHostilePatrolFromMetadataAnchors` failed at `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs:83`. This failure appears unrelated to the runtime-state caller migration and should be tracked separately if reproducible.

# Known gaps
- `RuntimeGameplayStateSystem` remains a compatibility bridge and still mirrors legacy static `InitialUnitsRuntimeState` so older callers continue working during migration.
- `GameBootstrap` still assigns `InitialUnitsRuntimeState.WorldCamera`; that object-reference state was intentionally outside this migrated flag slice.
- `RoadBuildSystem.SetBuildMode` remains a static legacy API, but it now uses a local `RuntimeGameplayStateSystem` instance instead of direct migrated static fields.

# Cross-lane impacts
- UI/HUD code should continue to behave the same, but migrated build-mode flags now flow through the gameplay runtime-state boundary.
- QA should verify build mode, road mode, building placement cancellation, HUD command mode clearing, and M01 build rejection in-editor.

# Next recommended task
Continue the runtime-state migration by addressing the remaining non-migrated `InitialUnitsRuntimeState` usages, starting with camera object reference state (`WorldCamera`) and any remaining diagnostics/static compatibility fields that need ECS singleton ownership.
