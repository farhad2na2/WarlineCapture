# Lane worker 46, 2026-11-19/20: DB CE S019-S024 (525 s) and FC S009/S031/S033 (1200 s) counted runs on the full ARIA stack

HEAD c11d6172a at start (W31d DB CE 525 s, W27b ARIA aircraft-service bound, W32b transport-heli escort, PR #80 terrain merge), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane46/`.
No asset or code changes. disk_guard 0 before each launch; one Editor of mine at a time (main checkout); no Editor stopped. HEAD moved to f9678edb9 during the run (STATUS.md only); the queue reported sourceUnchanged=1.
The stale working-tree files were left alone; this commit stages explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 894 s (PR #80 reimport + recompile) | compile1.log:2694; Library Redirect Path :93 |
| 1 python | 159 run, 2 FAILED: the 2 known test_quickcheck failures, no others | python.txt:33,57,74 |
| 2 queue A (36 jobs, one launch) | 33/36 wins; Completed jobs=36 recorded=36 wins=33 sourceUnchanged=1 exitCode=0 | a/unity.log:298082 |
| 4 gate 3 (objective rule) | 35/35 PASS, roster=match 35/35 (S019 abort has no gate line) | gate3.txt:1-35 |

Flags: `--menu-journey --validation-library --no-table --timeout 30000`. Queue ran 22:27-03:13.
runs.csv folder `_Evidence/candidates/3e52588bee9d481088b09bd7eeeb9afd2405deb780dea56dcbc4e4e28d25ecbc/` (4 rows each, S009, S019-S024, S031, S033).

## Pacing

- Deadlines honoured: 23 CE lines deadline=525.0 (24th job aborted before a terminal line), 12 FC lines deadline=1200.0.
- DB CE S019-S024: 23/24 Victory ConvoyDelivered 2/2 lost=0, 25.11-56.57% of 525 s. Over 50%: S020 fa-IR (53.34%), S022 all four (51.70-56.57%), S024 130365 en (55.36%).
- FC S031/S033: 8/8 Victory TicketsDepleted, ownZones=2 opposingZones=1 at the end, 48.25-57.07% of 1200 s.
- FC S009: 2/4 Victory (76.84%, 74.08%), 2 Defeat TicketsDepleted; the zone count swung 2/1 <-> 1/2 all match (extract-a.txt jobs 25-28).

## Losses and aborts (first failing line)

- S019 130365 en: Abort startupFailure=Timeout, match 0.0 s, job 434.7 s; first failing line :2095 `[SkirmishCandidateFingerprint] ... aborted=True reason=startupFailure=Timeout`, after SubScene re-imports at :2019 and :2036. First job of the first queue after PR #80; the queue had no warm-up row.
- S009 155923 en: Defeat TicketsDepleted 993.4 s (82.78%), ownZones=1 opposingZones=2 tickets=0/197 (:184224); first failing line :175946 opposing side reaches 2 zones at t=169.80.
- S009 130365 fa-IR: Defeat TicketsDepleted 786.7 s (65.56%), ownZones=1 opposingZones=2 tickets=0/345 (:210813); first failing line :203228 opposing side reaches 2 zones at t=133.23.

Whole log: 0 [AriaBlocked], 0 ariaStopped, 0 order_blocked, 16 order_retry, 0 [StartingGrantPlacement] failed, 1 startupFailure, 0 BlockedSpawn, 0 FailureCode, 0 MissingReference, 0 error CS, 0 "First-visit ARIA seed". Menu journey Passed 36/36; return probe Passed 35/35.

## Per job (a/unity.log)

| Job | Row / seed / locale | Terminal | Result | Clock / deadline (%) | Objective | Menu | Probe | AriaBlocked | ariaStopped | startupFail | countedWin | Gate 3 (gate3.txt line) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | S019 130365 en | - | Abort startupFailure=Timeout :2095 | 0.0 s, job 434.7 s | no match (SubScene re-import :2019, :2036) | Passed :1890 | - | 0 | 0 | 2 | 0 :2171 | - (no match) |
| 2 | S019 155923 en | :8812 | Victory ConvoyDelivered | 200.900 / 525.0 s (38.27%) | completed=2/2 lost=0 | Passed :3010 | Passed :9068 | 0 | 0 | 0 | 1 :9139 | PASS :1 |
| 3 | S019 104731 en | :15604 | Victory ConvoyDelivered | 185.987 / 525.0 s (35.43%) | completed=2/2 lost=0 | Passed :9858 | Passed :15861 | 0 | 0 | 0 | 1 :15932 | PASS :2 |
| 4 | S019 130365 fa-IR | :21951 | Victory ConvoyDelivered | 182.535 / 525.0 s (34.77%) | completed=2/2 lost=0 | Passed :16641 | Passed :22220 | 0 | 0 | 0 | 1 :22291 | PASS :3 |
| 5 | S020 130365 en | :30106 | Victory ConvoyDelivered | 241.590 / 525.0 s (46.02%) | completed=2/2 lost=0 | Passed :22987 | Passed :30362 | 0 | 0 | 0 | 1 :30433 | PASS :4 |
| 6 | S020 155923 en | :37328 | Victory ConvoyDelivered | 220.586 / 525.0 s (42.02%) | completed=2/2 lost=0 | Passed :31171 | Passed :37638 | 0 | 0 | 0 | 1 :37709 | PASS :5 |
| 7 | S020 104731 en | :44358 | Victory ConvoyDelivered | 227.528 / 525.0 s (43.34%) | completed=2/2 lost=0 | Passed :38480 | Passed :44649 | 0 | 0 | 0 | 1 :44720 | PASS :6 |
| 8 | S020 130365 fa-IR | :53132 | Victory ConvoyDelivered | 280.041 / 525.0 s (53.34%) | completed=2/2 lost=0 | Passed :45416 | Passed :53455 | 0 | 0 | 0 | 1 :53526 | PASS :7 |
| 9 | S021 130365 en | :58519 | Victory ConvoyDelivered | 193.016 / 525.0 s (36.76%) | completed=2/2 lost=0 | Passed :54224 | Passed :58809 | 0 | 0 | 0 | 1 :58880 | PASS :8 |
| 10 | S021 155923 en | :64108 | Victory ConvoyDelivered | 208.726 / 525.0 s (39.76%) | completed=2/2 lost=0 | Passed :59544 | Passed :64365 | 0 | 0 | 0 | 1 :64436 | PASS :9 |
| 11 | S021 104731 en | :69873 | Victory ConvoyDelivered | 207.501 / 525.0 s (39.52%) | completed=2/2 lost=0 | Passed :65098 | Passed :70181 | 0 | 0 | 0 | 1 :70252 | PASS :10 |
| 12 | S021 130365 fa-IR | :75657 | Victory ConvoyDelivered | 198.076 / 525.0 s (37.73%) | completed=2/2 lost=0 | Passed :70882 | Passed :75977 | 0 | 0 | 0 | 1 :76048 | PASS :11 |
| 13 | S022 130365 en | :82236 | Victory ConvoyDelivered | 271.401 / 525.0 s (51.70%) | completed=2/2 lost=0 | Passed :76686 | Passed :82544 | 0 | 0 | 0 | 1 :82615 | PASS :12 |
| 14 | S022 155923 en | :89364 | Victory ConvoyDelivered | 294.046 / 525.0 s (56.01%) | completed=2/2 lost=0 | Passed :83317 | Passed :89618 | 0 | 0 | 0 | 1 :89689 | PASS :13 |
| 15 | S022 104731 en | :96641 | Victory ConvoyDelivered | 296.973 / 525.0 s (56.57%) | completed=2/2 lost=0 | Passed :90327 | Passed :96967 | 0 | 0 | 0 | 1 :97038 | PASS :14 |
| 16 | S022 130365 fa-IR | :103839 | Victory ConvoyDelivered | 286.189 / 525.0 s (54.51%) | completed=2/2 lost=0 | Passed :97699 | Passed :104177 | 0 | 0 | 0 | 1 :104248 | PASS :15 |
| 17 | S023 130365 en | :109764 | Victory ConvoyDelivered | 201.808 / 525.0 s (38.44%) | completed=2/2 lost=0 | Passed :104909 | Passed :110072 | 0 | 0 | 0 | 1 :110143 | PASS :16 |
| 18 | S023 155923 en | :115470 | Victory ConvoyDelivered | 204.017 / 525.0 s (38.86%) | completed=2/2 lost=0 | Passed :110807 | Passed :115778 | 0 | 0 | 0 | 1 :115849 | PASS :17 |
| 19 | S023 104731 en | :121090 | Victory ConvoyDelivered | 205.337 / 525.0 s (39.11%) | completed=2/2 lost=0 | Passed :116510 | Passed :121398 | 0 | 0 | 0 | 1 :121469 | PASS :18 |
| 20 | S023 130365 fa-IR | :126280 | Victory ConvoyDelivered | 131.827 / 525.0 s (25.11%) | completed=2/2 lost=0 | Passed :122119 | Passed :126564 | 0 | 0 | 0 | 1 :126635 | PASS :19 |
| 21 | S024 130365 en | :132957 | Victory ConvoyDelivered | 290.643 / 525.0 s (55.36%) | completed=2/2 lost=0 | Passed :127319 | Passed :133265 | 0 | 0 | 0 | 1 :133336 | PASS :20 |
| 22 | S024 155923 en | :140161 | Victory ConvoyDelivered | 250.751 / 525.0 s (47.76%) | completed=2/2 lost=0 | Passed :134023 | Passed :140451 | 0 | 0 | 0 | 1 :140522 | PASS :21 |
| 23 | S024 104731 en | :147280 | Victory ConvoyDelivered | 241.169 / 525.0 s (45.94%) | completed=2/2 lost=0 | Passed :141239 | Passed :147570 | 0 | 0 | 0 | 1 :147641 | PASS :22 |
| 24 | S024 130365 fa-IR | :154639 | Victory ConvoyDelivered | 244.478 / 525.0 s (46.57%) | completed=2/2 lost=0 | Passed :148376 | Passed :154961 | 0 | 0 | 0 | 1 :155032 | PASS :23 |
| 25 | S009 130365 en | :169669 | Victory TicketsDepleted | 922.043 / 1200.0 s (76.84%) | ownZones=2 opposingZones=1 tickets=272/0 | Passed :155770 | Passed :169923 | 0 | 0 | 0 | 1 :169994 | PASS :24 |
| 26 | S009 155923 en | :184224 | Defeat TicketsDepleted | 993.415 / 1200.0 s (82.78%) | ownZones=1 opposingZones=2 tickets=0/197 | Passed :170752 | Passed :184516 | 0 | 0 | 0 | 0 :184587 | PASS :25 |
| 27 | S009 104731 en | :197880 | Victory TicketsDepleted | 888.991 / 1200.0 s (74.08%) | ownZones=2 opposingZones=1 tickets=273/0 | Passed :185324 | Passed :198134 | 0 | 0 | 0 | 1 :198205 | PASS :26 |
| 28 | S009 130365 fa-IR | :210813 | Defeat TicketsDepleted | 786.693 / 1200.0 s (65.56%) | ownZones=1 opposingZones=2 tickets=0/345 | Passed :198940 | Passed :211079 | 0 | 0 | 0 | 0 :211150 | PASS :27 |
| 29 | S031 130365 en | :221089 | Victory TicketsDepleted | 674.013 / 1200.0 s (56.17%) | ownZones=2 opposingZones=1 tickets=442/0 | Passed :211927 | Passed :221343 | 0 | 0 | 0 | 1 :221414 | PASS :28 |
| 30 | S031 155923 en | :230634 | Victory TicketsDepleted | 655.783 / 1200.0 s (54.65%) | ownZones=2 opposingZones=1 tickets=444/0 | Passed :222149 | Passed :230888 | 0 | 0 | 0 | 1 :230959 | PASS :29 |
| 31 | S031 104731 en | :242045 | Victory TicketsDepleted | 651.123 / 1200.0 s (54.26%) | ownZones=2 opposingZones=1 tickets=445/0 | Passed :231694 | Passed :242319 | 0 | 0 | 0 | 1 :242390 | PASS :30 |
| 32 | S031 130365 fa-IR | :253155 | Victory TicketsDepleted | 674.714 / 1200.0 s (56.23%) | ownZones=2 opposingZones=1 tickets=442/0 | Passed :243086 | Passed :253441 | 0 | 0 | 0 | 1 :253512 | PASS :31 |
| 33 | S033 130365 en | :263192 | Victory TicketsDepleted | 653.966 / 1200.0 s (54.50%) | ownZones=2 opposingZones=1 tickets=441/0 | Passed :254247 | Passed :263446 | 0 | 0 | 0 | 1 :263517 | PASS :32 |
| 34 | S033 155923 en | :274878 | Victory TicketsDepleted | 684.780 / 1200.0 s (57.07%) | ownZones=2 opposingZones=1 tickets=411/0 | Passed :264256 | Passed :275168 | 0 | 0 | 0 | 1 :275239 | PASS :33 |
| 35 | S033 104731 en | :287329 | Victory TicketsDepleted | 663.676 / 1200.0 s (55.31%) | ownZones=2 opposingZones=1 tickets=415/0 | Passed :275974 | Passed :287639 | 0 | 0 | 0 | 1 :287710 | PASS :34 |
| 36 | S033 130365 fa-IR | :297034 | Victory TicketsDepleted | 579.017 / 1200.0 s (48.25%) | ownZones=2 opposingZones=1 tickets=500/0 | Passed :288429 | Passed :297354 | 0 | 0 | 0 | 1 :297425 | PASS :35 |
