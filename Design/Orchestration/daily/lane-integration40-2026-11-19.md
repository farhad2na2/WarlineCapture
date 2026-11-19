# Lane worker 40, 2026-11-19: counted runs for City Crossroads FC S031-S036

HEAD 033660b12, branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane40/`.
No asset or code changes. disk_guard 0 before both launches; one Editor at a time; Fig/Fig-SquadPlay Editors untouched.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor ~43 s | compile1.log:507; Library Redirect Path :93 |
| 2 counted queue (18 jobs, one launch) | 18/18 Victory TicketsDepleted, Completed wins=18 sourceUnchanged=1 exitCode=0 | counted/unity.log:227513 |

Queue: `--menu-journey --validation-library --no-table --timeout 30000`, validation library enabled=1 (:599). No first-visit seed rejection.
The queue ran 00:48-04:14 (about 3 h 26 min).

## Per job (counted/unity.log)

| Job | Row / seed / locale | Terminal | Clock (deadline %) | End zones, tickets | Hold choice/tap | First own / 2 own | Menu | Probe | countedWin |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S031 155923 en | :10905 | 551.67 s (45.97) | 2/1, 500/0 | 2/1 | 42.3 / 51.7 s | :2240 | :11161 | :11232 |
| 2 | S031 104731 en | :21261 | 542.43 s (45.20) | 2/1, 500/0 | 8/6 | 42.4 / 42.6 s | :11976 | :21517 | :21588 |
| 3 | S031 130365 fa-IR | :30745 | 551.92 s (45.99) | 2/1, 500/0 | 2/1 | 41.5 / 52.0 s | :22325 | :31014 | :31085 |
| 4 | S032 155923 en | :47434 | 980.27 s (81.69) | 2/1, 111/0 | 5/2 | 42.0 / 52.2 s | :31823 | :47690 | :47761 |
| 5 | S032 104731 en | :60716 | 542.52 s (45.21) | 2/1, 500/0 | 6/2 | 46.3 / 46.3 s | :48498 | :61008 | :61079 |
| 6 | S032 130365 fa-IR | :79558 | 1032.93 s (86.08) | 2/1, 36/0 | 8/3 | 41.8 / 52.1 s | :61817 | :79827 | :79898 |
| 7 | S033 155923 en | :89546 | 551.23 s (45.94) | 2/1, 500/0 | 2/2 | 41.6 / 51.3 s | :80636 | :89802 | :89873 |
| 8 | S033 104731 en | :97171 | 551.57 s (45.96) | 2/1, 500/0 | 2/1 | 42.3 / 51.6 s | :90592 | :97427 | :97498 |
| 9 | S033 130365 fa-IR | :108017 | 597.01 s (49.75) | 2/1, 472/0 | 5/4 | 41.6 / 52.1 s | :98258 | :108286 | :108357 |
| 10 | S034 155923 en | :123165 | 794.71 s (66.23) | 2/1, 273/0 | 5/3 | 41.8 / 51.6 s | :109094 | :123439 | :123510 |
| 11 | S034 104731 en | :134734 | 551.80 s (45.98) | 2/1, 500/0 | 2/2 | 42.3 / 51.9 s | :124250 | :135027 | :135098 |
| 12 | S034 130365 fa-IR | :146878 | 542.59 s (45.22) | 2/1, 500/0 | 6/4 | 43.3 / 43.3 s | :135855 | :147182 | :147253 |
| 13 | S035 155923 en | :158791 | 761.87 s (63.49) | 2/1, 400/0 | 5/3 | 41.7 / 51.5 s | :147993 | :159047 | :159118 |
| 14 | S035 104731 en | :168930 | 551.82 s (45.98) | 2/1, 500/0 | 2/1 | 42.3 / 51.9 s | :159856 | :169186 | :169257 |
| 15 | S035 130365 fa-IR | :177158 | 551.75 s (45.98) | 2/1, 500/0 | 2/1 | 41.6 / 51.8 s | :169994 | :177426 | :177497 |
| 16 | S036 155923 en | :196522 | 879.62 s (73.30) | 2/1, 189/0 | 7/4 | 41.9 / 52.1 s | :178234 | :196797 | :196868 |
| 17 | S036 104731 en | :211664 | 542.87 s (45.24) | 2/1, 500/0 | 6/4 | 44.9 / 44.9 s | :197605 | :211955 | :212026 |
| 18 | S036 130365 fa-IR | :226751 | 542.52 s (45.21) | 2/1, 500/0 | 7/2 | 46.4 / 46.4 s | :212786 | :227055 | :227126 |

Every job: menu journey Passed (job 1 via TheaterPin_CC, the rest via ObjectiveTab_FC>BattleCard_S03x), terminal probe Passed MAIN MENU
interventions=0, appended=1 countedWin=1. Whole log: 0 [AriaBlocked]/[AriaBlockedControl], 0 [StartingGrantPlacement] failed,
0 startupFailure, 0 BlockedSpawn, 0 FailureCode, 0 MissingReference, 0 error CS. Gate-3 unused is not empty on any job (5-16 entries);
S033-S036 list building.airport and the aircraft roles (ARIA used no aircraft). Per-job detail: lane40/extract.txt.

## Findings
- With lane 38, City Crossroads FC S031-S036 now has 24/24 Mac Victories (4 per row: seeds 130365 en, 155923 en, 104731 en, 130365 fa-IR).
- 13 of the 18 jobs won at 45-50% of the deadline after holding 2/1 from about 52 s to the end.
- The 5 slow wins were S032 155923 and 130365 fa-IR, S034 155923, S035 155923 and S036 155923, at 63-86%. In each, ARIA fell to 1/2 own/opp
  at 223-354 s and its own tickets drained (S032 130365 fa-IR reached 36/0). It won the zone back to 2/1 at 462-783 s after more Hold choices.
  The established rows S032 and S036 were the closest. S032 130365 fa-IR ended at 86% with 36 tickets left, a thin margin for a counted win.
- ARIA chose Hold on all 18 jobs, first at 38-46 s, matching lane 38 and unlike the Desert Base and Mountain Pass FC losses (W16).
- No losses, so no loss zone timelines were needed. The zone timelines for every job are in extract.txt.
- Not tested: aircraft use (gate-3 still lists the airport and the air roles on S033-S036) and real player or device acceptance.
