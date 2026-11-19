# Windows dev results — W31c City Crossroads CE S043–S048 per-family deadline (2026-11-19)

Branch `missions/win-w31b-reducer-deadline` (W31c sits on top of W31b's reducer fix, which is merged in integration `391d02195`). The Windows dev lead did this task. Data only. Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `f3706fef8`. Log: `win-dev-unity\w31c-cc-ce\`.

**Outcome: the deadline works. The three established rows are accepted; the three field rows are not, because ARIA stops itself.**
- The game clock now runs on 450 s.
- S044, S046 and S048 win at 27.1–29.5 % of the deadline.
- S043, S045 and S047 end with ARIA stopping itself (`[AriaBlocked] reason=101 intent=ObserveObjective`). This is W27 cause 3 (the convoy-service handback blanks the objective frame), which the lead reported from Mac lane 44.

## 1. Data `f3706fef8` → assets `88d37b4f2`
`deadline_seconds` in both matrix copies (byte-identical), 18 rows each, S043–S048 (CE):
- Standard 1080 → **450**
- War 1500 → **625**
- LargeWar 1800 → **750**

Authoring 6/6, containment 6/6, preflight 6/6. No other row changed.

## 2. Menu runs (NON-counted, Standard Regular, seed 130365, en)

| Row | Result | match s | deadline (game) | % of deadline | Note |
|---|---|---|---|---|---|
| S043 CE ground field | **Abort** `ariaStopped:Blocked` | 213.3 | 450 | 47.40 | 101 ObserveObjective |
| S044 CE ground established | **Victory** ConvoyDelivered | 121.9 | 450 | **27.09** | return yes |
| S045 CE air field | **Abort** `ariaStopped:Blocked` | 264.2 | 450 | 58.71 | 101 ObserveObjective |
| S046 CE air established | **Victory** | 132.6 | 450 | **29.46** | return yes |
| S047 CE combined field | **Abort** `ariaStopped:Blocked` | 357.1 | 450 | 79.35 | 101 ObserveObjective |
| S048 CE combined established | **Victory** | 132.6 | 450 | **29.47** | return yes |

- `expandedStartupFailure` 0. Menu journey Passed 6/6.
- Harness evidence (26 MB) moved to `w31c-cc-ce\harness-evidence`, not committed.
- **The runner's percentage is stale.** The runner's `deadline_pct` column divides by `missions.csv` `deadline_s` (still 1080 for these rows). It needs **450** for S043–S048 (lead).

## Open
- **S043/S045/S047:** W27 cause 3 (`win-dev-unity\w27-aria-stops` cause list). Fix queued after W27 cause 1.
- **`missions.csv` `deadline_s`** 450 for S043–S048 (lead).
- **Pacing check.** The established wins (27–29 %) are near the low end of the 25–50 % band.
