Lane
Gameplay

Task
RuntimeCitySpawnerSystem phase 2 step 14: unblock focused runtime-city validation without requiring the normal Game runtime-city profile to stay enabled.

Files changed
- Assets/Game/Scripts/Editor/RuntimeCitySpawnerStep13Validation.cs
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Runtime city refactor roadmap now marks step 14 complete and records the dedicated validation override path.
- Runtime city responsibility audit no longer documents the old cityCount blocker as active.

User-visible behavior
- No gameplay behavior change.
- Normal `Game_RuntimeCitySpawner_Config.asset` is not modified or dirtied by validation.

Validation run
- `git diff --check -- Assets/Game/Scripts/Editor/RuntimeCitySpawnerStep13Validation.cs Design/Architecture/runtime_city_spawner_refactor_roadmap.md Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- Copied the touched files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main project was open in Unity.
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation -logFile /private/tmp/warlinecapture-runtime-city-step14-codexunity1.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation -logFile /private/tmp/warlinecapture-runtime-city-step14-architecture.log`

Validation result
- Passed: `git diff --check`.
- Passed: runtime city game-scene smoke validation. Log result: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.
- Passed: runtime city architecture batch validation. Log result: `[RuntimeCityArchitectureValidation] result=Passed methods=14`.

Known gaps
- The smoke validates configuration/reference wiring and the validation override path. It does not yet run a full play-mode city generation completion loop; that remains part of the later validation gate after the remaining orchestrator extraction.
- The main project could not run Unity batchmode because it was already open in Unity, so validation ran in `WarlineCapture-CodexUnity1` after copying only the touched files.

Cross-lane impacts
- UI/minimap lane should expect a later step to replace direct city generation minimap notification with a result/event boundary.
- Environment/city generation work can continue without changing the normal gameplay city-count performance profile.

Next recommended task
Step 15: extract city lifecycle state into `RuntimeCityLifecycleSystem`, including spawned/generating state, generation routine ownership, generation frame counters, and generation cadence/yield state.
