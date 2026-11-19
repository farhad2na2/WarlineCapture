# Windows dev results — W27 ARIA self-stop fixes: cause 1 and cause 3 (2026-11-19)

Branch `missions/win-w27-aria-stop-fixes` (from integration `391d02195`). The Windows dev lead did this task. Shared ARIA code, under the owner's standing OK given in this Windows session (2026-11-19): "Standing OK for tested shared code, tool and map work from the program lead's queue; no unit-stat or game-rule changes." Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at `d24f9b748`. Logs under `win-dev-unity\w27-utf\`, `w27-utf-b\` and `w27-menu\`. Cause list: `2026-11-18-w27-aria-stops.md`.

**Outcome:**
- **Cause 1 and cause 3 are fixed and tested.** The targeted stops are gone in play: no Blocked 115 and no 101 `ObserveObjective` in six runs.
- **Two other stops appeared** and were not part of this task: S059 (114, ReturnAircraft) and S075 (101, Inspect). See section 3.
- **Cause 2** (Industrial Basin: no progress signal) is **not done**. Parked by the lead's order: W32 comes next.

## 1. Changes
**Cause 1 `c040266fd`: the input layer's 3-press stop raced the planner's own retry (S061, S075).**
- **Objective attempts.** `AriaSkirmishPlanSystem.Objective.cs`: an objective touch attempt now ends after `MaxRejectedObjectiveContacts = 2` rejected re-presses, which is 3 presses of one target. `StepObjectiveOrder` then absorbs it as an `order_retry`. Before, the planner bound was three re-presses (four presses), so `AriaPlayDecisionSystem.cs:109` stopped ARIA with 115 first.
- **Fresh count on retry.** `AriaSkirmishPlanSystem.ObjectiveSkill.cs`: an absorbed retry, and an escort failure, reset the input count and target (`touch.Attempts = 0; touch.TargetId = 0`). Otherwise the stale count of 3 would stop the next press.
- **Advance bound.** `AriaSkirmishPlanSystem.cs`: the expanded Advance branch (`PublicTerrainApproach`) now gives the assault up after `MaxSameAttempts` presses of the same approach (`AriaFindBasePolicy.SameControlExhausted(touch.TargetId, touch.Attempts, view.AdvanceGround.Id)`), the same way an exhausted base search does. The give-up block moved into `GiveUpAssaultSearch` (`AriaSkirmishPlanSystem.Coverage.cs`), which both branches share.
- **Not touched:** the legacy S002 planner (`AriaGroundBaseAssaultPolicy`).

**Cause 3 `d24f9b748`: a handed-back convoy service blanked the objective frame (S068, CC CE field rows).**
- `SkirmishConvoyServiceJourney.PresentsEscortObjective(stage)` is true for `Resume` and `Handback`.
- On Handback, `MatchHudAssistantUiSystemHelper.ConvoyService.cs` now gives ARIA the ordinary escort objective (and resets the objective choice once) instead of an empty `Presented=false` objective.
- The stage stays latched, so the failed service is never retried. The player still sees the diagnosis.

No unit stats, no game rules and no gate changed.

## 2. EditMode tests (`InvokeUnity.ps1 -runTests -testPlatform EditMode`)
- **`w27-utf`, after cause 1:** AriaObjectiveSkill, AriaFindBaseRecovery, AriaObjectiveSupport, AriaObjectiveCoverage and SkirmishExpandedAria suites: **129/129 Passed**.
- **`w27-utf-b`, after cause 3:** the same suites plus SkirmishConvoyServiceJourney and MatchHudAssistantUiSystemHelper: **153/153 Passed**.
- **New tests:**
  - `RejectedObjectivePressesAreRetriedBeforeTheInputLayerStops`: three presses of an unresponsive Move, then a retry with a fresh input count, never 115.
  - `AdvanceGroundIsGivenUpBeforeAFourthPress`.
  - `HandbackKeepsTheEscortObjectiveForAria`.
  - The source check in `BothPlannersRecoverAndTheTouchDriverIgnoresItsOwnDevices` now also reads `Coverage.cs`.
  - Focused-runner counts: 15→16 and 23→24.

## 3. Menu runs (NON-counted, Standard Regular, en; `w27-menu\menu\unity.log`)

| Row | Seed | Result | match s | ARIA stop | Before |
|---|---|---|---|---|---|
| S043 CC CE ground field | 130365 | Defeat ConvoyImpossible (return yes) | 271.7 | — | Abort 101 ObserveObjective (Mac lane 44; W31c) |
| S045 CC CE air field | 130365 | **Victory** ConvoyDelivered | 346.8 | — | same Abort |
| S047 CC CE combined field | 130365 | Defeat ConvoyImpossible (return yes) | 328.7 | — | same Abort |
| S057 MP FC | 104731 | **Victory** TicketsDepleted | 544.9 | — | 115 (Mac) |
| S059 MP FC | 104731 | Abort | 291.0 | **114**, kind Unavailable, intent ReturnAircraft (log :55463) | 115 (Mac) |
| S075 IB BA air field | 130365 | Abort | 430.4 | **101**, kind Waiting, intent Inspect, control Squad3 (log :265146) | 115 at 333.9 s (W29) |

- **Order failures:** zero `order_retry` / `order_blocked` lines in the log. These rows ran at the integration deadlines: CC CE at 1080 s, because W31c was not merged yet.
- **The two new stops** are not cause 1 or cause 3:
  - **S059 (114).** A ReturnAircraft recruit target stayed Unavailable for 15 s (`AriaPlayDecisionSystem`; recruits at 189/245/290 s with 30 materials).
  - **S075 (101, Inspect).** This is the foreign-popup path, `AriaSkirmishPlanSystem.cs:824`. It reached 430 s instead of the earlier 334 s.
  - Neither is diagnosed further here.
- **Not run:** S061 and S068 were not rerun. Their failing W23b geometry is unmerged, and the integration W10 geometry doesn't reach those paths.
- Harness evidence (52 MB) moved to `w27-menu\harness-evidence`, not committed.

## Open
- **Cause 2:** Industrial Basin assault progress. Parked; W32 next.
- **New stops:** S059 114 (ReturnAircraft unavailable) and S075 101 (Inspect popup).
- **S043/S047 losses:** convoy lost after the handback. A play or balance result, not a stop.
