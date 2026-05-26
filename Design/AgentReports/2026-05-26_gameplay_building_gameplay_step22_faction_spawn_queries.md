Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 22: move faction spawn point queries into BuildingSpawnSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step22_faction_spawn_queries.md

Contracts touched
- BuildingGameplay roadmap now marks step 22 complete and records the 1717-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires faction production spawn point and available helipad spawn queries to live in BuildingSpawnSystem, not BuildingGameplaySystem.
- Focused architecture validation now includes BuildingFactionSpawnPointQueriesMustLiveInSpawnSystem.

User-visible behavior
- No intended gameplay behavior change.
- BuildingGameplaySystem.TryGetFactionProductionSpawnPoint now delegates to BuildingSpawnSystem.
- Available faction helipad spawn remains routed through BuildingSpawnSystem.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- First architecture run caught a contract wording mismatch; fixed and reran.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step22-rerun.log, [BuildingGameplayArchitectureValidation] result=Passed methods=24.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step22.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both final validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and is now 1717 lines.
- TryResolveAvailableFactionHelipadSpawn still uses the shell only to gather EntityManager/grid data before delegating to BuildingSpawnSystem.
- Configured unit prefab resolution remains for step 23.

Cross-lane impacts
- AI production/transport spawn lookup has a narrower building spawn query owner.
- No art, map, economy, UI layout, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 23: move configured unit prefab resolution.
