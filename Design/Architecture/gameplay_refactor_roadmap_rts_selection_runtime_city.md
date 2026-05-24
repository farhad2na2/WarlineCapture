# Gameplay Refactor Roadmap: RTS Selection And Runtime City

This document preserves the two active architecture refactor roadmaps so the work does not drift between implementation passes.

## RTSSelectionSystem 13-Step Plan

Target file: `Assets/Game/Scripts/Systems/RTSSelectionSystem.cs`

Goal: reduce `RTSSelectionSystem` from a gameplay facade into a small input orchestration shell, with gameplay state, query, command, visual marker, HUD, and transport behavior owned by narrow systems.

1. Complete: Mechanical ownership move
   - Move `RTSSelectionSystem` and `RoadBuildSystem` out of `Assets/Game/Scripts/UI`.
   - Keep public APIs stable.
   - Add architecture guard preventing either file from returning under UI ownership.

2. Complete: Extract focusable unit lookup
   - Create `FocusableUnitLookupSystem`.
   - Own clicked-unit lookup cache, changed-grid/footprint queries, padded footprint lookup, and focusable candidate policy.

3. Complete: Extract visible screen selection
   - Create `VisibleUnitSelectionSystem`.
   - Own visible player-unit query, select-all all/soldiers/vehicles filtering, screen-rectangle collection, and selected-tag application.

4. Complete: Extract focused-unit command actions
   - Create `FocusedUnitCommandSystem`.
   - Own destroy/health-zero, return-to-base respawn lookup, focused auto-attack cleanup, radar attack issue, and hold/stop selected-unit cleanup.

5. Complete: Extract selected-order preservation
   - Move `PreserveSelectedUnitOrders`, `RestorePreservedUnitOrders`, `PreservedOrderState`, and restore helpers into `SelectedUnitOrderSnapshotSystem`.

6. Complete: Extract building-target move order path
   - Move remaining move-to-building/base-breach target logic and direct movement component writes into a narrow command system.

7. Complete: Extract transport boarding orchestration
   - Move selected boarding-source collection, clicked/nearby transport resolution, boarding order creation, and boarding diagnostics coordination out of `RTSSelectionSystem`.

8. Complete: Extract focused-unit lifecycle
   - Move `RefreshFocusedUnit`, `FocusUnitEntity`, `TryFocusUnit`, selected tag/focus sync, and clear-selection focus handling into a dedicated selection focus system.

9. Complete: Extract attack-click orchestration
   - Move clicked attack target handling, attack validation dispatch, base-breach target resolution bridge, and attack marker command result handling into a narrow attack command system.

10. Complete: Extract order marker visual runtime
    - Move move/attack marker prefab instantiation, material property blocks, show/hide timers, and marker positioning into `SelectionOrderMarkerSystem`.

11. Pending: Extract HUD command/selection feedback
    - Move `BattleHudGameplayBridge` selection text, command mode, command result, and world marker visibility calls into a HUD feedback boundary.

12. Pending: Collapse camera-facing wrappers
    - Review remaining camera public methods.
    - Move direct callers to `RtsCameraSystem` where practical or keep only thin compatibility wrappers.

13. Pending: Final facade pass
    - Confirm `RTSSelectionSystem` owns no gameplay state, ECS mutation policy, visual marker lifecycle, transport/attack/building command logic, or HUD behavior.
    - Add/remove architecture guards.
    - Decide whether to keep a tiny input orchestration shell or retire/rename it.

## RuntimeCitySpawnerSystem 13-Step Plan

Target file: `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`

Goal: split runtime city generation before adding more map/city gameplay, so layout, road planning, prefab selection, visual realization, ECS spawn bridging, and walkability publication are separate responsibilities.

1. Pending: Audit current responsibilities
   - Inventory fields and methods into config, layout, road network, plot reservation, building selection, visual spawn, ECS/runtime spawn requests, decoration, validation/debug.
   - Write or update an architecture report before edits.

2. Pending: Extract city config read model
   - Create `RuntimeCityConfigSystem`.
   - Own derived config values, seed/default handling, bounds, density, and counts.
   - `RuntimeCitySpawnerSystem` should only request a config snapshot.

3. Pending: Extract city layout planning
   - Create `RuntimeCityLayoutSystem`.
   - Own district/city area planning, block layout, plot candidate generation, and reserved footprints.
   - Output plain data: roads, plots, blocked areas, and city zones.

4. Pending: Extract road layout planning
   - Create `RuntimeCityRoadLayoutSystem`.
   - Own procedural road strokes, intersections, and road reservations.
   - No prefab spawning in this system.

5. Pending: Extract building plot selection
   - Create `RuntimeCityBuildingPlotSystem`.
   - Own building footprint fit, plot scoring, valid placement filtering, and plot reservation.
   - No GameObject instantiation.

6. Pending: Extract prefab selection
   - Create `RuntimeCityPrefabSelectionSystem`.
   - Own weighted/random prefab choice, faction/city/military category choice, and fallback prefab policy.
   - No placement logic.

7. Pending: Extract visual realization
   - Create `RuntimeCityVisualSystem`.
   - Own GameObject instantiation, parent/root assignment, rotation, scale, and decoration visual placement.
   - Consume authored placement data only.

8. Pending: Extract ECS spawn request bridge
   - Create or reuse `BuildingRuntimeCitySpawnSystem`.
   - Own city building runtime/ECS spawn requests and building-registration handoff.
   - `RuntimeCitySpawnerSystem` must not write building runtime data directly.

9. Pending: Extract RoadBuild coupling
   - Create `RuntimeCityRoadBuildBridgeSystem`.
   - Own calls into `RoadBuildSystem`, road tile commit, and road/blocker sync.
   - Prevent city generation from knowing RoadBuild internals.

10. Pending: Extract occupancy/walkability publication
    - Create `RuntimeCityWalkabilitySystem`.
    - Own blocked cells, walkable reservations, and city obstruction publication.
    - Required before deeper unit movement/city gameplay.

11. Pending: Reduce RuntimeCitySpawnerSystem to orchestrator
    - Keep only sequence orchestration: read config, plan layout, publish roads/walkability, choose prefabs, spawn visuals/buildings.
    - No algorithm-heavy methods should remain.

12. Pending: Architecture tests
    - Guard that `RuntimeCitySpawnerSystem` does not contain prefab random selection logic, road stroke generation, direct building runtime spawn writes, or large plot/footprint algorithms.
    - Guard new systems exist and are called.

13. Pending: Focused validation
    - Run architecture tests.
    - Run runtime city generation smoke validation.
    - Load `Game` scene and verify city, roads, buildings, and blockers still appear.
    - If Unity is locked, use `WarlineCapture-CodexUnity1`, `WarlineCapture-CodexUnity2`, or `WarlineCapture-CodexUnity3` for batch validation.
