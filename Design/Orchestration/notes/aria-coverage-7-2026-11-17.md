# ARIA coverage-7: lane-12 findings (2026-11-17)

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane12/{qa,qb}/unity.log`; decision rows under
`Design/AgentReports/SkirmishExpansion/S0xx/_Evidence/candidates/8e48bbb4…/`.

1. **S050 `ariaStopped:Blocked` at 430.2 s (qb:81641), no `event=blocked` line.** It is the planner progress watchdog
   (`AriaGroundProgressPolicy`, 180 s without progress; the input-side 115 stop logs `event=blocked`, the watchdog does not):
   last progress at obs 209.9, enemy base at 580 HP from t≈172. In that window ARIA flipped roster pages: page 1
   (`assaultMask=28 attackOrderMask=0 selectedMask=4`, `pagesOrdered=0`) → Previous (-107720); page 0 (all ordered,
   `assaultSelecting=2`, the stale selection off-page) → the deselect toggle resolved to a swipe to page 1 (-268541236); and
   so on. Two column rules undid each other: "return to the first unordered page" ran before "clear the stale selection".
   Fix: clear the stale selection first (the card is visible on page 1), then return. Test:
   `StaleSelectionOnALaterPageIsClearedBeforeReturning`. Page traces (`event=tray_page`) now log for every expanded base
   assault, not only S002.
2. **S026/S049 unused kinds.**
   - S026 (Established): coverage started at t≈96 and was paced at one kind per ~35 s; a home raid made the purchase
     Busy at the moment a same-visit second purchase could start. Now: 12 s between drawer visits, up to three kinds per
     visit, combat kinds first (marksman, apc_fast, gunner, tank, then radar, apc_heavy, drone, transport heli).
   - S049 gunner/tank: S049 has no player tank or gunner actor at all — both are producible in its catalog but not in its
     starting force, and coverage did not include them (they are starting units in S002/S026). Gunner and tank are now
     coverage kinds (bits 64/128; in S002/S026 they are fielded from the start and skipped).
   - S049 apc_heavy, radar, tank, drone, transport_heli, helipad, satellite_dish: S049 starts at readiness Field
     (`SkirmishStart_Field.asset`, readiness 1); all of these need Established (research 240 materials, 45 s), and drone /
     transport heli additionally need the satellite dish / helipad. S049's materials stayed 330–500 for the whole match and
     readiness never rose. With the 250 reserve, research plus those purchases does not fit this economy without hurting the
     win (S049 won at 287.8 s). Left as a decision (below).
3. **Buildings in units_available.** The gate model lists a producer structure only when a catalog role needs it and the
   player does not start with it (`producerStructuresNotInStartingStructures`); it does not model readiness
   (`readinessGating=0`). Only S049 lists buildings (helipad, satellite_dish), and both need Established readiness
   (`SkirmishNativeProduction.RequestFailure`: Helipad/IntelStation producers → Established). ARIA's drawer can place them:
   the existing helipad flow (`StepHomePadPlacement`: Home focus, Buildings card, legal home pad site, confirm) is generic
   enough to take the satellite dish card as well. Plan (not implemented, needs a decision because of the cost): in Field
   missions, after the first accepted assault and above the reserve, research Established once; then build the helipad and
   the dish once each through the pad-site flow; then the coverage recruits (tank, radar, apc_heavy, drone, transport heli)
   unlock through the normal quote. Alternative: the gate model marks readiness-gated types as `researchGated` instead of
   plain unused for Field starts.
- Siege stays a plan.
- Tests: `StaleSelectionOnALaterPageIsClearedBeforeReturning`; batching/order updated (`SecondCoverageKindIsBoughtInTheSameDrawerVisit`).
