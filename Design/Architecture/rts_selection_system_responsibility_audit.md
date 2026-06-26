# RTSSelectionSystem Responsibility Audit

## Purpose

`RTSSelectionSystem` is currently a legacy gameplay facade. Its single acceptable reason to change should become input-to-command orchestration, but today it changes for selection state, UI read models, move orders, transport boarding, targeting, camera behavior, HUD feedback, and gameplay command policy.

## Current Responsibility Buckets

### Selection State

- Focused unit entity.
- Selected move-entity cache.
- Selected tag clearing and focus refresh.
- Select-all and rectangle selection entry points.

Target owner: `SelectionStateCompositionSystemHelper` plus ECS selected/focused components over time.

### Input And Drag Selection

- Pointer state.
- Drag rectangle lifetime.
- Selection hold activation.
- UI click suppression.

Target owner: keep temporarily in `RTSSelectionSystem` as the input facade, then move stable data to ECS request components.

### Move Orders

- Manual move target resolution.
- Formation offsets.
- Path request creation.
- Staggered group move behavior.
- Move order HUD result reporting.

Target owner: `UnitMoveOrderSystem`.

### Transport Boarding

- Boarding source selection.
- Capacity checks.
- Pickup, approach, disembark, and rope-drop cell selection.
- Boarding target component writes.

Target owner: `UnitTransportBoardingSystem`.

### Attack And Target Orders

- Attack target validation.
- Radar/missile target selection.
- Detector-radius checks.
- Engage target and combat command writes.

Target owner: `UnitTargetOrderSystem`.

### UI Read Models

- Focused unit label, description, health, capacity, passenger list, and status.
- Portrait pose and selected-unit framing data.
- HUD selection result text.

Target owner: `SelectionUiReadModelLookup`.

### Camera Control

- Follow/focus target movement.
- Mode-specific camera transition state.
- Fullscreen/build mode camera settings.

Target owner: `RtsCameraSystem` or a shell-edge camera service fed by ECS camera request components.

## First Extraction Completed

`SelectionStateCompositionSystemHelper` now owns:

- Focused unit storage.
- Selected move-entity cache storage.
- Cache eligibility for player move units.
- Cache mutation helpers used by `RTSSelectionSystem`.

`RTSSelectionSystem` remains the facade for behavior in this slice; future work should keep moving behavior out by bucket without adding new responsibilities to the facade.

## Second Extraction Completed

`UnitMoveOrderSystem` now owns:

- Manual move goal selection.
- Manual formation offsets.
- Reserved-goal and selected-current-footprint cell rules.

`UnitTransportBoardingSystem` now owns:

- Boardable player transport checks.
- Transport capacity normalization from source-prefab identity.
- Boarding click padding and landed-state policy.

`UnitTargetOrderSystem` now owns:

- Missile launcher radar target lookup.
- Friendly detector-radius checks.
- Target distance/classification helpers.
- Cleanup of accidental nearby air-selection move orders.

## Third Extraction Completed

`SelectionUiReadModelLookup` now owns:

- Focused unit label, description, health, capacity, ownership, attack, and vehicle read models.
- Focused transport passenger UI lists.
- Focused and selected portrait pose/framing calculations.
- Focused unit UI status and HUD selection status text.

## Fourth Extraction Completed

`UnitMoveOrderSystem` now owns:

- Grouped manual move command component writes.
- Immediate move command component writes.
- Shared movement-order cleanup.
- Ground path request creation and staggered retry-cooldown scheduling for grouped move orders.
- Air-unit move command path-request removal.

## Fifth Extraction Completed

`UnitTransportBoardingSystem` now owns:

- Soldier boarding candidate policy.
- Air transport pickup landing-cell search.
- Transport approach-cell search and passability checks.
- Disembark ring-cell search.
- Boarding footprint reservation.
- Transport helicopter rope-disembark request setup.

## Sixth Extraction Completed

