# Gameplay Refactor Roadmap: RTS Selection

This document preserves the active RTS selection architecture refactor roadmap so the work does not drift between implementation passes.

Runtime city generation has moved to `Design/Architecture/runtime_city_spawner_refactor_roadmap.md`.

## RTSSelectionSystem 13-Step Plan

Target file: `Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs`

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
    - Move move/attack marker prefab instantiation, material property blocks, show/hide timers, and marker positioning into `SelectionOrderMarkerPresentationSystemHelper`.

11. Complete: Extract HUD command/selection feedback
    - Move `BattleHudGameplayBridge` selection text, command mode, command result, and world marker visibility calls into a HUD feedback boundary.

12. Complete: Collapse camera-facing wrappers
    - Review remaining camera public methods.
    - Move direct callers to `RtsCameraSystem` where practical or keep only thin compatibility wrappers.

13. Complete: Final facade pass
    - Confirm `RTSSelectionSystem` owns no gameplay state, ECS mutation policy, visual marker lifecycle, transport/attack/building command logic, or HUD behavior.
    - Add/remove architecture guards.
    - Decision: keep `RTSSelectionSystem` temporarily as the input/UI compatibility shell. It still owns public UI-facing query/command entry points and focused transport disembark compatibility; do not retire/rename it until those surfaces move behind narrower systems.

## RTSSelectionSystem No-Managed-Shell Deletion Plan

Target file: `Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs`

Goal: delete the legacy `RTSSelectionSystem` source/type without replacing it with another managed orchestration shell. UI and shell code may write/read ECS requests, results, and read models; gameplay decisions and mutations must run through ECS data plus ECS systems.

1. Complete: Create ECS selection input request components
   - Add data-only ECS request components/buffers for pointer press/release, drag, click, camera drag, selection rectangle, and command intent requests.
   - No behavior migration in this step.

2. Complete: Move pointer state into ECS
   - Replace managed pointer/session fields with ECS singleton/state components.
   - Pointer sources write request buffers; ECS systems own session state.

3. Complete: Move selection rectangle selection into ECS
   - Selection rectangle becomes an ECS request processed by selection systems.
   - Selected-tag mutation remains in ECS systems.

4. Complete: Move move command flow into ECS requests
   - Screen/world click produces selected move-order requests and command results.
   - Selection command systems resolve clicked cells/targets and execute move orders.

5. Complete: Move attack command flow into ECS requests
   - Attack target mode and clicked attack commands become request/result buffers.
   - Attack systems own validation, issue results, and marker result data.

6. Complete: Move transport boarding/disembark into ECS requests
   - Boarding and disembark commands become ECS command buffers/results.
   - Transport mutation leaves `RTSSelectionSystem`.

7. Complete: Move focused-unit UI read model into ECS
   - Publish focused/selected unit UI data into ECS read-model components/buffers.
   - UI views read data only.

8. Complete: Move HUD feedback into ECS results
   - HUD consumes command result/read-model events instead of direct method calls.
   - `SelectionHudFeedbackSystem` becomes a result consumer or is deleted.

9. Complete: Move camera input/control into ECS requests
   - Zoom, pan, smooth focus, fullscreen iso, and normal iso mode become ECS camera request components.
   - Camera systems process request data.

10. Complete: Move assistant/tutorial commands to ECS request/result buffers
    - Assistant APIs write command requests and read command results.
    - No direct `RTSSelectionSystem` calls remain.

11. Complete: Move selection GUI rectangle to UI View only
    - View reads ECS rectangle state and draws.
    - No gameplay logic in the view.

12. In progress: Migrate all callers off `RTSSelectionSystem`
    - Bootstrap, runtime update, menu UI, assistant/tutorial, building interaction, and tests reference ECS boundaries or views only.
    - Current slices moved match-overlay/main-menu UI command calls, assistant runtime binding, all `MenuView` command/read/camera/marker calls, mission/building camera focus delegates, and building selection/transport/order compatibility delegates off direct `RTSSelectionSystem`.
    - Current slices also moved `GameBootstrap`, `MenuStartupSystem`, and `GameplayRuntimeUpdateSystem` to narrow selection delegates, and migrated functional battle HUD, missile launcher, and transport editor tests to narrower systems.
    - Remaining work is moving the last shell implementation out of managed startup and retiring architecture audit tests that still read the temporary runtime shell directly.

