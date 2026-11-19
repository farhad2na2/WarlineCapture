# Windows dev results — W31b objective clocks use the per-mission deadline (2026-11-19)

Branch `missions/win-w31b-reducer-deadline`, built on `missions/win-w31-pacing-ce-bt` (the W31 data and re-authored assets). The Windows dev lead did this task. Shared code, under the owner's standing OK given in this Windows session (2026-11-19): "Standing OK for tested shared code, tool and map work from the program lead's queue; no unit-stat or game-rule changes." Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `2fa3f170a`. Logs under `win-dev-unity\w31b-utf\` and `w31b-menu\`.

**Outcome: accepted.**
- All 12 rows win under their new deadlines: DB CE 420 s, CC BT 540 s.
- The wins land at **29.8–43.8 %** of the deadline, inside the 25–50 % band. Every row returns to the result screen.
- No other mission changes.

## 1. Code change `2fa3f170a`
- **Reducers.** `SkirmishConvoyReducer.Create`, `SkirmishBreakthroughReducer.Create` and `SkirmishFrontlineReducer.Create` take an optional `int deadlineSeconds = 0`. A positive value replaces the size default (CE/BT 1080/1500/1800; FC 1200/1500/1800); 0 keeps the default; a negative value throws.
- **Callers.**
  - `SkirmishNativeObjectiveLifecycleHooks.TryAttachAfterActualSpawn` passes `setup.DeadlineSeconds` from the session's `SkirmishResolvedSetupRecord`.
  - The three checkpoint baselines in `SkirmishNativeObjectiveCheckpoint` pass `setup.DeadlineSeconds`, so restore validation compares like with like.
- **No other mission changes.** Every FC/BT/CE matrix row other than the 12 W31 rows already equals its size default: FC 90, BT 72 and CE 72 rows checked. Their clocks are bit-identical.
- **What doesn't change.** Base Assault, unit stats and game rules are untouched.

## 2. EditMode tests
`-runTests -testPlatform EditMode` through `InvokeUnity.ps1` (`w31b-utf\results.xml`). The filter covered the three reducer suites, `SkirmishNativeObjectiveCheckpointTests` and `SkirmishCombinedArmsTests`.
- **Result: 64/64 Passed.**
- New tests (also registered in each suite's `RunFocusedValidation`; counts 14→15, 15→16, 13→14): `PerMissionDeadlineReplacesSizeDefaultAndZeroKeepsIt`.
  - **Convoy:** 420/583/700 s; 0 keeps the default; −1 throws; and a 420 s clock ends with `ObjectiveDeadline`.
  - **Breakthrough:** 540/750/900 s; 0 and 1080 give 1080; −1 throws.
  - **Frontline:** 900 s; 0 and 1200 give 1200; −1 throws.
- The existing 1080/1500/1800 and 1200 assertions are unchanged and pass.

## 3. Menu runs (NON-counted; `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, seed 130365, en; `w31b-menu\menu\unity.log`)

| Row | Result | match s | deadline (game) | % of deadline | W31 (1080 s clock) |
|---|---|---|---|---|---|
| S019 CE ground field | Victory ConvoyDelivered | 146.5 | 420 | **34.87** | 13.87 % |
| S020 CE ground established | Victory | 148.1 | 420 | **35.26** | 13.91 % |
| S021 CE air field | Victory | 128.6 | 420 | **30.63** | 13.66 % |
| S022 CE air established | Victory | 162.0 | 420 | **38.56** | 14.55 % |
| S023 CE combined field | Victory | 125.3 | 420 | **29.84** | 11.99 % |
| S024 CE combined established | Victory | 155.3 | 420 | **36.97** | 15.01 % |
| S037 BT ground field | Victory BreakthroughEvacuated | 212.5 | 540 | **39.35** | 20.00 % |
| S038 BT ground established | Victory | 163.2 | 540 | **30.23** | 16.44 % |
| S039 BT air field | Victory | 217.7 | 540 | **40.31** | 20.34 % |
| S040 BT air established | Victory | 236.7 | 540 | **43.83** | 16.75 % |
| S041 BT combined field | Victory | 219.0 | 540 | **40.56** | 19.91 % |
| S042 BT combined established | Victory | 182.8 | 540 | **33.85** | 16.40 % |

- Every row: `result_return_ok=yes`, `expandedStartupFailure` 0, menu journey Passed 12/12. No TimeLimit or deadline losses.
- Harness evidence (124 MB) moved to `w31b-menu\harness-evidence`, not committed.
- **The runner's percentage is stale.** The runner's own `deadline_pct=` column still divides by `missions.csv` `deadline_s`, which is 1080 for these rows, so it prints 11.6–21.9 %. The game-side `[SkirmishTerminalObjective] deadline=` and `deadlinePct=` (table above) use the real clock. **`missions.csv` needs `deadline_s` 420 (S019–S024) and 540 (S037–S042).** That is the lead's file; I did not edit it.

## Open
- `missions.csv` `deadline_s` for the 12 rows (lead).
- Mac counted runs pending.
