# Lane worker 47, 2026-11-20: counted runs on FC 1690 s + W27c (S019 CE rerun, CC FC S031/S033/S035, CC BT S038/S040/S042)

HEAD ef96020cf at start and end (W36 FC deadlines 1690/2113/2535 s, W27c ARIA same-control press bound), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-20/lane47/`.
No asset or code changes. disk_guard 0 before each launch; one Editor of mine at a time (main checkout); no Editor stopped.
The stale working-tree files were left alone; this commit stages explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 97 s | compile1.log:1931; Library Redirect Path :93 |
| 1 python | 159 run, 2 FAILED: the 2 known test_quickcheck failures, no others | python.txt:33,57,74 |
| 2 queue A (26 jobs, one launch) | 26/26 wins (25 counted + warm-up); Completed jobs=26 recorded=26 wins=26 sourceUnchanged=1 exitCode=0 | a/unity.log:349983 |
| 4 gate 3 (objective rule) | counted 25/25 PASS, roster=match (warm-up :1 PASS, excluded) | gate3.txt:2-26 |

Flags: `--menu-journey --validation-library --no-table --timeout 30000`. Queue ran 03:29-06:52.
runs.csv folder `_Evidence/candidates/a762c8426991dbd38c82f85660e7b79cab8f8b6e6311023540ea7599e47eca15/` (S019 1 row; S031, S033, S035, S038, S040, S042 4 rows each). The S021 file holds only the warm-up row and is not committed.

## Warm-up (not counted)

- Job 1 S021 130365 en: Victory ConvoyDelivered 170.836 / 525.0 s (32.54%), completed=2/2 lost=0 (:6991); menu journey Passed :1901, return probe Passed :7265, no startup failure. The cold SubScene import that aborted lane 46's first job did not recur.

## Pacing (counted)

- Deadlines honoured: S019 deadline=525.0; 12 FC lines deadline=1690.0; 12 BT lines deadline=540.0.
- S019 CE 130365 en: Victory ConvoyDelivered 32.17% of 525 s. With lane 46's three wins S019 now has 4/4.
- CC FC S031/S033/S035: 12/12 Victory TicketsDepleted, ownZones=2 opposingZones=1 at the end, 32.11-42.10% of 1690 s (lane 46 on 1200 s: 48-57%).
- CC BT S038/S040/S042: 12/12 Victory BreakthroughEvacuated 8/8 lost=0, 40.45-49.91% of 540 s. S038 130365 en (the W27c Blocked 115 case) won at 45.17%.

## Losses and aborts

None. Whole log: 0 [AriaBlocked], 0 ariaStopped, 0 order_blocked, 0 order_retry, 0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 aborted=True, 0 FailureCode, 0 MissingReference, 0 error CS, 0 "First-visit ARIA seed". Counted: menu journey Passed 25/25; return probe Passed 25/25 (warm-up 1/1 each, reported separately).

## Per job

| Job | Row / seed / locale | Terminal | Result | Clock / deadline (%) | Objective | Menu | Probe | AriaBlocked | ariaStopped | startupFail | countedWin | Gate 3 (gate3.txt line) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 (WARM-UP, not counted) | S021 130365 en | :6991 | Victory ConvoyDelivered | 170.836 / 525.0 s (32.54%) | completed=2/2 lost=0 | Passed :1901 | Passed :7265 | 0 | 0 | 0 | 1 :7336 | PASS :1 |
| 2 | S019 130365 en | :12849 | Victory ConvoyDelivered | 168.912 / 525.0 s (32.17%) | completed=2/2 lost=0 | Passed :8045 | Passed :13106 | 0 | 0 | 0 | 1 :13177 | PASS :2 |
| 3 | S031 130365 en | :21576 | Victory TicketsDepleted | 578.960 / 1690.0 s (34.26%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :13956 | Passed :21832 | 0 | 0 | 0 | 1 :21903 | PASS :3 |
| 4 | S031 155923 en | :33438 | Victory TicketsDepleted | 672.903 / 1690.0 s (39.82%) | ownZones=2 opposingZones=1 tickets=443/0 | Passed :22660 | Passed :33730 | 0 | 0 | 0 | 1 :33801 | PASS :4 |
| 5 | S031 104731 en | :41411 | Victory TicketsDepleted | 578.816 / 1690.0 s (34.25%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :34538 | Passed :41685 | 0 | 0 | 0 | 1 :41756 | PASS :5 |
| 6 | S031 130365 fa-IR | :52875 | Victory TicketsDepleted | 675.854 / 1690.0 s (39.99%) | ownZones=2 opposingZones=1 tickets=438/0 | Passed :42513 | Passed :53144 | 0 | 0 | 0 | 1 :53215 | PASS :6 |
| 7 | S033 130365 en | :65829 | Victory TicketsDepleted | 711.536 / 1690.0 s (42.10%) | ownZones=2 opposingZones=1 tickets=407/0 | Passed :53952 | Passed :66105 | 0 | 0 | 0 | 1 :66176 | PASS :7 |
| 8 | S033 155923 en | :78249 | Victory TicketsDepleted | 670.151 / 1690.0 s (39.65%) | ownZones=2 opposingZones=1 tickets=439/0 | Passed :66917 | Passed :78505 | 0 | 0 | 0 | 1 :78576 | PASS :8 |
| 9 | S033 104731 en | :89495 | Victory TicketsDepleted | 665.153 / 1690.0 s (39.36%) | ownZones=2 opposingZones=1 tickets=430/0 | Passed :79313 | Passed :89788 | 0 | 0 | 0 | 1 :89859 | PASS :9 |
| 10 | S033 130365 fa-IR | :98155 | Victory TicketsDepleted | 579.446 / 1690.0 s (34.29%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :90537 | Passed :98423 | 0 | 0 | 0 | 1 :98494 | PASS :10 |
| 11 | S035 130365 en | :110610 | Victory TicketsDepleted | 665.236 / 1690.0 s (39.36%) | ownZones=2 opposingZones=1 tickets=446/0 | Passed :99231 | Passed :110867 | 0 | 0 | 0 | 1 :110938 | PASS :11 |
| 12 | S035 155923 en | :123066 | Victory TicketsDepleted | 664.688 / 1690.0 s (39.33%) | ownZones=2 opposingZones=1 tickets=447/0 | Passed :111657 | Passed :123323 | 0 | 0 | 0 | 1 :123394 | PASS :12 |
| 13 | S035 104731 en | :130934 | Victory TicketsDepleted | 579.041 / 1690.0 s (34.26%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :124152 | Passed :131244 | 0 | 0 | 0 | 1 :131315 | PASS :13 |
| 14 | S035 130365 fa-IR | :139054 | Victory TicketsDepleted | 542.632 / 1690.0 s (32.11%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :132076 | Passed :139358 | 0 | 0 | 0 | 1 :139429 | PASS :14 |
| 15 | S038 130365 en | :150625 | Victory BreakthroughEvacuated | 243.894 / 540.0 s (45.17%) | completed=8/8 lost=0 | Passed :140079 | Passed :150916 | 0 | 0 | 0 | 1 :150987 | PASS :15 |
| 16 | S038 155923 en | :158421 | Victory BreakthroughEvacuated | 232.128 / 540.0 s (42.99%) | completed=8/8 lost=0 | Passed :151706 | Passed :158713 | 0 | 0 | 0 | 1 :158784 | PASS :16 |
| 17 | S038 104731 en | :235375 | Victory BreakthroughEvacuated | 224.102 / 540.0 s (41.50%) | completed=8/8 lost=0 | Passed :159503 | Passed :235667 | 0 | 0 | 0 | 1 :235738 | PASS :17 |
| 18 | S038 130365 fa-IR | :243110 | Victory BreakthroughEvacuated | 233.969 / 540.0 s (43.33%) | completed=8/8 lost=0 | Passed :236499 | Passed :243415 | 0 | 0 | 0 | 1 :243486 | PASS :18 |
| 19 | S040 130365 en | :254850 | Victory BreakthroughEvacuated | 265.046 / 540.0 s (49.08%) | completed=8/8 lost=0 | Passed :244205 | Passed :255105 | 0 | 0 | 0 | 1 :255176 | PASS :19 |
| 20 | S040 155923 en | :263139 | Victory BreakthroughEvacuated | 259.384 / 540.0 s (48.03%) | completed=8/8 lost=0 | Passed :255913 | Passed :263395 | 0 | 0 | 0 | 1 :263466 | PASS :20 |
| 21 | S040 104731 en | :279037 | Victory BreakthroughEvacuated | 265.259 / 540.0 s (49.12%) | completed=8/8 lost=0 | Passed :264185 | Passed :279329 | 0 | 0 | 0 | 1 :279400 | PASS :21 |
| 22 | S040 130365 fa-IR | :287069 | Victory BreakthroughEvacuated | 268.529 / 540.0 s (49.73%) | completed=8/8 lost=0 | Passed :280119 | Passed :287373 | 0 | 0 | 0 | 1 :287444 | PASS :22 |
| 23 | S042 130365 en | :295652 | Victory BreakthroughEvacuated | 269.505 / 540.0 s (49.91%) | completed=8/8 lost=0 | Passed :288163 | Passed :295943 | 0 | 0 | 0 | 1 :296014 | PASS :23 |
| 24 | S042 155923 en | :315308 | Victory BreakthroughEvacuated | 218.434 / 540.0 s (40.45%) | completed=8/8 lost=0 | Passed :296754 | Passed :315582 | 0 | 0 | 0 | 1 :315653 | PASS :24 |
| 25 | S042 104731 en | :340715 | Victory BreakthroughEvacuated | 262.260 / 540.0 s (48.57%) | completed=8/8 lost=0 | Passed :316390 | Passed :340973 | 0 | 0 | 0 | 1 :341044 | PASS :25 |
| 26 | S042 130365 fa-IR | :349101 | Victory BreakthroughEvacuated | 268.610 / 540.0 s (49.74%) | completed=8/8 lost=0 | Passed :341765 | Passed :349405 | 0 | 0 | 0 | 1 :349476 | PASS :26 |
