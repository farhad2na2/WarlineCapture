Status: accepted
Topic: Stale Gate 4 blocker text after accepted UI public-launch evidence
Docs reviewed:
- Design/AgentTasks/M01_CRITICAL_PATH.md
- Design/AgentTasks/ui_current.md
- Design/AgentTasks/qa-hci_current.md
- Design/AgentTasks/support-ftue_current.md
- Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md
- Design/AgentReports/2026-05-08_pm_public-launch-handoff-workspace-review.md

Finding:
Active task docs still describe Gate 4 as blocked by older public-launch and UI capture states even after the UI and Gameplay public-launch handoffs produced assigned-workspace evidence. `Design/AgentTasks/ui_current.md` still says public launch shows legacy/flat-brown evidence and assigns UI to fix the public-launch composition. `Design/AgentTasks/gameplay_current.md` still frames the public-launch world proof as active Gameplay work even though `/Users/farhad/Projects/WarlineCapture-CodexUnity1` validation now passes. `Design/AgentTasks/M01_CRITICAL_PATH.md` still lists public M01 launch-path wiring and UI route-driven safe-area/profile closure as remaining blockers. `Design/AgentTasks/support-ftue_current.md` still says the current Gate 4 blocker is UI route-driven capture/safe-area tooling followed by QA/HCI rerun.

Why it matters:
Agents are instructed to treat `Design/AgentTasks/*_current.md` as the only source of current priorities. Leaving stale UI/GamePlay/Gate 4 blocker language after the accepted public-launch validation can cause UI or Gameplay to repeat completed work, Support/FTUE to wait on the wrong deliverable, and QA/HCI to misread what is actually ready for rerun. The actual next critical-path action is QA/HCI affected Gate 4 rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`, with Support/FTUE re-engaging only if that rerun finds a concrete assistant issue.

Recommended fix:
Refresh the active task docs to reflect current state:
- Mark UI public-launch capture-composition evidence as accepted from `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- Mark Gameplay public-launch world/ECS evidence as accepted from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Remove or qualify stale claims that public launch still shows legacy 3D, flat brown/tiny-world, or unresolved UI capture composition.
- Keep QA/HCI blocked only on task refresh plus the remaining affected Gate 4 rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- Move Gameplay to the next PM-approved Chapter 1 task or waiting state; do not leave it on the completed public-launch proof.
- Update Support/FTUE waiting text so it waits on QA/HCI rerun or concrete assistant findings, not old UI route-driven capture tooling.

Affected lanes:
UI, Gameplay, QA/HCI, Support/FTUE, PM

Needs user decision:
No validation-context decision remains. PM/user needs to refresh active task files or authorize the PM assistant to do so.

Next task update needed:
Completed. `Design/AgentTasks/ui_current.md`, `Design/AgentTasks/gameplay_current.md`, `Design/AgentTasks/qa-hci_current.md`, `Design/AgentTasks/support-ftue_current.md`, and the Gate 4 section in `Design/AgentTasks/M01_CRITICAL_PATH.md` now reflect accepted Gameplay/UI public-launch evidence and the active QA/HCI rerun.
