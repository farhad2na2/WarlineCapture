# RoadBuildSystem Refactor Roadmap

This document owns the `RoadBuildSystem` refactor plan. Runtime city work is tracked in `runtime_city_spawner_refactor_roadmap.md`; building gameplay work should stay in its own roadmap when that refactor starts.

## Target

Target file: `Assets/Game/Scripts/Systems/RoadBuildSystem.cs`

Current size at roadmap creation: 4041 lines.

Goal: retire the broad managed `RoadBuildSystem` shell by moving road state, road visuals, input/session flow, ECS road projection, runtime-city road generation, and legacy building-placement compatibility into narrow ECS-aligned systems. The final state should have no production source file named `RoadBuildSystem.cs`; callers should depend on explicit road build/read/command boundaries.

## Current Responsibility Inventory

- Config projection and startup wiring: reads `RoadBuildSystemConfig`, stores camera/runtime roots, creates runtime scene roots, caches prefabs/settings.
- Road graph state: `_edgeCounts`, `_strokeIdsByCell`, `_strokes`, `_roadTiles`, `_nextStrokeId`, endpoint connection logic, stroke add/delete/restore.
- Runtime-city road generation API: `CreateRoadStrokeFromRoadCells`, `CreateAutobahnStrokeFromRoadCells`, standalone straight-chain connector helpers, autobahn connector lookup.
- Road build input/session: build-mode activation, pointer press/release, drag-axis selection, preview path construction, delete prompt, session snapshot/rollback/confirm.
- Road preview visuals: preview object pool, preview path rebuild, preview material alpha, preview placement.
- Road visual realization: prefab variant cache, mask resolution, chunk mesh building, special road object creation, marker alignment, footprint bounds.
- Road-to-ECS projection: grid query ownership, road/sidewalk/dirt buffer writes, deferred sync depth, blocker removal under roads.
- Road footprint queries for other systems: `HasRoadInFootprint`, `FillRoadFootprintMask`, road world footprint visitors.
- Legacy building compatibility: soldier-base placement, runtime building data, building selection/delete, spawned soldier helper, building blocker/combat entity creation. Most of this should migrate to `BuildingGameplaySystem` or existing building placement interaction boundaries.
- UI/HUD coupling: delete-road IMGUI modal, command-mode bridge calls, static minimap dirty notification.
- Static state compatibility: `SetBuildMode(bool)` creates a runtime gameplay state instance and should be replaced by an explicit command boundary.

## Non-Goals

- Do not redesign road art, road placement rules, or runtime-city city layout during this refactor.
- Do not move logic into UI views, `GameBootstrap`, or runtime-city generation systems.
- Do not add singleton/static gameplay state. Static helpers are allowed only for pure data/math operations.
- Do not use reflection or hidden global lookups to preserve compatibility.
- Do not rename serialized `RoadBuildSystemConfig` assets until a separate config migration plan exists.

## Phase 1: Stabilize Contracts And Baseline

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add architecture tests that require the roadmap, record the current size, and prevent new direct `RoadBuildSystem.Instance` or static runtime access.
   - Added `GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation` as the focused batch entry point for this refactor.
   - Added contract wording for the target road boundaries and serialized road config naming exception.
   - Expected output: future steps cannot drift without updating the contract.

2. Complete: Create `RoadBuildReadModelSystem`
   - Owns read-only state currently exposed as `IsRoadBuildModeActive`, `IsDraggingBuildInteraction`, pending placement state, and selected-road/delete-prompt state.
   - Existing camera/runtime callers should read this narrow boundary instead of storing `RoadBuildSystem`.
   - Created `RoadBuildReadModelSystem`.
   - `RtsSelectionRuntimeCameraSystem` and its context now consume `RoadBuildReadModelSystem` instead of `RoadBuildSystem`.
   - `SelectionGameplayStartupSystem` receives the read model for camera/read state instead of the broad road shell.
   - `ManagedGameplayStartupSystem` composes the read model from the current road shell as a temporary compatibility source until later steps move owned state into extracted road systems.
   - Expected output: selection/camera systems no longer need the broad road shell just to know whether a road/build interaction is active.

