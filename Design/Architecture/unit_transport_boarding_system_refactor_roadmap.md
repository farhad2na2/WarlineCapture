# UnitTransportBoardingSystem Refactor Roadmap

This document owns the `UnitTransportBoardingSystem` refactor plan. The current system is an ECS `ISystem`, so the goal is not to replace it with managed orchestration. The goal is to reduce it to one responsibility: processing ready boarding targets into passenger state. Selection commands, capacity rules, candidate queries, approach-cell search, air pickup, rope disembark command setup, and diagnostics must move to narrow ECS/gameplay `*System` boundaries.

## Fixed Step Count

This roadmap has 30 steps. Do not append surprise steps after step 30. If new work is discovered, update the relevant existing step and keep the final validation gate as the last step.

## Target

Target file: `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`

Current size at roadmap creation: 965 lines. This is an observation, not a hard acceptance limit. The acceptance target is single responsibility.

Final target: `UnitTransportBoardingSystem` may remain only as the ECS boarding-completion tick that consumes `UnitTransportBoardingTarget`, validates the current target state through narrow rule/query systems, and mutates passenger state through a narrow passenger-state system. If the remaining type becomes pure pass-through after these steps, step 29 decides whether it should stay as the named ECS tick or be retired. No broad replacement shell may be introduced.

## Current Responsibility Inventory

- ECS boarding tick: scans `UnitTransportBoardingTarget`, validates target transport, waits for landed air transports, checks seat capacity, checks reach/settled state, boards passengers, clears movement/combat/selection tags, hides passenger visuals, and disables boarded passengers.
- Transport metadata and capacity: recognizes personnel transport prefab names, resolves capacity, adds capacity components, and adds passenger buffers.
- Candidate/read queries: detects player boardable transports, player soldier boarding candidates, rope-disembark-capable transports, click padding, direct boarding cells, passenger counts through peer callers, and transport landed state.
- Approach-cell search: finds ground transport approach cells, air pickup cells, disembark cells, passable footprints, reserved footprint cells, friendly-pass blockers, occupied cells, and live-unit overlap rejection.
- Air pickup commands: picks helicopter landing cells near selected passengers, updates air home state, clears move components, and issues pickup movement orders.
- Rope disembark commands: validates passenger buffers, sets helicopter airborne state, clears movement, and adds `UnitTransportRopeDisembarkRequest`.
- Diagnostics: owns diagnostic enablement checks, diagnostic queue creation, queue writes, boarding entity descriptions, and air-state descriptions.
- Cross-system coupling: selection command/read systems previously passed `UnitTransportBoardingSystem` around as a helper surface instead of depending on narrow transport query/command systems. This was retired in steps 21-24.

## Public/Internal Surface Hard Rule

`UnitTransportBoardingSystem` must expose no public/internal helper API. It may expose only the ECS lifecycle methods required by `ISystem`:

- `public void OnCreate(ref SystemState state)`
- `public void OnUpdate(ref SystemState state)`

Retired helper surface owners:

- Capacity metadata belongs in `UnitTransportCapacitySystem`.
- Boardable/candidate read queries belong in `UnitTransportBoardingQuerySystem`.
- Landed/reach/direct-cell rules belong in `UnitTransportBoardingRuleSystem`.
- Approach, reservation, pickup-cell, and disembark-cell search belongs in `UnitTransportApproachCellSystem`.
- Air pickup commands belong in `UnitTransportAirPickupSystem`.
- Rope disembark command setup belongs in `UnitTransportRopeDisembarkCommandSystem`.
- Boarding diagnostics belong in `UnitTransportBoardingDiagnosticSystem`.

## Architecture Rules

