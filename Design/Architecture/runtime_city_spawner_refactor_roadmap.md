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
   - Created `RuntimeCityVisualSystem`.
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

13. Blocked: Focused validation
    - Architecture validation passed in `WarlineCapture-CodexUnity1` with `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
    - Added `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation` to load `Game` and validate runtime city, road, building-spawn, and blocker wiring.
    - Game-scene smoke validation currently fails because `Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset` has `cityCount: 0`, so runtime city generation is disabled and city/road/building spawn output cannot be verified in `Game`.
    - Next validation unblock: decide whether `Game` should re-enable runtime city generation for this smoke, or run the smoke against a dedicated validation scene/config that keeps runtime city generation enabled without changing the player's current performance profile.
