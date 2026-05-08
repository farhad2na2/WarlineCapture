Lane:
QA/HCI

Task:
M01 Gate 4 integrated readiness review after accepted UI integrated capture matrix, gameplay log-health classification, support/FTUE assistant handoffs, and current sprite-renderer visual evidence.

Files changed:
- Design/AgentReports/2026-05-07_qa-hci_m01-gate4-integrated-readiness.md

Contracts touched:
- Design/AgentTasks/qa-hci_current.md: completed the assigned QA/HCI readiness report without editing AgentTasks.
- Design/AgentTasks/M01_CRITICAL_PATH.md: reviewed Gate 4 criteria and left Gate 4 unaccepted because human/player-route and safe-area/device validation remain open.
- Design/WarlineCapture_M01_FirstContact_Production_Contract.md: reviewed M01 select, move, attack, objective, result, Build rejection, marker, capture, and 16:9/20:9 readability requirements.
- Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md, Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md, and Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md: reviewed typed ARIA Show Me / Do It / Stop, ownership, cancellation, and M01 recommendation requirements.
- No runtime API, prefab path, data schema, asset row, route id, or production source file was changed.

User-visible behavior:
No runtime behavior changed. This pass classifies M01 readiness from accepted evidence and records the remaining QA/HCI blockers before active balance QA can begin.

Validation run:
- Reviewed accepted UI handoff: Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md.
- Reviewed accepted PM UI review: Design/AgentReports/2026-05-07_pm_ui-m01-integrated-capture-matrix-review.md.
- Reviewed capture folder: Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/.
- Visual review: Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_CaptureMatrix_ContactSheet.png.
- Visual review: Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png.
- Image health check with Pillow: capture dimensions, alpha ranges, and nonzero RGB variance for the UI matrix and close sprite-renderer capture.
- Reviewed accepted gameplay log-health report: Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md.
- Reviewed accepted PM gameplay log-health review: Design/AgentReports/2026-05-07_pm_gameplay-m01-log-health-validation-review.md.
- Scanned available graphics-enabled PlayMode log /private/tmp/warlinecapture-m01-log-health-playmode-graphics.log for NullReferenceException, RenderTexture.Create failed, EntitiesGraphicsSystemUtility, AIProduction, AIBuild, AISquad, FreezeDetect, PerfDiag, RuntimeCitySpawner, preview-scene leaks, persistent allocation leaks, and Animator warnings.
- Reviewed support/FTUE command-intent and live context reports: Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md and Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md.
- Reviewed accepted UI assistant runtime binding fix: Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md and Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md.
- QA Unity workspace smoke checks were not rerun in this QA/HCI report pass because the PM-accepted UI and gameplay evidence already included focused Unity results after the latest handoffs. This pass verified captures/logs and classified readiness. No production code changed in this pass.

Validation result:
Needs fixes for final Gate 4 and active balance QA. No new blocker was found inside the accepted prefab/editor capture matrix itself: at 1920x1080 and 2400x1080, the objective tracker, minimap, squad tray, command bar, command feedback, selected squad panel, assistant entry/panel, assistant takeover/Stop ownership state, invalid-command recovery, and result popup are visible in the supplied evidence. The result popup is correctly modal in the result state.

Gate 4 cannot be accepted yet because the evidence is not a human/player-route run and safe area was not simulated. The UI report explicitly classifies the captures as prefab-based editor evidence, not device PlayMode interaction captures. Active balance QA remains blocked until the integrated M01 route is played end to end by a human or equivalent player-route automation with route logs/captures proving select, move, attack, invalid recovery, assistant ownership/Stop, result flow, and safe-area/readability behavior.

Known gaps:
- Safe area not simulated; 20:9 evidence is helpful but not enough for notched/rounded-corner mobile devices.
- No Android/device validation was run in this pass.
- No human player-operated M01 pass was completed through the intended route in this pass.
- The current UI capture matrix uses prefab/editor evidence. It does not prove real route entry, input handling, camera touch behavior, or actual player-input release during assistant takeover.
- Current AI-generated tactical/unit art remains review evidence only, not final approval. Final atlas/config packaging, hostile non-color readability treatment, and vfx.unit.destroyed.small remain open.
- Editor shutdown warnings still appear in the available graphics-enabled PlayMode log: preview-scene leak and persistent allocation leak warnings. No NullReferenceException, RenderTexture.Create failed, EntitiesGraphicsSystemUtility stack, AIProduction/AIBuild/AISquad noise, FreezeDetect, PerfDiag, or RuntimeCitySpawner hitch reproduced in that scanned log.
- Existing Animator warnings appear in the graphics-enabled PlayMode log. They were not classified as player-visible blockers in the accepted gameplay log-health report but remain QA noise to watch during the player-route pass.

Cross-lane impacts:
- QA/HCI keeps active balance QA blocked until player-route/safe-area Gate 4 evidence lands.
- UI has no immediate blocker from the reviewed capture matrix, but must support a player-route capture/device-safe-area pass if QA/PM requests it.
- Gameplay log-health is accepted for focused graphics-enabled editor evidence; Gameplay should only be re-engaged if the player-route pass reproduces freezes, input stalls, severe FPS drops, runtime exceptions, or gameplay-owned log spam.
- Support/FTUE typed intents and context provider are accepted for service-level readiness; Support/FTUE should only be re-engaged if the player-route pass shows ARIA recommendation, ownership, Stop, or result explanation behavior is misleading.
- Art/design remains owner of final M01 atlas approval, hostile non-color readability, and destroyed VFX.

