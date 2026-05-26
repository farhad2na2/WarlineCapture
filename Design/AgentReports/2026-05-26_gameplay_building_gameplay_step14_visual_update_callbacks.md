Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 14: move placement focus and visual update callbacks.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSourceSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step14_visual_update_callbacks.md

Contracts touched
- `BuildingGameplaySystem` step 14 contract now requires active-placement focus, placement visual update, confirm validation, and placement object handoff to live in `BuildingPlacementVisualUpdateSystem`.
- Architecture guard now validates the new visual-update boundary and the 1824-line transition ceiling.
- Existing preview/input/commit/lifecycle architecture tests now expect their runtime callback usage through `BuildingPlacementVisualUpdateSystem`.

User-visible behavior
No intended gameplay behavior change. Building placement preview, wall placement validation, camera follow/focus, and placement commit handoff should behave as before.

Validation run
- `git diff --check`
- Unity 6000.4.0f1 batch validation: `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`
- Unity 6000.4.0f1 EditMode: `BuildingRuntimeBoundaryValidationTests`

Validation result
Passed.
- Architecture batch: `result=Passed methods=16`
- Runtime boundary EditMode: `total=1 passed=1 failed=0`

Known gaps
- `BuildingGameplaySystem` still exists as temporary roadmap debt at 1824 lines.
- Step 15 remains: move wall placement preview/commit helper state and algorithms out of the shell.

Cross-lane impacts
No expected art, UI, or design impact. The change is internal gameplay architecture only.

Next recommended task
Continue with building gameplay roadmap step 15: move wall placement preview/commit helpers into the placement preview, commit, and barrier boundaries.
