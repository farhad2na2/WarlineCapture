# Match HUD Explicit Command Mode Plan

## Summary

The current match input behavior issues a move order whenever selected units click empty map space. The design target is explicit command mode:

- Select unit or group.
- Click `Move`.
- HUD enters Move command mode and shows destination feedback.
- Next valid map click issues a move order.
- Double-clicking a valid Move destination issues the order and keeps Move mode armed for more destinations.
- Invalid destinations show feedback and keep or clear mode according to command policy.
- Normal map clicks outside command mode focus/select units, board transports, attack only when explicitly armed, or do nothing; they must not move selected units by default.

This plan keeps the flow aligned with the current ECS request/result architecture. UI may emit command intent requests. Runtime input may emit pointer target requests. Gameplay systems consume requests, validate, mutate ECS orders, and publish command results. UI feedback consumes read models/results and does not own gameplay policy.

## Current State

- `RtsSelectionRuntimeInputSystem.HandlePointerReleased` currently falls through to `QueueMoveOrder` when a click is not attack, transport, or focus.
- `RtsSelectionPointerTargetCommandCompositionSystemHelper.RequestMoveOrder` already queues and processes move command requests, but it is called by the automatic empty-click path.
- `SelectionMoveCommandRequestSystem` already consumes `RtsSelectionCommandIntentKind.Move` requests and uses `SelectedMoveOrderCommandSystem` / `UnitMoveOrderSystem`.
- `SelectionUiCommandSystem` exposes Select, Hold, Stop, Attack-target, etc., but has no explicit `RequestMoveCommandMode` yet.
- `MatchOverlayCommandControlsView` has serialized Select, Build, Hold, Stop references, but no explicit Move or Attack button references.
- `BattleHudRuntimeFeedbackSystem` can show command instructions such as `Choose destination`.

## Design Principles

- No hierarchy searching in shipped command runtime. Add explicit serialized references for Move/Attack buttons.
- No direct unit order mutation from UI button code. Buttons only enqueue ECS command-mode intents.
- No direct camera/input mutation from UI beyond the existing click-suppression guard.
- Command mode state should be ECS-readable and command-generic, not a hard-coded Move boolean.
- Move V1 should use the same command-mode structure that later supports Attack, Stop, Hold, Build, Scan, Support, Destroy, and other buttons.
- Pointer processing decides whether a click is a command target only by consulting command mode, not by checking selected-unit existence alone.
- Command result feedback is centralized through existing HUD feedback systems.

## Target Architecture

### Command Mode State

Introduce an explicit tactical command mode state at the selection command boundary.

Preferred minimal shape:

- Extend `RtsSelectionInputStateComponent` or add a small command-state component on the existing input/command singleton.
- Store:
  - `ActiveCommandMode`
  - `ArmedFrame`
  - `OneShot`
  - optional `RequiresWorldTarget`

For V1:

- `Move` is an armed one-shot world-target command.
- A Move destination double-click promotes Move into persistent world-target mode until another command, selection mode, or clear action exits it.
- `Attack` can reuse the same model later, replacing or wrapping the existing explicit attack target boolean.
- `Stop` and `Hold` are immediate commands and should not wait for a map click.

### UI Command Flow

`MatchOverlayCommandInputUiSystemHelper` should bind:

- Move button -> `SelectionUiCommandSystem.RequestMoveCommandMode()`
- Attack button -> later `RequestAttackCommandMode()` or existing attack fallback through the same command mode boundary
- Stop button -> immediate `RequestStop()`
- Hold button -> immediate `RequestHoldPosition()`

The UI click sequence should still call the input click suppression guard so the button release does not also become a world click.

### Runtime Pointer Flow

Normal pointer release should branch in this order:

1. UI blocking / guard suppression.
2. Selection mode rectangle/focus behavior.
3. Active world-target command mode:
   - `Move`: issue move order to clicked destination.
   - `Attack`: issue attack to clicked entity/target when implemented.
   - invalid target: publish feedback.
   - accepted one-shot command: clear mode.
4. No active command mode:
   - Click unit -> focus/select.
   - Click boardable transport -> board only if policy says this is an implicit interaction.
   - Empty map -> no move order.

This removes the automatic `QueueMoveOrder` fallback.

### Results And Feedback

Move command results should continue to flow through:

- `SelectionMoveCommandRequestSystem`
- `RtsSelectionCommandResultFlushCompositionSystemHelper`
- `SelectionHudFeedbackSystem`
- `BattleHudRuntimeFeedbackSystem`

Accepted Move:

- show move marker
- clear one-shot Move mode and hide `FeedbackPanel`
- keep persistent Move mode selected after a destination double-click and restore `Choose destination`

Rejected Move:

- show reason in `FeedbackPanel`
- clear mode for hard blockers such as no selection
- optionally keep mode for destination-only invalid cases if the design wants retry; V1 can clear on all results for simple, predictable behavior

### Future Commands

Use the same state machine for:

- `Attack`: active target mode, entity/world-target click required.
- `Stop`: immediate selected-unit command, no map click.
- `Hold`: immediate selected-unit command, no map click.
- `Build`: sticky command mode owned by build placement systems.
- `Scan`, `Support`, `Destroy`: command intents with clear target policy per command.

## Implementation Steps

