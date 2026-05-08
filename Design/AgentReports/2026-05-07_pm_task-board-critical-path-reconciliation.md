# PM Dispatch: Critical Path Task Board Reconciliation

Date: 2026-05-07

## Trigger

The PM audit `Design/AgentReports/2026-05-07_pm_design-audit-stale-critical-path-gates.md` found stale M01 critical-path and lane task statuses after accepted reviews.

## Changes

Updated:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentTasks/qa-hci_current.md`

## Current Routing

- Gameplay Gate 1 is accepted.
- UI PREFAB-04 visual target lock is accepted, but Gate 2 remains active for separated production assets and prefab implementation.
- Support/FTUE Gate 3 is active for `CommandIntentExecutor` and live assistant `Do It` wiring.
- QA/HCI remains watcher/waiting until UI production and Support/FTUE wiring are ready.

## Next Agent Actions

- Gameplay: continue with `Design/AgentTasks/gameplay_current.md` for M01 sprite-atlas presenter first slice.
- UI: continue with `Design/AgentTasks/ui_current.md` for separated ARIA assistant button assets and reusable prefab.
- Support/FTUE: continue with `Design/AgentTasks/support-ftue_current.md` for live assistant command executor wiring.
- QA/HCI: continue watcher mode and prepare M01 smoke/readability/performance pass after Gate 2 and Gate 3 handoffs.

## User Decision Needed

No.
