# ARIA intercept worker — 2026-11-17

Goal: S002 (Base Assault, Desert Base) wins 130365 en, 104731 en, 155923 en, 130365 fa-IR with ARIA at normal input.
Before: 155923 en loses at 95.5 s (enemy lone tank slips past the column ~55 s, kills the base from 67 s).

## Diagnosis notes (from probe-seeds1 log and actor trace)
- ARIA trace `time` is unscaled time, about sim time + 14 s on 155923 (frame 2039 = sim 55.12 s, ARIA ~69.3).
  So in sim time: rocketeers (page 1) ordered at ~50.6 s; full-map inspection 52.4–54.2 s; deselect toggles 56–59.6 s;
  page swipes 61.4–64.5 s; base damage 67 s; DefendBase gestures from 68.6 s; group 0 ordered ~75.5 s.
- `homeThreat` stayed False the whole time, even while the tank shelled the base from ~25 units: the tank was never a
  presented minimap contact near home (fog: no own observer in acquisition range). Presented-contact home defence cannot
  see it; only HUD base damage fires, too late.
- Visibility = own unit acquisition radius (SkirmishFogService). The tank is fog-visible while it crosses the column
  (rifles/rocketeers engaged ~55–57 s).

## Change (branch missions/aria-intercept, commit 1ae08e59a)
- `AriaInterceptPolicy` (pure, Game.Simulation): while the army carries an accepted assault order, a fog-visible hostile
  vehicle crossing the column (≤40 of an own unit) or slipped behind the army's median distance to home (≤60 of an own
  unit), and ≥60 from the enemy base, is attacked by one group via normal controls: reuse a visible anti-armour selection,
  else select the anti-armour card on the page (never aircraft) → Attack → tap the vehicle (-20002). Hold up to 12 s
  (released when out of sight 4 s), then the ordinary sweep re-issues the base order. Busy owners win; ≤3 per match.
- Public projection of the slipping vehicle (SkirmishAriaPublicProjection, fog-visible only) → UiSkirmishModel →
  watch observation `InterceptThreat` (on-screen, reachable tap only).
- Trace: `[AriaS002Trace] event=intercept state=triggered|ordered|released|contact ... t=<match s>`.
- Tests: `Assets/Tests/Editor/SkirmishExpansion/AriaInterceptPolicyTests.cs` (5). Ran standalone with dotnet + NUnit shim:
  5/5 pass. Game.Simulation + Skirmish.Contracts compile standalone. Unity compile check pending (lane).

## Risks
- Needs the tank on screen at the crossing moment; if not, `event=intercept state=contact known=True offered=False`
  lines will show it and the rule does not fire.
- On winning seeds the rule may fire on the same tank crossing; cost ~3–4 gestures plus a short hold.

## Lane runs
### lane-s002gate (run by the lane worker, 1ae08e59a merged) — gate 0, not counted in my 3
Log `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane-s002gate/unity.log`, summary 1614115–1614160:
155923 en Victory 272.8 s; 130365 en Victory 266.7 s; 104731 en Victory 256.2 s; 130365 fa-IR Victory 261.1 s (all countedWin=1).
The rule never fired: 20 `event=intercept` lines, all `state=contact ... offered=False stage=0`. Known windows: t=53.97–56.88
(43075/44995), 54.77–58.28 (457461/460102), 54.44–56.93 (809897/811745), 54.03–57.40 (1227543/1229934). 155923 was won by
the column killing the tank, not by the rule.

Cause: (1) the tap point was never on screen — the camera sits on the advance point ahead of the column; (2) the median-based
"behind" test ended the known window after ~3 s; (3) at 56.88 the same tank was a presented home threat, which was Busy.

Fix e8b2b94b2: with Attack armed, a normal camera drag from the clear view centre toward the known vehicle (-20015; in Attack
mode a drag pans and never orders), then the tap; candidate = fog-visible vehicle within 60 of an own unit and within 40
(crossing) or closer to home than that nearest unit; presented home threat no longer blocks it (home defence already running
still does). Trace adds `pan=`, `screen=(x,y,z)`, `safe=`, `state=pan`. 8 policy tests pass standalone.