Next recommended task:
Run a player-route M01 Gate 4 capture/log pass at 1920x1080 and 2400x1080 with safe-area/device assumptions explicit. Capture the same eight states and include logs for select, move, attack, invalid recovery, assistant open, assistant takeover/Stop, result popup, and any frame/input stalls. Do not start active balance QA until that pass has no blocker findings.

## Current balance-QA gate status

Blocked. The automated and prefab/editor evidence is strong enough to remove the earlier UI/gameplay handoff deadlock, but it is not sufficient for active balance QA. Balance observations would still be invalid if route entry, touch input, safe-area clipping, assistant ownership cancellation, or frame/input behavior fails in a real player-operated M01 pass.

## QA findings

### QAHCI-G4-001: Integrated player-route evidence is still missing

- Severity: Blocker
- Affected lane: QA/HCI with UI and Gameplay support if route capture tooling is missing.
- Expected: Gate 4 includes an end-to-end M01 player-route pass proving match start, squad selection, move, attack, invalid-command recovery, assistant open, assistant takeover/Stop ownership state, and result popup through the intended playable flow.
- Actual: Current evidence is editor-prefab capture plus accepted focused tests. It does not prove player route entry, actual input handling, or real gameplay-to-UI event timing.
- Blocks next milestone: yes. Blocks active balance QA and M02 expansion.
- Recommended owner: QA/HCI for the pass; UI/GamePlay only if tooling or route blockers are found.

### QAHCI-G4-002: Safe-area/device behavior remains unverified

- Severity: Major
- Affected lane: UI / QA-HCI
- Expected: 20:9/mobile landscape validation states safe-area assumptions and proves top HUD, right-side controls, minimap, assistant panel, and result popup are not clipped by real device safe areas.
- Actual: UI correctly disclosed `safe area not simulated`. The 2400x1080 captures are readable, but they do not cover notches, rounded corners, or Android cutout behavior.
- Blocks next milestone: blocks active balance QA unless PM explicitly waives device/safe-area validation for this milestone.
- Recommended owner: QA/HCI with UI support.

### QAHCI-G4-003: Assistant ownership/Stop is visible in prefab evidence but not route-proven

- Severity: Major
- Affected lane: UI / Support-FTUE / QA-HCI
- Expected: During M01 player flow, ARIA takeover exposes visible ownership state, Stop is available, player input outside the panel releases assistant ownership, and Stop during result explanation does not close or acknowledge POP-05_MissionResult.
- Actual: Accepted tests and the capture matrix show visible assistant takeover/Stop state, but the current QA pass did not exercise actual player input release or result-flow Stop through a player-operated route.
- Blocks next milestone: blocks active balance QA until route-proven or explicitly waived.
- Recommended owner: QA/HCI for verification; UI/Support if behavior fails.

### QAHCI-G4-004: Final art/readability approval remains open

- Severity: Major for final art approval; not a blocker for the next player-route smoke if kept as review-art evidence.
- Affected lane: art-design / gameplay
- Expected: Final M01 readability includes final atlas/config packaging, hostile non-color readability treatment, and vfx.unit.destroyed.small for destroyed feedback.
- Actual: M01_SpriteRenderer_CloseCapture.png is acceptable current review evidence for grounding, scale, and unit visibility. It is not final art approval.
- Blocks next milestone: blocks final art approval and may block balance QA if hostile/friendly reads fail during player-route capture.
- Recommended owner: art-design with gameplay integration.

### QAHCI-G4-005: Remaining editor/tooling shutdown warnings should stay tracked

- Severity: Minor unless reproduced as player-visible instability.
- Affected lane: Gameplay / QA-HCI
- Expected: Integrated logs have no recurring runtime exceptions, freezes, frame hitches, input stalls, or severe noise that masks real failures.
- Actual: Accepted gameplay graphics-enabled PlayMode evidence removed prior NullReferenceException, RenderTexture.Create failed, EntitiesGraphicsSystemUtility, generic AI plan noise, FreezeDetect, PerfDiag, and RuntimeCitySpawner hitch concerns. The scanned log still contains preview-scene leak and persistent allocation warnings at editor shutdown plus Animator warnings.
- Blocks next milestone: no by itself, but escalates if reproduced during player-route/device validation or paired with visible instability.
- Recommended owner: QA/HCI watches in the next pass; Gameplay investigates only if reproduced as runtime/player-visible or PM raises it.

## Performance/freeze/log-health findings

- No FreezeDetect, PerfDiag, RuntimeCitySpawner hitch, NullReferenceException, RenderTexture.Create failed, EntitiesGraphicsSystemUtility stack, AIProduction, AIBuild, or AISquad entries reproduced in the available accepted graphics-enabled PlayMode log.
- Preview-scene leak and persistent allocation leak warnings remain editor shutdown/tooling risks.
- Animator warnings remain log-noise risks and should be watched during the player-route pass.
- No frame-rate measurement, device thermal data, or input latency timing was collected in this QA/HCI pass.

## New HCI risks introduced by latest handoffs

- The prefab capture matrix can look correct while real touch/camera input, route entry, safe-area cutouts, and player-input assistant cancellation remain untested.
- The 20:9 capture shows HUD surfaces close to screen edges; safe-area simulation/device capture is needed before treating mobile readability as passed.
- Result popup readability is strong in the supplied evidence, but result-flow Stop behavior still needs route-level validation because the popup is intentionally modal.
- The close sprite-renderer capture proves grounding and scale, but final hostile identification must not depend only on color tint once final art is approved.