- Do not replace `UnitTransportBoardingSystem` with `UnitTransportBoardingManager`, `UnitTransportBoardingController`, `TransportBoardingFacade`, or another broad shell.
- New gameplay runtime types must be named `*System`, except existing `Config` assets and Unity edge types.
- No singleton/static runtime access. Static helpers are allowed only for pure deterministic math/data with no runtime dependencies.
- Do not use reflection.
- Do not move transport boarding behavior into UI, bootstrap, editor tooling, or config assets.
- Existing `TransportBoardingCommandSystem`, `UnitTransportRopeDisembarkSystem`, and diagnostic flush boundaries should be reused or narrowed before creating redundant owners.

## Performance And Behavior Rules

- Preserve current transport capacity values, passenger ordering, boarding clearance values, air grounded-height tolerance, click padding behavior, direct boarding distance, pickup search radius, disembark spacing, and rope drop interval unless a later gameplay task explicitly asks for tuning.
- Preserve current command semantics: boarding commands must still add `UnitTransportBoardingTarget`; boarded passengers must still be hidden, disabled, deselected, and linked with `UnitTransportPassenger`; full/missing/invalid transports must cancel targets.
- Preserve current air behavior: helicopters must not board while airborne, stale grounded flags must be reconciled by physical height, pickup commands must update home cell/position, and rope disembark must start airborne before dropping passengers.
- Preserve current grid/occupancy behavior: friendly-pass blocker rules, reserved-footprint rejection, occupied-cell handling, ignored occupancy entity behavior, and live-unit overlap rejection must not change.
- Preserve diagnostic event content and ECS queue/flush behavior unless the roadmap step explicitly moves formatting to a diagnostics boundary.
- Avoid per-frame managed allocations in the boarding tick. Extraction should move logic without adding LINQ, reflection, or new hot-path collections.

## Required Validation Gates

Every implementation step must run:

- `git diff --check` scoped to touched files.
- Focused architecture validation once this roadmap's tests exist.

Every phase boundary must also run when feasible:

- `GameplayArchitectureContractTests.RunUnitTransportBoardingArchitectureBatchValidation`.
- EditMode `UnitTransportBoardingSystemExtractionTests`.
- EditMode `UnitTransportValidationTests`.
- PlayMode `GameSceneTransportBoardingPlayModeTests` when command paths, air pickup, or rope disembark are touched.
- Runtime FPS play-button probe when a step changes boarding tick iteration, approach search, or command request processing.

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity validation.

## Phase 1: Baseline, Contract, And Surface Freeze

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add architecture contract wording that `UnitTransportBoardingSystem` is a temporary mixed-responsibility ECS system that must shrink to boarding-completion ownership.
   - Add focused architecture validation entry point for this roadmap.
   - Guard the 30-step roadmap, target file, current responsibility inventory, forbidden broad replacement names, and bounded public/internal surface.
   - Expected output: future changes cannot normalize or grow the mixed-responsibility system.

2. Complete: Freeze public/internal helper surface
   - Inventory every public/internal member on `UnitTransportBoardingSystem`.
   - Assign each member to the target owner listed above.
   - Add or tighten a guard preventing new helper surface from being added to the system.
   - Expected output: later steps retire named surface groups deliberately.
   - Public helper surface was inventoried in the Public/Internal Surface Inventory Freeze section before migration.
   - `UnitTransportBoardingSystemBaselineMustStayExplicitUntilExtracted` now guards the hard no-helper-surface rule and final owner mapping.