3. Complete: Create `RoadBuildConfigSystem`
   - Owns projection from `RoadBuildSystemConfig` into a snapshot: prefabs, grid origin, build plane, road grid size, chunk size, preview alpha, soldier-base compatibility fields, and placement colors.
   - `RoadBuildSystem` temporarily delegates config copying to this system.
   - Created `RoadBuildConfigSystem` with an immutable `Snapshot`.
   - `RoadBuildSystem` now asks `RoadBuildConfigSystem.TryCreateSnapshot` and applies the snapshot to its existing compatibility fields.
   - Direct `config.*` field projection no longer lives in `RoadBuildSystem`.
   - Expected output: no copied config assignment remains in the broad shell.

4. Complete: Create `RoadRuntimeRootSystem`
   - Owns creation/disposal of `RuntimeRoads`, `RuntimeAutobahns`, `RuntimeAutobahnConnectors`, `RuntimeDebugStraightRoads`, and any temporary compatibility roots.
   - Preserve exact root names and transforms.
   - Created `RoadRuntimeRootSystem` with a `Roots` value for road, autobahn, connector, debug-straight, and temporary building roots.
   - `RoadBuildSystem` now requests roots through `RoadRuntimeRootSystem.CreateRoots` and disposes them through `DisposeRoots`.
   - Direct runtime root creation/disposal no longer lives in `RoadBuildSystem`.
   - Expected output: scene hierarchy composition is not owned by road gameplay logic.

## Phase 2: Extract Road Data And Queries

5. Complete: Create `RoadNetworkSystem`
   - Owns stroke ids, edge counts, cell-to-stroke index, road-tile map, endpoint connection rules, add/delete stroke mutation, and session snapshot data.
   - Exposes explicit operations: create stroke, delete stroke, restore snapshot, enumerate road tiles, query cell stroke ids.
   - Created `RoadNetworkSystem` with graph data types, stroke/edge/cell indexes, special-road metadata, create/delete mutation, snapshot capture/restore, and edge/mask queries.
   - `RoadBuildSystem` now delegates stroke creation/deletion, graph mask queries, special-road metadata rebuild, and session snapshot capture/restore to `RoadNetworkSystem`.
   - Visual chunk refresh and ECS projection remain in `RoadBuildSystem` until the planned visual/projection phases.
   - Expected output: road graph mutation is data-driven and testable without visuals.

6. Complete: Create `RoadPathPlanningSystem`
   - Owns drag-axis path planning, straight segment append, endpoint preview connection expansion, adjacent road cell enumeration, and preview mask construction.
   - Keep this pure-data where possible.
   - Created `RoadPathPlanningSystem` with drag-axis resolution, L-shaped path construction, preview proposed-edge/dirty-cell planning, endpoint preview expansion, and preview mask construction.
   - `RoadBuildSystem` now delegates release path creation and preview planning/masks to `RoadPathPlanningSystem`.
   - Preview GameObject pooling and visual placement remain in `RoadBuildSystem` until `RoadPreviewSystem`.
   - Expected output: input and runtime-city callers use the same road path rules without duplicating math.

7. Complete: Create `RoadFootprintQuerySystem`
   - Owns `HasRoadInFootprint`, `FillRoadFootprintMask`, road world footprint visitors, footprint kind detection, reserve/dirt/sidewalk marker classification, and bounds transform helpers.
   - Depends on `RoadNetworkSystem` and visual footprint data, not the broad shell.
   - Created `RoadFootprintQuerySystem` with context-driven road tile/special visual/visual footprint reads, road footprint mask queries, footprint visitors, footprint kind classification, grid center bounds checks, and bounds transform helpers.
   - `RoadBuildSystem` now delegates public footprint queries and road projection/blocker footprint visiting to `RoadFootprintQuerySystem`.
   - Shared combined visual footprint data moved behind `RoadFootprintQuerySystem` while chunk rendering remains a temporary consumer until RoadVisualVariantSystem/RoadChunkVisualSystem.
   - Expected output: `BuildingGameplaySystem` reads road footprint data through a narrow query boundary.

