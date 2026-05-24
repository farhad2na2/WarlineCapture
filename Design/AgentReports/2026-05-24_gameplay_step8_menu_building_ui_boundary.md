# Lane
Gameplay

# Task
Step 8 architecture refactor: remove the remaining `MenuView` runtime dependency on the `BuildingPlacementSystem` facade by routing building UI reads and placement/session commands through `BuildingUiCommandSystem`.

# Files changed
- `Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs`
- `Assets/Game/Scripts/Systems/MenuStartupSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

# Contracts touched
- Extended the Gameplay SOLID/ECS contract so building UI minimap flags, visible-building selection checks, live-unit preview prefab resolution, and placement/session UI commands must go through `BuildingUiCommandSystem`.
- Added architecture contract coverage that `MenuView` must not hold or receive a `BuildingPlacementSystem` facade instance.
- Updated PlayMode test setup calls for the narrowed `MenuView.Init` signature.

# User-visible behavior
No intended behavior change. Menu, camp, selected-building, minimap, placement cancel/focus/clear, and build-mode exit behavior should remain the same, but `MenuView` now receives a narrow UI boundary instead of the large placement facade.

# Validation run
`/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step8-building-ui-no-facade.log`

# Validation result
Passed. `GameplayArchitectureContractTests` completed 91/91 passing in `/Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152120933832640.xml`.

# Known gaps
- `MenuView` no longer holds or receives a `BuildingPlacementSystem` instance, but it still references nested DTO/enum contracts such as `BuildingPlacementSystem.PendingProductionUiEntry` and `BuildingPlacementSystem.CampRequestFailure`.
- `BuildingPlacementSystem` remains a temporary compatibility facade for several public contracts. Current line count is 2867.

# Cross-lane impacts
UI lane should continue using `BuildingUiCommandSystem` for building UI reads/commands. No art, map, AI tuning, or economy behavior was intentionally changed.

# Next recommended task
Extract the remaining nested UI/data contracts from `BuildingPlacementSystem` into standalone ECS/UI contract types, starting with `PendingProductionUiEntry` and `CampRequestFailure`, then migrate `MenuView` and `BuildingUiCommandSystem` to those standalone types.
