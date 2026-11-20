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
**RE-DECIDED 2026-11-17 late (orchestrator, owner-delegated):** legibility first, measured counterplay second, guard
tower only if the measurement fails (directive updated). The tower is removed from the template for the measurement.
**MEASURED 2026-11-18 (program lead, lane 20):** 155923 won 3 of 3 with the base alert only; the tank reached the base once
(base survived at 44/800, defenders ordered 1.2 s after the first hit). Tower stays off. Thin evidence: no second or third
tank-reaches-base seed among first-visit seeds; revisit if an owner/phone run loses the base to the lone tank.

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

## 2026-11-17 — For the orchestrator next daily review: Codex independent review (owner-shared)

The lead already has this handoff and owns execution. Figures below are the review claims; verify
against the named logs before relying on them.

1. **Corrected clock.** `run_aria_queue.py` writes whole Editor job time to `aria_time_s`; the gate
   needs the match clock (`runs.csv duration_seconds`). Claimed: `lane12/qb/unity.log` S026 won at
   147.868 s (line 38901), S049 at 194.951 s (line 129843), both under the 216 s floor (20% of
   1080 s); S002 lane12 wins 230–232 s (in band). Check: ledger rescored, failures kept, warm-ups /
   save probes / counted runs separated. Note: the 20% floor approved today was argued from
   whole-job times (256–273 s); S002 is at 21% on the match clock, so the margin is thin.
2. **Seed 155923** lost again at 95.777 s in the lane14 real-menu set (3 of 4). Check: one failing
   seed reproduced on unchanged inputs before another matrix; guard tower directive applied or not.
3. **Real-menu publication.** Authored rows stay Planned, the menu preflight rejects Planned, and
   publication waits on acceptance (circular). Proposal: run acceptance through the real catalog on
   an internal candidate. **DECIDED 2026-11-17 (owner: "Yes, internal only"):** authored missions under
   acceptance may be visible in the real Skirmish catalog on `missions/*` branches only; each row records the
   commit that made it visible; a failed mission goes back to Planned; gates unchanged. Checked: the only build
   pipeline (`Jenkinsfile.groovy:61`) clones `--branch main`, so nothing player-facing is built from `missions/*`.
4. **Early owner sampling** of the first BA, FC, BT, CE prototypes. Waits on the owner saying when
   and on which device.
5. **Metrics to review:** accepted missions per week, Unity minutes per accepted sibling, time to
   the first accepted sibling after S026.
6. The standalone battle host / quick simulator stays parked (Unity-first), so the owner goal of a
   99%-accurate quick check replacing Unity is deferred, not dropped.

## 2026-11-17 — Codex designer playtest (S002, S026): shared P0 for the next daily review

Source: `Design/Orchestration/owner_packets/2026-11-17-s002-s026-designer-playtest.md`. Claims as
reported, to be verified from logs. Recorded as designer play only: no owner manual win, no device
acceptance.

- **P0 (runtime, all missions).** Real-menu S026, Regular/Standard, seed 104729, ARIA off, normal
  reinforcement purchases and attrition: at about 9:36 a 16 MiB ECS allocator error in production
  actor adoption; at 18:00 (the deadline) the settlement system threw and the result never opened.
  The automated win at 2:32 never reaches this path.
- **Orchestrator ruling (2026-11-17):** no row is marked auto-accepted on a runtime candidate until
  one long-session regression passes on it: play past reinforcement purchases to deadline expiry,
  no allocator error, result screen opens and return works. Once per candidate, not per mission.
  Also: a focused data preflight per candidate; a separate phone playtest per objective family is
  an owner/device gate, recorded separately.
- **S002:** lost to one tank at 1:35 with zero troop losses. The review says fix threat legibility
  and verify counterplay before balance changes. This conflicts in order with the guard tower in
  the 2026-11-17 directive; waits on the owner (asked).

