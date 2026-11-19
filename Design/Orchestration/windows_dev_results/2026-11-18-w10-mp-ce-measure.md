# Windows dev results — W10 Mountain Pass Convoy Escort S067–S072 measure → author → menu (2026-11-19)

Branch `missions/win-w10-mp-ce`, continuing the drafts stage in `2026-11-18-w10-mp-ce.md`; merged with integration (`cba7909b0`).
The Windows dev lead made the fixes and commits after the Codex drafts stage.
Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\measure-S067-r2` and `win-dev-unity\w10-ce\`.

## 1. Routes `3bad99cdc`
Three vehicle routes reuse the W8 S055 r2 chains on host S050.

## 2. Defense pads off the road band `83088c4cc`
Same fix as BT (a/b swapped in CE): defense.approach.a → (1152,446), .b → (1209.5,446); bitmap flag 0 under the 3×4 tower, no instance-pad overlap (clear of the f2 satellite dish at (1173,447)).

## 3. r2 and binding `ee7d0cb19`
`RunCatalogAreaMeasurementAndExit`, host S050, seed 130365, en → `result=Completed`.
- ConvoyDestination 704 m2 inside playable, fully vehicle-reachable.
- convoy.origin.1–3 and defense.approach.a/b fit.
- Routes 223 / 292 / 322 m (flood), no failed segment.

`Design/AgentReports/SkirmishExpansion/S067/area-measurement-r2.json` committed (LF); S067–S072 bind it. `check_draft.py` 6/6 PASS.

## 4. Pipeline (`ee7d0cb19` → assets `89114e248`)
- `pad_preflight.py` 6/6 PASS. `AuthorFromDraftList` → `result=Completed drafts=6 passed=6 failed=0`. Containment `result=Passed failures=0` 6/6 (rows 192 field / 280 established).
- One NON-counted menu launch each (`run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; log `win-dev-unity\w10-ce\menu\unity.log`).

| Row | Menu journey | Result | Reason | match s | deadline % | delivered/lost |
|---|---|---|---|---|---|---|
| S067 ground field | Passed | **Defeat** | ConvoyImpossible | 94.0 | 8.70 | 0/2, 2 |
| S068 ground established | Passed | **Defeat** | ConvoyImpossible | 110.4 | 10.22 | 0/2, 2 |
| S069 air field | Passed | **Defeat** | ConvoyImpossible | 87.3 | 8.09 | 0/2, 2 |
| S070 air established | Passed | **Abort** | expandedStartupFailure=BlockedSpawn | 0 | 0 | — |
| S071 combined field | Passed | **Defeat** | ConvoyImpossible | 72.2 | 6.69 | 0/2, 2 |
| S072 combined established | Passed | **Abort** | expandedStartupFailure=BlockedSpawn | 0 | 0 | — |

The four defeats returned to the result screen (`result_return_ok=yes`); the aborts are `unproven`. Harness evidence (52 MB) moved to `win-dev-unity\w10-ce\harness-evidence`, not committed.

## 5. Integration merge `cba7909b0`
Conflicts only in the two shared registries; resolved as the union (integration first, then S067–S072). Not re-run in Unity.

## Open
- **Convoy destination at the enemy base (draft geometry defect).** ConvoyDestination (1186..1202, 354..398) is about 2 m from the enemy barracks pad (1218,387.5), about 6 m from the enemy staging pad and 5 m from `rally.enemy`. Trucks are hit on arrival (first hit on the Cargo Truck at cell (332,76) ≈ world (1192,376), inside the destination) and both are lost by 72–110 s. Same class as the BT exit problem (W23). Moving the destination needs a new measurement; not done here.
- **S070/S072 BlockedSpawn.** All 13 enemy buildings placed; then `[SkirmishStartingForce] failed prefab=Unit_Veh_Helicopter_Transport faction=2 member=0`. The enemy helipad (s052.f2.helipad, AirReturn (1261,467.5) 16×15) is identical in S058 (W8 FC) and S064/S066 (BT), which use the same army profile and start package and launched; CE S046/S048 on City Crossroads spawn the same helicopter. The layout-dump bitmap is clear at the pad centre (a scenery block sits at its north-east corner, x 1268–1276, z 468–476). Root cause not found from logs or code reading; the shared spawn code was not changed. Needs a startup-allocator capture for S070 (the existing capture is S002-only).
- Air/combined rows have no measured Airport position. Mac quickcheck and counted runs pending.
