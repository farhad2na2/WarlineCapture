Lane
Gameplay

Task
Extract building definition/prefab metadata ownership from BuildingPlacementSystem into BuildingDefinitionSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs
- Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-definition-system-extraction.md

Contracts touched
- Added BuildingDefinitionSystem ownership to the gameplay SOLID/ECS contract.
- Added GameplayArchitectureContractTests coverage preventing definition metadata dictionaries, runtime metadata cache, prefab alias lookup, production spawn metadata, prefab bounds helpers, and definition construction helpers from returning to BuildingPlacementSystem.

User-visible behavior
- No intended user-visible gameplay change.
- Building spawnable lookup, unit spawn prefab alias lookup, configured building definitions, runtime building definition construction, prefab bounds reads, and production-slot read helpers now route through BuildingDefinitionSystem.

Validation run
- `git diff --check`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-building-definition-system-tests.log`

Validation result
- `git diff --check` passed.
- Unity EditMode `GameplayArchitectureContractTests` passed: 78/78.

Known gaps
- BuildingPlacementSystem is still a large facade at 4,852 lines and still owns active placement lifecycle plus remaining migration debt.
- BuildingDefinitionSystem is intentionally narrow for definition/prefab metadata; broader authored config migration can continue later if needed.

Cross-lane impacts
- UI/building shop paths continue using the same public BuildingPlacementSystem facade.
- Art/prefab authoring contract is unchanged; existing BuildingDefinitionAuthoring and UnitGridAuthoring metadata still drives display names, footprints, production slots, prices, and requestability.

Next recommended task
- Continue shrinking BuildingPlacementSystem by extracting active placement lifecycle/state or the remaining grid/placement math that is not yet owned by BuildingPlacementValidationSystem or BuildingPlacementInputSystem.
