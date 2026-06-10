# Match HUD Board Command Mode Plan

## Summary

Boarding should be an explicit targeted command mode, consistent with Move, Attack, and Scan:

- Select one or more boardable player units, usually soldiers.
- Click `Board`.
- HUD enters Board Targeting Mode.
- Board button stays visually selected while the mode is active.
- Feedback tells the player to tap a transport.
- Valid friendly transports are highlighted.
- Clicking a valid transport issues boarding orders through ECS request systems.
- Reverse order is also supported: select a transport first, click `Board`, then tap or box-select eligible soldiers to load into that selected transport.
- Clicking another command exits Board mode and updates button visuals to the new active command.

This plan covers both boarding directions:

- Passenger-first: selected soldiers choose a transport target.
- Transport-first: selected transport chooses passenger targets.

The older selected-transport "call nearest soldiers to board this transport" behavior tracked in `Design/Architecture/match_hud_selected_squad_panel_command_plan.md` should be refined into the transport-first targeting flow below. It must not silently auto-pull units unless the player has explicitly chosen an auto-load shortcut later.

## Architecture Rules

- Follow `Design/Architecture/gameplay_solid_ecs_contract.md`.
- UI `*View` classes may hold serialized references and apply visual state only.
- UI button clicks must enqueue ECS/data requests through existing command boundaries.
- Gameplay validation, seat reservation, pathing, and passenger mutation belong in ECS `*System` code.
- Do not add runtime hierarchy path lookup, `Object.Find*`, `GameObject.Find`, `Camera.main`, static mutable view registries, direct gameplay mutation from UI, or unconditional runtime `Debug.Log*`.
- Button selected visuals must be driven from command-mode state/read-models, not local UI-only booleans.
- Feedback must flow through the existing command feedback path.

## User Behavior Contract

### Passenger-First Happy Path

1. Player selects one or more boardable soldiers.
2. `Board` becomes clickable.
3. Player clicks `Board`.
4. HUD enters Board Targeting Mode:
   - Board button uses the selected sprite.
   - Other command buttons are neutral.
   - Feedback panel shows `TAP TRANSPORT`.
   - Valid friendly transports show boarding target rings.
5. Player clicks a valid friendly transport.
6. ECS validates and reserves seats.
7. Selected soldiers receive boarding orders and move to the transport boarding point.
8. Board mode exits after the order is accepted.
9. Selection remains active.
10. Boarding completion updates passenger state and selected-panel read models.

### Transport-First Happy Path

1. Player selects one friendly transport vehicle or transport aircraft.
2. `Board` becomes clickable if the transport has free capacity.
3. Player clicks `Board`.
4. HUD enters Board Passenger Targeting Mode:
   - Board button uses the selected sprite.
   - Other command buttons are neutral.
   - Feedback panel shows `TAP UNITS TO BOARD`.
   - Valid nearby/player boardable soldiers show boarding candidate rings.
   - The selected transport can show a subtle transport anchor ring.
5. Player taps one eligible soldier or drag/box-selects several eligible soldiers.
6. ECS validates the selected transport and passenger candidates.
7. Seats are reserved immediately for accepted passengers.
8. Accepted passengers receive boarding orders and move to the selected transport boarding point.
9. Board mode exits after the order is accepted.
10. The transport remains selected so the selected panel can show passenger count/portraits.

### Transport-First Optional Shortcut

- If a transport is selected and the player double-clicks `Board`, a later pass may auto-pick nearest eligible soldiers up to free capacity.
- V1 should prefer explicit passenger targeting because it is easier to understand and avoids surprising unit movement.
- If the shortcut is implemented, feedback must say `Calling nearest units to board.` and the accepted passengers must be visible in feedback/read-models.

### Cancellation And Switching

- Clicking another command while Board mode is active clears Board mode.
- The newly clicked command becomes selected if it is a targeting mode.
- Immediate commands such as Hold or Stop clear Board mode and then execute.
- Clicking Board again while Board mode is already active cancels Board mode and returns to neutral command state.
- Clearing selection or entering build placement clears Board mode.

### Rejection Feedback

