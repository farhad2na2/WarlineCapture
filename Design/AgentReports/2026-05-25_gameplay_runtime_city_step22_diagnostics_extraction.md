Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 22: extract diagnostics/events into a narrow runtime-city diagnostics boundary.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityDiagnosticSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityLifecycleSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityStartupSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityRoadCommitSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Runtime city diagnostics ownership now belongs to RuntimeCityDiagnosticSystem.
- Runtime city gameplay systems must not format or emit direct Debug.Log* diagnostics outside the diagnostic boundary.
- Runtime city architecture validation now includes RuntimeCityDiagnosticSystem in the required extracted-system guard.

User-visible behavior
- No intended gameplay behavior change.
- Existing runtime city state diagnostics and warnings still emit through Unity logging when their existing conditions are met, but the formatting and direct Debug.Log* calls are centralized behind RuntimeCityDiagnosticSystem.

Validation run
- git diff --check on touched step 22 files.
- Runtime-city diagnostic token scan across Assets/Game/Scripts/Environment/RuntimeCity*.cs.
- Unity batchmode: GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation in WarlineCapture-CodexUnity1.
- Unity batchmode: RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation in WarlineCapture-CodexUnity1.

Validation result
- Passed: git diff --check.
- Passed: only RuntimeCityDiagnosticSystem contains RuntimeCityState and runtime-city Debug.Log* diagnostic strings.
- Passed: RuntimeCityArchitectureValidation result=Passed methods=21.
- Passed: RuntimeCityGameSceneSmokeValidation result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63.

Known gaps
- Step 21, incoming connector/ingress helper extraction, is still pending because the user explicitly requested step 22.
- Step 23, minimap notification event boundary, is still pending.
- RuntimeCitySpawnerSystem still exists as a managed orchestration shell until the remaining Phase 2 deletion steps complete.

Cross-lane impacts
- UI/minimap lane should expect step 23 to replace direct minimap notification calls with a result/event boundary.
- No art, scene, or config asset changes were made for this step.

Next recommended task
Complete step 21 next: extract incoming connector/ingress helpers into RuntimeCityLayoutSystem or a narrow RuntimeCityIngressSystem, then proceed to step 23 for minimap event publication.
