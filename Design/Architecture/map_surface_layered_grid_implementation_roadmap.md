# Map Surface Layered Grid Implementation Roadmap

This roadmap owns the implementation plan for terrain height, slopes, roads, bridges, and multi-layer walkable surfaces in Match gameplay.

## Target

Replace the current implicit flat `y = 0` gameplay assumption with an ECS-owned, precomputed, layered map-surface data boundary.

Units, vehicles, buildings, pathfinding, road placement, runtime city placement, and selection/command targeting must query the same surface data. The implementation must preserve the no-physics gameplay model: no per-frame raycasts, no collider-dependent grounding, and no broad managed scene-object lookup.

## Architecture Goal

The map surface is gameplay data:

- `MapSurfaceComponent` owns the baked surface blob reference and grid metadata.
- `MapSurfaceQuerySystem` exposes allocation-free sampling contexts for height, normal, slope, surface type, and layer selection.
- `MapSurfaceConnectionSystem` owns explicit connectivity between terrain, roads, bridge decks, highways under bridges, ramps, and other layered surfaces.
- `UnitSurfaceTrackingSystem` keeps units on the correct surface/layer.
- `UnitGroundingSystem` applies height to units.
- `VehicleSlopeAlignmentSystem` applies visual pitch/roll from surface normals for vehicles.
- `BuildingSurfacePlacementSystem` validates building footprint height/slope and produces placement height.
- `PathfindingSurfaceCostSystem` feeds walkability, slope, road, bridge, and layer connectivity into pathfinding.

Authoring and baking are editor/shell edge responsibilities:

- `MapSurfaceAuthoring` identifies map geometry, road surfaces, bridge decks, ramps, highways, and sampling settings.
- `MapSurfaceBakeSystem` builds the baked surface data during editor validation/building.
- `BridgeSurfaceAuthoring` may be used only as an authored bridge/overpass marker. Runtime bridge behavior must be ECS surface data.

## Performance Rules

- Do not use runtime physics raycasts for normal unit/building grounding.
- Do not require colliders on map meshes for gameplay surface queries.
- Do not add per-frame managed allocations in movement, pathfinding, placement, or grounding.
- Do not add LINQ, reflection, boxed delegates, managed object graphs, or string formatting to hot surface-sampling paths.
- Keep surface samples in contiguous arrays or blob data.
- Most cells must store one inline surface; multi-surface cells must use sparse ranges.
- Pathfinding must not scan all surfaces in a cell every node expansion unless the cell is known to be layered.
- Surface/layer switching must happen only through explicit connection edges such as ramps, bridge approaches, gates, stairs, or authored transitions.
- Grounding and visual slope alignment must be separate from pathfinding so expensive visual smoothing does not affect path search.

## Data Model

Each gameplay grid cell may contain one or more surface samples.

Single-layer cells:

- One surface sample: terrain, road, dirt, plaza, etc.

Layered cells:

- Multiple surface samples with different `SurfaceId` / `LayerId`.
- Example: highway below bridge plus bridge deck above.

Surface sample fields:

- cell index
- surface id
- layer id
- height
- normal
- slope
- surface type
- movement flags
- road/bridge flags
- connection range

Connection fields:

- from surface id
- to surface id
- edge direction
- transition type: same-layer, ramp, bridge-approach, road-join, blocked
- movement mask: infantry, vehicle, tank, air-grounded, building placement where relevant

## Required Validation Gates

Each implementation phase must run:

- `git diff --check`
- focused architecture contract tests for map-surface ownership
- compile validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

Phase boundaries must also run:

- Match scene load smoke
- manual unit move over flat ground
- manual unit move over sloped ground
- manual vehicle/tank move over sloped ground with visual pitch/roll
- building placement on valid flat/near-flat surface
- invalid building placement on too-steep or height-uneven footprint
- road-to-bridge-to-road path
- highway-under-bridge path that does not jump to bridge deck
- bridge deck path that does not jump down to highway
- runtime FPS/frame diagnostics before and after grounding/pathfinding changes

## Non-Goals

