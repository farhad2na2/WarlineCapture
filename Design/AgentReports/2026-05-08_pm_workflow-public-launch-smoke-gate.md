Status: workflow fixed
Topic:
Public launch path smoke is now required before manual QA/HCI readiness

Why the miss happened:
QA/HCI and UI validated the M01 slice through editor tooling, route-driven capture builders, prefab/UI captures, and focused PlayMode/EditMode tests. Those checks proved useful internal pieces, but they did not prove that a real user entering from Main Menu, Saga Map, Mission Briefing, Loadout, or Quick Custom would reach the intended production M01 slice.

The task wording allowed "player-route automation" or "equivalent route-driven harness" to count as route evidence. That left a gap: the harness could route to `WarlineCaptureRoute.Match` while the public launch buttons still entered the legacy 3D gameplay stack.

What changed:
- `Design/WarlineCapture_Agent_Coordination_Workflow.md` now has a Public Launch Path Smoke Rule.
- `Design/AgentTasks/M01_CRITICAL_PATH.md` now requires public launch path proof before Gate 4 can pass.
- `Design/AgentTasks/qa-hci_current.md` now blocks manual HCI/balance QA until a public M01 production launch path is verified.

New required evidence:
- Entry path used, such as `Main Menu -> Saga Map -> Mission Briefing -> Launch` or `Main Menu -> Quick Custom -> Launch`.
- Expected mission id and visual direction.
- Actual first visible gameplay state.
- Whether legacy/sandbox UI, legacy 3D world, old prototype scene, or wrong mission appeared.
- Screenshot or capture path when practical.

Cross-lane impact:
- UI/GamePlay must fix or clearly label the legacy public launch paths before QA/HCI asks for manual user feedback again.
- QA/HCI must not accept editor-only route captures as a replacement for public launch smoke.
- PM should reject any Gate 4 readiness claim that does not prove the user can actually start the intended M01 production slice.

Needs user decision:
No. This is a process correction from the user's manual test finding.

Next recommended task:
Gameplay/UI should add or wire a clear M01 production launch path, then QA/HCI should run the public launch smoke before any further manual HCI/balance request.
