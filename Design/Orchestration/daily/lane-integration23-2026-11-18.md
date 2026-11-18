# Lane integration 23 — helicopter and Farsi captures (2026-11-18)

Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane23/`.

## Step 1: merge and tests
- Merged `missions/capture-fixes2` (8b902d56f) with `--no-ff` as 2893cd8c2.
- Compile check: `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed` (compile.log:1300), 0 `error CS`.
- `[SkirmishCameraFramingTests] result=Passed tests=10` (tests.log:484); the Editor exited by itself.

## Step 2: S026 helicopter capture at 2400x1080, FAILED twice
- Run 1: `[S026HelicopterFraming] result=Failed reason=returnCommandNotObserved input=Touch aria=Off samples=225 visualReview=Pending` (heli.log:6554).
- Run 2: the same reason with samples=233 (heli2.log:6864). Wrapper exit 2 both times; the Editor exited by itself.
- What now works: purchase and delivery of `Unit_Veh_Helicopter_Transport`, the squad card found by group (`squadCard slot=1 group=24 page=2`
  after three TrayScroll touches), and stage 105 HUD Move. It ran for the first time: `input=Touch command=Move` was followed by `airborne=1`.
- Stage 107 failure: the probe alternates CommandWheelCue (235,888) and ReturnSector (1072,728) touches for 120 s. Each pair plays
  `VO.ARIA.Message.TacticalFeedbackUnitReturningToBase` (48 times in run 2), so the game accepts the Return command.
  But the probe's check `air.ReturningHome != 0` never becomes true. This needs a capture-code look: a transport heli's
  return may not set `ReturningHome`, or it may be cleared at once.
- Frames written in both runs: 00 return-from-recruitment, 01 takeoff, 02 cruise, 03 switch-back-ground, 04 landing.
  **No frame has `ownAirAboveCamera=1`**; takeoff, cruise and switch-back have ownAirVisible=1.
  Run copies: `lane23/heli-run1/`, `lane23/heli-run2/`. The committed set is run 2. The jsonl also keeps the run 1 and older rows.

## Step 3: Farsi ground captures at 2400x1080 (`WARLINE_CAPTURE_LOCALE=fa-IR`)
- S002 seed 130365: `result=Captured mode=ground catalog=S002 locale=fa-IR label=after` (s002fa.log:14241).
- S026 seed 104729: `result=Captured mode=ground catalog=S026 locale=fa-IR label=after` (s026fa.log:3978).
- Visual check (Read tool) of `S002-130365-2400x1080-fa-IR-ground-default.png` and `S026-104729-2400x1080-fa-IR-ground-opening.png`:
  the Farsi HUD text renders properly, with joined letters and right-to-left order, and no boxes or mirrored glyphs.
  The command bar, ARIA panel, squad card and objective strings are all readable. The digits are Latin.
- Observation (S026 opening): the 'محل تجمع' (rendezvous) objective chip is drawn over the settings and pause buttons in the top bar.

## Not established
Visual review by the owner, phone acceptance, and a passing helicopter capture.