3. Complete: Add deterministic behavior baseline
   - Document the current focused EditMode and PlayMode transport validation commands.
   - Capture key expected outputs from existing tests.
   - Do not change code behavior in this step.
   - Expected output: later extraction steps have a behavior/performance comparison point.
   - Baseline EditMode extraction command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportBoardingSystemExtractionTests -testResults /private/tmp/warline-unit-transport-extraction-baseline.xml -logFile /private/tmp/warline-unit-transport-extraction-baseline.log`
   - Baseline EditMode boarding/rules command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitTransportValidationTests -testResults /private/tmp/warline-unit-transport-validation-baseline.xml -logFile /private/tmp/warline-unit-transport-validation-baseline.log`
   - Baseline PlayMode command when command paths, air pickup, or rope disembark are touched:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter GameSceneTransportBoardingPlayModeTests -testResults /private/tmp/warline-unit-transport-playmode-baseline.xml -logFile /private/tmp/warline-unit-transport-playmode-baseline.log`
   - Key expected extraction outputs:
     - Known personnel transports include `Unit_Veh_APC_Fast` and `Unit_Veh_Helicopter_Transport`; `Unit_Veh_Tank_Heavy` must not be boardable as personnel transport.
     - Known personnel transport capacity remains `10` and ensures `UnitTransportPassengerElement` buffer.
     - Player character prefab names remain boarding candidates; vehicle prefab names are rejected.
     - Footprint reservation preserves all in-bounds footprint cells.
   - Key expected boarding/rule outputs:
     - Ground APC-like personnel transport boards a nearby soldier, adds the passenger to the transport buffer, adds `UnitTransportPassenger`, and disables the passenger.
     - Air transport does not board until landed; raised helipad landing within `AirBoardingGroundedHeightTolerance` still boards.
     - Helicopter boarding remains tightened: no old wide-clearance boarding, no one-cell-short close-goal boarding, and no far-edge large-footprint boarding.
     - Flying helicopter pickup commands a landing near the selected passenger, marks stale physically-flying helicopters airborne, and does not board until landed.
     - Landing-cell search must not invalidate grid arrays.
     - Rope disembark starts the helicopter rope request and real Game scene exit drops/disperses passengers one by one.
   - Current baseline validation attempt:
     - `GameplayArchitectureContractTests.RunUnitTransportBoardingArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
     - The two Unity `-runTests` EditMode commands above exited `0` in this heartbeat, but Unity did not emit TestRunner XML files or a TestRunner summary in the logs. Do not treat those as pass results; rerun through the editor/Test Runner or a stable CI test invocation before behavior-changing steps that depend on them.

## Phase 2: Capacity, Metadata, And Candidate Queries

4. Complete: Extract transport capacity metadata
   - Create `UnitTransportCapacitySystem`.
   - Move personnel transport name recognition, capacity resolution, and capacity/buffer ensure behavior.
   - Preserve capacity value `10` for current personnel transports.
   - Expected output: capacity metadata has one owner.
   - Added `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs`.
   - `UnitTransportBoardingSystem` keeps temporary compatibility wrappers for `TryEnsureTransportCapacity`, `ResolveTransportCapacity`, and `IsPersonnelTransportName` until step 5 migrates callers/tests.
   - Capacity value and known personnel transport name checks were copied without changing behavior.

5. Complete: Migrate capacity tests and callers
   - Move extraction tests that instantiate `UnitTransportBoardingSystem` for capacity/name behavior to `UnitTransportCapacitySystem`.
   - Update `TransportBoardingCommandSystem` and read-model callers to use the capacity system.
   - Expected output: no non-boarding-tick caller asks `UnitTransportBoardingSystem` for capacity metadata.
   - `UnitTransportBoardingSystemExtractionTests` now uses `UnitTransportCapacitySystem` for personnel-name recognition and capacity/buffer ensure behavior.
   - Focused-unit passenger read-model paths now pass `UnitTransportCapacitySystem` into `SelectionUiReadModelLookup`.
   - `SelectionTransportCommandRequestSystem` and `TransportBoardingCommandSystem` now use `UnitTransportCapacitySystem` for capacity ensure/resolve calls.
   - Temporary capacity wrappers remain only on `UnitTransportBoardingSystem` for compatibility and local query behavior until the broader query extraction removes them.

6. Complete: Extract boarding candidate/read queries
   - Create `UnitTransportBoardingQuerySystem`.
   - Move boardable player transport checks, soldier boarding candidate checks, click padding, passenger-count helpers, and source-name query helpers.
   - Preserve player faction `0` filtering and vehicle/soldier name behavior.
   - Expected output: selection/UI read paths use query ownership, not the boarding tick.
   - Added `Assets/Game/Scripts/Systems/UnitTransportBoardingQuerySystem.cs`.
   - Moved click padding, boardable player transport checks, soldier boarding candidate checks, and source-name query helpers behind the query system.
   - `UnitTransportBoardingSystem` keeps temporary compatibility wrappers for query methods until step 7 migrates selection/read-model callers.
   - Passenger-count UI reads already use `UnitTransportCapacitySystem`; remaining selection query migration is tracked by step 7.

