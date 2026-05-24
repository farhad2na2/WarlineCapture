Lane
Gameplay

Task
Step 7: route selected-building UI read model and modal building commands through BuildingUiCommandSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/MenuView.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-building-ui-selected-boundary.md

Contracts touched
- Expanded BuildingUiCommandSystem to include selected-building active state, selected-building display name, selected-building health, selected-building preview prefab, placement confirm, and selected-building delete commands.
- Updated gameplay_solid_ecs_contract.md so selected-building display/health/preview reads and selected-building modal confirm/destroy commands belong behind BuildingUiCommandSystem.
- Expanded GameplayArchitectureContractTests.MenuViewMustUseBuildingUiCommandBoundaryForBuildingUi to block MenuView from calling BuildingPlacementSystem for those selected-building UI paths.

User-visible behavior
- Intended no behavior change.
- Destroy confirmation, building placement confirmation, selected building portrait/name, and selected building health slider still use the same underlying data and commands.

Validation run
- git diff --check
- Unity EditMode in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step7-building-ui-selected.log

Validation result
- Passed.
- GameplayArchitectureContractTests: 91 total, 91 passed, 0 failed.
- Result XML: /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152111594202050.xml

Known gaps
- BuildingPlacementSystem still creates the BuildingUiCommandSystem context as a temporary bridge.
- MenuView still calls BuildingPlacementSystem for building role/owner/city flags, live-unit preview prefab resolution, and visible-selectable-building checks.
- RuntimeBuildingEntityLink and RTSSelectionSystem still depend on BuildingPlacementSystem.

Cross-lane impacts
- UI lane should keep using BuildingUiCommandSystem for selected-building UI and camp/resource request paths.
- No scene, art, or balance data changed.

Next recommended task
- Step 8: extract MenuView's remaining building query/read paths: IsRuntimeBuildingWall, IsRuntimeBuildingCityGenerated, TryGetRuntimeBuildingOwnerFaction, HasVisibleSelectableBuilding, and live-unit preview prefab resolution.
