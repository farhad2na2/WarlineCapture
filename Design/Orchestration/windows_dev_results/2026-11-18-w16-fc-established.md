# Windows dev results — W16 FC established rows (2026-11-20)

Branch `missions/win-w16-fc-established`. The Windows dev lead did this task.
- **Fix:** shared-code change `79d93b2eb`, under the owner's standing OK.
- **Merge:** `edb61d5d9` merges W36 (`d0136473a`), so DB FC rows use the 1690 s deadline. MP FC stays at 1200 s.
- **Runs:** NON-counted. `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, seed 130365, Standard Regular, en.
  - Unity 6000.5.8f1 via `InvokeUnity.ps1 -GuiLicensing`.
  - Main checkout detached at the commit under test.
- **Logs:** under `win-dev-unity\`:
  - `w16-baseline\`: integration `c11d6172a`, before the fix.
  - `w16-tests\`: the EditMode runs.
  - `w16-proof\`: `edb61d5d9`.
  - Each run folder has `menu\unity.log`, `terminal.txt` and `gate3.txt`. Harness evidence was moved to `harness-evidence\` and is not committed.

**Status: proof failed.** Do not merge `79d93b2eb` as is.
- **DB rows:** the escort change never got a reinforcement to the thinner zone. S008 and S012 still lose on tickets at the same moment as the baseline. S010 aborted with `ariaStopped:Blocked`, stop reason 115.
- **MP rows:** S060 went from Victory to Defeat. S055, S056 and S058 still win.
- **Gate 3:** PASS on all seven proof runs.

## Baseline (`c11d6172a`, deadline 1200)

| Row | Result | s | deadlinePct | zones own/opp | tickets |
|---|---|---|---|---|---|
| S008 DB FC ground est. | Abort (startup timeout, 316 s subscene import) | — | — | — | — |
| S010 DB FC air est. | Defeat | 650.2 | 54.18 | 1/2 | 0/450 |
| S012 DB FC comb. est. | Defeat | 649.0 | 54.08 | 1/2 | 0/452 |
| S055 MP FC ground field | Victory | 613.6 | 51.13 | 3/0 | 500/0 |
| S056 MP FC ground est. | Victory | 545.8 | 45.48 | 2/1 | 500/0 |
| S058 MP FC air est. | Victory | 545.8 | 45.48 | 2/1 | 500/0 |
| S060 MP FC comb. est. | Victory | 621.2 | 51.77 | 2/0 | 500/0 |

## Diagnosis (baseline S010 and S012)
- The two runs are identical in shape:
  - ARIA holds both nearer zones by about 92 s.
  - Every escort, for groups 2–9, goes to the first zone.
  - The second zone is left empty, and the enemy wave takes it unopposed at about 141 s.
  - From then on the enemy holds two zones and drains our tickets.
- The enemy objective commander masses all its groups into one wave aimed at the zone centres.

## Fix (`79d93b2eb`) and EditMode tests
- **Change:** in `AriaObjectiveEscortPolicy.Goal`, Frontline Control only. When the committed zone is owned and uncontested, escorts go to the other owned, uncontested zone that has fewer own groups moving to or holding it.
- **Test:** `FrontlineEscortsGarrisonTheThinnerOwnedZone`, six cases.
- **Results:**
  - `AriaObjectiveSkillTests.RunFocusedValidation`: `[AriaObjectiveSkill] result=Passed controls=19`.
  - `AriaObjectiveCoverageTests`: `[AriaObjectiveCoverage] result=Passed controls=9`.
- **Log overwrite:** a log-path quoting error made both runs write the same Unity log, so the AriaObjectiveSkillTests Unity log was overwritten. Its pass marker survives in `w16-tests\AriaObjectiveSkillTests.wrapper.out`; the AriaObjectiveCoverageTests log is complete.

## Proof (`edb61d5d9`, DB deadline 1690, MP deadline 1200)

| Row | Result | s | deadlinePct | zones own/opp | tickets | vs baseline |
|---|---|---|---|---|---|---|
| S008 DB FC | Defeat | 684.8 | 40.52 | 1/2 | 0/452 | baseline aborted |
| S010 DB FC | **Abort**: Blocked 115 at 233.9 s | 233.9 | 13.84 | 1/2 | 414/453 | was Defeat |
| S012 DB FC | Defeat | 649.4 | 38.43 | 1/2 | 0/451 | same |
| S055 MP FC | Victory | 545.7 | 45.47 | 2/0 | 500/0 | 51.13 → 45.47 |
| S056 MP FC | Victory | 546.0 | 45.50 | 2/1 | 500/0 | same |
| S058 MP FC | Victory | 547.3 | 45.61 | 2/0 | 500/0 | same |
| S060 MP FC | **Defeat** | 648.1 | 54.01 | 0/3 | 0/464 | was Victory |

- **deadlinePct** is the game's `[SkirmishTerminalObjective]` value. The runner's `deadline_pct` column still divides FC time by 1200 (S008 shows 57.07 %), so use the game value.
- **Every run:** `unused_starting=-`, `roster=match`, menu journey Passed.

## Why the fix did not work
- **DB S008, S010, S012:**
  - The garrison offer fires as intended: about 92 s, both zones owned, group 2 sent to the second zone.
  - The escort goal then flips between the second zone and the first zone whenever the enemy wave contests the first. Group 2's offer is re-issued every 11–22 s, alternating with offers for groups 8/9 to the first zone.
  - No group reaches the second zone. It falls at 142–144 s, the same moment as in the baseline.
  - Escort acceptance is still not traced (`escort_accepted` gap, noted by the lead), so I cannot tell whether group 2 accepted any of the four offers.
- **MP S060:**
  - At 45.7 s the garrison rule sent group 1 toward the second zone. Group 1 was the chooser's own holder of the first zone.
  - The chooser then moved group 1 to the second zone (73.9 s) and group 2 back to the first.
  - During that shuffle the enemy wave took all three zones by 181 s.
  - The baseline also flips escort goals, but it kept group 1 holding and retook zones by 159 s.
- **S010 Blocked 115:**
  - ARIA pressed the Move control three times with `selectedGroups=0`, so the press never entered move mode. Intent was Advance, supportStage 2.
  - This is the same stop code as W27c (S038), and it is the next task.

## Proposed next step (lead's call)
1. Leave `79d93b2eb` unmerged, or revert it on this branch.
2. A second attempt would have to:
   - keep a garrison goal stable once chosen, instead of falling back to the committed zone whenever that zone is briefly contested;
   - never send the chooser's own holding group.
   That still needs this same proof set to pass, MP rows included.
