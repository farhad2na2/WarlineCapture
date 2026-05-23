Lane
Gameplay

Task
Move remaining wall/gate placement helpers from BuildingPlacementSystem into existing placement systems.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-wall-gate-helper-extraction.md

Contracts touched
- Updated the gameplay SOLID/ECS contract to assign wall run/origin validation and overlap checks to BuildingPlacementValidationSystem.
- Updated the contract to assign wall-run origin construction, wall segment footprint helpers, and wall placement rotation helpers to BuildingPlacementCommitSystem.
- Updated the contract to assign road barrier gate classification and gate-to-nearby-wall alignment to BuildingBarrierSystem.
- Added architecture test coverage preventing these helpers from drifting back into BuildingPlacementSystem.

User-visible behavior
- No intended user-visible gameplay change.
- Wall preview/placement validity, runtime wall segment spawning, gate rotation alignment, and barrier classification should preserve existing behavior through delegated systems.

Validation run
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementValidationSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementCommitSystem.cs Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-wall-gate-helper-extraction-tests.log`

Validation result
- Focused `git diff --check` on touched code/docs passed.
- Unity EditMode `GameplayArchitectureContractTests` passed: 78/78.

Known gaps
- Full `git diff --check` still reports unrelated trailing whitespace in Assets/Game/Scenes/Game_Terrain2.unity, which was already outside this code extraction and was not modified by this task.
- BuildingPlacementSystem remains a large facade at 4,619 lines.
- Generic hauler approach rectangle distance still has a small local AxisDistance helper in BuildingPlacementSystem; it is not wall/gate placement ownership.

Cross-lane impacts
- No UI or art contract changes.
- Scene file changes from other work remain unrelated and were not touched.

Next recommended task
- Continue shrinking BuildingPlacementSystem by moving remaining active placement lifecycle/state into a dedicated placement lifecycle system, or move remaining generic placement/grid math into BuildingPlacementValidationSystem in smaller slices.
