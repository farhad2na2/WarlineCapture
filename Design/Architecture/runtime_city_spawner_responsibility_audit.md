# RuntimeCitySpawnerSystem Responsibility Audit

Status: Step 12 complete; Step 13 validation blocked by Game-scene runtime city config. This audit tracks the runtime city refactor baseline and the responsibilities already extracted.

Source file: `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`

Current size: 861 lines.

## Current Responsibilities

- Config snapshot consumption: receives `RuntimeCitySpawnerSystemConfig`, delegates projection/defaults to `RuntimeCityConfigSystem`, and consumes the resulting snapshot through read-only accessors.
- Generation lifecycle: owns `Init`, `InitForRoadOnly`, `Update`, `TryAutoSpawn`, `GenerateCity`, `GenerateCityRoutine`, coroutine yielding, and `_spawned` state.
- ECS/grid access: owns `EntityQuery` setup and `TryGetGridData` for grid, road, and blocker buffers.
- Base exclusion planning: collects initial base exclusion road rects before city planning.
- City layout planning consumption: delegates town radius, city-center planning, road-grid bounds, base exclusion checks, and city-center spacing to `RuntimeCityLayoutSystem`; still owns city chain travel, connection cells, ingress corridor pruning, and city road stroke creation until later steps.
- Road layout planning consumption: delegates town road strokes, straight road paths, city-to-city autobahn paths, autobahn anchor selection, and stroke segment helpers to `RuntimeCityRoadLayoutSystem`; commits planned roads through `RuntimeCityRoadBuildBridgeSystem`.
- Road build bridge consumption: delegates road cell-size queries, deferred road ECS sync, road/autobahn stroke commit, and temporary standalone straight-chain connector handoff to `RuntimeCityRoadBuildBridgeSystem`.
- Plot planning consumption: delegates plot candidate data, roadside/entry/corridor/scatter plot planning, adjacent decoration origin planning, scatter plot selection, plot spacing checks, and plot-to-origin centering to `RuntimeCityBuildingPlotSystem`.
- Walkability and occupancy consumption: delegates reserved footprint data, entrance-corridor reservation, reserved-footprint spacing checks, road-overlap checks, yard-fit validation, rectangle expansion, and adjacency/touch validation to `RuntimeCityWalkabilitySystem`.
- Building/decor spawn sequencing consumption: delegates city hall/landmark placement, corridor entrance building placement, bulk roadside/rural building placement, yard wall visuals, and decoration building sequencing to `RuntimeCityBuildingSpawnSystem`.
- Prefab selection consumption: delegates configured-prefab checks, random prefab selection, list shuffling, cached footprint estimation, major/minor footprint helpers, and renderer-bounds based footprint sizing to `RuntimeCityPrefabSelectionSystem`.
- Visual realization consumption: delegates runtime city visual root creation, visual-only prefab instantiation, transform placement, local-bounds centering, and child visibility toggles to `RuntimeCityVisualSystem`.
- Building runtime spawn bridge consumption: delegates city building spawn/delete calls and deferred side-effect begin/end handoff to `RuntimeCitySpawnBridgeSystem`, which wraps the existing `BuildingRuntimeCitySpawnSystem`.
- Walkability and occupancy side effects: consumes `RuntimeCityWalkabilitySystem`; later ECS obstruction publication can extend that boundary without returning reservation logic to the spawner.
- Diagnostics and validation: owns runtime city diagnostics toggles, pending initial-unit checks, fallback logging, and defensive validation.

## Current Allowed Debt

