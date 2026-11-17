# ARIA coverage-6: lane-10 findings (2026-11-17)

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane10/{qa,qb}/unity.log`; decision rows under
`Design/AgentReports/SkirmishExpansion/S0xx/_Evidence/candidates/bc60d9a8…/`.

1. **S002 130365 fa-IR Defeat (137.6 s).** Not a localization defect: the lone enemy tank (`role.tank.2.11.0`) reached the HQ
   at t≈69.7 (base 772 → 352 by 85 s). In the en jobs no home-defence round fired at all — the column killed the tank first;
   fa-IR ran a few seconds behind (page 1 at t=44.5 vs ~40) and the tank got through. Home defence: round 1 triggered t=70.95,
   recentre t=76.16 (`homeGroundRefusal=2`, before the camera moved), `gave_up` t=80.18 after the Home tap and the 4 s settle
   with `homeGroundRefusal=7` — every world candidate around home was crowded, off screen or covered. Fix: when the HQ is on
   screen and no world candidate is offered, sample 36 screen points on three rings around the shown HQ and attack-move onto
   the ground under the first point that is in the safe area, clear of presented contacts and not covered.
2. **Blocked stops (all generic planner, no `[AriaS002Trace] event=blocked` line; from the decision rows):**
   - S050 at 192.0 s (qb:168208): after a coverage purchase closed the drawer (obs 174–180), the sweep cleared and re-selected
     card 1073741850: `Inspect` (deselect) obs 182.3, `SelectSquad` attempts 1 obs 184.3, attempts 2 obs 186.3 → 115.
   - S006 at 225.5 s (qb:1127430): the same after Recruit (obs 210–214): card 1073741829 `Inspect` obs 216.5, `SelectSquad`
     attempts 1 obs 218.5, attempts 2 obs 220.5 → 115.
     Cause: each roster change (new recruits) restarts the sweep, which first clears any selection — even when it is exactly
     the unordered assault group it selects next. Fix: such a selection is kept (`SelectionIsPendingAssault`).
   - S049 at 351.1 s (qb:1007714): progress watchdog (last progress obs 165, stop obs ~345). The enemy base was never damaged,
     player units fell 17 → 8 by t≈190 and no structure-capable unit was left (`structureMask=0`); the generic planner has no
     structure-force replacement (only S002 had one), so ARIA waited in ObserveBattle. Fix: the generic planner rebuilds a
     lost structure force through the drawer (breacher first, rocketeer fallback), only after the first accepted assault.
3. **S026 unused apc_heavy, drone, radar, transport_heli.** Drone was never affordable (`affordMask=47`): the scoring helper
   assumes only Barracks and Ground Staging exist. Coverage affordability now uses the real production quote
   (`SkirmishProductionService.EvaluateQueue`: every producer, caps, stocks) plus the 250-materials reserve, quoted at most once a
   second. And the win came at 181 s with ~32 s per purchase; two kinds are now bought per drawer visit (the 25 s gap applies
   between visits). Siege stays a plan.
- Tests: `SecondCoverageKindIsBoughtInTheSameDrawerVisit`, `SelectionOfThePendingAssaultGroupIsKept`.
