# Visual iteration — 2026-10-03

User direction: prioritize native map visuals close to the approved mockups. Stop mission playtesting until the look is accepted. This supersedes the handoffs' immediate playthrough gate.

## Current comparison set

- Refinery: `SupplyLine/after-*.png` (six views; freshest capture: `beautify-refinery-fresh.log`).
- Airfield: `CityEdgeAirfield/after-OperationMap_Ch01M04_AirfieldReview.png` and Ch04M01.
- Port: `AshLinePort/after-OperationMap_Ch02M05_PortReview.png`.
- Copies and full logs: `SupplyLine/Evidence/20261003-visual-review/`.
- Rendering: native PC Premium profile for art comparison; this is not mobile acceptance evidence. The capture helper temporarily selects this profile and restores the previous selection.

## Changes and checks

New native weathered concrete material/texture, thin seams, yard and vehicle markings, smaller brown stains, alpha-blended dust and fewer fires/plumes. Shared render-only pass enabled for Airfield and Port; their layouts unchanged. Separate concrete mesh names avoid persisted-mesh collisions. Visual build avoids packaged mission-content rebuilds. Capture names now use asset file names so Dense City definitions will not overwrite each other.

Refinery, Airfield and Port authoring builds and screenshot captures passed. All three existing semantic hashes stayed unchanged. Refinery's earlier focused rules, regression and bake checks passed, as recorded in the validation evidence folder. These are separate from visual acceptance.

## Review remains pending

These are first comparison passes, not an accepted match to the mockups. Still improve concrete wear and irregular edge sand, grass/rock clusters, compact story vignettes, Airfield taxi/helipad outlines and Port quay details. The current mission cameras cover much larger areas than the mockup closeups, so retain both battle and close comparison views.

Frontier container regrouping is approved and remains pending. Dense City street/outskirts dressing remains pending. Keep Port containers in their current layout. Preserve every mission's gameplay pins.

## Playthrough and device gates

Deferred at the user's request. The first normal-input probe crashed natively in a Profiler deserialization stack; evidence is preserved in `20261003-watch-01-native-crash`. The retry was stopped cleanly at the user's visual-first request. Neither establishes play readiness. No successful complete mission/result/return or device acceptance is claimed.

Initial visual dispatches v3 and v3b failed while the Editor was compiling/importing; they are preserved. v3c passed with an unrelated concurrent CLI status timeout recorded. The subsequent concrete build and fresh capture passed. Do not discard failed evidence or treat it as a licensing blocker.
