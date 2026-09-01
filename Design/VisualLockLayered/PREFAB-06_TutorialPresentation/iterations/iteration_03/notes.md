# PREFAB-06 Tutorial Presentation — Iteration 3

Status: review-frozen; user acceptance pending.

Target lock:

- `../../reference/PREFAB-06_TutorialPresentationV3_Final_Target.png`

Accepted review evidence:

- `tutorial_presentation_v3_16x9.png`
- `tutorial_presentation_v3_20x9.png`
- `tutorial_presentation_v3_live_16x9.png`
- `tutorial_presentation_v3_live_20x9.png`
- `build-and-capture.log`
- `focused-validation.log`
- `behavior-validation.log`
- `live-16x9.log`
- `live-20x9.log`

Rejected iterations:

- Baseline used a small legacy lower-left card, detached ARIA portrait, old raster button frames, flat debug background, and no target guidance overlay.
- Iteration 1 moved to the V3 top-right composition but allowed the portrait to cover part of the progress label and let the cyan guide cross the normal red feedback banner.
- Iteration 2 corrected the draw order and banner state, but the guide stopped above the enlarged V3 controls and the first live route selected duplicate hidden Settings/Pause transforms.
- Iteration 3 connects the guide to the actual Rifle Squad and Move controls, resolves the active Match header only, and restores the normal header layout when the tutorial closes.

Frozen checks:

- Eight procedural gradients and a constant 3 px primary border contract.
- New `portrait_aria_v3.png` rendered through an aspect-preserving masked crop.
- Functional Do It, Show Me, and Skip buttons remain bound to the existing popup flow.
- The full-screen guide owns no raycast graphic; battlefield input remains available outside the visible panel.
- The temporary compact resource/settings/pause layout is reversible and hides only the normal embedded ARIA panel while the tutorial is open.
- Persian uses the existing Noto Sans Arabic narrative font; the complete 23-test tutorial behavior suite passes.
- Focused V3 prefab/header-layout validation passes three checks.
- Live Match-route Play Mode captures pass at exact 1920 x 1080 and 4800 x 2160 sizes.

The Play Mode capture route intentionally does not load a battlefield world, so its center is black. The deterministic review frames provide the target-background comparison; the live frames prove real route mounting, responsive geometry, and runtime bindings.
