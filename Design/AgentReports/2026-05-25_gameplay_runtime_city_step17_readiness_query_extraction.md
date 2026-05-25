Lane
Gameplay

Task
RuntimeCitySpawnerSystem phase 2 step 17: extract ECS query/readiness ownership into `RuntimeCityReadinessQuerySystem`.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Added the SOLID/ECS rule that runtime city ECS readiness query ownership, grid-data query caching, grid config lookup, initial-unit readiness checks, and initial base exclusion road-rect collection belong in `RuntimeCityReadinessQuerySystem`.
- Added architecture coverage preventing `World`, `EntityQuery`, `EntityManager`, `Allocator`, direct ECS query setup, `TryGetGridData`, `HasPendingInitialUnitsSpawn`, and initial base exclusion collection from returning to `RuntimeCitySpawnerSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city startup and generation still use the same public `RuntimeCitySpawnerSystem` surface.

Validation run
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs Assets/Game/Scripts/Environment/RuntimeCityReadinessQuerySystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied step-17 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main project was open in Unity.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step17-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step17-smoke.log`

Validation result
- Passed: `git diff --check`.
- Passed: runtime city architecture batch validation. Log result: `[RuntimeCityArchitectureValidation] result=Passed methods=17`.
- Passed: runtime city game-scene smoke validation. Log result: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` still owns city generation sequencing, city-chain planning, road commit sequencing, direct UI minimap notification, and runtime root reference.
- `RuntimeCityReadinessQuerySystem` still uses ECS query APIs directly by design; this step moved ownership, not the later generation-sequence extraction.
- The main project could not run Unity batchmode because it was already open in Unity, so validation ran in `WarlineCapture-CodexUnity1` after copying only the touched files.

Cross-lane impacts
- None expected for UI, AI, or building lanes in this step.
- Runtime city readiness is now a narrow query boundary for later composition work.

Next recommended task
Step 18: extract city generation sequence into `RuntimeCityGenerationSystem`, including generation routine ownership, deferred road sync ordering, deferred spawn side-effect ordering, city-list lifetime, RNG lifetime, bulk-building routine stepping, and completion notification.
