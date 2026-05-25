Lane
Gameplay

Task
RuntimeCitySpawnerSystem refactor step 21: extract incoming connector and ingress helpers.

Files changed
- Assets/Game/Scripts/Environment/RuntimeCityIngressSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityIngressSystem.cs.meta
- Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs
- Assets/Game/Scripts/Environment/RuntimeCityChainSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/runtime_city_spawner_refactor_roadmap.md
- Design/Architecture/runtime_city_spawner_responsibility_audit.md

Contracts touched
- Added RuntimeCityIngressSystem as the owner of city layout creation, incoming-anchor stroke wiring, inner connection-cell math, city connection offset math, and ingress-corridor pruning.
- Added an architecture guard preventing those helpers from returning to RuntimeCitySpawnerSystem.

User-visible behavior
- No intended gameplay behavior change.
- Runtime city generation still creates city roads/buildings/blockers through the existing validation path.

Validation run
- git diff --check on touched step 21 files.
- Static scan for moved ingress tokens across runtime-city files.
- Unity batchmode: GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation in WarlineCapture-CodexUnity1.
- Unity batchmode: RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation in WarlineCapture-CodexUnity1.

Validation result
- Passed: git diff --check.
- Passed: RuntimeCityArchitectureValidation result=Passed methods=22.
- Passed: RuntimeCityGameSceneSmokeValidation result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63.

Known gaps
- RuntimeCitySpawnerSystem still exists as a temporary shell.
- Step 23 is still pending: minimap notification must move to a result/event boundary.
- Step 24 is still pending: runtime root ownership must move out of the spawner shell.

Cross-lane impacts
- UI/minimap lane should expect the next runtime-city step to replace direct minimap notification calls.
- No art or scene asset changes were made.

Next recommended task
Step 23: move minimap notification to a result/event boundary so runtime city generation no longer holds a direct UI notification delegate.
