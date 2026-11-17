# Lane worker 3 (integration shift 3) 2026-11-17
Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane3-*
- 1 Merge placement-diag --no-ff; python 52 OK; compile Passed (lane3-compile.log:1929, 0 error CS).
- 2 Unit tests Passed cases=5 (lane3-unit.log:486).
- 3 Authoring S006/S026/S050/S049 Passed variants=12 (lane3-author/unity.log:672/855/1036/1223; batch 4/4 line 1246).
- 4 Containment Passed failures=0: S006 (lane3-cont-S006:748, rows=256, min admitted 676), S026 (:749, 974), S049 (:652, rows=168, 441), S050 (:748, 289).
- 5 Play 130365 en (lane3-play/unity.log): all 4 Abort expandedStartupFailure=BlockedSpawn countedWin=0 (2729/3893/4770/5643). preferredFirstReject: S006 sourceBuildings=4 road=2; S026 road=5; S049 road=2; S050 road=2. Pads admit 676/977/441+/289+ origins but each failure has all rejected by sourceBuildings/road (e.g. S006 barracks: sourceBuildings=548 roadOrWater=128 admittedByPad=676). Rescan gate-3/4: all unproven (no [SkirmishGateUnits] lines).
- 6 AP dump: lane3-dump/unity.log:2909 catalog=S004 result=Completed, dump.json kept. Catalog dumps (lead request): S002 FAILED (lane3-dump-S002/unity.log:2675 'Layout dump has no launched logical map'; TickLayoutDump loads the map via SkirmishS004Environment.LoadMapForSession, S004-only; first attempts lacked WARLINE_S002_SEED/LOCALE: ...-noseed/-noseed2). S026 FAILED (BlockedSpawn abort, unity.log:2571). S050 FAILED (BlockedSpawn, unity.log:2320/2405, exit 2). No layout_dump_to_draft run (S002 dump absent).
- 7 Probes skipped (no mission started).
- Editors that idled after an exception (no -quit) were stopped after 3 min (own PIDs verified); no LibraryInitializing.
