Status: needs fixes
Topic: QA/HCI completion report filename collision
Docs reviewed:
- Design/AgentTasks/qa-hci_current.md
- Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md
- Design/AgentReports/2026-05-07_pm_qa-hci-m01-watcher-smoke-regression-review.md
Finding:
The active QA/HCI task asks the QA agent to write its next completion report to `Design/AgentReports/2026-05-07_qa-hci_m01-watcher-smoke-regression.md`, but that file already contains the earlier automated smoke-regression handoff that PM reviewed. The next QA pass should not reuse the same filename.
Why it matters:
If QA follows the current instruction exactly, it can overwrite or blur the already-reviewed smoke evidence. PM would lose a clean audit trail between the first automated smoke pass, the Gameplay/UI follow-up reviews, and the next integrated Gate 4 readiness result.
Recommended fix:
Update the QA/HCI current task before QA resumes so the next report uses a new filename, for example `Design/AgentReports/2026-05-07_qa-hci_m01-gate4-integrated-readiness.md`. Keep the prior smoke-regression report immutable as historical evidence.
Affected lanes:
QA/HCI, PM
Needs user decision:
No.
Next task update needed:
Yes. PM should update `Design/AgentTasks/qa-hci_current.md` before QA/HCI is told to continue after Gameplay and UI handoffs land.
