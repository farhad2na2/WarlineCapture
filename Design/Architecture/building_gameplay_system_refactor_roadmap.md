# BuildingGameplaySystem Refactor Roadmap

This document owns the `BuildingGameplaySystem` refactor plan. Road build work is tracked in `road_build_system_refactor_roadmap.md`; runtime city work is tracked in `runtime_city_spawner_refactor_roadmap.md`. Keep this roadmap focused on deleting the broad managed building gameplay shell without adding gameplay features during the refactor.

## Target

Target file: `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`

Current size at roadmap creation: 2021 lines.

Step 4 dependency-injection transition size: 2082 lines. This is a temporary ceiling caused by assigning composition-owned child systems into the shell while the remaining context factories still live there. Later extraction steps should reduce this number; it must not increase without updating this roadmap and guard intentionally.

Step 5 dependency-binding transition size: 2071 lines. Dependency references now live in `BuildingGameplayDependencyCompositionSystemHelper`; the shell may read through that boundary only while later startup/context extraction steps remove the remaining shell callbacks.

Step 6 startup/config transition size: 2049 lines. Production composition now routes placement config, world camera, runtime root, road footprint query/context, faction visuals, and day/night directly into `BuildingPlacementStartupSystemHelper` and `BuildingGameplayDependencyCompositionSystemHelper`; `BuildingGameplaySystem.Init` remains only as temporary compatibility debt for tests/legacy callers.

Step 7 disposal transition size: 2041 lines. Production composition now disposes through `BuildingGameplayDisposalExecutionCompositionSystemHelper`; `BuildingGameplaySystem.Dispose` remains only as temporary tests/legacy compatibility and delegates to the disposal system.

Step 8 ECS query transition size: 1982 lines. Entity query caching now lives in `BuildingGameplayEcsQueryCompositionSystemHelper`; `BuildingGameplaySystem` may temporarily expose query delegates/handles to existing context factories until those factories move out in later phases.

Step 9 grid-data transition size: 1984 lines. Grid data retrieval and grid-cell pointer conversion now route through `BuildingGameplayGridDataCompositionSystemHelper`; `BuildingGameplaySystem` may temporarily expose wrapper delegates while placement, selection, validation, and runtime tick contexts migrate to narrow factories.

Step 10 invalid-cell cache transition size: 1958 lines. Placement invalid-cell prefix arrays, rebuild state, road-footprint mask creation, runtime blocker filtering, and cached placement validation now live in `BuildingPlacementInvalidCellSystem`; `BuildingGameplaySystem` may temporarily expose wrapper methods while context factories move out.

Step 11 spawn random-state transition size: 1951 lines. Building spawn random state now lives in `BuildingSpawnSystem`; production/runtime tick delegates read and write the state through that spawn owner instead of `BuildingGameplaySystem`.

Step 12 build-button command transition size: 1919 lines. Build-button placement start commands now route through `BuildingPlacementCommandSystem`; `BuildingGameplaySystem` keeps only temporary public wrappers for compatibility and context factories pass command-system delegates directly.

Step 13 session command transition size: 1919 lines. Placement confirm, cancel, exit, pointer-down, and active-placement cost commands now route through `BuildingPlacementCommandSystem`; `BuildingGameplaySystem` keeps only temporary public wrappers for compatibility.

Step 14 placement visual-update transition size: 1824 lines. Active-placement focus, placement visual update, confirm validation, and placement object handoff now route through `BuildingPlacementVisualUpdateCompositionSystemHelper`; `BuildingGameplaySystem` keeps only temporary wrapper callbacks and context creation for compatibility.

Step 15 wall helper transition size: 1770 lines. Wall preview scratch state now lives in `BuildingPlacementPreviewPresentationSystemHelper`, wall commit scratch state now lives in `BuildingPlacementContextCompositionSystemHelper`, and placement rotate-vertical policy now lives in `BuildingBarrierSystem`; `BuildingGameplaySystem` no longer owns wall-specific helper methods or collections.

Step 16 production button command transition size: 1765 lines. Selected-building production button commands now route through `BuildingUiCommandSystem`; `BuildingProductionRequestBoundary` owns active-building production request execution, and `BuildingUiContextSystem` wires the command boundary to the production request context.

Step 17 camp request transition size: 1736 lines. Camp item affordability, request execution, missing-producer failure reporting, focus memory, and deferred focus now route through `BuildingUiCommandSystem` and `BuildingProductionRequestBoundary`; `BuildingGameplaySystem` no longer owns camp request callbacks.

Step 18 UI read method transition size: 1742 lines. Selected-building flags, active-building flags, status/label/description/health/preview reads, and selected-building production affordability now route through `BuildingUiQuerySystem`; `BuildingGameplaySystem` keeps only temporary compatibility wrappers that delegate to the UI query boundary.

Step 19 menu binding transition size: 1742 lines. `BuildingGameplayCompositionSystemHelper.Result.BindMainMenu` now binds main-menu runtime dependency state through `BuildingGameplayDependencyCompositionSystemHelper` directly; menu startup continues to receive explicit UI command, UI query, and placement interaction systems/contexts.

Step 20 runtime building read API transition size: 1742 lines. Runtime building id lists, role filters, focus/destroyed/refugee/combat/owner/wall/city/approach reads, faction production count reads, and base-breach target read routing now use `BuildingRuntimeQuerySystem`; composition exposes `RuntimeQuery` and `RuntimeQueryContext` for direct consumers.

Step 21 runtime building spawn command transition size: 1742 lines. Runtime building spawn commands, initial roster/test spawn, runtime wall run/segment spawn, runtime footprint queries, and initial placement origin search remain behind `BuildingRuntimeSpawnCommandSystem`; composition now exposes `RuntimeSpawnCommand` and `RuntimeSpawnCommandContext`, and runtime-city spawn routes through the same command boundary instead of its own spawn-system instance.

Step 22 faction spawn point query transition size: 1717 lines. Faction production spawn-slot lookup and available faction helipad spawn resolution now route through `BuildingSpawnSystem`; `BuildingGameplaySystem` no longer scans runtime building spawn-slot arrays directly.

Step 23 configured unit prefab resolution transition size: 1678 lines. Configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution now route through `RuntimeUnitPrefabSystem`; `BuildingGameplaySystem` keeps only temporary compatibility wrappers.

Step 24 initial roster/test helper transition size: 1599 lines. Initial roster and initial-building spawn remain in `BuildingRuntimeSpawnSystem` / `BuildingRuntimeSpawnCommandSystem`; editor-only runtime test helpers moved out of `BuildingGameplaySystem`, and step 35 later deleted `BuildingGameplayTestHarness`.

