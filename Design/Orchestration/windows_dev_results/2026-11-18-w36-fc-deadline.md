# Windows dev results — W36 FC deadline data, DB FC S007–S012 and CC FC S031–S036 (2026-11-19)

Branch `missions/win-w36-fc-deadline`, from integration `c11d6172a`. Data only, plus re-authored assets. The Windows dev lead did this task.
- **Lead decision:** FC yes; BT stays at 540 s.
- **Unity:** Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits below. Logs under `win-dev-unity\w36-fc-deadline\`.

**Outcome: data and authoring done.** Not run in play on this branch. The game-side clock already honours per-mission deadlines (W31b).

## 1. Data `8e9bdffc9`
`deadline_seconds` changed in both setup-matrix copies, which stay byte-identical: `Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv` and `Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv`. 36 rows each.

| Rows | Standard | War | LargeWar |
|---|---|---|---|
| DB FC S007–S012, CC FC S031–S036 | 1200 → **1690** | 1500 → **2113** | 1800 → **2535** |

- **Source:** the W36 sweep (`missions/win-pacing-stack-sweep` `f899f77ed`). FC median was 56.3 % of 1200 s (676 s, 5 runs @130365); 676 / 0.40 ≈ 1690. War and LargeWar are scaled by 1500/1200 and 1800/1200, rounded half up.
- **Not changed:**
  - MP FC S055–S060 stays at 1200 (W16).
  - S079–S084 and S103–S108 have no drafts.
  - BT S037–S042 stays at 540/750/900.
  - DB CE stays at 525/729/875.
- **What it does.** FC still ends on tickets: 500 each, drained at 1 ticket/s while the other side holds two zones. The deadline is the `TicketsAtDeadline` timeout. ARIA's ticket wins don't get faster; the player gets more time.
- **Tests.** No test pins these rows. The S031/S007 fixtures build their own objective assets with 1200.

## 2. Authoring and containment, assets `6c239fe4b`
- **AuthorFromDraftList:** `[SkirmishMissionAuthoringBatch] result=Completed drafts=12 passed=12 failed=0`.
- **`RunAuthoredLayoutValidation`:** Passed failures=0 for all 12 rows (168 rows on field, 256 on established).
- **Objective assets:** every `SkirmishObjective_SNNN.asset` holds `standardDeadlineSeconds: 1690`, `warDeadlineSeconds: 2113`, `largeWarDeadlineSeconds: 2535`.
- **Binding assets:** the `SkirmishMissionLogicalBinding_SNNN.asset` diffs are regenerated reference ids only.

## Earlier attempt (not counted)
A first authoring run that also carried BT 670 s was stopped before it wrote anything, when the lead decided against BT. Its verified Unity tree was stopped. Logs are kept in `w36-deadline-data-stopped-bt-dropped\`.

## Open
- Merge after Mac lane 46 finishes counting FC S009/S031/S033 at 1200 (lead).
- `missions.csv` `deadline_s` for these 12 rows (lead's file).
