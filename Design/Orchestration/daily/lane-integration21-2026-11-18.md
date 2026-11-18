# Lane worker 21, 2026-11-18

Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane21/

- Step 0 (menu journey, current integration): S004 104733 en Victory 179.8 s; S002 130365 fa-IR Victory 227.2 s (21.04% of 1080), gate-3 unused= (empty), MenuJourney Passed, TerminalFrozen Passed, playerBaseHp=800, base_alert onset t=100.3 and 218.2, home_defense triggered/ordered, result_return_ok=yes.
- Step 1: merged missions/capture-fixes (a3e3953d9) and missions/longsession-reason (6a945d2e2), compile clean after each; python 148/148; EditMode SkirmishLongSessionRegressionLogTests 4/4; SkirmishCameraFramingTests Passed tests=7.
- Step 2: after captures (S026 104729, S002 130365) at 2400x1080 and 1920x1080 Captured, no ownAirAboveCamera=1 frame. S026 helicopter capture failed twice: helicopterSquadCardNotFound (purchase and delivery worked; squad page AirMask empty at stage 104, no wait before failing). Needs a capture-code fix (wait for the squad card to appear).
- Capture lesson: capture.sh needs WARLINE_ARIA_QUIT=1 or the Editor never exits after Captured.
- Step 3: Farsi spot capture skipped; capture entries hardcode locale en.
- Step 4: long session S026 104729: Regression Passed ... purchases=7 purchaseFailures=15 purchaseRefusedByCap=15, Draw TimeLimit 1080.0. All 15 purchaseSkipped: reason=ArmyCapacityReached field=capacity army=48/48 (47/48 for the last 4) supply=62/128 (57/128) materials=420 interactable=0.
