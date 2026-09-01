# Mission Result V3 — Iteration 04

Status: review candidate pending product acceptance.

## Runtime evidence

- `mission_result_v3_victory_16x9.png` — 1920×1080 victory state.
- `mission_result_v3_defeat_16x9.png` — 1920×1080 defeat state.
- `mission_result_v3_victory_20x9.png` — 4800×2160 victory state.
- `mission_result_v3_defeat_20x9.png` — 4800×2160 defeat state.
- Validation log: `/private/tmp/warline-mission-result-v3-iteration-04.log`.

## Corrected against the target locks

- One shared live prefab renders both outcome states; no flattened result UI.
- The background uses `AspectRatioFitter.EnvelopeParent` and never stretches.
- The 1672×941 authored composition remains centered at 16:9 and 20:9.
- Major header, middle, and footer panels use the same 3 px border weight.
- Continue and Retry use procedural directional gradients and overlap geometrically, so exactly one action is visible.
- Victory uses detailed shared-atlas filled stars; defeat uses procedural outlined stars without a duplicate raster asset.
- Defeat removes victory reward icons and applies the loss palette to the header, timer, objectives, rewards, footer accent, and action.
- Existing `MissionResultPopupView` and `CampaignMissionHudResultBinder` runtime bindings remain authoritative.

## Intentional shared-art choice

The battlefield is the existing shared Forward Post V3 plate. It is not a duplicate or a result-only baked composition. A future per-mission plate can replace this texture through the same aspect-preserved background slot without changing prefab geometry or runtime behavior.
