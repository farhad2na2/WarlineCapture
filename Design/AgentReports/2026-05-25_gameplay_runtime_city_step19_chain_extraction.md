Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 19: extract city-chain connection policy into `RuntimeCityChainSystem`.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md
- Design/AgentReports/2026-05-25_gameplay_runtime_city_step19_chain_extraction.md

Contracts touched
- Added the runtime city-chain ownership rule to Design/Architecture/gameplay_solid_ecs_contract.md.
- Marked roadmap step 19 complete in Design/Architecture/runtime_city_spawner_refactor_roadmap.md.
- Updated the runtime-city responsibility audit with `RuntimeCityChainSystem` as the owner for next-city planning and connection policy.
- Added `GameplayArchitectureContractTests.RuntimeCityChainConnectionPolicyMustLiveInRuntimeCityChainSystem`.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still follows the same city-chain planning rules, but `RuntimeCityGenerationSystem` now requests next-city planning through `RuntimeCityChainSystem`.
- `RuntimeCitySpawnerSystem` no longer owns `TryPlanNextCity`, cardinal direction state, reverse-direction avoidance, city exit validation, autobahn path validation, or source/target connection-cell resolution.

Validation run
- `git diff --check -- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied touched files into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main Unity project was open.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step19-architecture.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step19-smoke.log`

Validation result
- Passed `git diff --check`.
- Passed architecture batch: `[RuntimeCityArchitectureValidation] result=Passed methods=19`.
- Passed Game scene smoke: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

Known gaps
- `RuntimeCitySpawnerSystem` is still a temporary shell and still owns incoming-anchor stroke wiring, ingress corridor pruning, city road network commit, city road-cell population, direct minimap notification wrapper, runtime root reference, and child-system composition.
- `RuntimeCityGenerationSystem` still owns road commit sequencing around source exits, autobahn commits, standalone connector handoff, and occupied-road-cell mutation. Step 20 should extract this to `RuntimeCityRoadCommitSystem`.
- `RuntimeCityChainSystem` temporarily exposes `GetCityInnerConnectionCell` and `GetCityConnectionOffset` because incoming connector/ingress policy is scheduled for step 21.

Cross-lane impacts
- No UI, art, scene, or balance data changes intended.
- Validation copied files to CodexUnity1 only for batch execution; production edits remain in the main workspace.
- Existing unrelated worktree changes from other lanes were not modified.

Next recommended task
Step 20: extract city road commit sequence into `RuntimeCityRoadCommitSystem`, moving `CommitCityRoadNetwork`, `PopulateCityRoadCells`, source-exit road commit, autobahn commit, standalone connector handoff, occupied-road-cell mutation, and road commit failure result codes out of the spawner/generation path.