13. Pending: Delete the temporary runtime shell file and `.meta`
    - Remove the file and all temporary architecture allowances.
    - No production or test references to `RTSSelectionSystem` remain.

14. Pending: Full validation gate
    - Run architecture tests, focused selection/command tests, menu/bootstrap smoke, and runtime load/play validation.

## RTSSelectionSystem Final 10-Step Deletion Plan

Target file: `Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs`

Goal: finish deleting the legacy `RTSSelectionSystem` source/type without replacing it with another managed orchestration shell. Each step must reduce shell ownership and keep gameplay decisions in ECS request/result/read-model boundaries.

1. Complete: Architecture and runtime stabilization gate
   - Fix invalidated ECS buffer-handle paths caused by request processors mutating entities while reading dynamic buffers.
   - Confirm no new compile errors or runtime `ObjectDisposedException` regressions before extraction continues.

2. Complete: Extract runtime input tick
   - Move queued move-order consumption, normal pointer press/hold/release branching, UI click suppression, selection-hold triggering, live rectangle diffing, and rectangle request queueing into `RtsSelectionRuntimeInputCompositionSystemHelper`.
   - Keep `RTSSelectionSystem` as a temporary context builder/delegate only for this input slice.

3. Complete: Extract camera runtime tick
   - Move remaining camera update branches, build/fullscreen pan handling, smooth focus updates, pointer-driven camera drag state, and camera request flushing out of `RTSSelectionSystem`.
   - Keep camera mutations routed through ECS camera request/state systems, not direct shell methods.

4. Complete: Extract command result and marker flush tick
   - Move move/attack/transport command result draining, HUD feedback forwarding, screen-marker emission, and world-marker visibility forwarding out of `RTSSelectionSystem`.
   - Result consumers must use ECS buffers/read models and narrow shell-edge systems.

5. Complete: Extract remaining focus and selection compatibility commands
   - Move public focus/clear/select-all/select-filter command entry points to ECS command/request systems or existing selection boundaries.
   - `RTSSelectionSystem` must stop owning selected/focused command branching.

6. Complete: Extract remaining pointer target command dispatch
   - Move clicked attack, clicked transport boarding, clicked focus, and clicked building/target command dispatch into ECS request processors or existing narrow command systems.
   - Pointer systems may enqueue intent data, but gameplay command decisions must not live in the shell.

7. Complete: Migrate remaining production callers and startup wiring
   - Remove any production code that constructs, stores, or calls `RTSSelectionSystem`.
   - `ManagedGameplayStartupSystem`, `SelectionGameplayStartupSystem`, `GameBootstrap`, `GameplayRuntimeUpdateSystem`, and menu bindings must use ECS boundaries/delegates that do not expose the shell type.

8. Complete: Migrate tests and architecture audit references
   - Moved architecture/test reads off the deleted `Assets/Game/Scripts/Systems/RTSSelectionSystem.cs` artifact and onto the owning ECS systems/read models.
   - Added a contract guard that the retired source artifact and `public sealed class RTSSelectionSystem` type must not be restored.

9. Complete: Delete the shell and remove debt allowances
   - Removed the architecture allowance that described `RTSSelectionSystem` as temporary compatibility debt; it is now a hard retired source/type that must not be restored.
   - Deleted `Assets/Game/Scripts/Systems/SelectionRuntimeUpdateSystem.cs` and replaced the hidden monolithic `Update()` shell with explicit startup-composed runtime phases.
   - `SelectionRuntimeContextSystem` has been retired and must not be reintroduced.
   - Next substep: keep selection startup composed from owning narrow systems and remove any remaining test/contract allowances that referred to the old context shell.

10. Pending: Final validation gate
    - Run architecture tests, focused selection/command tests, menu/bootstrap smoke, and a focused runtime load/play validation.
    - Expected result: compile-clean, no `RTSSelectionSystem` source file or references, no architecture allowlist debt, and runtime selection/camera/building/unit flows still load and play.

## SelectionRuntimeContextSystem Deletion Plan

Target file: `Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs`

Goal: delete the remaining selection context-construction boundary without replacing it with another managed orchestration shell. Runtime selection must remain explicit startup-composed phases, and gameplay decisions must stay in ECS request/result/read-model systems.

1. Complete: Stabilize contract
   - Add/keep architecture guards that `SelectionRuntimeContextSystem` has no `Update()` method.
   - Add/keep guards that `SelectionRuntimeUpdateSystem.cs`, `RTSSelectionSystem.cs`, and the retired `RTSSelectionSystem` type must not be restored.

