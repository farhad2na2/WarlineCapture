Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 16: move production button commands.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs
- Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs
- Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step16_production_button_commands.md

Contracts touched
- `BuildingGameplaySystem` step 16 contract now requires selected-building production button commands to route through `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`.
- Architecture guard now validates the command routing and the 1765-line transition ceiling.

User-visible behavior
No intended gameplay behavior change. Primary, secondary, tertiary, and quaternary building production buttons should queue units as before.

Validation run
- `git diff --check`
- Unity 6000.4.0f1 batch validation: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity 6000.4.0f1 EditMode: `BuildingRuntimeBoundaryValidationTests`

Validation result
Passed.
- Architecture batch: `result=Passed methods=18`
- Runtime boundary EditMode: `total=1 passed=1 failed=0`

Known gaps
- `BuildingGameplaySystem` still exists as temporary roadmap debt at 1765 lines.
- Camp item request flow still uses temporary shell callbacks and is the next roadmap item.
- UI production read methods remain for step 18.

Cross-lane impacts
No expected art, UI, or design impact. This is internal gameplay architecture only.

Next recommended task
Continue with building gameplay roadmap step 17: move camp item request flow into `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`.
