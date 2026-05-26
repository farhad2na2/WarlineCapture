Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 19: move menu binding off the broad shell.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step19_menu_binding.md

Contracts touched
- BuildingGameplay roadmap now marks step 19 complete and records the 1742-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires menu startup binding to route through narrow building UI command/query/interaction systems and BuildingGameplayDependencySystem, not BuildingGameplaySystem.BindDependencies.
- Focused architecture validation now includes BuildingMenuBindingMustStayOffBuildingGameplayShell.

User-visible behavior
- No intended gameplay or UI behavior change.
- Menu startup still receives BuildingUiCommandSystem, BuildingUiQuerySystem, and BuildingPlacementInteractionSystem contexts.
- BuildingGameplayCompositionSystem.Result.BindMainMenu now writes main-menu dependency state directly into BuildingGameplayDependencySystem instead of calling BuildingGameplaySystem.BindDependencies.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step19.log, [BuildingGameplayArchitectureValidation] result=Passed methods=21.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step19.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and remains 1742 lines.
- BindGameplayFeatures, selection binding, citizen population binding, runtime query/spawn APIs, and context factories still route through the shell in later roadmap phases.

Cross-lane impacts
- UI startup continues to consume the same narrow building UI systems and contexts.
- No art, map, AI, economy, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 20: move runtime building read API into BuildingRuntimeQuerySystem.