- No selected unit: `Select units to board.`
- Selected unit cannot board: `Selected unit cannot board.`
- Selected transport has no free capacity: `Transport is full.`
- Selected transport cannot carry passengers: `This vehicle cannot carry passengers.`
- Tap terrain while passenger-first Board mode is active: `Tap a transport.`
- Tap terrain while transport-first Board mode is active: `Tap units to board.`
- Target cannot carry passengers: `Target cannot carry passengers.`
- Target cannot board transport: `That unit cannot board.`
- Enemy or neutral target: `Cannot board that vehicle.`
- Enemy, neutral, civilian, or non-commandable passenger target: `Cannot board that unit.`
- Transport is full: `Transport is full.`
- No reachable boarding point: `No path to boarding point.`
- Accepted command: `Boarding transport.`
- Transport-first accepted command: `Loading transport.`
- Boarding complete: `Unit onboard.`
- Partial capacity: `Some units could not board.`

## Target ECS Flow

### UI Request

- Board button calls a method on the existing UI command boundary, for example `SelectionUiCommandSystem.RequestBoardTargetMode()`.
- The request captures/suppresses the UI click release so the same click cannot also become a world click.
- The request enqueues a command intent, for example `RtsSelectionCommandIntentKind.EnterBoardTargetMode`.
- The command system chooses the Board targeting direction from current selection:
  - selected passenger-capable units and no focused transport: passenger-first;
  - selected transport with free capacity: transport-first;
  - mixed selected passengers plus exactly one selected transport: immediate explicit request can be accepted only if the selected transport is unambiguous, otherwise enter passenger-first target mode.

### Command Mode State

- Add `Board` to the shared tactical command mode state used by Move and Attack.
- Board is a world-target command mode with a direction/submode:
  - `PassengerToTransport`
  - `TransportToPassenger`
- Board mode is one-shot by default: accepted boarding clears the mode.
- Invalid target clicks keep Board mode active so the player can retry.
- Switching command modes clears the previous active mode.
- Transport-first Board mode stores the selected transport entity as the locked source. If that transport becomes invalid, full, deselected by a hard selection clear, or destroyed, Board mode exits with feedback.

### Validation Boundary

- The focus/command system validates the selected source side before arming Board mode.
- Passenger-first validation checks that at least one selected player unit can board. It does not choose the transport yet.
- Transport-first validation checks that the focused/selected transport can carry passengers and has free capacity. It does not choose passengers yet.
- On valid selection, it arms `Board` mode with the correct direction and publishes direction-specific feedback.
- On invalid selection, it rejects and leaves command mode neutral.

### Target Click Handling

- Runtime pointer release checks active command mode before default focus/select behavior.
- If passenger-first Board mode is active:
  - terrain click publishes `Tap a transport` and remains in Board mode;
  - invalid entity click publishes the relevant rejection and remains in Board mode;
  - valid transport click enqueues a boarding request.
- If transport-first Board mode is active:
  - terrain click publishes `Tap units to board` and remains in Board mode;
  - invalid entity click publishes the relevant rejection and remains in Board mode;
  - valid passenger click enqueues a boarding request for that passenger and the locked selected transport;
  - drag/box-select while Board mode is active may enqueue multiple passenger candidates instead of replacing selection, if the existing input stack can support this without broad churn.
- Normal map clicks without Board mode must not implicitly board.

### Boarding Request

- A request system consumes the target click and resolves the fixed source side plus target side.
- Passenger-first request uses selected passengers plus clicked transport.
- Transport-first request uses locked selected transport plus clicked/boxed passenger candidates.
- It validates:
  - passenger entities are still selected, alive, player-controlled, and boardable;
  - transport is friendly/player-controlled, alive, capacity-bearing, and not full;
  - transport accepts the passenger type;
  - boarding point is reachable or pickup behavior is available.
- It reserves seats immediately when accepted.
- It emits movement/boarding orders via existing transport systems such as `TransportBoardingCommandSystem`, `SelectionTransportCommandRequestSystem`, and the narrow transport boarding systems.
- Actual passenger mutation remains in the transport boarding ECS systems.

### Target Rings

