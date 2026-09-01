# SCN-10 Unit Command Wheel — Iteration 01

Status: review-frozen after deterministic and live runtime validation at 1920×1080 and 4800×2160.

Target locks:

- `reference/SCN-10_UnitCommandWheelV3_Final_Target.png`
- `reference/SCN-10_UnitCommandWheelV3_Targeting_Final_Target.png`

Matched in this iteration:

- Six sharp procedural radial sectors with visible top-to-bottom gradients and constant 3 px outer borders.
- Separate Black Hawk detail card using the existing helicopter portrait; no replacement unit art was introduced.
- Fresh V3 icons for the wheel, footer rail, left actions, resources, system controls, unit stats, and status badges.
- Base layout keeps the Black Hawk card beside the centered wheel.
- Attack-targeting layout moves the Black Hawk card down-left and the wheel right, matching the target composition.
- Narrow targeting rail is anchored at the far right and does not collide with the wheel.
- 20:9 layout fills the full canvas: selection panel flush left, ARIA flush right, footer rail fills the available width.
- ARIA and all unit portraits preserve aspect ratio.
- Runtime interaction path is verified: live portrait click opens the live wheel; live Attack click enters targeting presentation.

Validation evidence:

- `build-and-capture.log`: prefab build, foundation validation, and four deterministic captures passed.
- `focused-validation.log`: `[UnitCommandWheelV3Validation] result=Passed tests=3`.
- Four live capture logs passed before the images were frozen: base and targeting at both supported aspect ratios.

Capture note:

- The deterministic images use the checked gameplay capture background for visual-lock comparison.
- The live Menu-route harness intentionally does not load a gameplay world, so its background is black. Those live images validate the actual runtime-mounted UI, responsive layout, and click path; the match scene supplies gameplay behind the same transparent HUD at runtime.

Asset reuse:

- Unit/building images remain the existing runtime catalog art.
- UI icons are packed through `UI_V3_MatchIcons_01.spriteatlas`.
- The rejected opaque/checkerboard icon source sheet was removed; only the accepted transparent source sheets and split atlas inputs remain.
