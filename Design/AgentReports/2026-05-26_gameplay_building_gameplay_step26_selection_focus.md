Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 26: move building selection and camera focus helpers out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Added architecture contract coverage requiring building selection, visible-selectable checks, selected-building deletion, and camera-focus helpers to live in BuildingSelectionSystem rather than BuildingGameplaySystem.
- Added GameplayArchitectureContractTests coverage for roadmap step 26 and kept BuildingGameplaySystem at the 1542-line ceiling.

User-visible behavior
- No intended gameplay behavior change.
- Building selection, selected-building deletion, visible-selectable checks, and focus-world-position resolution continue to behave through the same public flows, but the implementation now routes through BuildingSelectionSystem.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode focused validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed: git diff --check.
- Passed: /private/tmp/warlinecapture-building-gameplay-arch-step26.log reported [BuildingGameplayArchitectureValidation] result=Passed methods=28.
- Passed: /private/tmp/warlinecapture-building-runtime-boundary-step26.xml reported total=1 passed=1 failed=0.

Known gaps
- BuildingGameplaySystem still owns runtime destruction/entity link callbacks and additional runtime wiring; those are the next removal targets.
- This step did not run a full playmode smoke because the focused architecture and runtime boundary checks cover the changed ownership surface.

Cross-lane impacts
- UI/menu code should keep calling the existing building gameplay surface until the later facade deletion work migrates all callers.
- No art, design, or scene asset changes.

Next recommended task
Step 27: move runtime destruction and entity link callbacks out of BuildingGameplaySystem into the narrower runtime entity/destruction boundary.
