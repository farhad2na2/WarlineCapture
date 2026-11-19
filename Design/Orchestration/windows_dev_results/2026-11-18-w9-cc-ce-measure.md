# Windows dev results — W9 City Crossroads Convoy Escort S043–S048 measure → author → menu (2026-11-18/19)

Branch `missions/win-w9-cc-ce`, continuing the drafts stage in `2026-11-18-w9-cc-ce.md`; merged with integration up to `033660b12` (`35fc29a9d`).
The Windows dev lead made the fixes and commits after the Codex drafts stage.
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\measure-S043-r2`, `measure-S043-r3`, `w9-ce-blockedspawn\`, `w9-ce\`.

## 1. Routes and r2 (`cb478a751`, `4029a7973`)
- Three vehicle routes from the W7 S031 r2 chains on host S026.
- r2 (`RunCatalogAreaMeasurementAndExit`, host S026, seed 130365, en): ConvoyDestination 1116 m2 inside playable and reachable; convoy.origin.1–3 and defense.approach.a/b `fitsAsIs`; routes 416/491/664 m (flood) with no failed segment.

## 2. Preflight failure and instance-pad fix `e9b83f008`
First pipeline: `pad_preflight.py` FAIL 5/26 on S046 and S048. Enemy Service 17 at (1091,320) and the air.enemy AirReturn 19 at (1064,374.5) hit S026 scenery; the air.player AirReturn 18 at (1013,719.5) overlapped Supply pads 4 and 8. All three and their anchors moved to the W7 S034 positions (1028,315.5), (1052,459.5) and (1052,651.5). Preflight 6/6 PASS after.

## 3. BlockedSpawn and defense-pad fix `9b74340e5`
Second pipeline: authoring 6/6, containment 6/6 (assets `5f80ac49f`), but all six menu launches aborted at 0 s with `expandedStartupFailure=BlockedSpawn`: enemy guard towers failed at int2(1097, 349|350) and int2(1070, 357). defense.approach.a (1098.5,352) and .b (1071.5,359.5) (S027 economy/service.enemy) sit on the S026 road band z 348–360. Preflight, containment and r2 did not catch this (queued as W22 by the lead).
defense.approach.a moved to (1098.5,366) and .b to (1057,368): layout-dump bitmap flag 0, no pad overlap.

## 4. r3 and binding `2ef1a3b0a`
Same command at `9b74340e5` → `[SkirmishAreaMeasurement] catalog=S026 result=Completed draft=s043-... areas=1 pads=5 routes=3 previewMutated=0`. ConvoyDestination 1116 m2 inside playable (1086 vehicle-passable cells); convoy.origin.1–3 fit; defense.approach.a/b fit with 100/100 vehicle-passable cells; routes 416/491/664 m, 0 failed segments.
`Design/AgentReports/SkirmishExpansion/S043/area-measurement-r3.json` replaces r2 (LF); drafts S043–S048 bind r3. `check_draft.py` 6/6 PASS.

## 5. Third pipeline (`2ef1a3b0a` → assets `2b062e63e`)
- Preflight 6/6 PASS. `AuthorFromDraftList` → `result=Completed drafts=6 passed=6 failed=0`. Containment `result=Passed failures=0` 6/6 (rows 192 field / 280 established).
- One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; log `win-dev-unity\w9-ce\menu\unity.log`). `expandedStartupFailure` 0.

| Row | Menu journey | Result | Reason | match s | deadline % | delivered/lost |
|---|---|---|---|---|---|---|
| S043 ground field | Passed | **Abort** | ariaStopped:Blocked (reason=101) | 176.4 | 16.33 | 0/2, 0 |
| S044 ground established | Passed | Victory | ConvoyDelivered | 122.0 | 11.29 | 2/2, 0 |
| S045 air field | Passed | **Abort** | ariaStopped:Blocked (reason=101) | 222.6 | 20.61 | 0/2, 0 |
| S046 air established | Passed | Victory | ConvoyDelivered | 205.1 | 18.99 | 2/2, 1 |
| S047 combined field | Passed | **Abort** | ariaStopped:Blocked (reason=101) | 220.0 | 20.37 | 0/2, 0 |
| S048 combined established | Passed | Victory | ConvoyDelivered | 130.7 | 12.10 | 2/2, 0 |

Victories returned to the result screen (`result_return_ok=yes`); the aborts are `unproven`. Harness evidence (91 MB) moved to `win-dev-unity\w9-ce\harness-evidence`, not committed.

## Open
- The three Field Base aborts are `[AriaBlocked] reason=101 kind=Waiting intent=ObserveObjective` (actions 69–88, t≈208–239 s), the ARIA block queued as W19 (S049). Not a draft defect; not fixed here (W19 needs the owner's OK). One Windows launch per row only.
- Air/combined rows have no measured Airport position.
- Mac quickcheck and counted runs pending.
