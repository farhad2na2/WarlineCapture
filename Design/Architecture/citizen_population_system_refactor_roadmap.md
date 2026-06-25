# CitizenPopulationSystem Refactor Roadmap

This document owns the `CitizenPopulationSystem` refactor plan. Runtime city, road build, RTS selection, and building gameplay refactors are tracked in their own roadmap documents. Keep this roadmap focused on splitting citizen population into narrow ECS-aligned systems before adding more civilian, economy, refugee, or city-life features.

## Target

Target file retired: `Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs`

Hard guard: `CitizenPopulationSystem.cs` must not exist.

Step 4 shared-data transition size: 2168 lines. Citizen enums, totals, and record data were extracted to `CitizenPopulationComponent.cs` and now live in `Assets/Game/Scripts/Components/CitizenPopulationRuntimeComponents.cs`; the then-existing shell still owns state dictionaries and behavior until later extraction steps move those owners out.

Step 5 state-storage transition size: 2139 lines. Citizen, household, visible-citizen dictionaries, id allocation, and scratch id lists now live in `CitizenPopulationStateSystem`; the then-existing shell still orchestrates record mutations until later behavior extraction steps move those owners out.

Step 6 ECS-projection transition size: 1943 lines. ECS world/query/entity handles, citizen/household entity projection, summary publication, grid config reads, and ECS entity destruction now live in `CitizenPopulationEcsProjectionSystem`; the then-existing shell still calls the projection while later behavior extraction steps move those callers out.

Step 7 totals/read-model transition size: 1895 lines. Citizen totals calculation now lives in `CitizenPopulationTotalsSystem`, and cached/public totals reads now live in `CitizenPopulationReadModelSystem`; the then-existing shell still delegates public totals calls until composition/read-model callers move off it.

Step 8 building-read transition size: 1790 lines. Runtime building query dependencies, role id caches, house id set, refresh cadence, and building query wrappers now live in `CitizenBuildingReadCompositionSystemHelper`; the then-existing shell still delegates building reads until household/refugee/schedule/danger behavior moves out.

Step 9 household-registration transition size: 1692 lines. New-house registration, removed-house synchronization, initial household/citizen creation, and initial work/shop/walk/city hall assignment now live in `CitizenHouseholdRegistrationSystem`; the then-existing shell still delegates rehousing and refugee displacement until the next steps move those policies out.

Step 10 rehousing-helper transition size: 1598 lines. Displaced-household search, rehouse-citizen mutation, living member/refugee counts, and rehousing assignment updates now live in `CitizenHouseholdRegistrationSystem`; the then-existing shell still delegates refugee-specific displacement and upkeep until step 11.

Step 11 refugee-displacement transition size: 1460 lines. Home-destroyed notification handling, displacement, nearest refugee tent selection, tent-loss replacement, refugee occupancy, assignment release, and refugee state transitions now live in `CitizenRefugeeSystem`; the then-existing shell still owns daily refugee upkeep until step 12.

Step 12 refugee-upkeep transition size: 1396 lines. Daily refugee upkeep timing, resource spending, unpaid-upkeep death behavior, and upkeep day tracking now live in `CitizenRefugeeSystem`; the then-existing shell only delegates the upkeep tick.

Step 13 schedule-policy transition size: 1213 lines. Weekday/weekend/refugee schedule constants, day/night/weekend policy, desired status selection, scheduled target selection, shop target selection, and schedule phase now live in `CitizenScheduleSystem`; the then-existing shell still runs the schedule update loop until status-transition extraction.

Step 14 status-transition transition size: 1143 lines. Status mutation, travel-status detection, desired-status travel mapping, travel-to-settled mapping, debug status mutation, arrival settling, and death status mutation now live in `CitizenStatusTransitionSystem`; the then-existing shell still coordinates schedule, danger, visible-unit side effects, and totals refresh until later extraction steps.

Step 15 danger-scanning transition size: 1015 lines. Danger source scan cadence, scene transform name scanning, danger radius checks, safe-building checks, flee target selection, and nearest-safe-building search now live in `CitizenDangerCompositionSystemHelper`; the transform scan remains temporary performance debt until authored danger markers or ECS events replace it.

Step 16 travel-grid transition size: 786 lines. World-to-cell conversion, travel origin selection, citizen reference anchors, visibility anchors, building approach world/cell resolution, long segment goal planning, deferred travel duration, and citizen world-position offsets now live in `CitizenTravelSystem`; the then-existing shell still coordinates visible unit spawning until visible-unit extraction.

Step 17 movement-command transition size: 755 lines. Civilian move-order component mutation now lives in `CitizenMovementCommandSystem`, including combat/path cleanup, target assignment, path request creation/removal, and manual move tagging; the then-existing shell still decides when to issue those commands until visible-unit sync extraction.

Step 18 prefab-selection transition size: 719 lines. Male/female visible citizen prefab loading, per-citizen prefab choice, and prefab selection reset now live in `CitizenPrefabSelectionSystem`; entity prefab resolution remains in `CitizenPrefabSystem`.

Step 19 visible-spawn transition size: 642 lines. Visible citizen clear/remove/spawn, civilian entity instantiation, spawn-cell placement, LocalTransform/previous-position setup, grid initialization cleanup, idle/combat disabling, civilian faction assignment, selection/path cleanup, CivilianUnitTag assignment, and visible-record creation now live in `CitizenVisibleUnitPresentationSystemHelper`; the then-existing shell still owns the visible sync loop until step 20.

