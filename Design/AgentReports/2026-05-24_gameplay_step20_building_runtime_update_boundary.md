Lane
Gameplay

Task
Step 20 - remove GameplayRuntimeUpdateSystem's direct BuildingPlacementSystem parameter.

Files changed
- Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeUpdateSystem.cs.meta
- Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step20_building_runtime_update_boundary.md

Contracts touched
- Updated the gameplay architecture contract so building runtime updates inside GameplayRuntimeUpdateSystem must go through BuildingRuntimeUpdateSystem instead of a direct BuildingPlacementSystem facade parameter.
- Added GameplayArchitectureContractTests coverage for the new BuildingRuntimeUpdateSystem boundary and for GameplayRuntimeUpdateSystem not referencing BuildingPlacementSystem or buildingPlacement?.Update.

User-visible behavior
- No intended behavior change.
- The managed runtime update order is unchanged; the "BuildingPlacement" performance step still runs in the same position.
- BuildingPlacementSystem.Update is now invoked through BuildingRuntimeUpdateSystem.Context.

Validation run
- Mirrored Step 20 changed files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran focused grep checks confirming GameplayRuntimeUpdateSystem.cs has no BuildingPlacementSystem or buildingPlacement?.Update references.
- Ran focused git diff whitespace check for Step 20 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: GameplayRuntimeUpdateSystem.cs has no direct BuildingPlacementSystem or buildingPlacement?.Update references.
- Passed: focused diff whitespace check for Step 20 files.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2481 lines.
- GameBootstrap still exposes BuildingPlacementSystem and still uses it to create the BuildingRuntimeUpdateSystem/context.
- BuildingRuntimeUpdateSystem currently wraps BuildingPlacementSystem.Update through a delegate until runtime update ownership can move fully out of the facade.

Cross-lane impacts
- No scene files changed.
- Worktree has unrelated untracked UI shell ECS files under Assets/Game/Scripts/UI/Shell/Ecs; this Gameplay step did not modify them.

Next recommended task
- Step 21: move BuildingRuntimeUpdateSystem ownership/context construction out of BuildingPlacementSystem and into managed composition or a building runtime bundle, so GameBootstrap no longer needs to reach into the facade for the runtime update boundary.
