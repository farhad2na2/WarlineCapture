# PM Task Board Reconciliation - Support/FTUE and QA/HCI

Date: 2026-05-07
Lane: PM
Status: accepted

## Trigger

Latest PM scan found no new completion reports after the accepted UI production fix and accepted Support/FTUE live context-provider handoff, but the Support/FTUE and QA/HCI current-task files still referenced older blockers.

## Decision

Support/FTUE is no longer assigned to implement live `AssistantContextProvider`; that work is accepted. The lane is now waiting as integration support for UI assistant runtime binding and should only implement a missing Support/FTUE API if the UI handoff exposes one.

QA/HCI is updated to treat these gates as accepted:

- UI PREFAB-04 assistant button production fix
- Support/FTUE command intent executor
- Support/FTUE live `AssistantContextProvider`

QA/HCI should keep active balance QA blocked until the UI assistant runtime binding and Gameplay sprite-renderer close tactical evidence land.

## Cross-Lane Notices

- UI: continue `Design/AgentTasks/ui_current.md`; bind the accepted assistant surface to live context and typed command execution.
- Gameplay: continue `Design/AgentTasks/gameplay_current.md`; finish the M01 sprite-atlas renderer hookup and close tactical capture evidence.
- Support/FTUE: do not repeat accepted provider work; wait for UI integration questions or PM-assigned result-popup close/acknowledge work.
- QA/HCI: monitor UI and Gameplay handoffs, including performance/freeze/log-health risks.

## Files Changed

- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-07_pm_support-qa-task-board-reconciliation.md`

## Validation

Documentation-only PM reconciliation. No Unity validation required.

## User Decision Needed

No.