Step 20 visible-travel transition size: 528 lines. Visible citizen spawn/despawn visibility decisions, destroyed-unit handling, missing-transform cleanup, path retry, long-distance move handoff, segment-reached move retargeting, building approach arrival checks, final distance arrival checks, and visible arrival resolution now live in `CitizenVisibleUnitPresentationSystemHelper`; the shell delegates the visible sync tick.

Step 21 event-boundary transition size: 520 lines. Visible-citizen-destroyed and home-building-destroyed notifications now live in `CitizenPopulationEventSystem`; `CitizenVisualLifecycleReporter` and `BuildingGameplayDependencyCompositionSystemHelper` bind the event boundary instead of calling `CitizenPopulationSystem`.

Step 22 debug-surface transition size: 485 lines. Citizen debug snapshot generation, debug status mutation, and debug kill command routing now live in `CitizenPopulationDebugSystem`; the shell retains temporary public debug wrappers until UI/menu callers move to the debug boundary.

Step 23 diagnostics transition size: 458 lines. Citizen population phase timing, pathfinding-skip flagging, slow-frame threshold policy, monotonic timing clamp, and `[CitizenPopulationDiag]` log formatting now live in `CitizenPopulationDiagnosticSystem`; the shell only marks phase boundaries.

Step 24 lifecycle transition size: 407 lines. Citizen population update intervals, next-run timestamps, pathfinding-pending skip policy, phase ordering, forced building refresh before logical updates, visible sync cadence, totals refresh cadence, and diagnostic phase sequencing now live in `CitizenPopulationLifecycleSystem`; the shell delegates one runtime tick to the lifecycle boundary.

Step 25 composition transition size: 359 lines. Citizen subsystem construction, runtime context storage, initial wiring, event boundary binding, init-time reset policy, and disposal/reset sequencing now live in `CitizenPopulationCompositionSystemHelper`; the shell keeps one composition result while remaining behavior migrates out.

Step 26 managed-startup composition transition size: 363 lines. `BuildingGameplayCompositionSystemHelper` now creates and stores the explicit `CitizenPopulationCompositionSystemHelper.Result`, and exposes the composition result through `ManagedGameplayStartupSystem.Result` for later runtime/read/event migration.

Step 27 runtime-update transition size: 76 lines. Citizen population runtime update orchestration, logical citizen tick callbacks, visible citizen sync, totals refresh callbacks, record storage callbacks, and death handling now live in `CitizenPopulationRuntimeUpdateSystem`; `GameplayRuntimeUpdateSystem` receives the composition-sourced runtime update action instead of `CitizenPopulationSystem`.

Step 28 menu-read transition size: 76 lines. Menu startup and `MenuView` no longer receive/store `CitizenPopulationSystem` for population stats reads; stats now read civilian deaths through the composition-provided `CitizenPopulationReadModelSystem`.

Step 29 event-coupling transition size: 76 lines. Building gameplay feature binding, gameplay feature startup, managed startup, and bootstrap now pass `CitizenPopulationEventSystem` directly instead of passing `CitizenPopulationSystem` just to reach `EventSystem`.

Step 30 visual-reporter transition size: 76 lines. `CitizenVisualLifecycleReporter` is guarded as an event-boundary consumer: it binds `CitizenPopulationEventSystem` directly and has no broad `CitizenPopulationSystem` reference.

Step 31 shell deletion transition size: 0 lines. `CitizenPopulationSystem.cs` and its `.meta` were deleted; managed startup now initializes/disposes `CitizenPopulationCompositionSystemHelper` directly, and bootstrap stores only the narrow runtime update/read/event/dispose boundaries.

Step 32 architecture allowance cleanup: temporary public-surface allowlists, broad-shell access allowlists, and line-count guards were removed. The contract now states the deleted shell must not return.

Goal: retire the broad managed `CitizenPopulationSystem` shell by moving citizen records, household records, ECS projection, building-read caches, schedule policy, refugee/displacement logic, danger/fleeing logic, visible civilian unit sync, resource upkeep, totals/read models, debug commands, and lifecycle orchestration into narrow systems. The final state should have no production source file named `CitizenPopulationSystem.cs`; callers should depend on explicit citizen composition, read, command, debug, and event boundaries.

## Current Responsibility Inventory