1. [x] Add this plan document.
2. [x] Add explicit command-mode state helpers.
   - Extend `RtsSelectionInputStateComponent` or add a command-state component.
   - Add `ArmCommandMode`, `ClearCommandMode`, `TryGetActiveCommandMode`, and `HasWorldTargetCommandMode` helpers on `RtsSelectionInputSystem`.
   - Keep the API command-generic; do not add `IsMoveMode` as the only concept.
3. [x] Add Move UI command intent.
   - Add `RtsSelectionCommandIntentKind.EnterMoveTargetMode` or reuse `Move` only for target clicks and add a separate mode intent.
   - Add `SelectionUiCommandSystem.RequestMoveCommandMode()`.
   - Call `CaptureUiClickSequence()` before queuing it.
4. [x] Bind Match HUD Move button without rebuilding the prefab.
   - Add serialized `moveButton` and later `attackButton` fields to `MatchOverlayCommandControlsView`.
   - Directly wire the existing prefab references, or use existing serialized tab array if already enough.
   - Add input binding in `MatchOverlayCommandInputUiSystemHelper`.
5. [x] Process Enter Move Mode in the focus/command boundary.
   - Extend `RtsSelectionFocusCommandCompositionSystemHelper` external command processing.
   - Validate that selected movable units exist before arming.
   - On no selection, publish `NoSelection` feedback and do not arm.
   - On success, arm `TacticalCommandMode.Move`, show `Choose destination`, and suppress the button click release.
6. [x] Gate map-click move issuing behind active command mode.
   - Remove the empty-click `QueueMoveOrder` fallback from `RtsSelectionRuntimeInputSystem`.
   - When active mode is Move, issue the move order to the clicked destination.
   - Empty map click with no active command mode should not enqueue Move.
7. [x] Clear command mode from command results.
   - Accepted Move clears active one-shot mode.
   - Rejected Move clears or preserves based on the chosen retry policy; V1 clears for simplicity.
   - Selection clear, deselect all, build mode, and explicit selection mode also clear Move mode.
8. [x] Update command button visual feedback.
   - Move button selected sprite/state while Move mode is armed.
   - Move button neutral when mode clears.
   - Disabled/no-selection state should show feedback rather than enqueueing a destination wait.
9. [ ] Extend tests.
   - Button click arms Move mode and shows `Choose destination`.
   - Selected unit + empty map click without Move mode does not enqueue a move order.
   - Move mode + valid map click enqueues exactly one move order and clears mode.
   - Move mode + destination double-click enqueues the order and keeps Move mode selected.
   - Move mode + invalid target publishes feedback and does not mutate unit order.
   - Stop/Hold remain immediate and do not require a map click.
   - UI click suppression prevents the Move button release from issuing a world command.
10. [ ] Runtime validation.
    - In Play Mode, select a unit and click empty terrain: no movement.
    - Click Move, then terrain: unit moves.
    - Click Move with no selection: feedback panel appears with no-selection reason.
    - Click Stop/Hold: immediate command behavior remains intact.

## Test Plan

- EditMode:
  - `RtsSelectionInputSystemTests` for command-mode state helpers and click gating.
  - `WarlineCaptureUiCommandExchangeTests` or a new focused command-mode test for UI command requests.
  - `BattleHudRuntimeFeedbackSystemConnectionTests` for HUD mode feedback and clear behavior.
  - `WarlineCaptureUiShellTests` for serialized Move/Attack button references if new fields are added.
- Runtime smoke:
  - Select a soldier.
  - Click map without Move.
  - Confirm no move order, no move marker, and selection remains.
  - Click Move.
  - Confirm feedback says `Choose destination`.
  - Click valid destination.
  - Confirm move order, marker, command mode clear, and feedback hidden.
  - Double-click valid destination.
  - Confirm move order, marker, Move button remains selected, and feedback returns to `Choose destination`.

## Risks

- Existing implicit attack/transport behavior may still fire before focus. The move fix should not accidentally break those flows; they need explicit policy review later.
- Existing explicit attack mode uses a separate boolean. It should eventually migrate into the shared command-mode state to prevent two competing command states.
- Command tab visuals currently toggle independently from gameplay command mode. The implementation must make command result/mode state the source of truth for selected visuals.
- Direct prefab wiring must be minimal. Do not run the prefab builder for this task unless explicitly requested.

## Progress

- [x] Plan documented.
- [x] Step 2: command-mode state helpers.
- [x] Step 3: Move UI command intent.
- [x] Step 4: Move button binding.
- [x] Step 5: Enter Move Mode processing.
- [x] Step 6: Map-click move gating.
- [x] Step 7: Result-based mode clearing.
- [x] Step 8: Button visual feedback.
- [x] Step 9: Tests.
- [ ] Step 10: Runtime validation.

## Implementation Notes

- Move is now armed by `RtsSelectionCommandIntentKind.EnterMoveTargetMode`.
- Actual destination clicks still use `RtsSelectionCommandIntentKind.Move`.
- Empty terrain clicks without an active world-target command mode no longer enqueue move orders.
- A second Move destination click near the previous destination within `RtsSelectionInputSystem.MoveTargetDoubleClickSeconds` keeps Move mode persistent after the command result.
- Rejected Move-mode requests clear command visuals before showing the rejection text so `FeedbackPanel` stays visible.
- Attack remains on the existing path for now and should be migrated into the shared command-mode state in a later pass.
