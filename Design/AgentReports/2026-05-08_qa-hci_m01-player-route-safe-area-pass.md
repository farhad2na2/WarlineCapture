Lane:
QA/HCI

Task:
M01 player-route and safe-area Gate 4 pass after the integrated readiness review.

Files changed:
- Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md

Contracts touched:
- Design/AgentTasks/qa-hci_current.md: completed the active QA/HCI player-route/safe-area report without editing AgentTasks.
- Design/AgentTasks/M01_CRITICAL_PATH.md: Gate 4 remains not accepted because safe-area/device or route-driven capture evidence is still missing.
- Design/M01_FirstContact_Production_Contract.md: validated the automated route coverage for M01 runtime spawn, selection, attack, result readiness, survival guard, and build rejection.
- Design/FTUE_And_Command_Assistant_Design.md, Design/AssistantPanel_M01_Implementation_Contract.md, and Design/AssistantRuntime_M01_Wiring_Plan.md: validated assistant typed runtime binding, player-input release, Stop behavior, and result-flow Stop behavior through focused EditMode tests.
- No runtime API, prefab path, route id, mission id, data schema, source design contract, asset row, or production source file was changed.

User-visible behavior:
No runtime behavior changed. This pass reran focused QA/HCI validation and classifies the remaining Gate 4 blocker: the project still lacks route-driven 1920x1080/2400x1080 captures with explicit safe-area/device evidence.

Validation run:
- Unity PlayMode, QA workspace: `Chapter01M01PlayModeValidationTests`, results `/private/tmp/warlinecapture-qa-hci-player-route-playmode-results.xml`, log `/private/tmp/warlinecapture-qa-hci-player-route-playmode.log`.
- Unity EditMode, QA workspace: `WarlineCaptureUiAssistantRuntimeBindingTests`, results `/private/tmp/warlinecapture-qa-hci-assistant-runtime-results.xml`, log `/private/tmp/warlinecapture-qa-hci-assistant-runtime.log`.
- Unity EditMode, QA workspace: `WarlineCaptureUiShellTests`, results `/private/tmp/warlinecapture-qa-hci-shell-results.xml`, log `/private/tmp/warlinecapture-qa-hci-shell.log`.
- Reviewed accepted UI capture matrix: `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`.
- Reviewed contact sheet: `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_CaptureMatrix_ContactSheet.png`.
- Reviewed sprite-renderer evidence: `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`.
- Verified capture dimensions with `sips`: all eight 1920x1080 captures are 1920x1080, all eight 2400x1080 captures are 2400x1080, contact sheet is 1920x976, and the close sprite-renderer capture is 1920x1080.
- Scanned QA logs for `NullReferenceException`, `RenderTexture.Create failed`, `EntitiesGraphicsSystemUtility`, `AIProduction`, `AIBuild`, `AISquad`, `FreezeDetect`, `PerfDiag`, `RuntimeCitySpawner`, leak warnings, Animator warnings, exceptions, errors, and failures.

Validation result:
Needs fixes for final Gate 4. The equivalent player-route automation passed: `Chapter01M01PlayModeValidationTests` passed 3/3 and covered Game scene M01 runtime spawn/camera anchoring, command squad selection, attack order, hostile damage, result readiness, command squad survival guard, and M01 build rejection feedback. Assistant/runtime validation passed 7/7 and covered live service presentation, typed Show/Do/Stop routing, takeover ownership, player-input release, result explanation Stop leaving `POP-05_MissionResult` open, and accepted button/panel mounting. Shell validation passed 15/15 and covered route instantiation, match result popup flow, modal overlay behavior, route navigation, and screen prefab structure.

Gate 4 still cannot be accepted because this pass did not produce route-driven screenshots for the eight required states and did not validate real or simulated safe-area cutouts. The existing 1920x1080 and 2400x1080 captures remain accepted prefab/editor evidence, not player-route capture evidence. No human/device pass was run.

