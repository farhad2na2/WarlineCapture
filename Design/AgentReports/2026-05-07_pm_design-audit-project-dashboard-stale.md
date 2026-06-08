Status: advisory
Topic: Project state dashboard is stale after accepted M01 capture milestone
Docs reviewed:
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/Project_State_Source.json`
- `Design/Project_State_Dashboard.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-sprite-capture-fix-review.md`
Finding:
- `M01_CRITICAL_PATH.md` now marks the Gameplay renderer/capture follow-up accepted for current review-art evidence and moves Gate 4 to active QA/HCI smoke/readability.
- `Project_State_Dashboard.md` and its JSON source still show the earlier 32% overall baseline, Visual Direction Lock at 41%, World Gameplay Iso Assets at 14%, and no note that the M01 sprite-renderer/capture evidence milestone was accepted.
- The critical path itself says the project state dashboard should be updated if accepted gates materially change completion.
Why it matters:
- The user is relying on the dashboard for overall completion percentage and regular estimate updates.
- If accepted PM milestones do not flow into the dashboard, the percent and forecast will lag behind actual progress and make planning/priority decisions less reliable.
Recommended fix:
- After the QA/HCI smoke result lands, update `Design/Project_State_Source.json` and regenerate `Design/Project_State_Dashboard.md`.
- Include the accepted M01 sprite-renderer/capture milestone, the current QA/HCI gate state, and any new forecast delta.
- Avoid changing the overall percentage on this heartbeat alone; the capture is meaningful progress, but Gate 4 has not passed yet.
Affected lanes:
- PM
- QA/HCI
- Gameplay
Needs user decision:
- No immediate user decision required.
Next task update needed:
- Not now. Revisit immediately after the QA/HCI smoke handoff is reviewed.
