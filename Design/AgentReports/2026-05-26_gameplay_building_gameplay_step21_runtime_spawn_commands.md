Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 21: move runtime building spawn command routing behind BuildingRuntimeSpawnCommandSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeCitySpawnSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step21_runtime_spawn_commands.md

Contracts touched
- BuildingGameplay roadmap now marks step 21 complete and records the 1742-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires runtime spawn commands to route through BuildingRuntimeSpawnCommandSystem and BuildingRuntimeSpawnSystem, with runtime-city building spawn using the same command boundary.
- Focused architecture validation now includes BuildingRuntimeSpawnCommandsMustRouteThroughRuntimeSpawnCommandSystem.

User-visible behavior
- No intended gameplay behavior change.
- BuildingGameplayCompositionSystem.Result now exposes RuntimeSpawnCommand and RuntimeSpawnCommandContext for direct consumers.
- BuildingRuntimeCitySpawnSystem now routes city building spawn through BuildingRuntimeSpawnCommandSystem instead of owning a separate BuildingRuntimeSpawnSystem instance.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step21.log, [BuildingGameplayArchitectureValidation] result=Passed methods=23.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step21.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and remains 1742 lines.
- BuildingGameplaySystem spawn wrappers still exist for compatibility until production/test callers migrate to RuntimeSpawnCommand and RuntimeSpawnCommandContext.
- Editor tests still use BuildingGameplayTestHarness for several spawn scenarios; full test harness removal is tracked in later roadmap phases.

Cross-lane impacts
- Runtime city now shares the same building spawn command boundary as the rest of building gameplay.
- No art, map, AI, economy, UI layout, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 22: move faction spawn point queries.
