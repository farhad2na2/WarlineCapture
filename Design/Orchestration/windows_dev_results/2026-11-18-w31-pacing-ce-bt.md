# Windows dev results — W31 per-family deadlines, DB CE S019–S024 and CC BT S037–S042 (2026-11-19)

Branch `missions/win-w31-pacing-ce-bt` (from integration `2441e8270`). The W31 brief comes from the lead's commit `ec3a948d4` (owner decision 2 of 2026-11-19). The Windows dev lead did this task. Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\w31-db-ce\` and `w31-cc-bt\`.

**Outcome: the data change is done but has no effect in play, so it is not accepted.** The CE and BT match clocks ignore the authored deadline. Both objective reducers hard-code the deadline by map size, which is shared runtime code. Per the brief ("No shared code"), I stopped. The fix needs the owner's OK in this session.

## 1. Data change `2aad1a83e`
`deadline_seconds` changed in both setup-matrix copies, which are byte-identical: `Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv` and `Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv`. 36 rows each.

| Family | Standard | War | LargeWar |
|---|---|---|---|
| DB CE S019–S024 | 1080 → **420** | 1500 → **583** | 1800 → **700** |
| CC BT S037–S042 | 1080 → **540** | 1500 → **750** | 1800 → **900** |

- **Intended improvement:** a clock that actually pressures the escort or breakthrough. Mac counted wins landed at 11–21 % of 1080 s, under the 20 % gate-2 floor.
- **Nothing else changed.**
  - No other row changed, and each row's objective (CE/BT) was checked.
  - No test pins these rows. `SkirmishCombinedArmsTests.cs:63` pins S005 only. The fixture at `SkirmishMissionLogicalBindingSubjectTestFixture.cs:38` builds its own asset.
- **Historical hash pins.** The 2026-10-15 authoring-input snapshots under `Design/AgentReports/SkirmishExpansion/Full120/LogicalInputs/20261015-01/` pin the old matrix sha256 `b28cfe04…`. They are historical records and were left unchanged.

## 2. Re-authoring and runs
- **DB CE** (`2aad1a83e` → assets `8f6a91ca2`): preflight 6/6, authoring 6/6, containment 6/6.
- **CC BT** (→ assets `98c426ef5`): preflight 6/6, authoring 6/6, containment 6/6.
- The per-mission `SkirmishObjective_SNNN.asset` files now hold the new values (e.g. S019 420/583/700, S037 540/750/900), and each `SkirmishScenario_SNNN.asset` references its own objective asset (`objectiveConfig` guid).
- **Menu runs.** One NON-counted run per row, 130365 en, menu journey Passed 12/12. All 12 won, but **the logged clock deadline is still `deadline=1080.0`** on every row:

| Rows | Match s | % of 1080 (logged) | % of the new deadline |
|---|---|---|---|
| S019–S024 | 129.5–162.1 | 11.99–15.01 | 30.8–38.6 % of 420 |
| S037–S042 | 177.1–219.6 | 16.40–20.34 | 32.8–40.7 % of 540 |

The last column is what the win times would be against the new deadlines if the clock honoured them. It is not measured, because the clock didn't change.

## 3. Why the new deadline is ignored
- **CE:** `SkirmishConvoyReducer.Create` (`Assets/Game/Scripts/Runtime/Skirmish/SkirmishConvoyReducer.cs:65-71`) sets the deadline from the size alone (Standard 1080, War 1500, LargeWar 1800).
- **BT:** `SkirmishBreakthroughReducer.Create` (`SkirmishBreakthroughReducer.cs:75-85`) does the same.
- **Callers:** `SkirmishNativeObjectiveLifecycleHooks.cs:42/44` and `SkirmishNativeObjectiveCheckpoint.cs:179/200` pass only `SizeId`, not `setup.DeadlineSeconds`.
- **So the data is ignored.** `setup.DeadlineSeconds` (`SkirmishSetupCompiler.cs:275`, from `definition.ObjectiveConfig`) has the new values, but the CE/BT clock never reads it.
- **The fix (shared code, not done).** Pass `setup.DeadlineSeconds` into both `Create` calls (lifecycle hooks and checkpoint baseline), and update the reducer tests that assert `T(1080)` (`SkirmishConvoyReducerTests.cs:27`, `SkirmishBreakthroughReducerTests.cs:31`).
- **Check for FC (not done).** `SkirmishFrontline*` may have the same pattern.

Harness evidence (20 MB, 105 MB) moved to each run's `harness-evidence` folder, not committed.

## Open
- **Shared-code fix for the CE/BT reducer deadline** (owner's OK in this session). Then rerun the 12 rows.
- **Do not merge this branch as accepted pacing.** Its data is consistent, but it has no effect until the reducer fix lands.