Step 25 visual helper transition size: 1583 lines. Placement visual instance creation, placement visual positioning, footprint-center delegates, runtime visual initialization, runtime marker visibility refresh, and owner-faction visual tint now route directly through `BuildingPlacementVisualPresentationSystemHelper`, `BuildingPlacementGridSystem`, `BuildingRuntimeVisualSystem`, and `BuildingRuntimeOwnershipSystem`; `BuildingGameplaySystem` no longer keeps visual helper wrapper methods.

Step 26 building selection transition size: 1542 lines. Visible selectable checks, selected-building deletion, select/focus, focus-world-position resolution, and camera-focus selection flow now route through `BuildingSelectionSystem`; `BuildingGameplaySystem` no longer keeps private selection/focus helper wrappers.

Step 27 runtime destruction/entity-link transition size: 1538 lines. Runtime building delete callbacks and runtime entity destroyed callbacks now route through `BuildingRuntimeEntitySystem` using `BuildingCombatSystem`; `BuildingGameplaySystem` no longer exposes `DeleteBuildingById` or `HandleRuntimeBuildingEntityDestroyed` shell methods.

Step 28 runtime entity creation transition size: 1513 lines. Runtime blocker creation, runtime building path-blocking policy, and runtime building combat entity creation now bind inside `BuildingRuntimeContextSystem` against `BuildingRuntimeEntitySystem`; `BuildingGameplaySystem` no longer keeps private creation/policy wrapper methods.

Step 29 redirect/hauler bridge transition size: 1473 lines. Runtime creation redirect callbacks, deferred redirect footprint callbacks, pending marker refresh callbacks, selected hauler order assignment, and building approach checks now bind through `BuildingRuntimeContextSystem` to `BuildingPlacementRedirectCompositionSystemHelper` / `BuildingResourceHaulerBridgeSystem`; `BuildingGameplaySystem` no longer keeps private redirect or hauler bridge wrapper methods.

Step 30 placement context factory transition size: 1446 lines. Placement cancel/begin/confirm lifecycle context creation plus placement session/command context creation now live in `BuildingPlacementContextCompositionSystemHelper`; `BuildingGameplaySystem` no longer declares private cancel/begin/confirm/session factory wrappers.

Step 31 runtime context factory transition size: 1446 lines. Runtime spawn command context creation now lives in `BuildingRuntimeContextSystem`; managed composition and runtime tick context creation use `BuildingRuntimeContextSystem` directly for runtime visual/combat/query/barrier contexts instead of shell context wrapper methods.

Step 32 production/UI context factory transition size: 1446 lines. Production context source creation now routes through `BuildingProductionContextCompositionSystemHelper.CreateSource`, UI context source creation routes through `BuildingUiContextSystem.CreateSource`, interaction context source creation routes through `BuildingPlacementInteractionContextCompositionSystemHelper.CreateSource`, and runtime resource prefab source creation routes through `BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateSource`.

Step 33 runtime tick composition transition size: 1417 lines. Runtime tick source assembly now uses `BuildingGameplaySourceCompositionSystemHelper` child systems directly for runtime visual/combat/barrier/input/boundary tick phases; `BuildingGameplaySystem` no longer exposes `RuntimeTickSystem`, `RuntimeTickDomains`, `RuntimeInputDomains`, runtime state getter delegates, runtime boundary query delegates, or tick-only production/resource properties.

Goal: retire `BuildingGameplaySystem.cs` as a broad managed orchestration shell. Runtime building behavior must remain in narrow `*System` boundaries: placement, validation, preview, commit, runtime spawn, runtime query, UI command/query, production, resources, combat, barriers, selection, and runtime tick publication. The final state should have no production source file named `BuildingGameplaySystem.cs`; callers should consume explicit building systems, ECS request/response buffers, or narrow context systems from `BuildingGameplayCompositionSystemHelper.Result`.

## Current Responsibility Inventory

- Managed lifetime and composition: constructs dozens of building, resource, runtime, UI, placement, production, and selection systems internally.
- Startup/config projection: initializes placement config, camera, runtime UI root, road footprint query, faction visuals, day/night, and prefab/runtime settings.
- Dependency binding: stores menu, selection camera, building interaction, grid blocker, runtime city, citizen population, faction visual, and day/night dependencies.
- Entity query ownership: caches world/entity queries for grid data, redirect units, prefab registry, selected units, haulers, live units, faction units, and runtime boundary publication.
- Placement invalid-cell cache: owns prefix arrays and rebuild logic that mixes road footprint masks, grid roads, dynamic blockers, and runtime blockers.
- Public placement commands: begin soldier base/tent/factory placement, begin placement for configured spawnables, confirm/cancel placement, placement pointer notification, and exit build mode.
- UI production command facade: selected-building production buttons, camp item requests, production affordability checks, and production focus.
- Runtime building facade: runtime building ids, focus positions, owner faction, destroyed state, approach cells, combat info, wall/gate data, wall run spawn, runtime building spawn, initial roster spawn, and test tick helpers.
- Visual and marker wrappers: visual instance creation, placement object positioning, runtime owner tint/faction, marker visibility, resource animation, and destroyed-building cleanup callbacks.
- Combat/barrier/entity wrappers: blocker creation, combat entity creation, gate/pass faction updates, barrier door test data, and runtime building entity links.
- Context factories: creates source/context values for placement, runtime, spawn, production, UI, interaction, runtime query, runtime resource prefab, runtime visual, combat, redirect, barrier, and selection.
- Test compatibility: `BuildingGameplayTestHarness` inherits from `BuildingGameplaySystem`, keeping broad shell construction in many editor tests.

## Public/Internal Surface Inventory Freeze

Step 3 freezes the current `BuildingGameplaySystem` public/internal surface. Every name below is temporary debt and has an assigned target owner. New public/internal members must not be added to the shell; instead, add the behavior to the owning narrow system.