### aria-intercept-q1 (iteration 1, e8b2b94b2 merged as 68dfb689f) — owner pause arrived mid-queue
Compile: aria-intercept-compile exit 0, line 1779 `result=Passed`, no `error CS`.
Queue (the tool rejects duplicate jobs, so 155923 fa-IR stood in for the second 155923 sample):
- 295620 155923 en countedWin=1 — Victory 233.92 s
- 880032 155923 fa-IR countedWin=0 — Abort ariaStopped:Blocked 370.9 s (Blocked reason 101, line 879958)
- 1353583 130365 en countedWin=0 — Abort ariaStopped:Blocked 194.0 s (reason 115, FindBase -169578, line 1353509)
- 104731 en started 1353928; the Editor shut down mid-job after an asset refresh (1474919). No harness line, no queue Completed marker.
- 130365 fa-IR not run. queue.out: parsed 0, missions.csv not updated.
Intercept traces: triggered 6, pan 4, released 6, ordered 0, contact 24.

Diagnosis (read-only, no code changed under the pause):
1. At t≈55 the tank projects to screen y≈3037 (screen 1080 high): about 2.8 screens off view. A drag clamped to about 300 px
   cannot bring it in before it leaves sight (released 3–4 s later each time). Repeated drags on the same target id count
   as touch retries (attempts 1→3 at 684035–701889), so more drags would block ARIA (StopReason 115).
2. Every release left Attack armed (attackMode=True). In 130365 en the next owner (FindBase) then failed 3 times → Blocked 115.
3. The 155923 fa-IR Blocked 101 at ~370 s had an intercept at t=119–131 earlier; not yet tied to the rule.

If work resumes: disarm Attack before any release (tap Attack again); vary the pan target id per drag or credit pan
progress so drags are not retries; use larger drags or a minimap/camera focus; or revert 1ae08e59a/e8b2b94b2 on
missions/integration (worse than the 4/4 baseline). Lead's decision.

## Iteration 2 (code only, commit e3387c8ae on missions/aria-intercept)
Branch synced: merged missions/integration (0c7602308), then restored the rule by reverting 3e9c744b7 on the branch
(a685f5ccb). Merging the branch into integration brings that restore; do not also revert 3e9c744b7 on integration.

(1) Blocked aborts in aria-intercept-q1:
- 130365 en, Blocked 115 at line 1353509 (intent FindBase, target -169578, attempts 3, attackMode=True): the intercept
  released at t=180.42 (line 1319990) with Attack armed and group slot 0 selected; the base-assault step then aimed the armed
  Attack at the enemy focus control three times. All 6 q1 releases had attackMode=True. Fix: stage 6 toggles Attack off
  before hand-back (bounded 4 s).
- 155923 fa-IR, Blocked 101 at line 879958 (~388 s): the planner progress watchdog (StopReason 101), with objective progress
  stuck since about 208 s. The army was at the base (distance 20.9) with rifles only (page 1 assault mask 0); no structure
  force remained. Earlier, the intercept at t=119-131 (lines 670433-706323) detoured structure group slot 4 with three
  drags (attempts 1-3) and then a FindBase map navigation. That is a plausible contributor, but the log does not prove it.
  With one-drag admission that intercept would not start.