`UnitTargetOrderSystem` now owns:

- Attack target validation.
- Attack source validation.
- Selected-unit attack order component writes.
- Direct radar attack component writes.
- Base-breach attack order component writes.
- Commanded attack-order cleanup.

## Seventh Extraction Completed

`RtsCameraSystem` now owns:

- Camera drag-session state.
- Smooth camera focus target state.
- Smooth focus velocity state.
- Smooth focus target advancement and completion clearing.

## Eighth Extraction Completed

`RtsCameraSystem` now also owns:

- Camera mode transition state.
- Fullscreen iso target state.
- Perspective and fullscreen iso transition velocities.
- Camera pan and zoom transform writes.
- Perspective and fullscreen iso camera mode writes.
- Ground-center movement and viewport ground-plane ray queries.
- Ground-span camera mode fitting calculations.

`RTSSelectionSystem` remains the input/UI facade and still decides when camera actions are requested from runtime/UI state.

## Ninth Extraction Completed

`RtsSelectionInputCompositionSystemHelper` now owns:

- Pointer drag origin/current/last-position state.
- UI-click and world-release suppression state.
- Selection-hold timing state.
- Live selection rectangle cache state.
- Deferred move-order click queue state.
- Last-known pointer position state.

`RTSSelectionSystem` still performs input orchestration and gameplay command dispatch; this slice only moved mutable input session state and small state-only helpers.

## Tenth Extraction Started

`RuntimeGameplayStateSystem` now owns the compatibility boundary for the first migrated `InitialUnitsRuntimeState` flags used by `RTSSelectionSystem`:

- Play/build/map mode flags through `RuntimeGameplayStateComponent`.
- Selection-mode and world-click suppression flags through `RuntimeGameplayStateComponent`.
- Camera zoom-held flags through `RuntimeCameraInputComponent`.
- Initial camera-focus requests through `RuntimeCameraFocusRequestComponent`.

The wrapper still mirrors the legacy static state so other unmigrated callers keep working during the migration.

## Eleventh Extraction Started

The first UI caller group now routes migrated runtime flags through `RuntimeGameplayStateSystem`:

- `MainMenuPlayUI`
- `MenuView`

These callers no longer touch the migrated `InitialUnitsRuntimeState` play/build/map, selection mode, suppress-click, zoom-held, or camera-focus flags directly.

## Twelfth Extraction Started

The build-mode caller group now routes migrated runtime flags through `RuntimeGameplayStateSystem`:

- `RoadBuildSystem`
- `BuildingPlacementSystem`
- `GameBootstrap`

These callers no longer touch the migrated `InitialUnitsRuntimeState` play/build/map, selection mode, suppress-click, zoom-held, or camera-focus flags directly. `GameBootstrap` still assigns `InitialUnitsRuntimeState.WorldCamera` because camera object references are legacy compatibility state outside this migrated slice.

## Thirteenth Extraction Started

Remaining production `PlayRequested` callers now use the runtime-state boundary:

- Managed callers use `RuntimeGameplayStateSystem`.
- ECS `ISystem` callers read `RuntimeGameplayStateComponent` directly.
- AI and threat validation tests seed `RuntimeGameplayStateComponent` through `RuntimeGameplayStateTestHelper`.

`InitialUnitsRuntimeState.PlayRequested` remains only inside `RuntimeGameplayStateSystem` as the legacy compatibility mirror and inside editor/test code.

## Fourteenth Extraction Started

`PlayerAutoModeEnabled` now flows through `RuntimeGameplayStateComponent` and `RuntimeGameplayStateSystem`.

The migrated production callers are:

- `GameBootstrap`
- `MenuView`

Direct production access to `InitialUnitsRuntimeState.PlayerAutoModeEnabled` is now blocked by architecture contract coverage, with `RuntimeGameplayStateSystem` remaining the sole production compatibility bridge.

## Fifteenth Extraction Started

