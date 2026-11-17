# Decisions pending — for the orchestrator's daily cycle

Hard decisions the program lead cannot take under the defaults. Newest first. Each item: question,
options, evidence, recommendation, what runs meanwhile.

## 2026-11-17 — No legal Airport site on City Crossroads or Mountain Pass

**Question.** Gate 3 requires ARIA to use "aircraft and the Airport" where the mission makes them available. The
air and combined Base Assault drafts on City Crossroads (S027–S030) and Mountain Pass (S051–S054) need a legal
Airport (210×55 cells, with runway). How should these eight missions handle aircraft?

**Evidence.** Airport-only layout dumps with the game's own placement checks and the Airport definition from
`Building_Airport.prefab` (lane9, `program-logs/2026-11-17/lane9/`, notes `daily/lane-probes9-2026-11-17.md`):
City Crossroads tested 3 948 positions, 3 946 rejected by existing map buildings, 2 by roads → 0 legal; Mountain
Pass tested 513, 508 rejected by buildings, 5 by roads → 0 legal. Desert Base and the AP map (CityEdgeAirfield)
have airfields already (S004 uses one).

**Options.**
1. Map reuse policy default: try the next existing Campaign source for these eight missions (e.g. move the air
   variants of the City/Mountain families onto a source with an airfield). Changes which map they use.
2. Keep the maps; on City Crossroads and Mountain Pass the air profile uses helipads and helicopters only (no
   Airport, no fixed-wing); gate 3 then lists only what the mission makes available. Changes the army profile for
   these maps.
3. Mark the eight rows `source-fit unresolved` and continue with the other 112.

**Recommendation.** Option 2 (fewest shared values; helipads are already measured on these maps; no new map).
**Meanwhile:** the eight drafts stay unauthored; work continues on ground missions.

## 2026-11-17 — S002 seed 155923: one enemy tank can end the match at ~95 s

**DECIDED 2026-11-17 (owner via orchestrator):** guard tower at the player base in the Base Assault
template; no more ARIA intercept work. See `daily/2026-11-17-directive.md`.

**Question.** On seed 155923 the enemy's lone tank (420 HP) crosses ARIA's advancing column at ~55 s; when the
column does not kill it, it destroys the 800-HP player base in ~28 s (first hit ~67 s, base dead ~95 s). Is this
an ARIA skill problem to keep working on, or a pacing problem for the budget?

**Evidence.** Baseline ARIA on current code: 155923 en lost at 95.5 s (`program-logs/2026-11-17/probe-seeds1`),
won at 272.8 s (`lane-s002gate` 1614115), so roughly a coin flip decided by whether the column kills the tank
in passing (Codex 019: `briefs/codex-019-seed-loss-analysis-report.txt`). Four ARIA attempts, all worse than
baseline and reverted: opening home guard 1/6 (`aria-gate1`); intercept v1 never fired (`lane-s002gate`);
intercept v2 1 win + 2 Blocked (`aria-intercept-q1`); column camera + intercept 3/5, intercept never fired
(`aria-intercept-q2`). Reasons (`daily/aria-intercept-2026-11-17.md`, `daily/aria-worker-2026-11-17.md`): the
tank is in fog except ~4.5 s at the crossing; ARIA's camera is ~600 units away at that moment; a recall
after the first base hit needs 24–29 s. Baseline re-run 12:45 (`lead-s002-gate3`): 155923 en Abort startupFailure=Timeout (harness), 130365 en Victory 275.9 s,
104731 en **Defeat 127.9 s** (won twice earlier today), 130365 fa-IR Victory 256.9 s — so early losses are not limited to
one seed; today's baseline record per seed: 155923 1W 1L, 104731 2W 1L, 130365 en 3W. A human might also lose this seed. Time spent: ~4 h (over the 2 h rule).

**Options.**
1. Starting forces (2026-11-06 authorization): give the player base one starting defence (e.g. a guard
   tower, as BT/CE already use) or remove the enemy's lone opening tank in the BA-G-E budget. Shared template
   → re-run every mission that consumes it.
2. Raise player base HP or lower the tank's damage — unit/structure stats: not authorized.
3. One more ARIA attempt with a different idea (e.g. keep the anti-armour group as rearguard for the first
   90 s). The last three attempts each broke the baseline.
4. Accept seed variance: replace 155923 in the acceptance seed set. Changes the gate → orchestrator/owner.

**Update 13:15.** The 104731 loss at 127.9 s has an ARIA input cause: the roster-tray swipe overshot page 1 and
looped, so the tank/car/APC page was never ordered (`aria-unit-coverage` notes). A paging fix is being built; re-judge
the pacing question after it runs.

**Recommendation.** Option 1 with a guard tower at the player base in the BA template (fewest shared values,
same mechanism the other objectives use), re-run the three BA missions that start today. **Meanwhile:** no
tuning; ARIA stays on the 4/4 baseline; S002 is recorded with blocker "155923 lone-tank loss (variance)".

## 2026-11-17 — Gate 2 time band versus the Base Assault deadline (S002 first)

**DECIDED 2026-11-17 (owner via orchestrator):** gate 2 band is 20–70% of the deadline; no deadline
change. See `daily/2026-11-17-directive.md`.

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

**Update 10:00.** S002 won 3 of 3 English + 1 Farsi at 272.8, 266.7, 256.2, 261.1 s (`program-logs/2026-11-17/lane-s002gate/unity.log` 1614115–1614160): 24–25% of 1080 s.

**Recommendation.** Decide once S002 wins reliably (the 155923 save is not yet by design) (aria-intercept worker, running now). Option 3
changes the fewest shared values and keeps the published 18-minute rule; option 2 if the band
must stay tied to the deadline.

**Meanwhile.** No deadline, force or resource change is made. Wins are recorded with their times.