Known gaps:
- No route-driven screenshots were produced for match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, or result popup.
- No real device, Android build, simulator safe-area, notch, or rounded-corner validation was run.
- No human touch/camera manipulation pass was completed.
- Existing PlayMode route automation proves route logic and ECS outcome behavior, but not real touch ergonomics, camera drag/pinch behavior, or capture-state timing.
- Current generated tactical/unit art remains review evidence only, not final approval. Final atlas/config packaging, hostile non-color readability treatment, and `vfx.unit.destroyed.small` remain open.
- Logs still show editor/tooling noise: Unity licensing reconnect messages that recover, Xcode plist read warnings, ADB/usbmuxd shutdown messages, legacy `Animator is not playing an AnimatorController` warnings in the PlayMode route log, preview-scene leak warnings, and persistent allocation leak warnings.

Cross-lane impacts:
- QA/HCI keeps active balance QA blocked until route-driven capture and safe-area/device evidence exists.
- UI should provide or support route-driven capture/safe-area tooling if PM wants Gate 4 closed without a manual device pass.
- Gameplay does not have a new code blocker from this pass. Re-engage Gameplay only if route-driven/manual validation reproduces freezes, input stalls, runtime exceptions, severe FPS drops, or gameplay-owned log spam.
- Support/FTUE does not have a new code blocker from this pass. Re-engage Support/FTUE only if a route-driven/manual pass shows ARIA recommendation, ownership, Stop, or result explanation behavior is misleading.
- Art/design final approval remains separate from Gate 4 player-route validation.

Next recommended task:
Produce route-driven 1920x1080 and 2400x1080 capture evidence with explicit safe-area/device assumptions, or run a real device/manual player-route pass and capture the same eight states. Do not start active balance QA until that pass has no blocker findings.

## Current balance-QA gate status

Blocked. The automated route and assistant/runtime tests are green, but balance QA would still be invalid if safe-area clipping, real touch/camera behavior, route-capture timing, or player-operated assistant ownership release fails.

## QA Unity workspace smoke checks run

- `Chapter01M01PlayModeValidationTests`: Passed 3/3.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: Passed 7/7.
- `WarlineCaptureUiShellTests`: Passed 15/15.
- Initial Unity invocations with `-quit` exited without Test Runner XML and were discarded as invalid evidence. The accepted evidence is from the reruns without `-quit`, matching the known-good Test Runner pattern.

## Performance/freeze/log-health findings

- No `NullReferenceException`, `RenderTexture.Create failed`, `EntitiesGraphicsSystemUtility`, `AIProduction`, `AIBuild`, `AISquad`, `FreezeDetect`, `PerfDiag`, or `RuntimeCitySpawner` issue reproduced in the accepted QA PlayMode route log.
- The old `RuntimeCitySpawner=1350.3ms` hitch did not reproduce.
- Remaining log noise: recovered Unity licensing reconnect messages, Xcode plist read warnings, ADB/usbmuxd shutdown messages, legacy Animator warnings, preview-scene leak warnings, and persistent allocation leak warnings.
- No FPS/thermal/device timing or manual input latency metrics were collected.

## New HCI risks introduced by latest handoffs

- The accepted prefab/editor matrix can remain visually correct while route-driven capture timing or real touch/camera behavior fails.
- The 2400x1080 captures are readable but do not prove safe-area cutout handling.
- Assistant Stop and result-flow Stop are test-proven at service/prefab level, but still need player-route screenshot or manual evidence in the result state.

## Waiting/blocker ownership fields

Waiting on lane:
QA/HCI, with UI support if capture tooling is required.

Waiting on exact file/report/asset/command:
Route-driven or device/manual evidence for `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md` covering the eight required states at 1920x1080 and 2400x1080 with explicit safe-area/device assumptions.

Owner of next action:
QA/HCI owns the validation pass. UI owns any missing route-driven capture/safe-area tooling if the existing QA-accessible tooling is insufficient.

Can my lane still continue fallback work? no. QA/HCI ran the available focused route automation, assistant/runtime validation, shell validation, capture-dimension checks, visual evidence review, and log scan. The remaining blocker requires route-driven capture tooling or a device/manual safe-area setup.

## QA findings