8. Complete: Create `RoadGridProjectionSystem`
   - Owns `EntityQuery` creation/caching, `GridRoad`, `GridRoadSidewalk`, and `GridRoadDirt` buffer writes, clear projection, deferred sync depth, and invalidated-handle safety.
   - Must reacquire buffers after structural changes instead of storing stale `DynamicBuffer` handles.
   - Created `RoadGridProjectionSystem` with query caching, road buffer lookup, road/sidewalk/dirt projection writes, clear projection, deferred sync state, grid data lookup, and runtime blocker cleanup over road footprints.
   - `RoadBuildSystem` now delegates deferred sync begin/end, road ECS sync requests, clear projection, grid-data lookup, and blocker cleanup to `RoadGridProjectionSystem`.
   - Projection writes reacquire ECS buffers inside each projection/clear operation instead of keeping long-lived dynamic buffer handles.
   - Expected output: road-to-ECS projection is isolated and can be performance tested.

## Phase 3: Extract Visual Ownership

9. Complete: Create `RoadVisualVariantSystem`
   - Owns prefab variant cache, combined visual data, marker layout cache, mask normalization, variant lookup, and prefab-to-mask mapping.
   - Created `RoadVisualVariantSystem` with variant data, connector marker data, marker layouts, prefab mapping, variant cache construction, combined visual mesh/material/footprint data construction, visual cache disposal, autobahn mask normalization, and axis/direction mask helpers.
   - `RoadBuildSystem` now delegates prefab lookup, visual cache rebuild, visual cache disposal, variant lookup, axis/direction mask helpers, and read access to visual data/marker layouts through `RoadVisualVariantSystem`.
   - Chunk rendering, preview object pooling, and special-road object placement remain in `RoadBuildSystem` until the next visual ownership phases.
   - Expected output: variant and marker parsing no longer sit beside input/session code.

10. Complete: Create `RoadChunkVisualSystem`
    - Owns chunk membership, dirty chunk queue, chunk mesh build/rebuild/dispose, mesh/material lifetime, and normal road placement transforms.
    - Created `RoadChunkVisualSystem` with context-driven road tile, visual data, special-road cell, root, grid, and chunk-size inputs.
    - `RoadBuildSystem` now delegates normal road chunk add/remove, dirty chunk rebuild, chunk disposal/clear, and placement-position calculation to `RoadChunkVisualSystem`.
    - Preview object pooling and special-road object placement remain in `RoadBuildSystem` until steps 11 and 12.
    - Expected output: chunk rendering can be optimized independently from road graph mutation.

11. Complete: Create `RoadPreviewSystem`
    - Owns preview object pool, preview object creation/release, preview path rebuild, preview alpha/material setup, and preview cleanup.
    - Created `RoadPreviewSystem` with context-driven visual data, road root, grid placement settings, path planning, network state, visual type resolution, and variant lookup.
    - `RoadBuildSystem` now delegates preview update/clear/disposal to `RoadPreviewSystem` and no longer owns road preview GameObject lists, pools, material alpha setup, preview object creation/release, or preview rebuild loops.
    - Expected output: pointer input/session state can ask for preview changes without owning preview GameObjects.

12. Complete: Create `RoadSpecialVisualSystem`
    - Owns autobahn and connector cell metadata, special road object creation/destruction, marker-to-marker alignment, special road mask selection, standalone debug straight road visuals, and connector marker logging.
    - Created `RoadSpecialVisualSystem` with context-driven road tiles, strokes, marker layouts, connector marker data, runtime roots, grid placement settings, prefab lookup, and variant lookup.
    - `RoadBuildSystem` now delegates special road rebuild, special/debug visual disposal, connector road-cell lookup, connector marker logging, standalone straight chain creation, standalone chain end lookup, and debug city road-network creation.
    - `RoadSpecialVisualSystem` owns the special road object registry while exposing it read-only through the existing footprint context until later query callers move off the shell.
    - Expected output: autobahn/special-road visuals are not coupled to road graph storage or ECS projection.

## Phase 4: Extract Build Interaction

13. Complete: Create `RoadBuildSessionSystem`
    - Owns build-mode activation, road session begin/confirm/cancel, delete-road prompt state, session snapshot handoff, and minimap dirty event publication.
    - Created `RoadBuildSessionSystem` with session state, active tool mode, delete-prompt state, build-click skip frames, road session snapshot storage, road/soldier-base build-mode activation, confirm/cancel road session commands, and exit-build-mode command flow.
    - Created `RoadMinimapEventSystem` as the road minimap event boundary; RoadBuildSystem no longer invokes `MainMenuPlayUI.NotifyStaticMinimapChanged` directly.
    - `RoadBuildSystem` now delegates road build activation, road session confirm/cancel, exit build mode, delete-prompt mutation, skip-frame consumption, and minimap event publication.
    - Expected output: road build lifecycle is explicit and UI-independent.