- `RuntimeCitySpawnerSystem` remains the temporary city-generation orchestrator while steps 5-10 extract responsibilities.
- It may receive `BuildingRuntimeCitySpawnSystem` and context at startup only so `RuntimeCitySpawnBridgeSystem` can be configured.
- It may receive `RoadBuildSystem` at startup only so `RuntimeCityRoadBuildBridgeSystem` can be configured.
- It may own remaining city-chain and road sequencing methods only until their matching roadmap steps are completed.
- It must not reintroduce copied `RuntimeCitySpawnerSystemConfig` field assignment; config projection belongs in `RuntimeCityConfigSystem`.
- It must not reintroduce town radius, city-center planning, road-grid bounds, base exclusion checks, or city-center spacing helpers; those belong in `RuntimeCityLayoutSystem`.
- It must not reintroduce town road stroke generation, straight path generation, autobahn anchor selection, or low-level stroke segment helpers; those belong in `RuntimeCityRoadLayoutSystem`.
- It must not reintroduce stored `RoadBuildSystem` state or direct RoadBuild method calls; road cell sizing, deferred road ECS sync, stroke commit, autobahn commit, and straight-chain connector handoff belong in `RuntimeCityRoadBuildBridgeSystem`.
- It must not reintroduce plot candidate data, roadside/entry/corridor/scatter plot helpers, adjacent decoration origin planning, scatter plot selection, plot spacing checks, or plot-to-origin centering; those belong in `RuntimeCityBuildingPlotSystem`.
- It must not reintroduce reserved footprint data, entrance-corridor reservation, reserved-footprint spacing checks, road-overlap checks, yard-fit validation, rectangle expansion, or adjacency/touch validation into `RuntimeCityBuildingPlotSystem` or `RuntimeCitySpawnerSystem`; those belong in `RuntimeCityWalkabilitySystem`.
- It must not reintroduce random prefab selection, list shuffle, configured-prefab membership loops, prefab footprint cache state, renderer-bounds footprint estimation, or major/minor footprint helpers; those belong in `RuntimeCityPrefabSelectionSystem`.
- It must not reintroduce runtime city visual root ownership, visual-only prefab instantiation, footprint-center positioning, local-bounds centering, descendant visibility toggles, or visual wrapper transform setup; those belong in `RuntimeCityVisualSystem`.
- It must not reintroduce stored `BuildingRuntimeCitySpawnSystem` state, stored city-spawn context, private city spawn/delete wrappers, or direct deferred side-effect calls; those belong in `RuntimeCitySpawnBridgeSystem`.
- It must not call `BuildingPlacementSystem`, `BuildingPlacementSystem.Instance`, or `_buildingPlacement`.
- It must not reintroduce city hall/landmark placement, corridor entrance building placement, bulk roadside/rural building placement, yard wall visual algorithms, or decoration building sequencing; those belong in `RuntimeCityBuildingSpawnSystem`.
- New city gameplay must not add new responsibilities to `RuntimeCitySpawnerSystem`; add a new ECS boundary system or extend the planned owner below.

## Target Boundaries

- `RuntimeCityConfigSystem`: config snapshots, generation settings, seed/default handling, density, counts, placement policy numbers, and prefab category lists. Extracted in step 2.
- `RuntimeCityLayoutSystem`: city/district layout data, centers, road-grid bounds, base exclusion checks, and city-center spacing. Extracted in step 3.
- `RuntimeCityRoadLayoutSystem`: town road strokes, straight road paths, city-to-city autobahn paths, autobahn anchor selection, and low-level stroke segment helpers. Extracted in step 4.
- `RuntimeCityBuildingPlotSystem`: plot candidate data, plot scoring, roadside/entry/corridor/scatter plot planning, adjacent origin planning, plot spacing, and plot-to-origin centering. Extracted in step 5.
- `RuntimeCityPrefabSelectionSystem`: configured-prefab membership checks, random prefab selection, list shuffling, footprint estimation/cache, and major/minor footprint helpers. Extracted in step 6.
- `RuntimeCityVisualSystem`: GameObject instantiation, parent/root assignment, rotation, scale, child visibility, and decoration visuals. Extracted in step 7.
- `RuntimeCitySpawnBridgeSystem`: runtime city generation spawn/delete/deferred-side-effect bridge over `BuildingRuntimeCitySpawnSystem`. Extracted in step 8.
- `BuildingRuntimeCitySpawnSystem`: low-level city building runtime/ECS spawn and delete handoff.
- `RuntimeCityRoadBuildBridgeSystem`: calls into `RoadBuildSystem`, road cell-size queries, deferred road ECS sync, road/autobahn stroke commit, and temporary standalone straight-chain connector handoff. Extracted in step 9.
- `RuntimeCityWalkabilitySystem`: reserved footprint data, entrance-corridor reservation, reserved-footprint spacing checks, road-overlap checks, yard-fit validation, rectangle expansion, and adjacency/touch validation. Extracted in step 10.
- `RuntimeCityBuildingSpawnSystem`: city hall/landmark placement, corridor entrance building placement, bulk roadside/rural building placement, yard wall visuals, and decoration building sequencing. Extracted in step 11.

## Method Group Baseline