- Do not redesign current pathfinding in the first pass.
- Do not change unit movement speeds, road costs, path budgets, path request ordering, or path scheduling as part of surface introduction.
- Do not make buildings automatically terraform the map in the first pass.
- Do not add physics-based movement.
- Do not create a `SurfaceManager`, `TerrainManager`, `BridgeController`, service locator, singleton, or broad facade.
- Do not move map-surface gameplay policy into bootstrap, UI, views, or editor scene builders.

## Step 1 Baseline Inventory

Current flat-ground behavior is spread across several gameplay boundaries. This inventory is the step-1 contract: future steps must migrate these touchpoints intentionally, while preserving current pathfinding behavior and performance.

Core grid/world conversion:

- `Assets/Game/Scripts/Components/GridComponents.cs`: `GridUtils.CellToWorldCenter` currently returns cell centers with `y = 0`.
- Current flat-grid `int2` cell math is not itself debt. The debt is converting those cells to world positions without querying a surface/layer.

Unit movement, facing, and grounding:

- `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs`: movement targets and final positions use `GridUtils.CellToWorldCenter`.
- `Assets/Game/Scripts/Systems/UnitGridSnapSystem.cs`: snaps entities to flat cell centers.
- `Assets/Game/Scripts/Systems/UnitIdleWanderSystem.cs`: computes wander origins/offsets on a flat plane.
- `Assets/Game/Scripts/Utilities/UnitVehicleMovementUtility.cs`: flattens forward vectors for yaw-only vehicle movement.
- `Assets/Game/Scripts/Systems/UnitLookAtTargetSystem.cs`, `UnitMoveVisualStateSystem.cs`, and `UnitEngagedMovementSystem.cs`: flatten target/direction deltas on y.
- `Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs`, `UnitTransportAirPickupSystem.cs`, `UnitTransportRopeDisembarkSystem.cs`, `UnitTransportRopeDisembarkCommandSystem.cs`, and `UnitDeathSystem.cs`: use explicit ground-y policies that must be routed through surface sampling where relevant.

Initial, runtime, and production spawn:

- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`, `InitialUnitSpawnApplySystem.cs`, `InitialBlockerSpawnSystem.cs`, and `InitialUnitsBlockerChurnSystem.cs`: initial spawn/blocker positions come from flat cell centers.
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs`: respawn position uses flat cell centers.
- `Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs` and `BuildingPlacementRedirectSystem.cs`: runtime unit/building spawn points and redirected produced units use flat cell centers.
- `Assets/Game/Scripts/Systems/BuildingRoadLegacyEcsSystem.cs`: legacy road/building entity placement still uses flat cell centers.

Building placement, visuals, production, and selection:

- `Assets/Game/Scripts/Systems/BuildingPlacementStartupSystem.cs`, `BuildingPlacementGridSystem.cs`, `BuildingPlacementVisualUpdateSystem.cs`, `BuildingRuntimeCompositionSystem.cs`, and `BuildingRuntimeCompositionQuerySystem.cs`: placement and runtime building centers use `BuildPlaneY` or flatten world y.
- `Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs`: building focus/selection positions flatten y.
- `Assets/Game/Scripts/Systems/BuildingProductionRequestSystem.cs`, `BuildingProductionTransportSystem.cs`, and `BuildingProductionTransportBridgeSystem.cs`: runway/transport bridge positions and directions assume flat ground or fixed offsets.

Road build, road visuals, and road footprint projection:

- `Assets/Game/Scripts/Systems/RoadBuildStartupSystem.cs`: initializes road `BuildPlaneY = 0`.
- `Assets/Game/Scripts/Systems/RoadBuildCompositionContextSystem.cs`: projects pointer input to a flat build plane.
- `Assets/Game/Scripts/Systems/RoadPreviewSystem.cs`, `RoadChunkVisualSystem.cs`, `RoadFootprintQuerySystem.cs`, and `RoadSpecialVisualSystem.cs`: preview, chunk, footprint, and special-road visuals use `BuildPlaneY` or y-zero directions.
- `Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs`: projects road tiles into flat `GridRoad` data. This must remain as the existing road/pathfinding compatibility source until surface flags are mapped in later steps.

Runtime city, decorations, and blockers:

- `Assets/Game/Scripts/Environment/RuntimeCityVisualSystem.cs`, `RuntimeDecorationSpawnerSystem.cs`, and `RuntimeGridBlockerSystem.cs`: runtime city visuals/decorations/blocker visuals use local flat y or flat cell centers.
- Runtime city building and road generation must keep existing counts, random order, yield points, and reservation rules while adopting surface queries.

Selection, camera focus, command targeting, and markers:

- `Assets/Game/Scripts/Systems/RtsCameraSystem.cs`: focus and camera ray projection assume a flat `y = 0` plane.
- `Assets/Game/Scripts/Systems/SelectionOrderMarkerSystem.cs`: order markers use grid-origin y plus a small offset.
- `Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs` and `SelectionTransportCommandRequestSystem.cs`: command targets and transport target positions resolve against flat grid/world coordinates.
- `Assets/Game/Scripts/Systems/SelectionUiQuerySystem.cs` and `MissionCameraSystem.cs`: selected/focused unit and mission camera vectors flatten y.

Pathfinding integration rule:

- Reuse the current `UnitPathfindingSystem` pipeline and extracted pathfinding child systems. Surface work must enter as read-only surface/walkability/cost context and, later, path result surface/layer metadata. Do not replace the existing optimized request scheduling, budgets, allocator lifetimes, path pooling, or grid traversal with a new pathfinder.

## Step Plan

1. Complete: Baseline current flat-ground contracts
   - Inventory all places that write `LocalTransform.Position.y = 0`, call `GridUtils.CellToWorldCenter`, spawn units/buildings at flat y, or assume flat road/building placement.
   - Record pathfinding, building placement, road, unit spawn, and runtime city touchpoints.
   - Add architecture test inventory guard for known flat-ground assumptions.

2. Complete: Add map-surface architecture contract guards
   - Update `GameplayArchitectureContractTests` to require the roadmap.
   - Guard against new runtime raycast-based grounding.
   - Guard against new collider dependency in movement/pathfinding/placement.
   - Guard against `SurfaceManager`, `TerrainManager`, `BridgeController`, and singleton-style surface ownership.

3. Complete: Define ECS surface components
   - Add `MapSurfaceComponent` as the ECS singleton data owner.
   - Add surface blob reference, grid origin, cell size, dimensions, and surface availability flags.
   - Keep behavior out of components.

4. Complete: Define unit/building surface components
   - Add `UnitSurfaceComponent` for current surface id, layer id, last sampled height, and grounding flags.
   - Add `BuildingSurfaceComponent` for placement surface id, layer id, foundation height, and slope summary.
   - Add vehicle visual alignment data only where needed, not to every unit.

5. Complete: Define authored surface config
   - Add `MapSurfaceAuthoring` with serialized sampling settings and a root bake controller.
   - Add `MapBakeGroupAuthoring` on map folders to classify terrain, road, bridge, ramp, blocker, and ignored decoration sources.
   - Add `BridgeSurfaceAuthoring` only for bridge/overpass metadata: bridge deck, lower pass-through surface, approaches, clearance, and allowed movement masks.
   - Keep authoring classes as reference/config binders only.

6. Complete: Add editor bake skeleton
   - Add `MapSurfaceBakeSystem` editor boundary.
   - It must create deterministic surface samples from authored geometry/config.
   - First version may output a single-layer flat surface equivalent to current behavior for regression safety.

7. Complete: Add baked data asset/output path
   - Store baked surface data as a generated asset or serialized blob source that can be loaded by Match startup.
   - Do not build runtime surface data by scanning scene hierarchy during gameplay.
   - Add validation that Match has exactly one active map surface asset/reference.

8. Complete: Add `MapSurfaceQuerySystem`
   - Create allocation-free context creation for surface sampling.
   - Add pure data sampling functions for cell lookup, surface lookup, bilinear height, normal, slope, and nearest valid surface.
   - Static helpers are allowed only if they are pure stateless data/math operations.

9. Complete: Validate flat-equivalent runtime
   - Wire the baked flat-equivalent surface into Match.
   - Confirm existing units/buildings still behave exactly as before.
   - This step proves the new boundary can exist without changing gameplay.

10. Complete: Add single-layer terrain height bake
   - Bake height and normal for the current Match map surface.
   - Use mesh/terrain source data at editor time.
   - Do not require runtime colliders.