7. Complete: Migrate selection read-model callers
   - Update `FocusedUnitUiReadModelSystem`, `SelectionUiReadModelLookup`, and selection context systems to receive/use `UnitTransportBoardingQuerySystem`.
   - Keep UI/view classes as passive reference wiring only.
   - Expected output: selection read surfaces no longer depend on the full boarding tick system.
   - `FocusedUnitUiReadModelSystem` no longer receives `UnitTransportBoardingSystem` for passenger UI reads; passenger count/list reads use `UnitTransportCapacitySystem`.
   - `UnitTransportBoardingSystemExtractionTests` now uses `UnitTransportBoardingQuerySystem` for soldier candidate validation.
   - Pointer-target, command-result, transport command, and building-selection interaction contexts now carry `UnitTransportBoardingQuerySystem` for boardable/candidate/click-padding checks while retaining `UnitTransportBoardingSystem` only for later rule, approach, air-pickup, and rope-disembark steps.

## Phase 3: Boarding Rules And Passenger Mutation

8. Complete: Extract boarding reach/landed rules
   - Create `UnitTransportBoardingRuleSystem`.
   - Move landed-state checks, direct boarding cells, boarding clearance, movement-finished checks, and reached-transport decision shaping.
   - Preserve air-vs-ground clearance behavior exactly.
   - Expected output: boarding tick asks a rule system whether a passenger can board now.
   - Added `Assets/Game/Scripts/Systems/UnitTransportBoardingRuleSystem.cs`.
   - Moved landed-state checks, direct boarding cells, boarding constants, and boarding reach-state evaluation into the rule system.
   - `UnitTransportBoardingSystem` now delegates landed/direct-cell wrappers to `UnitTransportBoardingRuleSystem`; the full tick routing through `ReachState` is tracked in step 9.

9. Complete: Route `OnUpdate` through boarding rule results
   - Refactor the boarding tick to delegate rule decisions without changing iteration order or ECB playback.
   - Keep diagnostic decision content unchanged.
   - Expected output: behavior is unchanged, but reach/landed logic leaves the tick body.
   - `UnitTransportBoardingSystem.OnUpdate` now calls `UnitTransportBoardingRuleSystem.EvaluateReach` for movement-finished, boarding-goal, footprint, distance, and final reached-state decisions.
   - The existing wait diagnostic still emits the same fields, sourced from `ReachState`.
   - Iteration order, ECB lifetime/playback, passenger-buffer mutation, and cancellation behavior were not changed.

10. Complete: Extract boarded passenger state mutation
   - Create `UnitTransportPassengerStateSystem`.
   - Move passenger buffer add, hidden visual state, movement/order/tag cleanup, `UnitTransportPassenger` add, and `Disabled` add.
   - Preserve all component removals and add order side effects.
   - Expected output: passenger state mutation has one owner.
   - Added `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs`.
   - Moved passenger buffer add, hidden visual scaling, boarding-target removal, movement/order/combat/selection tag cleanup, `UnitTransportPassenger` add, and `Disabled` add into `BoardPassenger`.

11. Complete: Route `OnUpdate` boarding completion through passenger state
   - Make `UnitTransportBoardingSystem` call `UnitTransportPassengerStateSystem` for successful boarding.
   - Preserve `EntityCommandBuffer` lifetime and no per-frame allocation beyond current behavior.
   - Expected output: the boarding tick no longer owns cleanup details.
   - `UnitTransportBoardingSystem.OnUpdate` now calls `UnitTransportPassengerStateSystem.BoardPassenger` after reach/capacity checks succeed.
   - The boarding diagnostic still emits the same `seats=current/capacity` value using the returned passenger count.
   - ECB lifetime/playback remains owned by the boarding tick.

