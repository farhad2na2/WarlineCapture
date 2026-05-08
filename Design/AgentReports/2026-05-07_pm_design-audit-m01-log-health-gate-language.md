Status: advisory
Topic: M01 log-health gate language conflict
Docs reviewed:
- Design/AgentTasks/M01_CRITICAL_PATH.md
- Design/AgentTasks/gameplay_current.md
- Design/AgentTasks/qa-hci_current.md
Finding:
Gate 1 in M01_CRITICAL_PATH still lists "M01 PlayMode log/performance risks are fixed or classified with evidence" as an accepted result, while Gate 4 and the active Gameplay/QA tasks correctly state that the later QA/HCI smoke pass still has unresolved log-health classification blockers.
Why it matters:
Agents reading only the accepted Gate 1 summary could conclude that log/performance work is closed and deprioritize the active Gameplay P1 task. That creates a coordination risk because the current blocker is not the original Gate 1 runtime direction pass; it is a newer QA/HCI smoke log-health pass that still needs classification or fixes before Gate 4 can pass.
Recommended fix:
Clarify Gate 1 wording to say the original fixed-roads/runtime-direction log-performance pass is accepted, while the later QA/HCI smoke log-health classification remains a Gate 4 follow-up owned by Gameplay. Keep the active Gameplay and QA task wording unchanged unless the PM updates the critical path summary.
Affected lanes:
Gameplay, QA/HCI, PM
Needs user decision:
No.
Next task update needed:
Yes, low-risk PM doc cleanup when editing the critical path next. Do not block current Gameplay work; the active Gameplay task already points to the correct P1 log-health classification report.