`WorldCamera` now flows through a managed ECS camera-reference boundary:

- `RuntimeCameraReferenceComponent`
- `RuntimeCameraReferenceSystem`

The migrated production callers are:

- `GameBootstrap`
- `UnitModelSpawnSystem`
- `UnitRenderBudgetSystem`

Direct production access to `InitialUnitsRuntimeState.WorldCamera` is now blocked by architecture contract coverage, with `RuntimeCameraReferenceSystem` remaining the sole production compatibility bridge.

## Sixteenth Extraction Started

AI log enablement now flows through a runtime diagnostics boundary:

- `RuntimeDiagnosticsStateComponent`
- `RuntimeDiagnosticsSystem`

The temporary `AILog` compatibility facade has now been retired after AI diagnostic call sites moved to ECS diagnostic events.

Direct production access to `InitialUnitsRuntimeState.VerboseAILogs` and `InitialUnitsRuntimeState.ShouldLogAI` is now blocked by architecture contract coverage, with `RuntimeDiagnosticsSystem` remaining the sole production compatibility bridge.

## Seventeenth Extraction Started

Transport boarding diagnostics now flow through the runtime diagnostics boundary:

- `RuntimeDiagnosticsStateComponent`
- `RuntimeDiagnosticsSystem`

The migrated production callers are:

- `RTSSelectionSystem`
- `UnitTransportBoardingSystem`

Direct production access to `InitialUnitsRuntimeState.TransportBoardingDiagnostics` is now blocked by architecture contract coverage, with `RuntimeDiagnosticsSystem` remaining the sole production compatibility bridge.

## Eighteenth Extraction Started

`AIBuildPlannerSystem` no longer calls the static `AILog` facade.

AI build diagnostics now flow through ECS diagnostic events:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The build planner gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Nineteenth Extraction Started

`AIProductionSystem` no longer calls the static `AILog` facade.

AI production diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The production system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twentieth Extraction Started

`AISquadSystem` no longer calls the static `AILog` facade.

AI squad diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The squad system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twenty-First Extraction Started

`AITargetingSystem` no longer calls the static `AILog` facade.

AI targeting diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The targeting system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twenty-Second Extraction Started

`AICombatOrderSystem` no longer calls the static `AILog` facade.

AI combat-order diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The combat-order system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twenty-Third Extraction Started

`AIEconomySystem` no longer calls the static `AILog` facade.

AI economy diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The economy system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twenty-Fourth Extraction Started

`AIFactionControlSystem` no longer calls the static `AILog` facade.

AI faction-control diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

The faction-control system gates diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries, and lets the flush system write logs at the shell/logging boundary.

## Twenty-Fifth Extraction Started

`GameBootstrap` AI config diagnostics no longer call the static `AILog` facade.

Bootstrap AI config diagnostics now flow through the same ECS diagnostic event path:

- `AIDiagnosticLogQueueComponent`
- `AIDiagnosticLogComponent`
- `AIDiagnosticLogFlushSystem`

`GameBootstrap` now gates AI config diagnostic message construction before formatting strings, queues `AIDiagnosticLogComponent` entries at gameplay start, and explicitly flushes that queue through `AIDiagnosticLogFlushSystem` so startup validation remains visible at the shell/logging boundary. The diagnostic component now carries a severity byte so missing-config diagnostics can remain warnings without reintroducing static `AILog` calls.

## Twenty-Sixth Extraction Started

Transport boarding diagnostics no longer call `Debug.Log` directly from the boarding command/execution paths.

Transport boarding diagnostics now flow through an ECS diagnostic event path:

- `TransportBoardingDiagnosticLogQueueComponent`
- `TransportBoardingDiagnosticLogComponent`
- `TransportBoardingDiagnosticLogFlushSystem`

`RTSSelectionSystem` and `UnitTransportBoardingSystem` now gate transport diagnostic message construction before formatting entity/pathing details, queue `TransportBoardingDiagnosticLogComponent` entries, and let the flush system write Unity logs at the shell/logging boundary.

