Status: needs fixes
Topic:
UI public launch visible-scene handoff review

Reviewed handoff:
`Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Reviewed QA/HCI validation:
`Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Finding:
The updated UI handoff appears to move the technical launch path forward: automated public launch tests pass, legacy `UI_Canvas` and old scene roots are reported inactive, and public-launch captures now exist for campaign and Quick Custom paths. However, the handoff is not acceptable for manual HCI/balance readiness because the visible gameplay evidence is still not readable or actionable.

Validation accepted:
- `Chapter01M01PlayModeValidationTests`: reported 5/5 passed.
- `WarlineCaptureUiQuickCustomTests`: reported 16/16 passed.
- `WarlineCaptureUiSagaCampaignTests`: reported 8/8 passed.
- Capture files exist at `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
- The captures are 1280x720 PNGs.
- QA/HCI confirms the captures no longer show the obvious old 3D prototype visuals.

Validation still needed:
- A readable player-facing screenshot/capture showing the actual first playable M01 scene with enough context for HCI.
- Visible HUD/objective/assistant or a clearly accepted explanation why the first frame intentionally hides them.
- Camera framing where units, targets, and next action are readable without guesswork.
- Evidence that a player can understand where they are, identify the objective, select a unit, issue an action, see feedback, and recover from invalid input.

Reason for needs-fixes:
The current campaign and Quick Custom captures show a mostly empty brown field with tiny centered gameplay content. That may prove old-scene suppression, but it does not prove usable M01 production gameplay. The visible gameplay HCI gate requires player-operable evidence, not just a non-legacy render.

Cross-lane notices:
- Gameplay/UI still own the visible-scene readability blocker.
- QA/HCI correctly rejected the handoff for manual-readiness evidence.
- PM should not ask the user to test this yet.
- Marker/VFX readiness remains separately open unless waived later.

Needs user decision:
No. This is an implementation/evidence quality issue for Gameplay/UI.

Next task:
Gameplay/UI should revise the public launch implementation/camera/capture so campaign and Quick Custom/Test launch produce a readable first playable scene. The next handoff must include screenshot/capture evidence that satisfies the visible gameplay HCI gate.