## Save & Exit for Frontline Control / Breakthrough / Convoy Escort (program lead, 2026-11-18)
- **Question:** offer Save & Exit on FC/BT/CE Skirmish missions? Today `SkirmishNativePlayerSaveService.CanSave` needs `SupportsGenericSave`, which requires `ObjectiveConfig.Kind == BaseAssault`, so FC/BT/CE never show Save (finding on missions/fcbt-result-copy 2e9b72c88). The snapshot and checkpoint services already cover non-base objectives (incl. `SkirmishNativeConvoyCheckpoint`); resume validations are still Base-Assault-only.
- **Options:** (a) keep Save off for FC/BT/CE (gate 4 records "checkpoint: not offered"); (b) relax the Kind check and prove capture/resume/recovery per objective in Unity.
- **Recommendation:** (a) for now; revisit after ARIA can play FC/BT/CE. Missions run 18–20 minutes, so Save is a convenience rather than a necessity.
- **Meanwhile:** nothing enabled; FC/BT/CE rows record "checkpoint: not offered".

## S004 (Desert Base, army A Airfield): Airport does not fit, and the air cap blocks drone/attack-heli coverage (program lead, 2026-11-18)
- **DECIDED by the owner (Farhad, in chat, 2026-11-18): "do as you recommend for S004".** Airport: option (a). S004 offers no Airport and no fixed-wing aircraft (fighter, strike, transport_plane) on this source, and gate 3 lists only what the mission makes available. Air cap: unchanged for now; decide after the coverage-9 run shows the S004 pacing, for all army-A rows together. This decision covers S004 only; the City Crossroads / Mountain Pass Airport entry stays open.
- **Evidence:** lane 24 S004 runs (`program-logs/2026-11-18/lane24/`): 3 wins at 16–17% of 1080 s, gate 3 unused = airport, aa, apc_fast, attack_heli, breacher, drone, fighter, marksman, radar, strike, transport_heli, transport_plane. Coverage worker analysis (missions/aria-coverage-9 8bb2ba21e): 6 types were policy gaps (now fixed in code, unverified). The remaining 6 have two causes:
  1. **Air cap.** InitialSetupMatrix S004 Standard `tactical_air_cap_each`=2. The starting transport heli plus the bought light attack heli fill it, so drone and attack_heli can never be fielded together with them.
  2. **Airport source fit.** The lane3 S004 layout dump has 0 Airport candidates (no clear 210×55 m site on the S004 playable area). Fighter, strike and transport_plane need the Airport (and Full Arsenal research, 440–520 materials each against 900 starting).
- **Options:**
  - Air cap: (a) raise S004's `tactical_air_cap_each` to 3–4. This is production-capacity tuning, which the 2026-11-06 owner direction allows, but it also changes player air limits, and the same question applies to every army-A row. (b) Keep 2 and accept sequential coverage (a drone only after an air loss). This is unreliable for gate 3.
  - Airport: same choices as the City/Mountain Airport entry above. (a) No Airport / no fixed-wing for army A on this source; gate 3 then lists only what is available. (b) Move S004 to another existing Campaign source with an Airport site. No new map.
- **Recommendation:** Airport (a), consistent with the CC/MP entry. Air cap: raise only after the coverage-9 run shows how much S004 pacing changes; decide it together for all army-A rows, not per mission.
- **Meanwhile:** coverage-9 merges and runs (lane 26). S004 stays not accepted; the pacing re-measurement uses full coverage.
- **Update (program lead, 2026-11-18, lane 36 on W13 3e65ef053, `program-logs/2026-11-18/lane36/s004/unity.log`):** 4 counted runs (3 EN + 1 FA), 4/4 Victory at 18.4–33.9% of 1080 s, menu journey and return Passed, 0 AriaBlocked. Coverage fixes are in: gate-3 unused went from 12 types to `attack_heli;drone` in every run (plus `aa` 2/4, `apc_fast` 1/4). The only remaining cause is the air cap (cause 1). S004 now fails only gate 3, so the air-cap choice is the last thing holding S004. The decision is ready to take: raise `tactical_air_cap_each` for army-A rows (production-capacity tuning, allowed by the 2026-11-06 owner direction), or keep 2 and S004 stays unaccepted.

