Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 20: extract city road commit sequence into `RuntimeCityRoadCommitSystem`.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md
- Design/AgentReports/2026-05-25_gameplay_runtime_city_step20_road_commit_extraction.md

Contracts touched
- Added the runtime city road-commit ownership rule to Design/Architecture/gameplay_solid_ecs_contract.md.
- Marked roadmap step 20 complete in Design/Architecture/runtime_city_spawner_refactor_roadmap.md.
- Updated the runtime-city responsibility audit with `RuntimeCityRoadCommitSystem` as the owner for city road network commit, road-cell population, source-exit commit, autobahn commit, standalone connector handoff, and occupied-road-cell mutation.
- Added `GameplayArchitectureContractTests.RuntimeCityRoadCommitSequenceMustLiveInRuntimeCityRoadCommitSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still creates the same city roads, source exits, autobahn connectors, and standalone connector chain.
- `RuntimeCitySpawnerSystem` no longer owns `CommitCityRoadNetwork`, `PopulateCityRoadCells`, or direct road-build commit calls.
- `RuntimeCityGenerationSystem` now requests road commits through `RuntimeCityRoadCommitSystem`.

Validation run
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied touched files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main Unity project was open.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step20-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step20-smoke.log`

Validation result
- Passed `git diff --check`.
- Passed architecture batch: `[RuntimeCityArchitectureValidation] result=Passed methods=20`.
- Passed Game scene smoke: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` is still a temporary shell and still owns incoming-anchor stroke wiring, ingress corridor pruning, direct minimap notification wrapper, runtime root reference, and child-system composition.
- `RuntimeCityGenerationSystem` still owns high-level generation order and still uses a hard-coded standalone connector length of 9.
- `RuntimeCityChainSystem` temporarily exposes `GetCityInnerConnectionCell` and `GetCityConnectionOffset` because incoming connector/ingress policy is scheduled for step 21.

Cross-lane impacts
- No UI, art, scene, or balance data changes intended.
- Validation copied files to CodexUnity1 only for batch execution; production edits remain in the main workspace.
- Existing unrelated worktree changes from other lanes were not modified.

Next recommended task
Step 21: extract incoming connector/ingress helpers into `RuntimeCityIngressSystem` or fold them into `RuntimeCityLayoutSystem`, moving `CreateCityLayout` incoming-anchor wiring, `GetCityInnerConnectionCell`, `GetCityConnectionOffset`, `PruneIngressCorridorStrokes`, and related ingress-corridor policy out of `RuntimeCitySpawnerSystem`.
