Status: advisory
Topic:
Stale M01 critical-path gate/task statuses after accepted PM reviews

Docs reviewed:
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-target-lock-review.md`
- `Design/AgentReports/2026-05-07_pm_support-assistant-service-slice-review.md`

Finding:
The task board has not yet been reconciled after accepted reviews. `M01_CRITICAL_PATH.md` still shows Gate 1 and Gate 2 as active even though PM reviews accepted Gameplay Gate 1 and the UI PREFAB-04 visual-target handoff. `ui_current.md` is completed but does not carry the next UI production-asset/prefab task. `support-ftue_current.md` still points at the already accepted assistant recommendation service slice instead of the next live assistant `Do It` / `CommandIntentExecutor` wiring task.

Why it matters:
Agents using `AUTO_CONTINUE.md` may reread stale lane files and either idle, repeat accepted work, or miss the new critical-path sequence. This undermines the speed-control workflow because the M01 gate file is the main routing mechanism for all agents.

Recommended fix:
On the next explicit PM task-board update:
- Mark Gate 1 accepted in `M01_CRITICAL_PATH.md`.
- Mark the PREFAB-04 visual-target portion of Gate 2 accepted, while keeping UI production assets/prefab implementation open.
- Replace `ui_current.md` with the next UI task: separated `aria_waveform_icon.png`, `aria_button_state_set.png`, and reusable animated `PREFAB-04_AssistantButton` prefab.
- Replace `support-ftue_current.md` with the next Support/FTUE task: `CommandIntentExecutor` boundary and live assistant `Do It` wiring to accepted gameplay hooks.
- Ensure QA/HCI remains waiting until the UI production and Support/FTUE wiring gates are ready, then assign M01 smoke/readability/performance.

Affected lanes:
PM, UI, Support/FTUE, QA/HCI

Needs user decision:
No. This is a PM routing cleanup.

Next task update needed:
Yes. The PM should update the task files when allowed by the thread, then tell agents to continue from the refreshed critical path.
