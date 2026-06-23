# RuntimeCitySpawnerSystem Refactor Roadmap

This document owns the runtime city generation refactor plan. Keep the RTS selection roadmap in `gameplay_refactor_roadmap_rts_selection_runtime_city.md`; city generation work should be tracked here so the responsibilities do not drift together.

## RuntimeCitySpawnerSystem 13-Step Plan

Target file: `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`

Goal: split runtime city generation before adding more map/city gameplay, so layout, road planning, prefab selection, visual realization, ECS spawn bridging, and walkability publication are separate responsibilities.

1. Complete: Audit current responsibilities
   - Inventory fields and methods into config, layout, road network, plot reservation, building selection, visual spawn, ECS/runtime spawn requests, decoration, validation/debug.
   - Architecture report: `runtime_city_spawner_responsibility_audit.md`.
   - Baseline architecture guard expectations are in `GameplayArchitectureContractTests` so drift is visible during the refactor instead of only at the final validation gate.

2. Complete: Extract city config read model
   - Created `RuntimeCityConfigSystem`.
   - Owns config snapshot/default handling, seed values, density, counts, placement policy numbers, and prefab category lists.
   - `RuntimeCitySpawnerSystem` now consumes a `RuntimeCityConfigSystem.Snapshot` instead of copying every config field locally.

3. Complete: Extract city layout planning
   - Created `RuntimeCityLayoutSystem`.
   - Owns layout data containers, town-radius calculation, chain-axis/center planning, road-grid bounds, base exclusion checks, and city-center spacing.
   - `RuntimeCitySpawnerSystem` now asks `RuntimeCityLayoutSystem` for layout planning helpers while road planning is owned by `RuntimeCityRoadLayoutSystem`.

4. Complete: Extract road layout planning
   - Created `RuntimeCityRoadLayoutSystem`.
   - Owns town road strokes, straight road paths, city-to-city autobahn paths, autobahn anchor selection, and low-level stroke segment helpers.
   - `RuntimeCitySpawnerSystem` commits roads through `RuntimeCityRoadBuildBridgeSystem`.

5. Complete: Extract building plot selection
   - Created `RuntimeCityBuildingPlotSystem`.
   - Owns plot candidate data, roadside/entry/corridor/scatter plot planning, adjacent decoration origin planning, scatter plot selection, plot spacing checks, and plot-to-origin centering.
   - `RuntimeCitySpawnerSystem` owned the actual building/decor spawn loops until step 11 moved them behind `RuntimeCityBuildingSpawnSystem`.

6. Complete: Extract prefab selection
   - Created `RuntimeCityPrefabSelectionSystem`.
   - Owns random prefab choice, configured-prefab membership checks, list shuffling, cached footprint estimation, major/minor footprint helpers, and renderer-bounds based footprint sizing.
   - `RuntimeCitySpawnerSystem` still passes category lists from the config snapshot, but no longer owns prefab random selection or footprint cache algorithms.

7. Complete: Extract visual realization
   - Created `RuntimeCityVisualPresentationSystemHelper`.
   - Owns `RuntimeCityVisuals` root creation, GameObject visual-only instantiation, parent/root assignment, footprint-center positioning, rotation/scale setup, local-bounds centering, and child visibility toggles.
   - `RuntimeCitySpawnerSystem` consumes footprint values from `RuntimeCityPrefabSelectionSystem`.

8. Complete: Extract ECS spawn request bridge
   - Created `RuntimeCitySpawnBridgeSystem` over the existing `BuildingRuntimeCitySpawnSystem`.
   - Owns city building runtime/ECS spawn/delete calls and deferred side-effect begin/end handoff.
   - `RuntimeCitySpawnerSystem` still receives the managed spawn dependencies at startup, but no longer stores `BuildingRuntimeCitySpawnSystem`, its context, or private spawn/delete wrappers.

9. Complete: Extract RoadBuild coupling
   - Created `RuntimeCityRoadBuildBridgeSystem`.
   - Owns calls into `RoadBuildSystem`, road cell-size queries, deferred road ECS sync, road/autobahn stroke commit, and temporary standalone straight-chain connector handoff.
   - `RuntimeCitySpawnerSystem` still receives the managed road build dependency at startup, but no longer stores `RoadBuildSystem` or calls road build methods directly.