## Gate 3 ("every available unit type used") on objective missions (FC/BT/CE) — 2026-11-18, program lead
**Question.** Should gate 3 on Frontline Control, Breakthrough and Convoy Escort require every available type, or only the objective-relevant roles, with full-catalog coverage proven in a separate production validation?
**Evidence.** Codex 038 (`WarlineCapture-program-logs/briefs/codex-038-fcbtce-gate3-budget-report.txt`). All three start with 450 materials, an 800 bank cap and one depot (50 per 30 s). Buying one of each unused type costs 1,845 (S007), 2,165 (S013) and 2,565 (S019), plus 720 for readiness research. With the matches ending at 593 s, 377 s and 115 s, the starting stock would need to be 2,415 / 2,925 / 3,575, above the 800 cap. Longer deadlines do not help, because tickets, evacuation and delivery end these missions early. S019 is also under the 20% pacing floor (won at 10.7%).
**Options.** (a) An objective-specific gate 3: objective-relevant roles must be used in the mission run, and every type is proven once in a dedicated production/coverage run. (b) Keep gate 3 and raise resources and the bank cap per mission; this changes the economy feel of these missions. (c) Keep everything; FC/BT/CE cannot be accepted.
**Recommendation.** (a). The economy stays as designed, and every type still gets exercised.
**Meanwhile.** No gate change. Lane runs continue to record `unused=`. The S019 pacing (10.7%) is handled as a separate pacing item.
- **Update (program lead, 2026-11-19).** City Crossroads FC S031-S036 have 4/4 counted Mac wins each (lane 38 `program-logs/2026-11-18/lane38/q/unity.log:74155`, lane 40 `program-logs/2026-11-19/lane40/counted/unity.log:227513`) at 45-86% of the deadline, with menu journey and return Passed and 0 AriaBlocked. Gates 1, 2 and 4 are met, so gate 3 is now the only thing holding six missions. With option (a) they become acceptable as soon as an objective-relevant role set is defined and one coverage run proves the rest. The air/combined rows S033-S036 also used no aircraft, so option (a) needs a rule on whether aircraft count as objective-relevant on FC air rows. Lane 41 is running counted seeds for 10 Desert Base BT/CE rows, which will add to this group. This is ready for the daily decision.

## Convoy Escort wins land under the 20% gate-2 floor; City Crossroads BT is borderline (program lead, 2026-11-19)

**Question.** Gate 2 needs wins at 20-70% of the deadline. Every CE row so far wins at 11-14% of its 1080 s Standard deadline. How should the
CE family meet the band, and should City Crossroads BT be retuned?

**Evidence.**
- Desert Base CE, Mac lane 41 counted (`program-logs/2026-11-19/lane41/a/unity.log`): 13 wins, all ConvoyDelivered 2/2 lost=0, at 11.42-14.34%
  (115-155 s). S019 130365 en 11.47% (:14184), S020 13.59-13.75%, S021 11.48/11.62%, S022 14.00-14.34%, S023 11.42/11.51%, S024 13.97-14.10%.
- Mountain Pass CE W23b (non-counted, Windows): 6% (destination before the defenses; W23c moves it past them).
- Desert Base BT in the same run: 24.22-35.34%, inside the band. City Crossroads BT W23b (non-counted, Windows): 16.06-20.62%.
- Deadlines are per mission (`Scenarios/SNNN/SkirmishObjective_SNNN.asset`: standard 1080 / war 1500 / large war 1800), but every row copies
  `MATCH_SETUP.md`'s 18-minute Standard rule, so a change is a family rule for 18 CE rows (S019-S024, S043-S048, S067-S072).
