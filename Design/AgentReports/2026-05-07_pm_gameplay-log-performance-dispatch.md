# PM Dispatch: Gameplay Log/Performance Cleanup

Date: 2026-05-07

## Trigger

The gameplay typed command hooks were accepted in `Design/AgentReports/2026-05-07_pm_gameplay-typed-command-hooks-review.md`, leaving the gameplay lane idle.

## New Gameplay Task

`Design/AgentTasks/gameplay_current.md` now assigns gameplay to investigate and reduce known M01 PlayMode log/performance risks:

- repeated `EntitiesGraphicsSystemUtility.RootsHandlerDelegate` / resource-GC `NullReferenceException` entries
- preview-scene leak warnings
- `RuntimeCitySpawner` / `FreezeDetect` hitches

## Reason

QA/HCI is explicitly tracking frame drops, freezes, hitches, exceptions, and log health before active balance QA. This task keeps gameplay moving on those readiness risks while Support/FTUE continues assistant command-executor integration.

## Cross-Lane State

- Support/FTUE: continue connecting assistant `Do It` actions to accepted gameplay hooks.
- UI: continue `PREFAB-04` target-lock quality/alignment work.
- QA/HCI: use gameplay's cleanup report to update the balance-QA gate.
