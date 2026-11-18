# Lane integration 36 — 2026-11-18

Lane worker 36. HEAD 3e65ef053 (W13 merged). Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane36/`.
Fig-SquadPlay Editor (PID 36170) was open the whole shift and was not touched. One WarlineCapture Editor at a time; no Editor stopped by hand.

## Step 1: compile and Python
- `compile1.log:1677` `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed`, 0 `error CS`, wrapper exit 0, Library Redirect Path :93.
- Python: 153 tests OK (`python.txt:33-35`).

## Step 2: counted S004 queue (W13 enemy base)
Command as the brief (`--menu-journey --no-table --timeout 9000`); `--validation-library` was not needed.
`s004/unity.log:1390901` `[SkirmishAriaQueue] result=Completed jobs=4 recorded=4 wins=4 sourceUnchanged=1`, queue rc=0.

Enemy (faction 2) starting structures, identical on all 4 jobs and equal to the W13 Windows table:

| structure | preferred | actual | W13 table |
|---|---|---|---|
| refinery | (894,562) | (914,579) | (914,579) |
| fuel_bladder | (917,588) | (900,579) | (900,579) |
| ground_staging | (860,537) | (852,529) | (852,529) |
| barracks | (888,533) | (885,526) | (885,526) |

Lines: job 1 :2273-:2688, job 2 :329656-:330071, job 3 :706454-:706869, job 4 :977310-:977725.

| job | result | clock | % deadline | gate-3 unused | menu | return |
|---|---|---|---|---|---|---|
| 130367 en | Victory MainBaseDestroyed | 199.1 s | 18.43% | aa;apc_fast;attack_heli;drone | Passed :2030 | Passed :328657 |
| 155925 en | Victory MainBaseDestroyed | 365.7 s | 33.87% | attack_heli;drone | Passed :329440 | Passed :705464 |
| 104733 en | Victory MainBaseDestroyed | 274.4 s | 25.40% | aa;attack_heli;drone | Passed :706274 | Passed :976309 |
| 130367 fa-IR | Victory MainBaseDestroyed | 302.4 s | 28.00% | attack_heli;drone | Passed :977074 | Passed :1390653 |

Whole log: 0 `[AriaBlocked]`/`[AriaBlockedControl]`, 0 `[StartingGrantPlacement] failed`, 0 startupFailure, 0 BlockedSpawn, 0 `error CS`.
Harness appended 4 rows (countedWin=1) to `Design/AgentReports/SkirmishExpansion/S004/_Evidence/candidates/3be8c3d1.../runs.csv`; whether they count is the lead's call. The rest of that evidence folder stays untracked.

## Open
- attack_heli and drone unused on every S004 job (gate-3 decision pending with the lead).
- Real player/device acceptance not done (automated ARIA only).