- CE ends on delivery or truck loss, not on attrition; a timeout never decided a CE run.

**Options.**
1. CE family deadline: Standard 1080 -> 480 s (war 667, large war 800, same ratio). The measured wins land at 24-32%. One data change per CE row
   plus the matrices/tests that pin the value; owner direction 2026-11-06 allows deadline revisions.
2. Keep 1080 s; make CE harder (longer route through the defenses, more ambush) until delivery takes 216+ s. More design work per map; risks turning
   wins into truck losses rather than slower wins.
3. Change gate 2 for CE: measure against a per-layout target time; the deadline stays a safety limit.

**Recommendation.** Option 1 for CE: it keeps the gate and the convoy route design, and a shorter clock is what makes an escort tense. City Crossroads BT:
decide after its Mac counted runs (the CC BT merge is pending the Mac Editor); if they stay under 20%, apply the same treatment (Standard ~900 s).

**Meanwhile.** No deadline change. CE rows keep running; their wins are recorded with their percentages and the rows stay not accepted on gate 2.

## DECIDED by the owner (Farhad, in chat, 2026-11-19): program-lead recommendations 1, 2, 4, 5 and the workflow changes; Airport rule changed

Owner's words: "regarding my decisions and recommended workflow, do your recommendations. But for number 3 \"apply your S004 rule (helipads and
helicopters only) to every map.\" I prefer we have Airport. Investigate if we can change the map to add the airport or even expand the map to add the airport."

1. **Gate 3 on objective missions (FC/BT/CE):** ARIA must use every unit type in the player's starting force, plus any type the objective requires.
   Production-only types, Airport and aircraft that the start does not include do not count. Base Assault keeps the full rule.
   Evaluator: `Tools/Missions/gate3_objective.py` (program lead, Mac), applied to the logged `[SkirmishGateUnits]` lines.
2. **Pacing:** agents set a per-family deadline so measured wins land at 25-50%, under the 2026-11-06 budget-revision rule
   (record old/new values, update the mission data and the matrices together). First: Desert Base CE and City Crossroads BT (W31).
3. **Airport: NOT the S004 helipad rule.** The owner prefers an Airport. Investigate changing the map, or expanding it, to fit an Airport on City Crossroads,
   Mountain Pass and Industrial Basin. This is the explicit owner request that `CAMPAIGN_MAP_REUSE_POLICY.md` requires for physical map work, and it
   covers investigation now. Building it waits for the findings. S004 keeps its earlier decision.
   Owner follow-up (same chat): "its acceptable a map without airport but not all without". So one of CC/MP/IB may go without an Airport,
   but not all of them: the study ranks the three by Airport feasibility and recommends which to change. Rows on a map that stays without one use helipads.
   Owner follow-up: "you can consider small airport as well. for example some maps with big airport that a transporter can land and smaller
   airport for jets and drones". The study also sizes a small airport (fighter/strike/drone, no transport_plane) per map.
4. **Standing OK for shared ARIA and test-tool fixes** that come with passing tests and change no unit stats or game rules. Windows' own rules require the owner's
   words in the Windows session; the program lead forwards this quote, and Windows confirms with the owner there.
5. **Map fallback:** when the named Campaign source cannot fit a mission, use an existing map that already works (Desert Base or City Crossroads) and keep the
   theater name. Record the fit evidence.
Workflow: Mac counted runs only after Windows' non-counted runs land in the pacing band; merges only when the lane is idle and its commit is in; one
combined owner list for shared-code requests. Certify-by-family (fewer counted runs per sibling): the owner agreed with the recommendation ("ok so go as you recommend"). Keep 4 counted runs per mission for now; revisit only if Mac time becomes the limit.

## DECIDED by the owner (Farhad, in chat, 2026-11-19): Airport plan per map ("Approve the plan")

