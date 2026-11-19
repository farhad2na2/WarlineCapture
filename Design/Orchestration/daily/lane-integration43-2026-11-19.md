# Lane worker 43, 2026-11-19: DB CE S019-S024 and CC BT S037-S042 counted re-runs on the W31b deadlines

HEAD 391d02195 at start (W31+W31b merged), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane43/`.
No asset or code changes. disk_guard 0 before each launch; one Editor at a time (main checkout); Fig/Fig-SquadPlay untouched; no Editor stopped.
The stale working-tree files from eaa64700c were left alone; this commit stages explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 225 s (recompile) | compile1.log:1770; Library Redirect Path :93 |
| 1 python | 159 run, 2 FAILED (test_quickcheck), reproduced with no Editor running | python.txt:33,57,74; python2.txt:74 |
| 2 queue A (48 jobs, one launch) | 48/48 Victory; Completed jobs=48 recorded=48 wins=48 sourceUnchanged=1 exitCode=0 | a/unity.log:480765 |
| 4 gate 3 (objective rule) | 13/48 PASS, 35/48 FAIL | gate3.txt:1-48 |

Flags: `--menu-journey --validation-library --no-table --timeout 30000`; validation library enabled=1 (a/unity.log:604); Library Redirect Path :92. No "First-visit ARIA seed" rejection, so no re-runs.
Queue ran 14:21-17:47. runs.csv folder `_Evidence/candidates/f92e2ede624a2b91abbd155c464b6c560955a5aba2d631156d59bf3572a1226a/` (4 rows each, S019-S024, S037-S042).

## Pacing (the W31b check)

- Every job logged the new deadline: 24 CE jobs deadline=420.0, 24 BT jobs deadline=540.0; 0 lines with deadline=1080.
- DB CE S019-S024: ConvoyDelivered 2/2 lost=0 24/24, 124.6-210.1 s = 29.66-50.01% of 420 s (mean 39.49%). S020 104731 en is 50.01% (:45330), just over the 50% ceiling; all others are inside 25-50%.
- CC BT S037-S042: BreakthroughEvacuated 8/8 lost=0 exitOpen=1 24/24, 163.7-242.5 s = 30.32-44.90% of 540 s (mean 37.69%), all inside 25-50%.

## Per job (a/unity.log)

| # | Row seed locale | Result | Clock / deadline (%) | Objective | Menu | Return probe | AriaBlocked | countedWin | Gate 3 (gate3.txt line) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S019 130365 en | Victory ConvoyDelivered :5626 | 157.561 / 420.0 s (37.51%) | completed=2/2 lost=0 | Passed :1502 | Passed :5970 | 0 | 1 :6041 | FAIL :1 unused apc_armored;car |
| 2 | S019 155923 en | Victory ConvoyDelivered :12399 | 207.140 / 420.0 s (49.32%) | completed=2/2 lost=0 | Passed :6793 | Passed :12653 | 0 | 1 :12724 | PASS :2 |
| 3 | S019 104731 en | Victory ConvoyDelivered :18124 | 165.873 / 420.0 s (39.49%) | completed=2/2 lost=0 | Passed :13405 | Passed :18434 | 0 | 1 :18505 | FAIL :3 unused apc_armored;car |
| 4 | S019 130365 fa-IR | Victory ConvoyDelivered :23466 | 151.732 / 420.0 s (36.13%) | completed=2/2 lost=0 | Passed :19172 | Passed :23752 | 0 | 1 :23823 | FAIL :4 unused apc_armored;car;rifle |
| 5 | S020 130365 en | Victory ConvoyDelivered :30603 | 207.374 / 420.0 s (49.37%) | completed=2/2 lost=0 | Passed :24429 | Passed :30931 | 0 | 1 :31002 | FAIL :5 unused apc_armored;car;gunner;rifle;tank |
| 6 | S020 155923 en | Victory ConvoyDelivered :37689 | 163.775 / 420.0 s (38.99%) | completed=2/2 lost=0 | Passed :31688 | Passed :37997 | 0 | 1 :38068 | FAIL :6 unused apc_armored;car;gunner;rifle;tank |
| 7 | S020 104731 en | Victory ConvoyDelivered :45330 | 210.062 / 420.0 s (50.01%) | completed=2/2 lost=0 | Passed :38700 | Passed :45692 | 0 | 1 :45763 | FAIL :7 unused apc_armored;car;gunner;rifle;tank |
| 8 | S020 130365 fa-IR | Victory ConvoyDelivered :52363 | 201.376 / 420.0 s (47.95%) | completed=2/2 lost=0 | Passed :46392 | Passed :52683 | 0 | 1 :52754 | FAIL :8 unused apc_armored;car;gunner;rifle;tank |
| 9 | S021 130365 en | Victory ConvoyDelivered :58217 | 154.745 / 420.0 s (36.84%) | completed=2/2 lost=0 | Passed :53323 | Passed :58491 | 0 | 1 :58562 | PASS :9 |
| 10 | S021 155923 en | Victory ConvoyDelivered :63457 | 137.964 / 420.0 s (32.85%) | completed=2/2 lost=0 | Passed :59258 | Passed :63731 | 0 | 1 :63802 | FAIL :10 unused apc_armored;car;rifle |
| 11 | S021 104731 en | Victory ConvoyDelivered :68998 | 137.200 / 420.0 s (32.67%) | completed=2/2 lost=0 | Passed :64434 | Passed :69270 | 0 | 1 :69341 | FAIL :11 unused apc_armored;car;rifle |
| 12 | S021 130365 fa-IR | Victory ConvoyDelivered :74373 | 124.571 / 420.0 s (29.66%) | completed=2/2 lost=0 | Passed :70097 | Passed :74639 | 0 | 1 :74710 | FAIL :12 unused apc_armored;car;rifle |
| 13 | S022 130365 en | Victory ConvoyDelivered :80987 | 160.411 / 420.0 s (38.19%) | completed=2/2 lost=0 | Passed :75445 | Passed :81277 | 0 | 1 :81348 | FAIL :13 unused aa;apc_armored;car;gunner;rifle;transport_heli |
| 14 | S022 155923 en | Victory ConvoyDelivered :87604 | 175.006 / 420.0 s (41.67%) | completed=2/2 lost=0 | Passed :82044 | Passed :87913 | 0 | 1 :87984 | FAIL :14 unused aa;apc_armored;car;gunner;rifle;transport_heli |
| 15 | S022 104731 en | Victory ConvoyDelivered :95201 | 174.013 / 420.0 s (41.43%) | completed=2/2 lost=0 | Passed :88703 | Passed :95509 | 0 | 1 :95580 | FAIL :15 unused aa;apc_armored;car;gunner;rifle;rocketeer;transport_heli |
| 16 | S022 130365 fa-IR | Victory ConvoyDelivered :102100 | 170.638 / 420.0 s (40.63%) | completed=2/2 lost=0 | Passed :96209 | Passed :102420 | 0 | 1 :102491 | FAIL :16 unused aa;apc_armored;car;gunner;rifle;transport_heli |
| 17 | S023 130365 en | Victory ConvoyDelivered :107710 | 169.950 / 420.0 s (40.46%) | completed=2/2 lost=0 | Passed :103120 | Passed :108036 | 0 | 1 :108107 | FAIL :17 unused apc_armored;car |
| 18 | S023 155923 en | Victory ConvoyDelivered :113228 | 141.314 / 420.0 s (33.65%) | completed=2/2 lost=0 | Passed :108794 | Passed :113520 | 0 | 1 :113591 | FAIL :18 unused apc_armored;car;rifle |
| 19 | S023 104731 en | Victory ConvoyDelivered :118736 | 138.553 / 420.0 s (32.99%) | completed=2/2 lost=0 | Passed :114255 | Passed :119010 | 0 | 1 :119081 | FAIL :19 unused apc_armored;car;rifle |
| 20 | S023 130365 fa-IR | Victory ConvoyDelivered :124115 | 163.285 / 420.0 s (38.88%) | completed=2/2 lost=0 | Passed :119786 | Passed :124381 | 0 | 1 :124452 | FAIL :20 unused apc_armored;car |
| 21 | S024 130365 en | Victory ConvoyDelivered :130446 | 193.626 / 420.0 s (46.10%) | completed=2/2 lost=0 | Passed :125125 | Passed :130792 | 0 | 1 :130863 | FAIL :21 unused aa;apc_armored;gunner;rifle;tank;transport_heli |
| 22 | S024 155923 en | Victory ConvoyDelivered :137696 | 164.631 / 420.0 s (39.20%) | completed=2/2 lost=0 | Passed :131580 | Passed :138004 | 0 | 1 :138075 | FAIL :22 unused aa;apc_armored;gunner;rifle;tank;transport_heli |
| 23 | S024 104731 en | Victory ConvoyDelivered :144247 | 171.430 / 420.0 s (40.82%) | completed=2/2 lost=0 | Passed :138777 | Passed :144575 | 0 | 1 :144646 | FAIL :23 unused aa;apc_armored;gunner;rifle;tank;transport_heli |
| 24 | S024 130365 fa-IR | Victory ConvoyDelivered :150128 | 137.960 / 420.0 s (32.85%) | completed=2/2 lost=0 | Passed :145330 | Passed :150430 | 0 | 1 :150501 | FAIL :24 unused aa;apc_armored;gunner;rifle;tank;transport_heli |
| 25 | S037 130365 en | Victory BreakthroughEvacuated :169061 | 239.974 / 540.0 s (44.44%) | completed=8/8 lost=0 | Passed :151245 | Passed :169371 | 0 | 1 :169442 | PASS :25 |
| 26 | S037 155923 en | Victory BreakthroughEvacuated :176007 | 218.053 / 540.0 s (40.38%) | completed=8/8 lost=0 | Passed :170048 | Passed :176261 | 0 | 1 :176332 | PASS :26 |
| 27 | S037 104731 en | Victory BreakthroughEvacuated :182486 | 240.499 / 540.0 s (44.54%) | completed=8/8 lost=0 | Passed :177028 | Passed :182740 | 0 | 1 :182811 | PASS :27 |
| 28 | S037 130365 fa-IR | Victory BreakthroughEvacuated :203873 | 242.482 / 540.0 s (44.90%) | completed=8/8 lost=0 | Passed :183429 | Passed :204175 | 0 | 1 :204246 | PASS :28 |
| 29 | S038 130365 en | Victory BreakthroughEvacuated :211512 | 164.223 / 540.0 s (30.41%) | completed=8/8 lost=0 | Passed :204948 | Passed :211820 | 0 | 1 :211891 | FAIL :29 unused apc_armored;car;tank |
| 30 | S038 155923 en | Victory BreakthroughEvacuated :218872 | 169.678 / 540.0 s (31.42%) | completed=8/8 lost=0 | Passed :212610 | Passed :219126 | 0 | 1 :219197 | FAIL :30 unused apc_armored;car;tank |
| 31 | S038 104731 en | Victory BreakthroughEvacuated :226345 | 182.900 / 540.0 s (33.87%) | completed=8/8 lost=0 | Passed :219899 | Passed :226599 | 0 | 1 :226670 | FAIL :31 unused apc_armored;car;tank |
| 32 | S038 130365 fa-IR | Victory BreakthroughEvacuated :247960 | 187.240 / 540.0 s (34.67%) | completed=8/8 lost=0 | Passed :227354 | Passed :248226 | 0 | 1 :248297 | FAIL :32 unused apc_armored;car;tank |
| 33 | S039 130365 en | Victory BreakthroughEvacuated :253941 | 217.848 / 540.0 s (40.34%) | completed=8/8 lost=0 | Passed :248981 | Passed :254231 | 0 | 1 :254302 | FAIL :33 unused car |
| 34 | S039 155923 en | Victory BreakthroughEvacuated :261059 | 216.977 / 540.0 s (40.18%) | completed=8/8 lost=0 | Passed :254963 | Passed :261555 | 0 | 1 :261626 | PASS :34 |
| 35 | S039 104731 en | Victory BreakthroughEvacuated :271726 | 226.724 / 540.0 s (41.99%) | completed=8/8 lost=0 | Passed :262341 | Passed :272003 | 0 | 1 :272074 | PASS :35 |
| 36 | S039 130365 fa-IR | Victory BreakthroughEvacuated :283157 | 235.954 / 540.0 s (43.70%) | completed=8/8 lost=0 | Passed :272682 | Passed :283443 | 0 | 1 :283514 | PASS :36 |
| 37 | S040 130365 en | Victory BreakthroughEvacuated :293766 | 175.454 / 540.0 s (32.49%) | completed=8/8 lost=0 | Passed :284210 | Passed :294020 | 0 | 1 :294091 | FAIL :37 unused aa;apc_armored;car;transport_heli |
| 38 | S040 155923 en | Victory BreakthroughEvacuated :311490 | 234.671 / 540.0 s (43.46%) | completed=8/8 lost=0 | Passed :294810 | Passed :311744 | 0 | 1 :311815 | FAIL :38 unused aa;apc_armored;car;transport_heli |
| 39 | S040 104731 en | Victory BreakthroughEvacuated :340313 | 171.073 / 540.0 s (31.68%) | completed=8/8 lost=0 | Passed :312453 | Passed :340567 | 0 | 1 :340638 | FAIL :39 unused apc_armored;car;rocketeer;transport_heli |
| 40 | S040 130365 fa-IR | Victory BreakthroughEvacuated :356201 | 169.347 / 540.0 s (31.36%) | completed=8/8 lost=0 | Passed :341358 | Passed :356467 | 0 | 1 :356538 | FAIL :40 unused aa;apc_armored;car;rocketeer;transport_heli |
| 41 | S041 130365 en | Victory BreakthroughEvacuated :371521 | 217.731 / 540.0 s (40.32%) | completed=8/8 lost=0 | Passed :357176 | Passed :371775 | 0 | 1 :371846 | PASS :41 |
| 42 | S041 155923 en | Victory BreakthroughEvacuated :378098 | 222.394 / 540.0 s (41.18%) | completed=8/8 lost=0 | Passed :372565 | Passed :378352 | 0 | 1 :378423 | PASS :42 |
| 43 | S041 104731 en | Victory BreakthroughEvacuated :393813 | 215.281 / 540.0 s (39.87%) | completed=8/8 lost=0 | Passed :379140 | Passed :394067 | 0 | 1 :394138 | PASS :43 |
| 44 | S041 130365 fa-IR | Victory BreakthroughEvacuated :405153 | 214.997 / 540.0 s (39.81%) | completed=8/8 lost=0 | Passed :394796 | Passed :405419 | 0 | 1 :405490 | PASS :44 |
| 45 | S042 130365 en | Victory BreakthroughEvacuated :429801 | 199.917 / 540.0 s (37.02%) | completed=8/8 lost=0 | Passed :406174 | Passed :430091 | 0 | 1 :430162 | FAIL :45 unused apc_armored;rocketeer;tank;transport_heli |
| 46 | S042 155923 en | Victory BreakthroughEvacuated :437601 | 163.744 / 540.0 s (30.32%) | completed=8/8 lost=0 | Passed :430884 | Passed :437891 | 0 | 1 :437962 | FAIL :46 unused apc_armored;tank;transport_heli |
| 47 | S042 104731 en | Victory BreakthroughEvacuated :463778 | 180.837 / 540.0 s (33.49%) | completed=8/8 lost=0 | Passed :438600 | Passed :464086 | 0 | 1 :464157 | FAIL :47 unused apc_armored;rocketeer;tank;transport_heli |
| 48 | S042 130365 fa-IR | Victory BreakthroughEvacuated :479537 | 176.567 / 540.0 s (32.70%) | completed=8/8 lost=0 | Passed :464763 | Passed :479857 | 0 | 1 :479928 | FAIL :48 unused aa;apc_armored;tank;transport_heli |

## Findings

- Gate 3 per mission (passing jobs out of 4): S019 1, S020 0, S021 1, S022 0, S023 0, S024 0, S037 4, S038 0, S039 3, S040 0, S041 4, S042 0. roster=match on all 48. Common unused starting roles: apc_armored and car (DB CE); aa/transport_heli on S022/S024/S040/S042; tank on S038/S042.
- Python: test_actual_failure_reasons_survive_the_report gets an extra `S019.Standard.deadline` failure (python.txt:38), and test_same_binary_for_changed_orders_and_several_seeded_missions exits 1 with "10/12 objective probes" on S019/S020 (:64). This is probably quickcheck's abstract deadline probe after the W31 420 s change. Not fixed (no code changes).
- Whole log: 0 [AriaBlocked]/[AriaBlockedControl], 0 order_blocked, 4 order_retry failures=1 (all in won jobs: S019 155923 :9041, S020 130365 :27484, S020 104731 :41610, S020 fa-IR :48848), 0 [StartingGrantPlacement] failed, 0 [SkirmishStartingForce] failed, 0 startupFailure, 0 BlockedSpawn, 0 physicalTakeover, 0 FailureCode, 0 MissingReference, 0 error CS; [CanonicalAircraftGeometry] result=Failed stale stamp 48x (Codex 040, did not block play).
- MenuJourney Passed 48/48 (S019 :1502 via TheaterPin_DB>ObjectiveTab_CE, S037 :151245 via TheaterPin_CC>ObjectiveTab_BT, the rest ObjectiveTab>BattleCard). Return probe Passed MAIN MENU interventions=0 48/48. appended=1 countedWin=1 48/48.
