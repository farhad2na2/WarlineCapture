# Windows dev results — W32b transport helicopter escort (gate 3) (2026-11-19)

Branch `missions/win-w32b-heli-escort` (from integration `65b1808de`, which includes W32). The Windows dev lead did this task. Shared code, under the owner's standing OK given in this Windows session (2026-11-19): "Standing OK for tested shared code, tool and map work from the program lead's queue; no unit-stat or game-rule changes." Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `b40f41cf6`. Logs under `win-dev-unity\w32b-utf\` and `w32b-menu\`.

**Outcome:**
- **Accepted for gate 3.** The transport heli is now used on every row run (S022 ×2, S024, S042), and gate 3 passes on all of them.
- **Pacing is at the top of the band.**
  - DB CE established is about 48–52 % of the new 525 s deadline.
  - CC BT S042 is at 50.5 % of 540 s, against 33.9 % before W32 (W31b run).

## 1. Cause and change `b40f41cf6`
- **Cause.** `UiShellEcsGateway.PublicObjective.cs` builds the public own-group rows. It set `CanMove` only for ground domains (infantry, ground, logistics, convoy trucks). The `TransportHeli` (Air domain) therefore had `CanMove=false`, and `AriaObjectiveEscortPolicy.Eligible` (which requires `CanMove`) never offered it. W32 had made it eligible by role, but this row flag still excluded it.
- **Change.** An own `TransportHeli` group gets `CanMove` (`airMove`), never `CanHold`.
  - The objective chooser still never picks it; it only takes combat infantry on FC/BT and trucks on CE.
  - The convoy service needs trucks.
  - So the heli is only an escort candidate.
- No gate, unit stat or game rule changed.

## 2. EditMode tests (`w32b-utf`)
- Suites: AriaObjectiveCoverage, AriaObjectiveSkill, SkirmishExpandedAria, MatchHudAssistantUiSystemHelper, SkirmishConvoyServiceJourney and AriaAircraftService.
- **Result: 139/139 Passed.**
- `StartingGroupsOfEveryRoleAreEscortCandidates` now also checks the gateway rule.

## 3. Menu runs (NON-counted, Standard Regular, en) and `gate3_objective.py --summary`
This branch is pre-W31d, so DB CE still ran at 420 s.

| Row | Seed | Result | match s | % of game deadline | % of 525 s (W31d) | Gate 3 | transport_heli used |
|---|---|---|---|---|---|---|---|
| S022 DB CE air established | 130365 | Victory | 271.1 | 64.54 (of 420) | 51.6 | PASS | yes |
| S022 DB CE air established | 155923 | Victory | 252.7 | 60.17 (of 420) | 48.1 | PASS | yes |
| S024 DB CE combined established | 130365 | Victory | 253.2 | 60.29 (of 420) | 48.2 | PASS | yes |
| S042 CC BT combined established | 130365 | Victory | 272.6 | 50.47 (of 540) | — | PASS | yes |

- **Before W32b** (`w32-pacing-db-ce`): S022/S024 failed gate 3 (transport_heli unused) and won in 238–243 s. The heli escort adds about 10–30 s.
- **S042:** W31b won in 182.8 s (33.85 % of 540). With W32 and W32b it takes 272.6 s (50.47 %).
- No `[AriaBlocked]` or abort lines. Every run returned to the result screen. `roster=match`.
- Harness evidence (26 MB) moved to `w32b-menu\harness-evidence`, not committed.

## Open
- **CC BT pacing after W32/W32b.** S042 is at 50.5 %; S037–S042 are not swept. Suggest a W32-build sweep like the DB CE one.
- **DB CE established rows** sit at about 48–52 % of 525 s, at the band's top edge.
- **S047 service detour** (`2026-11-18-w32-db-ce-pacing-sweep.md` section 4): a candidate cause, not changed.