14. Complete: Create `RoadBuildInputSystem`
    - Owns pointer-state processing, pointer-over-UI checks, pressed/released/drag handling, drag-axis updates, pending start cell, and clicked-road delete selection.
    - Consumes `RoadBuildSessionSystem`, `RoadPathPlanningSystem`, `RoadNetworkSystem`, and `RoadPreviewSystem`.
    - Created `RoadBuildInputSystem` with pointer-state processing, active road-mode guards, building-placement drag handoff, road stroke drag start/release, drag-axis updates, clicked-road delete selection, and pending road-build cancellation.
    - `RoadBuildSystem.Update()` is now a thin input delegation wrapper while stroke creation and building-placement side effects remain behind injected domain callbacks for compatibility.

15. Complete: Create `RoadBuildCommandSystem`
    - Owns public commands currently on the shell: activate road build mode, confirm/cancel session, exit road mode, and the replacement for static `SetBuildMode(bool)`.
    - Uses `RuntimeGameplayStateSystem` through explicit context.
    - Created `RoadBuildCommandSystem` with explicit context for runtime gameplay state, road session commands, and road drag cleanup.
    - `RoadBuildSystem` command methods now delegate to `RoadBuildCommandSystem`; the legacy static `SetBuildMode(bool)` is a compatibility wrapper around the command boundary.
    - Campaign mission guard test now calls `RoadBuildCommandSystem` directly instead of `RoadBuildSystem.SetBuildMode`.

16. Complete: Create `RoadDeletePromptSystem`
    - Owns delete-road modal state and result handling.
    - Move IMGUI drawing out of road graph/session logic, or replace it with an existing UI command surface if available.
    - Created `RoadDeletePromptSystem` with explicit runtime/session/delete-stroke context.
    - `RoadBuildSystem.OnGui()` is now a temporary wrapper that delegates delete prompt drawing and delete/cancel result handling to RoadDeletePromptSystem.

## Phase 5: Remove Legacy Building Responsibility

