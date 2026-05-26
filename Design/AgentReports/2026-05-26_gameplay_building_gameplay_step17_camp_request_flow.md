Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 17: move camp item request flow.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step17_camp_request_flow.md

Contracts touched
- `BuildingGameplaySystem` step 17 contract now requires camp item request flow to route through `BuildingUiCommandSystem` and `BuildingProductionRequestSystem`.
- Architecture guard now validates the camp request routing and the 1736-line transition ceiling.

User-visible behavior
No intended gameplay behavior change. Camp item affordability checks, missing-producer messages, request execution, and deferred producer focus should behave as before.

Validation run
- `git diff --check`
- Unity 6000.4.0f1 batch validation: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity 6000.4.0f1 EditMode: `BuildingRuntimeBoundaryValidationTests`

Validation result
Passed.
- Architecture batch: `result=Passed methods=19`
- Runtime boundary EditMode: `total=1 passed=1 failed=0`

Known gaps
- `BuildingGameplaySystem` still exists as temporary roadmap debt at 1736 lines.
- UI read methods remain for step 18.

Cross-lane impacts
No expected art, UI, or design impact. This is internal gameplay architecture only.

Next recommended task
Continue with building gameplay roadmap step 18: move UI read methods behind `BuildingUiQuerySystem`.