(2) Never ordered: 6 triggers, 4 pans, 0 ordered. At t=54-56 the crossing tank projected to screen (-298..-438, 3032..3043),
about 2.8 screens off view (z≈240), because ARIA's camera sits on the advance point ahead of the column. The HUD minimap is
non-interactive except opening the full map, so moving the camera costs 3 gestures (~5.4 s) plus Arm and tap; the tank stays
visible about 4.5 s (known windows 54.05–58.56, 55.93–59.28, 54.81–59.86). It cannot be ordered inside that window from that
camera. Fix: offer only tappable or one-drag-away contacts, with at most one drag. Consequence: at the 55 s crossing the rule
will not start unless the camera already shows the column. A rule that orders there needs ARIA to keep the camera on the
column during the march (a camera-policy change, for the lead to decide).
(3) Editor exit during job 4 (104731 en): not a crash (the only Unity crash report today is Unity-2026-11-17-100054.ips at
10:00:54), not a timeout (7200 s limit; it ran about 22 min), and no harness abort line. The last lines are an asset refresh
with no changes (line 1474919, "RefreshV2(NoUpdateAssetOptions)", new=0 changed=0, the kind triggered when the Editor
window regains focus), then a normal shutdown (1474941–1474951 "Application is shutting down"). queue.out ended 10:45:46,
44 s after the program pause commit at 10:45:02 (c60bb10e0). Most likely it was closed by hand at the pause.
Tests: 11 pass standalone (dotnet + NUnit shim); Game.Simulation builds. Unity compile not yet run.

### Column camera (lead-approved "Home OK"), commit 481eacbac
After the visible page's assault groups and the anti-armour groups carry their order, ARIA taps the HUD Home button once
(skipped if the column is already framed), then keeps the marching column (own mobile units more than 40 from home, supply
vehicles excluded) in the middle third with ordinary drags. Each drag starts on the far side of the centre, so one drag
covers about 58 world units sideways or 24 forward. Rules: at most one camera action every 2.5 s; yields to every other owner;
drags alternate controls -20016/-20017 (never touch retries); at most 12 drags; stops at the enemy base; after 2 re-finds of
the Advance point (existing map navigation, each step a distinct control) it stops for the match (today's behaviour).
The intercept runs first; the Attack-disarm fix is kept. Trace: `event=column_camera state=home|home_skipped|recentre|refind|done|settled`.
14 policy tests pass standalone. Expected in the next queue: `column_camera state=home` at about t=51–53 on each job, then the
crossing tank `event=intercept ... offered=True` or `pan=True`, then `state=ordered`.

## aria-intercept-q2 (iteration 2 of 3, 481eacbac merged as 2cf24f34b; reverted as 88d29f828)
Compile aria-intercept-compile2: exit 0, line 1950 `result=Passed`, no `error CS`.
Log `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/aria-intercept-q2/unity.log`, queue summary 1024785–1024860
(`result=Completed jobs=5 recorded=5 wins=3 sourceUnchanged=1`). Match seconds are runs.csv duration_seconds (candidate feb06b53…).
- 32286 155923 en Defeat (queue 164.6 s; match 98.75 s)
- 246083 155923 fa-IR Victory (queue 284.3 s; match 235.27 s)
- 283393 130365 en Defeat (queue 138.8 s; match 97.80 s)
- 585431 104731 en Victory (queue 262.9 s; match 226.17 s)
- 1024668 130365 fa-IR Victory (queue 261.1 s; match 230.74 s)
Traces: column_camera home 5, recentre 24, refind 5, settled 28, done 3; intercept contact 19, triggered 0, ordered 0.
Cause (155923 en): Home was tapped at t=27.5, during the sweep. The `PageOrdered` + `StructureOrdered` condition was already true
on an early page. After Home, the projected column point was unstable (screen (-3815,8886,z=-10.4), then (78379,-248487,z=0.2)):
points near or behind the camera plane. At t=55.2 the sweep had to re-find the Advance point (`refind`), which moved the camera
away exactly at the crossing. The crossing tank was never offered (`known=True offered=False pan=False`, screen z<0). Home defence
fired on base damage at t=70 and the base fell at 98.75 s. Worse than the 4/4 baseline, so the merge was reverted (88d29f828);
integration Assets equal ecaf17a07 again (rule 1ae08e59a only).
Next, if a third iteration is allowed: tap Home only after the whole sweep is done (`AssaultIssued`), never mid-sweep;
reject projected points with z below the near plane or far outside the screen before treating them as drag targets.
