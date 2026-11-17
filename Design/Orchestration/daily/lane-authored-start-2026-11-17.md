# Lane worker (authored-start) 2026-11-17

- Step 1: cherry-picked 3b4c598eb, c8d92c7ef; merged missions/aria-intercept; fixed stale unittest (1c6d75408). Compile: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane-compile2 (Passed, 0 error CS). First authoring launch crashed in Bee (fd assertion, lane-author), relaunch fine.
- Step 2: S002 gate 4/4 Victory (272.8/266.7/256.2/261.1 s; intercept events 4/6/6/4), /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane-s002gate/unity.log lines 1614115-1614160.
- Step 3/4: footprints from S002 dump (+aliases via VisualKey) and S004 dump (helipad 16x15, airport 210x55, /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane-s004b). Authored Passed variants=12: S049 (lane-author2), S026+S050 (lane-author3:687,851), S006 (lane-author4:654).
- Step 5 (play, seed 130365 en): S049 Abort (lane-s049play: Building_Barrack f2 int2(347,186) BlockedSpawn, 2 failures), S026 Abort 34.8 s (lane-play:4701, 18 starting-building failures incl. both barracks), S050 Abort 40.9 s (lane-play:4716), S006 Abort 30.8 s (lane-s006, 16 failures, barracks f1 int2(921,643) f2 int2(1373,643)). Startup defect: placement of pad cells is rejected (BlockedSpawn); not tuned.
- S004 seed note: first-visit seeds are 104733/130367/155925; S004 104733 Victory 240.8 s (lane-s004b).
- 5b Airport probe skipped: no S026/S050 start.
