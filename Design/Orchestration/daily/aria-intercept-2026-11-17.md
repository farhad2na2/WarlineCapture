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
(pending — waiting for "lane is yours")
