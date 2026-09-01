# SCN-00 First Launch — Iteration 6

Status: current review candidate only. It is not accepted until the user
explicitly confirms it.

## Target

`../../reference/SCN-00_FirstLaunchV3_LanguageChoice_Final_Target.png`

## Corrections

- Rejected the procedural WARLINE/CAPTURE reconstruction after it produced a
  missing-glyph square and a rank mark that did not match the Main Menu V3
  target.
- Added exactly one canonical high-resolution Main Menu V3 logo sprite:
  `Assets/Game/Art/UI/V3Shared/Sprites/Brand/ui_v3_brand_logo_mainmenu.png`.
- Packed that sprite exactly once in
  `Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_Brand_01.spriteatlas`.
- Rebuilt all 17 logo-owning V3 prefabs (18 logo references) to use the same
  canonical sprite. The validation rejects alternate logo sprites and
  procedural WARLINE text.
- Preserved the logo aspect ratio and disabled logo raycasts.
- Added one canonical atlas entry for each reusable semantic gradient: green,
  red, amber, blue, cyan, and graphite. These are packed in
  `UI_V3_CoreChrome_01.spriteatlas`; no screen-local gradient PNGs are allowed.
- Fixed First Launch input targets so language cards and Continue have active
  raycast target graphics.

## Evidence

- `language_choice_v3_16x9.png` and `language_choice_v3_20x9.png` show the
  corrected shared logo and responsive layout at 1920x1080 and 4800x2160.
- The same folder contains the rebuilt Commander Identity, Comic Playback, and
  ARIA Guidance states at both aspect ratios.
- Focused validation passed with 9 tests and `pointerTargets=Passed`.
- Manual Play Mode proof: clicked Persian, observed the selection move from
  English to Persian, clicked Continue, and observed the narrative advance to
  Persian dialogue.
- Shared-brand validation passed with `prefabs=17`, `references=18`,
  `canonicalBitmap=1`, and `duplicate=0`.

## Remaining visual comparison

- User acceptance is pending.
- The target uses slightly different card proportions, portrait crops, and
  lower-dock spacing. Those differences remain for a later focused visual pass;
  they are not hidden by the logo/input correction recorded here.
