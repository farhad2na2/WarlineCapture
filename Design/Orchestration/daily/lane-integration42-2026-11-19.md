# Lane worker 42, 2026-11-19: counted runs for City Crossroads BT and the Desert Base BT/CE gaps

HEAD 7aaf1b968 at start (W23b CC BT, W24, W25, W28 merged), branch missions/integration; the program lead's later commits (to 4884ab38c) touched only Design/Orchestration and Tools/Missions, and both queues report sourceUnchanged=1.
Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-19/lane42/`. No asset or code changes. disk_guard 0 before every launch; one Editor at a time (main checkout); Fig/Fig-SquadPlay Editors untouched; no Editor stopped.
Stale working-tree files from eaa64700c (16 runs.csv, lane 41 daily note, state.json/STATUS.md) were left alone; this commit stages only explicit paths and a STATUS blob built from HEAD.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0, Editor 51 s; python 153 OK | compile1.log:608; Library Redirect Path :93; python.txt:33-35 |
| 2 queue A, City Crossroads BT S037-S042 (24 jobs, one launch) | 23/24 Victory, 1 Abort (ariaStopped:Manual); Completed wins=23 sourceUnchanged=1 exitCode=0 | a/unity.log:270622 |
| 3 queue B, Desert Base gaps (8 jobs, one launch) | 8/8 Victory; Completed wins=8 sourceUnchanged=1 exitCode=0 | b/unity.log:49245 |

Both queues: `--menu-journey --validation-library --no-table --timeout 30000`, validation library enabled=1 (a :600, b :599). No "First-visit ARIA seed" rejection, so no re-runs.
Queue A ran 09:26-11:00, queue B 11:00-11:42. Source hash 8f917a480366a8ec94feff3503f3707e9ba039570a8ee98769553422450815ce (new; lane 41 was 80228993...).

## Findings

- CC BT S037-S042 after W23b: 23/23 played jobs BreakthroughEvacuated 8/8 lost=0 exitOpen=1 at 14.85-20.79% of 1080 s; 0 order_retry/order_blocked, 0 AriaBlocked (W23 had order_blocked x6 and AriaBlocked 101/111).
- S040 130365 fa-IR aborted at 143.9 s (13.33%, 4/8 evacuated, lost=0): first failing line a :212985 `[AriaTouchInput] physicalTakeover device=Mouse type=FastMouse generated=False`,
  then :213031 `aborted=True reason=ariaStopped:Manual`. A real (non-generated) mouse event reached the GUI Editor window; it is the only physicalTakeover in this log or in lanes 40/41. Not a game failure; not re-run (brief allows re-runs only for seed rejections). S040 has 3/4 counted wins.
- W25 fix holds: S021/S023 155923 en, which aborted BlockedSpawn in lane 41, both won ConvoyDelivered 2/2 at 11.95% / 11.81% with 0 startup failure.
- S013 won 3/3 (BreakthroughEvacuated 8/8, 26.48-31.08%); S019 (W28 pad) won 3/3 (ConvoyDelivered 2/2, 14.43-15.25%). With lane 41's 130365 en win each has 4 wins, but on two source hashes; whether they count together is the lead's call.
- Pacing: BT CC wins 14.85-20.79%, CE wins 11.81-15.25%: below the 20% floor (except S037 104731 at 20.79%) and below the owner's 25-50% per-family target (STATUS 14:10). S013 is inside it.
- Menu journey Passed 32/32; terminal probe Passed MAIN MENU interventions=0 on all 31 played-out jobs; countedWin=1 on all 31 wins.
- Gate-3 unused is not empty on any job (9-20 entries; aircraft roles on all CC BT rows, building.airport on S039-S042, S021, S023).
- Whole logs: 0 [AriaBlocked]/[AriaBlockedControl], 0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 FailureCode, 0 MissingReference, 0 error CS.
  `[CanonicalAircraftGeometry] result=Failed` (stale stamps, Codex 040) logs once per job (24 + 8) and did not block play.

runs.csv rows appended under `<ID>/_Evidence/candidates/8f917a48.../runs.csv` for S013, S019, S021, S023, S037-S042 (committed; trace folders left untracked).

## Per job

## Queue A (a/unity.log)

| Job | Row / seed / locale | Terminal | Result | Clock (deadline %) | Objective | Gate-3 unused | Menu | Probe | countedWin |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S037 130365 en | :8070 | Victory BreakthroughEvacuated | 199.588 s (18.48) | completed=8/8 lost=0 | 13 :8056 | Passed :2336 | Passed :8380 | 1 :8451 |
| 2 | S038 130365 en | :15587 | Victory BreakthroughEvacuated | 177.621 s (16.45) | completed=8/8 lost=0 | 9 :15573 | Passed :9216 | Passed :15879 | 1 :15950 |
| 3 | S039 130365 en | :21763 | Victory BreakthroughEvacuated | 212.794 s (19.70) | completed=8/8 lost=0 | 17 :21749 | Passed :16687 | Passed :22037 | 1 :22108 |
| 4 | S040 130365 en | :29734 | Victory BreakthroughEvacuated | 183.941 s (17.03) | completed=8/8 lost=0 | 15 :29720 | Passed :22845 | Passed :30044 | 1 :30115 |
| 5 | S041 130365 en | :51940 | Victory BreakthroughEvacuated | 211.177 s (19.55) | completed=8/8 lost=0 | 20 :51926 | Passed :30852 | Passed :52196 | 1 :52267 |
| 6 | S042 130365 en | :92344 | Victory BreakthroughEvacuated | 185.272 s (17.15) | completed=8/8 lost=0 | 17 :92330 | Passed :53004 | Passed :92636 | 1 :92707 |
| 7 | S037 155923 en | :98394 | Victory BreakthroughEvacuated | 207.049 s (19.17) | completed=8/8 lost=0 | 11 :98380 | Passed :93465 | Passed :98686 | 1 :98757 |
| 8 | S037 104731 en | :104786 | Victory BreakthroughEvacuated | 224.564 s (20.79) | completed=8/8 lost=0 | 11 :104772 | Passed :99515 | Passed :105078 | 1 :105149 |
| 9 | S037 130365 fa-IR | :110420 | Victory BreakthroughEvacuated | 211.958 s (19.63) | completed=8/8 lost=0 | 11 :110406 | Passed :105845 | Passed :110707 | 1 :110778 |
| 10 | S038 155923 en | :120560 | Victory BreakthroughEvacuated | 190.361 s (17.63) | completed=8/8 lost=0 | 9 :120546 | Passed :111515 | Passed :120816 | 1 :120887 |
| 11 | S038 104731 en | :128817 | Victory BreakthroughEvacuated | 182.902 s (16.94) | completed=8/8 lost=0 | 9 :128803 | Passed :121647 | Passed :129109 | 1 :129180 |
| 12 | S038 130365 fa-IR | :159540 | Victory BreakthroughEvacuated | 168.864 s (15.64) | completed=8/8 lost=0 | 9 :159526 | Passed :129917 | Passed :159843 | 1 :159914 |
| 13 | S039 155923 en | :170543 | Victory BreakthroughEvacuated | 211.314 s (19.57) | completed=8/8 lost=0 | 17 :170529 | Passed :160674 | Passed :170818 | 1 :170889 |
| 14 | S039 104731 en | :176665 | Victory BreakthroughEvacuated | 213.025 s (19.72) | completed=8/8 lost=0 | 17 :176651 | Passed :171649 | Passed :176962 | 1 :177033 |
| 15 | S039 130365 fa-IR | :182372 | Victory BreakthroughEvacuated | 212.758 s (19.70) | completed=8/8 lost=0 | 17 :182358 | Passed :177770 | Passed :182657 | 1 :182728 |
| 16 | S040 155923 en | :190843 | Victory BreakthroughEvacuated | 160.330 s (14.85) | completed=8/8 lost=0 | 14 :190829 | Passed :183465 | Passed :191135 | 1 :191206 |
| 17 | S040 104731 en | :198201 | Victory BreakthroughEvacuated | 186.008 s (17.22) | completed=8/8 lost=0 | 14 :198187 | Passed :191944 | Passed :198513 | 1 :198584 |
| 18 | S040 130365 fa-IR | :213017 | Abort ariaStopped:Manual :213031 | 143.922 s (13.33) | completed=4/8 lost=0 | 14 :213003 | Passed :199321 | - | 0 :213088 |
| 19 | S041 155923 en | :219023 | Victory BreakthroughEvacuated | 212.794 s (19.70) | completed=8/8 lost=0 | 20 :219009 | Passed :213797 | Passed :219295 | 1 :219366 |
| 20 | S041 104731 en | :224741 | Victory BreakthroughEvacuated | 212.526 s (19.68) | completed=8/8 lost=0 | 20 :224727 | Passed :220086 | Passed :225013 | 1 :225084 |
| 21 | S041 130365 fa-IR | :242827 | Victory BreakthroughEvacuated | 213.386 s (19.76) | completed=8/8 lost=0 | 20 :242813 | Passed :225820 | Passed :243113 | 1 :243184 |
| 22 | S042 155923 en | :254864 | Victory BreakthroughEvacuated | 186.433 s (17.26) | completed=8/8 lost=0 | 17 :254850 | Passed :243920 | Passed :255118 | 1 :255189 |
| 23 | S042 104731 en | :262322 | Victory BreakthroughEvacuated | 190.005 s (17.59) | completed=8/8 lost=0 | 16 :262308 | Passed :255926 | Passed :262650 | 1 :262721 |
| 24 | S042 130365 fa-IR | :269754 | Victory BreakthroughEvacuated | 181.231 s (16.78) | completed=8/8 lost=0 | 17 :269740 | Passed :263456 | Passed :270074 | 1 :270145 |

## Queue B (b/unity.log)

| Job | Row / seed / locale | Terminal | Result | Clock (deadline %) | Objective | Gate-3 unused | Menu | Probe | countedWin |
|---|---|---|---|---|---|---|---|---|---|
| 1 | S021 155923 en | :6789 | Victory ConvoyDelivered | 129.108 s (11.95) | completed=2/2 lost=0 | 17 :6775 | Passed :2356 | Passed :7045 | 1 :7116 |
| 2 | S023 155923 en | :11964 | Victory ConvoyDelivered | 127.561 s (11.81) | completed=2/2 lost=0 | 20 :11950 | Passed :7863 | Passed :12218 | 1 :12289 |
| 3 | S013 155923 en | :18645 | Victory BreakthroughEvacuated | 335.614 s (31.08) | completed=8/8 lost=0 | 11 :18631 | Passed :13047 | Passed :18953 | 1 :19024 |
| 4 | S013 104731 en | :25507 | Victory BreakthroughEvacuated | 305.276 s (28.27) | completed=8/8 lost=0 | 12 :25493 | Passed :19931 | Passed :25815 | 1 :25886 |
| 5 | S013 130365 fa-IR | :31887 | Victory BreakthroughEvacuated | 286.008 s (26.48) | completed=8/8 lost=0 | 13 :31873 | Passed :26609 | Passed :32153 | 1 :32224 |
| 6 | S019 155923 en | :37190 | Victory ConvoyDelivered | 159.111 s (14.73) | completed=2/2 lost=0 | 13 :37176 | Passed :32872 | Passed :37444 | 1 :37515 |
| 7 | S019 104731 en | :43175 | Victory ConvoyDelivered | 155.880 s (14.43) | completed=2/2 lost=0 | 13 :43161 | Passed :38221 | Passed :43429 | 1 :43500 |
| 8 | S019 130365 fa-IR | :48625 | Victory ConvoyDelivered | 164.669 s (15.25) | completed=2/2 lost=0 | 15 :48611 | Passed :44161 | Passed :48891 | 1 :48962 |