- Composition/startup/dependency owner: `Init`, `BindDependencies`, `Dispose`, `RoadPreviewPrefab`, `BuildButtonPreviewDistanceMultiplier`, `UnitCommandButtonPreviewDistanceMultiplier`.
- Runtime tick/composition owner: `RuntimeBuildingRegistry`, `RuntimeContextSystem`, `EnsureEntityQueries`.
- Runtime city/resource/prefab owner: `RuntimeCitySpawnSystem`, `RuntimeQuerySystem`, `RuntimeResourcePrefabContextSystem`, `CreateRuntimeBuildingQueryContext`, `CreateRuntimeResourcePrefabContextSource`, `TryResolveConfiguredUnitPrefabEntity`, `TryResolveSpawnUnitPrefab`, `TrySpendDollars`, `SetInitialResourceTotals`.
- Placement state/command owner: `HasPendingBuildingPlacement`, `CanConfirmBuildingPlacement`, `HasActiveBuilding`, `CurrentActiveBuildingId`, `IsDraggingPlacementPreview`, `BeginDeferredRuntimeBuildingSideEffects`, `EndDeferredRuntimeBuildingSideEffects`, `BeginSoldierBasePlacement`, `BeginSoldierTentPlacement`, `BeginFactoryPlacement`, `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `ExitBuildMode`, `NotifyPlacementUiPointerDown`, `CreateActivePlacementPointerContext`.
- UI query/read owner: `HasSelectedBuilding`, `PlacementStatusText`, `SelectedBuildingLabel`, `SelectedBuildingDescription`, `CanCreatePrimaryUnitFromSelectedBuilding`, `CanCreateSecondaryUnitFromSelectedBuilding`, `CanCreateTertiaryUnitFromSelectedBuilding`, `CanCreateQuaternaryUnitFromSelectedBuilding`, `CanCreateUnitFromSelectedBuilding`.
- Production command owner: `CreateUnitFromSelectedBuilding`, `CreateUnitFromBuilding`, `CreateSecondaryUnitFromSelectedBuilding`, `CreateSecondaryUnitFromBuilding`, `CreateTertiaryUnitFromSelectedBuilding`, `CreateTertiaryUnitFromBuilding`, `CreateQuaternaryUnitFromSelectedBuilding`, `CreateQuaternaryUnitFromBuilding`, `ArmNextProductionFromUi`, `CreateSoldierFromSelectedBuilding`.
- Runtime building query/spawn owner: `GetRuntimeHouseBuildingIds`, `GetRuntimeBuildingIdsByRole`, `TryGetRuntimeBuildingFocusWorldPosition`, `TryGetRuntimeBuildingDestroyedState`, `TryGetRuntimeBuildingRefugeeSettings`, `TryGetRuntimeBuildingCombatInfo`, `TryResolveBaseBreachTarget`, `TryGetRuntimeBuildingApproachCell`, `IsRuntimeBuildingApproachCell`, `TryGetRuntimeBuildingPlacementFootprint`, `TryGetRuntimeWallSegmentFootprint`, `TryGetFactionProductionSpawnPoint`, `TryResolveAvailableFactionHelipadSpawn`, `TrySpawnRuntimeBuilding`, `TrySpawnRuntimeWallRun`, `TrySpawnRuntimeWallSegment`, `SpawnInitialTestRoster`.
- Selection/combat/barrier owner: `BuildingSelectionClickSystem`, `BuildingPlacementInteractionSystem`, `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, `ClearSelectedBuilding`, `DeleteSelectedBuilding`, `SyncDestroyedRuntimeBuildingCombatEntitiesForTests`, `TickRuntimeForTests`, `UpdateRoadBarrierDoorsForTests`, `TryGetRuntimeBuildingDoorOpen01ForTests`, `TryGetRuntimeBuildingEntitiesForTests`, `IsRuntimeBuildingDestroyedForTests`, `GetRuntimeRoadBarrierGateRectsForTests`.
- Context factory owner: `CreateBuildingProductionContextSource`, `CreateBuildingRuntimeContextSource`, `CreateRuntimeContextSystemSource`, `CreateBuildingRuntimeQueryContext`, `CreateBuildingUiCommandContext`, `CreateBuildingUiQueryContext`, `CreateBuildingPlacementInteractionContext`, `CreateBuildingRuntimeVisualContext`, `CreateBuildingPlacementRedirectContext`, `CreateBuildingCombatContext`, `CreateBuildingRuntimeQueryContext`, `CreateBuildingSelectionClickContext`, `CreateBuildingBarrierContext`.

## Non-Goals

- Do not change building placement rules, production balance, combat behavior, resource economy, or UI flow while performing this refactor.
- Do not move logic into `GameBootstrap`, UI views, runtime city, road build, or selection systems to hide the dependency.
- Do not introduce singleton/static gameplay access. Static methods are acceptable only for pure data/math helpers.
- Do not use reflection, service locators, hidden global state, or broad "manager"/"facade" replacements.
- Do not rename serialized `BuildingPlacementSystemConfig` or existing assets in this refactor. Asset/config migration is separate.
- Do not create a new broad managed shell with a different name. Composition may exist only to wire narrow systems and expose explicit result fields.

## Completion Definition

- No production source file named `BuildingGameplaySystem.cs`.
- No production code constructs, stores, or type-references `BuildingGameplaySystem`.
- `BuildingGameplayCompositionSystemHelper.Result` exposes only narrow systems, contexts, read models, command systems, update delegates, and disposal hooks.
- `BuildingGameplayTestHarness` is deleted or replaced by narrow editor test fixtures.
- Architecture tests hard-fail if `BuildingGameplaySystem.cs` returns.
- Focused validation passes: building architecture guard, building runtime boundary tests, placement validation tests, production/resource tests, bootstrap/menu playmode smoke, and one runtime load/play-button smoke.

## Phase 1: Stabilize Contracts And Baseline

1. Complete: Add roadmap and baseline architecture guard
   - Add this document to the architecture contract checks.
   - Record the 2021-line baseline.
   - Add a focused building gameplay architecture batch validation entry point.
   - Guard against new production references from bootstrap, runtime city, selection, road build, UI views, and tests beyond the current known shell/test harness debt.
   - Added `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation`.
   - Added guards for roadmap tracking, baseline line count, and bounded production references.

2. Complete: Add deletion target contract
   - Update `gameplay_solid_ecs_contract.md` with the final target: `BuildingGameplaySystem.cs` must be deleted at the end of this roadmap.
   - Define allowed temporary debt explicitly: temporary `BuildingGameplaySystem` shell compatibility and editor-only `BuildingGameplayTestHarness`.
   - Expected output: future steps cannot claim completion while preserving the broad shell indefinitely.
   - Added contract wording that the final target is deletion of `BuildingGameplaySystem.cs`.
   - Added temporary-debt wording limiting production usage first to `BuildingGameplayCompositionSystemHelper` and editor usage to `BuildingGameplayTestHarness`; step 34 later removed the production construction allowance.
   - Added `BuildingGameplayDeletionTargetContractMustBeExplicit` to the focused architecture validation batch.

