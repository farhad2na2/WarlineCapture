# Lane integration 25: ARIA FC/BT/CE first runs, HUD chip, helicopter capture (2026-11-18)

Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane25/`. Every Editor showed `Library Redirect Path` within 5 s.

## Step 1: merges
- `missions/hud-chip-heli-return` (4e0782d6d, d3c4cd1bb) merged --no-ff as b062cc058. Compile clean (compile1.log:1591, 0 `error CS`).
- `missions/aria-objective-skills` (538ce0663..ddb142ccb, five commits) merged --no-ff as e05c44059. Compile clean (compile2.log:2066, 0 `error CS`).
- Python `unittest discover` (Tools/Missions): 149 OK (pytests.txt).

## Step 2: Unity tests (no `-quit`; I stopped my own verified Editor after each marker)
- `[SkirmishHudChipPlacement] result=Passed` (chip.log:483). The class has no namespace, so the brief's `Game.Tests.Editor.` name does not resolve. I ran it as `SkirmishHudChipPlacementTests.RunFocusedValidation`.
- `[AriaObjectiveSkill] result=Passed controls=15` (ariaobj.log:485).
- `[SkirmishTerminalObjectiveEvidence] result=Passed controls=5` (termobj.log:484).
- `[SkirmishCameraFramingTests] result=Passed tests=10` (framing.log:483).
- EditMode run (regress.xml): AriaBaseAlertDefenseTests 6/6, SkirmishMissionHudGuidanceTests 2/2, SkirmishResultHonestyTests 6/6.

## Step 3: ARIA first runs (`--menu-journey`; queue log fcbt/unity.log, per-job extract fcbt/job-extract.txt)
All five MenuJourney lines Passed, each through the objective tab (ObjectiveTab_BA/FC/BT/CE).

| Job | Result | Match clock | % deadline | Terminal objective | Return |
|---|---|---|---|---|---|
| S004 104733 en (warm-up) | Victory | 180.9 s | 16.75% of 1080 | n/a (BA) | probe Passed MAIN MENU |
| S007 130365 en | Victory TicketsDepleted | 589.8 s | 49.15% of 1200 | zones 2/1, tickets 500/0 (:430650) | probe Passed (:430929) |
| S013 130365 en | Victory BreakthroughEvacuated | 301.8 s | 27.94% of 1080 | completed 8/8, lost 0, exitOpen 1 (:436795) | probe Passed (:437088) |
| S019 130365 en | Abort ariaStopped:Blocked | 133.3 s | 12.34% of 1080 | outcome=None, completed 0/2, lost 0 (:451915) | result_return_ok=unproven |
| S007 130365 fa-IR | Victory TicketsDepleted | 645.6 s | 53.80% of 1200 | zones 2/1, tickets 463/0 (:461245) | probe Passed (:461511) |

- Gate-3 `unused=`:
  - S007 en: helipad, satellite_dish, apc_fast, apc_heavy, breacher, drone, gunner, marksman, radar, rocketeer, siege, tank, transport_heli.
  - S013: the same 13 types.
  - S019: the same list plus convoy_logic and tanker, minus nothing.
  - S007 fa-IR: the same as S007 en but without rocketeer (12 types).
  - ARIA used: apc_armored, car, rifle, tanker, and rocketeer in fa-IR. S019 used apc_armored, car and rifle.
- Frame and block events:
  - S007 en: 6 `frame_missing frameReason=4 prior=popup` (t=39.2, 54.2, 64.5, 100.8, 355.7, 418.5).
  - S013: 4 of the same kind (t=17.4, 33.9, 69.2, 173.1).
  - S007 fa-IR: 6 of the same kind (t=30.6, 56.7, 73.2, 83.4, 300.9, 516.5).
  - None of these three runs had `frame_lost`, `order_blocked` or `[AriaBlocked]`.
  - S019: 15 `frame_missing frameReason=7 prior=proceed map=True`, from t=49.25 to t=129.97, each followed by `frame_restored`. Then `order_blocked failures=6 retryAt=159.3 t=132.11` and `[AriaBlocked] reason=101 attempts=0 actions=47 kind=Waiting intent=EscortOriginalTruck frame=4138` (:451884). ARIA keeps opening the map during the escort, and the objective frame is missing while the map is up.
- Last `[AriaObjective]` lines:
  - S007 en: Hold orders accepted (t=549-581), then recruit ReturnAircraft.
  - S013: score completed 4/8, then a rifle recruit at t=297.
  - S019: frame_missing/frame_restored pairs, then order_blocked.
  - S007 fa-IR: Hold order accepted, then score 463/16 at t=630.
- fa-IR result text: `[SkirmishNativeResultLocalization] result=Passed locale=fa-IR` (:461271). No `resultNotLocalized`, and no Latin text on the result card.

## Step 4: S002 regression
`S002:130365:en`: MenuJourney Passed (s002/unity.log:2269). Victory in 228.5 s (21.15%). `unused=` is empty, and the terminal probe Passed (:248146).

## Step 5: captures at 2400x1080
- Helicopter run 1 was launched without `WARLINE_REVIEW_WIDTH/HEIGHT` and `WARLINE_CAMERA_FRAMING_LABEL`. It wrote 1920x1080 frames to `CameraFraming/unlabelled`. I moved them to `lane25/heli-run1-1920-unlabelled/` and kept them. That run still reported result=Passed (heli.log:3612).
- Run 2 (label=after, 2400x1080) passed:
  - `command=Return observed=ReturningHome afterSeconds=0.30` (heli2.log:3411)
  - `touchdown=1` (:3505)
  - `result=Passed reason=paidHelicopterRecruitReturnMoveCruiseReturnTouchdown samples=36` (:3547)
- Run 2 frames: 00 return-from-recruitment, 01 takeoff, 02 turn (new), 03 switch-back-ground. The 02-cruise and 04-landing files in `after/` are lane 23 leftovers.
- No frame has `ownAirAboveCamera=1`.
- S026 ground captures:
  - fa-IR: `result=Captured ... locale=fa-IR label=after` (s026-fa-IR.log:3819).
  - en: `result=Captured ... locale=en` (s026-en.log:3821).
  - Wrapper exit 2 for both (ARIA quit). Both Editors exited by themselves.
- Visual check (Read tool) of both opening PNGs and the fa-IR default PNG: the gear and pause buttons are now clear. **But the RENDEZVOUS / 'محل تجمع' chip is not drawn at all**, neither on its anchor nor clamped into the play area with an arrow. The overlap is gone because the chip is hidden, so the player loses the rendezvous guidance. Likely cause, not verified: `PlaceWorldChip` returns false (no safe play area, or `TryPlace` fails), or the anchor is off-screen.
- Captures committed in 466242a2b.

## Not established
- Owner visual review, phone acceptance, and a visible RENDEZVOUS chip.
- An S019 ARIA result (it blocks at 133 s).
- One seed per FC/BT mission only; pacing is not established.