## Twenty-Seventh Extraction Started

`FocusableUnitLookupSystem` now owns the clicked-unit focus lookup cache that was previously inside `RTSSelectionSystem`:

- focusable unit cell coverage cache
- focusable unit changed-grid and changed-footprint queries
- focusable candidate policy for transient/grounded air units
- padded footprint lookup and closest screen-space candidate selection

`RTSSelectionSystem` still owns the input-to-command flow for focus and attack clicks, but it no longer owns the focusable lookup cache or its refresh algorithms.

## Twenty-Eighth Extraction Started

`VisibleUnitSelectionSystem` now owns the visible screen-selection query/filter slice that was previously inside `RTSSelectionSystem`:

- screen-rectangle player unit collection
- select-all soldiers/vehicles/all filter policy
- visible player unit existence checks
- selected-unit tag application for visible selection results

`RTSSelectionSystem` still owns the public select-all entry points and HUD feedback, but it no longer owns the visible-unit entity query or soldiers/vehicles filter iteration.

## Twenty-Ninth Extraction Started

`FocusedUnitCommandSystem` now owns focused-unit command component mutations that were previously inside `RTSSelectionSystem`:

- focused unit destroy/health-zero mutation
- focused return-to-base respawn spawn-point lookup
- focused auto-attack command cleanup
- missile launcher radar target-mode policy and direct radar attack write
- hold/stop selected-unit movement component cleanup

`RTSSelectionSystem` still owns public UI command entry points and HUD feedback, but it no longer owns the focused-command mutation algorithms.

## Thirtieth Extraction Started

`SelectedUnitOrderSnapshotSystem` now owns selected-unit order snapshot/restore state that was previously inside `RTSSelectionSystem`:

- selected-unit order snapshot storage
- engage target, unit target, path request, path follow, and path range component capture
- generic component restore helper
- snapshot clearing when no gameplay world exists

`RTSSelectionSystem` still exposes the public preserve/restore methods for compatibility, but it no longer owns preserved-order storage or restore algorithms.

## Thirty-First Extraction Started

`BuildingTargetMoveOrderSystem` now owns building-target move order behavior that was previously inside `RTSSelectionSystem`:

- building approach-cell search around the target footprint
- approach candidate scoring against walkable, blocked, and occupied grid data
- selected-unit move component writes for building-target movement
- manual move tag assignment for building-target movement
- already-moving-to-goal skip policy for building-target movement

`RTSSelectionSystem` still exposes `TryRequestMoveOrderToBuilding` for compatibility and still clears selection / emits the screen marker after a successful command, but it no longer owns the building-target move algorithm.

## Thirty-Second Extraction Started

`TransportBoardingCommandSystem` now owns transport boarding click orchestration that was previously inside `RTSSelectionSystem`:

- selected boarding-source collection and selected move cache fallback
- clicked or nearby boardable transport resolution
- pending boarding target counts for seat availability
- boarding order creation and passenger movement command writes
- air pickup landing command handoff
- transport boarding command diagnostics coordination

`RTSSelectionSystem` still owns the pointer entry point, move-order marker emission, selection clearing, and focused-unit reset after a successful boarding command, but it no longer owns the boarding command algorithm.

## Thirty-Third Extraction Started

`FocusedUnitLifecycleSystem` now owns focused-unit lifecycle behavior that was previously inside `RTSSelectionSystem`:

- focused entity existence and validity checks
- focused-unit refresh from current selected tags
- enemy selected-tag cleanup for focused enemy entities
- selected-tag clearing and selected move-cache clearing
- single-selection focus synchronization
- direct focus assignment for explicit focus commands
- clicked focus command routing before RTS input suppression side effects

`RTSSelectionSystem` still owns the input suppression flags, camera drag reset, HUD command mode, and building-selection bridge calls around focus changes, but it no longer owns the focused-unit lifecycle mutation algorithms.

