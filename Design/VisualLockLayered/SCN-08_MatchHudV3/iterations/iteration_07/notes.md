# SCN-08 Tactical Feedback — Iteration 07

Target lock: `../../reference/SCN-08_MatchHudV3_TacticalFeedback_Final_Target.png`

## Corrected

- Moved the hostile warning strip to its target-lock lane and preserved a clean gap before the right-anchored ARIA panel at both ratios.
- Center-anchored the shared V3 settings and pause sprites inside their header buttons and added builder validation for the centered geometry.
- Integrated the one shared ARIA portrait into a matching black telemetry stage so the opaque portrait source no longer reads as a pasted rectangle.
- Tightened the portrait bay to the target height and kept the portrait aspect-preserved.
- Replaced the tactical review's legacy `AlertCue` shortcut with the real embedded `AriaTutorialBriefingView`, including its bound `DO IT` and `SHOW ME` controls.
- Kept one permanent ARIA panel; the same panel conditionally shows tutorial copy/actions and always owns the minimap.
- Preserved the shared `UI_V3_MatchIcons_01` command set, procedural gradients, constant 3 px borders, and resolution-independent world feedback.
- Replaced the circular/leaf-like ground placeholder with reusable procedural perspective ellipse rings for the selected squad and hostile target.

## Proof

- `tactical_feedback_v3_16x9.png`
- `tactical_feedback_v3_20x9.png`
- `tactical_feedback_v3_live_16x9.png`
- `tactical_feedback_v3_live_20x9.png`
- `build-and-capture.log`
- `live-16x9.log`
- `live-20x9.log`
- `focused-validation.log`
- `tutorial-focused-validation.log`

Both live captures used the actual Menu → Match route, selected the real `AttackCommand`, and emitted `CanvasRouteCaptureValidation result=Passed` at `1920x1080` and `4800x2160`.
The command/selection regression passed 18 tests, including the new V3 ground-ring/no-placeholder check; the embedded ARIA tutorial regression passed 4 tests and confirmed one panel, two actions, and no Skip control.
