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

## Step Plan

1. Pending: Baseline current flat-ground contracts
   - Inventory all places that write `LocalTransform.Position.y = 0`, call `GridUtils.CellToWorldCenter`, spawn units/buildings at flat y, or assume flat road/building placement.
   - Record pathfinding, building placement, road, unit spawn, and runtime city touchpoints.
   - Add architecture test inventory guard for known flat-ground assumptions.

2. Pending: Add map-surface architecture contract guards
   - Update `GameplayArchitectureContractTests` to require the roadmap.
   - Guard against new runtime raycast-based grounding.
   - Guard against new collider dependency in movement/pathfinding/placement.
   - Guard against `SurfaceManager`, `TerrainManager`, `BridgeController`, and singleton-style surface ownership.

3. Pending: Define ECS surface components
   - Add `MapSurfaceComponent` as the ECS singleton data owner.
   - Add surface blob reference, grid origin, cell size, dimensions, and surface availability flags.
   - Keep behavior out of components.

4. Pending: Define unit/building surface components
   - Add `UnitSurfaceComponent` for current surface id, layer id, last sampled height, and grounding flags.
   - Add `BuildingSurfaceComponent` for placement surface id, layer id, foundation height, and slope summary.
   - Add vehicle visual alignment data only where needed, not to every unit.

5. Pending: Define authored surface config
   - Add `MapSurfaceAuthoring` with serialized sampling settings and explicit references to terrain/road/bridge/ramp source roots.
   - Add `BridgeSurfaceAuthoring` only for bridge/overpass metadata: bridge deck, lower pass-through surface, approaches, clearance, and allowed movement masks.
   - Keep authoring classes as reference/config binders only.

6. Pending: Add editor bake skeleton
   - Add `MapSurfaceBakeSystem` editor boundary.
   - It must create deterministic surface samples from authored geometry/config.
   - First version may output a single-layer flat surface equivalent to current behavior for regression safety.

7. Pending: Add baked data asset/output path
   - Store baked surface data as a generated asset or serialized blob source that can be loaded by Match startup.
   - Do not build runtime surface data by scanning scene hierarchy during gameplay.
   - Add validation that Match has exactly one active map surface asset/reference.

8. Pending: Add `MapSurfaceQuerySystem`
   - Create allocation-free context creation for surface sampling.
   - Add pure data sampling functions for cell lookup, surface lookup, bilinear height, normal, slope, and nearest valid surface.
   - Static helpers are allowed only if they are pure stateless data/math operations.

9. Pending: Validate flat-equivalent runtime
   - Wire the baked flat-equivalent surface into Match.
   - Confirm existing units/buildings still behave exactly as before.
   - This step proves the new boundary can exist without changing gameplay.

10. Pending: Add single-layer terrain height bake
   - Bake height and normal for the current Match map surface.
   - Use mesh/terrain source data at editor time.
   - Do not require runtime colliders.

11. Pending: Add `UnitGroundingSystem`
   - Sample the unit's current surface and apply `LocalTransform.Position.y`.
   - Preserve `x/z` movement ownership in existing movement/path systems.
   - Add ground offset support from unit config/authoring.

12. Pending: Add initial spawn grounding
   - Route initial unit spawn, respawn, produced-unit spawn, transport disembark, and runtime city/citizen spawn positions through surface sampling.
   - Preserve existing spawn order and counts.

13. Pending: Add building placement height query
   - Add `BuildingSurfacePlacementSystem` footprint sampling.
   - Compute average height, max height delta, max slope, and selected surface/layer.
   - Do not commit invalid uneven placements.

14. Pending: Add building runtime spawn grounding
   - Apply surface/foundation height to runtime-created buildings.
   - Keep buildings upright in first pass.
   - Preserve current building costs, production, and spawn semantics.

15. Pending: Add slope classification
   - Bake slope per surface sample.
   - Add movement masks for infantry, wheeled vehicle, tracked vehicle, and building placement.
   - Do not change path costs yet; only publish data.

16. Pending: Add pathfinding surface read context
   - Extend pathfinding request/job context with map-surface read data.
   - Preserve request budgets, scheduling, allocator lifetimes, and path output layout.
   - Keep flat behavior if no surface data exists.

17. Pending: Add slope walkability rejection
   - Reject cells/surfaces above movement-type max slope.
   - Preserve current walkability rules for flat maps.
   - Add tests for soldiers vs tanks if movement masks differ.