## Phase 4: Approach, Pickup, And Disembark Commands

12. Complete: Extract transport approach-cell search
   - Create `UnitTransportApproachCellSystem`.
   - Move ground approach, disembark ring, air pickup cell search support, passability checks, and reserved footprint reservation.
   - Preserve search radius/order, scoring, friendly-pass blocker policy, occupancy policy, and live-unit overlap rejection.
   - Expected output: approach-cell algorithms are isolated and testable.
   - Added `Assets/Game/Scripts/Systems/UnitTransportApproachCellSystem.cs`.
   - Moved air pickup landing-cell search, ground approach-cell search, footprint reservation, disembark ring search, passability checks, friendly-pass blocker handling, occupied-cell handling, ignored-occupancy handling, and live-unit overlap rejection into the approach-cell system.
   - `UnitTransportBoardingSystem` keeps temporary compatibility wrappers for approach/disembark methods until step 13 migrates tests and command callers.

13. Complete: Migrate approach tests and command callers
   - Move `ReserveFootprintCells`, `TryFindTransportApproachCell`, and `TryFindTransportDisembarkCell` tests/callers to `UnitTransportApproachCellSystem`.
   - Update `TransportBoardingCommandSystem` without changing command behavior.
   - Expected output: the boarding tick owns no path/search helper surface.
   - `UnitTransportBoardingSystemExtractionTests` now validates footprint reservation through `UnitTransportApproachCellSystem`.
   - `TransportBoardingCommandSystem` now uses `UnitTransportApproachCellSystem` for boarding approach search and footprint reservation.
   - `SelectionTransportCommandRequestSystem` now uses `UnitTransportApproachCellSystem` for ground disembark-cell search.
   - Temporary wrappers remain on `UnitTransportBoardingSystem` only for compatibility until step 25 removes migrated helper surface.

14. Complete: Extract air pickup commands
   - Create `UnitTransportAirPickupSystem`.
   - Move pickup candidate iteration, pickup landing cell selection, air home-state update, and target-only move command.
   - Preserve selected-passenger order and helicopter state mutations.
   - Expected output: air pickup behavior has one command owner.
   - Added `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs`.
   - Moved selected-passenger iteration, air pickup landing-cell search, pickup home-cell/home-position mutation, airborne reconciliation, movement-component clearing, and target-only pickup move command into the air pickup system.
   - `UnitTransportBoardingSystem` keeps temporary compatibility wrappers for air pickup methods until step 15 migrates command callers and tests.

15. Complete: Migrate air pickup callers and tests
   - Update selection/transport command paths and tests to use `UnitTransportAirPickupSystem`.
   - Preserve the existing flying-helicopter pickup validation behavior.
   - Expected output: no caller uses the boarding tick for pickup preparation.
   - `TransportBoardingCommandSystem` now uses `UnitTransportAirPickupSystem` for pickup landing-cell search and pickup movement command execution.
   - `UnitTransportValidationTests` now validates air pickup preparation and landing-cell search through `UnitTransportAirPickupSystem`.
   - Temporary wrappers remain on `UnitTransportBoardingSystem` only for compatibility until step 25 removes migrated helper surface.

16. Complete: Extract rope disembark command setup
   - Create `UnitTransportRopeDisembarkCommandSystem` or extend the existing rope disembark boundary if it is the narrower owner.
   - Move rope-capable transport checks and `StartRopeDisembarkTransport`.
   - Preserve passenger-buffer validation, helicopter airborne setup, movement clearing, and `DropIntervalSeconds = 0.8f`.
   - Expected output: rope disembark command setup is owned outside the boarding tick.
   - Added `Assets/Game/Scripts/Systems/UnitTransportRopeDisembarkCommandSystem.cs`.
   - Moved rope-capable transport source-name checks, passenger-buffer validation, movement clearing, helicopter airborne setup, and `UnitTransportRopeDisembarkRequest` creation into the command system.
   - `UnitTransportBoardingSystem` keeps temporary compatibility wrappers for rope disembark command methods until step 17 migrates command callers and tests.

