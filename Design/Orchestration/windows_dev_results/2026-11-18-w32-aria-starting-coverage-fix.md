# Windows dev results — W32 fix: ARIA gives every starting group a role in the objective (2026-11-19)

Branch `missions/win-w32-aria-starting-coverage`. It contains the W27 cause 1+3 fixes (`missions/win-w27-aria-stop-fixes`), and it is merged with integration `e8e491d90`. The read-only diagnosis is in `2026-11-18-w32-aria-starting-coverage.md`. The Windows dev lead did this task. Shared ARIA code, under the owner's standing OK given in this Windows session (2026-11-19): "Standing OK for tested shared code, tool and map work from the program lead's queue; no unit-stat or game-rule changes." Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`. Logs under `win-dev-unity\w32-utf\`, `w32-menu\` and `w32-baseline-w27\`.

**Outcome:**
- **Gate 3 passes on every row run: 5/5** (S014, S020, S032, S043, S047), against 0/3 on the pre-W32 baseline.
- **All three briefed rows still win.**
- **The pacing changed.**
  - S020 CE got slower: 36 → **58 %** of its 420 s deadline, above the 50 % band.
  - S032 FC got faster: 69 → 54 % of its 1200 s.
  - S014 BT is about the same: 29 → 34 %.
- **Not accepted as same-pacing for S020.** Lead's call.

## 1. Change `1152a9c72` (no gate, unit stat or game rule changed)
1. **Escort turn.** `AriaSkirmishPlanSystem.ObjectiveSkill.cs`: one `StepObjectiveEscort` turn after each accepted objective order (`plan.ObjectiveEscortTurn`, set where `ObjectiveCoverageTurn` is set), before support and coverage. FC's Hold re-issues used to leave escorts no idle frame.
2. **Offer expiry on attempt time.** `MatchHudAssistantUiSystemHelper.ObjectiveSkill.cs`: the escort offer clock runs only while the offered group is exclusively selected, as ARIA does to order it (`AriaObjectiveEscortPolicy.OfferExpired`). An offer ARIA never reached no longer lapses unused.
3. **Eligibility** (`AriaObjectiveEscortPolicy.Eligible`).
   - On FC/BT/CE these take an ordinary Move to the objective goal:
     - combat infantry the objective chooser did not pick (new `AriaEscortCandidate.Chosen`, taken from `publicObjectiveChoice`);
     - the `TransportHeli`.
   - Never: the chosen group, originals, radar, logistics and combat air.
   - Base Assault: unchanged, vehicles only.
4. **Bounded support recruit.** `StepObjectivePopup`: the support deadline also bounds a recruit while its Build drawer is open. A dropped recruit waits 15 s before it can be chosen again. Before, S032 held the drawer for the 60 s popup limit five times.

## 2. EditMode tests (`w32-utf`)
- Suites: AriaObjectiveSkill, AriaObjectiveCoverage, AriaObjectiveSupport, AriaFindBaseRecovery, SkirmishConvoyServiceJourney, SkirmishExpandedAria, MatchHudAssistantUiSystemHelper, AriaOffensiveAir and AriaAircraftService.
- **Result: 176/176 Passed.**
- New tests:
  - `EscortGetsATurnAfterAnAcceptedObjectiveOrder`;
  - `StuckSupportRecruitIsBoundedWhileTheDrawerIsOpen`;
  - `StartingGroupsOfEveryRoleAreEscortCandidates`: car, APC, tank, AA, heli, gunner and rocketeer are eligible; chosen, radar and attack heli are not; offer expiry.
- Updated: `ConvoyInfantryEscortsToTheDestination`. On FC, unassigned infantry are now eligible and the chosen group is not.
- Focused-runner counts: 16→18 and 8→9.

## 3. Menu runs (NON-counted, Standard Regular, en) with `Tools/Missions/gate3_objective.py --summary`

| Row | Seed | Pre-W32 baseline (`w32-baseline-w27`, at `15e5bbe02`) | W32 (`w32-menu`, at `1152a9c72`) | Gate 3 before → after |
|---|---|---|---|---|
| S014 DB BT ground | 155923 | Victory 313.7 s (29.04 %) | Victory 363.3 s (33.64 %) | FAIL (apc_armored, car, tank) → **PASS** |
| S020 DB CE ground established | 155923 | Victory 152.2 s (36.23 % of 420) | Victory 242.8 s (**57.81 %**) | FAIL (apc_armored, car, gunner, rifle, tank) → **PASS** |
| S032 CC FC | 130365 | Victory 831.6 s (69.30 % of 1200) | Victory 653.0 s (54.41 %) | FAIL (apc_armored, car, tank) → **PASS** |
| S043 CC CE ground field | 130365 | — (W27: Defeat 271.7 s) | **Victory** 147.2 s (13.63 % of 1080) | **PASS** |
| S047 CC CE combined field | 130365 | — (W27: Defeat 328.7 s) | Defeat ConvoyImpossible 339.2 s | **PASS** |

- **ARIA used** (`units_used_by_aria`, W32), for example S020: apc_armored, breacher, car, convoy_logic, gunner, rifle, rocketeer, tank, tanker.
- **No ARIA stop** (`ariaStopped`) in any W32 run. `roster=match` everywhere.
- **Transport heli: unproven.** None of these rows starts with a transport heli, so the heli Move path did not run.
- **S043/S047 truck losses** (lead asked): S043 now wins. S047 still loses its trucks (ConvoyImpossible). Not diagnosed further.
- Harness evidence (38 MB, 31 MB) moved to each run's `harness-evidence` folder, not committed.

## Open
- **S020 pacing:** 58 % of 420 s, above the 50 % band. The escorts appear to slow the convoy. Lead's call: accept, retune the deadline, or rework the CE escort timing.
- **Transport-heli escort Move:** needs a row whose start includes a transport heli.
- **S047** convoy loss.
- **W27b** (S059 114, S075 101 Inspect) is next.
