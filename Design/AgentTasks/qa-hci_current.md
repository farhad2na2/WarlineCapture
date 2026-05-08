# QA/HCI Current Task

Date: 2026-05-08
Status: waiting
Priority: P1 wait for UI route-driven capture/safe-area tooling

## Assignment

Wait for reviewed fixes to the current M01 Gate 4 blockers, then rerun only the affected QA/HCI checks. Do not begin balance conclusions until a public player launch path reaches the intended M01 production slice and Gate 4 has no blocker findings.

## Context

Read first:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-validation-plan.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-validation-plan-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-playmode-validation.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-playmode-validation-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-target-lock.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-target-lock-review.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-presentation-review.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-match-hud-mount-fix-review.md`
- `Design/AgentReports/2026-05-07_pm_support-runtime-wiring-review.md`
- `Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md`
- `Design/AgentReports/2026-05-07_support-ftue_live-assistant-context-provider.md`
- `Design/AgentReports/2026-05-07_pm_support-ftue_live-assistant-context-provider-review.md`
- `Design/AgentReports/2026-05-07_support-ftue_integration-support-watch.md`
- `Design/AgentReports/2026-05-07_pm_support-ftue-integration-support-watch-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-presenter.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-fix-review.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-review.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-atlas-renderer-review.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-update-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-fix-review.md`
- `Design/AgentReports/2026-05-07_pm_design-audit-qa-capture-matrix.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md`

The QA/HCI validation plan is accepted and M01 has a technical PlayMode baseline. Gameplay Gate 1 is accepted and the M01 sprite-presenter contract slice is accepted. Support/FTUE command executor wiring and live `AssistantContextProvider` are accepted. The assistant HUD mount capture fix, PREFAB-04 visual target, revised assistant button production implementation, and UI assistant runtime-binding fix are accepted.

Gameplay sprite-renderer code/test proof and the close tactical capture fix are accepted for current review-art evidence. The accepted PNG is `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`. Use it for grounding, scale, and readability checks, while keeping final art approval separate.

Automated QA/HCI smoke is green but Gate 4 is not accepted. Gameplay log-health is accepted for focused editor/non-headless evidence in `Design/AgentReports/2026-05-07_gameplay_m01-log-health-classification.md` and `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-health-validation-review.md`. UI integrated capture-matrix evidence is accepted for QA review in `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md` and `Design/AgentReports/2026-05-07_pm_ui-m01-integrated-capture-matrix-review.md`. QA/HCI integrated readiness and player-route automation reports are complete, but Gate 4 remains blocked by public launch-path mismatch, incomplete named safe-area profiles, reason-code Unity validation, marker/VFX readiness, and unverified real touch/camera ergonomics. The user manually confirmed both Quick Custom and Saga Map/campaign launch paths still show the old 3D prototype. Active balance QA remains blocked until a reviewed Gameplay/UI handoff provides a public M01 production launch path and the other affected blockers are closed. Final art approval remains blocked on final atlas/config packaging, hostile non-color readability treatment, and `vfx.unit.destroyed.small`.

## Required Work

- Wait for a reviewed Gameplay/UI handoff that fixes or explicitly labels the public launch-path mismatch reported in `Design/AgentReports/2026-05-08_pm_manual-test-quick-custom-launches-legacy-3d.md`.
- Before any manual HCI/balance request, verify a public player path reaches the intended M01 production slice:
  - `Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch`.
  - Any direct/quick/test launch path that the user is asked to use.
- Report expected mission id, expected visual direction, actual first visible gameplay state, whether legacy `UI_Canvas` or old 3D prototype appears, and screenshot/capture evidence when practical.
- Do not accept route-only, XML-only, component-only, or inactive-legacy-UI evidence as manual readiness. The QA/HCI pass must prove what the player sees after the launch/input action.
- For every public path that is claimed ready for the user, capture or explicitly observe the actual rendered scene/camera and state whether it visually matches the intended M01 2D/isometric production direction.
- Treat any mismatch between the promised task and the visible screen as a blocker, even if automated route/controller tests pass.
- Treat the user-reported upside-down tactical ground/map under the soldiers as a blocker until a revised Gameplay handoff proves the ground orientation is correct. QA/HCI should compare the public launch view against the accepted M01 gameplay reference, metadata anchors, road/objective direction, and minimap/camera mapping; readable units over a flipped/rotated ground plate are not manual-ready.
- PM reviewed the updated Gameplay handoff in `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`. QA/HCI may note the visual orientation improvement, but public launch remains blocked until Gameplay proves/fixes ECS-backed visible terrain/map ownership and removes or justifies broad lookup usage in touched validation.
- Wait for reviewed UI safe-area closure for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.
- Wait for reviewed reason-code runtime validation after Unity EditMode tests pass.
- After reviewed fixes land, rerun the affected M01 player-route/safe-area/public-launch checks.
- Capture/log the same eight required states at `1920x1080` and `2400x1080`: match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.
- State safe-area/device assumptions explicitly. If safe area is simulated, document the simulated insets/cutout. If using a real device, document device/aspect/cutout. If safe-area validation cannot be run, report the exact blocker.
- Verify route entry, actual input handling, camera/touch behavior, player-input release during assistant takeover, and result-flow Stop behavior.
- Run a minimum HCI sanity pass before requesting user feedback:
  - Can a player understand where they are after launch?
  - Can they identify the objective and next required action?
  - Can they select the intended unit without ambiguity?
  - Can they issue move/attack commands and see immediate feedback?
  - Can they recover from invalid input through visible feedback/assistant guidance?
  - Does the camera framing support the task without hiding the unit, target, controls, or objective?
  - Are there obvious freezes, input stalls, or severe frame drops during first interaction?
- Review `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` as current visual evidence. Do not mark current AI-generated art as final approved.
- Use the PM-locked first capture matrix for final Gate 4 review: `1920x1080` landscape and `2400x1080` landscape, with safe-area assumptions stated by UI.
- Track performance and stability risks explicitly: frame-rate drops, long frame hitches, visible game freezes, input stalls, excessive first-interaction cost, memory/leak warnings, recurring exceptions, and log spam.
- Track whether remaining PlayMode `NullReferenceException`, preview-scene leak warnings, headless URP render-target errors, generic AI plan noise, and any `FreezeDetect`/`PerfDiag` output stay fixed, documented as benign, or still blocking active balance QA during the integrated pass.
- Treat the old `RuntimeCitySpawner=1350.3ms` hitch as downgraded unless a new report reproduces it.
- Track UI assistant runtime binding, Gameplay sprite-renderer visual evidence, performance/freeze stability, and log health as current major HCI/readiness gates.
- Treat missing visible takeover ownership state, player-input takeover cancellation, or `Stop` closing/acknowledging `POP-05_MissionResult` as QA findings against the integrated assistant route.
- Treat any reproducible gameplay freeze, multi-frame input stall, or repeated severe FPS drop during M01 select/move/attack/result flow as at least a major QA finding, and as a blocker if it prevents completion or invalidates balance observations.
- Keep the readiness gate current, but do not invent new design requirements.
- Do not run full balance conclusions until a human can play the integrated M01 route with mounted HUD/assistant surfaces.
- Do not edit production code, prefabs, or source design contracts in this pass.
- Do not mark art assets complete.

## Validation Required

- If a fix handoff lands, verify the relevant public launch path, automated smoke tests, or captures where practical.
- QA/HCI's assigned Unity workspace for focused validation/captures is `/Users/farhad/Projects/WarlineCapture-CodexUnity3`. Do not use the Gameplay workspace (`WarlineCapture-CodexUnity`) or UI workspace (`WarlineCapture-CodexUnity2`) unless PM explicitly reassigns a temporary workspace.
- If Unity batchmode hits `LicenseClient-farhad` reconnect/time-out loops before tests start, rerun the same required command with Codex escalation/out-of-sandbox execution in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`. QA/HCI confirmed this resolves the sandbox licensing issue and produced a clean 5/5 public-launch PlayMode rerun. Do not switch to Gameplay/UI workspaces to work around licensing.
- Report blocker/major/minor/polish severity for any QA findings.
- Keep manual HCI and balance QA blocked unless all readiness gates are satisfied.

## Completion Report

Write the next rerun report to a new file:

`Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and also include:

- Current balance-QA gate status
- QA Unity workspace smoke checks run or explicitly deferred
- Performance/freeze/log-health findings
- New HCI risks introduced by latest handoffs
- Waiting/blocker ownership fields if any blocker remains