3. Complete: Freeze public surface inventory
   - Add a contract test that inventories every public/internal member still exposed by `BuildingGameplaySystem`.
   - Group each member by target owner: placement, production, runtime spawn, runtime query, UI, selection, combat/barrier, visual, context, or test-only.
   - Expected output: later steps remove explicit surface groups, not random line ranges.
   - Added the Public/Internal Surface Inventory Freeze section.
   - Added `BuildingGameplayPublicInternalSurfaceInventoryMustStayFrozen` to prevent new shell members and require owner assignment for every exposed member.

## Phase 2: Move Lifetime And Startup Ownership To Composition

4. Complete: Move child system construction into `BuildingGameplayCompositionSystemHelper`
   - Construct all narrow building systems in composition instead of inside `BuildingGameplaySystem`.
   - Pass them through a typed composition result/source.
   - Do not change behavior; only move lifetime ownership.
   - Expected output: `BuildingGameplaySystem` no longer decides which systems exist.
   - Added `BuildingGameplaySourceCompositionSystemHelper` as the composition-owned child system source.
   - `BuildingGameplayCompositionSystemHelper.Initialize` now creates child systems and passes them to `BuildingGameplaySystem`.
   - `BuildingGameplaySystem` assigns child system fields from the source instead of constructing them inline.
   - The parameterless shell constructor remains only for temporary test harness compatibility and routes through composition-owned child system creation.

5. Complete: Extract building dependency binding
   - Create or extend a narrow `BuildingGameplayDependencyCompositionSystemHelper`.
   - Own menu, selection camera, selection building interaction, grid blocker, runtime city, citizen population, faction visuals, and day/night references.
   - Replace `BuildingGameplaySystem.BindDependencies` storage with dependency-system reads.
   - Added `BuildingGameplayDependencyCompositionSystemHelper` for menu, selection camera, selection building interaction, runtime blocker, runtime city, citizen population, faction visual, and day/night references.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the dependency system.
   - `BuildingGameplaySystem` no longer declares direct dependency fields and routes startup/runtime dependency binding plus callbacks through `BuildingGameplayDependencyCompositionSystemHelper`.

6. Complete: Move placement startup/config wiring
   - Route `BuildingPlacementSystemConfig`, world camera, runtime UI root, road footprint query/context, faction visuals, and day/night into `BuildingPlacementStartupSystemHelper` plus dependency systems directly from composition.
   - Keep serialized config compatibility unchanged.
   - Expected output: `BuildingGameplaySystem.Init` is no longer the startup/config gateway.
   - `BuildingGameplayCompositionSystemHelper.Initialize` now configures `BuildingGameplayDependencyCompositionSystemHelper` and `BuildingPlacementStartupSystemHelper` directly before constructing runtime contexts.
   - `BuildingPlacementStartupSystemHelper` now owns road footprint query/context and exposes road-footprint mask/query helpers.
   - Added `BuildingRuntimeObjectPresentationSystemHelper` as the narrow runtime object destruction boundary used by startup/disposal compatibility code.
   - `BuildingGameplaySystem` no longer stores road footprint query/context fields, and production composition no longer calls `building.Init(...)`.

7. Complete: Move disposal ownership
   - Composition disposes runtime objects through the owning systems directly.
   - Remove `BuildingGameplaySystem.Dispose` as the disposal gateway.
   - Expected output: lifecycle is a set of explicit disposal hooks, not shell disposal.
   - Added `BuildingGameplayDisposalExecutionCompositionSystemHelper` to own runtime building object/entity destruction, runtime registry clearing, and placement startup disposal.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the disposal system.
   - `BuildingGameplayCompositionSystemHelper.Result.Dispose` now calls `BuildingGameplayDisposalExecutionCompositionSystemHelper` directly instead of `building.Dispose`.
   - `BuildingGameplaySystem.Dispose` remains only as temporary compatibility for tests/legacy callers and delegates to `BuildingGameplayDisposalExecutionCompositionSystemHelper`.

## Phase 3: Move Query And Shared Runtime Data Ownership

8. Complete: Extract ECS query ownership
   - Create `BuildingGameplayEcsQueryCompositionSystemHelper`.
   - Own world/entity query caching and invalidation for grid data, unit prefab registry, spawn prefab candidates, selected units, haulers, live units, faction units, redirect units, and runtime boundary entity.
   - Expected output: no `EntityQuery` fields remain in `BuildingGameplaySystem`.
   - Added `BuildingGameplayEcsQueryCompositionSystemHelper` with the previous query cache and query creation logic.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the query system.
   - `BuildingGameplaySystem` no longer declares `World` or `EntityQuery` cache fields and delegates `EnsureEntityQueries` plus query handle reads to `BuildingGameplayEcsQueryCompositionSystemHelper`.

9. Complete: Extract grid data access
   - Move `TryGetGridData`, `TryGetGridForSelection`, `TryGetGridForPlacementInput`, and grid-cell pointer conversion into explicit query/input systems.
   - Systems must reacquire buffers after structural changes.
   - Expected output: placement, selection, validation, and runtime tick contexts read grid data through a narrow query boundary.
   - Added `BuildingGameplayGridDataCompositionSystemHelper` with grid data retrieval and grid-cell pointer conversion delegates.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the grid data system.
   - `BuildingGameplaySystem` delegates grid-data and grid-cell access to `BuildingGameplayGridDataCompositionSystemHelper`, keeping only temporary wrapper methods for existing context factories.

10. Complete: Extract placement invalid-cell cache
   - Create `BuildingPlacementInvalidCellSystem`.
   - Own prefix arrays, prefix dimensions, rebuild flags, road footprint mask creation, runtime blocker checks, and cached-footprint validation.
   - Expected output: road footprint and runtime blocker coupling leaves the broad shell.
   - Added `BuildingPlacementInvalidCellSystem` with invalid-prefix state, rebuild, road footprint mask creation, runtime blocker filtering, cached-footprint checks, and placement rect validation.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the invalid-cell system.
   - `BuildingGameplaySystem` no longer stores invalid-prefix arrays/dimensions or directly calls road-footprint/runtime-blocker cache rebuild helpers.

