Lane
Gameplay

Task
RuntimeCitySpawnerSystem phase 2 step 16: extract runtime city startup gate into `RuntimeCityStartupSystem`.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Added the SOLID/ECS rule that runtime city startup gating, spawn-on-start readiness, play-request checks, mission exclusion policy, dependency availability checks, required prefab readiness, initial-unit readiness gating, and startup result shaping belong in `RuntimeCityStartupSystem`.
- Added architecture coverage preventing those startup gate branches from returning to `RuntimeCitySpawnerSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city auto-start and manual `GenerateCity()` still use the same public spawner surface, but readiness decisions now flow through `RuntimeCityStartupSystem.Result`.

Validation run
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs.meta Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied step-16 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main project was open in Unity.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step16-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step16-smoke.log`

Validation result
- Passed: `git diff --check`.
- Passed: runtime city architecture batch validation. Log result: `[RuntimeCityArchitectureValidation] result=Passed methods=16`.
- Passed: runtime city game-scene smoke validation. Log result: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` still owns ECS/grid readiness query creation, `HasPendingInitialUnitsSpawn`, initial base exclusion collection, city generation sequence, city-chain planning, road commit sequencing, direct UI minimap notification, and runtime root reference.
- `RuntimeCityStartupSystem` currently consumes delegates for grid readiness and initial-unit readiness; step 17 should move the underlying ECS query ownership into a dedicated readiness query system.
- The main project could not run Unity batchmode because it was already open in Unity, so validation ran in `WarlineCapture-CodexUnity1` after copying only the touched files.

Cross-lane impacts
- None expected for UI, AI, or building lanes in this step.
- Runtime city startup policy is now available as a narrow result boundary for later composition work.

Next recommended task
Step 17: extract ECS query/readiness ownership into `RuntimeCityReadinessQuerySystem`, including grid-data query caching, `TryGetGridData`, `HasPendingInitialUnitsSpawn`, and initial base exclusion road-rect collection.
