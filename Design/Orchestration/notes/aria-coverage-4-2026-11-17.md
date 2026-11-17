# ARIA coverage-4: lane-6 findings and fixes (2026-11-17)

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane6/qa|qb/unity.log` and the decision rows in
`Design/AgentReports/SkirmishExpansion/S0xx/_Evidence/candidates/78520559…/*.jsonl.input.jsonl`.

1. **Radar never used in S002.** It was bought in turn, but the purchase timed out. 130365: `coverage kind=radar state=buying`
   t=169.40 → `gave_up` t=179.43 (drawer open); 104731: 165.18 → 175.18. Both exactly +10.0 s (`BuySeconds`). Marksman and
   apc_fast needed ~7 s; the radar card sits lower in the Vehicles list (Build, Vehicles tab, scroll, card, produce). The gate
   model is right: no production reservation, no order → unused. Fix: while the purchase keeps landing taps the deadline
   extends by 6 s per tap, capped at 24 s from the start.
2. **S002 155923 used only gunner/rifle/rocketeer.** Page 0 was ordered (t≈25); the chevron reached page 1 (t=39.7), and then
   no page-1 card resolved as a touch target (`squadCardMask=31 squadTargetMask=0`) for 147 s: intent ObserveBattle, Waiting.
   Re-paging at t=186.7 (page 0 resolved 31, page 1 again 0) did not help. `AriaGroundColumnPolicy` returned "not handled"
   when the next card was unavailable, with no bound. Why page 1 refused every card in this session (raycast owner mismatch,
   card presentation, swipe reachability) is not in these files: slot diagnostics were not captured. Fix: column stall guard —
   a card not offered for 8 s gives the page up (counted as inspected, remembered so it does not reopen the sweep).
3. **Generic-planner coverage never recruited (S026, S050).** The breacher fired (t≈66–79), then home defence ran and the
   re-sweep armed Attack for the page-2 breacher; neither the enemy base nor its approach ground was offered, so the planner
   searched (`FindBase`: map open, map focus) with Attack armed for 50+ s until the match ended. `AssaultIssued` never latched
   again, and coverage needs it (and treats armed Attack as busy). Fixes: (a) the base search is bounded (`AriaFindBasePolicy`:
   4 search taps, never a third press of the same control; then Attack is toggled off and the column latched with that page
   remembered as skipped); (b) coverage is gated by the first accepted assault (latched), not the current sweep.
4. **S049 Blocked.** Progress watchdog, not a touch retry: the enemy base sat at 140 HP from t≈130; last progress obs 144,
   stop at obs ~324 (180 s). From obs 150 the planner alternated Attack / tap base for slot 3 (the only structure group); 202
   `ArmyAttack` receipts were accepted and verified, but the group's Attack order never showed in the roster and the base took
   no more damage. ARIA input works; the structure unit cannot finish the base (likely pathing/reach on the re-authored S049
   layout; for the layout owner). Fix here: the column stall guard gives that order up after 20 s instead of re-arming
   indefinitely. The watchdog still ends a match where nothing can damage the base.
- Siege stays a plan (Full Arsenal research).
- Trace: `[AriaS002Trace] event=column_recovery state=page_skipped|find_base_given_up ... squadTargets= enemyBase= advanceGround=`.
- Tests: `AriaColumnRecoveryTests` (5).