11. Complete: Move building spawn random state
   - Move `_buildingSpawnRandomState` into `BuildingSpawnSystem` or a narrow `BuildingSpawnRandomSystem`.
   - Production/runtime spawn contexts receive explicit get/set delegates from the owner.
   - Expected output: random state is owned by spawn logic, not gameplay composition.
   - Moved the spawn random-state field and property into `BuildingSpawnSystem`.
   - Added a spawn-owned helipad resolver overload that updates the owned random state internally.
   - Production runtime tick composition now uses `BuildingSpawnSystem.BuildingSpawnRandomState` get/set delegates instead of routing through `BuildingGameplaySystem`.

## Phase 4: Move Placement Command Surface

12. Complete: Extract build-button placement commands
   - Create `BuildingPlacementCommandSystem` or extend the existing placement interaction boundary.
   - Own `BeginSoldierBasePlacement`, `BeginSoldierTentPlacement`, `BeginFactoryPlacement`, and configured-spawnable placement start.
   - Expected output: UI buttons call a building placement command boundary, not `BuildingGameplaySystem`.
   - Added `BuildingPlacementCommandSystem` with build-button placement commands and configured-spawnable placement start.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the placement command system.
   - Interaction and production request context factories now pass command-system delegates for soldier-base and configured-spawnable placement starts.

13. Complete: Move placement confirm/cancel/exit commands
   - Move `ConfirmBuildingPlacement`, `CancelBuildingPlacement`, `ExitBuildMode`, and placement pointer notification to `BuildingPlacementCommandSystem` / `BuildingPlacementInteractionSystem`.
   - Preserve build mode and active placement behavior.
   - Expected output: active placement lifecycle is only in placement systems.
   - `BuildingPlacementCommandSystem` now routes confirm, cancel, exit, pointer-down, and active-placement cost commands to `BuildingPlacementSessionSystem`.
   - `BuildingGameplaySystem` no longer calls `BuildingPlacementSessionSystem` command methods directly.
   - UI and interaction context factories now use command-system delegates for confirm, cancel, and exit.

14. Complete: Move placement focus and visual update callbacks
   - Move active-placement focus, placement visual update, placement validation for confirm, and placement object handoff into placement lifecycle/preview/commit systems.
   - Expected output: `BuildingGameplaySystem` no longer contains placement update or commit helper methods.
   - Added `BuildingPlacementVisualUpdateCompositionSystemHelper` to own active-placement focus, placement visual update, confirm validation for wall placement, current placement focus resolution, and placement object handoff to commit/lifecycle systems.
   - `BuildingGameplaySourceCompositionSystemHelper` now owns the visual-update system.
   - `BuildingGameplaySystem` delegates placement visual callbacks through `BuildingPlacementVisualUpdateCompositionSystemHelper` and no longer calls preview update, pointer hover, wall validation, placement commit, or preview-release methods directly.

15. Complete: Move wall placement preview/commit helpers
   - Move remaining wall preview runs, wall commit runs, wall footprint clone helpers, wall validation context, and rotate-vertical resolution into `BuildingPlacementPreviewPresentationSystemHelper`, `BuildingPlacementCommitSystem`, and `BuildingBarrierSystem`.
   - Expected output: no wall-specific collections or helper algorithms remain in the shell.
   - `BuildingPlacementPreviewPresentationSystemHelper` now owns wall preview scratch runs and the wall placement preview rebuild helper.
   - `BuildingPlacementContextCompositionSystemHelper` now owns wall commit scratch runs and creates commit requests without a shell-owned scratch list.
   - `BuildingBarrierSystem` now owns placement rotate-vertical policy for walls and gates.
   - `BuildingGameplaySystem` no longer owns `_wallPreviewRuns`, `_wallCommitRuns`, `RebuildWallPlacementPreview`, `CreateWallValidationContext`, `ResolvePlacementRotateVertical`, or a clone-definition wrapper.

## Phase 5: Move UI Command And Read Surface

16. Complete: Move production button commands
   - Move selected-building production buttons and indexed production commands into `BuildingUiCommandSystem` and `BuildingProductionRequestBoundary`.
   - Preserve primary/secondary/tertiary/quaternary UI behavior.
   - Expected output: UI does not call `BuildingGameplaySystem` for unit production.
   - `BuildingUiCommandSystem` now owns primary/secondary/tertiary/quaternary selected-building and building-id production button commands plus the UI production-arm command.
   - `BuildingProductionRequestBoundary` now owns active-building production request execution.
   - `BuildingUiContextSystem` wires UI production commands to fresh production request contexts and frame counts.
   - `BuildingGameplaySystem` production command wrappers now delegate through `BuildingUiCommandSystem`, and placement interaction uses the same UI command boundary.

17. Complete: Move camp item request flow
   - Move camp item affordability, required-building failure, producer focus, and arm-next-production command into `BuildingUiCommandSystem`.
   - Expected output: camp UI command result semantics remain stable but no longer depend on shell private methods.
   - `BuildingUiContextSystem` now wires `BuildingUiCommandSystem` camp request delegates directly to `BuildingProductionRequestBoundary` using fresh production request contexts.
   - `BuildingGameplaySystem` no longer owns `GetCampRequestFailure`, `TryRequestCampItem`, or `FocusLastCampProductionRequest` callbacks.
   - Existing camp UI behavior remains routed through `BuildingUiCommandSystem` while production request policy remains in `BuildingProductionRequestBoundary`.

18. Complete: Move UI read methods
   - Move selected-building health, preview prefab, `CanCreate*`, active/selected building flags, and pending/produced UI entries behind `BuildingUiQuerySystem`.
   - Expected output: menu/HUD reads from UI query/read models only.
   - `BuildingUiQuerySystem` now owns scalar selected-building UI reads and selected-building production affordability reads.
   - `BuildingUiContextSystem` wires those read delegates and production request context into the UI query context.
   - `BuildingGameplaySystem` UI read compatibility wrappers now delegate through `BuildingUiQuerySystem` instead of directly reading placement query or production request systems.

19. Complete: Move menu binding off shell
   - `BuildingGameplayCompositionSystemHelper.Result.BindMainMenu` binds narrow UI command/query systems directly.
   - Remove `mainMenu => building.BindDependencies(...)`.
   - Expected output: menu startup no longer needs the shell to bind building UI.
   - `Result.BindMainMenu` now writes the main-menu dependency into `BuildingGameplayDependencyCompositionSystemHelper` without calling `BuildingGameplaySystem.BindDependencies`.
   - `MenuStartupSystem` continues to bind `BuildingUiCommandSystem`, `BuildingUiQuerySystem`, and `BuildingPlacementInteractionSystem` from managed composition.

## Phase 6: Move Runtime Building Query And Spawn Surface

