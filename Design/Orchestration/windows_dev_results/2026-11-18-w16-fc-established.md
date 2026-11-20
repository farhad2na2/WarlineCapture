# Windows dev results — W16 FC established rows (2026-11-20)

Branch `missions/win-w16-fc-established`. The Windows dev lead did this task.
- **Fix:** shared-code change `79d93b2eb`, under the owner's standing OK.
- **Merge:** `edb61d5d9` merges W36 (`d0136473a`), so DB FC rows use the 1690 s deadline. MP FC stays at 1200 s.
- **Runs:** NON-counted. `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, seed 130365, Standard Regular, en.
  - Unity 6000.5.8f1 via `InvokeUnity.ps1 -GuiLicensing`.
  - Main checkout detached at the commit under test.
- **Logs:** under `win-dev-unity\`:
  - `w16-baseline\`: integration `c11d6172a`, before the fix.
  - `w16-tests\`: the EditMode runs.
  - `w16-proof\`: `edb61d5d9`.
  - Attempt 2: `w16b-utf\`, `w16b-proof\` and `w16b-proof-s008\` (`b54234b7f`); `w16c-utf\` and `w16c-proof\` (`f97366308`).
  - Each run folder has `menu\unity.log`, `terminal.txt` and `gate3.txt`. Harness evidence was moved to `harness-evidence\` and is not committed.

**Status (attempt 2, `f97366308`): DB second zone now usually holds; 4 of 5 DB FC rows win at about 35 % of 1690 s.** S008 still lost once,
with its second zone falling at 140.6 s while the garrison goal stayed stable. Under the lead's rule that ends escort work here; the next lever is
the FC deadline or force budget. Offer flicker is about 0 (at most 0.5 s per row). Full detail in "Attempt 2" below.

**Attempt 1 status (kept for the record): proof failed.** Do not merge `79d93b2eb` alone.
- **DB rows:** the escort change never got a reinforcement to the thinner zone. S008 and S012 still lose on tickets at the same moment as the baseline. S010 aborted with `ariaStopped:Blocked`, stop reason 115.
- **MP rows:** S060 went from Victory to Defeat. S055, S056 and S058 still win.
- **Gate 3:** PASS on all seven proof runs.

## Baseline (`c11d6172a`, deadline 1200)

| Row | Result | s | deadlinePct | zones own/opp | tickets |
|---|---|---|---|---|---|
| S008 DB FC ground est. | Abort (startup timeout, 316 s subscene import) | — | — | — | — |
| S010 DB FC air est. | Defeat | 650.2 | 54.18 | 1/2 | 0/450 |
| S012 DB FC comb. est. | Defeat | 649.0 | 54.08 | 1/2 | 0/452 |
| S055 MP FC ground field | Victory | 613.6 | 51.13 | 3/0 | 500/0 |
| S056 MP FC ground est. | Victory | 545.8 | 45.48 | 2/1 | 500/0 |
| S058 MP FC air est. | Victory | 545.8 | 45.48 | 2/1 | 500/0 |
| S060 MP FC comb. est. | Victory | 621.2 | 51.77 | 2/0 | 500/0 |

## Diagnosis (baseline S010 and S012)
- The two runs are identical in shape:
  - ARIA holds both nearer zones by about 92 s.
  - Every escort, for groups 2–9, goes to the first zone.
  - The second zone is left empty, and the enemy wave takes it unopposed at about 141 s.
  - From then on the enemy holds two zones and drains our tickets.
- The enemy objective commander masses all its groups into one wave aimed at the zone centres.

## Fix (`79d93b2eb`) and EditMode tests
- **Change:** in `AriaObjectiveEscortPolicy.Goal`, Frontline Control only. When the committed zone is owned and uncontested, escorts go to the other owned, uncontested zone that has fewer own groups moving to or holding it.
- **Test:** `FrontlineEscortsGarrisonTheThinnerOwnedZone`, six cases.
- **Results:**
  - `AriaObjectiveSkillTests.RunFocusedValidation`: `[AriaObjectiveSkill] result=Passed controls=19`.
  - `AriaObjectiveCoverageTests`: `[AriaObjectiveCoverage] result=Passed controls=9`.
- **Log overwrite:** a log-path quoting error made both runs write the same Unity log, so the AriaObjectiveSkillTests Unity log was overwritten. Its pass marker survives in `w16-tests\AriaObjectiveSkillTests.wrapper.out`; the AriaObjectiveCoverageTests log is complete.

## Proof (`edb61d5d9`, DB deadline 1690, MP deadline 1200)

| Row | Result | s | deadlinePct | zones own/opp | tickets | vs baseline |
|---|---|---|---|---|---|---|
| S008 DB FC | Defeat | 684.8 | 40.52 | 1/2 | 0/452 | baseline aborted |
| S010 DB FC | **Abort**: Blocked 115 at 233.9 s | 233.9 | 13.84 | 1/2 | 414/453 | was Defeat |
| S012 DB FC | Defeat | 649.4 | 38.43 | 1/2 | 0/451 | same |
| S055 MP FC | Victory | 545.7 | 45.47 | 2/0 | 500/0 | 51.13 → 45.47 |
| S056 MP FC | Victory | 546.0 | 45.50 | 2/1 | 500/0 | same |
| S058 MP FC | Victory | 547.3 | 45.61 | 2/0 | 500/0 | same |
| S060 MP FC | **Defeat** | 648.1 | 54.01 | 0/3 | 0/464 | was Victory |

- **deadlinePct** is the game's `[SkirmishTerminalObjective]` value. The runner's `deadline_pct` column still divides FC time by 1200 (S008 shows 57.07 %), so use the game value.
- **Every run:** `unused_starting=-`, `roster=match`, menu journey Passed.

## Why the fix did not work
- **DB S008, S010, S012:**
  - The garrison offer fires as intended: about 92 s, both zones owned, group 2 sent to the second zone.
  - The escort goal then flips between the second zone and the first zone whenever the enemy wave contests the first. Group 2's offer is re-issued every 11–22 s, alternating with offers for groups 8/9 to the first zone.
  - No group reaches the second zone. It falls at 142–144 s, the same moment as in the baseline.
  - Escort acceptance is still not traced (`escort_accepted` gap, noted by the lead), so I cannot tell whether group 2 accepted any of the four offers.
- **MP S060:**
  - At 45.7 s the garrison rule sent group 1 toward the second zone. Group 1 was the chooser's own holder of the first zone.
  - The chooser then moved group 1 to the second zone (73.9 s) and group 2 back to the first.
  - During that shuffle the enemy wave took all three zones by 181 s.
  - The baseline also flips escort goals, but it kept group 1 holding and retook zones by 159 s.
- **S010 Blocked 115:**
  - ARIA pressed the Move control three times with `selectedGroups=0`, so the press never entered move mode. Intent was Advance, supportStage 2.
  - This is the same stop code as W27c (S038), and it is the next task.

## Proposed next step (lead's call)
1. Leave `79d93b2eb` unmerged, or revert it on this branch.
2. A second attempt would have to:
   - keep a garrison goal stable once chosen, instead of falling back to the committed zone whenever that zone is briefly contested;
   - never send the chooser's own holding group.
   That still needs this same proof set to pass, MP rows included.

## Attempt 2 (lead's rules, 2026-11-20)
The lead's rules for attempt 2:
- Once a garrison zone is chosen, it stays chosen until that zone is lost or the garrisoning group arrives and holds it. No flipping back on contest.
- Never take the chooser's own holding group.
- Prefer a group that isn't already committed to the first zone.

**Base:** the branch merges W27c (`7a20d95d0`, Blocked 115 bounds), so S010's attempt-1 stop path is covered.

### Changes
- **`976876e4f`** (`AriaObjectiveSkillPolicy.cs`, `MatchHudAssistantUiSystemHelper.ObjectiveSkill.cs`):
  - New `AriaGarrisonState` keeps the chosen zone, the committed zone, the bound group, and whether that group's Move was accepted.
  - `Goal(..., ref garrison)` returns the kept zone until it is lost, an own group holds it, or the bound group's Move is accepted (then other escorts return to the committed zone). A bound group that dies before arriving frees the offer.
  - `Pick(..., garrison: true)` never takes a group holding another zone (`Holding`) and prefers groups not sent to the committed zone (`Committed`). The 2-arg `Pick` and other objective types are unchanged.
- **`f97366308`** (attempt 2b): the fresh chooser skips a zone an own group already holds.
  - The `b54234b7f` proof showed why this is needed. After a garrison arrived, the holder check reset the choice, and the next frame re-chose the same zone because it was still the thinner one.
  - Offers then flipped every frame: S060 87 s, S010 58 s, S055 38 s, S056 11 s, S012 10 s, S058 2 s. Attempt 1 had about 0.
  - Every flip reset the 45 s offer clock and the escort touch plan.

### EditMode (12 suites, same filter as W27c)
- **`b54234b7f`:** 213/213 Passed (`w16b-utf\results.xml`).
- **`f97366308`:** 213/213 Passed (`w16c-utf\results.xml`).
- **New tests:**
  - `FrontlineGarrisonZoneStaysChosenUntilHeldOrLost`: the zone is kept on contest; the bound group keeps it; death frees it; it is released when held or lost. Attempt 2b adds the 2-vs-1 held zone, which stays on the committed zone for 3 frames, and the case where the holder leaves and the zone is re-chosen.
  - `GarrisonOfferSkipsHoldersAndPrefersUncommittedGroups`.
- **Marker:** `[AriaObjectiveSkill] result=Passed controls=22`.

### Proofs (NON-counted, @130365, DB deadline 1690, MP deadline 1200)
Flicker is the seconds of rapid escort-offer bursts (consecutive offers less than 0.3 s apart), from `escort_offer` lines.

| Row | Attempt 2 `b54234b7f` | flicker s | Attempt 2b `f97366308` | flicker s | Attempt 1 | Baseline (1200 s) |
|---|---|---|---|---|---|---|
| S008 DB FC ground est. | Victory 35.04 % (warm rerun) | 0.4 | **Defeat 38.37 %**, 1/2 | 0.3 | Defeat 40.52 % | Abort (startup) |
| S009 DB FC ground fresh | — | — | Victory 69.53 %, tickets 9/0 | 0.1 | — | Mac lane 46 counted @1200: W2 L2 |
| S010 DB FC air est. | Victory 35.00 % | 57.9 | Victory 34.98 % | 0 | Abort 115 | Defeat 54.18 % |
| S011 DB FC air fresh | — | — | Victory 35.01 % | 0 | — | 89.71 %, mixed gate 3 |
| S012 DB FC comb. est. | Victory 35.05 % | 9.6 | Victory 35.06 % | 0 | Defeat 38.43 % | Defeat 54.08 % |
| S055 MP FC ground field | Victory 45.46 % | 37.8 | Victory 59.63 % | 0 | Victory 45.47 % | Victory 51.13 % |
| S056 MP FC ground est. | Victory 57.70 % | 11.1 | Victory 56.23 % | 0.5 | Victory 45.50 % | Victory 45.48 % |
| S058 MP FC air est. | Victory 45.58 % | 2.1 | Victory 45.53 % | 0 | Victory 45.61 % | Victory 45.48 % |
| S060 MP FC comb. est. | Victory 55.97 %, 3/0 | 86.7 | **Defeat 54.81 %**, 1/2 | 0.3 | Defeat 54.01 % | Victory 51.77 % |

- **All runs:**
  - Gate 3 PASS, `unused_starting=-`, `roster=match`, menu journey Passed.
  - Zero `[AriaBlocked] reason=115`.
  - deadlinePct is the game value.
- **S008 in the `b54234b7f` queue:** it was the first job after the checkout and aborted at startup (`startupFailure=Timeout`, cold import). Its warm rerun is `w16b-proof-s008\`.
- **The `f97366308` queue** began with a throwaway warm-up, S058 @130366, and S008 then started normally.
  - The warm-up aborted at 468.7 s with `ariaStopped:Blocked` reason 101.
  - The objective Move for group 41 was refused six times (`order_blocked failures=6`) while the enemy held all 3 zones from 383.7 s. That is the designed order-blocked stop in a lost position, not 115.
  - It is not a proof row; it is noted here as a separate finding.

### Reading
- **DB second zone:**
  - S009, S010, S011 and S012 hold both zones from about 92 s to the end. Attempt 1 and the baseline lost the second zone at 141–144 s.
  - S008 held in one run (`b54234b7f`). In the `f97366308` run it took both zones later (105 s) and lost the second at **140.6 s**.
  - In that run the garrison goal stayed on that zone (group 3 offered at 113.8 s and 124.0 s, no flips). The chooser took group 3 for that zone at 140.6 s, as it fell.
  - So this run is the lead's stop case: the second zone fell by about 145 s with a stable goal. The next lever is the FC deadline or force budget, not escort logic.
- **S060:**
  - Lost in the `f97366308` run and in attempt 1, won in the baseline and the `b54234b7f` run.
  - In the losing run our infantry fell from 20 to 1 by 142 s and both zones were lost (82 s and 142 s). That is the fight, not the garrison choice.
  - There is one 3 s slow alternation (offers about 0.7 s apart) at 105–108 s, between group 8 to zone A and group 4 to zone B; it is below the burst threshold.
- **Run-to-run variance:** the same rows on the same seed diverge before the garrison logic runs (S008 owned 2 zones at 92 s in one run and 105 s in the other). One run per row cannot separate variance from a defect. The counted Mac lane is the judge.

### Open
- Escort acceptance is still not traced (`escort_accepted`), as noted in attempt 1.
- S008 and S060 FC need a counted lane decision on the deadline or force budget if they keep losing there.
