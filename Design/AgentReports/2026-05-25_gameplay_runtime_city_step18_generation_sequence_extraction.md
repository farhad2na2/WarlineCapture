Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 18: extract city generation sequence into a narrow runtime-city generation boundary.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md
- Design/AgentReports/2026-05-25_gameplay_runtime_city_step18_generation_sequence_extraction.md

Contracts touched
- Added the runtime city generation boundary rule to Design/Architecture/gameplay_solid_ecs_contract.md.
- Marked roadmap step 18 complete in Design/Architecture/runtime_city_spawner_refactor_roadmap.md.
- Updated the runtime-city responsibility audit with the new `RuntimeCityGenerationSystem` owner and the reduced spawner size.
- Added `GameplayArchitectureContractTests.RuntimeCityGenerationSequenceMustLiveInRuntimeCityGenerationSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still uses the existing layout, road, walkability, building-spawn, road-build, spawn-bridge, startup, readiness, and lifecycle systems.
- The broad spawner now starts generation through `RuntimeCityGenerationSystem.TryBegin(...)` instead of owning the generation coroutine.

Validation run
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied touched files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main Unity project was open.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step18-architecture-final.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step18-smoke.log`

Validation result
- Passed `git diff --check`.
- Passed architecture batch: `[RuntimeCityArchitectureValidation] result=Passed methods=18`.
- Passed Game scene smoke: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` is still a temporary shell and still owns city-chain helper methods, city road commit helper methods, incoming connector helper math, direct minimap notification wrapper, runtime root reference, and child-system composition.
- `RuntimeCityGenerationSystem` receives city-chain and road-commit helpers as delegates until steps 19-21 extract those policies.
- Runtime city diagnostics still use direct log delegates; step 22 should move them to a structured diagnostics boundary.

Cross-lane impacts
- No UI, art, scene, or balance data changes intended.
- Validation copied files to CodexUnity1 only for batch execution; production edits remain in the main workspace.
- Existing unrelated worktree changes from other lanes were not modified.

Next recommended task
Step 19: extract city-chain connection policy into `RuntimeCityChainSystem`, moving `TryPlanNextCity`, travel-direction selection, reverse-direction avoidance, candidate target-center policy, city spacing checks, autobahn length policy, and source/target connection-cell resolution out of `RuntimeCitySpawnerSystem`.