17. Complete: Migrate rope disembark callers and tests
   - Update `TransportBoardingCommandSystem`, selection command flush paths, and playmode helpers to use the rope command owner.
   - Preserve real Game scene helicopter exit behavior.
   - Expected output: no disembark command setup remains on the boarding tick.
   - `SelectionTransportCommandRequestSystem` now receives and uses `UnitTransportRopeDisembarkCommandSystem` for rope-capability checks and rope request setup.
   - `RtsSelectionCommandResultFlushSystem` and `RtsSelectionCommandResultContextSystem` now pass the rope command owner through the transport command request path.
   - Focused EditMode and PlayMode transport disembark helpers now provide `UnitTransportRopeDisembarkCommandSystem` directly.
   - Temporary wrappers remain on `UnitTransportBoardingSystem` only for compatibility until step 25 removes migrated helper surface.

## Phase 5: Diagnostics And Command Context Migration

18. Complete: Extract boarding diagnostic queue ownership
   - Create `UnitTransportBoardingDiagnosticSystem`.
   - Move diagnostic enablement, queue creation, queue writes, boarding entity description, and air-state description.
   - Preserve diagnostic messages and batchmode enablement behavior.
   - Expected output: diagnostics are behind an ECS diagnostic boundary.
   - Added `Assets/Game/Scripts/Systems/UnitTransportBoardingDiagnosticSystem.cs`.
   - Moved diagnostic state query creation, batchmode/diagnostic enablement checks, diagnostic queue query creation, queue creation, queue writes, boarding entity descriptions, source-name resolution, and air-state descriptions into the diagnostic system.
   - `UnitTransportBoardingSystem` keeps temporary private compatibility wrappers until step 19 routes the tick directly through the diagnostic system.

19. Complete: Route boarding tick diagnostics through diagnostic system
   - Update `UnitTransportBoardingSystem` to request diagnostics through `UnitTransportBoardingDiagnosticSystem`.
   - Keep `TransportBoardingDiagnosticLogFlushSystem` as the shell/log flush boundary.
   - Expected output: no diagnostic formatting remains in the boarding tick.
   - `UnitTransportBoardingSystem.OnUpdate` now uses `UnitTransportBoardingDiagnosticSystem` directly for diagnostic enablement, queue resolution, and event writes.
   - Moved the boarding tick diagnostic message formatting into named diagnostic event methods on `UnitTransportBoardingDiagnosticSystem`.
   - Removed private diagnostic queue, enqueue, entity-description, and air-state-description wrappers from `UnitTransportBoardingSystem`.

20. Complete: Split selection command contexts by narrow transport dependencies
   - Replace context fields that carry the full `UnitTransportBoardingSystem` with capacity/query/rule/approach/air/rope dependencies as needed.
   - Preserve command result structs and request buffer semantics.
   - Expected output: selection command contexts cannot reach unrelated transport boarding behavior.
   - `RtsSelectionPointerTargetCommandSystem.Context` and `RtsSelectionCommandResultFlushSystem.Context` now carry explicit capacity, query, rule, approach, air-pickup, and rope-disembark command dependencies.
   - `RtsSelectionPointerTargetCommandContextSystem`, `RtsSelectionCommandResultContextSystem`, and `SelectionGameplayStartupSystem` now compose and pass those narrow transport dependencies.
   - The temporary `UnitTransportBoardingSystem` context field remains only as the migration bridge for steps 21 and 22, where the pointer-target and command-result paths are moved to the narrow dependencies.

