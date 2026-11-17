# ARIA FindBase Blocked and S004 start stop (lane 15 repro), 2026-11-17

Branch missions/aria-defence-ranged, stacked on 7290b1ced.

## r2 S002 155923 en: Abort ariaStopped:Blocked at match 204.0 s
Gestures (time = observation clock):
- 213.41 Attack -56152 (act 92)
- 215.16 FindBase -61514 (1255, 912.5), att=1 (act 93)
- 216.93 FindBase -61514, att=2 (act 94)
- 217.15 `column_recovery state=find_base_given_up findBaseGiveUps=1`
- 218.70 FindBase -61514, att=3 (act 95)
- 219.57 `event=blocked reason=115 intent=FindBase target=-61514 attempts=3`, with attackMode=True and enemyHealth=0

Cause:
1. The search gave up correctly after two presses (MaxSameControl).
2. Its single Attack-off press never went out: Attack was not offered that frame, so the decision was Unavailable.
3. The next frame Attack was still armed, so the planner re-entered the search with a fresh count and pressed -61514 again.
4. The input layer refuses the fourth consecutive press of one target (AriaPlayDecisionSystem, 115).

The same branch exists in the generic planner (S026/S049/S050/S006/S004).

## r2 S004 104733 warm-up: Abort ariaStopped:Blocked at match 12.5 s
- `[S004AriaConsent] result=Passed` (Play, Confirm). The run evidence (`s004-aria-rs-104733-en-3.jsonl.input.jsonl`) shows phase=Starting, actions=0, attempts=0, stopReason=0 at 8.5 s and 10.5 s.
- It is not FindBase: ARIA never left Starting. AriaPlayInputSystem gave a start only 3 s after it was due. In that window the start fails when:
  - the observation is stale (more than 5 frames, which sets code 4);
  - or `touch.Start()` is refused (driver already running, or any touchscreen contact, mouse button or key held). That path set no stop code, so the logged reason was 0.
- S004 isn't traced (traces are S002-only), so which condition fired is not in the log.

## Fix
1. **AriaFindBasePolicy.Recover / AriaFindBaseRecovery** (both planners, through StepFindBaseRecovery). After a give-up:
   - Attack is toggled off through its own control, at most 2 completed presses; the second only after a 1.5 s settle;
   - otherwise ARIA waits (Waiting, not a press);
   - the search may resume after 20 s.
2. **SameControlExhausted:** the search never asks for a third consecutive press of the control the input layer last pressed. It reads the session's target and attempts, so the count survives planner resets. Exhaustion counts as a give-up and goes to recovery.
3. **Start (AriaStartPolicy):**
   - the start is retried for 15 s instead of 3 s, then stops with a code: 4 stale observation, 117 start refused;
   - `[AriaStartBlocked]` logs stale, refusals, waited and the devices holding a press;
   - ARIA's own synthetic touchscreens ("AriaDemonstrationTouch", e.g. the consent probe's) no longer count as physical input in the start check;
   - every Blocked stop now logs `[AriaBlocked] reason target attempts actions kind offered goal intent` for all scenarios.

## Tests
- `AriaFindBaseRecoveryTests` (6):
  - a replay of the r2 pattern with Attack never offered: no 115 in 200 s, never a third press, the search retries;
  - the bounded disarm with settle;
  - waiting when Attack is not armed or not offered;
  - the same-control count;
  - the start grace and codes;
  - a source scan of both planners, the driver and the input system.
- All 54 standalone policy tests pass. The Unity compile has not run.
