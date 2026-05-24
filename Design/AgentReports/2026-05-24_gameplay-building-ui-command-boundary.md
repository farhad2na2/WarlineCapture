Lane
Gameplay

Task
Step 6: extract the UI-facing building camp/resource read-command surface out of BuildingPlacementSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs
- Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs.meta
- Assets/Game/Scripts/Systems/MenuStartupSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/MenuView.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-building-ui-command-boundary.md

Contracts touched
- Added BuildingUiCommandSystem as the boundary for MenuView camp/resource request paths.
- Updated gameplay_solid_ecs_contract.md to state that menu/camp money reads, catalog reads, request validation/commands, and pending production UI read models belong behind BuildingUiCommandSystem.
- Added GameplayArchitectureContractTests.MenuViewMustUseBuildingUiCommandBoundaryForCampAndResourceUi to block MenuView from calling BuildingPlacementSystem directly for those paths.

User-visible behavior
- Intended no behavior change.
- Camp catalog population, money display, camp request validation, camp request commands, and pending-production countdown UI still use the same underlying gameplay data and commands.

Validation run
- git diff --check
- Unity EditMode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step6-building-ui-command.log

Validation result
- Passed.
- GameplayArchitectureContractTests: 91 total, 91 passed, 0 failed.
- Result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152099783754090.xml

Known gaps
- BuildingPlacementSystem still creates the BuildingUiCommandSystem context as a temporary composition bridge.
- MenuView still uses BuildingPlacementSystem for selection, placement confirmation/cancel, destroy building, building preview, and selected-building status paths.
- BuildingPlacementSystem line count increased slightly to 2852 because the temporary context bridge was added before remaining callers are migrated off the facade.

Cross-lane impacts
- UI lane should use BuildingUiCommandSystem for camp/resource request paths going forward.
- No art, map, or scene data changed.

Next recommended task
- Step 7: extract selected-building UI read model and placement confirmation/destroy commands into a dedicated UI boundary so MenuView no longer needs BuildingPlacementSystem for selected building panels and modal actions.