20. Complete: Move runtime building read API
   - Move runtime building ids, role filters, focus position, destroyed state, refugee settings, owner faction, wall/gate flags, combat info, approach-cell queries, and base-breach target resolution into `BuildingRuntimeQuerySystem`.
   - Expected output: AI/citizen/runtime-city callers consume `BuildingRuntimeQuerySystem.Context`.
   - `BuildingRuntimeQuerySystem` now owns base-breach target read routing through its context.
   - `BuildingRuntimeContextSystem` wires base-breach target routing to the barrier domain while exposing it as a runtime query read.
   - `BuildingGameplayCompositionSystemHelper.Result` now exposes `RuntimeQuery` and `RuntimeQueryContext`, and citizen population creation consumes those fields directly.
   - `BuildingGameplaySystem.TryResolveBaseBreachTarget` is now only a temporary compatibility wrapper over `BuildingRuntimeQuerySystem`.

21. Complete: Move runtime building spawn commands
   - Move `TrySpawnRuntimeBuilding`, initial building spawn, placement origin search, runtime wall segment spawn, wall run spawn, and runtime placement footprint queries into `BuildingRuntimeSpawnCommandSystem`, `BuildingRuntimeSpawnSystem`, and a narrow wall spawn boundary.
   - Expected output: runtime city and tests do not call shell spawn helpers.
   - `BuildingGameplayCompositionSystemHelper.Result` now exposes `RuntimeSpawnCommand` and `RuntimeSpawnCommandContext` for direct consumers.
   - `BuildingRuntimeCitySpawnSystem` now routes city building spawn through `BuildingRuntimeSpawnCommandSystem` instead of owning a separate `BuildingRuntimeSpawnSystem`.
   - `BuildingGameplaySystem` spawn wrappers remain only as temporary compatibility wrappers over `BuildingRuntimeSpawnCommandSystem` until test and production callers migrate to the composition-owned command context.

22. Complete: Move faction spawn point queries
   - Move faction production spawn point and available helipad spawn resolution into `BuildingRuntimeSpawnSystem` or `BuildingRuntimeQuerySystem`.
   - Expected output: AI production/transport spawn logic reads a narrow building runtime boundary.
   - `BuildingSpawnSystem` now owns faction production spawn-slot lookup from runtime building data.
   - `BuildingGameplaySystem.TryGetFactionProductionSpawnPoint` is now only a temporary compatibility wrapper over `BuildingSpawnSystem`.
   - Available faction helipad spawn remains routed through `BuildingSpawnSystem`.

23. Complete: Move configured unit prefab resolution
   - Move `TryResolveConfiguredUnitPrefabEntity`, `TryResolveSpawnUnitPrefab`, and live-unit preview prefab resolution into `RuntimeUnitPrefabSystem` / `BuildingSpawnPrefabSystem`.
   - Expected output: prefab registry lookup is not owned by building gameplay composition.
   - `RuntimeUnitPrefabSystem` now owns configured unit prefab entity lookup, spawn prefab reverse lookup, and live-unit preview prefab resolution.
   - `BuildingRuntimeResourcePrefabContextCompositionSystemHelper` now includes runtime building data in the runtime unit prefab context for produced-unit preview fallback.
   - `BuildingGameplaySystem` prefab methods are now only temporary compatibility wrappers over `RuntimeUnitPrefabSystem`.

24. Complete: Move initial roster/test helpers
   - Move `SpawnInitialTestRoster`, `TrySpawnInitialBuilding`, and runtime test tick helpers into editor test fixtures or narrow runtime spawn systems.
   - Expected output: production shell no longer carries test-only spawn behavior.
   - Initial roster and initial-building spawn commands remain owned by `BuildingRuntimeSpawnSystem` / `BuildingRuntimeSpawnCommandSystem`.
   - `BuildingGameplayTestHarness` now owns editor-only runtime test tick, destroyed-combat sync, barrier-door update/read, runtime entity read, destroyed-state read, and gate-rect read helpers.
   - `BuildingGameplaySystem` no longer exposes initial roster or editor-only runtime test helper methods.

## Phase 7: Move Visual, Selection, Combat, Barrier, And Redirect Surface

25. Complete: Move visual instance and positioning helpers
   - Move `CreateBuildingVisualInstance`, `PositionBuildingObject`, footprint center, transformed bounds, marker visibility, resource visuals, owner faction tint, and visual initialization into `BuildingVisualSystem` / `BuildingRuntimeVisualSystem`.
   - Expected output: GameObject visual policy is isolated.
   - Placement visual instance creation and positioning now route directly to `BuildingPlacementVisualPresentationSystemHelper` from context construction.
   - Footprint-center delegates now route directly to `BuildingPlacementGridSystem`.
   - Runtime visual initialization and marker refresh now route directly to `BuildingRuntimeVisualSystem`, and owner-faction tint remains behind `BuildingRuntimeOwnershipSystem`.
   - `BuildingGameplaySystem` no longer declares visual helper wrapper methods.

26. Complete: Move building selection and camera focus
   - Move visible selectable checks, select/focus building, clear selected building, delete selected building, and focus-world-position callbacks into `BuildingSelectionSystem`, `BuildingSelectionClickSystem`, and `SelectionUiCameraSystem`.
   - Expected output: selection does not call shell methods.
   - `BuildingSelectionSystem` now owns visible selectable checks, selected-building deletion, focus-world-position resolution, and focus/camera routing through its context.
   - Building UI and interaction context sources now call `BuildingSelectionSystem` directly for selected delete/clear and visible selectable queries instead of routing through shell helper methods.
   - `BuildingGameplaySystem` no longer declares private visible-selection, select/focus, or focus-world-position helper wrappers.

27. Complete: Move runtime destruction and entity link callbacks
   - Move destroyed runtime building cleanup, runtime building entity destroyed callbacks, blocker/combat entity link handling, and destroyed combat sync to `BuildingRuntimeEntitySystem` / `BuildingCombatSystem`.
   - Expected output: entity lifetime callbacks do not pass through the shell.
   - `BuildingRuntimeEntitySystem.Context` now carries the combat destruction boundary; selected-building delete callbacks, runtime-city delete callbacks, and runtime entity destroyed callbacks route through `BuildingRuntimeEntitySystem`.
   - `BuildingGameplaySystem` no longer exposes public/internal `DeleteBuildingById` or `HandleRuntimeBuildingEntityDestroyed` methods.

