# WarlineCapture Handoff

Lane: Gameplay

Task: Complete `RuntimeCityBuildingSpawnSystem` refactor roadmap steps 34-36.

Files changed:
- `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnContextSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityCompositionSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityGenerationSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs` deleted
- `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs.meta` deleted
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/runtime_city_building_spawn_system_refactor_roadmap.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/runtime_city_spawner_responsibility_audit.md`

Contracts touched:
- Runtime-city building-spawn contract now records that the coordinator shell was deleted in step 35.
- Building-spawn dependency bundling now belongs to `RuntimeCityBuildingSpawnContextSystem.Systems`.
- Architecture guards now prevent `RuntimeCityBuildingSpawnSystem.cs`, `RuntimeCitySpawnerSystem.cs`, or broad manager/controller/facade replacements from returning.

User-visible behavior:
- No intended gameplay or layout behavior change.
- Runtime city generation still uses the same building counts, spawn bridge, road/walkability semantics, deferred side-effect order, yield cadence, and random-state handoff.

Validation run:
- `git diff --check` on touched files.
- `GameplayArchitectureContractTests.RunRuntimeCityBuildingSpawnArchitectureBatchValidation`
- `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`
- `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`
- Unity TestRunner command for `BuildingPlacementValidationSystemTests`
- Unity TestRunner command for `BootstrapAndMenuPlayModeTests`
- `RuntimeFpsPlayButtonProbe.Run`

Validation result:
- Focused building-spawn architecture: passed, methods=7.
- Runtime-city architecture: passed, methods=28.
- Runtime-city Game scene smoke: passed, `cityPrefabs=36`, `productionCityCount=1`, `validationCityCount=1`, `buildingSpawnables=32`, `blockerPrefabs=63`.
- Runtime FPS play-button probe: completed, `avgFps=297.0`, `minFps=3.3`, `maxFps=387.4`.
- Unity TestRunner commands exited cleanly but did not emit XML or summary lines.

Known gaps:
- Unity QuickSearch emitted its existing startup indexing exception during the FPS probe.
- `UnitPathfindingPendingStateReadSystem.Dispose` logged an EntityQuery disposal null reference during editor teardown after the FPS probe result was written.
- TestRunner XML output is still unavailable for the named EditMode/PlayMode filters in batchmode.

Cross-lane impacts:
- Runtime city source no longer contains `RuntimeCityBuildingSpawnSystem.cs`; any other lane referencing that type must move to `RuntimeCityBuildingSpawnContextSystem.Systems` or direct child systems.
- No config or scene asset migration was performed.

Next recommended task:
- Audit the shutdown-time `UnitPathfindingPendingStateReadSystem.Dispose` EntityQuery disposal path separately; it is outside runtime-city building spawn but appears in runtime probe teardown logs.
