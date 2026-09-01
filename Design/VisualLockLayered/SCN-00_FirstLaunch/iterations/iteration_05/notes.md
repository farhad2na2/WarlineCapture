# SCN-00 First Launch — Runtime Iteration 5

Status: current review candidate only. It is not accepted until the user
explicitly confirms it.

## Target states

- Language Choice
- Comic Playback
- Commander Identity
- ARIA Guidance

## Evidence

- `*_live_16x9.png` and `*_live_20x9.png` are captures from the real Menu scene
  in Play Mode at exact 1920x1080 and 4800x2160 output sizes.
- `*_reference_render_16x9.png` and `*_reference_render_20x9.png` are the sharp,
  deterministic prefab renders used for pixel-level visual comparison.
- `live_capture_16x9.log` and `live_capture_20x9.log` contain the passing state
  and final-result markers.

## Corrections in this iteration

- Replaced the hex-like language substitute with procedural circular globe
  rings.
- Replaced the unsupported Commander Identity warning glyph with a procedural
  warning triangle.
- Reused the shared V3 commander chevrons and support badge for the identity
  cards instead of weak text/glyph substitutes.
- Normalized selection-frame borders to the 3 px V3 contract.
- Preserved the ARIA portrait aspect ratio and added the target-like cyan wash,
  telemetry rails, ticks, and reticle around it.
- Found the QA reviewer bar overlapping Comic, Identity, and Guidance during
  the first real Menu-scene Play Mode run. The capture route now suppresses
  reviewer-only controls; those invalid frames were not frozen as evidence.
- Confirmed all four final states at both required aspect ratios without panel
  intersections or stretched character art.
- Fixed the Edit Mode-only tiny preview: the `1672x941` reference composition
  now scales to the active canvas through an `[ExecuteAlways]` layout view and
  a `DrivenRectTransformTracker`, without dirtying `Menu.unity`.
- The live capture route restores the Game view to `1920x1080` when complete
  and never closes the user's open Editor.
- Replaced every per-screen/procedural WARLINE logo with the one canonical
  `shared_brand_logo_lockup.png` sprite in the dedicated one-item
  `UI_V3_Brand_01.spriteatlas`.
- The all-screen brand gate passed across 17 prefabs and 18 references with
  `duplicate=0`; standalone procedural `WARLINE` text is rejected.
- Shared-foundation setup now validates before importing and rebuilds only
  when validation fails, keeping repeated live proof captures fast.

## Latest live capture markers

- `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=1920x1080 suffix=16x9`
- `[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested=4800x2160 suffix=20x9`
- `[V3SharedBrandLogoMigrationBuilder] result=Passed prefabs=17 references=18 ... duplicate=0`
