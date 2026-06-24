# RuntimeCityBuildingSpawnSystem Refactor Roadmap

This document owns the `RuntimeCityBuildingSpawnSystem` refactor plan. The class was created during the `RuntimeCitySpawnerSystem` split, but it now concentrates several separate city-building responsibilities. This roadmap is the source of truth for shrinking that responsibility without changing runtime-city layout, road behavior, building counts, random generation order, or performance characteristics.

## Fixed Step Count

This roadmap has 36 steps. Do not append surprise steps after step 36. If new work is discovered, update the relevant existing step and keep the final validation gate as the last step.

## Target

Target file: `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs`

Current size at roadmap creation: 1107 lines. This is an observation, not a hard acceptance limit. The acceptance target is single responsibility.

Final target: `RuntimeCityBuildingSpawnSystem` may remain only as an algorithm-light city-building spawn coordinator with high-level methods used by `RuntimeCityGenerationSystem`. It must not own landmark placement algorithms, roadside plot placement algorithms, rural scatter placement, yard-wall fit/visual algorithms, decoration prefab classification, decoration placement algorithms, spawn/reserve validation, mutable config/dependency state, or broad compatibility surface. If the coordinator becomes pure pass-through after these steps, deletion can be decided inside step 35, not as a surprise step.

## Current Responsibility Inventory

- Mutable config/dependency storage: stores a `RuntimeCityConfigSystem.Snapshot` plus plot, walkability, prefab selection, visual, spawn bridge, and diagnostic systems through `Configure`.
- Config projection helpers: exposes many private property wrappers for city counts, spacing, landmark config, health, and prefab lists.
- Public generation surface: `SpawnCityImportantBuildings`, `EnsureCityHall`, `SpawnCityBulkBuildingsRoutine`, and `SpawnCorridorEntranceBuildings`.
- Random state bridge: nested `GenerationRandomState` class exists only so coroutine sequencing can update Unity.Mathematics random state.
- Landmark placement: city hall, clock tower, fountain, monument, pillar, landmark offsets, hall-distance filtering, spawn/delete/reserve validation.
- Bulk building sequencing: entry shops/houses, central shops, gas stations, outer shops, roadside houses, rural houses, house yard walls, other buildings, rural other buildings, and decoration buildings.
- Plot placement: `PlaceFromPlots` handles plot spacing, prefab choice, footprint lookup, spawn, post-spawn validation, reservation, and placement-anchor recording.
- Rural placement: random scatter attempts, distance limits, road-cell rejection, spacing, spawn, validation, reservation, and anchor recording.
- Yard walls: house selection, padding attempts, gate-side selection, wall/gate/pillar visual spawning, horizontal and vertical wall-run splitting.
- Decoration buildings: allocates classified prefab lists, places cloth covers adjacent to anchors, places central archways, and places free scatter decoration buildings.
- Cross-system coupling: reaches into plot, walkability, prefab selection, visual, spawn bridge, and diagnostic systems from most methods.

## Public/Internal Surface Inventory Freeze

New public/internal members must not be added to `RuntimeCityBuildingSpawnSystem`. Later steps may remove members from this list as callers migrate to target owners.

Allowed temporary public/internal surface:

- `public void Configure(...)`
  - Target owner: `RuntimeCityBuildingSpawnContextCompositionSystemHelper` and `RuntimeCityCompositionSystem`.
- `public void SpawnCityImportantBuildings(...)`
  - Target owner: coordinator delegating to `RuntimeCityHallSpawnSystem` and `RuntimeCityLandmarkSpawnSystem`.
- `public void EnsureCityHall(...)`
  - Target owner: `RuntimeCityHallSpawnSystem`.
- `public IEnumerator SpawnCityBulkBuildingsRoutine(...)`
  - Target owner: `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper`.
- `public void SpawnCorridorEntranceBuildings(...)`
  - Target owner: `RuntimeCityCorridorBuildingSpawnPrefabSystemHelper`.
- `public sealed class GenerationRandomState`
  - Target owner: `RuntimeCityGenerationRandomSystem` or a nested type on `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper`.

## Architecture Rules

- Do not replace `RuntimeCityBuildingSpawnSystem` with `RuntimeCityBuildingManager`, `RuntimeCityBuildingController`, `RuntimeCityBuildingSpawnerFacade`, or another broad shell.
- New gameplay runtime types must be named `*System`, except existing `Config` assets and Unity edge types.
- No singleton/static runtime access. Static helpers are allowed only for pure deterministic math/data with no runtime dependencies.
- Do not use reflection.
- Do not move runtime-city building generation into UI, bootstrap, editor tooling, or config assets.
- Do not make `RuntimeCitySpawnerSystem` grow again. Any extracted behavior must stay out of the old spawner.
- `RuntimeCityCompositionSystem` may compose systems and contexts, but must not absorb city-building placement policy.

