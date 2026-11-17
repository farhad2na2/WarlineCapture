# Lane worker 17 (2026-11-17) - stopped: Unity startup hang
Logs: /private/tmp/w17 (compile.log, ce-pin.log, s007-measure.log)
- 1 Merged missions/aria-defence-ranged (3a62d3cd9) --no-ff. Python 145 OK. Compile Passed (compile.log:1864, 0 error CS, no new metas).
- 2 pad_preflight FAIL on spawn.infantry.{player,enemy} for S007/S013/S019 (scenery); 2 m shift each -> PASS; drafts committed.
- S019 step 1 failed: 'Existing chassis/config pins differ; do not overwrite' (ce-pin.log:2254); Unit_Veh_Truck_Tray.prefab sha256 09773ac2... vs pinned a1d734b6...; nothing replaced.
- S007 step 2: Editor never logged Library Redirect Path in 6 min; terminated my Editor. Needs one owner click.
- Step 4 skipped on program lead's instruction (S002 guard tower removal). S013, visibility, menu launches, owner packet sections 2-4: not done (no prototype launched).
