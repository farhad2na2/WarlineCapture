# Lane worker 48b, 2026-11-20: rest of lane 48 on W16 attempt 2 (DB FC S010 x3, S011, S012; CC FC S032/S034/S036) at 1690 s

Code cf5f53bd1 (W16 attempt 2 + W36 FC 1690 s + W27c); HEAD c45f4becf at start, orchestration-only commits since. Branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-20/lane48b/`.
No asset or code changes; no compile check (no recompile reported; same candidate hash 8f7bb8da... as lane 48); Python tests skipped per brief. disk_guard 0 before each launch.
The stale working-tree files were left alone; commits stage explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 2 queue A attempt 1 (FAILED) | Editor pid 48605 stuck on a startup modal dialog after licensing, no Library Redirect Path, 0/24 jobs; stopped by the program lead 11:25:51 (queue rc=143), who moved lane 48's stale Temp/__Backupscenes/0.backup to lane48b/temp-backup-moved/ | a/unity.log:42 (last line); a.out.txt; commit c57a30182 |
| 2 queue A attempt 2 (24 jobs, one launch) | Completed jobs=24 recorded=24 wins=18 sourceUnchanged=1 exitCode=0; counted 17 W / 3 L / 3 A of 23; warm-up won | a2/unity.log:300574; a2.out.txt; Library Redirect Path :92 |
| 4 gate 3 (objective rule) | counted 21 PASS + 1 INCOMPLETE (S011 fa-IR consent abort) of 22 lines, roster=match on all; S011 155923 startup abort has no line; warm-up :1 PASS, excluded | gate3.txt:2-23 |

Flags: `--menu-journey --validation-library --no-table --timeout 30000`. Attempt 2 ran 11:26:33-16:58:41. Every terminal line logs deadline=1690.0 (22/22; the 2 aborts before a terminal line have none).
runs.csv folder `_Evidence/candidates/8f7bb8da9b7eb15d127c1b2109ea650864347cc7a57b655607632cc16f4f0c7e/`: S010 (lane 48 job 14 row + 3 rows here), S011, S012, S032, S034, S036 (4 rows each) committed; the S031 file holds only warm-up rows (lane 48 + 48b) and is not committed.

## Warm-up (not counted)

- Job 1 S031 130366 en: Victory TicketsDepleted 586.103 / 1690.0 s (34.68%) (:11985); menu Passed :1845, probe Passed :12285; gate 3 PASS.

## Per mission (counted)

- S010 DB FC: 3/3 Victory, 35.00-35.16%. With lane 48's 130365 en win (35.22%): 4/4.
- S011 DB FC: W1 L1 A2. 130365 en W 35.08%; 155923 en ABORT startupFailure=Timeout; 104731 en LOSS 37.75%; 130365 fa-IR ABORT exception.
- S012 DB FC: W2 L2. 130365 en LOSS 36.83%; 155923 en LOSS 36.58%; 104731 en W 35.20%; 130365 fa-IR W 35.10%.
- S032 CC FC: W3 A1. 40.80 / 36.79 / 39.69%; 130365 fa-IR ABORT ariaStopped:Blocked at 32.84% while ahead (tickets 500/71).
- S034 CC FC: 4/4 Victory, 36.84 / 32.77 / 40.39 / 37.18%.
- S036 CC FC: 4/4 Victory, 41.69 / 39.69 / 40.27 / 39.45%.
- Every win ends ownZones=2 opposingZones=1; every loss ends ownZones=1 opposingZones=2 tickets=0.

## Losses and aborts (first failing line)

- Job 6 S011 155923 en: Abort startupFailure=Timeout, match 0 s, job 970.8 s. First failing line a2/unity.log:76545 `[SkirmishCandidateFingerprint] ... aborted=True reason=startupFailure=Timeout`; before it a SubScene import (:76479, :76487) and a 22.6 s DB scene load with 21.8 s thread wait (:76486).
- Job 7 S011 104731 en: Defeat TicketsDepleted 637.924 s (37.75%) ownZones=1 opposingZones=2 tickets=0/465 (:86081). First failing line :81665, an own zone lost (2/1 -> 1/1 at t=130.55); opposingZones=2 at :81763 (t=138.38).
- Job 8 S011 130365 fa-IR: Abort exception, job 471.5 s. First failing line :89125 `InvalidOperationException: Visible ARIA consent flow timed out.` (AriaConsentProbe.cs:40, match t~23 s); :89138 aborted=True reason=exception.
- Job 9 S012 130365 en: Defeat TicketsDepleted 622.384 s (36.83%) ownZones=1 opposingZones=2 tickets=0/492 (:98993). First failing line :95268 (2/1 -> 1/1 at t=114.91); opposingZones=2 at :95412 (t=122.80).
- Job 10 S012 155923 en: Defeat TicketsDepleted 618.279 s (36.58%) ownZones=1 opposingZones=2 tickets=0/496 (:109119). First failing line :105136 (2/1 -> 1/1 at t=110.84); opposingZones=2 at :105222 (t=118.66).
- Job 16 S032 130365 fa-IR: Abort ariaStopped:Blocked at 555.036 s (32.84%), ownZones=2 opposingZones=1 tickets=500/71. First failing line :187388 `[AriaBlocked] reason=115 ... kind=Control ... intent=Scout`; :187404 AriaBlockedControl control=PreviousSquadPage; :187449 aborted=True.

Whole log a2: 1 [AriaBlocked], 1 [AriaBlockedControl], 2 ariaStopped lines (job 16), 0 order_blocked, 0 order_retry, 0 [StartingGrantPlacement] failed, 2 startupFailure lines (job 6), 0 BlockedSpawn, 3 aborted=True, 0 FailureCode, 0 MissingReference, 0 error CS, 0 "First-visit ARIA seed". Counted: menu journey Passed 23/23; return probe Passed 20/20 on the jobs that reached a result (none on the 3 aborts); countedWin=1 17/23.

## Per job

| Job | Row / seed / locale | Terminal | Result | Clock / deadline (%) | Objective | Menu | Probe | AriaBlocked | ariaStopped | startupFail | countedWin | Gate 3 (gate3.txt line) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 (WARM-UP, not counted) | S031 130366 en | :11985 | Victory TicketsDepleted | 586.103 / 1690.0 s (34.68%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :1845 | Passed :12285 | 0 | 0 | 0 | 1 :12356 | PASS :1 |
| 2 | S010 155923 en | :29902 | Victory TicketsDepleted | 593.879 / 1690.0 s (35.14%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :12964 | Passed :30179 | 0 | 0 | 0 | 1 :30250 | PASS :2 |
| 3 | S010 104731 en | :46786 | Victory TicketsDepleted | 591.424 / 1690.0 s (35.00%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :30976 | Passed :47133 | 0 | 0 | 0 | 1 :47204 | PASS :3 |
| 4 | S010 130365 fa-IR | :63489 | Victory TicketsDepleted | 594.270 / 1690.0 s (35.16%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :47911 | Passed :63814 | 0 | 0 | 0 | 1 :63885 | PASS :4 |
| 5 | S011 130365 en | :75345 | Victory TicketsDepleted | 592.914 / 1690.0 s (35.08%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :64569 | Passed :75605 | 0 | 0 | 0 | 1 :75676 | PASS :5 |
| 6 | S011 155923 en | - | Abort startupFailure=Timeout :76545 | - | - | Passed :76408 | - | 0 | 0 | 2 | 0 :76620 | - (no line, startup abort) |
| 7 | S011 104731 en | :86081 | Defeat TicketsDepleted | 637.924 / 1690.0 s (37.75%) | ownZones=1 opposingZones=2 tickets=0/465 | Passed :77265 | Passed :86389 | 0 | 0 | 0 | 0 :86460 | PASS :6 |
| 8 | S011 130365 fa-IR | - | Abort exception :89138 | - | - | Passed :87117 | - | 0 | 0 | 0 | 0 :89195 | INCOMPLETE :7 |
| 9 | S012 130365 en | :98993 | Defeat TicketsDepleted | 622.384 / 1690.0 s (36.83%) | ownZones=1 opposingZones=2 tickets=0/492 | Passed :89819 | Passed :99265 | 0 | 0 | 0 | 0 :99336 | PASS :8 |
| 10 | S012 155923 en | :109119 | Defeat TicketsDepleted | 618.279 / 1690.0 s (36.58%) | ownZones=1 opposingZones=2 tickets=0/496 | Passed :99951 | Passed :109427 | 0 | 0 | 0 | 0 :109498 | PASS :9 |
| 11 | S012 104731 en | :122659 | Victory TicketsDepleted | 594.936 / 1690.0 s (35.20%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :110159 | Passed :123005 | 0 | 0 | 0 | 1 :123076 | PASS :10 |
| 12 | S012 130365 fa-IR | :134751 | Victory TicketsDepleted | 593.240 / 1690.0 s (35.10%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :123772 | Passed :135037 | 0 | 0 | 0 | 1 :135108 | PASS :11 |
| 13 | S032 130365 en | :148034 | Victory TicketsDepleted | 689.566 / 1690.0 s (40.80%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :135846 | Passed :148288 | 0 | 0 | 0 | 1 :148359 | PASS :12 |
| 14 | S032 155923 en | :161346 | Victory TicketsDepleted | 621.716 / 1690.0 s (36.79%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :149043 | Passed :161600 | 0 | 0 | 0 | 1 :161671 | PASS :13 |
| 15 | S032 104731 en | :174809 | Victory TicketsDepleted | 670.779 / 1690.0 s (39.69%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :162277 | Passed :175081 | 0 | 0 | 0 | 1 :175152 | PASS :14 |
| 16 | S032 130365 fa-IR | :187435 | Abort ariaStopped:Blocked :187449 | 555.036 / 1690.0 s (32.84%) | ownZones=2 opposingZones=1 tickets=500/71 | Passed :175762 | - | 2 | 2 | 0 | 0 :187506 | PASS :15 |
| 17 | S034 130365 en | :200660 | Victory TicketsDepleted | 622.628 / 1690.0 s (36.84%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :188167 | Passed :200932 | 0 | 0 | 0 | 1 :201003 | PASS :16 |
| 18 | S034 155923 en | :213359 | Victory TicketsDepleted | 553.846 / 1690.0 s (32.77%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :201653 | Passed :213762 | 0 | 0 | 0 | 1 :213833 | PASS :17 |
| 19 | S034 104731 en | :227083 | Victory TicketsDepleted | 682.657 / 1690.0 s (40.39%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :214464 | Passed :227355 | 0 | 0 | 0 | 1 :227426 | PASS :18 |
| 20 | S034 130365 fa-IR | :242082 | Victory TicketsDepleted | 628.287 / 1690.0 s (37.18%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :228079 | Passed :242402 | 0 | 0 | 0 | 1 :242473 | PASS :19 |
| 21 | S036 130365 en | :257572 | Victory TicketsDepleted | 704.523 / 1690.0 s (41.69%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :243134 | Passed :257898 | 0 | 0 | 0 | 1 :257969 | PASS :20 |
| 22 | S036 155923 en | :272727 | Victory TicketsDepleted | 670.712 / 1690.0 s (39.69%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :258622 | Passed :272999 | 0 | 0 | 0 | 1 :273070 | PASS :21 |
| 23 | S036 104731 en | :286490 | Victory TicketsDepleted | 680.602 / 1690.0 s (40.27%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :273738 | Passed :286744 | 0 | 0 | 0 | 1 :286815 | PASS :22 |
| 24 | S036 130365 fa-IR | :299737 | Victory TicketsDepleted | 666.710 / 1690.0 s (39.45%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :287534 | Passed :300003 | 0 | 0 | 0 | 1 :300074 | PASS :23 |