## Performance And Behavior Rules

- Preserve current building counts, target ratios, spacing constants, attempt limits, landmark offsets, clearance values, road overlap rules, yard-wall rules, and fallback labels/descriptions unless a later gameplay task explicitly asks for tuning.
- Preserve current coroutine yield points and startup loading behavior unless the roadmap step explicitly says otherwise.
- Preserve Unity.Mathematics random consumption order within each building-generation phase. If a step changes helper boundaries, pass random state explicitly and validate deterministic behavior through smoke tests.
- Do not add per-frame managed allocations. Runtime city generation may allocate planning lists during generation, but extra LINQ/`FindAll` allocations should be removed or cached when touching decoration classification.
- Keep spawn/delete/reserve validation centralized so failed post-spawn validation does not leak buildings or reservations.
- Do not change road grid size, road-cell size, runtime-city road generation, building spawn bridge semantics, or walkability/reserved-footprint semantics.

## Required Validation Gates

Every implementation step must run:

- `git diff --check` scoped to touched files.
- Focused runtime-city building-spawn architecture validation once the tests exist.

Every phase boundary must also run when feasible:

- `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
- `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`.
- EditMode `BuildingPlacementValidationSystemTests`, because city building spawn depends on building footprint/road validity.
- PlayMode `BootstrapAndMenuPlayModeTests`, because runtime city starts after the menu play flow.
- Runtime FPS play-button probe when a step changes coroutine sequencing, generation yield points, or placement loops.

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity validation.

## Phase 1: Baseline, Contract, And Surface Freeze

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add architecture contract wording that `RuntimeCityBuildingSpawnSystem` is a temporary mixed-responsibility building spawn coordinator.
   - Add focused architecture validation entry point for this roadmap.
   - Guard the 36-step roadmap, target file, current responsibility inventory, forbidden broad replacement names, and no new public/internal surface.
   - Expected output: future changes cannot normalize or grow the mixed-responsibility coordinator.
   - Added this roadmap and `GameplayArchitectureContractTests.RunRuntimeCityBuildingSpawnArchitectureBatchValidation`.
   - Added guards for the 36-step roadmap, 1107-line baseline, target file, forbidden broad replacement names, current mixed responsibilities, and bounded public surface.
   - Updated `gameplay_solid_ecs_contract.md` with the RuntimeCityBuildingSpawnSystem follow-up refactor target and broad-shell replacement ban.

2. Complete: Freeze public/internal surface
   - Inventory every public/internal member on `RuntimeCityBuildingSpawnSystem`.
   - Assign each member to the target owner listed above.
   - Add a guard preventing new public/internal members from being added to the coordinator.
   - Expected output: later steps retire named surface groups deliberately.
   - Public/internal surface is inventoried in the Public/Internal Surface Inventory Freeze section.
   - `RuntimeCityBuildingSpawnSystemBaselineMustStayExplicitUntilExtracted` guards the allowed temporary public surface while the coordinator is decomposed.

3. Complete: Add deterministic behavior baseline
   - Document current runtime-city smoke command and expected key output.
   - Add or extend validation to confirm city generation still has city prefabs, spawnables, and blocker prefabs available.
   - Capture an initial runtime FPS probe only if the editor is stable enough for a meaningful sample.
   - Expected output: later extraction steps have a behavior/performance comparison point.
   - Baseline smoke command: Unity 6000.4.0f1 batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, `-executeMethod RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`, log `/private/tmp/warline-runtime-city-building-spawn-step3-smoke.log`.
   - Baseline smoke result: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.
   - No fresh FPS probe was recorded for this doc/test-only baseline step. The probe remains required when a later step changes coroutine sequencing, yield points, or placement loops.

## Phase 2: Context And Shared Placement Boundary

4. Complete: Create `RuntimeCityBuildingSpawnContextCompositionSystemHelper`
   - Move dependency/config context construction into a narrow context system.
   - Context must include config snapshot, plot, walkability, prefab selection, visual, spawn bridge, and diagnostics dependencies.
   - Do not add placement policy here.
   - Expected output: generation systems can receive explicit context instead of mutable fields on `RuntimeCityBuildingSpawnSystem`.
   - Added `RuntimeCityBuildingSpawnContextCompositionSystemHelper` with a `Context` containing config, plot, walkability, prefab selection, visual, spawn bridge, and diagnostic dependencies.
   - `RuntimeCityCompositionSystem` now constructs building-spawn context through the context system before configuring `RuntimeCityBuildingSpawnSystem`.
   - Added focused architecture guard coverage for context ownership.

5. Complete: Route public methods through explicit context
   - Add context-taking overloads or internal methods for the existing public generation surface.
   - Keep current public methods as temporary compatibility delegates only.
   - Expected output: behavior can move to child systems without depending on coordinator fields.
   - `RuntimeCityBuildingSpawnSystem.Configure` now receives the explicit building-spawn context assembled by `RuntimeCityCompositionSystem`.
   - Existing public generation methods now delegate to private context-taking methods; no new public/internal compatibility surface was added.
   - The first seam keeps helper internals unchanged except top-level plot, prefab-shuffle, visual-root, and diagnostic reads that now use the explicit context.

6. Complete: Create `RuntimeCityBuildingPlacementPrefabSystemHelper`
   - Centralize prefab footprint lookup, pre-spawn reserved-footprint checks, `TrySpawnCityBuilding`, post-spawn reserved/road/touch validation, delete-on-failure, reservation, and optional anchor recording.
   - Preserve current spawn bridge behavior and default max health.
   - Expected output: repeated spawn/delete/reserve logic leaves landmark, plot, rural, and decoration algorithms.
   - Added `RuntimeCityBuildingPlacementPrefabSystemHelper` with request/result data and `TrySpawnAndReserve`.
   - Routed current building spawn/delete/reserve validation through the placement boundary while preserving algorithm loops, labels, descriptions, reservation padding, road checks, touch checks, and anchor recording.
   - Direct building spawn bridge calls no longer live in `RuntimeCityBuildingSpawnSystem`.

7. Complete: Move shared plot placement helper
   - Move `PlaceFromPlots` behavior into `RuntimeCityBuildingPlacementPrefabSystemHelper` or a narrow `RuntimeCityRoadsidePlacementSystem`.
   - Preserve plot ordering, spacing, random prefab choice, reservation padding, and anchor recording.
   - Expected output: roadside placement can be reused by entry, central, outer, and corridor building systems.
   - Moved `PlaceFromPlots` into `RuntimeCityBuildingPlacementPrefabSystemHelper`.
   - Entry, central, outer, gas-station, other-building, and corridor placement calls now route through the shared placement boundary.
   - `RuntimeCityBuildingSpawnSystem` no longer owns the shared roadside plot placement loop.

8. Complete: Remove mutable config/dependency reads from shared placement paths
   - Replace coordinator property-wrapper reads in placement helpers with explicit context/config values.
   - Keep fallback labels/descriptions unchanged.
   - Expected output: shared placement logic no longer depends on `RuntimeCityBuildingSpawnSystem` fields.
   - Bulk and corridor shared placement calls now pass the active context parameter and values from `context.Config`.
   - Rural, decoration, archway, cloth-cover, and adjacent-decoration placement paths now receive explicit context and config values instead of using coordinator `_context` or property wrappers for their shared placement calls.
   - Fallback labels/descriptions, coroutine yield points, placement counts, spacing, and random-call order were preserved.

## Phase 3: Landmarks

9. Complete: Extract landmark offset policy
   - Create `RuntimeCityLandmarkOffsetSystem`.
   - Move hall, clock tower, fountain, monument, and pillar offset arrays plus hall-distance filtering.
   - Preserve offset order exactly.
   - Expected output: landmark placement order remains deterministic and testable.
   - Added `RuntimeCityLandmarkOffsetSystem` with the exact existing hall, clock-tower, fountain, monument, and pillar offset ordering.
   - Moved landmark hall-distance filtering into `RuntimeCityLandmarkOffsetSystem.IsTooCloseToHall`.
   - `RuntimeCityBuildingSpawnSystem` now reads landmark offsets from the offset boundary instead of owning offset arrays.

10. Complete: Extract city hall placement
   - Create `RuntimeCityHallSpawnSystem`.
   - Move `TrySpawnHall` and `EnsureCityHall` behavior.
   - Preserve hall candidate shuffle, centered-origin search, clearance, delete-on-failed-post-validation, reservation, and failure diagnostic.
   - Expected output: city hall placement has one owner.
   - Added `RuntimeCityHallSpawnSystem` as the city hall placement owner.
   - Moved hall candidate shuffle, centered-origin offset search, shared spawn/reserve validation, clearance reservation, and hall failure diagnostics out of `RuntimeCityBuildingSpawnSystem`.
   - `RuntimeCityBuildingSpawnSystem.EnsureCityHall` is now only a compatibility delegate into the hall spawn boundary.

11. Complete: Extract non-hall landmark placement
   - Create `RuntimeCityLandmarkSpawnSystem`.
   - Move clock tower, fountain, monument, and pillar placement using shared landmark offsets and shared placement validation.
   - Preserve display names/descriptions and road/reserved validation.
   - Expected output: `SpawnCityImportantBuildings` delegates landmark work only.
   - Added `RuntimeCityLandmarkSpawnSystem` as the owner for clock tower, fountain, monument, and pillar placement.
   - Moved non-hall landmark prefab selection, offset iteration, hall-distance filtering, display labels/descriptions, and shared placement validation out of `RuntimeCityBuildingSpawnSystem`.
   - `RuntimeCityBuildingSpawnSystem.SpawnCityImportantBuildings` now preserves the hall-first order and delegates non-hall landmark work through the landmark spawn boundary.

12. Complete: Collapse important-building coordinator
   - Update `RuntimeCityBuildingSpawnSystem.SpawnCityImportantBuildings` to call hall and landmark systems through context.
   - Remove private landmark methods from the coordinator.
   - Expected output: coordinator no longer owns landmark algorithms.
   - `SpawnCityImportantBuildings` now directly sequences `RuntimeCityHallSpawnSystem` first and `RuntimeCityLandmarkSpawnSystem` second through the explicit context.
   - Removed the private `EnsureCityHall` wrapper so the coordinator no longer owns private hall or landmark placement methods.
   - Added contract coverage requiring the important-building coordinator to remain delegation-only.

## Phase 4: Bulk Roadside And Rural Buildings

13. Complete: Extract bulk plot planning
   - Create `RuntimeCityBulkPlotPlanUtilitySystemHelper`.
   - Move central, outer, and entry plot collection plus shuffling.
   - Preserve plot ranges, entry-road behavior, and shuffle order.
   - Expected output: bulk generation has explicit plot-plan data.
   - Added `RuntimeCityBulkPlotPlanUtilitySystemHelper` with an explicit `Plan` result for central, outer, and entry plots.
   - Moved central, outer, and entry plot collection plus central-then-outer-then-entry shuffling out of `RuntimeCityBuildingSpawnSystem`.
   - `SpawnCityBulkBuildingsRoutine` now consumes the explicit plot plan before preserving the existing building placement sequence and yield cadence.

14. Complete: Extract entry building placement
   - Create `RuntimeCityEntryBuildingSpawnSystem`.
   - Move entry shops and entry houses from the bulk routine.
   - Preserve counts, labels, descriptions, spacing, and anchor recording.
   - Expected output: entrance roadside buildings have one owner.
   - Added `RuntimeCityEntryBuildingSpawnSystem` as the owner for entry shops and entry houses.
   - Moved entry counts, labels, descriptions, spacing, shared placement calls, and anchor recording out of `RuntimeCityBuildingSpawnSystem`.
   - `SpawnCityBulkBuildingsRoutine` still owns the same two yield points around entry building placement.

15. Complete: Extract central and outer roadside placement
   - Create `RuntimeCityRoadsideBuildingSpawnSystem`.
   - Move central shops, gas stations, outer shops, and roadside houses.
   - Preserve central shop target, gas station spacing, rural ratio split, labels, descriptions, and anchors.
   - Expected output: roadside commercial/residential placement has one owner.
   - Added `RuntimeCityRoadsideBuildingSpawnSystem` with a `Plan` for central shop, rural house, and roadside house targets.
   - Moved central shops, gas stations, outer shops, and roadside houses out of `RuntimeCityBuildingSpawnSystem`.
   - Preserved the existing four yield points around central market, gas station, outer shop, and roadside house placement.

16. Complete: Extract rural scatter placement
   - Create `RuntimeCityRuralBuildingSpawnSystem`.
   - Move `PlaceRuralHouses` behavior for rural houses and rural other buildings.
   - Preserve distance limits, attempt limits, road rejection, spacing, random prefab choice, reservation, and anchor recording.
   - Expected output: rural scatter placement has one owner.
   - Added `RuntimeCityRuralBuildingSpawnSystem` as the owner for rural house and rural other-building scatter placement.
   - Moved rural scatter attempt limits, distance checks, road rejection, spacing, random prefab choice, spawn/reserve calls, and anchor recording out of `RuntimeCityBuildingSpawnSystem`.
   - `SpawnCityBulkBuildingsRoutine` now delegates both rural house and rural other-building scatter through the rural building spawn boundary.

17. Complete: Extract bulk routine sequencing
   - Create `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper`.
   - Move coroutine sequencing and `yield return null` cadence for entry, roadside, rural, yard walls, other buildings, and decorations.
   - Preserve every existing yield point.
   - Expected output: `SpawnCityBulkBuildingsRoutine` becomes a coordinator delegate.
   - Added `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper` as the owner for the bulk building coroutine sequence and the existing eleven `yield return null` points.
   - `RuntimeCityBuildingSpawnSystem.SpawnCityBulkBuildingsRoutine` now delegates to the routine system and passes only narrow yard-wall and decoration callbacks for domains that are extracted in later steps.
   - Entry, roadside, rural, yard-wall, other-building, and decoration sequencing now lives outside the coordinator without changing random-state handoff or yield cadence.

18. Complete: Move `GenerationRandomState`
   - Move the coroutine random-state bridge to `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper` or `RuntimeCityGenerationRandomSystem`.
   - Update `RuntimeCityGenerationSystem` to use the new owner.
   - Preserve random state handoff back to `RuntimeCityGenerationSystem`.
   - Expected output: coordinator no longer owns coroutine random plumbing.
   - `GenerationRandomState` now lives in `RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper`.
   - `RuntimeCityGenerationSystem` creates the bulk-routine random bridge, passes it into `SpawnCityBulkBuildingsRoutine`, and copies the updated value back after the coroutine returns.
   - `RuntimeCityBuildingSpawnSystem` no longer owns a nested random-state bridge type.

## Phase 5: Corridor Entrance Buildings

19. Complete: Extract corridor entrance placement
   - Create `RuntimeCityCorridorBuildingSpawnPrefabSystemHelper`.
   - Move `SpawnCorridorEntranceBuildings`.
   - Preserve corridor plot building, shuffle, shop/house counts, labels, descriptions, spacing, and reservation behavior.
   - Expected output: corridor-side buildings have one owner.
   - Added `RuntimeCityCorridorBuildingSpawnPrefabSystemHelper` as the owner for corridor entrance shop/house placement.
   - `RuntimeCityBuildingSpawnSystem.SpawnCorridorEntranceBuildings` now delegates to the corridor system while preserving the existing public method.
   - Corridor plot building, shuffle, counts, labels, descriptions, zero spacing, and reserved-footprint placement calls were moved unchanged.

20. Complete: Route generation through corridor boundary
   - Update `RuntimeCityGenerationSystem` context to consume the corridor spawn system or a coordinator method that delegates only.
   - Keep generation sequencing unchanged.
   - Expected output: corridor placement is no longer implemented by `RuntimeCityBuildingSpawnSystem`.
   - `RuntimeCityCompositionSystem` now owns and passes the explicit building-spawn context, stateless placement system, and corridor spawn system into generation.
   - `RuntimeCityGenerationSystem` now calls `RuntimeCityCorridorBuildingSpawnPrefabSystemHelper.SpawnCorridorEntranceBuildings` directly for corridor-side buildings.
   - `RuntimeCityBuildingSpawnSystem.SpawnCorridorEntranceBuildings` remains only as a compatibility wrapper for callers that have not migrated yet.

## Phase 6: Yard Walls

21. Complete: Extract yard-wall fit planning
   - Create `RuntimeCityYardWallPlanSystem`.
   - Move house shuffle, target count, padding candidate creation/shuffle, and yard-rect fit checks.
   - Preserve house wall chance, min/max distance, and `CanPlaceHouseYardRect` calls.
   - Expected output: yard-wall candidate selection has one owner.
   - Added `RuntimeCityYardWallPlanSystem` with `HousePlan` creation for shuffled houses and success target count.
   - Moved padding candidate creation/shuffle plus `CanPlaceHouseYardRect` fit checks into `RuntimeCityYardWallPlanSystem.TryFindYardRect`.
   - `RuntimeCityBuildingSpawnSystem` now keeps only yard-wall orchestration, gate choice, visual spawning, and reservation for later extraction steps.

22. Complete: Extract yard gate math
   - Create `RuntimeCityYardGateSystem`.
   - Move `YardSide`, `GetPreferredYardGateSide`, and `GetCenteredOpeningStart`.
   - Preserve gate-side decision and opening clamp behavior.
   - Expected output: gate policy is pure and testable.
   - Added `RuntimeCityYardGateSystem` as the owner for `YardSide`, preferred gate-side selection, and centered opening start math.
   - `RuntimeCityBuildingSpawnSystem` now depends on the yard gate system for gate side and opening positions.
   - Gate-side decision and opening clamp behavior were moved unchanged.

23. Complete: Extract yard wall visuals
   - Create `RuntimeCityYardWallVisualSystem`.
   - Move boundary visual spawning, horizontal/vertical side placement, wall-run splitting, gate placement, pillar placement, and rotation choices.
   - Preserve wall/gate/pillar footprint math and visual-only spawn calls.
   - Expected output: visual spawning logic is isolated from placement planning.
   - Added `RuntimeCityYardWallVisualSystem` as the owner for yard boundary visual spawning, side placement, wall-run splitting, gate placement, pillar placement, and rotation choices.
   - `RuntimeCityBuildingSpawnSystem` now delegates yard visual spawning through the visual system while preserving reservation timing after successful visual creation.
   - Wall/gate/pillar footprint math and visual-only spawn calls were moved unchanged.

24. Complete: Extract house yard wall orchestration
   - Create `RuntimeCityHouseYardWallSystem`.
   - Move `PlaceHouseYardWalls` and `TryBuildHouseYardWall` orchestration.
   - Use yard plan, gate, visual, walkability, prefab selection, and reservation boundaries.
   - Expected output: house yard walls are fully out of `RuntimeCityBuildingSpawnSystem`.
   - Added `RuntimeCityHouseYardWallSystem` as the owner for house yard-wall placement orchestration and successful-wall target counting.
   - `RuntimeCityBuildingSpawnSystem` now passes a narrow callback to the bulk routine that delegates to `RuntimeCityHouseYardWallSystem` with explicit dependencies and config values.
   - Yard-rect planning, gate selection, visual spawning, prefab selection, and reserved-footprint reservation are now coordinated outside the building-spawn coordinator.

## Phase 7: Decoration Buildings

25. Complete: Extract decoration prefab classification
   - Create `RuntimeCityDecorationGroupPrefabSystemHelper`.
   - Move cloth-cover, archway, and free-scatter classification.
   - Avoid repeated LINQ/`FindAll` allocation where practical; generation-time list allocation is acceptable only if explicit and bounded.
   - Preserve current name matching rules.
   - Expected output: decoration classification has one owner.
   - Added `RuntimeCityDecorationGroupPrefabSystemHelper` with a single-pass grouping method for cloth-cover, archway, and free-scatter decoration prefabs.
   - `RuntimeCityBuildingSpawnSystem.PlaceCityDecorationBuildings` now consumes grouped decoration prefab lists instead of owning classification.
   - The existing ordinal-ignore-case name matching rules and fallback to the original prefab list when no free-scatter prefabs exist were preserved.

26. Complete: Extract cloth-cover placement
   - Create `RuntimeCityClothCoverSpawnPrefabSystemHelper`.
   - Move `PlaceClothCoverBuildings` and `TrySpawnAdjacentDecoration`.
   - Preserve anchor shuffle, prefab cursor behavior, adjacency candidates, touch validation, and reservation.
   - Expected output: adjacent decoration placement has one owner.
   - Added `RuntimeCityClothCoverSpawnPrefabSystemHelper` as the owner for cloth-cover adjacent decoration placement.
   - `RuntimeCityBuildingSpawnSystem.PlaceCityDecorationBuildings` now delegates cloth-cover placement through the cloth-cover system.
   - Anchor shuffle, prefab cursor behavior, adjacency candidate shuffle, required-touch validation, labels/descriptions, and reservation were moved unchanged.

27. Complete: Extract central archway placement
   - Create `RuntimeCityArchwaySpawnPrefabSystemHelper`.
   - Move `PlaceCentralArchwayBuildings`.
   - Preserve min/max hall distance, attempts, prefab cycling, labels, descriptions, and reservation.
   - Expected output: archway placement has one owner.
   - Added `RuntimeCityArchwaySpawnPrefabSystemHelper` as the owner for central archway decoration placement.
   - `RuntimeCityBuildingSpawnSystem.PlaceCityDecorationBuildings` now delegates archway placement through the archway system.
   - Min/max hall distance, attempt budget, prefab cycling, labels, descriptions, plot spacing, and reservation were moved unchanged.

28. Complete: Extract free scatter decoration placement
   - Create `RuntimeCityFreeScatterDecorationPrefabSystemHelper`.
   - Move remaining free-scatter decoration placement from `PlaceCityDecorationBuildings`.
   - Preserve distance checks, attempt count, plot spacing, labels, descriptions, and reservation.
   - Expected output: scatter decoration placement has one owner.
   - Added `RuntimeCityFreeScatterDecorationPrefabSystemHelper` as the owner for free-scatter decoration placement.
   - `RuntimeCityBuildingSpawnSystem.PlaceCityDecorationBuildings` now delegates remaining free-scatter placement through the free-scatter system.
   - Distance checks, attempt budget, plot spacing, random prefab choice, labels, descriptions, and reservation were moved unchanged.

29. Complete: Extract decoration sequencing
   - Create `RuntimeCityDecorationBuildingSpawnSystem`.
   - Move `PlaceCityDecorationBuildings` orchestration over decoration groups, cloth covers, archways, and free scatter.
   - Preserve placement order and count accounting.
   - Expected output: decoration building sequencing is fully out of the coordinator.
   - Added `RuntimeCityDecorationBuildingSpawnSystem` as the owner for decoration building sequencing.
   - Moved decoration group creation, cloth-cover placement, archway placement, remaining-count calculation, free-scatter fallback prefabs, and free-scatter placement handoff out of `RuntimeCityBuildingSpawnSystem`.
   - `RuntimeCityBuildingSpawnSystem` now delegates decoration sequencing through a narrow bulk-routine callback without owning decoration count accounting.

## Phase 8: Coordinator Retirement Pass

30. Complete: Remove private algorithm methods from coordinator
   - Delete migrated private methods from `RuntimeCityBuildingSpawnSystem`.
   - Coordinator may only compose context and call child systems.
   - Expected output: no private landmark, plot, rural, yard-wall, or decoration algorithms remain in the coordinator.
   - Confirmed `RuntimeCityBuildingSpawnSystem` no longer owns private landmark, plot, rural scatter, yard-wall, or decoration algorithm methods.
   - Added architecture guard coverage so migrated algorithm tokens cannot return to the coordinator.
   - The remaining coordinator surface is public compatibility entry points plus child-system delegation; mutable context/config state is left for step 31.

31. Complete: Remove mutable `Configure` state
   - Remove `_config` and dependency fields from `RuntimeCityBuildingSpawnSystem`.
   - Replace `Configure` with explicit context construction in `RuntimeCityBuildingSpawnContextCompositionSystemHelper` and composition.
   - Expected output: coordinator has no mutable runtime config/dependency cache.
   - Removed `RuntimeCityBuildingSpawnSystem.Configure`, cached config, cached dependency fields, and cached building-spawn context.
   - Generation now passes `RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context` explicitly into building-spawn methods.
   - Composition still creates and stores the generation context, but no longer configures mutable state on the building-spawn coordinator.

32. Complete: Update `RuntimeCityCompositionSystem` ownership
   - Compose all extracted child systems explicitly.
   - Ensure composition remains wiring-only and does not absorb city-building placement policy.
   - Expected output: child system ownership is visible and single-purpose.
   - Moved extracted building-spawn child system instances into `RuntimeCityCompositionSystem`.
   - Added an explicit `RuntimeCityBuildingSpawnSystem.Systems` dependency set passed through `RuntimeCityGenerationSystem.Context`.
   - `RuntimeCityBuildingSpawnSystem` now receives child systems explicitly and no longer hides child-system construction internally.

33. Complete: Update `RuntimeCityGenerationSystem` surface
   - Keep generation sequencing stable while replacing broad building-spawn calls with explicit child-system or coordinator calls.
   - Do not change road generation, city chaining, deferred road ECS sync, deferred building side effects, or minimap publication.
   - Expected output: generation no longer depends on a mixed-responsibility building spawn object.
   - `RuntimeCityGenerationSystem` no longer receives or calls `RuntimeCityBuildingSpawnSystem`.
   - Hall, landmark, bulk building, house yard-wall, and decoration sequencing now route directly through the composed child systems in `RuntimeCityBuildingSpawnSystem.Systems`.
   - Road generation, city chaining, deferred road ECS sync, deferred building side effects, minimap publication, yield points, and random-state handoff were left unchanged.

34. Complete: Architecture contract and audit update
   - Update `gameplay_solid_ecs_contract.md`.
   - Update `runtime_city_spawner_responsibility_audit.md` or add a focused building-spawn audit section.
   - Add guards that extracted responsibilities cannot return to `RuntimeCityBuildingSpawnSystem` or `RuntimeCitySpawnerSystem`.
   - Expected output: drift is blocked by tests/docs.
   - Updated the SOLID/ECS contract and runtime-city responsibility audit with the post-step-33 building-spawn boundaries.
   - Added focused architecture guard coverage so extracted responsibilities cannot drift back into `RuntimeCityBuildingSpawnSystem`, the deleted `RuntimeCitySpawnerSystem`, or a broad replacement shell.
   - Refreshed the broader runtime-city building-spawn sequencing guard to validate direct child-system calls from `RuntimeCityGenerationSystem`.

35. Complete: Coordinator deletion audit
   - Review whether `RuntimeCityBuildingSpawnSystem` is still meaningful or pure pass-through.
   - If pure pass-through, delete it and route callers directly through child systems.
   - If still meaningful, keep it only as an algorithm-light coordinator and document why.
   - Expected output: no broad shell remains under old or new names.
   - Deleted `Assets/Game/Scripts/Environment/RuntimeCityBuildingSpawnSystem.cs` and its `.meta` because generation no longer called it and the remaining methods were pass-through wrappers.
   - Moved the child-system dependency bundle to `RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems`.
   - `RuntimeCityCompositionSystem` now creates `RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems`, and `RuntimeCityGenerationSystem` receives that dependency bundle directly.
   - Added deletion guards so the coordinator shell cannot be restored.

36. Complete: Validation gate
   - Run focused runtime-city building-spawn architecture validation.
   - Run `GameplayArchitectureContractTests.RunRuntimeCityArchitectureBatchValidation`.
   - Run `RuntimeCitySpawnerStep13Validation.RunGameSceneSmokeValidation`.
   - Run EditMode `BuildingPlacementValidationSystemTests`.
   - Run PlayMode `BootstrapAndMenuPlayModeTests`.
   - Run runtime FPS play-button probe if coroutine sequencing, yield points, or placement loops changed in the final batch.
   - Write a WarlineCapture handoff report under `Design/AgentReports`.
   - Expected result: compile clean, runtime-city smoke still passes, building placement still respects roads/footprints, menu play still starts generation, no city-building spawn broad shell remains, and no new runtime-city performance regression appears.
   - Focused runtime-city building-spawn architecture validation passed: `[RuntimeCityBuildingSpawnArchitectureValidation] result=Passed methods=7`.
   - Broader runtime-city architecture validation passed: `[RuntimeCityArchitectureValidation] result=Passed methods=28`.
   - Runtime city Game scene smoke passed: `[RuntimeCityGameSceneSmokeValidation] result=Passed cityPrefabs=36 productionCityCount=1 validationCityCount=1 buildingSpawnables=32 blockerPrefabs=63`.
   - Unity TestRunner commands for `BuildingPlacementValidationSystemTests` and `BootstrapAndMenuPlayModeTests` exited cleanly but did not emit XML or summary lines in batchmode; this matches the existing Unity TestRunner caveat on this project.
   - Runtime FPS play-button probe completed: `[RuntimeFpsPlayButtonProbe] result=completed avgFps=297.0 minFps=3.3 maxFps=387.4 logs=9 output=/private/tmp/warlinecapture-runtime-fps-probe.json`.
   - Known validation caveats: Unity QuickSearch emitted its startup indexing exception during the FPS probe, and `UnitPathfindingPendingStateReader.Dispose` logged an EntityQuery disposal null reference during editor teardown after the probe result was already written.
   - Wrote handoff report: `Design/AgentReports/2026-05-27_gameplay_runtime_city_building_spawn_refactor_final.md`.

## Progress Notes

- Step 1 complete: roadmap, contract wording, baseline guard, broad replacement guard, and focused architecture batch are in place.
- Step 2 complete: public/internal surface is inventoried, assigned to target owners, and guarded so it cannot grow while the coordinator is decomposed.
- Step 3 complete: runtime-city smoke baseline is recorded with cityPrefabs=36, productionCityCount=1, validationCityCount=1, buildingSpawnables=32, and blockerPrefabs=63.
- Step 4 complete: building-spawn dependency/config context construction moved to `RuntimeCityBuildingSpawnContextCompositionSystemHelper`.
- Step 5 complete: public building-spawn generation entry points delegate through explicit private context-taking methods.
- Step 6 complete: repeated building spawn/delete/reserve validation moved to `RuntimeCityBuildingPlacementPrefabSystemHelper`.
- Step 7 complete: shared roadside/corridor plot placement moved to `RuntimeCityBuildingPlacementPrefabSystemHelper.PlaceFromPlots`.
- Step 8 complete: shared placement paths use explicit context/config values instead of coordinator mutable field/property reads.
- Step 9 complete: landmark offset arrays and hall-distance filtering moved to `RuntimeCityLandmarkOffsetSystem`.
