Status: accepted
Topic:
QA/HCI visible gameplay gate added

Source:
User feedback that QA/HCI missed simple manual launch failures because it was not checking the visible scene/gameplay strongly enough.

Finding:
QA/HCI was allowed to over-trust route state, component checks, Test Runner XML, and prefab/editor captures. That let a public launch path appear technically validated while the user still saw the old visible prototype scene.

PM action:
- Added a `Visible Gameplay HCI Gate` section to `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
- Updated `Design/AgentTasks/qa-hci_current.md` so QA/HCI cannot accept route-only, XML-only, component-only, or inactive-legacy-UI evidence as manual readiness.
- Added minimum visible gameplay/HCI checks: visible scene correctness, objective clarity, unit selection, move/attack feedback, invalid-input recovery, camera framing, and obvious freeze/input-stall review.

New rule:
Before the user is asked for manual HCI/balance feedback, QA/HCI must prove what the player actually sees and can do. A route state like `WarlineCaptureRoute.Match`, ECS component presence, or inactive `UI_Canvas` is supporting evidence only; it is not sufficient acceptance evidence.

Cross-lane impacts:
- Gameplay/UI must provide player-visible capture/manual-observation evidence for public launch fixes.
- QA/HCI must reject future launch/readiness reports that lack visible scene evidence for the promised user path.
- PM should keep manual user testing blocked until QA/HCI has accepted visible gameplay evidence.

Needs user decision:
No.

Next recommended task:
Gameplay/UI should continue the active public launch blocker with visible rendered-scene evidence. QA/HCI should use the new visible gameplay gate on the next handoff.
