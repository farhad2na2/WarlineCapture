# Windows dev results — W8 Mountain Pass Frontline Control S055–S060 (2026-11-18)

Branch `missions/win-w5-mp-fc` (W5 drafts, continued), merged with integration up to `9570c9e70` (`096ab45ce`). W5's earlier results are in `2026-11-18-w5-mp-fc.md` (`a8f76ae77`).
The Windows dev lead made the route fixes and commits (no Codex worker for W8).
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\w8-measure-S055-r1` and `win-dev-unity\w8\`.

## 1. Route fixes `4871b75fe` (S055–S060)
- FlankNorthRuins (basemove.route.2 + S050 dump candidate #292) and FlankSouthSweep (new basemove.route.3 from S050 dump candidates #31/#1/#37/#95) become 6 m vehicle routes like S007, independent of MainHighway.
- All three routes end at `rally.enemy` (S050 dump #293) instead of the `base.enemy` goal that returned native code 7.

## 2. Measurement r2 and binding `ff81cb84a`
`RunCatalogAreaMeasurementAndExit` on host S050, seed 130365, en → `[SkirmishAreaMeasurement] catalog=S050 result=Completed`.
FrontlineA/B/C 1824/2139/1936 m2, all inside playable and reachable. Routes MainHighway 223 m, FlankNorthRuins 292 m, FlankSouthSweep 322 m (flood); every segment flood `Reached`, no failed native segment. (Native path metres use the same small scale as the accepted S031 r2, so flood lengths are quoted.)
`Design/AgentReports/SkirmishExpansion/S055/area-measurement-r2.json` committed (LF, `eol=lf`); S055–S060 `area_measurement.artifact` = that file. `check_draft.py` 6/6 PASS. quickcheck not run (no .NET 8 SDK on Windows).

## 3. W6 steps b–d (`ff81cb84a` → assets `25c4b2cf5`)
- b. `pad_preflight.py --draft <draft> --dump LayoutDumps/S050/dump.json`: PREFLIGHT PASS 6/6.
- b. `AuthorFromDraftList` → `[SkirmishMissionAuthoringBatch] result=Completed drafts=6 passed=6 failed=0`; each `[SkirmishMissionAuthoring] result=Passed catalog=S05x/S060 source=opmap.ch05.citywide_alert variants=12 publication=InProgress library=Planned readiness=0`.
- c. `RunAuthoredLayoutValidation` → `[SkirmishInstancePadContainment] catalog=S0xx result=Passed failures=0` for all six (rows 168 field / 256 established).
- d. One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; log `win-dev-unity\w8\menu\unity.log`). The queue prints `class=counted`, but `--no-table` writes no result row; Windows runs are not counted.

| Row | Menu journey | expandedStartupFailure | Result | Reason | match s | deadline % | zones own/opp | tickets own/opp |
|---|---|---|---|---|---|---|---|---|
| S055 ground field | Passed | none | Victory | TicketsDepleted | 617.2 | 51.43 | 2/0 | 500/0 |
| S056 ground established | Passed | none | **Defeat** | TicketsDepleted | 593.1 | 49.43 | 0/3 | 0/461 |
| S057 air field | Passed | none | Victory | TicketsDepleted | 744.9 | 62.07 | 2/0 | 500/0 |
| S058 air established | Passed | none | **Defeat** | TicketsDepleted | 601.9 | 50.16 | 0/3 | 0/476 |
| S059 combined field | Passed | none | Victory | TicketsDepleted | 836.5 | 69.71 | 2/0 | 500/0 |
| S060 combined established | Passed | none | **Defeat** | TicketsDepleted | 608.9 | 50.74 | 0/2 | 0/456 |

All six returned to the result screen (`result_return_ok=yes`, `[SkirmishTerminalFrozen] result=Passed`). Harness evidence (189 MB under `Design/AgentReports/SkirmishExpansion/S05x/_Evidence`) moved to `win-dev-unity\w8\harness-evidence`, not committed.

## 4. Integration merge `096ab45ce`
Conflicts only in the two shared registries, `SkirmishPublicationManifest.asset` and `SkirmishExpansionCatalog.asset`: integration appended W7's S031–S036, this branch appended S055–S060. Resolved as the union (S031–S036 first, then S055–S060). Against integration the branch is now pure additions (70 files). The merged registries have not been re-run in Unity on Windows yet.

## Open
- The three Established Base rows lose on frontline tickets: by ~50% of the deadline the enemy holds every zone and ARIA none. This matches the established-FC losses on S008/S010/S012 (pending W16), so it is not treated as a W8 draft defect; one Windows launch per row only.
- ARIA used no aircraft on the air/combined rows (S057–S060 `unused` lists `building.airport` and the air roles).
- Mac quickcheck and counted runs pending.