28. Complete: Move combat and blocker creation
   - Move blocker entity creation, path-blocking policy, combat entity creation, and gate friendly-pass faction update into `BuildingRuntimeEntitySystem`, `BuildingCombatSystem`, and `BuildingBarrierSystem`.
   - Expected output: ECS combat/blocker entities are created by their domain owners.
   - `BuildingRuntimeContextSystem.CreateCreationContext` now binds path-blocking, blocker creation, and combat entity creation directly to `BuildingRuntimeEntitySystem`.
   - `BuildingGameplaySystem` no longer declares private `CreateBlockerEntity`, `ShouldRuntimeBuildingBlockPathing`, or `CreateBuildingCombatEntity` wrappers.

29. Complete: Move redirect and hauler bridge calls
   - Move redirect-around-building, selected hauler order assignment, hauler approach checks, and deferred marker refresh flushing to `BuildingPlacementRedirectCompositionSystemHelper` / `BuildingResourceHaulerBridgeSystem`.
   - Expected output: resource/transport side effects do not use shell callbacks.
   - `BuildingRuntimeContextSystem.CreateCreationContext` now binds runtime creation redirect callbacks directly to `BuildingPlacementRedirectCompositionSystemHelper`.
   - `BuildingRuntimeContextSystem.CreateRuntimeQueryContext` and `CreateBarrierContext` now bind building approach checks directly to `BuildingResourceHaulerBridgeSystem`.
   - `BuildingRuntimeContextSystem.TryAssignSelectedHaulerOrders` now owns the selected-hauler bridge call used by building selection.
   - `BuildingGameplaySystem` no longer declares private `RedirectUnitsAroundPlacedBuilding`, `TryAssignSelectedHaulerOrders`, `TryGetRuntimeBuildingApproachCell(RuntimeBuildingData, ...)`, `IsRuntimeBuildingApproachCell(RuntimeBuildingData, ...)`, or `IsHaulerAtBuildingApproach` wrappers.

## Phase 8: Move Context Factories Out Of The Shell

30. Complete: Move placement context factories
   - Move placement cancel/begin/confirm/session/source/context creation into existing placement context systems.
   - Expected output: `BuildingGameplayCompositionSystemHelper` can create placement contexts without a `BuildingGameplaySystem` instance.
   - `BuildingPlacementContextCompositionSystemHelper.CreateSessionContext` now owns placement session context construction.
   - `BuildingPlacementContextCompositionSystemHelper.CreateCommandContext` now owns placement command context construction.
   - `BuildingGameplaySystem` no longer declares private `CreatePlacementCancelContext`, `CreatePlacementBeginContext`, `CreatePlacementConfirmContext`, or `CreatePlacementSessionContext` wrappers.

31. Complete: Move runtime context factories
   - Move runtime context source, runtime spawn command context, runtime query context, runtime resource prefab source, runtime entity context, runtime visual context, combat context, redirect context, barrier context, and selection contexts into owner context systems.
   - Expected output: all runtime tick and runtime city contexts are constructed without shell delegates.
   - `BuildingRuntimeContextSystem.CreateSpawnCommandContext` now owns runtime spawn command context construction.
   - `BuildingGameplayCompositionSystemHelper.Initialize` creates runtime spawn command and runtime query contexts through `BuildingRuntimeContextSystem`.
   - `BuildingGameplayCompositionSystemHelper.CreateRuntimeTickSource` creates runtime visual, combat, and barrier contexts through `BuildingRuntimeContextSystem`.
   - `BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateSource`, `BuildingSelectionSystem.CreateContext`, and `BuildingSelectionClickSystem.CreateContext` now expose owner-side construction overloads for later shell wrapper removal.

32. Complete: Move production and UI context factories
   - Move production update/request/resource-hauler context source, UI command/query context, UI context source, and interaction context source into context systems that consume explicit dependencies.
   - Expected output: production/runtime tick and menu binding no longer require shell context methods.
   - `BuildingProductionContextCompositionSystemHelper.CreateSource` now owns production source construction.
   - `BuildingUiContextSystem.CreateSource` now owns UI command/query source construction.
   - `BuildingPlacementInteractionContextCompositionSystemHelper.CreateSource` now owns interaction source construction.
   - `BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateSource` is now used by the shell wrapper instead of constructing source data directly.

33. Complete: Update runtime tick composition
   - `BuildingGameplayCompositionSystemHelper.CreateRuntimeTickSource` uses direct systems and context systems only.
   - Remove `BuildingGameplaySystem.RuntimeTickDomains`, `RuntimeInputDomains`, and all shell get/set delegates from the tick source.
   - Expected output: `BuildingRuntimeUpdateSystem` is fully independent from the shell.
   - `BuildingGameplayCompositionSystemHelper.CreateRuntimeTickSource` now accepts `BuildingGameplaySourceCompositionSystemHelper` and uses direct child systems for production tick, boundary publish, visual resource updates, destroyed-building sync, barrier doors, redirect marker flush, and input tick.
   - Removed shell runtime tick/input domain properties and tick-only shell delegates from `BuildingGameplaySystem`.
   - Runtime boundary publish now uses `BuildingGameplayEcsQueryCompositionSystemHelper` and a local composition entity-manager resolver instead of shell wrappers.

## Phase 9: Migrate Consumers And Tests

