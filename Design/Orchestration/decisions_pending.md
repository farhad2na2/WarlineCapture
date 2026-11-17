# Decisions pending — for the orchestrator's daily cycle

Hard decisions the program lead cannot take under the defaults. Newest first. Each item: question,
options, evidence, recommendation, what runs meanwhile.

## 2026-11-17 — Gate 2 time band versus the Base Assault deadline (S002 first)

**Question.** Gate 2 requires time-to-win at 40–70% of the deadline. S002 wins in about 236–268 s
against a 1080 s deadline (22–25%). How should Base Assault missions meet the band?

**Facts** (Codex 022, `/Volumes/T7/Projects/WarlineCapture-program-logs/briefs/codex-022-deadline-gate2-data-report.txt`):
- 1080 s comes from the shared asset `SkirmishObjective_BA.standardDeadlineSeconds`, used by S002–S006,
  S026, S049, S050 today and by the template (`MATCH_SETUP.md`: BA Standard 18 min) for all BA rows.
  The compiler checks it against both `InitialSetupMatrix.csv` copies; tests pin 1080 for S002.
- The tuning loop allows ±40% (minimum 648 s): 236 s is still only 36%. A per-mission S002 deadline
  needs a new S002-only objective asset, both matrices and test updates.
- The plan's early-win rule says: strengthen the enemy or reduce the player. S002 still loses
  seed 155923 to a single tank, so making it harder now works against gate 2's 3-of-3 requirement.
- At expiry current code returns Draw/TimeLimit, not the plan's health-percentage tiebreak.

**Options.**
1. Keep the gate; per mission, cut the deadline to the −40% floor (648 s) and strengthen the enemy
   until wins land at 260–450 s. Needs a per-mission objective asset for each BA mission.
2. Keep the gate; change the shared BA Standard deadline once (e.g. 600 s) for all BA missions —
   one shared value, one test/matrix update, outside the ±40% rule.
3. Change gate 2 for BA: measure the band against a per-layout target time (e.g. 40–70% of a
   measured ARIA median) instead of the hard deadline, which stays a safety limit.

**Recommendation.** Decide after S002 wins 3 of 3 (aria-intercept worker, running now). Option 3
changes the fewest shared values and keeps the published 18-minute rule; option 2 if the band
must stay tied to the deadline.

**Meanwhile.** No deadline, force or resource change is made. Wins are recorded with their times.