- Managed composition and lifecycle: subsystem construction, runtime context storage, initial wiring, event boundary binding, init-time reset policy, and disposal/reset sequencing moved to `CitizenPopulationCompositionSystemHelper`. Update cadence, pathfinding skip behavior, forced building refresh, visible sync cadence, totals cadence, and diagnostic phase sequencing moved to `CitizenPopulationLifecycleSystem`.
- ECS query and entity projection: moved to `CitizenPopulationEcsProjectionSystem`.
- Citizen and household record storage: state storage moved to `CitizenPopulationStateSystem`.
- Runtime building read cache: moved to `CitizenBuildingReadCompositionSystemHelper`.
- Household registration and rehousing: new-house registration, removed-house synchronization, displaced-household search, rehouse-citizen mutation, assignment updates, and living member/refugee counts moved to `CitizenHouseholdRegistrationSystem`.
- Refugee and displacement behavior: home destruction, missing-home displacement, refugee tent assignment, tent loss, occupancy, assignment release, refugee state transitions, and daily upkeep moved to `CitizenRefugeeSystem`.
- Schedule policy: weekday/weekend/refugee timing constants, day-of-week logic, night policy, shopping cadence, work/lunch/walk/city hall decisions, target resolution, and schedule phase moved to `CitizenScheduleSystem`.
- Status transition policy: status mutation, travel-status detection, desired-status travel mapping, travel-to-settled mapping, debug status mutation, arrival settling, and death status mutation moved to `CitizenStatusTransitionSystem`.
- Danger/fleeing behavior: scene transform name scanning for fire/burn/smoke/explosion, danger position cache, danger radius checks, safe-building evaluation, and flee target selection moved to `CitizenDangerCompositionSystemHelper`; this transform scan is recorded as temporary performance debt.
- Visible civilian ECS units: visible spawn/despawn decisions, destroyed-unit handling, path retry, long-distance move handoff, segment-reached retargeting, visible arrival resolution, clear/remove/spawn, and spawn-time component setup moved to `CitizenVisibleUnitPresentationSystemHelper`; visible citizen prefab selection moved to `CitizenPrefabSelectionSystem`, and move-order component mutation moved to `CitizenMovementCommandSystem`.
- Movement and path goal helpers: world-to-cell conversion, travel origin selection, citizen reference anchors, visibility anchors, building approach world/cell resolution, long segment goal planning, deferred travel duration, and citizen world-position offsets moved to `CitizenTravelSystem`.
- Totals/read model: calculation moved to `CitizenPopulationTotalsSystem`, and cached/public totals reads moved to `CitizenPopulationReadModelSystem`.
- Debug surface: citizen debug snapshot generation, debug status mutation, and debug kill command routing moved to `CitizenPopulationDebugSystem`.
- Event surface: visible-citizen-destroyed and home-building-destroyed notifications moved to `CitizenPopulationEventSystem`.
- Diagnostics: phase timing, pathfinding-skip flagging, slow-frame threshold policy, monotonic timing clamp, and log formatting moved to `CitizenPopulationDiagnosticSystem`.
- Lifecycle tick: update intervals, next-run timestamps, pathfinding-pending skip policy, forced building refresh before logical updates, phase ordering, visible sync cadence, and totals refresh cadence moved to `CitizenPopulationLifecycleSystem`.
- Composition: extracted citizen subsystem construction, context storage, init wiring, event boundary binding, and disposal/reset sequencing to `CitizenPopulationCompositionSystemHelper`.
- Startup composition handoff: `BuildingGameplayCompositionSystemHelper` creates/stores the explicit citizen composition result and `ManagedGameplayStartupSystem.Result` carries it forward for runtime/read/event boundaries.
- Runtime update: logical citizen tick callbacks, visible citizen sync, totals refresh callbacks, record storage callbacks, death handling, and lifecycle update invocation moved to `CitizenPopulationRuntimeUpdateSystem`; `GameplayRuntimeUpdateSystem` invokes a composition-sourced runtime update action.
- Menu/UI reads: population stats reads moved to `CitizenPopulationReadModelSystem`; `MenuStartupSystem` and `MenuView` no longer receive/store the broad shell for stats reads.
- Building/citizen event coupling: building gameplay feature binding now receives `CitizenPopulationEventSystem` directly from composition instead of passing the broad shell to reach its event boundary.
- Visual reporter coupling: `CitizenVisualLifecycleReporter` binds `CitizenPopulationEventSystem` directly and is guarded against broad shell references.

## Retired Shell Guard

The broad shell has been deleted. Do not add a source file named `CitizenPopulationSystem.cs`, and do not add replacement broad shells such as `CitizenPopulationManager`, `CitizenPopulationFacade`, or `CitizenPopulationController`. New citizen behavior belongs in a narrow citizen `*System`, ECS component/buffer, command boundary, read model, or debug boundary.

## Non-Goals

- Do not change civilian gameplay rules, schedule balance, refugee capacity/upkeep behavior, visibility distances, danger radius, or movement behavior while refactoring.
- Do not add new citizen features during the refactor.
- Do not move citizen logic into `GameBootstrap`, UI views, runtime city, road build, building gameplay, or selection systems.
- Do not introduce singleton/static gameplay access. Static helpers are acceptable only for pure data/math or immutable constants.
- Do not use reflection, service locators, hidden globals, or broad replacement shells.
- Do not rename existing serialized assets or citizen component names unless a separate data migration plan exists.
- Do not store long-lived `DynamicBuffer`, `EntityQuery`, or `EntityManager` handles across structural changes without a narrow owner that reacquires them safely.

## Completion Definition

- No production source file named `CitizenPopulationSystem.cs`.
- No production code constructs, stores, or type-references `CitizenPopulationSystem`.
- Citizen composition is explicit and narrow, with separate systems for lifecycle, state storage, ECS projection, building reads, scheduling, refugee behavior, danger, visible unit sync, movement commands, totals, diagnostics, debug, and event handling.
- Existing callers consume `CitizenPopulationCompositionSystemHelper` or narrower boundaries.
- `CitizenVisualLifecycleReporter` no longer binds the broad shell; it publishes into a citizen event/command boundary.
- `BuildingGameplayDependencyCompositionSystemHelper` no longer stores the broad shell; building destruction notifications route to a citizen event system.
- `GameplayRuntimeUpdateSystem` no longer calls `CitizenPopulationSystem.Update`; it calls a narrow citizen runtime update boundary.
- Architecture tests hard-fail if `CitizenPopulationSystem.cs` returns.
- Focused validation passes: citizen architecture batch, citizen component/projection tests, schedule/refugee tests, visible citizen spawn/move tests, bootstrap/menu play-button smoke, and one runtime load validation with citizen population enabled.