## Thirty-Fourth Extraction Started

`AttackOrderCommandSystem` now owns attack order command orchestration that was previously inside `RTSSelectionSystem`:

- selected attack-capable unit query ownership
- clicked attack target resolution handoff
- attack target validation dispatch
- attack order issue dispatch into `UnitTargetOrderSystem`
- base-breach target resolution bridge through `BuildingPlacementInteractionBoundaryCompositionSystemHelper`
- attack command result and target-position return contract

`RTSSelectionSystem` still owns the pointer entry point, attack marker visual emission, HUD result application, command mode cleanup, and focus clearing after a successful attack command, but it no longer owns the attack-click command algorithm.

## Thirty-Fifth Extraction Started

`SelectionOrderMarkerPresentationSystemHelper` now owns order marker visual runtime behavior that was previously inside `RTSSelectionSystem`:

- move and attack marker prefab instantiation
- runtime marker GameObject lifetime
- marker renderer and material property block ownership
- move and attack marker show/hide timers
- grid-blocked validation for move marker placement
- marker world-position projection to grid origin height

`RTSSelectionSystem` still owns the command entry points and HUD world-marker visibility bridge, but it no longer owns marker GameObjects, marker renderer state, marker query state, or marker timing.

## Thirty-Sixth Extraction Started

`SelectionHudFeedbackSystem` now owns HUD command and selection feedback behavior that was previously inside `RTSSelectionSystem`:

- `BattleHudGameplayBridge` lookup/cache ownership
- focused-unit HUD display text and status application
- squad-selection HUD labels
- command mode feedback
- command result feedback
- HUD world-marker visibility forwarding

`RTSSelectionSystem` still owns the gameplay command branches that decide which HUD feedback to request, but it no longer owns the `BattleHudGameplayBridge` dependency or direct HUD bridge calls.

## Thirty-Seventh Extraction Completed

`RtsCameraSystem` now receives direct calls for:

- Perspective and fullscreen iso camera mode application.
- Perspective and fullscreen iso camera mode interpolation.
- Camera ground-center lookup and movement.
- Visible ground-span lookup.
- Orthographic and perspective fit calculations.

`RTSSelectionSystem` keeps the public camera command methods as compatibility entry points while callers migrate, but it no longer keeps private one-line wrappers around camera math and mutation APIs.

## Thirty-Eighth Extraction Completed

`SelectedMoveOrderCommandSystem` now owns selected move-order command orchestration that was previously inside `RTSSelectionSystem`:

- clicked-unit rejection for move commands
- selected move-query consumption
- clicked cell validation handoff
- selected manual move goal assignment orchestration
- group path-request staggering
- move-order diagnostic formatting
- move-order command result reporting

`RTSSelectionSystem` still owns the pointer command entry point, HUD command mode/result forwarding, screen-marker event emission, and world-marker visibility request.

## Final Facade Decision

`RTSSelectionSystem` is not ready to delete or rename. It should remain as the temporary input/UI compatibility shell until these remaining surfaces are migrated:

- focused transport passenger disembark mutation
- public UI-facing focused-unit and selected-unit read/query methods
- public assistant/tutorial command entry points
- remaining pointer/camera/build-mode orchestration branches

## No-Managed-Shell Deletion Started

The deletion direction is now stricter: do not replace `RTSSelectionSystem` with another managed orchestration shell. Selection input and command flow must migrate to ECS request/result data.

Step 1 added data-only request components:

- `RtsSelectionInputRequestQueueComponent`
- `RtsSelectionPointerRequestElement`
- `RtsSelectionCommandIntentRequestElement`
- `RtsSelectionPointerRequestKind`
- `RtsSelectionCommandIntentKind`

No behavior migrated in this step. Runtime behavior should remain unchanged until pointer writers and ECS request processors are introduced.

Step 2 moved pointer/session state into ECS:

- `RtsSelectionInputStateComponent` stores drag origin/current positions, pointer UI suppression flags, pending release suppression, selection-hold state, queued move-order click state, live selection rectangle state, and last-known pointer position.
- `RtsSelectionInputStateCompositionSystemHelper` owns singleton creation/cache and ensures request buffers exist on the same entity.
- `RtsSelectionInputCompositionSystemHelper` remains a temporary compatibility accessor, but it no longer owns those values as managed fields.
- Follow-up deletion work moved normal pointer input runtime orchestration into `RtsSelectionRuntimeInputCompositionSystemHelper`, including queued move-order consumption, pointer press/hold/release branching, selection-hold triggering, live selection rectangle diffing, and rectangle request queueing. `RTSSelectionSystem` now builds a narrow context and delegates this tick slice while remaining shell behavior is retired.

Step 3 moved selection rectangle selection into ECS request processing:

- Live drag and committed rectangle selection now enqueue `RtsSelectionPointerRequestElement` with `SelectionRectUpdated` or `SelectionRectCommitted`.
- `RtsSelectionPointerRequestElement.SelectionFilter` carries all/soldiers/vehicles selection filters.
- `SelectionRectangleRequestCompositionSystemHelper` consumes rectangle requests, collects visible units, applies selected tags, updates selected move cache, applies focused/squad HUD handoff, and clears selected buildings when a unit selection is made.
- `RTSSelectionSystem` still triggers request processing synchronously during the compatibility phase, but it no longer owns the rectangle selection mutation algorithm.

Step 4 moved move command flow into ECS request/result processing:

- Move clicks now enqueue `RtsSelectionCommandIntentRequestElement` with `RtsSelectionCommandIntentKind.Move`.
- `RtsSelectionCommandResultElement` carries accepted/rejected command results, screen marker emission, and world marker visibility data.
- `SelectionMoveCommandRequestSystem` consumes move command requests, dispatches selected move execution through `SelectedMoveOrderCommandSystem`, and publishes command results.
- `RTSSelectionSystem` still applies HUD feedback and marker events during the compatibility phase, but it no longer directly calls selected move-order execution.

Step 5 moved attack command flow into ECS request/result processing:

- Attack clicks now enqueue `RtsSelectionCommandIntentRequestElement` with `RtsSelectionCommandIntentKind.Attack`.
- `RtsSelectionCommandIntentRequestElement.ExplicitAttackTargetMode` carries attack-target-mode context into the request processor.
- `RtsSelectionCommandResultElement` carries attack command result state plus attack marker world/screen payloads.
- `SelectionAttackCommandRequestSystem` consumes attack command requests, dispatches clicked attack execution through `AttackOrderCommandSystem`, and publishes command results.
- `RTSSelectionSystem` still applies HUD feedback and marker events during the compatibility phase, but it no longer directly calls clicked attack-order execution.

Step 6 moved transport boarding/disembark command flow into ECS request/result processing:

- Board-transport clicks now enqueue `RtsSelectionCommandIntentRequestElement` with `RtsSelectionCommandIntentKind.BoardTransport`.
- Focused transport exit commands now enqueue `RtsSelectionCommandIntentRequestElement` with `RtsSelectionCommandIntentKind.DisembarkTransport`.
- `RtsSelectionCommandResultElement` carries boarding marker cell/world/faction payloads for result consumers.
- `SelectionTransportCommandRequestSystem` consumes board/disembark requests, dispatches boarding through `TransportBoardingCommandSystem`, owns focused transport disembark mutation, and publishes command results.
- `RTSSelectionSystem` still applies marker events during the compatibility phase, but it no longer directly calls clicked boarding command execution or owns focused disembark mutation.

Step 7 moved focused-unit UI read models into ECS data:

- `FocusedUnitUiReadModelComponent` stores focused entity, label, description, health, capacity, ownership, vehicle, attack, status, world position, and portrait pose read-model data.
- `FocusedUnitPassengerUiReadModelElement` stores focused transport passenger rows.
- `FocusedUnitUiReadModelSystem` publishes the focused read model from selection state and `SelectionUiReadModelLookup` into an ECS singleton plus passenger buffer.
- `RTSSelectionSystem` compatibility getters now read focused UI data from that ECS read model instead of directly resolving labels, status, health, passenger rows, world position, or portrait pose.

Step 8 moved HUD feedback into ECS result data:

- `SelectionHudFeedbackElement` stores HUD selection, squad-selection, command-mode, command-result, and world-marker visibility feedback events.
- `SelectionHudFeedbackSystem` now publishes/consumes a `SelectionHudFeedbackQueueComponent` singleton buffer and flushes those events to `BattleHudGameplayBridge` at the shell edge.
- `RTSSelectionSystem` compatibility wrappers now enqueue HUD feedback events and flush them through `SelectionHudFeedbackSystem` instead of directly calling bridge-style feedback methods.
- Hot command request/result loops pre-create the feedback queue before reading command buffers so feedback publication does not introduce structural changes while command buffers are being consumed.

Step 9 moved camera input/control into ECS request data:

- `RtsCameraRequestElement` stores camera control requests for drag, pan, perspective zoom, fullscreen iso zoom, smooth focus, mode transitions, instant mode application, ground-center movement, and camera state flags.
- `RtsCameraStateComponent` mirrors camera state after request processing so later UI/shell callers can migrate away from direct managed state reads.
- `RtsCameraRequestSystem` owns request singleton creation, camera request processing, state mirroring, and shell-edge marker-hide callbacks for pan.
- `RTSSelectionSystem` still keeps public camera compatibility entry points and read-only camera geometry queries, but camera mutations now enqueue and flush ECS camera requests instead of calling `RtsCameraSystem` mutation APIs directly.

Step 10 moved M01 assistant/tutorial commands into ECS request/result processing:

- `M01AssistantCommandRequestElement` stores assistant select-runtime-entity, move-selected-to-cell, and attack-runtime-entity commands.
- `M01AssistantCommandResultElement` stores accepted/rejected assistant command results.
- `M01AssistantCommandRequestSystem` consumes assistant commands, applies selected tags, issues immediate move commands, dispatches attack orders through `AttackOrderCommandSystem`, and forwards command results through `SelectionHudFeedbackSystem`.
- `M01AssistantCommandRuntime` and `CommandIntentExecutor` now write requests and read results instead of calling `RTSSelectionSystem`; `AssistantContextProvider` now resolves typed command readiness from ECS world/loader state instead of requiring the selection shell.

Step 11 moved selection rectangle GUI drawing to the UI view boundary:

- `SelectionRectangleView` reads `RtsSelectionInputStateComponent` through `RtsSelectionInputStateCompositionSystemHelper` and draws only the current live rectangle.
- `RTSSelectionSystem` no longer owns selection rectangle GUI colors, the 1x1 GUI texture, `OnGui`, or GUI draw helpers.
- `GameplayRuntimeUpdateSystem.OnGui` now routes selection rectangle rendering through the view while keeping road-build GUI drawing on its existing path.

Step 12 started caller migration off the selection shell:

- `SelectionUiCommandSystem` now owns UI-facing selection command intent publication for select all, select soldiers, select vehicles, deselect all, hold, stop, attack/target-mode, and focused transport disembark requests.
- `MatchOverlayCommandControlsController` and `MainMenuPlayUI` no longer hold or call `RTSSelectionSystem`; they enqueue command intents through `SelectionUiCommandSystem`.
- `SelectionUiReadModelSystem` now owns UI-facing focused-unit, focused transport passenger, selected-unit list, and visible player-unit read calls.
- `SelectionUiCameraSystemHelper` now owns `MenuView` camera toggle state and fullscreen map camera focus commands through the ECS camera request boundary.
- `SelectionScreenMarkerUiSystemHelper` now owns UI-facing move/attack/hide screen-marker events.
- `MenuView` command buttons now use `SelectionUiCommandSystem`, its focused/selected read-model calls use `SelectionUiReadModelSystem`, its camera calls use `SelectionUiCameraSystemHelper`, and its marker hooks use `SelectionScreenMarkerUiSystemHelper`; it no longer holds or calls `RTSSelectionSystem`.
- `AssistantRuntimeBinding` no longer receives or forwards `RTSSelectionSystem`.
- `MissionCameraSystem` and `MissionStartupSystem` now focus the camera through `SelectionUiCameraSystemHelper` instead of `RTSSelectionSystem`.
- `BuildingGameplaySystem` now routes active-placement, production-focus, and building-selection camera focus callbacks through `SelectionUiCameraSystemHelper`.
- `SelectionBuildingInteractionSystem` now owns building-side selection clearing, transport boarding click checks, and building-target move-order compatibility. `BuildingGameplaySystem` and `BuildingGameplayCompositionSystemHelper` no longer depend on `RTSSelectionSystem`.
- `GameBootstrap`, `MenuStartupSystem`, and `GameplayRuntimeUpdateSystem` no longer accept or call `RTSSelectionSystem`; managed startup exposes narrow menu-bind, runtime-update, and dispose delegates while the remaining shell implementation is retired.
- Functional editor tests for battle HUD command feedback, missile launcher radar attack, and transport disembark/nearby boarding now exercise focused command, HUD feedback, selection input request, and transport command systems directly instead of constructing `RTSSelectionSystem`.
- Final deletion Step 3 moved camera runtime tick orchestration into `RtsSelectionRuntimeCameraSystemHelper`: play-state camera reset, fullscreen iso zoom/mode/pan, build-mode transition/pan, normal zoom, initial focus consumption, smooth focus ticking, camera request flushing, and public camera compatibility delegation are no longer implemented directly in `RTSSelectionSystem`.
- Final deletion Step 4 moved command result and marker flush orchestration into `RtsSelectionCommandResultFlushCompositionSystemHelper`: move/attack/transport command result draining, command-result HUD feedback forwarding, command-mode cleanup, move/attack screen marker requests, order marker projection for command results, world-marker visibility forwarding, and order-marker visibility ticking are no longer implemented directly in `RTSSelectionSystem`.
- Final deletion Step 5 moved focus and selection compatibility commands into `RtsSelectionFocusCommandCompositionSystemHelper`: external selection command request branching, clear focus, deselect all, select-all/select-filter request routing, direct focus entity compatibility, and select-runtime-entity compatibility are no longer implemented directly in `RTSSelectionSystem`.
- Final deletion Step 6 moved pointer target command dispatch into `RtsSelectionPointerTargetCommandCompositionSystemHelper`: clicked move intent queueing, clicked attack intent queueing, clicked board-transport intent queueing, clicked focus dispatch, clicked unit/cell resolution, boardable-transport click tests, and building-target move compatibility are no longer implemented directly in `RTSSelectionSystem`.
- Final deletion Step 7 moved production startup wiring off the retired `RTSSelectionSystem` type and production code no longer constructs, stores, or calls `RTSSelectionSystem`.
- Final deletion Step 8 moved architecture/test reads off the deleted `Assets/Game/Scripts/Systems/RTSSelectionSystem.cs` artifact and added a contract guard that the retired source/type must not be restored.
- Final deletion Step 9 removed the architecture allowance that described `RTSSelectionSystem` as temporary compatibility debt and deleted `SelectionRuntimeUpdateSystem.cs`. Runtime selection phases are now composed explicitly by startup instead of hidden behind a monolithic `Update()` shell.

## Recommended Next Slices

1. Keep `SelectionRuntimeContextSystem.cs` deleted.
2. Keep selection runtime composition on the owning narrow ECS/UI systems.
