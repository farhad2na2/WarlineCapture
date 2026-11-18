# Windows dev results — W13 runway building placement (2026-11-18)

Branch `missions/win-w13-runway-placement`: W13 commit `1c9c1b109` (cut from `acdec4331`), merged with integration `7970b1c54` (`fa3eb3130`) and `e2a823d63` (`00f5c38a0`). Owner approved this shared-code task in the Windows session.
Codex gpt-6.2-sol xhigh; brief, transcript and report: `D:\Projects\WarlineCapture-program-logs\2026-11-18\codex\w13-runway-placement*`. The worker made no commits; the Windows dev lead reviewed and committed.

## Change
- `BuildingPlacementAdapterCompositionSystemHelper.cs`: shared placement validity (starting grants and player/enemy construction) also requires every baked MapSurface cell under the effective footprint to carry `MapSurfaceMovementMask.BuildingPlacement`. No baked surface data, or cells outside it → unchanged behaviour. The existing nearest-legal search then relocates.
- `BuildingRuntimeQueryCompositionSystemHelper.cs` ~:135: overlap with a placed airport uses its runway projected with the airport's quarter turn (model size was already stored rotated).
- `SkirmishStartingBuildingsService.cs`: one `[SkirmishStartingBuilding] placed catalog= faction= structure= preferred=(x,z) actual=(x,z)` line per starting structure.
- `Tools/Missions/pad_preflight.py --surface <decoded primary-surface JSON>`: reports preferred/best-origin cells without BuildingPlacement; without it, says the bit was not checked.
- Tests: `BuildingPlacementOriginSearchTests` + `S004EnemyRefinerySearchRespectsBakedBuildingPlacement(true/false)`, `PlacedAirportOverlapUsesRotatedRunwayAndStoredModelDimensions`; added to `BuildingFootprintReadinessValidation.Run` (marker now `catalog=25 geometry=7 spawn=3`).

## Checks (Windows, Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `fa3eb3130`)
- `python -m unittest test_pad_preflight` (from Tools/Missions): 15 tests OK, 1 skipped (Mac-history calibration class).
- UTF EditMode `-testFilter "BuildingPlacementOriginSearchTests|Building"`: W13 584 tests, 578 pass / 6 fail; base `7970b1c54` 581 tests, 575 pass / the same 6 fail. All 13 `BuildingPlacementOriginSearchTests` pass, including the 3 new ones. The 6 failures predate W13: CanonicalBarracksRuntimeAndCatalogFootprintsFitTheM2Lot, 3× BuildingProductionQueueCompositionSystemHelperTests, SkirmishStartingBuildingsTests.StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce (`componentMissing`), GroundMissileLauncherRuntimeTests.AttackOrder... (logs `win-dev-unity\w13-utf`, `w13-utf-base-7970b1c54`).
- `BuildingFootprintReadinessValidation.Run`: **FAILED on W13 and on base `7970b1c54` alike**, at its first step `BuildingFootprintAudit.Run`: `EvidenceChain_Clinic: footprint must enclose the model with less than one cell of rounding`. The W13 marker cannot be reached on Windows until that audit failure is fixed (models are present; only 3 menu PNGs are LFS pointers in this checkout). Logs `w13-footprint`, `w13-footprint-base-7970b1c54`.
- Not run after the `e2a823d63` merge (it adds W15 only; no W13 file changed).

## Starting-structure moves (startup-only launches, `WARLINE_SKIRMISH_STARTUP_ONLY=1`, `RunFocusedAriaAndExit`, Standard, seed 130365; S004 130367)
W13 launches on S004 and the 12 audit rows (S006, S008–S024 even, S026, S050), then a local-only comparison build `09adb89de` (= `7970b1c54` + only the log line) on every row where W13 moved anything by ≥1.4 m (S004, S006, S014). Logs `win-dev-unity\w13-startup-w13\<id>`, `w13-startup-base\<id>`.

| Row | Structure (faction) | preferred | base actual | W13 actual | W13 vs base |
|---|---|---|---|---|---|
| S004 | refinery (2) | (894,562) | (888,556) — on the runway | (914,579) | 34.7 m |
| S004 | fuel_bladder (2) | (917,588) | (917,588) | (900,579) | 19.2 m |
| S004 | ground_staging (2) | (860,537) | (860,537) | (852,529) | 11.3 m |
| S004 | barracks/HQ (2) | (888,533) | (885,530) | (885,526) | 4.0 m |

Everything else is identical on base and W13: S004's other 14 structures (player base moves of 1.4–32.7 m from preferred already happen on base), S006's two fabrication depots (~7 m from preferred on both), and the S014–S024 enemy `watchtower.defense_approach_b` (1.4 m on both). S008, S010, S012, S026, S050: no structure leaves its preferred origin.
Runway (x 490.66..909.34, z 543.35..576.65): the W13 refinery rect starts at x 914 / z 579, the barracks ends at z 541.

**Re-runs:** of the rows with Mac match evidence, only **S004** starts differently (enemy base). S008–S012 and S020/S022/S024 are unchanged by W13.

## Notes
- S050's W13 startup launch logged all 20 placements, then exited 2 with `[SkirmishS003Startup] result=Failed reason=candidateSourceChangedDuringRun`; I had created a local branch in another worktree during that run. Its placement lines are kept; no other row was affected.
- Counted S004 matches stay on the Mac.
