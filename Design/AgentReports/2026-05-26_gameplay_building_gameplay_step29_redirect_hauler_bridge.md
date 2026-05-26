Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 29: move redirect and hauler bridge callbacks out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Added architecture contract coverage requiring runtime redirect callbacks, selected-hauler order assignment, and building approach checks to bind through BuildingRuntimeContextSystem to BuildingPlacementRedirectSystem / BuildingResourceHaulerBridgeSystem.
- Added GameplayArchitectureContractTests coverage for roadmap step 29 and lowered the BuildingGameplaySystem ceiling to 1473 lines.

User-visible behavior
- No intended gameplay behavior change.
- Runtime creation still redirects units around placed buildings, deferred marker refresh still flushes through the redirect system, and selected resource hauler building orders/approach checks still use the same hauler bridge behavior.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode focused validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed: git diff --check.
- Passed: /private/tmp/warlinecapture-building-gameplay-arch-step29.log reported [BuildingGameplayArchitectureValidation] result=Passed methods=32.
- Passed: /private/tmp/warlinecapture-building-runtime-boundary-step29.xml reported total=1 passed=1 failed=0.

Known gaps
- BuildingGameplaySystem still owns broad context factory construction and several compatibility read/command surfaces.
- Unity logs still include non-fatal local license/Xcode plist noise during batchmode startup; validation completed successfully.

Cross-lane impacts
- No art, scene, UI asset, or balance changes.
- Future runtime creation and selection work should use BuildingRuntimeContextSystem for redirect/hauler bridge bindings instead of adding private callbacks to BuildingGameplaySystem.

Next recommended task
Step 30: start Phase 8 by moving remaining context factory construction out of BuildingGameplaySystem, beginning with placement/runtime context source construction that still keeps the shell alive.