2. Complete: Extract config state
   - Move config/default value application out of `SelectionRuntimeContextSystem` into `SelectionRuntimeConfigStartupSystemHelper` or an existing config boundary.
   - Keep config data read-only after initialization.

3. Complete: Extract diagnostics helpers
   - Move selection and transport diagnostic queue helpers to the existing ECS diagnostics/logging boundary.
   - Runtime selection code must enqueue diagnostics without owning diagnostic entity creation/formatting policy.

4. Complete: Extract query ownership
   - Move `EnsureEntityQueries` and cached `EntityQuery` fields into the systems that use them.
   - Prioritize rectangle, focus, move, attack, transport, and marker systems.

5. Complete: Extract HUD feedback context
   - Move HUD helper methods for selection, squad selection, command mode, command result, clear selection, and world marker visibility into `SelectionHudFeedbackSystem` or a narrow feedback context boundary.

6. Complete: Extract camera context builder
   - Move `CreateRuntimeCameraContext` into `RtsSelectionRuntimeCameraSystemHelper` or a narrow ECS-style context builder.
   - Startup should pass concrete camera/request/runtime dependencies, not a broad selection context object.

7. Complete: Extract input context builder
   - Move `CreateRuntimeInputContext` into `RtsSelectionRuntimeInputCompositionSystemHelper` or a narrow ECS-style context builder.
   - Pointer input should depend on input state, runtime state, camera delegates, and command request systems only.

8. Complete: Extract command-result context builder
   - Move `CreateCommandResultFlushContext` into `RtsSelectionCommandResultFlushCompositionSystemHelper` or a narrow context builder.
   - Command result flushing must own its ECS buffer/read dependencies directly.

9. Complete: Extract focus command context builder
   - Move `CreateFocusCommandContext` into `RtsSelectionFocusCommandCompositionSystemHelper` or a narrow context builder.
   - Focus/select-all/clear command flow must not require `SelectionRuntimeContextSystem`.

10. Complete: Extract pointer-target context builder
    - Move `CreatePointerTargetCommandContext` into `RtsSelectionPointerTargetCommandCompositionSystemHelper` or a narrow context builder.
    - Clicked move, attack, transport, focus, and building-target command flow must not require `SelectionRuntimeContextSystem`.

11. Complete: Move remaining compatibility methods
    - Removed the obsolete focused-unit/selected-unit UI read compatibility surface from `SelectionRuntimeContextSystem`.
    - Focused-unit labels, health/capacity/status, transport passenger rows, selected-unit read lists, and visible-unit read helpers now stay behind `SelectionUiReadModelUiSystemHelper` and ECS read-model data.
    - Remaining production dependency on `SelectionRuntimeContextSystem` is runtime context construction/update composition only; that is the deletion target for step 12.

12. Complete: Delete context file
    - Deleted `Assets/Game/Scripts/Systems/SelectionRuntimeContextSystem.cs` and `.meta`.
    - `SelectionGameplayStartupSystem` now composes the runtime selection phases directly from the narrow ECS/UI boundary systems instead of constructing `SelectionRuntimeContextSystem`.

13. Complete: Remove debt allowances
    - Replaced old allowance wording with a hard rule: `SelectionRuntimeContextSystem` must not exist.
    - Removed tests that inspected the deleted context file and replaced them with deletion guards plus owning-system checks.

14. Complete: Validation gate
    - Local static validation passed: `git diff --check` is clean.
    - Local deletion validation passed: `SelectionRuntimeContextSystem` only appears in architecture docs/tests as a hard deletion rule.
    - Unity compile/domain reload passed: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -logFile /private/tmp/warlinecapture-step14-main-compile.log`.
    - Architecture tests passed: `GameplayArchitectureContractTests` (`95/95`).
    - Focused selection command tests passed: `RtsSelectionInputSystemTests` (`5/5`) and `BattleHudGameplayBridgeConnectionTests` (`7/7`).
    - Bootstrap/menu playmode smoke passed: `BootstrapAndMenuPlayModeTests` (`7/7`).
    - Additional transport scene smoke was investigated and fixed: `GameSceneTransportBoardingPlayModeTests` now passes (`2/2`) after repairing the `GameSubScene_InitialUnitsSpawner_Config` roster to include `Unit_Veh_Helicopter_Transport`.
