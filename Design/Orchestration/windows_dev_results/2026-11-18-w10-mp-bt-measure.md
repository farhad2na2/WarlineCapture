# Windows dev results — W10 Mountain Pass Breakthrough S061–S066 measure → author → menu (2026-11-19)

Branch `missions/win-w10-mp-bt`, continuing the drafts stage in `2026-11-18-w10-mp-bt.md`; merged with integration (`7ec59835f`).
The Windows dev lead made the fixes and commits after the Codex drafts stage.
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\measure-S061-r2` and `win-dev-unity\w10-bt\`.

## 1. Routes `c24b23d0f`
MainHighway, FlankNorthRuins and FlankSouthSweep reuse the W8 S055 r2 vehicle chains on host S050 (ending at `rally.enemy`).

## 2. Defense pads off the road band `e9c8c3dc9`
Before measuring, `objective_pad_surface.py` (interim W22-style check against the S050 layout-dump bitmap) flagged both guard-tower spots on road. defense.approach.a moved (1209.5,441) → (1209.5,446) and .b (1188.5,416.5) → (1152,446): bitmap flag 0 under the 3×4 tower and no instance-pad overlap. (A first choice, (1165,446), overlapped the f2 satellite dish on the CE rows and was dropped for both families.)

## 3. r2 and binding `91803b68f`
`RunCatalogAreaMeasurementAndExit`, host S050, seed 130365, en → `[SkirmishAreaMeasurement] catalog=S050 result=Completed`.
- Breakthrough areas 1491 / 2619 / 1845 m2, all inside playable and reachable from both bases.
- defense.approach.a/b fit (98/98 and 100/100 vehicle-passable cells).
- Routes 223 / 292 / 322 m (flood), no failed segment.

`Design/AgentReports/SkirmishExpansion/S061/area-measurement-r2.json` committed (LF); S061–S066 bind it. `check_draft.py` 6/6 PASS.

## 4. Pipeline (`91803b68f` → assets `e65ab2954`)
- `pad_preflight.py` 6/6 PASS. `AuthorFromDraftList` → `result=Completed drafts=6 passed=6 failed=0`. Containment `result=Passed failures=0` 6/6 (rows 192 field / 280 established).
- One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; log `win-dev-unity\w10-bt\menu\unity.log`). `expandedStartupFailure` 0.

| Row | Menu journey | Result | Reason | match s | deadline % | completed/lost |
|---|---|---|---|---|---|---|
| S061 ground field | Passed | Victory | BreakthroughEvacuated | 317.2 | 29.37 | 8/8, 1 |
| S062 ground established | Passed | **Defeat** | BreakthroughImpossible | 631.7 | 58.49 | 0/8, 5 |
| S063 air field | Passed | Victory | BreakthroughEvacuated | 166.3 | 15.40 | 8/8, 0 |
| S064 air established | Passed | **Defeat** | BreakthroughImpossible | 595.4 | 55.13 | 0/8, 5 |
| S065 combined field | Passed | **Defeat** | BreakthroughImpossible | 137.3 | 12.71 | 0/8, 5 |
| S066 combined established | Passed | **Defeat** | BreakthroughImpossible | 456.8 | 42.29 | 0/8, 5 |

All six returned to the result screen (`result_return_ok=yes`, `[SkirmishTerminalFrozen] result=Passed`). Harness evidence (152 MB) moved to `win-dev-unity\w10-bt\harness-evidence`, not committed.

## 5. Integration merge `7ec59835f`
Conflicts only in `SkirmishPublicationManifest.asset` and `SkirmishExpansionCatalog.asset`; resolved as the union (integration's S031–S036 and S055–S060 first, then S061–S066). Against integration the branch is pure additions plus those two registries. The merged registries have not been re-run in Unity.

## Open
- Four defeats with 0/8 evacuated. BreakthroughExit sits about 1 m from the enemy staging pad and 8 m from the enemy base, the same placement as the City Crossroads BT exit (W23). Not tuned here; moving the exit needs a new measurement. One Windows launch per row only.
- Air/combined rows have no measured Airport position.
- Mac quickcheck and counted runs pending.