- While passenger-first Board mode is active, a read-model/projection system exposes valid friendly transport targets.
- While transport-first Board mode is active, a read-model/projection system exposes valid player passenger candidates for the locked selected transport.
- Marker/ring rendering uses the existing runtime marker path.
- Rings are hidden immediately when Board mode exits.
- Invalid targets are not highlighted as valid.

## Progress Tracker

### Phase 1: Audit And Contract

- [x] Create this plan document.
- [x] Audit current Move/Attack command-mode state and identify where `Board` should be added.
- [x] Audit existing selected-panel `Board` behavior and replace silent auto-call behavior with explicit transport-first Board targeting unless a deliberate shortcut is approved.
- [x] Audit transport command/request systems for the narrow owner of passenger-first and transport-first board requests.

### Phase 2: Command Mode State

- [x] Add `Board` to the shared tactical command mode enum/state.
- [x] Add Board direction/submode state for `PassengerToTransport` and `TransportToPassenger`.
- [x] Store the locked selected transport entity for transport-first mode.
- [x] Add command-state helpers for arming, clearing, and querying Board mode.
- [x] Ensure Board mode is cleared by selection clear, build placement, and other command-mode transitions.
- [x] Ensure clicking Board while already active toggles Board mode off.
- [ ] Ensure transport-first Board mode exits if the locked transport becomes invalid, destroyed, full, or no longer commandable.

### Phase 3: UI Wiring And Visuals

- [x] Add or reuse serialized Board button references on the relevant Match HUD `*View`.
- [x] Add `SelectionUiCommandSystem.RequestBoardTargetMode()`.
- [x] Bind Board button clicks to the request boundary without gameplay policy in the view.
- [x] Drive Board selected sprite from command-mode read-model state.
- [x] Verify Board deselects visually when Move, Attack, Scan, Build, Hold, Stop, Return, Destroy, or another command is clicked through shared command-mode clear paths.
- [x] Verify Board selected visual works for both passenger-first and transport-first modes at the shared command-mode state level.

### Phase 4: Feedback

- [x] Add passenger-first Board command-mode feedback: `TAP TRANSPORT`.
- [x] Add transport-first Board command-mode feedback: `TAP UNITS TO BOARD`.
- [x] Add no-selection rejection feedback.
- [x] Add non-boardable-selection rejection feedback.
- [x] Add invalid selected transport and full selected transport rejection feedback through command rejection paths.
- [x] Add invalid target, wrong faction, full transport, no-path, accepted, complete, and partial-capacity feedback messages through existing transport command result paths.
- [ ] Verify feedback icon/severity follows the command feedback panel rules.

### Phase 5: Targeting And Markers

- [x] Route Board-mode world clicks before normal map click behavior.
- [x] Prevent terrain clicks in Board mode from issuing Move orders.
- [x] Add valid transport target read-model/projection for Board mode.
- [x] Add valid passenger target read-model/projection for transport-first Board mode.
- [x] Show valid friendly transport target rings while Board mode is active.
- [x] Show valid passenger candidate rings while transport-first Board mode is active.
- [x] Hide rings when Board mode exits or switches.

### Phase 6: Boarding Request Execution

- [x] Add passenger-first and transport-first Board target request kinds, or reuse existing transport requests only if semantics match exactly.
- [x] Collect selected passenger candidates through ECS selection data.
- [x] Resolve locked selected transport for transport-first Board mode through ECS command-mode state.
- [x] Collect clicked passenger candidates for transport-first Board mode.
- [x] Collect boxed passenger candidates for transport-first Board mode.
- [x] Validate target transport through transport query/rule/capacity systems.
- [x] Validate transport-first passenger targets through transport query/rule/capacity systems.
- [x] Reserve seats on accepted command through pending boarding order accounting.
- [x] Issue move-to-boarding-point and `UnitTransportBoardingTarget` setup through the existing transport command boundary.
- [x] Preserve helicopter/aircraft rule: boarding requires landed or valid pickup behavior.
- [x] Preserve APC/ground transport boarding point/radius behavior.
- [x] Keep selected units selected after the command is accepted.

### Phase 7: Tests And Validation

