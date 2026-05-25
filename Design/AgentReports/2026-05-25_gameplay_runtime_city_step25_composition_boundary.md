Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 25: move runtime city composition out of the spawner constructor path.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Runtime city child-system graph construction, bridge/visual/minimap configuration, context factories, update orchestration, and child-system disposal now belong to RuntimeCityCompositionSystem.
- RuntimeCitySpawnerSystem may remain temporarily only as a public compatibility shell over the composition boundary.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation, building spawn, road commit, visual root parenting, and minimap invalidation still run through the same validation path.

Validation run
- git diff --check on touched step 25 files.
- Static scan for child system graph ownership in RuntimeCityCompositionSystem versus RuntimeCitySpawnerSystem.
- Unity batchmode: GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation in WarlineCapture-CodexUnity1.
- Unity batchmode: RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation in WarlineCapture-CodexUnity1.

Validation result
- Passed: git diff --check.
- Passed: RuntimeCityArchitectureValidation result=Passed methods=25.
- Passed: RuntimeCityGameSceneSmokeValidation result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63.

Known gaps
- RuntimeCitySpawnerSystem still exists as a temporary compatibility shell.
- Step 26 is still pending: migrate RuntimeGridBlockerSystem and RuntimeDecorationSpawnerSystem dependencies off RuntimeCitySpawnerSystem.
- Step 27 is still pending: rename or delete the spawner shell after peer dependencies are removed.

Cross-lane impacts
- No scene, art, UI prefab, or config asset changes were made.
- Runtime city public API remains stable for current callers.

Next recommended task
Step 26: migrate peer dependencies off RuntimeCitySpawnerSystem to narrow city lifecycle/read-result boundaries.