## Planned Boundaries

- `CitizenPopulationCompositionSystemHelper`: constructs and wires narrow citizen systems; owns only composition, contexts, update delegation, disposal delegation, and public result fields.
- `CitizenPopulationLifecycleSystem`: owns runtime update cadence, pathfinding-pending skip policy, forced building refresh, phase sequencing, and timed totals/visible sync gates.
- `CitizenPopulationRuntimeUpdateSystem`: owns citizen runtime update orchestration, lifecycle callbacks, record storage callbacks, visible sync handoff, totals refresh callbacks, and death handling.
- `CitizenPopulationStateSystem`: owns citizen/household records, ids, dictionaries, visible-citizen records, scratch collections, and pure record mutation.
- `CitizenPopulationEcsProjectionSystem`: owns ECS world/query/entity creation, component synchronization, summary entity publication, and entity destruction.
- `CitizenBuildingReadCompositionSystemHelper`: owns runtime building role caches and building read delegates against `BuildingRuntimeReadModelCompositionSystemHelper`.
- `CitizenHouseholdRegistrationSystem`: owns new house registration, target assignment, rehousing, removed-house detection, and household-to-home mapping policies.
- `CitizenRefugeeSystem`: owns displacement, refugee tent assignment, tent loss, occupancy counting, upkeep charging, and refugee death policy.
- `CitizenScheduleSystem`: owns schedule constants, day/hour policy, desired status/target selection, travel status conversion, and arrival settling policy.
- `CitizenDangerCompositionSystemHelper`: owns danger source scanning, danger source cache, safe-building checks, and flee target selection.
- `CitizenTravelSystem`: owns travel-origin policy, world/grid conversion, approach-cell goal resolution, segment goal planning, deferred travel duration, and world-position offsets.
- `CitizenVisibleUnitPresentationSystemHelper`: owns visible citizen spawn/despawn, prefab selection, instantiated unit component setup, path retry, visible arrival checks, and visible unit record mutation.
- `CitizenMovementCommandSystem`: owns ECS component mutations for civilian move orders.
- `CitizenPopulationTotalsSystem`: owns totals calculation, summary publication, and read model projection.
- `CitizenPopulationDebugSystem`: owns debug snapshot/status/kill commands.
- `CitizenPopulationEventSystem`: owns external events such as home destroyed and visible unit destroyed.
- `CitizenPopulationDiagnosticSystem`: owns phase timing and slow-frame diagnostics.
- `CitizenPopulationReadModelSystem`: owns public totals/read-only state consumed by UI/menu/HUD.

## Refactor Steps

1. Complete: Add architecture roadmap and baseline guard
   - Add this document to architecture checks.
   - Record the 2254-line baseline.
   - Add a focused `RunCitizenPopulationArchitectureBatchValidation` entry point.
   - Guard against new broad-shell public members, direct singleton/static access, and new peer dependencies on `CitizenPopulationSystem`.
   - Added `GameplayArchitectureContractTests.RunCitizenPopulationArchitectureBatchValidation`.
   - Added guards for roadmap tracking, baseline line count, planned boundary inventory, bounded production references, and singleton access prevention.

2. Complete: Add final deletion contract
   - Update `gameplay_solid_ecs_contract.md` with the final target: `CitizenPopulationSystem.cs` must be deleted at the end of this roadmap.
   - Define allowed temporary debt explicitly: broad shell may exist only while listed extraction steps are incomplete.
   - Add hard wording that no replacement broad `CitizenPopulationManager`, `CitizenPopulationFacade`, or similar shell is allowed.
   - Added final deletion target wording to `gameplay_solid_ecs_contract.md`.
   - Added `CitizenPopulationDeletionTargetContractMustBeExplicit` to the focused architecture validation batch.

3. Complete: Freeze public surface inventory
   - Add architecture tests for the current public surface listed in this document.
   - Assign every public member to a target owner.
   - Prevent adding new public/internal methods to the shell during the refactor.
   - Added a temporary public-surface freeze guard to the focused architecture validation batch during extraction.
   - That temporary guard was retired in step 32 after the shell was deleted.

4. Complete: Extract shared citizen data contracts
   - Move `CitizenGender`, `CitizenLifeState`, `CitizenStatus`, `CitizenPopulationTotals`, citizen record, household record, and visible citizen record into narrow data files.
   - Keep names ECS-aligned: `CitizenPopulationComponent` / `CitizenPopulationSystem` style, not manager/session/builder.
   - Do not change serialized ECS component names.
   - Added `CitizenPopulationComponent.cs`; later moved to `Assets/Game/Scripts/Components/CitizenPopulationRuntimeComponents.cs`.
   - Moved `CitizenGender`, `CitizenLifeState`, `CitizenStatus`, `CitizenPopulationTotals`, `CitizenRecordComponent`, `CitizenHouseholdRecordComponent`, and `VisibleCitizenComponent` out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationSharedDataContractsMustLiveOutsideShell` to the focused architecture validation batch.

5. Complete: Extract citizen population state storage
   - Create `CitizenPopulationStateSystem`.
   - Move id allocation, dictionaries, home-household mapping, visible-citizen record map, scratch lists, and record store/get helpers.
   - Expected output: the shell no longer owns citizen/household/visible record collections.
   - Added `CitizenPopulationStateSystem.cs`.
   - Moved citizen/household/visible record dictionaries, home-household mapping, id allocation, scratch id lists, and record store/get helpers behind `_state`.
   - Added `CitizenPopulationStateSystemMustOwnCitizenRecordStorage` to the focused architecture validation batch.

6. Complete: Extract ECS projection and query ownership
   - Create `CitizenPopulationEcsProjectionSystem`.
   - Move world/entity manager resolution, citizen/household/grid queries, summary entity creation, component sync, entity destruction, ECS counts, and ECS summary reads.
   - Ensure all ECS lookups reacquire safely after structural changes.
   - Added `CitizenPopulationEcsProjectionSystem.cs`.
   - Moved ECS world/entity/query handles, citizen/household entity creation, component synchronization, summary entity publication, grid config reads, entity destruction, and ECS counts out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationEcsProjectionSystemMustOwnEntityProjection` to the focused architecture validation batch.

