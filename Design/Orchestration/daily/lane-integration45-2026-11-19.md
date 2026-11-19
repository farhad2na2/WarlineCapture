# Lane worker 45, 2026-11-19: CC CE S043-S048 (450 s) and CC BT S038/S039 (540 s) counted runs after W32 + W31c

HEAD 7de2a3c79 at start (W32+W27 1+3 and W31c merged), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane45/`.
No asset or code changes. disk_guard 0 before each launch; one Editor of mine at a time (main checkout); no Editor stopped.
The program was paused on owner request during the run: the queue finished its last jobs and no further launch or re-run was made.
The stale working-tree files were left alone; this commit stages explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 289 s (recompile) | compile1.log:1912; Library Redirect Path :93 |
| 1 python | 159 run, 2 FAILED: the 2 known test_quickcheck failures from lane 43, no others | python.txt:33,57,74 |
| 2 queue A (32 jobs, one launch) | 27/32 wins; Completed jobs=32 recorded=32 wins=27 sourceUnchanged=1 exitCode=0 | a/unity.log:721936 |
| 4 gate 3 (objective rule) | 24/32 PASS, 8/32 FAIL (S046, S048 transport_heli unused) | gate3.txt:1-32 |

Flags: `--menu-journey --validation-library --no-table --timeout 30000`; validation library enabled=1 (a/unity.log:627). Queue ran 18:03-21:51.
runs.csv folder `_Evidence/candidates/d45f98527e22f861b46716b441d21f59abd229fd8241f07eff309d88daabc2bf/` (4 rows each, S038, S039, S043-S048).

## Pacing

- Deadlines honoured: 23 CE lines deadline=450.0 plus 1 deadline=450.3 (the S046 fa-IR timeout), 8 BT lines deadline=540.0, 0 deadline=1080.
- CC CE S043-S048: 20/24 Victory ConvoyDelivered 2/2, 33.03-96.82% of 450 s. Over 50%: S045 155923 (77.82%) and fa-IR (96.82%), S046 three wins (51.30-58.00%), S047 155923 (71.37%) and fa-IR (60.52%), S048 three wins (50.89-53.75%).
- CC BT S038/S039: 7/8 Victory BreakthroughEvacuated 8/8 lost=0, 46.83-64.31% of 540 s; S038 fa-IR 50.75%, S039 64.31/55.62/52.08% over 50%.

## Losses and aborts (first failing line)

- S045 104731 en: Defeat ConvoyImpossible 201.2 s, completed=0/2 lost=2 (:334020); first convoy loss :327860 t=145.45.
- S046 130365 fa-IR: Defeat ObjectiveDeadline 450.3 s, completed=1/2 lost=0 (:504203); 1/2 delivered by :501116 t=83.98, second convoy never arrived.
- S047 104731 en: Defeat ConvoyImpossible 258.9 s, completed=0/2 lost=2 (:531906); first loss :531824 t=252.30 (expected possible, kept as evidence).
- S048 104731 en: Defeat ConvoyImpossible 293.5 s, completed=1/2 lost=2 (:600862); first loss :597171 t=239.23.
- S038 130365 en: Abort ariaStopped:Blocked 225.2 s, completed=6/8 (:630776); first failing line :630729 `[AriaBlocked] reason=115 kind=Control intent=EvacuateOriginals`, `[AriaBlockedControl] control=Squad0` :630745; no return probe.

## Per job (a/unity.log)

| # | Row seed locale | Terminal | Result | Clock / deadline (%) | Objective | Menu | Return probe | AriaBlocked | countedWin | Gate 3 (gate3.txt line) |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | S043 130365 en | :10187 | Victory ConvoyDelivered | 190.449 / 450.0 s (42.32%) | completed=2/2 lost=0 | Passed :1997 | Passed :10441 | 0 | 1 :10512 | PASS :1 |
| 2 | S043 155923 en | :24234 | Victory ConvoyDelivered | 176.802 / 450.0 s (39.29%) | completed=2/2 lost=1 | Passed :11226 | Passed :24488 | 0 | 1 :24559 | PASS :2 |
| 3 | S043 104731 en | :33975 | Victory ConvoyDelivered | 223.719 / 450.0 s (49.72%) | completed=2/2 lost=0 | Passed :25294 | Passed :34229 | 0 | 1 :34300 | PASS :3 |
| 4 | S043 130365 fa-IR | :45554 | Victory ConvoyDelivered | 149.844 / 450.0 s (33.30%) | completed=2/2 lost=0 | Passed :35007 | Passed :45820 | 0 | 1 :45891 | PASS :4 |
| 5 | S044 130365 en | :119300 | Victory ConvoyDelivered | 211.614 / 450.0 s (47.03%) | completed=2/2 lost=0 | Passed :46575 | Passed :119554 | 0 | 1 :119625 | PASS :5 |
| 6 | S044 155923 en | :199073 | Victory ConvoyDelivered | 205.896 / 450.0 s (45.75%) | completed=2/2 lost=0 | Passed :120360 | Passed :199399 | 0 | 1 :199470 | PASS :6 |
| 7 | S044 104731 en | :267003 | Victory ConvoyDelivered | 204.454 / 450.0 s (45.43%) | completed=2/2 lost=0 | Passed :200205 | Passed :267257 | 0 | 1 :267328 | PASS :7 |
| 8 | S044 130365 fa-IR | :302914 | Victory ConvoyDelivered | 210.235 / 450.0 s (46.72%) | completed=2/2 lost=0 | Passed :268064 | Passed :303198 | 0 | 1 :303269 | PASS :8 |
| 9 | S045 130365 en | :310910 | Victory ConvoyDelivered | 166.156 / 450.0 s (36.92%) | completed=2/2 lost=0 | Passed :303997 | Passed :311164 | 0 | 1 :311235 | PASS :9 |
| 10 | S045 155923 en | :322505 | Victory ConvoyDelivered | 350.185 / 450.0 s (77.82%) | completed=2/2 lost=1 | Passed :311952 | Passed :322759 | 0 | 1 :322830 | PASS :10 |
| 11 | S045 104731 en | :334020 | Defeat ConvoyImpossible | 201.233 / 450.0 s (44.72%) | completed=0/2 lost=2 | Passed :323462 | Passed :334294 | 0 | 0 :334365 | PASS :11 |
| 12 | S045 130365 fa-IR | :342391 | Victory ConvoyDelivered | 435.689 / 450.0 s (96.82%) | completed=2/2 lost=1 | Passed :334971 | Passed :342658 | 0 | 1 :342729 | PASS :12 |
| 13 | S046 130365 en | :367773 | Victory ConvoyDelivered | 261.013 / 450.0 s (58.00%) | completed=2/2 lost=0 | Passed :343335 | Passed :368047 | 0 | 1 :368118 | FAIL :13 unused transport_heli |
| 14 | S046 155923 en | :440062 | Victory ConvoyDelivered | 230.845 / 450.0 s (51.30%) | completed=2/2 lost=0 | Passed :368724 | Passed :440408 | 0 | 1 :440479 | FAIL :14 unused transport_heli |
| 15 | S046 104731 en | :495963 | Victory ConvoyDelivered | 242.737 / 450.0 s (53.94%) | completed=2/2 lost=0 | Passed :441163 | Passed :496235 | 0 | 1 :496306 | FAIL :15 unused transport_heli |
| 16 | S046 130365 fa-IR | :504203 | Defeat ObjectiveDeadline | 450.261 / 450.3 s (100.00%) | completed=1/2 lost=0 | Passed :496990 | Passed :504546 | 0 | 0 :504617 | FAIL :16 unused transport_heli |
| 17 | S047 130365 en | :517815 | Victory ConvoyDelivered | 148.638 / 450.0 s (33.03%) | completed=2/2 lost=0 | Passed :505313 | Passed :518087 | 0 | 1 :518158 | PASS :17 |
| 18 | S047 155923 en | :525504 | Victory ConvoyDelivered | 321.156 / 450.0 s (71.37%) | completed=2/2 lost=0 | Passed :518854 | Passed :525776 | 0 | 1 :525847 | PASS :18 |
| 19 | S047 104731 en | :531906 | Defeat ConvoyImpossible | 258.906 / 450.0 s (57.53%) | completed=0/2 lost=2 | Passed :526543 | Passed :532178 | 0 | 0 :532249 | PASS :19 |
| 20 | S047 130365 fa-IR | :537955 | Victory ConvoyDelivered | 272.333 / 450.0 s (60.52%) | completed=2/2 lost=0 | Passed :532910 | Passed :538275 | 0 | 1 :538346 | PASS :20 |
| 21 | S048 130365 en | :545473 | Victory ConvoyDelivered | 229.000 / 450.0 s (50.89%) | completed=2/2 lost=0 | Passed :538915 | Passed :545727 | 0 | 1 :545798 | FAIL :21 unused transport_heli |
| 22 | S048 155923 en | :575281 | Victory ConvoyDelivered | 241.866 / 450.0 s (53.75%) | completed=2/2 lost=0 | Passed :546480 | Passed :575573 | 0 | 1 :575644 | FAIL :22 unused transport_heli |
| 23 | S048 104731 en | :600862 | Defeat ConvoyImpossible | 293.467 / 450.0 s (65.21%) | completed=1/2 lost=2 | Passed :576250 | Passed :601116 | 0 | 0 :601187 | FAIL :23 unused transport_heli |
| 24 | S048 130365 fa-IR | :620876 | Victory ConvoyDelivered | 240.497 / 450.0 s (53.44%) | completed=2/2 lost=0 | Passed :601817 | Passed :621237 | 0 | 1 :621308 | FAIL :24 unused transport_heli |
| 25 | S038 130365 en | :630776 | Abort ariaStopped:Blocked (first fail :630729 AriaBlocked 115) | 225.201 / 540.0 s (41.70%) | completed=6/8 lost=0 | Passed :621902 | - | 2 | 0 :630847 | PASS :25 |
| 26 | S038 155923 en | :637655 | Victory BreakthroughEvacuated | 256.781 / 540.0 s (47.55%) | completed=8/8 lost=0 | Passed :631497 | Passed :637909 | 0 | 1 :637980 | PASS :26 |
| 27 | S038 104731 en | :655674 | Victory BreakthroughEvacuated | 262.374 / 540.0 s (48.59%) | completed=8/8 lost=0 | Passed :638589 | Passed :656000 | 0 | 1 :656071 | PASS :27 |
| 28 | S038 130365 fa-IR | :663402 | Victory BreakthroughEvacuated | 274.045 / 540.0 s (50.75%) | completed=8/8 lost=0 | Passed :656721 | Passed :663668 | 0 | 1 :663739 | PASS :28 |
| 29 | S039 130365 en | :678025 | Victory BreakthroughEvacuated | 347.266 / 540.0 s (64.31%) | completed=8/8 lost=0 | Passed :664370 | Passed :678279 | 0 | 1 :678350 | PASS :29 |
| 30 | S039 155923 en | :700415 | Victory BreakthroughEvacuated | 300.330 / 540.0 s (55.62%) | completed=8/8 lost=0 | Passed :679026 | Passed :700669 | 0 | 1 :700740 | PASS :30 |
| 31 | S039 104731 en | :712429 | Victory BreakthroughEvacuated | 252.876 / 540.0 s (46.83%) | completed=8/8 lost=0 | Passed :701420 | Passed :712683 | 0 | 1 :712754 | PASS :31 |
| 32 | S039 130365 fa-IR | :720959 | Victory BreakthroughEvacuated | 281.228 / 540.0 s (52.08%) | completed=8/8 lost=0 | Passed :713390 | Passed :721245 | 0 | 1 :721316 | PASS :32 |