11. Complete: Add `UnitGroundingSystem`
   - Sample the unit's current surface and apply `LocalTransform.Position.y`.
   - Preserve `x/z` movement ownership in existing movement/path systems.
   - Add ground offset support from unit config/authoring.

12. Complete: Add initial spawn grounding
   - Route initial unit spawn, respawn, produced-unit spawn, transport disembark, and runtime city/citizen spawn positions through surface sampling.
   - Preserve existing spawn order and counts.

13. Complete: Add building placement height query
   - Add `BuildingSurfacePlacementSystem` footprint sampling.
   - Compute average height, max height delta, max slope, and selected surface/layer.
   - Do not commit invalid uneven placements.

14. Complete: Add building runtime spawn grounding
   - Apply surface/foundation height to runtime-created buildings.
   - Keep buildings upright in first pass.
   - Preserve current building costs, production, and spawn semantics.

15. Complete: Add slope classification
   - Bake slope per surface sample.
   - Add movement masks for infantry, wheeled vehicle, tracked vehicle, and building placement.
   - Do not change path costs yet; only publish data.

16. Complete: Add pathfinding surface read context
   - Extend pathfinding request/job context with map-surface read data.
   - Preserve request budgets, scheduling, allocator lifetimes, and path output layout.
   - Keep flat behavior if no surface data exists.

17. Complete: Add slope walkability rejection
   - Reject cells/surfaces above movement-type max slope.
   - Preserve current walkability rules for flat maps.
   - Add tests for soldiers vs tanks if movement masks differ.
   - Added `MapSurfacePathingValidationSystem` as a narrow hot-path data boundary.
   - `PathfindBatchJob` now rejects cells/footprints whose primary surface cannot support the moving unit mask or exceeds the movement-type max slope.
   - The flat fallback remains explicitly inert through `hasSurfaceData == 0`, and traversal cost constants/budgets are unchanged.

18. Complete: Add slope movement cost
   - Add slope cost contribution behind a config flag or exact documented defaults.
   - Validate no regression in current flat map path costs.
   - Do not tune gameplay values in this step.
   - Added `MapSurfacePathCostComponent` and `MapSurfacePathCostSystem`.
   - Slope traversal cost is disabled by default and returns zero unless `EnableSlopeCost` is explicitly enabled on the map-surface entity.
   - Existing path traversal cost constants remain unchanged.

19. Complete: Add road surface priority
   - Mark road samples as road/bridge/highway.
   - Preserve existing road preference/cost behavior by mapping old road data to surface flags.
   - Keep road and surface ownership separate but queryable from the same path context.
   - Added `MapSurfaceRoadPrioritySystem` to normalize road/bridge/highway/ramp flags during baking.
   - `PathfindBatchJob` now routes existing sidewalk/dirt-road buffer priority through the surface road-priority boundary while preserving the same traversal cost constants.

20. Complete: Add multi-layer surface storage
   - Replace single-sample cell assumptions with `CellSurfaceRange`.
   - Store one surface inline where possible and sparse extra surfaces only for layered cells.
   - Validate memory and sampling cost.
   - Added `MapSurfaceCellSurfaceRange` and `MapSurfaceLayeredCellSystem`.
   - `MapSurfaceQuerySystem` now exposes range-based sampling while primary-surface sampling remains stable.
   - Pathfinding still uses the primary surface only; layered traversal switches are deferred to explicit connection steps.

21. Complete: Add bridge deck bake
   - Bake bridge deck surfaces above lower roads/highways.
   - Mark bridge surfaces with bridge type, road type, normal, height, and movement mask.
   - Do not connect bridge to lower highway unless an authored connector exists.
   - Added `MapSurfaceBridgeBakeSystem` for explicit bridge deck and approach bake sources.
   - Bridge decks are marked as `BridgeDeck` with road/bridge flags and layer id >= 1.
   - No bridge-to-lower-surface connection is generated in this step.

22. Complete: Add lower highway/road preservation under bridges
   - Ensure lower surfaces under bridges remain walkable/pathable.
   - Store separate surface ids and layer ids for lower and upper surfaces.
   - Validate highway path under bridge remains on lower layer.
   - Added explicit lower pass-through bake source metadata for road/highway surfaces under bridges.
   - Lower surfaces keep layer id >= 0 and bridge decks keep layer id >= 1, with no automatic vertical connection.