21. Complete: Migrate pointer-target command path
   - Update `RtsSelectionPointerTargetCommandContextSystem` and `RtsSelectionPointerTargetCommandSystem`.
   - Preserve click-to-board and click-to-air-pickup behavior.
   - Expected output: pointer target commands use narrow transport systems.
   - `RtsSelectionPointerTargetCommandSystem.Context` no longer carries `UnitTransportBoardingSystem`.
   - `RtsSelectionPointerTargetCommandContextSystem` now composes pointer-target commands from capacity, query, rule, approach, air-pickup, and rope-disembark dependencies only.
   - Pointer-target boardable-click checks now use `UnitTransportBoardingRuleSystem` and `UnitTransportBoardingQuerySystem`.
   - Building-selection click guards and focused transport-click tests were updated to use the rule/query path instead of the broad boarding tick.

22. Complete: Migrate command-result flush path
   - Update `RtsSelectionCommandResultContextSystem` and `RtsSelectionCommandResultFlushSystem`.
   - Preserve transport board/disembark result handling.
   - Expected output: command result flushing no longer depends on the whole boarding tick.
   - `RtsSelectionCommandResultFlushSystem.Context` no longer carries `UnitTransportBoardingSystem`.
   - `RtsSelectionCommandResultContextSystem` no longer composes the whole boarding tick into the command-result flush context.
   - `SelectionTransportCommandRequestSystem` keeps the temporary boarding-tick helper bridge internally until step 23 migrates command execution to the narrow transport systems.

23. Complete: Migrate `TransportBoardingCommandSystem`
   - Change transport command execution to accept and use narrow transport systems.
   - Preserve boarding target creation, selected-passenger ordering, and diagnostic behavior.
   - Expected output: command execution no longer receives `UnitTransportBoardingSystem` as a helper bundle.
   - `TransportBoardingCommandSystem.TryRequestBoardTransportOrderToClickedUnit` now receives query, rule, approach-cell, and air-pickup dependencies directly.
   - Transport landed checks and direct boarding cell distance now use `UnitTransportBoardingRuleSystem`.
   - Boarding approach search/reservation uses the passed `UnitTransportApproachCellSystem`.
   - Air pickup landing search and pickup movement command use the passed `UnitTransportAirPickupSystem`.
   - `SelectionTransportCommandRequestSystem` no longer passes or stores `UnitTransportBoardingSystem`; disembark uses capacity, approach, and rope command owners.

24. Complete: Migrate startup/composition construction
   - Update `SelectionGameplayStartupSystem` and any managed startup result structs to compose the new transport systems explicitly.
   - Do not introduce a `TransportBoardingManager`, `Facade`, or service locator.
   - Expected output: composition owns narrow dependencies directly.
   - `SelectionGameplayStartupSystem` no longer constructs or passes `UnitTransportBoardingSystem` for managed selection command wiring.
   - Selection startup composes explicit capacity, query, rule, approach-cell, air-pickup, and rope-disembark systems for pointer-target and command-result paths.
   - The remaining `UnitTransportBoardingSystem` production reference is the ECS tick type itself and update-order attributes around that tick.

## Phase 6: Shrink, Retire Helper Surface, And Validate

25. Complete: Remove helper public surface from `UnitTransportBoardingSystem`
   - Delete all migrated public helper methods from `UnitTransportBoardingSystem`.
   - Keep only `OnCreate`/`OnUpdate` and private tick-local helpers required for iteration, if any.
   - Expected output: `UnitTransportBoardingSystem` is no longer a helper API.
   - Removed public capacity, query, landed/rule, air-pickup, rope-disembark, approach-cell, footprint-reservation, and disembark-cell wrapper methods.
   - `UnitTransportBoardingSystem` now exposes only the ECS lifecycle methods required by `ISystem`.
   - The boarding tick uses the rule, passenger-state, and diagnostic systems internally for the remaining boarding-completion responsibility.