10. Complete: Extract occupancy/walkability publication
    - Created `RuntimeCityWalkabilitySystem`.
    - Owns reserved footprint data, entrance-corridor reservation, reserved-footprint spacing checks, road-overlap checks, yard-fit validation, rectangle expansion, and adjacency/touch validation.
    - `RuntimeCitySpawnerSystem` temporarily sequenced building/decor spawn attempts until step 11; occupancy validation and reservations no longer live in the plot system.

11. Complete: Reduce RuntimeCitySpawnerSystem to orchestrator
    - Created `RuntimeCityBuildingSpawnSystem`.
    - Owns city hall/landmark placement, corridor entrance building placement, bulk roadside/rural building placement, yard wall visuals, and decoration building sequencing.
    - `RuntimeCitySpawnerSystem` now orchestrates generation lifecycle and delegates building/decor spawn algorithms to the building spawn boundary.

12. Complete: Architecture tests
    - Added and tightened `GameplayArchitectureContractTests` guards for each extracted runtime-city boundary.
    - Final guard asserts that `RuntimeCitySpawnerSystem` delegates to config, layout, road layout, plot, walkability, prefab selection, building spawn, visual, spawn bridge, and road-build bridge systems.
    - Final guard asserts that prefab random selection logic, road stroke generation, direct building runtime spawn writes, visual instantiation, building/decor spawn sequencing, and large plot/footprint algorithms do not return to `RuntimeCitySpawnerSystem`.
    - Roadmap and audit checks are part of the guard, so future refactor drift must update the contract intentionally.

