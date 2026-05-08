Status: blocked
Topic:
Unreported public launch captures do not satisfy visible gameplay HCI gate

Source:
PM heartbeat review of untracked public-launch capture artifacts.

Files reviewed:
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`

Finding:
Two public-launch capture images exist, but no Gameplay/UI handoff report has landed yet. The captures do not satisfy the visible gameplay HCI gate as-is.

Observed visual issues:
- Both captures are 1280x720 and show a mostly empty brown field with tiny gameplay content near the center.
- The campaign capture shows only a small visible object/ground patch and does not clearly show selectable units, command feedback, objective context, HUD, assistant, or a usable player task.
- The Quick Custom capture shows small sprites/markers, but still lacks visible HUD/objective context and does not prove a playable, understandable first interaction.
- Neither capture demonstrates the player can understand where they are, identify the objective, select a unit, issue the next action, recover from invalid input, or read command feedback.

Why it matters:
The new QA/HCI visible gameplay gate requires proof of what a player actually sees and can operate. These captures may show that the old 3D prototype is being suppressed, but they do not yet prove a usable M01 production slice. Replacing the old prototype with an empty or unframed scene is still not acceptable for manual readiness.

Required fix/evidence:
Gameplay/UI should provide a proper handoff report and improved visible evidence that includes:
- entry path used;
- active mission id;
- route state and `UI_Canvas` state;
- old-3D visibility status;
- current M01 2D/isometric visibility status;
- visible HUD/objective/assistant or a clear explanation why the first frame intentionally hides them;
- camera framing that makes units, targets, and the next action readable;
- screenshot/capture/log paths;
- focused validation results.

Affected lanes:
- Gameplay
- UI
- QA/HCI

Needs user decision:
No. This is a lane-quality blocker. The user should not be asked to test until the agents provide readable visible gameplay evidence.

Next recommended task:
Gameplay/UI should finish the public visible-scene fix and submit a standard handoff report. QA/HCI should reject the current captures unless the final handoff adds stronger visible-scene/HCI evidence.
