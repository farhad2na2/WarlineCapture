# Lane worker 41, 2026-11-19: counted runs for Desert Base BT/CE and the FC field winners

HEAD ba493b0a3 (eecbac49d + program-lead gate-3 note), branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane41/`.
No asset or code changes. disk_guard 0 before every launch; one Editor at a time (main checkout); Fig/Fig-SquadPlay Editors untouched; no Editor stopped.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 46 s; python 153 OK | compile1.log:863; Library Redirect Path :93; python.txt:33-35 |
| 2 queue A, Desert Base BT/CE (32 jobs, one launch) | 30/32 Victory, 2 Abort (BlockedSpawn); Completed wins=30 sourceUnchanged=1 exitCode=0 | a/unity.log:481192 |
| 3 queue B, FC field winners (14 jobs, one launch) | 12/14 Victory, 2 Abort (ariaStopped:Blocked); Completed wins=12 sourceUnchanged=1 exitCode=0 | b/unity.log:132371 |

Both queues: `--menu-journey --validation-library --no-table --timeout 30000`, validation library enabled=1 (a :601, b :600). No "First-visit ARIA seed" rejection, so no re-runs.
Queue A ran 04:28-06:42, queue B 06:44-09:17.

## Findings

- S013 and S019 (first Mac runs) won: S013 BreakthroughEvacuated 8/8 at 35.12%, S019 ConvoyDelivered 2/2 at 11.47%.
- BT S014-S018 won 15/15 (8/8 evacuated, lost=0, 24-35% of the deadline). CE S020/S022/S024 won 9/9 (2/2 delivered, 13.6-14.3%).
- CE field rows S021 and S023 aborted at seed 155923 en before play (24 s): first failing line `[SkirmishStartingForce] failed prefab=Unit_Veh_Truck_Tray faction=1 member=0`
  (a :416162 and :448590), then `aborted=True reason=expandedStartupFailure=BlockedSpawn` (:416187, :448614). Their 104731 en and 130365 fa-IR jobs won.
  The established CE rows (S020/S022/S024) spawned fine at 155923.
- Desert Base FC S009/S011 won 8/8 (2/1 zones, 49-54% of the deadline, S011 155923 at 89.71% with 62 tickets left after zones swung several times).
  Their first own zone came at 118.8-122.9 s on en seeds (65/77 s on fa-IR); Mountain Pass S057/S059 reach it at 30-31 s.
- Mountain Pass FC S057/S059 won 4/6. Both 104731 en jobs aborted on `[AriaBlocked] reason=115 kind=Control intent=ControlZone attempts=3`, Move, selectedMask=8
  (S057 b :99705/:99721 at t=263.6, S059 b :123880/:123896 at t=421.3), reason `ariaStopped:Blocked`.
  - S057 104731: zones 1/0@30.0, 2/0@44.1, 1/1@126.6 to the end (500/456 tickets); Hold choices t=38.7, 146.7, 183.7 (2 taps).
  - S059 104731: zones 1/0@30.2, 2/0@44.4, 2/1@126.3, 1/1@201.2, 1/2@209.2 to the end (298/344); one Hold choice t=38.8, then Move and 7 ObserveObjective choices.
- Menu journey Passed 46/46; terminal probe Passed MAIN MENU interventions=0 on all 42 played-out jobs; countedWin=1 on all 42 wins.
- Gate-3 unused is not empty on any job (7-20 entries; building.airport unused wherever offered).
- Whole logs: no [StartingGrantPlacement] failed, FailureCode, MissingReference or error CS. [AriaBlocked] only in the 2 queue B aborts; BlockedSpawn only in the 2 queue A aborts.
  `[CanonicalAircraftGeometry] result=Failed` (stale stamps, Codex 040) logs once per job at EditorPlayEntry, playModeBlocked=0.
- Hold choices/taps per FC job: S009 9/2, 8/4, 11/6, 11/5; S011 6/1, 16/7, 12/3, 8/4; S057 5/3, 3/2 (abort), 7/3; S059 5/2, 1/1 (abort), 6/2 (extract-b.txt).

runs.csv rows appended under `<ID>/_Evidence/candidates/80228993546b34189d35190f9a73a9a8b1a895022144abeceecdf152a57824b0/runs.csv` for S009, S011, S013-S024, S057, S059
(committed; trace folders left untracked). Whether they count is the lead's call.

## Per job

## Queue A (a/unity.log)

| Job | Row / seed / locale | Terminal | Result | Clock (deadline %) | Objective | Gate-3 unused | Menu | Probe | countedWin |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S013 130365 en | :8782 | Victory BreakthroughEvacuated | 379.318 s (35.12) | completed=8/8 lost=0 | 11 :8768 | Passed :2350 | Passed :9113 | 1 :9184 |
| 2 | S019 130365 en | :14184 | Victory ConvoyDelivered | 123.826 s (11.47) | completed=2/2 lost=0 | 13 :14170 | Passed :10030 | Passed :14440 | 1 :14511 |
| 3 | S014 155923 en | :44494 | Victory BreakthroughEvacuated | 308.485 s (28.56) | completed=8/8 lost=0 | 7 :44480 | Passed :15251 | Passed :44786 | 1 :44857 |
| 4 | S014 104731 en | :91899 | Victory BreakthroughEvacuated | 307.252 s (28.45) | completed=8/8 lost=0 | 7 :91885 | Passed :45594 | Passed :92211 | 1 :92282 |
| 5 | S014 130365 fa-IR | :100533 | Victory BreakthroughEvacuated | 302.158 s (27.98) | completed=8/8 lost=0 | 8 :100519 | Passed :93021 | Passed :100838 | 1 :100909 |
| 6 | S015 155923 en | :106779 | Victory BreakthroughEvacuated | 274.489 s (25.42) | completed=8/8 lost=0 | 17 :106765 | Passed :101646 | Passed :107053 | 1 :107124 |
| 7 | S015 104731 en | :113940 | Victory BreakthroughEvacuated | 274.821 s (25.45) | completed=8/8 lost=0 | 18 :113926 | Passed :107862 | Passed :114214 | 1 :114285 |
| 8 | S015 130365 fa-IR | :120456 | Victory BreakthroughEvacuated | 261.593 s (24.22) | completed=8/8 lost=0 | 17 :120442 | Passed :115042 | Passed :120725 | 1 :120796 |
| 9 | S016 155923 en | :146098 | Victory BreakthroughEvacuated | 311.125 s (28.81) | completed=8/8 lost=0 | 13 :146084 | Passed :121533 | Passed :146391 | 1 :146462 |
| 10 | S016 104731 en | :154254 | Victory BreakthroughEvacuated | 310.726 s (28.77) | completed=8/8 lost=0 | 13 :154240 | Passed :147199 | Passed :154547 | 1 :154618 |
| 11 | S016 130365 fa-IR | :213469 | Victory BreakthroughEvacuated | 312.494 s (28.93) | completed=8/8 lost=0 | 13 :213455 | Passed :155356 | Passed :213794 | 1 :213865 |
| 12 | S017 155923 en | :220371 | Victory BreakthroughEvacuated | 272.727 s (25.25) | completed=8/8 lost=0 | 20 :220357 | Passed :214602 | Passed :220664 | 1 :220735 |
| 13 | S017 104731 en | :240059 | Victory BreakthroughEvacuated | 261.918 s (24.25) | completed=8/8 lost=0 | 20 :240045 | Passed :221473 | Passed :240335 | 1 :240406 |
| 14 | S017 130365 fa-IR | :246584 | Victory BreakthroughEvacuated | 277.208 s (25.67) | completed=8/8 lost=0 | 20 :246570 | Passed :241144 | Passed :246870 | 1 :246941 |
| 15 | S018 155923 en | :315391 | Victory BreakthroughEvacuated | 318.067 s (29.45) | completed=8/8 lost=0 | 15 :315377 | Passed :247617 | Passed :315706 | 1 :315777 |
| 16 | S018 104731 en | :354880 | Victory BreakthroughEvacuated | 312.108 s (28.90) | completed=8/8 lost=0 | 16 :354866 | Passed :316514 | Passed :355173 | 1 :355244 |
| 17 | S018 130365 fa-IR | :393807 | Victory BreakthroughEvacuated | 381.652 s (35.34) | completed=8/8 lost=0 | 15 :393793 | Passed :356001 | Passed :394111 | 1 :394182 |
| 18 | S020 155923 en | :400530 | Victory ConvoyDelivered | 146.743 s (13.59) | completed=2/2 lost=0 | 12 :400516 | Passed :394919 | Passed :400822 | 1 :400893 |
| 19 | S020 104731 en | :407257 | Victory ConvoyDelivered | 146.764 s (13.59) | completed=2/2 lost=0 | 12 :407243 | Passed :401631 | Passed :407549 | 1 :407620 |
| 20 | S020 130365 fa-IR | :414468 | Victory ConvoyDelivered | 148.459 s (13.75) | completed=2/2 lost=0 | 12 :414454 | Passed :408357 | Passed :414772 | 1 :414843 |
| 21 | S021 155923 en | - | Abort expandedStartupFailure=BlockedSpawn :416187 | - | - | - | Passed :415580 | - | 0 :416261 |
| 22 | S021 104731 en | :421155 | Victory ConvoyDelivered | 123.958 s (11.48) | completed=2/2 lost=0 | 17 :421141 | Passed :416997 | Passed :421411 | 1 :421482 |
| 23 | S021 130365 fa-IR | :426561 | Victory ConvoyDelivered | 125.536 s (11.62) | completed=2/2 lost=0 | 17 :426547 | Passed :422219 | Passed :426829 | 1 :426900 |
| 24 | S022 155923 en | :433098 | Victory ConvoyDelivered | 151.168 s (14.00) | completed=2/2 lost=0 | 16 :433084 | Passed :427576 | Passed :433409 | 1 :433480 |
| 25 | S022 104731 en | :439908 | Victory ConvoyDelivered | 152.197 s (14.09) | completed=2/2 lost=0 | 16 :439894 | Passed :434217 | Passed :440219 | 1 :440290 |
| 26 | S022 130365 fa-IR | :446901 | Victory ConvoyDelivered | 154.825 s (14.34) | completed=2/2 lost=0 | 16 :446887 | Passed :441027 | Passed :447223 | 1 :447294 |
| 27 | S023 155923 en | - | Abort expandedStartupFailure=BlockedSpawn :448614 | - | - | - | Passed :448031 | - | 0 :448686 |
| 28 | S023 104731 en | :453997 | Victory ConvoyDelivered | 123.339 s (11.42) | completed=2/2 lost=0 | 20 :453983 | Passed :449445 | Passed :454253 | 1 :454324 |
| 29 | S023 130365 fa-IR | :459558 | Victory ConvoyDelivered | 124.277 s (11.51) | completed=2/2 lost=0 | 20 :459544 | Passed :455063 | Passed :459846 | 1 :459917 |
| 30 | S024 155923 en | :466621 | Victory ConvoyDelivered | 150.854 s (13.97) | completed=2/2 lost=0 | 19 :466607 | Passed :460654 | Passed :466930 | 1 :467001 |
| 31 | S024 104731 en | :473500 | Victory ConvoyDelivered | 152.019 s (14.08) | completed=2/2 lost=0 | 19 :473486 | Passed :467739 | Passed :473811 | 1 :473882 |
| 32 | S024 130365 fa-IR | :480201 | Victory ConvoyDelivered | 152.322 s (14.10) | completed=2/2 lost=0 | 20 :480187 | Passed :474620 | Passed :480524 | 1 :480595 |

## Queue B (b/unity.log)

| Job | Row / seed / locale | Terminal | Result | Clock (deadline %) | Objective | Gate-3 unused | Menu | Probe | countedWin |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S009 130365 en | :12053 | Victory TicketsDepleted | 590.890 s (49.24) | ownZones=2 opposingZones=1 tickets=500/0 | 13 :12039 | Passed :2294 | Passed :12309 | 1 :12380 |
| 2 | S011 130365 en | :21549 | Victory TicketsDepleted | 590.530 s (49.21) | ownZones=2 opposingZones=1 tickets=500/0 | 16 :21535 | Passed :13119 | Passed :21805 | 1 :21876 |
| 3 | S009 155923 en | :31486 | Victory TicketsDepleted | 590.248 s (49.19) | ownZones=2 opposingZones=1 tickets=500/0 | 13 :31472 | Passed :22633 | Passed :31742 | 1 :31813 |
| 4 | S009 104731 en | :42708 | Victory TicketsDepleted | 642.143 s (53.51) | ownZones=2 opposingZones=1 tickets=465/0 | 14 :42694 | Passed :32550 | Passed :43000 | 1 :43071 |
| 5 | S009 130365 fa-IR | :52528 | Victory TicketsDepleted | 590.761 s (49.23) | ownZones=2 opposingZones=1 tickets=500/0 | 13 :52514 | Passed :43808 | Passed :52832 | 1 :52903 |
| 6 | S011 155923 en | :66783 | Victory TicketsDepleted | 1076.546 s (89.71) | ownZones=2 opposingZones=1 tickets=62/0 | 17 :66769 | Passed :53661 | Passed :67039 | 1 :67110 |
| 7 | S011 104731 en | :75829 | Victory TicketsDepleted | 590.471 s (49.21) | ownZones=2 opposingZones=1 tickets=500/0 | 16 :75815 | Passed :67847 | Passed :76085 | 1 :76156 |
| 8 | S011 130365 fa-IR | :84960 | Victory TicketsDepleted | 590.947 s (49.25) | ownZones=2 opposingZones=1 tickets=500/0 | 17 :84946 | Passed :76894 | Passed :85247 | 1 :85318 |
| 9 | S057 155923 en | :92902 | Victory TicketsDepleted | 696.578 s (58.05) | ownZones=2 opposingZones=0 tickets=500/0 | 13 :92888 | Passed :86062 | Passed :93233 | 1 :93304 |
| 10 | S057 104731 en | :99752 | Abort ariaStopped:Blocked :99766 | 254.494 s (21.21) | ownZones=1 opposingZones=1 tickets=500/456 | 16 :99738 | Passed :94043 | - | 0 :99823 |
| 11 | S057 130365 fa-IR | :107627 | Victory TicketsDepleted | 691.980 s (57.66) | ownZones=2 opposingZones=0 tickets=500/0 | 13 :107613 | Passed :100564 | Passed :107931 | 1 :108002 |
| 12 | S059 155923 en | :115897 | Victory TicketsDepleted | 691.792 s (57.65) | ownZones=2 opposingZones=0 tickets=500/0 | 16 :115883 | Passed :108744 | Passed :116190 | 1 :116261 |
| 13 | S059 104731 en | :123927 | Abort ariaStopped:Blocked :123941 | 412.060 s (34.34) | ownZones=1 opposingZones=2 tickets=298/344 | 19 :123913 | Passed :117000 | - | 0 :123998 |
| 14 | S059 130365 fa-IR | :131706 | Victory TicketsDepleted | 656.676 s (54.72) | ownZones=2 opposingZones=0 tickets=500/0 | 16 :131692 | Passed :124753 | Passed :131973 | 1 :132044 |
