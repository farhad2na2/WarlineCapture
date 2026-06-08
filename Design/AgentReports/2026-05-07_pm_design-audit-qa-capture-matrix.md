Status: advisory
Topic: QA/HCI capture matrix needs exact device and safe-area definition before final smoke
Docs reviewed:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-validation-plan.md`
- `Design/AgentReports/2026-05-07_pm_qa-hci-validation-plan-review.md`
- `Design/M01_FirstContact_Production_Contract.md`
Finding:
- The QA/HCI plan and critical path correctly require 16:9 and 20:9 captures, plus checks for selected squad, enemy patrol, objective tracker, command feedback, assistant panel, result popup, and minimap.
- The exact final capture matrix is still not pinned to concrete resolutions, safe-area assumptions, orientation, and required per-state screenshots. The accepted QA plan itself lists this as an open question.
Why it matters:
- A QA agent can run a technically valid 16:9/20:9 smoke pass while using different resolutions or safe-area settings than UI, Gameplay, or PM expects.
- This can create false confidence for AAA mobile readability: clipped HUD, occluded patrols, assistant-panel overlap, or touch-target problems can hide if every lane captures a different viewport.
- It also makes before/after regression comparisons weaker because captures are not guaranteed to be reproducible across agents.
Recommended fix:
- Before final M01 QA/HCI smoke, define a small locked capture matrix in the QA task or a dedicated PM note:
  - Desktop/landscape: 16:9 at one exact resolution.
  - Mobile/tall or wide target: 20:9 at one exact resolution with safe-area setting stated.
  - Required states: match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, result popup.
  - Required visibility: player squad, hostile patrol, objective tracker, minimap, command feedback, assistant entry/panel, and result flow must not be clipped or hidden.
- Keep broader device testing for later. This recommendation is only for the first reproducible M01 acceptance capture set.
Affected lanes:
- QA/HCI
- UI
- Gameplay
- Support/FTUE
Needs user decision:
- No immediate decision while Gameplay capture is still blocked.
- Before final M01 QA/HCI smoke, PM/user should approve the exact two capture resolutions and safe-area assumption.
Next task update needed:
- Not needed until Gameplay submits an accepted fully framed sprite-renderer capture.
- Then update `Design/AgentTasks/qa-hci_current.md` with the locked capture matrix before telling QA/HCI to run final smoke.
