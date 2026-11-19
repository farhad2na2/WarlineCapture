# Windows dev results — W32 ARIA leaves starting units idle on FC/BT/CE: read-only diagnosis (2026-11-19)

Branch `missions/win-w32-aria-starting-coverage` (from integration `49512fe1f`). The Windows dev lead did this task, with a read-only code and log read by a subagent. No Unity run, no code change. The fix waits for W31, W27 cause 1 and the owner's OK in this session.

**Answer: cause (a).** The idle starting groups are never commanded. The "used" recorder works.

The Mac failing jobs (S014/S020 at 155923) are not on this machine, so these Windows logs at seed 130365 were used instead:
- `w14` (S020 CE);
- `w7` (S032 FC);
- `w23b-cc-bt` (S038 BT);
- for counts only: `w25-db-ce-field-seeds` and `w28-s019-seeds`.

Log line numbers below refer to each run's `menu\unity.log`.

| Row | Starting roles (initial_world) | Recorded as used | What ARIA commanded |
|---|---|---|---|
| S020 CE (w14:3387, 8457) | rifle, gunner, rocketeer, car, apc, tank, convoy, support | breacher, convoy_logic, marksman, tanker | only the truck groups 23/24/25. Escort offers went to groups 1→2→3 at 7.5 / 55.0 / 100.1 s |
| S032 FC (w7:13701, 24561) | same, no convoy | breacher, marksman, rifle, rocketeer (produced), tanker | only groups 1/2 (rifle), plus one choice of group 3. Escort offers went to 6→7→8 (w7:14677, 17272, 19654) |
| S038 BT (w23b:11034, 54283) | same | breacher, gunner, rifle, tanker | groups 1–3 (originals) and 4 (corridor). Escort offers went to 6→7→8 (w23b:12026, 17204, 54107) |

- **No escort order ever completed.** All five logs contain zero `escort_accepted` or `escort_retry` lines.
- **The starting groups could be selected.** They exist as separate army groups, one role each (`groups=14` / `groups=11`; `SkirmishArmyGroupRecord.Role`, `SkirmishS002AriaRunHarness.GateUnits.cs:320`).

## Root causes
1. **The objective chooser only picks objective subjects.** `SkirmishPlayerObjectivePolicyUtilitySystemHelper.Candidate`:
   - FC/BT: dismounted combat infantry only (`:119-120, 153`).
   - CE: convoy-truck groups only (`:169`).
   - Ties go to the lowest group id (`:93-94, 217-218`). So rifle groups 1–2 (FC), or the originals and group 4 (BT), take every objective order. Gunner, rocketeer and all vehicles are never chosen.
2. **The escort, the only order path for vehicles (and CE infantry), is starved.**
   - It runs last in `StepObjectiveSkill` (`AriaSkirmishPlanSystem.ObjectiveSkill.cs:104`), after objective, support and coverage steps (`:84-103`).
   - The 45 s offer (`AriaObjectiveSkillPolicy.cs:162`) expires on the simulation clock whether or not ARIA tried it.
   - An expired group goes into `escortSkipped`, which is cleared only when the presentation epoch changes (`MatchHudAssistantUiSystemHelper.ObjectiveSkill.cs:15, 60-61, 84-86`).
   - So each group gets one window, and it lapses while ARIA is busy elsewhere.
3. **A support recruit that can't succeed holds the Build drawer open for 60 s, again and again.**
   - S032: 5 times, about 300 s of a 543 s match (64.8→125.0, 197→257, 279→339, 372→432, 476→537 s). Each follows a `recruit kind=AttackAircraft` line. S038 shows the same (107.4→167.6 s).
   - While the popup is open, the recruit is retried (`ObjectiveSkill.cs:235-244` → `ObjectiveSupport.cs:170`).
   - The 30 s support deadline is checked only in `StepPublicObjectiveSupport` (`ObjectiveSupport.cs:67`), which doesn't run during the popup.
   - The forced close (`ObjectiveSkill.cs:248-250`) sets no `NextDecisionAt`, so the same recruit is chosen again.
4. **The transport heli has no command path on FC/CE.**
   - The escort accepts the Ground domain only, plus CE infantry (`AriaObjectiveSkillPolicy.cs:170-173`).
   - `StepOffensiveAir` handles combat air only.
   - Boarding is BT-only, once the exit is open (`AriaSkirmishPlanSystem.PublicBoarding.cs`).
5. **AA is in the Ground domain** (`SkirmishRosterContracts.cs:177`), so it is covered by the escort path and has the same starvation.

## (b) ruled out
- "Used" is written at `GateUnits.cs:183-185`. Its sources are verified AriaCommandEvidence Move orders (`:198-200, 292-303`) and group-ledger advances (`:322-335`).
- That path demonstrably records the same kind of order: convoy_logic from truck Moves (S020), rifle from Move/Hold (S032), gunner from the corridor Move (S038).
- An escort uses the same select / Move / ground-tap / receipt sequence (`ObjectiveSkill.cs:141-158`), so it would be counted if it ran.

## Narrowest ARIA-side fix (proposed; no gate or unit-stat change; needs the owner's OK)
1. **Give escorts a turn.** Once `plan.ObjectiveAccepted > 0`, run `StepObjectiveEscort` before support and coverage until each eligible starting group has one accepted escort. Alternatively, give escorts the existing `ObjectiveCoverageTurn` slot (`:93-97`).
2. **Make the offer expire on real attempts, not elapsed time.** Expire a group only after a failed escort attempt (EscortFailures), or don't count time while the frame is missing or ARIA's popup is open (`MatchHudAssistantUiSystemHelper.ObjectiveSkill.cs:84-86`).
3. **Widen escort eligibility** (`AriaObjectiveSkillPolicy.cs:170-173`):
   - FC/BT: combat infantry the chooser left unassigned (gunner, rocketeer) escort or hold the committed zone or corridor.
   - TransportHeli takes an ordinary Move to the goal. **Unproven:** that the Move button and its receipt work for helicopters.
4. **Bound the stuck support recruit.** Enforce `support.Deadline` and `RepeatedContacts` inside `StepObjectivePopup`, and set `NextDecisionAt` after a forced close.

Each would need an EditMode test, then non-counted S014/S020/S032 runs to show the evaluator passes and the rows still win at the same pacing.

## Unproven
- Which player group id maps to which role. On FC/BT, groups 6/7/8 must be ground vehicles, given the escort filter.
- Whether the escort ever started a touch sequence; no trace line records that.
