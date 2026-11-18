# Lane integration 35 — 2026-11-18

Brief: `WarlineCapture-program-logs/briefs/lane-integration35-2026-11-18.md`. Logs: `WarlineCapture-program-logs/2026-11-18/lane35/` (L).
HEAD at start e2a823d63 (W15 merge deed06125). Wrapper only, GUI licensing, one Editor at a time, disk_guard 0 before each launch,
`Library Redirect Path` within 3 min on every launch. No asset or code changes; missions.csv not touched (`--no-table`).

## Step 1 — compile and Python
- Compile: exit 0, `L/compile1.log:936` `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed`, 0 `error CS`, Editor 3 min 27 s (W15 recompile).
- Python: `Ran 150 tests`, `OK` (`L/python.txt:33-35`).

## Step 2 — W15 focused test
- `Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation`, no -quit: `L/expanded.log:973` `[SkirmishS002AriaHarnessTests] result=Passed`,
  `:985` `[SkirmishExpandedAriaTests] result=Passed`, 0 `error CS`. Editor stopped by own verified PID 95820 at 198 s (`L/expanded.stop.txt`, wrapper rc=143).
- So W15 (defender test) and W15b (BootPlayingSession EditMode gate) both hold on the Mac.

## Step 3 — BT S014-S018, CE S021/S023 first Mac matches (seed 130365, en)
`L/q.out.txt`, `L/q/unity.log`; `--menu-journey --validation-library` (:611 enabled=1) `--no-table --timeout 12000`.
`[SkirmishAriaQueue] result=Completed jobs=7 recorded=7 wins=7 sourceUnchanged=1 exitCode=0` (:55632).

| row | result | clock | deadline | menu journey | probe |
|---|---|---|---|---|---|
| S014 BT | Victory BreakthroughEvacuated 8/8 lost 0 | 338.4 s | 31.34% (:17245) | Passed :1960 | Passed :17553 |
| S015 BT | Victory BreakthroughEvacuated 8/8 lost 0 | 303.5 s | 28.10% (:23548) | Passed :18255 | Passed :23840 |
| S016 BT | Victory BreakthroughEvacuated 8/8 lost 0 | 267.4 s | 24.76% (:30946) | Passed :24517 | Passed :31254 |
| S017 BT | Victory BreakthroughEvacuated 8/8 lost 0 | 305.4 s | 28.28% (:36897) | Passed :32012 | Passed :37151 |
| S018 BT | Victory BreakthroughEvacuated 8/8 lost 0 | 318.5 s | 29.49% (:45158) | Passed :37980 | Passed :45448 |
| S021 CE | Victory ConvoyDelivered 2/2 lost 0 | 124.8 s | 11.56% (:50097) | Passed :46277 | Passed :50370 |
| S023 CE | Victory ConvoyDelivered 2/2 lost 0 | 136.2 s | 12.61% (:55085) | Passed :51177 | Passed :55339 |

- Startup (W15b live path): 0 startupFailure, 0 FailureCode, 0 MissingReference, 0 BlockedSpawn, 0 `[SkirmishStartingForce] failed`,
  0 `[StartingGrantPlacement] failed`, 0 `[AriaBlocked]`/`[AriaBlockedControl]`, 0 `error CS` in the whole log.
- Menu path `Hotspot>ObjectiveTab_BT|CE>BattleCard_<id>>StartBattleButton`; every probe `frozenResultAnd:MAIN MENU interventions=0`.
- Gate-3 unused (`[SkirmishGateUnits] event=used`): S014 (:17231) apc_armored, apc_fast, apc_heavy, car, drone, marksman, radar, siege, tank,
  transport_heli. S015 (:23534) 17 incl. airport, helipad, satellite_dish. S016 (:30932) 12 incl. airport. S017 (:36883) 20 incl. airport.
  S018 (:45144) 15 incl. airport. S021 (:50083) 17 incl. airport, rifle. S023 (:55071) 20 incl. airport. ARIA used 4-7 roles per BT row
  (always rifle, rocketeer, tanker) and breacher, convoy_logic, gunner, rocketeer, tanker on CE rows.
- ARIA objective choices: BT rows Move OpenObjectiveCorridor/EvacuateOriginals plus Hold on S015/S016/S017; CE rows 2 Hold + 3 Move EscortOriginalTruck.
- runs.csv (countedWin=1 each): `<ID>/_Evidence/candidates/071ed527.../runs.csv` for the 7 rows, committed; the evidence folders are untracked.
  Whether these count is the lead's call; rows stay planned. One Burst BC1016 line (W17) appears in the log, as in earlier lanes.

## Open
- Single seed per row only; second/third seeds and fa-IR not run. Airport/helipad never used by ARIA on any of these rows.
- W13 (runway placement) reviewed by the lead but not merged at the time of this lane, so it was not exercised here.
