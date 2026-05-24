Lane
Gameplay

Task
Step 19 - remove CitizenPopulationSystem's direct BuildingPlacementSystem dependency.

Files changed
- Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs
- Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay_step19_citizen_population_narrow_boundary.md

Contracts touched
- Updated the gameplay architecture contract so CitizenPopulationSystem must receive narrow building/resource/prefab systems or contexts directly from managed composition and must not accept BuildingPlacementSystem.
- Updated GameplayArchitectureContractTests to assert CitizenPopulationSystem has no BuildingPlacementSystem references.

User-visible behavior
- No intended behavior change.
- Citizen building reads still use BuildingRuntimeQuerySystem.
- Citizen upkeep spending still uses CitizenResourceSystem with RuntimeResourceSystem-backed context.
- Citizen prefab/entity resolution still uses CitizenPrefabSystem with RuntimeUnitPrefabSystem-created context.

Validation run
- Mirrored Step 19 changed files to /Users/farhad/Projects/WarlineCapture-CodexUnity1.
- Ran Unity 6000.4.0f1 batchmode EditMode tests with filter GameplayArchitectureContractTests.
- Ran focused grep checks confirming CitizenPopulationSystem.cs has no BuildingPlacementSystem or _buildingPlacementSystem references.
- Ran focused git diff whitespace check for Step 19 files.

Validation result
- Passed: GameplayArchitectureContractTests, 93 total, 93 passed, 0 failed.
- Passed: CitizenPopulationSystem.cs has no direct BuildingPlacementSystem references.
- Passed: focused diff whitespace check for Step 19 files.

Known gaps
- BuildingPlacementSystem still exists as the compatibility facade and is currently 2474 lines.
- ManagedGameplayStartupSystem still creates BuildingPlacementSystem and pulls narrow contexts from it for citizen composition.
- GameBootstrap still exposes BuildingPlacementSystem because GameplayRuntimeUpdateSystem and other managed compatibility paths still receive the facade.

Cross-lane impacts
- No scene files changed.
- Worktree has unrelated untracked UI shell ECS files under Assets/Game/Scripts/UI/Shell/Ecs; this Gameplay step did not modify them.

Next recommended task
- Step 20: remove GameplayRuntimeUpdateSystem's direct BuildingPlacementSystem parameter by introducing a narrow building runtime update boundary or by passing only the update/action delegates it actually needs.