- Lifecycle: `Init`, `InitForRoadOnly`, `Update`, `Dispose`, `TryAutoSpawn`, `GenerateCity`, `GenerateCityRoutine`, `ShouldYield`.
- Config and ECS access: `ApplyConfigIfAvailable` delegates to `RuntimeCityConfigSystem`; `EnsureEntityQueries`, `TryGetGridData`, `HasPendingInitialUnitsSpawn`, and `CollectInitialBaseExclusionRoadRects` remain in `RuntimeCitySpawnerSystem`.
- Layout and roads: `CreateCityLayout`, `TryPlanNextCity`, `ConnectCitiesWithAutobahn`, and `CommitCityRoadNetwork` remain; town radius, city-center planning, road-grid bounds, and base exclusion checks are delegated to `RuntimeCityLayoutSystem`; road stroke/path planning is delegated to `RuntimeCityRoadLayoutSystem`.
- Road method handoff: `RuntimeCitySpawnerSystem` calls `RuntimeCityRoadLayoutSystem.BuildTownRoadStrokes`, `BuildStraightRoadPath`, `BuildCityToCityAutobahnPath`, and `AddStroke`; the implementations must stay out of the spawner.
- Road build handoff: `RuntimeCitySpawnerSystem` calls `RuntimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells`, `BeginDeferredRoadEcsSync`, `EndDeferredRoadEcsSync`, `CreateRoadStrokeFromRoadCells`, `CreateAutobahnStrokeFromRoadCells`, `CreateStandaloneStraightRoadChainFromConnector`, and `TryGetStandaloneStraightChainEndRoadCell`.
- Building placement and plots: city hall/landmark placement, corridor entrance building placement, bulk roadside/rural building placement, yard wall visuals, and decoration building sequencing are delegated to `RuntimeCityBuildingSpawnSystem`; plot data, roadside/entry/corridor/scatter planning, adjacent origin planning, and plot spacing helpers are delegated to `RuntimeCityBuildingPlotSystem`; reserved footprint, road-overlap, yard-fit, and touch helpers are delegated to `RuntimeCityWalkabilitySystem`.
- Prefabs and visuals: `RuntimeCityPrefabSelectionSystem.GetRandomPrefab`, `Shuffle`, `GetCachedFootprintCells`, `GetMajorFootprint`, and `GetMinorFootprint`; runtime city visual root creation and visual-only prefab instantiation are delegated to `RuntimeCityVisualSystem`.
- Runtime building bridge: `RuntimeCitySpawnBridgeSystem.TrySpawnCityBuilding`, `DeleteCityBuilding`, `BeginDeferredSideEffects`, and `EndDeferredSideEffects`.

## Step 1 Guards

- `GameplayArchitectureContractTests.RuntimeCitySpawnerSystemMustUseRuntimeCitySpawnBoundary` keeps the existing no-`BuildingPlacementSystem` rule.
- `GameplayArchitectureContractTests.RuntimeCitySpawnerRefactorDocsMustRecordBaselineAndTargetBoundaries` verifies the roadmap and this audit stay present and name the planned owners.
- `GameplayArchitectureContractTests.RuntimeCitySpawnerBaselineMustStayExplicitUntilExtracted` verifies current large responsibilities remain explicitly tracked until later steps tighten the guards.
- `GameplayArchitectureContractTests.RuntimeCityConfigProjectionMustLiveInRuntimeCityConfigSystem` prevents copied config assignment from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityLayoutPlanningMustLiveInRuntimeCityLayoutSystem` prevents city-center and road-grid layout helpers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityRoadLayoutPlanningMustLiveInRuntimeCityRoadLayoutSystem` prevents road stroke/path helpers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityBuildingPlotPlanningMustLiveInRuntimeCityBuildingPlotSystem` prevents plot candidate and plot-origin helpers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityWalkabilityMustLiveInRuntimeCityWalkabilitySystem` prevents reserved footprint and road-overlap helpers from returning to `RuntimeCityBuildingPlotSystem` or `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityPrefabSelectionMustLiveInRuntimeCityPrefabSelectionSystem` prevents prefab random selection, shuffle, and footprint-cache helpers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityVisualRealizationMustLiveInRuntimeCityVisualSystem` prevents runtime visual root/instantiation helpers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCitySpawnBridgeMustLiveInRuntimeCitySpawnBridgeSystem` prevents city generated building spawn/delete/deferred-side-effect wrappers from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityRoadBuildCouplingMustLiveInRuntimeCityRoadBuildBridgeSystem` prevents road build controller state and direct road build calls from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCityBuildingSpawnSequencingMustLiveInRuntimeCityBuildingSpawnSystem` prevents city building/decor spawn sequencing from returning to `RuntimeCitySpawnerSystem`.
- `GameplayArchitectureContractTests.RuntimeCitySpawnerFinalArchitectureGuardMustStayAlgorithmLight` is the final runtime-city guard for step 12. It verifies that `RuntimeCitySpawnerSystem` delegates to the extracted runtime city boundaries and does not reintroduce prefab random selection logic, road stroke generation, direct building runtime spawn writes, visual instantiation, building/decor spawn sequencing, or large plot/footprint algorithms.
- `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation` is the deterministic batch entry point used for step 13 architecture validation when Unity TestRunner does not emit result XML.
- `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` loads `Game` and validates runtime city, road, building-spawn, and blocker wiring. It currently reports the focused validation blocker: `Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset` has `cityCount: 0`, so runtime city generation is disabled and spawned city output cannot be verified in `Game`.