13. Complete: Focused validation blocker recorded
    - Architecture validation passed in `WarlineCapture-CodexUnity1` with `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
    - Added `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` to load `Game` and validate runtime city, road, building-spawn, and blocker wiring.
    - The validation blocker was that the smoke depended on the normal `Game_RuntimeCitySpawner_Config.asset` runtime-city profile, which may be disabled or tuned down for gameplay performance.
    - Step 14 resolves this by giving the smoke its own in-memory validation override.

## RuntimeCitySpawnerSystem Phase 2 Deletion Plan

Target file: `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`

Goal: remove the remaining managed city orchestrator responsibilities before adding more city/map features. The existing extracted systems should stay as owners for config, layout, roads, plots, walkability, prefab selection, visuals, road-build bridging, and runtime building spawn bridging. `RuntimeCitySpawnerSystem` should either disappear or become a very small compatibility boundary with no gameplay policy, no per-frame generation logic, and no direct ECS query ownership.

Non-goals:
- Do not rewrite city visuals or road aesthetics in this refactor pass.
- Do not add new city gameplay features while retiring the orchestrator.
- Do not move logic back into `RoadBuildSystem`, `BuildingGameplaySystem`, `GameBootstrap`, or UI views.
- Do not add singleton/static runtime state.

14. Complete: Unblock focused validation
    - `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` now creates an in-memory validation copy of the scene runtime-city config.
    - The validation copy forces `spawnOnStart = true`, `generateBuildings = true`, and `cityCount >= 1` without dirtying or saving `Game_RuntimeCitySpawner_Config.asset`.
    - The production asset remains the source of prefab/config references, so missing prefabs and spawnable-registration mistakes are still caught.
    - Expected output: the smoke can validate runtime city config, road config, building-spawn wiring, and grid blocker wiring without requiring the normal gameplay profile to keep runtime city generation enabled.

15. Complete: Extract city lifecycle state
    - Created `RuntimeCityLifecycleSystem`.
    - Owns spawned/generating state, generation routine ownership, generation start/end frame counters, generation move-next counters, generation diagnostic cadence, and generation yield cadence.
    - `RuntimeCitySpawnerSystem` now delegates `HasSpawned`, `IsGenerating`, generation begin/tick/complete/cancel, and `ShouldYield` to the lifecycle boundary.
    - `RuntimeCitySpawnerSystem.Update()` no longer directly owns or advances the generation `IEnumerator`.

16. Complete: Extract runtime city startup gate
    - Created `RuntimeCityStartupSystem`.
    - Owns spawn-on-start evaluation, play-request readiness, city-count checks, M01/mission exclusion policy, spawn-system availability checks, road-system availability checks, prefab-list readiness, initial-unit readiness gating, and initial-unit wait diagnostic cadence.
    - `RuntimeCitySpawnerSystem.TryAutoSpawn()` now consumes a narrow startup result: `None`, `MarkSpawned`, or `Generate`.
    - Manual `GenerateCity()` also uses the startup gate's dependency/readiness result so dependency checks do not remain duplicated in the spawner.

17. Complete: Extract ECS query/readiness ownership
    - Created `RuntimeCityReadinessQuerySystem`.
    - Owns grid-data query caching, `TryGetGridData`, `TryGetGridConfig`, `HasPendingInitialUnitsSpawn`, and initial base exclusion road-rect collection.
    - Caches grid-data queries per world and clears cached query ownership during runtime city disposal.
    - `RuntimeCitySpawnerSystem` no longer has `Unity.Entities` or `Unity.Collections` dependencies and no longer owns `World`, `EntityQuery`, `EntityManager`, `Allocator`, or direct ECS readiness query setup.

18. Complete: Extract city generation sequence
    - Created `RuntimeCityGenerationSystem`.
    - Owns `GenerateCity` begin flow, `GenerateCityRoutine`, deferred road sync begin/end ordering, deferred spawn side-effect begin/end ordering, city-list lifetime, RNG lifetime, bulk-building routine stepping, and completion notification.
    - Keeps the existing extracted systems as dependencies and receives the still-pending city-chain/road-commit helpers as explicit delegates until steps 19-21 extract those policies.
    - `RuntimeCitySpawnerSystem` now starts generation through `RuntimeCityGenerationSystem.TryBegin(...)` and no longer owns the city generation coroutine.

19. Complete: Extract city-chain connection policy
    - Created `RuntimeCityChainSystem`.
    - Owns `TryPlanNextCity`, city travel-direction selection, reverse-direction avoidance, target-center candidate policy, city spacing checks, autobahn length policy, source/target connection-cell resolution, city exit validation, and autobahn path validation.
    - Keeps low-level stroke/path construction inside `RuntimeCityRoadLayoutSystem`.
    - `RuntimeCityGenerationSystem` now requests next-city planning through `RuntimeCityChainSystem`; `RuntimeCitySpawnerSystem` no longer owns city-chain travel policy.

20. Complete: Extract city road commit sequence
    - Created `RuntimeCityRoadCommitSystem`.
    - Owns `CommitCityRoadNetwork`, `PopulateCityRoadCells`, source-exit road commit, autobahn commit, standalone connector handoff, occupied-road-cell mutation, and road commit failure result codes.
    - Keeps actual road-build calls inside `RuntimeCityRoadBuildBridgeSystem`.
    - `RuntimeCityGenerationSystem` now requests road commits through a narrow result-returning boundary; `RuntimeCitySpawnerSystem` no longer owns city road commit helpers.

21. Complete: Extract incoming connector/ingress helpers
    - Created `RuntimeCityIngressSystem`.
    - Owns `CreateCityLayout`, incoming-anchor wiring, inner connection-cell math, city connection offset math, and ingress-corridor pruning.
    - `RuntimeCityGenerationSystem` and `RuntimeCityChainSystem` now request city layout and ingress connector policy through `RuntimeCityIngressSystem`.
    - Expected output: `RuntimeCitySpawnerSystem` no longer owns city connection helper math.

22. Complete: Extract diagnostics/events
    - Created `RuntimeCityDiagnosticSystem`.
    - Owns runtime city state diagnostics, warning formatting, generation wait diagnostics, hall-placement failure diagnostics, city-chain planning failure diagnostics, and road commit failure diagnostics.
    - Runtime city lifecycle, startup, generation, road commit, and building spawn systems now publish diagnostics through `RuntimeCityDiagnosticSystem` instead of formatting direct `Debug.Log*` calls.
    - Expected output: city generation emits diagnostics through one runtime-city diagnostic boundary, not direct gameplay logs.

23. Complete: Move minimap notification to result/event boundary
    - Created `RuntimeCityMinimapEventSystem`.
    - `RuntimeCityGenerationSystem` now publishes a static-minimap-changed event through `RuntimeCityMinimapEventSystem` instead of receiving or invoking a direct UI callback.
    - `RuntimeCityMinimapEventSystem` owns the UI-facing flush to `MainMenuPlayUI.NotifyStaticMinimapChanged`.
    - Expected output: runtime city generation has no direct UI reference.

24. Complete: Remove runtime root ownership from the spawner
    - Runtime root creation/lookup remains in `RuntimeRootSystem`.
    - Runtime city visual root ownership remains in `RuntimeCityVisualPresentationSystemHelper`.
    - Removed `_runtimeRoot` storage from `RuntimeCitySpawnerSystem`; the spawner only passes the composed root into `RuntimeCityVisualPresentationSystemHelper`.
    - Expected output: city generation does not own scene hierarchy composition.

25. Complete: Move composition out of the spawner constructor path
    - Created `RuntimeCityCompositionSystem`.
    - Owns creation/wiring of `RuntimeCityConfigSystem`, `RuntimeCityLifecycleSystem`, `RuntimeCityStartupSystem`, `RuntimeCityReadinessQuerySystem`, `RuntimeCityGenerationSystem`, `RuntimeCityChainSystem`, `RuntimeCityRoadCommitSystem`, `RuntimeCityIngressSystem`, `RuntimeCityMinimapEventSystem`, `RuntimeCityDiagnosticSystem`, plot/walkability/prefab/visual/bridge systems, context factories, update orchestration, and disposal.
    - `RuntimeCitySpawnerSystem` is now a thin public shell delegating init, update, dispose, public generation, and house-prefab queries to `RuntimeCityCompositionSystem`.
    - Expected output: startup composition is explicit and narrow.

26. Complete: Migrate peer dependencies off `RuntimeCitySpawnerSystem`
    - Created `RuntimeCityReadModelCompositionSystemHelper` as the narrow city state read boundary for peer systems.
    - `RuntimeCityCompositionSystem` publishes `SpawnOnStartEnabled`, `HasSpawned`, and `IsGenerating` into the read model.
    - `RuntimeGridBlockerPresentationSystemHelper` and `RuntimeDecorationSpawnerPresentationSystemHelper` now depend on `RuntimeCityReadModelCompositionSystemHelper` instead of storing or calling the broad `RuntimeCitySpawnerSystem` shell.
    - Expected output: no peer system stores or calls the broad spawner type.

27. Complete: Delete the spawner shell
    - Deleted `RuntimeCitySpawnerSystem.cs` and `.meta`.
    - `GameBootstrap`, `GameplayFeatureStartupCompositionSystemHelper`, `GameplayRuntimeUpdateSystem`, and building gameplay binding now use `RuntimeCityCompositionSystem` directly.
    - Runtime city update diagnostics now report the step as `RuntimeCity`.
    - Expected output: no broad managed `RuntimeCitySpawnerSystem` orchestrator remains.

28. Complete: Architecture contract and guards
    - Updated `gameplay_solid_ecs_contract.md` with the final runtime-city ownership map after shell deletion.
    - Added hard guard coverage for the deleted `RuntimeCitySpawnerSystem.cs` shell and final contract wording.
    - Documented serialized config names as explicit data compatibility debt: `RuntimeCitySpawnerSystemConfig`, `RuntimeCitySpawnerSystemSceneConfigAsset`, and `Game_RuntimeCitySpawner_Config.asset` may remain until a separate asset migration plan exists.

29. Complete: Validation gate
    - `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation` passed in `WarlineCapture-CodexUnity1` with 28 runtime-city guards.
    - `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` passed with runtime city generation forced on through the dedicated in-memory validation path: `productionCityCount=1`, `validationCityCount=1`, `cityPrefabs=36`, `buildingSpawnables=32`, `blockerPrefabs=63`.
    - Added and ran `RuntimeCitySpawnerStep13Validation.RunGameSceneCityDisabledValidation` to prove normal gameplay can keep the production asset's references while validating with `cityCount=0`; result passed with `productionCityCount=1`, `validationCityCount=0`.
    - `RuntimeFpsPlayButtonProbe.Run` completed and clicked the Game button without fallback; result `completed`, `avgFps=158.83`, `sampleCount=6896`, and four frame-rate diagnostics captured.
    - Unity TestRunner playmode filtering exited without emitting result XML, so the project runtime play-button probe is the recorded bootstrap/menu smoke evidence for this gate.
    - Runtime probe note: one `UnityEditor.Search.SearchDatabase` QuickSearch indexing `ArgumentOutOfRangeException` appeared during editor startup and is outside gameplay/runtime-city code; generation startup also still has expected one-time hitches while city/building data initializes.