Study: `Design/Orchestration/notes/airport-map-feasibility-2026-11-19.md` (offline; every site still needs a native Airport-only layout dump).
This is the explicit owner request the map reuse policy requires for the new building type and the new physical derivative below.
- **City Crossroads:** small airport (120x40, fighter + strike; no transport_plane) on both sides. Clone `OperationMap_CityCrossroads` into a
  Skirmish-only logical window with extended bounds (playable 804-1300 x 156-875) over existing terrain; the original asset and S025 stay untouched.
  One "CC air" layout for its 16 air/combined rows.
- **Mountain Pass:** full Airport (210x55) on both sides via route B2: a Skirmish-only enlarged copy of the Citywide Alert crop (new MapId and folder; never
  re-run `CH05M01CitywideAlertConfigBuilder` in place; CH05-M01 protected-source hashes unchanged). Check whether the extra depth also fixes the MP BT/CE
  endpoint source fit (W23c), which would replace the Armor Break move (W26).
- **Industrial Basin:** stays helipad-only ("a map without airport" is allowed).
- **New building:** a small-airport variant of Building_Airport with its own structure id and mappings (producer, eligibility, enemy AI build choice, gate
  model). Drones stay on the satellite dish. Transport_plane is removed through the per-map army profile.

## DECIDED by the owner (Farhad, in chat, 2026-11-19): Operations decisions per the program-lead recommendation; mockups for review

Owner's words: "regarding operation decisions. go for your recommendation. just for number 3 Mockups, show me the mockups I can approve. So far we used codex
imagegen for the mockups, we can use the same." Survey: `Design/Orchestration/notes/operations-readiness-2026-11-19.md`.
1. **Definition of done (test runs):** the same counted rule as Skirmish per mission (3 EN + 1 FA ARIA wins on Regular, menu journey, result/return).
   ACCEPTANCE.md's other checks stay (Partial, Defeat, Withdraw, checkpoint restore, exactly-once settlement). Instead of the full 5-seed x every-difficulty
   matrix on every mission, each family's first mission also runs one Easy and one Hard win.
2. **New mechanics are approved:** Repair/Interact commands, civilian escort/release and the missing unit roles, as additions. Existing unit stats and Skirmish/Campaign
   rules stay unchanged.
3. **Mockups:** Codex ImageGen boards for the new family HUDs (ESCORT O002, REPAIR O003, RESCUE O005). Each goes to the owner for approval before implementation.
4. **Build order:** the free intro O001-O003 first. Extending existing tools (an Operations job format in run_aria_queue, a draft format) is allowed.
The trigger for Operations production stays: one accepted Skirmish mission per objective type. Offline prep (drafts, tool extensions, a gap table) may start now.
- Owner approved the three Operations HUD mockups v01 (O002 escort, O003 repair, O005 rescue) on 2026-11-19: "Approve all three". Board: `Design/AgentReports/OperationsMockups/review.md`.

## DECIDED by the owner (Farhad, in chat, 2026-11-20): match UI polish, six boards approved
The owner reported inconsistent match UI while missions were being built: left panel padding, off-style centre info panels,
empty or bare ARIA panel areas, uncolourful YOUR BASE / ENEMY BASE, placeholder Operations ground markers, and wanting high-end
selection and target markers. Codex ImageGen boards: `Design/AgentReports/MatchUiPolish/review.md`. The audit also found
NEUTRA/L wrapping, S041/S046 frame overflow, the S026 stacked bar, the minimap overlap and the ping looking like the ring.
- Owner: "Approve all six" (2026-11-20). Implementation runs alongside mission work as Codex lab tasks:
  ui-a (boards 1+3), ui-b (2+4) and ui-c (6). ui-d (board 5, Operations markers) waits until the O001 triage and W38 merge.
  Each task needs native EN/FA captures compared with the mockup and an ARIA regression on accepted missions before merge.