### QAHCI-G4-006: Route-driven capture evidence is still missing

- Severity: Blocker
- Affected lane: gameplay / UI / QA-HCI
- Reproduction steps:
  1. Run the current QA/HCI pass using available automation.
  2. Review `/private/tmp/warlinecapture-qa-hci-player-route-playmode-results.xml`.
  3. Review `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/`.
- Expected: Gate 4 evidence includes route-driven captures of match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup at 1920x1080 and 2400x1080.
- Actual: PlayMode route automation passes and prefab/editor captures exist, but no screenshots are captured from the running player route.
- Blocks next milestone: yes. Blocks active balance QA and Gate 4 acceptance.
- Recommended owner: QA/HCI for validation; UI if route-driven capture tooling is required.

### QAHCI-G4-007: Safe-area/device behavior remains unverified

- Severity: Blocker for mobile Gate 4 acceptance
- Affected lane: UI / QA-HCI
- Reproduction steps:
  1. Review the accepted UI matrix report.
  2. Confirm it states safe area was not simulated.
  3. Run the current QA/HCI pass; no device or simulated safe-area command is available in the pass.
- Expected: 20:9/mobile validation states real or simulated safe-area insets/cutout assumptions and proves HUD, minimap, assistant panel, command controls, and result popup are not clipped.
- Actual: 2400x1080 captures are readable, but no safe-area inset, notch, or rounded-corner evidence exists.
- Blocks next milestone: yes, unless PM explicitly waives safe-area/device validation for M01.
- Recommended owner: QA/HCI with UI support.

### QAHCI-G4-008: Player-route automation passes but does not prove human touch ergonomics

- Severity: Major
- Affected lane: gameplay / UI
- Reproduction steps:
  1. Run `Chapter01M01PlayModeValidationTests`.
  2. Inspect test coverage in `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`.
- Expected: Gate 4 HCI evidence includes actual or equivalent player input coverage for route entry, camera/touch behavior, selection, move, attack, invalid recovery, assistant ownership release, and result popup.
- Actual: Existing PlayMode automation validates route logic, ECS selection/attack/result readiness, and build rejection, but it does not simulate real touch/camera gestures or produce user-facing capture states.
- Blocks next milestone: yes for balance QA; no new gameplay code blocker by itself.
- Recommended owner: QA/HCI for manual/device pass; Gameplay/UI only if route or input bugs reproduce.

### QAHCI-G4-009: Assistant ownership and result Stop remain route-capture gaps despite green service tests

- Severity: Major
- Affected lane: UI / support-FTUE
- Reproduction steps:
  1. Run `WarlineCaptureUiAssistantRuntimeBindingTests`.
  2. Review prefab/editor capture state `M01_Integrated_*_07_AssistantTakeoverStop.png`.
  3. Attempt to find equivalent player-route screenshots from the current pass.
- Expected: Player-route evidence shows visible ownership, Stop availability, player-input release, and result-flow Stop leaving `POP-05_MissionResult` open.
- Actual: Service/prefab tests pass and prefab captures show the state, but no player-route screenshot evidence exists.
- Blocks next milestone: blocks active balance QA until route-proven or explicitly waived.
- Recommended owner: QA/HCI for route validation; UI/Support if behavior fails.

### QAHCI-G4-010: Editor/tooling log noise persists

- Severity: Minor unless reproduced as player-visible instability
- Affected lane: gameplay / UI / support-FTUE
- Reproduction steps:
  1. Run the QA PlayMode route command.
  2. Scan `/private/tmp/warlinecapture-qa-hci-player-route-playmode.log`.
- Expected: Gate 4 logs have no severe runtime exceptions, freezes, input stalls, or log spam that masks failures.
- Actual: No previously tracked severe M01 log-health failures reproduced. The log still contains Animator warnings, editor preview-scene leak warnings, persistent allocation leak warnings, and external tooling/licensing/Xcode/ADB noise.
- Blocks next milestone: no by itself.
- Recommended owner: QA/HCI watches; Gameplay/UI investigate only if warnings become player-visible or PM escalates.
