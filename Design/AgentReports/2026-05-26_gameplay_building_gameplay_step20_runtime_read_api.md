Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 20: move runtime building read API routing into BuildingRuntimeQuerySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step20_runtime_read_api.md

Contracts touched
- BuildingGameplay roadmap now marks step 20 complete and records the 1742-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires runtime building read APIs to route through BuildingRuntimeQuerySystem and BuildingRuntimeQuerySystem.Context, including base-breach target read routing.
- Focused architecture validation now includes BuildingRuntimeReadApiMustRouteThroughRuntimeQuerySystem.

User-visible behavior
- No intended gameplay behavior change.
- Runtime building read wrappers still exist temporarily for compatibility, but base-breach target routing now delegates through BuildingRuntimeQuerySystem.
- BuildingGameplayCompositionSystem.Result now exposes RuntimeQuery and RuntimeQueryContext so direct consumers can migrate away from BuildingGameplaySystem.
- Citizen population creation now consumes the composition-owned RuntimeQuery and RuntimeQueryContext fields directly.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step20.log, [BuildingGameplayArchitectureValidation] result=Passed methods=22.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step20.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and remains 1742 lines.
- Runtime spawn commands, faction spawn point queries, configured prefab resolution, and test-only spawn helpers remain for later roadmap steps.
- Some compatibility wrappers still route through BuildingGameplaySystem until production/test consumers finish migrating to RuntimeQuery and RuntimeQueryContext.

Cross-lane impacts
- Citizen population has a clearer path to runtime building read models through composition-owned query fields.
- No art, map, AI, economy, UI layout, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 21: move runtime building spawn commands.