17. Complete: Move soldier-base placement commands to building gameplay
    - Move `BeginSoldierBasePlacement`, `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `DeleteSelectedBuilding`, `ClearSelectedBuilding`, and `ExitBuildMode` compatibility paths to `BuildingPlacementInteractionSystem` / `BuildingGameplaySystem` command boundaries.
    - Road build should only clear or block road interactions when building placement is active through a read model.
    - Added `ExitBuildMode` to `BuildingPlacementInteractionSystem` and its context source, backed by `BuildingGameplaySystem.ExitBuildMode`.
    - RoadBuildSystem building command wrappers now delegate to BuildingPlacementInteractionSystem instead of running fallback building placement, production, selection, or delete logic.
    - Road session cancellation now calls the building interaction cancel wrapper rather than road-owned `CancelBuildingPlacementInternal`.

18. Complete: Move legacy runtime building storage out of road build
    - Move `BuildingDefinition`, `RuntimeBuildingData`, `BuildingPlacementState`, `_runtimeBuildings`, `_selectedBuildingId`, and `_nextBuildingId` ownership to building systems.
    - Preserve `RuntimeBuildingEntityLink` behavior through building interaction context, not road build.
    - Created `BuildingRoadLegacyStorageSystem` backed by existing building-domain contracts: `BuildingDefinition`, `RuntimeBuildingData`, `BuildingPlacementLifecycleCompositionSystemHelper.PlacementState`, and `RuntimeBuildingSystem<RuntimeBuildingData>`.
    - RoadBuildSystem no longer declares nested building data/state classes or owns `_runtimeBuildings`, `_selectedBuildingId`, `_nextBuildingId`, `_soldierBaseDefinition`, or `_activeBuildingPlacement`.
    - Legacy road runtime links now configure through `BuildingPlacementInteractionSystem` when the compatibility path is available.

19. Complete: Move building ECS creation helpers out of road build
    - Move blocker entity creation, combat entity creation, runtime link attachment, and player-unit spawn-near-building helper to building systems.
    - Created `BuildingRoadLegacyEcsSystem` for the remaining road legacy building ECS compatibility path.
    - RoadBuildSystem now delegates blocker/combat entity creation and runtime link attachment to that building-owned boundary.
    - Player-unit spawn-near-building compatibility logic no longer lives in RoadBuildSystem.
    - Expected output: road build has no direct building combat/blocker/unit spawn responsibility.

20. Complete: Remove road-to-building compatibility callbacks
    - Replace `HandleRuntimeBuildingEntityDestroyed` road callback with a building-owned destruction path.
    - Update `RuntimeBuildingEntityLink` to call building interaction only.
    - Removed the RoadBuildSystem destruction callback and RuntimeBuildingEntityLink road-controller fallback overload.
    - Runtime building links now call BuildingPlacementInteractionSystem only.
    - Expected output: destroyed building cleanup no longer reaches through road build.

## Phase 6: Extract Runtime-City Road API

21. Complete: Create `RoadRuntimeGenerationSystem`
    - Owns runtime-city-facing road generation commands: create road stroke from cells, create autobahn stroke, standalone connector chain, connector road-cell lookup, chain-end lookup, and debug city road generation if still needed.
    - Created `RoadRuntimeGenerationSystem` for runtime-city-facing road commands and road generation read/sync helpers.
    - RoadBuildSystem now delegates runtime road generation wrappers to this boundary while callers migrate in step 22.
    - Runtime city should depend on this boundary through `RuntimeCityRoadBuildBridgeCompositionSystemHelper`, not on `RoadBuildSystem`.
    - Expected output: runtime city no longer requires the broad road shell.

22. Complete: Migrate `RuntimeCityRoadBuildBridgeCompositionSystemHelper`
    - Change bridge configuration from `RoadBuildSystem` to `RoadRuntimeGenerationSystem` plus any required read/query systems.
    - RuntimeCityRoadBuildBridgeCompositionSystemHelper now stores RoadRuntimeGenerationSystem plus its context.
    - RuntimeCityCompositionSystemHelper receives the runtime road generation boundary instead of a RoadBuildSystem for road generation.
    - Runtime city startup readiness now checks HasRoadRuntimeGenerationSystem.
    - Preserve runtime-city validation smoke behavior.
    - Expected output: runtime city road build bridge has no direct broad-shell reference.

23. Complete: Migrate `BuildingGameplaySystem` road queries
    - Replace `_roadBuildController.FillRoadFootprintMask` and `_roadBuildController.HasRoadInFootprint` with `RoadFootprintQuerySystem`.
    - BuildingGameplaySystem now stores RoadFootprintQuerySystem plus context instead of RoadBuildSystem for placement validation.
    - BuildingGameplayCompositionSystemHelper passes the narrow road footprint boundary during building initialization.
    - Expected output: building placement validation depends on road footprint query only.

24. Complete: Migrate selection/camera/menu references
    - Move `RtsSelectionRuntimeCameraSystem`, `SelectionGameplayStartupSystem`, `MainMenuPlayUI`, `MenuStartupSystem`, and `GameplayRuntimeUpdateSystem` off `RoadBuildSystem`.
    - Use `RoadBuildReadModelSystem`, `RoadBuildCommandSystem`, and narrow update systems.
    - GameplayRuntimeUpdateSystem now receives narrow road runtime update and IMGUI actions instead of RoadBuildSystem.
    - MenuStartupSystem now receives a narrow road menu-bind action, and MainMenuPlayUI no longer accepts RoadBuildSystem.
    - Selection camera/startup systems remain on RoadBuildReadModelSystem.
    - Expected output: no non-road caller stores the broad shell.

## Phase 7: Composition, Deletion, And Guards

25. Complete: Create temporary `RoadBuildCompositionSystem`
    - Owns wiring of extracted road systems only while callers migrate.
    - Must not own graph algorithms, visual algorithms, ECS buffer writes, input processing, or building placement logic.
    - RoadBuildCompositionSystem now owns narrow source/context/lifecycle wiring, RoadBuildReadModelSystem wiring, and building-interaction binding after the legacy shell was retired.
    - ManagedGameplayStartupSystem consumes the composition result instead of directly constructing RoadBuildSystem or RoadBuildReadModelSystem.
    - Expected output: constructor/startup wiring is explicit and easy to delete later.

26. Complete: Move managed startup wiring off `RoadBuildSystem`
    - Update `ManagedGameplayStartupSystem`, `GameBootstrap`, and feature startup to construct/configure extracted road systems.
    - Managed startup now passes road footprint queries, runtime generation, runtime update/gui/dispose actions, and menu/runtime bind actions from RoadBuildCompositionSystem.Result.
    - GameBootstrap no longer stores RoadBuildSystem; it stores the road read/runtime-generation boundaries and narrow actions.
    - GameplayFeatureStartupCompositionSystemHelper now receives RoadRuntimeGenerationSystem plus context and a road gameplay bind action instead of RoadBuildSystem.
    - Expected output: startup does not instantiate `new RoadBuildSystem()`.

27. Complete: Replace runtime update and GUI delegates
    - `GameplayRuntimeUpdateSystem` should call narrow road input/session/projection update systems.
    - `OnGui` should be removed or delegated to `RoadDeletePromptSystem`.
    - RoadBuildCompositionSystem runtime update action now calls RoadBuildInputSystem.Update through RoadBuildInputContext and RoadBuildInputCamera.
    - RoadBuildCompositionSystem GUI action now calls RoadDeletePromptSystem.OnGui through RoadDeletePromptContext.
    - Runtime loop wiring no longer uses RoadBuildSystem.Update or RoadBuildSystem.OnGui delegates.
    - Expected output: no runtime loop calls `roadBuild?.Update()` or `roadBuild?.OnGui()`.

28. Complete: Delete `RoadBuildSystem.cs`
    - Delete source and `.meta`.
    - Fix all compile references.
    - RoadBuildSystem.cs and its meta file were deleted.
    - RoadBuildRuntimeStateSystem.cs was later retired by `road_build_runtime_state_system_refactor_roadmap.md`; explicit road boundaries now own runtime behavior.
    - Production source no longer references the RoadBuildSystem type; RoadBuildSystemConfig remains as serialized config compatibility debt.
    - Expected output: no production or test source file named `RoadBuildSystem.cs`.

29. Complete: Remove temporary architecture allowances
    - Add hard guard: `RoadBuildSystem.cs` must not exist.
    - Remove any allowlist entries that temporarily permit broad shell references.
    - Keep serialized `RoadBuildSystemConfig` name as documented data compatibility debt until a separate migration.
    - RoadBuildCompositionSystem exposes no broad `RoadState` or temporary road-runtime holder field.
    - RoadBuildRuntimeStateSystem follow-up roadmap deleted the temporary holder and moved composition wiring to source/context/lifecycle systems.
    - Architecture validation now rejects restoring `RoadBuildSystem.cs`, `RoadBuildSystem.cs.meta`, exact production `RoadBuildSystem` type references, or `RoadBuildRuntimeStateSystem.cs`.
    - Expected output: architecture tests reject shell restoration.

30. Complete: Validation gate
    - Run architecture batch covering road build guards.
    - Run runtime-city smoke validation, because runtime city uses road generation.
    - Run building placement smoke validation, because building gameplay uses road footprint queries.
    - Run bootstrap/menu play-button smoke.
    - Run one focused performance diagnostic pass and compare road/build/runtime-city update steps for regressions.
    - Road-build architecture validation passed: 31 methods.
    - Runtime-city architecture validation passed: 28 methods.
    - Runtime-city Game scene smoke passed with cityPrefabs=36, productionCityCount=1, validationCityCount=1, buildingSpawnables=32, blockerPrefabs=63.
    - Building placement validation passed 4/4 and building runtime boundary validation passed 1/1.
    - Bootstrap/menu play-mode smoke passed 7/7.
    - Runtime FPS play-button probe completed and clicked Play. It reported no persistent RoadBuild runtime cost after startup; observed RuntimeCity startup hitches are recorded as follow-up performance debt.
    - Expected result: compile-clean, runtime city can still build roads, building placement still respects roads, road build mode can create/delete/rollback roads, and no broad road shell remains.

## Proposed First Implementation Batch

Start with steps 1-4 only:

- They add tracking and reduce config/root/read-model coupling without touching the risky graph/visual/runtime-city behavior.
- They provide architecture tests before the major extractions.
- They keep public behavior unchanged and create the seams needed for the later graph and visual extractions.
