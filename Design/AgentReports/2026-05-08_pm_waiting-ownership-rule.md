Status: accepted
Topic: Waiting/blocker ownership rule
Docs reviewed:
- Design/WarlineCapture_Agent_Coordination_Workflow.md
- Design/AgentTasks/AUTO_CONTINUE.md
- Design/AgentTasks/ui_current.md
- Design/AgentTasks/qa-hci_current.md
Finding:
The workflow allowed agents to report waiting/blocked/idle without proving whether the next deliverable belonged to their own lane or another lane. This allowed the UI and QA/HCI lanes to mirror each other's waiting language even though UI owned the missing Gate 4 integrated capture matrix.
Why it matters:
The PM assistant is supposed to catch and unblock these cases. A required ownership check makes the deadlock visible immediately: if the waiting lane owns the missing file/report/asset, it must continue or report a concrete technical blocker instead of waiting.
Recommended fix:
Done. `Design/WarlineCapture_Agent_Coordination_Workflow.md` and `Design/AgentTasks/AUTO_CONTINUE.md` now require every waiting/blocked/idle report to name the waiting lane, exact deliverable, owner of next action, and whether fallback work is possible. Reports that wait on a same-lane deliverable should be marked `needs fixes`.
Affected lanes:
All lanes
Needs user decision:
No.
Next task update needed:
UI should continue producing `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md`; QA/HCI remains correctly waiting for that file.
