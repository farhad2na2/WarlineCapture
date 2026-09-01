# Splash / Loading V3 — Iteration 4

Target: `../../reference/SCN-01_SplashLoadingV3_Final_Target.png`

Files:

- `splash_v3_16x9.png`: 1920x1080 capture
- `splash_v3_20x9.png`: 2400x1080 capture representing the required 4800x2160 aspect

Corrections included:

- the single environment plate uses aspect-fill cover cropping and is never
  stretched non-uniformly
- logo, status chips, and loading footer share one centered 1672x941 authored
  reference frame that remains fully visible at 16:9 and 20:9
- the logo rank mark uses the target's diamond-and-chevron motif instead of a
  five-point star
- reusable chrome, borders, progress fill, and readability treatment remain
  procedural/shared; the background is the only raster sprite
- prefab validation passed with one raster sprite, nine gradients, and fifty
  Images

Review status: candidate only; not accepted until the user explicitly confirms.
