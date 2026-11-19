# Windows dev results — W9 City Crossroads Breakthrough S037–S042 measure → author → menu (2026-11-18/19)

Branch `missions/win-w9-cc-bt`, continuing the drafts stage in `2026-11-18-w9-cc-bt.md`; merged with integration up to `033660b12` (`8aede2c9b`).
The Windows dev lead made the fixes and commits after the Codex drafts stage.
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\measure-S037-r2`, `measure-S037-r3`, `w9-bt-blockedspawn\`, `w9-bt\`.

## 1. Routes and r2 (`15178e5e0`, `8ea173ba9`)
- FlankNorthRuins/FlankSouthSweep use the W7 S031 r2 chains on host S026; CorridorA grows to the W7 FrontlineB rectangle.
- r2 (`RunCatalogAreaMeasurementAndExit`, host S026, seed 130365, en): CorridorA/B/Exit 3567/6069/1116 m2 inside playable and reachable; defense.approach.a/b `fitsAsIs`; routes 416/491/664 m (flood) with no failed flood or native segment.

## 2. Preflight failure and instance-pad fix `308e06781`
First pipeline: `pad_preflight.py` FAIL 2/26 on S040 and S042. Enemy Service 17 at (1091,320) and the air.enemy AirReturn 19 at (1064,374.5) (S027-derived) hit S026 scenery. Both pads and their anchors moved to the W7 S034 positions (1028,315.5) and (1052,459.5), which pass on S026. Preflight 6/6 PASS after.

## 3. BlockedSpawn and defense-pad fix `8eef23a65`
Second pipeline: authoring 6/6, containment 6/6 (assets `269de5b2e`), but all six menu launches aborted at 0 s with `expandedStartupFailure=BlockedSpawn`: `[SkirmishStartingBuilding] failed building=Building_GuardTower faction=2 preferred=int2(1097, 349|350)`. defense.approach.b at (1098.5,352) (S027 service.enemy) sits on the S026 road band z 348–360, which has no building placement. Preflight skips objective pads, containment checks the envelope only, and r2 reported `fitsAsIs=true`, so none of them caught it (queued as W22 by the lead).
defense.approach.b moved to (1098.5,366): layout-dump bitmap flag 0, no pad overlap. defense.approach.a (1087.5,371) already placed.

## 4. r3 and binding `37565bcf4`
Same command at `8eef23a65` → `[SkirmishAreaMeasurement] catalog=S026 result=Completed draft=s037-... areas=3 pads=2 routes=3 previewMutated=0`. Areas unchanged (3567/6069/1116 m2); defense.approach.a (1087.5,371) and .b (1098.5,366) fit with 100/100 vehicle-passable cells; routes 416/491/664 m, 0 failed segments.
`Design/AgentReports/SkirmishExpansion/S037/area-measurement-r3.json` replaces r2 (LF); drafts S037–S042 bind r3. `check_draft.py` 6/6 PASS. (A first r3 attempt failed before measuring because my script passed both jobs as one draft path; log kept as `measure-S037-r3-argfail`.)

## 5. Third pipeline (`37565bcf4` → assets `617826762`)
- Preflight 6/6 PASS. `AuthorFromDraftList` → `result=Completed drafts=6 passed=6 failed=0`. Containment `result=Passed failures=0` 6/6 (rows 192 field / 280 established).
- One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; log `win-dev-unity\w9-bt\menu\unity.log`). `expandedStartupFailure` 0.

| Row | Menu journey | Result | Reason | match s | deadline % | completed/lost |
|---|---|---|---|---|---|---|
| S037 ground field | Passed | Victory | BreakthroughEvacuated | 219.4 | 20.32 | 8/8, 0 |
| S038 ground established | Passed | **Defeat** | BreakthroughImpossible | 149.7 | 13.86 | 1/8, 5 |
| S039 air field | Passed | **Defeat** | BreakthroughImpossible | 217.5 | 20.14 | 1/8, 5 |
| S040 air established | Passed | **Defeat** | BreakthroughImpossible | 234.5 | 21.72 | 1/8, 5 |
| S041 combined field | Passed | Victory | BreakthroughEvacuated | 220.8 | 20.45 | 8/8, 0 |
| S042 combined established | Passed | **Defeat** | BreakthroughImpossible | 164.8 | 15.26 | 1/8, 5 |

All six returned to the result screen (`result_return_ok=yes`). Harness evidence (104 MB) moved to `win-dev-unity\w9-bt\harness-evidence`, not committed.

## Open
- The defeats look like an objective-geometry problem, not ARIA or the moved tower. In S038 and S040 the player rifle squad dies at (1076–1080, 399–417), inside BreakthroughExit (1064..1094.5, 374.5..409.5). That exit was built from S027 air.enemy and the enemy rally, so it sits next to the enemy barracks (1116,380) and watchtower (1098,405), ~19 m away. The moved defense tower at (1098.5,366) is ~50 m away. Moving the exit away from the enemy base needs a new r-measurement; not done here. One Windows launch per row only.
- Air/combined rows have no measured Airport position.
- Mac quickcheck and counted runs pending.