7. Complete: Extract population totals/read model
   - Create `CitizenPopulationTotalsSystem` and `CitizenPopulationReadModelSystem`.
   - Move totals calculation, summary publication, `Totals`, and `GetTotals`.
   - Publish totals through a read model instead of broad shell fields.
   - Added `CitizenPopulationTotalsSystem.cs`.
   - Added `CitizenPopulationReadModelSystem.cs`.
   - Moved cached totals state, totals calculation, summary publication call, and `GetTotals` backing reads out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationTotalsAndReadModelMustLiveOutsideShell` to the focused architecture validation batch.

8. Complete: Extract runtime building read cache
   - Create `CitizenBuildingReadCompositionSystemHelper`.
   - Move role-list refresh cadence, house/shop/city hall/refugee tent/military camp lists, role id sets, building focus/destroyed/refugee/approach reads, and nearest-building helpers.
   - Keep all building reads behind `BuildingRuntimeReadModelCompositionSystemHelper`; no building placement facade access.
   - Added `CitizenBuildingReadCompositionSystemHelper.cs`.
   - Moved `BuildingRuntimeReadModelCompositionSystemHelper` dependency storage, role id caches, house id set, refresh cadence, focus/destroyed/refugee/approach query wrappers, and role list read accessors out of `CitizenPopulationSystem.cs`.
   - Added `CitizenBuildingReadSystemMustOwnRuntimeBuildingCache` to the focused architecture validation batch.

9. Complete: Extract household registration
   - Create `CitizenHouseholdRegistrationSystem`.
   - Move new-house detection, household creation, male/female citizen creation, work/shop/lunch/walk/city hall target assignment, removed-house sync, and household rehome candidate selection.
   - Expected output: house lifecycle behavior is isolated from schedule/visible-unit behavior.
   - Added `CitizenHouseholdRegistrationSystem.cs`.
   - Moved removed-house synchronization, new-house detection, household/citizen id allocation, initial household creation, male/female citizen creation, and initial building assignment out of `CitizenPopulationSystem.cs`.
   - Added `CitizenHouseholdRegistrationSystemMustOwnNewHouseRegistration` to the focused architecture validation batch.

10. Complete: Extract rehousing and assignment helpers
   - Move rehouse-citizen mutation, displaced-household search, household reference position, living member counts, and assignment updates into the household/refugee boundary.
   - Keep record writes routed through `CitizenPopulationStateSystem`.
   - Moved displaced-household search, rehouse-citizen mutation, assignment updates, living member counts, refugee member counts, and alive/awaiting-rehousing checks into `CitizenHouseholdRegistrationSystem.cs`.
   - Added `CitizenHouseholdRegistrationSystemMustOwnRehousingHelpers` to the focused architecture validation batch.

11. Complete: Extract refugee displacement behavior
   - Create `CitizenRefugeeSystem`.
   - Move `NotifyHomeBuildingDestroyed` handling, displacement, refugee tent assignment, tent loss, refugee occupancy, assignment release, and refugee state transitions.
   - External home destruction should become an event into `CitizenPopulationEventSystem`, not a broad shell method.
   - Added `CitizenRefugeeSystem.cs`.
   - Moved home-destroyed notification behavior, missing-home displacement calls, nearest refugee tent selection, tent-loss replacement, refugee occupancy, assignment release, and refugee state transitions out of `CitizenPopulationSystem.cs`.
   - Added `CitizenRefugeeSystemMustOwnDisplacementAndTentState` to the focused architecture validation batch.

12. Complete: Extract refugee upkeep
   - Move daily upkeep charge timing, `CitizenResourceSystem.TrySpendDollars` usage, unpaid-upkeep death behavior, and upkeep day tracking into `CitizenRefugeeSystem`.
   - Keep resource spending through `CitizenResourceSystem` context only.
   - Moved refugee upkeep day tracking, resource-context checks, daily upkeep cost calculation, dollar spending, unpaid-upkeep death behavior, and assignment release on failed upkeep into `CitizenRefugeeSystem.cs`.
   - Added `CitizenRefugeeSystemMustOwnUpkeep` to the focused architecture validation batch.

13. Complete: Extract citizen schedule policy
   - Create `CitizenScheduleSystem`.
   - Move weekday/weekend/refugee time constants, day-of-week, night policy, schedule phase, desired status, scheduled target, shop target, and weekday shopping policy.
   - Make this boundary mostly pure-data and easy to test.
   - Added `CitizenScheduleSystem.cs`.
   - Moved weekday/weekend/refugee schedule constants, day-of-week, night policy, schedule phase, desired status selection, scheduled target selection, shop target resolution, and weekday shopping cadence out of `CitizenPopulationSystem.cs`.
   - Added `CitizenScheduleSystemMustOwnSchedulePolicy` to the focused architecture validation batch.

14. Complete: Extract status transition policy
   - Move `SetCitizenStatus`, travel-status checks, travel-to-settled mapping, desired-status-to-travel mapping, arrival resolution, dead marking policy, and status debug mutation helpers.
   - Route all record writes through `CitizenPopulationStateSystem`.
   - Added `CitizenStatusTransitionSystem.cs`.
   - Moved status mutation, travel-status checks, desired-status travel mapping, travel-to-settled mapping, debug status mutation, arrival resolution, and death status mutation out of `CitizenPopulationSystem.cs`.
   - Added `CitizenStatusTransitionSystemMustOwnStatusTransitions` to the focused architecture validation batch.

15. Complete: Extract danger source scanning
   - Create `CitizenDangerCompositionSystemHelper`.
   - Move danger source cache, scan interval, transform scanning, name filters, danger radius, safe-building checks, flee target selection, and nearest-safe-building policy.
   - Record the current scene-transform scan as temporary performance debt; later replacement should use authored danger markers or ECS events.
   - Added `CitizenDangerCompositionSystemHelper.cs`.
   - Moved danger source cache, scan interval, transform name scanning, danger radius, safe-building checks, flee target selection, and nearest-safe-building search out of `CitizenPopulationSystem.cs`.
   - Added `CitizenDangerSystemMustOwnDangerScanning` to the focused architecture validation batch.

16. Complete: Extract travel and grid goal planning
   - Create `CitizenTravelSystem`.
   - Move world-to-cell conversion, travel origin policy, anchor position resolution, building approach world/cell helpers, segment goal planning, deferred travel duration, and citizen offset positioning.
   - The system may depend on `CitizenBuildingReadCompositionSystemHelper`, `CitizenPopulationEcsProjectionSystem`, and `CitizenPopulationStateSystem`, but must not own visible unit spawning.
   - Added `CitizenTravelSystem.cs`.
   - Moved world-to-cell conversion, travel origin selection, citizen/household reference anchors, visibility anchors, building approach world/cell resolution, long segment goal planning, deferred travel duration, and citizen world-position offsets out of `CitizenPopulationSystem.cs`.
   - Added `CitizenTravelSystemMustOwnTravelAndGridPlanning` to the focused architecture validation batch.

17. Complete: Extract movement command mutation
   - Create `CitizenMovementCommandSystem`.
   - Move ECS mutations for `EngageTarget`, `UnitPathFollow`, `UnitPathRange`, `AutoWanderMoveTag`, `UnitTarget`, `UnitPathRequest`, and `ManualMoveOrderTag`.
   - Add tests that command writes do not leave combat/path components in conflicting states.
   - Added `CitizenMovementCommandSystem.cs`.
   - Moved civilian move-order component mutation for combat/path cleanup, target assignment, path request creation/removal, and manual move tagging out of `CitizenPopulationSystem.cs`.
   - Added `CitizenMovementCommandSystemMustOwnMoveCommandMutation` to the focused architecture validation batch.

18. Complete: Extract visible citizen prefab selection
   - Create `CitizenPrefabSelectionSystem` or extend `CitizenPrefabSystem` if the existing boundary fits.
   - Move male/female prefab loading, per-citizen prefab selection, and configured prefab resolution usage.
   - Keep entity prefab resolution in `CitizenPrefabSystem`.
   - Added `CitizenPrefabSelectionSystem.cs`.
   - Moved male/female prefab loading, per-citizen prefab selection, and prefab-selection reset out of `CitizenPopulationSystem.cs`.
   - Kept configured prefab entity resolution in `CitizenPrefabSystem`.
   - Added `CitizenPrefabSelectionSystemMustOwnVisibleCitizenPrefabSelection` to the focused architecture validation batch.

19. Complete: Extract visible citizen spawn setup
   - Create `CitizenVisibleUnitPresentationSystemHelper`.
   - Move visibility distance checks, spawn/despawn decisions, civilian entity instantiation, `UnitGrid`, `LocalTransform`, `UnitPrevWorldPos`, movement behavior, combat disabling, faction assignment, `CivilianUnitTag`, selection clearing, and target/path component cleanup.
   - Added `CitizenVisibleUnitPresentationSystemHelper.cs`.
   - Moved visible citizen clear/remove/spawn, civilian entity instantiation, spawn-cell placement, LocalTransform/previous-position setup, grid initialization cleanup, idle/combat disabling, civilian faction assignment, selection/path cleanup, `CivilianUnitTag` assignment, and visible-record creation out of `CitizenPopulationSystem.cs`.
   - Visibility distance decisions remain in the shell until the visible sync loop moves in step 20.
   - Added `CitizenVisibleUnitPresentationSystemHelperMustOwnVisibleSpawnSetup` to the focused architecture validation batch.

20. Complete: Extract visible citizen travel sync
   - Move path retry, long-distance move handoff, segment-reached checks, building approach arrival checks, visible unit destroyed checks, and visible arrival resolution into `CitizenVisibleUnitPresentationSystemHelper`.
   - Keep actual move command component writes delegated to `CitizenMovementCommandSystem`.
   - Moved visible citizen spawn/despawn visibility decisions, destroyed-unit handling, missing-transform cleanup, path retry, long-distance move handoff, segment-reached move retargeting, building approach arrival checks, final distance arrival checks, and visible arrival resolution out of `CitizenPopulationSystem.cs`.
   - Kept move-order component writes delegated to `CitizenMovementCommandSystem`.
   - Added `CitizenVisibleUnitPresentationSystemHelperMustOwnVisibleTravelSync` to the focused architecture validation batch.

21. Complete: Extract visible citizen lifecycle events
   - Create `CitizenPopulationEventSystem`.
   - Move `NotifyVisibleCitizenDestroyed` and `NotifyHomeBuildingDestroyed` into explicit event commands.
   - Update `CitizenVisualLifecycleReporter` and building destruction dependencies to bind this event boundary instead of `CitizenPopulationSystem`.
   - Added `CitizenPopulationEventSystem.cs`.
   - Moved visible-citizen-destroyed and home-building-destroyed notifications out of `CitizenPopulationSystem.cs`.
   - Updated `CitizenVisualLifecycleReporter` to bind `CitizenPopulationEventSystem`.
   - Updated `BuildingGameplayDependencyCompositionSystemHelper` to store and notify `CitizenPopulationEventSystem`.
   - Added `CitizenPopulationEventSystemMustOwnExternalCitizenEvents` to the focused architecture validation batch.

22. Complete: Extract debug command/read surface
   - Create `CitizenPopulationDebugSystem`.
   - Move `TryGetCitizenDebugSnapshot`, `TrySetCitizenStatusForDebug`, and `TryKillCitizenForDebug`.
   - Debug reads may inspect ECS projection through a narrow context but must not reach into all systems directly.
   - Added `CitizenPopulationDebugSystem.cs`.
   - Moved citizen debug snapshot generation, debug status mutation, and debug kill command routing out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationDebugSystemMustOwnDebugSurface` to the focused architecture validation batch.

