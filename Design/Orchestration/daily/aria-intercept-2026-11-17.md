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