- [x] Add focused tests for Board button request queueing and click suppression.
- [ ] Add tests for no-selection and non-boardable-selection rejection.
- [x] Add tests for selected transport entering transport-first Board mode.
- [ ] Add tests for selected full/non-transport vehicle rejecting Board mode.
- [ ] Add tests that Board selected sprite follows command state.
- [ ] Add tests that clicking another command clears Board visuals.
- [ ] Add tests that terrain click in Board mode does not issue Move.
- [ ] Add tests that valid transport click emits a boarding request and clears one-shot Board mode.
- [x] Add tests that valid passenger command request with locked transport stores transport/passenger data for ECS processing.
- [ ] Add tests that invalid/full transport keeps Board mode active and shows feedback.
- [ ] Add tests that invalid passenger target keeps transport-first Board mode active and shows feedback.
- [ ] Run `git diff --check`.
- [ ] Run focused Unity EditMode validation.
- [x] Run Unity compile validation in the documented shadow project.
- [ ] Run a runtime smoke test in the main project or documented shadow project.

## Progress Notes

- 2026-06-10: Implemented explicit Board command mode, passenger-first and transport-first submodes, UI request routing, selected visual state, target click routing, transport-first clicked-passenger request execution, and transport command result handling. Synced `Assets/Game/Scripts`, `Assets/Tests/Editor`, and `Assets/Tests/PlayMode` into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` to remove stale shadow drift, then fixed the one real compile error in `TransportBoardingCommandSystem`. Shadow Unity batch compile passed with `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -logFile /private/tmp/warline-board-command-compile.log`.
- 2026-06-10: Added Board target preview projections/rings by reusing the existing runtime marker pool with source-aware predicates. Passenger-first mode highlights boardable friendly transports; transport-first mode highlights eligible player passenger candidates for the locked selected transport. Added focused input tests for Board request queueing and Board mode state storage. Shadow Unity compile passed again after syncing changed files. Focused EditMode test command exited successfully but did not emit `/private/tmp/warline-board-command-editmode-results.xml`, so test-result validation remains open.
- 2026-06-10: Implemented transport-first rectangle boarding. Dragging while a selected transport is in Board mode now shows the existing selection rectangle without entering normal selection mode, collects visible eligible passenger candidates in the rectangle, queues one explicit transport/passenger command per candidate, and processes them through the existing transport command result path. Added focused queue/source tests for explicit passenger board requests. Shadow Unity compile passed. Focused EditMode command still exits cleanly without emitting XML results, so focused test-result validation remains open.

## Runtime Smoke Checklist

- [ ] Select a soldier and click Board: Board button selected, feedback says `TAP TRANSPORT`.
- [ ] Select a transport and click Board: Board button selected, feedback says `TAP UNITS TO BOARD`.
- [ ] Click Move while Board is active: Board deselects, Move selects.
- [ ] Click Attack while Board is active: Board deselects, Attack selects.
- [ ] Click Hold while Board is active: Board deselects, Hold executes immediate command.
- [ ] Click terrain while Board is active: no movement order, feedback remains Board-specific.
- [ ] Click a full transport: error feedback, Board remains selected.
- [ ] Click a valid APC/transport aircraft: command accepted, Board deselects, units move to board.
- [ ] With selected transport Board mode active, click a valid soldier: command accepted, Board deselects, soldier moves to board selected transport.
- [ ] With selected transport Board mode active, click an invalid/civilian/enemy/non-boardable unit: error feedback, Board remains selected.
- [ ] Boarding complete: passenger state/read-model updates.

## Open Decisions

- Should the selected transport remain selected after transport-first Board is accepted? Recommended: yes.
- After passenger-first boarding is accepted, should selection switch to the transport automatically or keep the passenger selection context until it becomes invalid?
- Should multiple selected soldiers auto-split across several transports in passenger-first mode, or only board the clicked transport up to capacity and reject the remainder?
- In transport-first mode, should drag/box-select be supported in V1, or should V1 only support clicking one passenger at a time?
- For transport aircraft, should Board V1 require the aircraft to be landed, or should it allow automatic pickup landing near the selected passengers?