23. Complete: Add bridge/ramp connection bake
   - Bake explicit approach/ramp edges from road to bridge deck.
   - Bake same-layer edges along bridge deck.
   - Bake same-layer edges along highway underneath.
   - Do not infer vertical transitions by height proximity alone.
   - Added `MapSurfaceConnectionBakeSystem` to create explicit bridge-approach, ramp, bridge-deck same-layer, and lower-road same-layer connection records.
   - Connection bake clamps authored edge directions and does not inspect height/slope to infer layer transitions.

24. Complete: Add `MapSurfaceConnectionSystem`
   - Own connection validation and runtime read contexts.
   - Pathfinding must use connection edges for layered transitions.
   - Surface sampling must stay separate from connection traversal.
   - Added `MapSurfaceConnectionSystem` with allocation-free runtime connection context, indexed connection reads, movement-mask validation, and explicit connection lookup.
   - Surface sampling remains in `MapSurfaceQuerySystem`; layered path traversal consumes this boundary in later path-result/surface-id steps.

25. Complete: Add unit surface tracking
   - `UnitSurfaceTrackingSystem` updates current surface/layer from path result or current cell.
   - Units keep their layer while moving under/over bridges.
   - Layer changes only occur when path result crosses an explicit connection edge.
   - Added `UnitSurfaceTrackingSystem` to update `UnitSurfaceComponent` from the unit's current grid cell while preserving any already-known surface/layer when that layer exists in the cell.
   - `UnitGroundingSystem` now applies only the tracked height and ground offset to `LocalTransform.y`.
   - Path-result-driven layer transitions are deferred to the upcoming path result surface-id step.

26. Complete: Add path result surface ids
   - Extend path nodes or path-follow state with surface/layer ids where needed.
   - Preserve path pool allocator behavior.
   - Validate no managed allocation or path output regression.
   - Added `UnitPathSurfaceNode` as a parallel ECS buffer for path surface/layer metadata.
   - `UnitPathResultApplySystem` now writes surface metadata during result application while keeping the path pool and job `NativeStream` output as `int2` cells.
   - `UnitPathSurfaceMetadataSystem` resolves metadata from the current map-surface read context without native allocations, scene scans, or physics.

27. Complete: Add vehicle slope alignment
   - Add `VehicleSlopeAlignmentSystem`.
   - Apply pitch/roll from sampled normal while preserving movement yaw.
   - Smooth visual rotation and clamp pitch/roll.
   - Soldiers remain upright by default.
   - Added `VehicleSlopeAlignmentSystem` after movement/grounding to project each ground vehicle's current yaw forward onto the sampled surface normal.
   - Vehicle pitch/roll is clamped and smoothed without changing grid movement, pathfinding, or soldier rotation behavior.
   - `UnitGridAuthoring` adds `VehicleSurfaceAlignmentComponent` only for non-air units that use vehicle motion.

28. Complete: Add building foundation visual handling
   - Buildings use footprint average/foundation height.
   - Add validation for max footprint height delta.
   - Defer terrain modification/terraforming to later gameplay tasks.
   - Added `BuildingFoundationVisualSystem` to apply evaluated foundation height to runtime building visuals and matching combat entities.
   - Runtime building creation now records `BuildingSurfaceComponent` on the combat entity from the same footprint result used for visual height.
   - Existing runtime spawn, placement, blocker, and pathing semantics are unchanged; invalid/uneven footprint data is recorded but not retuned here.

29. Complete: Add road placement surface validation
   - Road build placement queries surface slope/height.
   - Road segments can define whether they are ground roads, bridge decks, or ramps.
   - Preserve current road build session/rollback behavior.
   - Added `RoadSurfacePlacementSystem` to validate road paths against baked surface height delta, slope, movement mask, layer consistency, and road surface type.
   - Road input now consults an optional surface-validation delegate before creating a stroke; if no surface context is configured, current flat/no-surface road creation remains unchanged.
   - Road mutation and session snapshot/rollback ownership remain separate from surface validation.

