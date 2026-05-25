Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 23: move minimap notification to a result/event boundary.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityMinimapEventSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityMinimapEventSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Runtime city static minimap invalidation now belongs to RuntimeCityMinimapEventSystem.
- RuntimeCityGenerationSystem must publish static-minimap-changed events and must not receive or invoke direct UI callbacks.
- RuntimeCitySpawnerSystem must not own `_mainMenuPlayUi`, direct `NotifyStaticMinimapChanged` callbacks, or minimap notification delegates.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still invalidates the static minimap after generation completes, but now through RuntimeCityMinimapEventSystem.

Validation run
- git diff --check on touched step 23 files.
- Static scan for runtime-city minimap notification ownership.
- Unity batchmode: GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation in WarlineCapture-CodexUnity1.
- Unity batchmode: RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation in WarlineCapture-CodexUnity1.

Validation result
- Passed: git diff --check.
- Passed: RuntimeCityArchitectureValidation result=Passed methods=23.
- Passed: RuntimeCityGameSceneSmokeValidation result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63.

Known gaps
- RuntimeCitySpawnerSystem still exists as a temporary shell.
- Step 24 is still pending: runtime root ownership must move out of the spawner shell.
- Step 25 is still pending: runtime city composition should move out of the spawner constructor path.

Cross-lane impacts
- UI/minimap behavior is now behind RuntimeCityMinimapEventSystem for runtime city generation.
- No art, scene, or config asset changes were made.

Next recommended task
Step 24: remove runtime root ownership from RuntimeCitySpawnerSystem and keep scene hierarchy/root handling in the runtime root and visual boundaries.