34. Complete: Migrate production consumers off `BuildingGameplaySystem`
   - Update `ManagedGameplayStartupSystem`, `GameplayFeatureStartupCompositionSystemHelper`, `MenuStartupSystem`, `GameplayRuntimeUpdateSystem`, runtime city composition, selection systems, citizen population startup, AI tests/helpers, and any remaining production caller to use composition result fields.
   - Expected output: `rg "BuildingGameplaySystem" Assets/Game -g '*.cs'` finds no production dependency except the file being retired.
   - In progress: citizen population context creation and gameplay feature binding now use composition-owned dependency/resource context systems instead of `BuildingGameplaySystem`.
   - In progress: runtime tick and runtime boundary production source creation now use composition-owned `BuildingProductionContextCompositionSystemHelper.Source` instead of `BuildingGameplaySystem.CreateBuildingProductionContextSource`.
   - In progress: composition result now exposes selection-click, runtime-city spawn, runtime-query, UI command/query, and placement-interaction systems from `BuildingGameplaySourceCompositionSystemHelper` instead of reading those systems back through `BuildingGameplaySystem`.
   - In progress: runtime visual/combat/barrier/query/production composition now uses a composition-owned `CreateRuntimeContextSource` instead of `BuildingGameplaySystem.CreateRuntimeContextSystemSource`.
   - In progress: spawn command, runtime-city spawn, and boundary spawn composition now use a composition-owned `CreateBuildingRuntimeContextSource` instead of `BuildingGameplaySystem.CreateBuildingRuntimeContextSource`.
   - In progress: selection-click composition now uses composition-owned `BuildingSelectionSystem` / `BuildingSelectionClickSystem` contexts instead of `BuildingGameplaySystem.CreateBuildingSelectionClickContext`.
   - In progress: runtime input tick composition now creates active-placement pointer and placement visual-update contexts from composition child systems instead of `BuildingGameplaySystem.CreateActivePlacementPointerContext`.
   - In progress: UI command/query composition now uses composition-owned `BuildingUiContextSystem` source construction instead of `BuildingGameplaySystem.CreateBuildingUiCommandContext` / `CreateBuildingUiQueryContext`.
   - In progress: production composition no longer constructs `BuildingGameplaySystem`; interaction and disposal contexts now compose through `BuildingPlacementInteractionContextCompositionSystemHelper`, `BuildingPlacementCommandSystem`, and `BuildingGameplayDisposalExecutionCompositionSystemHelper`.
   - Completed: `rg "BuildingGameplaySystem" Assets/Game/Scripts -g '*.cs'` now finds only `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs`.

35. Complete: Replace `BuildingGameplayTestHarness`
   - Migrate editor tests from `BuildingGameplayTestHarness : BuildingGameplaySystem` to focused fixtures around runtime spawn, runtime query, UI command/query, placement command, production, combat/barrier, and boundary publication systems.
   - Expected output: `BuildingGameplayTestHarness.cs` is deleted.
   - In progress: `BuildingRuntimeBoundaryValidationTests` now uses `BuildingGameplayCompositionSystemHelper.Result` directly for runtime tick/disposal instead of `BuildingGameplayTestHarness`.
   - In progress: `AIBuildPlannerValidationTests` and `AIProductionValidationTests` now publish runtime boundary state through a narrow runtime tick action; `AIProductionValidationTests` spawns producer buildings through `BuildingRuntimeSpawnCommandSystem`.
   - In progress: `AIEndToEndValidationTests` now uses `BuildingGameplayCompositionSystemHelper.Result` for runtime tick/disposal and no longer references `BuildingGameplayTestHarness`.
   - In progress: `InitialFactionBaseValidationTests` now uses `BuildingGameplayCompositionSystemHelper.Result`, `BuildingRuntimeSpawnCommandSystem`, and `BuildingSpawnSystem` instead of `BuildingGameplayTestHarness`; its runtime placement and helipad resolver paths pass through direct composition systems.
   - In progress: `BaseBreachValidationTests` now uses a local composition-backed fixture over `BuildingRuntimeSpawnCommandSystem`, `BuildingRuntimeQuerySystem`, `BuildingBarrierSystem`, and `BuildingCombatSystem` instead of `BuildingGameplayTestHarness`.
   - Completed: `BuildingGameplayTestHarness.cs` and `.meta` were deleted after all test callers moved to composition-backed systems, narrow runtime tick callbacks, or local fixtures.
   - Known validation gap: the full `InitialFactionBaseValidationTests` group currently has one authored-config failure because faction 1 is configured with zero units, while `SceneInitialUnitsConfig_EnablesFactionBasesWithRealPrefabsAndUnitMinimum` still requires at least five unit types for every faction.

36. Complete: Remove test helper shell dependencies
   - Replace `RuntimeGameplayStateTestHelper.SetBuildingPlacement` and `PublishBuildingRuntimeBoundary` shell parameters with narrow runtime boundary/query/context parameters.
   - Expected output: editor tests no longer type against `BuildingGameplaySystem`.
   - `RuntimeGameplayStateTestHelper` now accepts only `Action` runtime tick callbacks for building runtime boundary publication; the `BuildingGameplayTestHarness` overloads were removed.

## Phase 10: Delete Shell And Remove Debt

37. Complete: Convert `BuildingGameplaySystem` to a temporary empty adapter
   - After production and test consumers are migrated, reduce the file to no behavior or remove it immediately if no references remain.
   - This step is allowed only as a compile bridge for one step; no new logic may be added.
   - Expected output: deletion blockers are explicit and mechanical.
   - Completed by skipping the adapter bridge: production and editor consumers had already moved to narrow systems/fixtures, so the shell could be deleted directly.

38. Complete: Delete `BuildingGameplaySystem`
   - Delete `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs` and `.meta`.
   - Fix remaining compile references by routing to narrow systems.
   - Expected output: no source file named `BuildingGameplaySystem`.
   - Deleted `Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs` and `.meta`.
   - Production `rg "BuildingGameplaySystem" Assets/Game/Scripts -g '*.cs'` now returns no source references.

39. Complete: Remove architecture debt allowances
   - Remove temporary production/test allowlist entries that permitted `BuildingGameplaySystem`.
   - Remove contract wording that describes the shell as current composition.
   - Add a hard rule: `BuildingGameplaySystem.cs` and `BuildingGameplayTestHarness.cs` must not exist.
   - Expected output: architecture tests fail if either shell returns.
   - Updated the SOLID/ECS contract and focused architecture batch guard to require both files to remain deleted.

40. Complete: Validation gate
   - Run building gameplay architecture validation.
   - Run building runtime boundary tests.
   - Run building placement validation tests.
   - Run building production/resource tests.
   - Run bootstrap/menu playmode smoke.
   - Passed `GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation` in Unity batchmode.
   - Passed `BuildingRuntimeBoundaryValidationTests`, `BuildingPlacementValidationSystemTests`, `BuildingProductionSystemTests`, `FactionResourceSystemTests`, and `BaseBreachValidationTests` in Unity edit-mode validation.
   - Resource validation exposed and fixed destroyed storage being counted in `FactionResourceSystem`.
   - Run one focused runtime load/play-button smoke with buildings, initial units, production, and selection.
   - Passed `RuntimeFpsPlayButtonProbe.Run` in Unity batchmode without `-nographics`: result completed, Game button clicked, runtime initialized, AI build/production/squad logs advanced, units spawned, average sample FPS 309.0. The earlier `-nographics` pass was discarded as noisy because URP/Entities Graphics emitted render-target and package GC errors that did not reproduce with graphics enabled.
   - Expected output: compile clean, no architecture debt remains, runtime load still spawns buildings/units, building UI still binds, production still queues, and runtime FPS diagnostics show no new building tick regression.