23. Complete: Extract diagnostics
   - Create `CitizenPopulationDiagnosticSystem`.
   - Move phase timing, slow-frame threshold, diagnostic enable policy, and log formatting.
   - Replace direct `Debug.Log` formatting in update with a diagnostic boundary.
   - Added `CitizenPopulationDiagnosticSystem.cs`.
   - Moved citizen population phase timing, pathfinding-skip flagging, slow-frame threshold policy, monotonic timing clamp, and `[CitizenPopulationDiag]` log formatting out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationDiagnosticSystemMustOwnUpdateDiagnostics` to the focused architecture validation batch.

24. Complete: Extract lifecycle update sequencing
   - Create `CitizenPopulationLifecycleSystem`.
   - Move update intervals, next-run timestamps, pathfinding pending skip policy, update phase ordering, and force-refresh behavior.
   - Expected output: the then-existing shell `Update` delegates to a lifecycle boundary with explicit contexts.
   - Added `CitizenPopulationLifecycleSystem.cs`.
   - Moved update intervals, next-run timestamps, pathfinding-pending skip policy, forced building refresh before logical updates, phase ordering, visible sync cadence, totals refresh cadence, and diagnostic phase sequencing out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationLifecycleSystemMustOwnUpdateSequencing` to the focused architecture validation batch.

