# Lane worker 2 (integration shift 2) 2026-11-17
Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane2-*
- Step 1: merged pad-frame-fix, airport-probe, objective-authoring (007ffc3ee); python tests 51 OK; compile Passed (lane2-compile, line 1833).
- Step 2: lane2-author: S006,S026,S050,S049 Passed variants=12 (lines 656/826/993/1160).
- Step 3: S049 Passed (lane2-cont-S049:485, rows=168). S026/S050 failed first (no building.helipad structure_footprints entry); added helipad 16x15 to drafts; re-authored (lane2-author2: 661/835) and containment Passed (lane2-cont2-S026:487, S050:488, rows=256). S006: Failed (checker only reads Tools/Missions/drafts; S006 draft is in examples/, has no instance_pads); no draft-path option, skipped.
- Step 4: play 130365 en, queue lane2-play: S026, S050, S049 all Abort expandedStartupFailure=BlockedSpawn, countedWin=0 (unity.log 3726/4644/5527). No [SkirmishGateUnits] lines (runs aborted at start). Pad-frame fix + containment pass did not remove BlockedSpawn; cause not yet diagnosed (per-building failure lines not found by grep).
- Steps 5-7 not run (lead: later slot).