30. Complete: Add runtime city surface integration
   - Runtime city roads, buildings, decorations, and walkable reservations use surface queries.
   - Keep existing runtime city counts, random order, yield points, and placement rules.
   - Added `RuntimeCitySurfaceIntegrationSystem` as the fail-open surface boundary for city visual grounding, footprint reservation checks, road path checks, and primary-surface lookup.
   - `RuntimeCityVisualSystem` now routes visual-only city prefab centers through the surface integration boundary when a baked surface is configured.
   - Runtime city generation counts, random order, coroutine/yield behavior, walkability reservation ownership, and road/building placement rules are unchanged.

31. Complete: Add command targeting surface resolution
   - Selection/move commands resolve the target surface from clicked point plus current unit layer/context.
   - Clicking bridge deck and highway under bridge must be distinguishable by authored target data or camera ray-to-surface bake query.
   - Do not use runtime physics raycasts as the normal command path.
   - Added `MapSurfaceCommandTargetSystem` to resolve pointer command targets from baked surface samples with flat-plane fallback when no surface data exists.
   - Selection command targeting and building/transport click helpers now route through the surface command-target boundary and preserve existing cell-only command/path requests.
   - Focused/selected unit `UnitSurfaceComponent` data is used as preferred surface/layer context for layered target tie-breaking; pathfinding scheduling and move-order batching remain unchanged.

32. Complete: Add diagnostics and visualization
   - Add editor-only visual overlays for surface height, slope, layer ids, bridges, ramps, and blocked cells.
   - Add runtime diagnostics counters without hot-path string formatting.
   - Add debug capture commands for PM/QA review.
   - Added `MapSurfaceDiagnosticsComponent` and `MapSurfaceDiagnosticsSystem` to publish low-frequency surface counters as ECS data.
   - Added editor-only `MapSurfaceEditorOverlaySystem` for height, slope, layer, road/bridge/ramp, and blocked-cell overlays.
   - Added `MapSurfaceDebugCaptureSystem` editor menu command to capture the selected `MapSurfaceAuthoring` summary for PM/QA review without scene-wide object scans.

33. Complete: Add focused tests
   - Tests for flat-equivalent surface.
   - Tests for slope height sampling.
   - Tests for building footprint validation.
   - Tests for bridge deck and lower highway both walkable.
   - Tests for no invalid layer jump.
   - Added `MapSurfaceLayeredGridFocusedTests` covering flat-equivalent sampling, slope sampling/classification, building footprint height-delta rejection, independent bridge/highway walkability, and no same-cell layer jump without explicit connection.

34. Complete: Add runtime validation scene/probe
   - Create a small deterministic bridge/slope validation map or scene fixture.
   - Probe unit move over slope.
   - Probe tank visual pitch/roll.
   - Probe bridge-over-highway route separation.
   - Added `MapSurfaceRuntimeValidationProbeSystem` to run deterministic surface probes over an in-memory slope plus bridge/highway layered fixture.
   - Extended focused tests with `RuntimeValidationProbeCoversSlopeTankAndBridgeSeparation` for slope grounding, tank pitch/roll input, and bridge/highway layer separation.

35. Complete: Performance validation gate
   - Compare frame diagnostics with current baseline.
   - Measure unit grounding cost, pathfinding cost, and memory footprint.
   - Ensure no measurable FPS regression from surface sampling.
   - Ensure no GC allocation spikes in movement/pathfinding/grounding.
   - Added `MapSurfacePerformanceValidationSystem` with baseline frame-budget and allocation-budget checks for surface height/normal sampling plus pathing validation.
   - Added focused performance validation for bounded allocations, sample counts, pathing checks, elapsed budget, and estimated surface memory footprint.

36. Complete: Final architecture gate
   - Ensure no map-surface gameplay logic lives in bootstrap, UI views, editor builders, or broad facades.
   - Ensure no new static mutable state or singleton access.
   - Ensure no per-frame physics raycast path was introduced for normal gameplay.
   - Update this roadmap to complete with final validation evidence.
   - Added final architecture guard coverage to keep map-surface runtime ownership in ECS/data systems, not bootstrap, UI views, broad facades, or non-map editor builders.
   - Final validation evidence:
     - `git diff --check` passed.
     - Unity EditMode `-testFilter MapSurface` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` with `42/42` tests at `/private/tmp/warline-map-surface-step36b-final.xml`.
