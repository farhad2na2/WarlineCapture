# Lane worker 38, 2026-11-18: Mac native check of City Crossroads FC S031-S036

HEAD 9570c9e70 (W4+W7 merge aee6ee07c), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane38/`.
No asset or code changes. disk_guard 0 before every launch; one Editor at a time; Fig/Fig-SquadPlay Editors untouched.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 40 s | compile1.log:635; Library Redirect Path :93 |
| 1 python | 153 OK | python.txt:33-35 |
| 1 quickcheck S031-S036 | 90/90 objective probes on 18 size rows, result=checked, no battles | quickcheck.txt:1-2 |
| 2 containment | 6/6 Passed failures=0 (rows 168 field / 256 established) | contain-S031..S036.log :675 :769 :673 :769 :677 :769 |
| 3 queue (seed 130365 en) | 6/6 Victory TicketsDepleted, Completed wins=6 sourceUnchanged=1 | q/unity.log:74155 |

## Step 3 per job (q/unity.log)

| Row | Terminal | Clock (deadline %) | Zones own/opp | Hold choice/tap | Menu | Probe |
|---|---|---|---|---|---|---|
| S031 ground field | :10459 | 550.97 s (45.91) | 2/1 | 2/2 | :2397 Passed | :10751 Passed |
| S032 ground established | :22773 | 543.30 s (45.28) | 2/1 | 4/2 | :11520 Passed | :23027 Passed |
| S033 air field | :32609 | 553.64 s (46.14) | 2/1 | 2/1 | :23835 Passed | :32863 Passed |
| S034 air established | :49078 | 963.56 s (80.30), tickets 122/0 | 2/1 | 10/4 | :33684 Passed | :49370 Passed |
| S035 combined field | :58934 | 579.93 s (48.33) | 2/1 | 6/4 | :50128 Passed | :59208 Passed |
| S036 combined established | :73587 | 780.55 s (65.05), tickets 338/0 | 2/1 | 7/2 | :59956 Passed | :73877 Passed |

All six: appended=1 countedWin=1, terminal probe MAIN MENU interventions=0. Whole log: 0 [AriaBlocked]/[AriaBlockedControl],
0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 error CS. No first-visit seed rejection.

## Findings
- Unlike the Desert Base FC established losses, ARIA chose Hold on every City Crossroads row, and all six won.
- Mac clocks match Windows W7 within 2 points except S034 (80.30% vs 76.42%) and S036 (65.05% vs 45.28%); S034 is
  the slowest row and the closest to the deadline.
- Gate-3 unused is not empty on any row (:10445, :22759, :32595, :49064, :58920, :73573). S033-S036 list building.airport
  and every aircraft role: ARIA used no aircraft on the air and combined rows (as on Windows; no measured Airport on City Crossroads).
- S031's menu journey took a longer path (Hotspot x3 > BackButton > TheaterPin_CC > ...) before reaching BattleCard_S031; Passed.

## Open items
- Gate-3 decision on air/combined rows with no aircraft use (S033-S036) is the lead's call; runs.csv rows stay planned.
- Counted seeds 155923 / 104731 / 130365 fa-IR not run (not in this brief).