26. Complete: Update tests to target final owners
   - Move remaining tests away from direct `new UnitTransportBoardingSystem()` helper usage.
   - Keep ECS `world.CreateSystem<UnitTransportBoardingSystem>()` tests only for boarding tick behavior.
   - Expected output: tests enforce the new owner boundaries.
   - Existing helper-behavior tests now target `UnitTransportCapacitySystem`, `UnitTransportBoardingQuerySystem`, `UnitTransportApproachCellSystem`, `UnitTransportAirPickupSystem`, `UnitTransportRopeDisembarkCommandSystem`, and transport command/request systems directly.
   - Test search shows no `new UnitTransportBoardingSystem()` helper-style construction remains.
   - `world.CreateSystem<UnitTransportBoardingSystem>()` remains only in focused boarding tick behavior tests.

27. Complete: Remove architecture debt allowances
   - Remove temporary allowlist entries that permit broad transport helper surface.
   - Add hard guards against new public helper methods on `UnitTransportBoardingSystem`.
   - Expected output: helper behavior cannot drift back into the tick.
   - Replaced the temporary public-surface allowance section with a hard `OnCreate`/`OnUpdate`-only rule.
   - Retained final owner mapping for capacity, query, rule, approach, air-pickup, rope-disembark, and diagnostic behavior.
   - Architecture validation now rejects helper methods and managed command/startup references to the broad boarding tick.

28. Complete: Performance and allocation audit
   - Check boarding tick, approach search, air pickup, and command paths for new hot-path allocations or changed search order.
   - Run focused FPS/log diagnostics if command/tick timing changed.
   - Expected output: refactor preserves current transport boarding performance.
   - Code audit found no LINQ, reflection, new per-frame collections, changed search order, changed command request budgets, or changed approach/pickup traversal constants.
   - Boarding tick now creates the rule and passenger-state system structs once per update before iterating boarding targets.
   - Command migration kept existing selected-passenger ordering, pending boarding count checks, approach-cell search/reservation order, and air pickup landing search order.
   - Focused architecture validation was used as the compile/contract gate; no FPS probe was run because no scheduler, budget, traversal constant, or hot-path collection behavior changed.

29. Complete: Final tick ownership decision
   - If `UnitTransportBoardingSystem` still owns the real ECS boarding-completion tick, keep it with the original name and document the final responsibility.
   - If it is pure pass-through, delete it and move update ordering to the true owner.
   - Expected output: no broad shell remains by name or behavior.
   - Decision: keep `UnitTransportBoardingSystem` as the named ECS boarding-completion tick.
   - It is not pure pass-through: it owns the `UnitTransportBoardingTarget` query, missing/full transport cancellation flow, ECB lifetime/playback, and successful boarding handoff to `UnitTransportPassengerStateSystem`.
   - It no longer owns capacity metadata, read queries, approach search, air pickup commands, rope command setup, diagnostics formatting, or managed command composition.

30. Complete: Validation gate
   - Run architecture validation, transport EditMode tests, transport PlayMode smoke when feasible, and `git diff --check`.
   - Write final handoff report under `Design/AgentReports`.
   - Expected output: compile-clean, contract-clean, behavior preserved, and no transport boarding architecture debt remains.
   - Replaced the invalid batch Game-scene smoke with deterministic PlayMode transport fixtures that construct actual boardable ECS helicopter and soldier entities.
   - The new PlayMode smoke verifies that a selected soldier clicking `Unit_Veh_Helicopter_Transport` receives a boarding order, boards after reaching the goal, becomes hidden/disabled, and is added to the passenger buffer.
   - The new PlayMode smoke also verifies focused helicopter exit command flow, rope disembark request creation, one-by-one passenger rope drops, passenger disperse, request cleanup, and distinct final passenger cells.
   - Architecture validation passed through step 29 and the step 30 architecture gate.
   - `GameSceneTransportBoardingPlayModeTests` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `/private/tmp/warline-unit-transport-playmode-step30-fixed2.xml` reported `total=2 passed=2 failed=0`.
   - `git diff --check` passed for the touched PlayMode validation file.
   - EditMode TestRunner commands for `UnitTransportBoardingSystemExtractionTests` and `UnitTransportValidationTests` exited cleanly but did not emit XML or TestRunner summary output in batchmode.