25. Complete: Create citizen population composition
   - Create `CitizenPopulationCompositionSystemHelper`.
   - Own construction/wiring of all extracted citizen systems and contexts.
   - Return explicit result fields: update boundary, read model, debug system/context, event system/context, dispose action.
   - Added `CitizenPopulationCompositionSystemHelper.cs`.
   - Moved citizen subsystem construction, runtime context storage, initial wiring, event boundary binding, init-time reset policy, and disposal/reset sequencing out of `CitizenPopulationSystem.cs`.
   - Added `CitizenPopulationCompositionSystemHelperMustOwnCitizenSystemWiring` to the focused architecture validation batch.
   - Do not hide gameplay policy inside composition.

26. Complete: Migrate managed startup to citizen composition
   - Update `ManagedGameplayStartupSystem` and building gameplay composition flow so startup creates `CitizenPopulationCompositionSystemHelper` or receives it from a higher composition boundary.
   - Keep building runtime query, day/night, camera, resource context, and prefab context explicit.
   - `BuildingGameplayCompositionSystemHelper.Result` now owns a `CitizenPopulationCompositionSystemHelper.Result`.
   - Later deletion removed the transitional shell handoff; composition now initializes `CitizenPopulationCompositionSystemHelper` directly.
   - `ManagedGameplayStartupSystem.Result` exposes the citizen composition result for later runtime/read/event migration.
   - Added `CitizenPopulationManagedStartupMustCreateComposition` to the focused architecture validation batch.

27. Complete: Migrate runtime update off the broad shell
   - Update `GameplayRuntimeUpdateSystem` to call `CitizenPopulationLifecycleSystem` through citizen composition result, not `CitizenPopulationSystem.Update`.
   - Keep performance diagnostics step name stable as `CitizenPopulation` unless intentionally renamed.
   - Added `CitizenPopulationRuntimeUpdateSystem.cs`.
   - Moved logical citizen tick callbacks, visible citizen sync, totals refresh callbacks, record storage callbacks, and death handling out of `CitizenPopulationSystem.cs`.
   - `GameplayRuntimeUpdateSystem` now receives an `Action` sourced from `ManagedGameplayStartupSystem.Result.CitizenPopulationComposition.RuntimeUpdateSystem.Update`.
   - Added `CitizenPopulationRuntimeUpdateMustUseCompositionBoundary` to the focused architecture validation batch.

