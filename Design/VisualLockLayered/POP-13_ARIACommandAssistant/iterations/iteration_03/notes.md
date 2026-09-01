# POP-13 ARIA Command Assistant — Iteration 3

Status: review-frozen; not user-accepted.

Target lock:

`../../reference/POP-13_ARIACommandAssistantV3_Final_Target.png`

## Visual comparison

- Replaced the rejected 2460x1510 gold modal with the target's 510x690
  top-right panel on a responsive 1672x941 reference frame.
- ARIA uses `portrait_aria_v3.png` through an aspect-preserving envelope crop;
  no axis stretching is used.
- All panel/button frames use procedural directional gradients and either no
  frame or the constant 3 px V3 border.
- Target, Integrity, and Range use the canonical shared Match V3 icons; no
  screen-local copies or placeholder icons were added.
- The voice knob is a procedural 48-segment disc, avoiding the rejected small
  hexagonal raster appearance. The switch reads/writes the persisted voice
  setting and applies it at runtime.
- The visible panel alone captures pointer input. The battlefield and other HUD
  areas remain available.
- At 20:9 the panel remains on the true right edge and the compacted resource,
  Settings, and Pause controls do not overlap it.
- The live validation harness intentionally has no battlefield scene loaded,
  so its center is black. Static captures provide the deterministic gameplay
  backdrop comparison; live captures prove the actual Menu-to-Match Canvas
  mount and exact aspect behavior.

## Evidence

- `aria_command_assistant_v3_16x9.png` — deterministic 1920x1080 comparison.
- `aria_command_assistant_v3_20x9.png` — deterministic 4800x2160 comparison.
- `aria_command_assistant_v3_live_16x9.png` — Play Mode, exact 1920x1080.
- `aria_command_assistant_v3_live_20x9.png` — Play Mode, exact 4800x2160.
- `build-and-capture.log` — shared foundation, Match HUD, POP-13 build and render.
- `focused-validation.log` — three POP-13 V3 structural/visual checks.
- `behavior-validation.log` — 23 assistant runtime behavior checks.
- `tutorial-regression.log` — three shared Tutorial surface regression checks.
- `live-16x9.log` and `live-20x9.log` — exact-size Play Mode gates.