18. Pending: Add slope movement cost
   - Add slope cost contribution behind a config flag or exact documented defaults.
   - Validate no regression in current flat map path costs.
   - Do not tune gameplay values in this step.

19. Pending: Add road surface priority
   - Mark road samples as road/bridge/highway.
   - Preserve existing road preference/cost behavior by mapping old road data to surface flags.
   - Keep road and surface ownership separate but queryable from the same path context.

20. Pending: Add multi-layer surface storage
   - Replace single-sample cell assumptions with `CellSurfaceRange`.
   - Store one surface inline where possible and sparse extra surfaces only for layered cells.
   - Validate memory and sampling cost.

21. Pending: Add bridge deck bake
   - Bake bridge deck surfaces above lower roads/highways.
   - Mark bridge surfaces with bridge type, road type, normal, height, and movement mask.
   - Do not connect bridge to lower highway unless an authored connector exists.

22. Pending: Add lower highway/road preservation under bridges
   - Ensure lower surfaces under bridges remain walkable/pathable.
   - Store separate surface ids and layer ids for lower and upper surfaces.
   - Validate highway path under bridge remains on lower layer.

23. Pending: Add bridge/ramp connection bake
   - Bake explicit approach/ramp edges from road to bridge deck.
   - Bake same-layer edges along bridge deck.
   - Bake same-layer edges along highway underneath.
   - Do not infer vertical transitions by height proximity alone.

24. Pending: Add `MapSurfaceConnectionSystem`
   - Own connection validation and runtime read contexts.
   - Pathfinding must use connection edges for layered transitions.
   - Surface sampling must stay separate from connection traversal.

25. Pending: Add unit surface tracking
   - `UnitSurfaceTrackingSystem` updates current surface/layer from path result or current cell.
   - Units keep their layer while moving under/over bridges.
   - Layer changes only occur when path result crosses an explicit connection edge.

26. Pending: Add path result surface ids
   - Extend path nodes or path-follow state with surface/layer ids where needed.
   - Preserve path pool allocator behavior.
   - Validate no managed allocation or path output regression.

27. Pending: Add vehicle slope alignment
   - Add `VehicleSlopeAlignmentSystem`.
   - Apply pitch/roll from sampled normal while preserving movement yaw.
   - Smooth visual rotation and clamp pitch/roll.
   - Soldiers remain upright by default.

28. Pending: Add building foundation visual handling
   - Buildings use footprint average/foundation height.
   - Add validation for max footprint height delta.
   - Defer terrain modification/terraforming to later gameplay tasks.

29. Pending: Add road placement surface validation
   - Road build placement queries surface slope/height.
   - Road segments can define whether they are ground roads, bridge decks, or ramps.
   - Preserve current road build session/rollback behavior.

30. Pending: Add runtime city surface integration
   - Runtime city roads, buildings, decorations, and walkable reservations use surface queries.
   - Keep existing runtime city counts, random order, yield points, and placement rules.

31. Pending: Add command targeting surface resolution
   - Selection/move commands resolve the target surface from clicked point plus current unit layer/context.
   - Clicking bridge deck and highway under bridge must be distinguishable by authored target data or camera ray-to-surface bake query.
   - Do not use runtime physics raycasts as the normal command path.

32. Pending: Add diagnostics and visualization
   - Add editor-only visual overlays for surface height, slope, layer ids, bridges, ramps, and blocked cells.
   - Add runtime diagnostics counters without hot-path string formatting.
   - Add debug capture commands for PM/QA review.

33. Pending: Add focused tests
   - Tests for flat-equivalent surface.
   - Tests for slope height sampling.
   - Tests for building footprint validation.
   - Tests for bridge deck and lower highway both walkable.
   - Tests for no invalid layer jump.

34. Pending: Add runtime validation scene/probe
   - Create a small deterministic bridge/slope validation map or scene fixture.
   - Probe unit move over slope.
   - Probe tank visual pitch/roll.
   - Probe bridge-over-highway route separation.

35. Pending: Performance validation gate
   - Compare frame diagnostics with current baseline.
   - Measure unit grounding cost, pathfinding cost, and memory footprint.
   - Ensure no measurable FPS regression from surface sampling.
   - Ensure no GC allocation spikes in movement/pathfinding/grounding.

36. Pending: Final architecture gate
   - Ensure no map-surface gameplay logic lives in bootstrap, UI views, editor builders, or broad facades.
   - Ensure no new static mutable state or singleton access.
   - Ensure no per-frame physics raycast path was introduced for normal gameplay.
   - Update this roadmap to complete with final validation evidence.
