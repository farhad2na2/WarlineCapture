# Lane worker 22 — 2026-11-18 (05:31–06:03): FC/BT/CE prototypes start from the menu

Brief: program-logs/briefs/lane-integration22-2026-11-18.md. Logs: program-logs/2026-11-18/lane22/ (L).
One Editor at a time on this project; Fig/Fig-SquadPlay Editors untouched. disk_guard ok (240 GB) before each launch.

| Step | Result (harness line) | Log | Commit |
|---|---|---|---|
| Merge fcbt-authoring-fixes | compile `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed`, no error CS; python 148/148; test_drafts PASS | L/compile1.log | merge on integration |
| Planner test | `[SkirmishInstancePadPlannerTests] result=Passed controls=9`; entry does not quit by itself, stopped my verified PID 25801 after the marker | L/pads.log | – |
| S007 measure r2 | `[SkirmishAreaMeasurement] ... result=Completed areas=3 pads=0 routes=3 previewMutated=0` | L/s007m.log | ab22905df, draft 647c90bfe |
| S007 author | `result=Passed catalog=S007` instancePads rows=168 measured=168 derived=0 | L/s007author.log | 68be965d4 |
| S007 containment | `catalog=S007 result=Passed failures=0 rows=168` | L/s007contain.log | – |
| S007 menu | `[SkirmishMenuJourney] catalog=S007 result=Passed` (:2324); no expandedStartupFailure, no StartingGrantPlacement failure; Abort ariaStopped:Blocked (`[AriaBlocked] reason=101 intent=Recruit`) at match 8.581 s (0.72%) | L/s007menu/unity.log | (visible since 8ab8f5eeb) |
| S013 measure r2 | Completed areas=3 pads=2 routes=3; corridors+exit reachable, tower pads fitsAsIs | L/s013m.log | 24d2b4af9, draft 84f05b205 |
| S013 author | `result=Passed catalog=S013` rows=192 measured=168 rolePads=24 (:845) | L/s013author.log | d487e0bf0 |
| S013 containment | `catalog=S013 result=Passed failures=0 rows=192` | L/s013contain.log | visible b3285b36c |
| S013 menu | MenuJourney Passed (:2163); clean start; Abort ariaStopped:Blocked reason=101 (:3352) at 8.438 s (0.78%) | L/s013menu/unity.log | – |
| S019 convoy assets | `[SkirmishConvoyLogicalAssetsAuthoringEntry] result=Passed created=0` | L/s019convoy.log | – |
| S019 measure r2 | Completed areas=1 pads=5 routes=3; destination truck-reachable, 3 bays + 2 tower pads fitsAsIs | L/s019m.log | ebea9317c, draft 6ba5e1380 |
| S019 author | `result=Passed catalog=S019` rows=192 measured=168 rolePads=24 (:852) | L/s019author.log | 3e57302a9 |
| S019 containment | `catalog=S019 result=Passed failures=0 rows=192` | L/s019contain.log | visible 38c0405fc |
| S019 menu | MenuJourney Passed (:2303); clean start, player ConvoyLogic:3 (:2576); Abort ariaStopped:Blocked reason=101 (:3303) at 8.452 s (0.78%) | L/s019menu/unity.log | – |

Notes
- All three routes in every measurement: flood fill reached each segment, native PathfindBatchJob returned NativeQueryFailed on the middle segments (lengthBasis=flood). Accepted per OBJECTIVE_PROTOTYPES step 3 (native else flood).
- S013/S019 draft tower pads stay 10x10 (the 3x4 watchtower fits as is; a 3x4 pad would leave no snap margin for containment).
- The aborts are ARIA's: it has no FC/BT/CE skill and blocks at Recruit. No FC/BT/CE mission has been played to a result; the owner play (packet sections 2–5) is the next evidence.
- S013 defender towers are outside the 90 m initial_world trace radius; their spawn is not shown in the log.
- S019 was made visible like S013 (owner allowance 2026-11-17 covers all three prototypes, internal, unpublished).
