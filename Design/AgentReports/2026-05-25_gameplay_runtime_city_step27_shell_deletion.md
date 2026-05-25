# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
RuntimeCitySpawnerSystem refactor step 27: delete the temporary spawner shell and migrate remaining callers to the runtime city composition boundary.

## Files changed
- `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs` deleted
- `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs.meta` deleted
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs`
- `Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/PlayMode/BootstrapAndMenuPlayModeTests.cs`
- `Design/Architecture/runtime_city_spawner_refactor_roadmap.md`
- `Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-25_gameplay_runtime_city_step27_shell_deletion.md`

## Contracts touched
- Runtime city shell deletion is now documented as step 27 complete.
- Architecture contract now states `RuntimeCitySpawnerSystem.cs` must not be restored.
- Added architecture guard `RuntimeCitySpawnerSystemShellMustStayDeleted`.
- Updated existing runtime-city guards to inspect `RuntimeCityCompositionSystem` after shell deletion.

## User-visible behavior
No intended gameplay behavior change. Runtime city update still runs during gameplay, blockers/decorations still wait for runtime city completion, and building house classification still queries runtime city configured house prefabs.

## Validation run
- `git diff --check --` on touched files.
- Static search for deleted shell construction/update tokens.
- Unity batchmode: `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
- Unity batchmode: `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`.

## Validation result
- Diff check passed.
- Static search found no production construction/update references to the deleted shell.
- Architecture validation passed: `[RuntimeCityArchitectureValidation] result=Passed methods=27`.
- Runtime city smoke passed: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

## Known gaps
- Config class and asset names still contain `RuntimeCitySpawnerSystemConfig`; this was left untouched because it is serialized project data and not the deleted runtime shell.
- Historical docs and old reports still mention `RuntimeCitySpawnerSystem`; those are not production references.

## Cross-lane impacts
- Architecture lane: the old runtime-city shell file is now a hard-deleted type.
- UI/building lane: building gameplay now receives `RuntimeCityCompositionSystem` for configured-house prefab checks.
- Performance diagnostics label changed from `RuntimeCitySpawner` to `RuntimeCity`.

## Next recommended task
Step 28: final architecture contract cleanup and hard guards for the deleted shell, including deciding whether serialized config names should remain for compatibility or get a separate asset migration plan.
