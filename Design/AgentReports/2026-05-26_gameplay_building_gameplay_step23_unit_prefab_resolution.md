Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 23: move configured unit prefab resolution into RuntimeUnitPrefabSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step23_unit_prefab_resolution.md

Contracts touched
- BuildingGameplay roadmap now marks step 23 complete and records the 1678-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution to live in RuntimeUnitPrefabSystem, not BuildingGameplaySystem.
- Focused architecture validation now includes BuildingConfiguredUnitPrefabResolutionMustLiveInRuntimeUnitPrefabSystem.

User-visible behavior
- No intended gameplay behavior change.
- Configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution still use the same registry/live-unit/produced-unit/source-key fallback order.
- BuildingGameplaySystem keeps temporary compatibility wrappers over RuntimeUnitPrefabSystem.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step23.log, [BuildingGameplayArchitectureValidation] result=Passed methods=25.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step23.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and is now 1678 lines.
- TryResolveConfiguredUnitPrefabEntity, TryResolveSpawnUnitPrefab, and TryResolveLiveUnitPreviewPrefab remain as compatibility wrappers until consumers and context factories move off the shell.
- Initial roster/test helper migration remains for step 24.

Cross-lane impacts
- Menu/HUD live-unit preview reads now route through the RuntimeUnitPrefabSystem boundary via the existing BuildingUiQuerySystem context.
- Citizen prefab context creation remains routed through BuildingRuntimeResourcePrefabContextSystem.
- No art, map, economy, UI layout, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 24: move initial roster/test helpers.
