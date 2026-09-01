# Settings V3 — Iteration 19

Target: `../../reference/POP-06_SettingsV3_Final_Target.png`

Files:

- `settings_menu_16x9.png`
- `settings_menu_20x9.png`
- `settings_match_16x9.png`
- `settings_match_20x9.png`

Corrections included:

- outer modal border, left tab-rail border, and active-page border are separate
  constant 3 px frames; none crosses or touches the tab buttons
- selected Audio tab, ON toggles, Reset, and Apply retain directional gradients
- popup scale is responsive: target 84% of live canvas height, capped at 76% of
  canvas width, so 16:9 matches the target proportions and 20:9 remains fully
  visible without clipping
- offscreen QA now simulates the same CanvasScaler Expand geometry as runtime

Iteration 18 was rejected because its 20:9 capture reused the Editor's 16:9
canvas and clipped the popup. Iteration 19 corrects that capture/runtime parity.

Review status: candidate only; final post-fix Play Mode confirmation resumes
after the host is unlocked, and acceptance still requires explicit user
confirmation.
