Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 28: move combat and blocker creation ownership out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Added architecture contract coverage requiring runtime building blocker creation, path-blocking policy, and combat entity creation to bind through BuildingRuntimeContextSystem to BuildingRuntimeEntitySystem, not private shell wrappers on BuildingGameplaySystem.
- Added GameplayArchitectureContractTests coverage for roadmap step 28 and lowered the BuildingGameplaySystem ceiling to 1513 lines.

User-visible behavior
- No intended gameplay behavior change.
- Runtime buildings still create the same blocker entities and combat entities; the callback binding now happens in BuildingRuntimeContextSystem against BuildingRuntimeEntitySystem instead of through private BuildingGameplaySystem wrapper methods.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode focused validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed: git diff --check.
- Passed: /private/tmp/warlinecapture-building-gameplay-arch-step28.log reported [BuildingGameplayArchitectureValidation] result=Passed methods=30.
- Passed: /private/tmp/warlinecapture-building-runtime-boundary-step28.xml reported total=1 passed=1 failed=0.

Known gaps
- BuildingGameplaySystem still owns redirect/hauler bridge wrapper callbacks and broad context factory construction.
- Gate friendly-pass owner-faction projection was already in BuildingRuntimeOwnershipSystem; this step did not change that behavior.
- Unity logs still include non-fatal local license/Xcode plist noise during batchmode startup; validation completed successfully.

Cross-lane impacts
- No art, scene, UI asset, or balance changes.
- Runtime building creation consumers should continue using BuildingRuntimeCreationSystem / BuildingRuntimeContextSystem boundaries; do not add new blocker/combat creation callbacks to BuildingGameplaySystem.

Next recommended task
Step 29: move redirect and hauler bridge calls so resource/transport side effects no longer use BuildingGameplaySystem callbacks.
