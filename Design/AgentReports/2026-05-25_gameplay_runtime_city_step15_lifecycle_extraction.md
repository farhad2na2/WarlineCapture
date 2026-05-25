Lane
Gameplay

Task
RuntimeCitySpawnerSystem phase 2 step 15: extract city lifecycle state into `RuntimeCityLifecycleSystem`.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Added the SOLID/ECS rule that runtime city lifecycle state, spawned/generating flags, generation routine ownership, frame counters, and yield cadence belong in `RuntimeCityLifecycleSystem`.
- Added architecture coverage preventing `_spawned`, `_generationRoutine`, generation frame counters, generation diagnostic cadence, and direct `_generationRoutine.MoveNext()` ownership from returning to `RuntimeCitySpawnerSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still starts, ticks, cancels, and completes through the same public `RuntimeCitySpawnerSystem` surface.

Validation run
- `git diff --check -- Assets/Game/Scripts/Editor/RuntimeCitySpawnerStep13Validation.cs Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md Design/AgentReports/2026-05-25_gameplay_runtime_city_step14_validation_unblock.md`
- Copied step-15 files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main project was open in Unity.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step15-architecture-rerun.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step15-smoke.log`

Validation result
- Passed: `git diff --check`.
- Passed: runtime city architecture batch validation. Log result: `[RuntimeCityArchitectureValidation] result=Passed methods=15`.
- Passed: runtime city game-scene smoke validation. Log result: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` still owns startup gating, ECS/grid readiness queries, city generation sequence, city-chain planning, road commit sequencing, direct UI minimap notification, and runtime root reference.
- The main project could not run Unity batchmode because it was already open in Unity, so validation ran in `WarlineCapture-CodexUnity1` after copying only the touched files.

Cross-lane impacts
- None expected for UI, AI, or building lanes in this step.
- Runtime city public surface remains stable for current peer systems.

Next recommended task
Step 16: extract runtime city startup gate into `RuntimeCityStartupSystem`, including spawn-on-start readiness, play-request gating, M01 exclusion policy, dependency availability checks, prefab-list readiness, and initial-unit readiness gating.
