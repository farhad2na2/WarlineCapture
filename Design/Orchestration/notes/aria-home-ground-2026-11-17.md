# ARIA blind home defence at low frame rate (Windows lane, 2026-11-17)

Evidence: `origin/missions/windows-lane` `Design/Orchestration/windows_lane_results/2026-11-17-1741.md` and
`Design/AgentReports/SkirmishExpansion/S002/_Evidence/candidates/3ccdf85a…/s002-aria-rs-130365-en-1.jsonl.{input,actors}.jsonl`.

- One enemy tank (`role.tank.2.11.0`) drove to the player HQ (1001, 303.5) and stopped 25.8 units from it at t=69.3,
  then shelled it until the base fell at 96.6 s. The nearest player units were 96–105 units away (trucks/tanker); the
  column was at the enemy side.
- Why it is no threat to the policy: `ObserveMapThreat` only reads presented minimap contacts (active, not last-seen markers);
  `presentedHomeThreat` stayed False in every decision row on Windows and on the Mac (the Mac's raids also log
  `reason=baseDamage`). With no player unit near, the tank is not presented: there is no public contact to tap, so `threat`
  and `threatGround` are correctly False. The ordinary answer is an Attack Move onto home ground (`HomeGround`), which the
  Mac offered after one Home focus and Windows never did (`homeGround=False` in rounds 1–3, after `state=recentre` too).
- Which check refused the home-ground candidates on Windows is not in the files (crowded by presented contacts, off screen,
  or under a HUD control). All 9 candidates lie on the column side of home only, and the round judged the re-centre in the
  frame the focus tap completed.

## Fix (missions/aria-home-ground, stacked on missions/aria-coverage-4)
- HomeGround: when no candidate toward the column is offered and the camera shows home, search the other 7 directions around
  home (same clearance, on-screen and HUD checks).
- Home defence re-centre (stage 10) waits up to 4 s after the Home tap for a target before giving the round up.
- Evidence: `homeGroundRefusal=` on every `event=home_defense` line (1 crowded, 2 off screen, 4 covered by HUD, 8 not
  evaluated), so the next Windows run shows the cause if it persists.
- Test: `RecentreWaitsForTheCameraBeforeGivingUp`.
- Not changed: the generic planner's home defence (other missions) still uses only presented contacts (legacy all-group
  sweep without HomeGround); worth the same treatment if a generic mission shows this.
