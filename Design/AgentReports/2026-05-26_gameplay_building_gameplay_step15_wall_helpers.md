Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 15: move wall placement preview/commit helpers.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementPreviewSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step15_wall_helpers.md

Contracts touched
- `BuildingGameplaySystem` step 15 contract now requires wall preview/commit scratch state, wall validation context construction, and placement rotate-vertical policy to live in placement preview/context/barrier systems.
- Architecture guard now validates the new wall helper ownership and the 1770-line transition ceiling.

User-visible behavior
No intended gameplay behavior change. Wall placement preview, wall commit, wall validation, and gate/wall rotation policy should behave as before.

Validation run
- `git diff --check`
- Unity 6000.4.0f1 batch validation: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity 6000.4.0f1 EditMode: `BuildingRuntimeBoundaryValidationTests`

Validation result
Passed.
- Architecture batch: `result=Passed methods=17`
- Runtime boundary EditMode: `total=1 passed=1 failed=0`

Known gaps
- `BuildingGameplaySystem` still exists as temporary roadmap debt at 1770 lines.
- UI command/read surface extraction starts next.

Cross-lane impacts
No expected art, UI, or design impact. This is internal gameplay architecture only.

Next recommended task
Continue with building gameplay roadmap step 16: move selected-building production button commands into `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`.
