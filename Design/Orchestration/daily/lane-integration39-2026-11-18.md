# Lane worker 39, 2026-11-18: Mac native check of Mountain Pass FC S055-S060

HEAD f0a0f7c06 (includes W8 merge b77ef5114), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane39/`.
No asset or code changes. disk_guard 0 before every launch; one Editor at a time; Fig/Fig-SquadPlay Editors untouched.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 49 s | compile1.log:642; Library Redirect Path :93 |
| 1 python | 153 OK | python.txt:33-35 |
| 1 quickcheck S055-S060 | 90/90 objective probes on 18 size rows, result=checked, no battles | quickcheck.txt:1-2 |
| 2 containment | 6/6 Passed failures=0 (rows 168 field / 256 established) | contain-S055..S060.log :676 :770 :675 :770 :674 :770 |
| 3 queue (seed 130365 en) | 2/6 Victory (S057, S059), 4 Defeat TicketsDepleted; Completed wins=2 sourceUnchanged=1 | q/unity.log:77021 |

## Step 3 per job (q/unity.log)

| Row | Terminal | Result | Clock (deadline %) | Zones own/opp, tickets | Hold choice/tap | First own zone / max own | Menu | Probe |
|---|---|---|---|---|---|---|---|---|
| S055 ground field | :11403 | Defeat | 616.20 s (51.35) | 0/2, 0/457 | 1/1 | 30.1 s / 2 | :2311 Passed | :11659 Passed |
| S056 ground established | :29788 | Defeat | 601.34 s (50.11) | 1/2, 0/457 | 5/1 | 31.0 s / 2 | :12469 Passed | :30044 Passed |
| S057 air field | :37823 | Victory | 714.49 s (59.54) | 2/0, 500/0 | 6/3 | 30.4 s / 2 | :30854 Passed | :38080 Passed |
| S058 air established | :55200 | Defeat | 593.54 s (49.46) | 1/2, 0/477 | 1/1 | 31.0 s / 2 | :38890 Passed | :55456 Passed |
| S059 combined field | :61911 | Victory | 544.60 s (45.38) | 2/1, 500/0 | 2/2 | 30.4 s / 2 | :56302 Passed | :62168 Passed |
| S060 combined established | :76450 | Defeat | 592.57 s (49.38) | 0/2, 0/476 | 1/1 | 31.2 s / 2 | :62992 Passed | :76743 Passed |

All six: appended=1, terminal probe MAIN MENU interventions=0; countedWin=1 on S057 and S059 only. Whole log: 0 [AriaBlocked]/[AriaBlockedControl],
0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 error CS. No first-visit seed rejection. --validation-library enabled=1 (:600).

## Zone timeline (ARIA `event=score` lines, own/opp@t; sampled, not every change)
- S055: 1/0@30 2/0@45 1/0@88 0/0@108 0/1@109 0/2@116 (to the end). Hold only at t=39 (:4698).
- S056: 1/0@31 2/0@47 1/0@89 0/0@93 0/2@101 0/3@125 0/2@437 1/2@452. Hold at t=47 (:15884), then t=411, 430, 445, 516 (after the zones were lost).
- S057 (won): 1/0@30 2/0@45 1/0@93 1/1@114 1/0@254 2/0@262. Hold at t=38, 118, 133, 162, 236, 246.
- S058: 1/0@31 2/0@45 1/0@69 1/1@77 0/1@86 0/2@150 1/2@332. Hold only at t=44 (:42390).
- S059 (won): 1/0@30 2/0@45 2/1@80. Hold at t=38, 93 (:60264).
- S060: 1/0@31 2/0@46 0/2@129. Hold only at t=46 (:66405).

## Findings
- Every row took both near zones by ~45 s. The four losing rows lost both by 108-150 s and never recovered. In S055/S058/S060, ARIA chose Hold exactly
  once (~45 s) and only Move afterwards. In S056, its later Holds (411-516 s) came after the zones were gone. The winners chose Hold again after 90 s.
  For W16: on City Crossroads (lane 38), ARIA held every row and won. On Mountain Pass, a single early Hold loses, so the Hold re-issue cadence is the comparison to make.
- S055 (ground field) lost on the Mac (51.35%) but won on Windows W8 (Victory 51.43%). The Mac field rows are not a clean 3/3 as on Windows.
  The established-row losses (S056/S058/S060 at ~50%) match Windows.
- Gate-3 unused is not empty on any row (:11389, :29774, :37809, :55186, :61897, :76436). S057-S060 list building.airport and the aircraft roles,
  so ARIA used no aircraft, as on Windows W8.
- S055's menu journey went Hotspot > BackButton > TheaterPin_MP > ... > BattleCard_S055 (first job of the session, as S031 in lane 38). It Passed.

## Open items
- Whether these runs.csv rows count is the lead's call (runs.csv under S05x/_Evidence/candidates/f2a1e744.../; evidence folders left untracked).
- Counted seeds 155923 / 104731 / 130365 fa-IR not run (not in this brief).
