# Windows dev results — W31d DB CE S019–S024 family deadline 525 s (2026-11-19)

Branch `missions/win-w31d-db-ce-deadline` (from integration `65b1808de`). The Windows dev lead did this task. Data only; no code change. Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`. Log: `win-dev-unity\w31d-db-ce\`.

**Outcome: done.** Authoring 6/6, containment 6/6. In the check runs, all six rows win at 33.4–45.2 % of the new 525 s deadline.

## Data `9100a406c` → assets `4ca084089`
- `deadline_seconds` in both InitialSetupMatrix copies (byte-identical), 18 rows each, S019–S024 (CE). No other row changed.
  - Standard 420 → **525**
  - War 583 → **729**
  - LargeWar 700 → **875**
- **Why:** the W32 sweep (`2026-11-18-w32-db-ce-pacing-sweep.md`, 12 runs) won at 41.7–57.8 % of 420 s, median 50.1 %. One value per family, as the owner decided.
- **Checks:** `AuthorFromDraftList` → `result=Completed drafts=6 passed=6 failed=0`. Containment `result=Passed failures=0` 6/6 (rows 192/280). Preflight 6/6.

## Check runs (NON-counted, Standard Regular, seed 130365, en; game `deadline=525`)

| Row | Result | match s | % of 525 |
|---|---|---|---|
| S019 | Victory | 190.6 | 36.30 |
| S020 | Victory | 237.4 | 45.22 |
| S021 | Victory | 175.1 | 33.35 |
| S022 | Victory | 229.9 | 43.79 |
| S023 | Victory | 181.2 | 34.51 |
| S024 | Victory | 234.9 | 44.74 |

- Every row returns to the result screen (`result_return_ok=yes`); `expandedStartupFailure` 0.
- Harness evidence (27 MB) moved to `w31d-db-ce\harness-evidence`, not committed.
- `missions.csv` `deadline_s` needs 525 for S019–S024 (lead).
