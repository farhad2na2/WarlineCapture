Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 27: move runtime destruction and entity-link callbacks out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Added architecture contract coverage requiring runtime building delete callbacks and runtime entity destroyed callbacks to route through BuildingRuntimeEntitySystem / BuildingCombatSystem, not public shell methods on BuildingGameplaySystem.
- Added GameplayArchitectureContractTests coverage for roadmap step 27 and lowered the BuildingGameplaySystem ceiling to 1538 lines.

User-visible behavior
- No intended gameplay behavior change.
- Selected-building delete, runtime-city building delete, and RuntimeBuildingEntityLink destroyed callbacks still use the same combat/destruction behavior, but their callback surface now routes through BuildingRuntimeEntitySystem.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode focused validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed: git diff --check.
- Passed: /private/tmp/warlinecapture-building-gameplay-arch-step27.log reported [BuildingGameplayArchitectureValidation] result=Passed methods=29.
- Passed: /private/tmp/warlinecapture-building-runtime-boundary-step27.xml reported total=1 passed=1 failed=0.

Known gaps
- BuildingGameplaySystem still owns blocker/combat entity creation wrappers, runtime creation wiring, redirect/hauler bridge callbacks, and broad context factory construction.
- Unity logs still include non-fatal local license/Xcode plist noise during batchmode startup; validation completed successfully.

Cross-lane impacts
- Runtime city and RuntimeBuildingEntityLink flows should continue to use existing interaction/context entry points; their delete/entity-destroy callback behavior now resolves through BuildingRuntimeEntitySystem.
- No art, scene, UI asset, or balance changes.

Next recommended task
Step 28: move combat and blocker creation ownership out of BuildingGameplaySystem so ECS blocker/combat entity creation is owned entirely by BuildingRuntimeEntitySystem / BuildingCombatSystem / BuildingBarrierSystem.
