# SCN-08 Full Map — Iteration 1

Status: review-frozen; not user-accepted.

Target lock: `../../reference/SCN-08_FullMapV3_Final_Target.png`

## Implemented

- Replaced the legacy single-frame popup with the V3 header, legend, tactical map,
  map-info rail, quick-toggle rail, and footer composition.
- All primary surfaces use procedural directional gradients and independent 3 px
  borders. No border is shared across or drawn through an adjacent panel.
- Reused the existing shared Sahrin map art with `AspectRatioFitter.EnvelopeParent`;
  neither supported aspect ratio stretches the bitmap.
- Replaced placeholder minimap dots with the existing sharp V3 friendly, hostile,
  and neutral sprites from `UI_V3_MatchIcons_01.spriteatlas`.
- Added functional Close, drag/tap focus, zoom, Center on HQ, and five toggle
  controls. Toggle checks are procedural, so no unsupported font glyph or local
  duplicate texture is required.
- Removed the generated viewport drag relay before prefab serialization; the live
  minimap recreates it at runtime and the saved prefab contains no missing scripts.

## Comparison and corrections

The first render was rejected because its old map source contained baked markers
and a baked white viewport, which duplicated the live overlay. It also used a
check character unavailable in the Oxanium font. The final render uses a clean
shared map, independent V3 preview markers, a single cyan viewport, and procedural
check marks. Long legend labels were reduced just enough to render without
ellipsis.

Compared with the target, the final modal geometry, five major regions, constant
border weight, typography hierarchy, cyan viewport, route, marker palette, and
footer placement align. The runtime harness does not load the battlefield world,
so its center is intentionally the live shell over black; the deterministic pair
provides the gameplay-background composition and populated tactical-map state.

## Gates

- Deterministic prefab captures: 1920x1080 and 4800x2160 passed.
- Live Menu-to-Match shell captures: 1920x1080 and 4800x2160 passed after mounting
  the actual Full Map popup and applying full-map projection/viewport interaction.
- Focused prefab suite: `[MatchHudFullMapV3Validation] result=Passed tests=3`.
- Prefab validation: 33 procedural gradients, 3 px primary borders, shared V3
  marker sprites, responsive 1672x941 composition, and all required actions bound.
