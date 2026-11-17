# ARIA DefendBase Blocked 115, S002 130365 fa-IR (Windows lane, 2026-11-17)

Evidence: `origin/missions/windows-lane` `Design/AgentReports/SkirmishExpansion/S002/_Evidence/candidates/f027bd14…/`
`s002-aria-rs-130365-fa-1.jsonl.input.jsonl` (decision rows) and `.jsonl` (base HP), summary
`Design/Orchestration/windows_lane_results/2026-11-17-1624.md` (log line 247331: `event=blocked reason=115 intent=DefendBase
target=-301842 attempts=3 screen=(722.60, 912.50)`).

## Which touch, and why
Base damage started at t≈72 (base HP 800 → 744 → … 296); `home_defense state=triggered reason=baseDamage round=1`. Decision rows:

| obs. time | target | step |
|---|---|---|
| 73.6 | -301842 FocusPlayer (home focus control) | stage 0/1, completes |
| 75.8 | -297326 previous roster page | stage 1 |
| 77.9 | 1073741825 card slot 0 | stage 3, one proportionate defender |
| 80.1 | -296288 Attack | stage 4, arms Attack |
| 82.1, 84.1, 86.2, 88.3 | -301842 FocusPlayer, attempts 0→3 | stage 4: Attack armed, HomeGround not offered |

`AriaHomeDefensePolicy` stage 4 pressed the home focus whenever its defence ground (`HomeGround`, base damage without a
presented contact) was not offered, with Attack armed and with no bound. Every focus tap completed, but the ground never became
available, and the fourth press of the same control stopped ARIA (reason 115). The same pattern stopped q1 today (FindBase
pressing a focus control with Attack armed after the intercept handed back). A camera-focus control pressed with Attack armed
does not re-centre the view; why HomeGround stayed unavailable on Windows fa-IR (clearance against home contacts, an RTL HUD panel
over the candidate points, or the focus not moving the camera) cannot be told from these files.

## Fix (missions/aria-defend-blocked)
- Stage 4 without a defence target: toggle Attack off once (a focus with Attack armed does not move the camera), press the home
  focus once (stage 10), then if a target is offered arm Attack and defend; otherwise give the round up.
- Giving up (stage 9): toggle Attack off once if still armed (never hand an armed Attack to the next owner), observe 4 s, then a new
  round. Rounds alternate controls, and the policy never issues the home focus twice in a row (the next round skips its opening
  focus when the last press was that focus), so no control reaches three consecutive presses and ARIA keeps playing.
- Stage 5: a ground tap that completes with Attack still armed is repeated once, then the round is given up (was unbounded).
- Fallback targets: base damage prefers HomeGround, then ThreatGround, then the visible contact (Threat); a minimap threat
  prefers ThreatGround, Threat, then HomeGround.
- Trace: `event=home_defense state=recentre|gave_up_disarm|gave_up` plus `recovery= attackMode= homeGround= threatGround=
  threat= focus=` on every home-defence line.
- Tests: `AriaHomeDefenseBaseDamageTests` (4 new; one simulates 120 s with the ground never offered and asserts no control is
  pressed three times in a row and Attack ends disarmed).

## Coverage recruits vs home defence
No collision in this run: the decision rows show no Recruit intent before DefendBase; the sweep was still on roster page 1 at
t≈71, so the breacher/coverage steps (which need the first assault and structure orders accepted) had not started. In general
home defence runs earlier in the step order, closes a drawer a purchase opened (DrawerOpen → CloseDrawer) before anything else,
and the breacher, coverage and support steps all treat a home threat or an active home-defence stage as Busy. Cost: a purchase
interrupted by a home threat is given up for the match (breacher stage 2 / coverage Done bit), not retried after the raid.
