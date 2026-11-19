# Windows dev results — W7 City Crossroads Frontline Control S031–S036 (2026-11-18)

Branch `missions/win-w4-cc-fc` (W4 drafts `64ba63811`, continued), merged with integration up to `3e65ef053`. W4's earlier results moved to `2026-11-18-w4-cc-fc.md` (`44e88b6b0`).
Codex gpt-6.2-sol high for the draft fixes: brief/transcript/report `D:\Projects\WarlineCapture-program-logs\2026-11-18\codex\w4-cc-fc-w7*`. The worker made no commits; the Windows dev lead reviewed and committed.
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\w7-measure-S031-r1`, `w7-measure-S031-r2`, `w7\`.

## 1. First measurement r1 (draft `44e88b6b0`)
`RunCatalogAreaMeasurementAndExit` on host S026, seed 130365, en → `[SkirmishAreaMeasurement] catalog=S026 result=Completed ... areas=3 routes=3 previewMutated=0 physicalSourceModified=0`.
Found: FlankNorthRuins/FlankSouthSweep share west_refined 00–06; native vehicle query `NativeQueryFailed failureCode=7` (GoalPlacementBlocked) on west_refined 05→06 and 06→07; FrontlineB 1200 m2 (A 6069, C 2902). r1 is not committed.

## 2. Draft fixes `5a529c206` (S031–S036, same geometry)
- FlankNorthRuins skips west_refined 06–07 via S026 dump candidate #274 (977,555) and S026 `candidate.route.tank.primary.waypoint.04`.
- FlankSouthSweep gets its own chain from S026 layout-dump candidates #303, #393, #398, #279, #371, #360, #357 (centres of Unity-measured legal Fuel Bladder / Ground Staging footprints, checked against `LayoutDumps/S026/dump.json`); it shares only the route start/goal anchors.
- FrontlineB grows to ~3500 m2 between primary waypoint 02 and dump #303.

## 3. Re-measurement r2 (`5a529c206`) and binding `72c89f7ba`
Same command → `result=Completed areas=3 pads=0 routes=3`. FrontlineA/B/C 6069/3567/2902 m2, inside playable, infantry and truck reachable from both bases and both stagings (min reachable cells 5173/3005/2495). Routes MainHighway 416 m, FlankNorthRuins 491 m, FlankSouthSweep 664 m (flood); every segment flood `Reached` and native `NativeQueryFoundExactGoal`.
`Design/AgentReports/SkirmishExpansion/S031/area-measurement-r2.json` committed (LF, `eol=lf`); S031–S036 `area_measurement.artifact` = that file, notes start `candidate=1, measured 2026-11-18 (Windows W7 r2 ...)`.
`check_draft.py` 6/6 PASS; `test_drafts.py` PASS (40 drafts); `test_check_draft.py` OK. quickcheck not run (no .NET 8 SDK on Windows).

## 4. W6 steps b–d (`f65e0f86c` → assets `1fe08c9ee`)
- b. `pad_preflight.py --draft <draft> --dump LayoutDumps/S026/dump.json`: PREFLIGHT PASS 6/6.
- b. `AuthorFromDraftList` → `[SkirmishMissionAuthoringBatch] result=Completed drafts=6 passed=6 failed=0`; each `[SkirmishMissionAuthoring] result=Passed catalog=S03x source=opmap.skirmish.city_crossroads variants=12 publication=InProgress library=Planned readiness=0`.
- c. `RunAuthoredLayoutValidation` → `[SkirmishInstancePadContainment] catalog=S03x result=Passed failures=0` for all six (rows 168 field / 256 established).
- d. One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; one Editor, `-releaseCodeOptimization`; log `win-dev-unity\w7\menu\unity.log`). The queue prints `class=counted`, but `--no-table` writes no result row; Windows runs are not counted.

| Row | Menu journey | expandedStartupFailure | Result | match s | deadline % |
|---|---|---|---|---|---|
| S031 ground field | Passed | none | Victory | 576.6 | 48.05 |
| S032 ground established | Passed | none | Victory | 543.1 | 45.26 |
| S033 air field | Passed | none | Victory | 543.6 | 45.30 |
| S034 air established | Passed | none | Victory | 917.0 | 76.42 |
| S035 combined field | Passed | none | Victory | 575.6 | 47.96 |
| S036 combined established | Passed | none | Victory | 543.4 | 45.28 |

All six returned to the result screen (`result_return_ok=yes`). Harness evidence (219 MB under `Design/AgentReports/SkirmishExpansion/S03x/_Evidence`) moved to `win-dev-unity\w7\harness-evidence`, not committed.

## Open
- ARIA used no aircraft on the air/combined rows (S033–S036 `unused` lists airport/aircraft); no measured Airport position exists on City Crossroads (W4 note). Established War/LargeWar second-refinery pads stay compiler-derived (this launch used Standard).
- Mac quickcheck and counted runs pending.
