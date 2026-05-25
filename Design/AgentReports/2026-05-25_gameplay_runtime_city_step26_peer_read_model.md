# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
RuntimeCitySpawnerSystem refactor step 26: migrate peer dependencies off the broad spawner shell.

## Files changed
- `Assets/Game/Scripts/Environment/RuntimeCityReadModelSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityReadModelSystem.cs.meta`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs`
- `Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/runtime_city_spawner_refactor_roadmap.md`
- `Design/Architecture/runtime_city_spawner_responsibility_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/AgentReports/2026-05-25_gameplay_runtime_city_step26_peer_read_model.md`

## Contracts touched
- Added `RuntimeCityReadModelSystem` as the narrow state-read boundary for runtime city peers.
- Updated architecture contract: peer systems must not store or call `RuntimeCitySpawnerSystem` when they only need `SpawnOnStartEnabled`, `HasSpawned`, or `IsGenerating`.
- Added architecture guard `RuntimeCityPeerSystemsMustUseRuntimeCityReadModelSystem`.
- Updated runtime city roadmap and responsibility audit for step 26 completion.

## User-visible behavior
No intended gameplay behavior change. Runtime blockers and decorations still wait for runtime city generation when city auto-spawn is enabled, but they now read that state through `RuntimeCityReadModelSystem`.

## Validation run
- `git diff --check --` on touched files.
- Unity batchmode: `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
- Unity batchmode: `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`.

## Validation result
- Diff check passed.
- Architecture validation passed: `[RuntimeCityArchitectureValidation] result=Passed methods=26`.
- Runtime city smoke passed: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.

## Known gaps
- `RuntimeCitySpawnerSystem` still exists as the temporary public compatibility shell and is still constructed by `GameplayFeatureStartupSystem`.
- Some UI/building binding still receives the spawner shell for compatibility; this is the deletion blocker for step 27.

## Cross-lane impacts
- Architecture lane: contract now rejects peer systems depending directly on `RuntimeCitySpawnerSystem` for city state.
- UI/building lane: no API behavior changed, but future deletion of the spawner shell will require migrating remaining binding surfaces.

## Next recommended task
Step 27: audit remaining production/test callers of `RuntimeCitySpawnerSystem`, migrate non-essential callers to narrower boundaries, then rename or delete the temporary spawner shell if no compatibility surface remains.