28. Complete: Migrate menu/UI reads off the broad shell
   - Update `MenuStartupSystem`, menu/HUD consumers, and any population display code to consume `CitizenPopulationReadModelSystem` or totals context.
   - Remove direct `CitizenPopulationSystem` parameters from UI startup once callers are migrated.
   - `MenuStartupSystem` now passes `CitizenPopulationReadModelSystem` into `MenuView`.
   - `MenuView` stores the read model and reads stats civilian deaths from `CitizenPopulationReadModelSystem.Totals`.
   - `GameBootstrap` stores the composition read model for menu startup.
   - Added `CitizenPopulationMenuReadsMustUseReadModelBoundary` to the focused architecture validation batch.

29. Complete: Migrate building/citizen event coupling
   - Update `BuildingGameplayDependencyCompositionSystemHelper` so home-building destroyed notifications route to `CitizenPopulationEventSystem`.
   - Remove `CitizenPopulationSystem` storage from building gameplay dependencies.
   - Building gameplay feature binding now accepts `CitizenPopulationEventSystem` directly.
   - `GameplayFeatureStartupCompositionSystemHelper` and `GameBootstrap` pass the composition event boundary instead of the broad shell.
   - Added `CitizenPopulationBuildingEventCouplingMustUseEventBoundary` to the focused architecture validation batch.

30. Complete: Migrate visual reporter coupling
   - Update `CitizenVisualLifecycleReporter` to bind `CitizenPopulationEventSystem` or a narrow visible-unit event command.
   - Remove broad shell references from visual lifecycle components.
   - `CitizenVisualLifecycleReporter` already binds `CitizenPopulationEventSystem` directly.
   - Added `CitizenPopulationVisualReporterMustUseEventBoundary` to the focused architecture validation batch.

31. Complete: Delete the broad shell
   - Delete `Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs` and `.meta`.
   - Fix all production and test references.
   - Expected output: no production or test source references `CitizenPopulationSystem`.
   - `BuildingGameplayCompositionSystemHelper` initializes and disposes `CitizenPopulationCompositionSystemHelper` directly.
   - `ManagedGameplayStartupSystem` and `GameBootstrap` no longer construct, store, or expose the broad shell.
   - Added `CitizenPopulationShellMustBeDeleted` to the focused architecture validation batch.

32. Complete: Remove temporary architecture allowances
   - Remove public-surface allowlists, temporary broad-shell wording, line-count allowances, and any contract entries that assume `CitizenPopulationSystem` exists.
   - Add hard guard: `CitizenPopulationSystem.cs` must not exist.
   - Removed stale citizen public-surface, line-count, and production-reference allowlist tests.
   - Updated `gameplay_solid_ecs_contract.md` to make shell deletion the hard rule.

33. Pending: Validation gate
   - Run focused architecture batch: `GameplayArchitectureContractTests.RunCitizenPopulationArchitectureBatchValidation`.
   - Run citizen projection/state tests, schedule tests, refugee/displacement tests, visible unit spawn/move tests, and totals/read model tests.
   - Run bootstrap/menu play-button smoke.
   - Run one focused runtime load validation with citizen population enabled.
   - Expected result: compile clean, no architecture allowlist debt remains, citizens still spawn from houses, visible civilians move/arrive/despawn, home destruction still displaces households, refugee upkeep still charges/kills correctly, and UI totals still update.

## Progress Notes

- Step 1 complete: roadmap and baseline architecture guard are in place.
- Step 2 complete: final deletion contract wording and validation guard are in place.
- Step 3 complete: public surface inventory and owner assignments are guarded.
- Step 4 complete: shared citizen data contracts are outside the broad shell.
- Step 5 complete: citizen population state storage is outside the broad shell.
- Step 6 complete: citizen ECS projection/query ownership is outside the broad shell.
- Step 7 complete: citizen totals calculation and read-model state are outside the broad shell.
- Step 8 complete: runtime building read caches are outside the broad shell.
- Step 9 complete: household registration is outside the broad shell.
- Step 10 complete: rehousing helpers are outside the broad shell.
- Step 11 complete: refugee displacement and tent state are outside the broad shell.
- Step 12 complete: refugee upkeep is outside the broad shell.
- Step 13 complete: citizen schedule policy is outside the broad shell.
- Step 14 complete: citizen status transition policy is outside the broad shell.
- Step 15 complete: citizen danger scanning and flee-target policy are outside the broad shell.
- Step 16 complete: citizen travel and grid goal planning are outside the broad shell.
- Step 17 complete: citizen movement command mutation is outside the broad shell.
- Step 18 complete: visible citizen prefab selection is outside the broad shell.
- Step 19 complete: visible citizen spawn/despawn setup is outside the broad shell.
- Step 20 complete: visible citizen travel sync is outside the broad shell.
- Step 21 complete: external citizen lifecycle events are outside the broad shell.
- Step 22 complete: citizen debug command/read behavior is outside the broad shell.
- Step 23 complete: citizen population update diagnostics are outside the broad shell.
- Step 24 complete: citizen population runtime tick sequencing is outside the broad shell.
- Step 25 complete: citizen subsystem construction and init/dispose wiring are outside the broad shell.
- Step 26 complete: managed startup now creates and carries the explicit citizen composition result.
- Step 27 complete: gameplay runtime update no longer invokes the broad citizen shell.
- Step 28 complete: menu/UI population stats reads are off the broad citizen shell.
- Step 29 complete: building/citizen event coupling is off the broad citizen shell.
- Step 30 complete: visual citizen lifecycle reporting is off the broad citizen shell.
- Step numbers are fixed. If new work is discovered, update the relevant existing step instead of appending surprise steps beyond the validation gate.
