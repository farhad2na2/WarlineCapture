# SCN-12 District Detail Actions — Iteration 03

Review candidate only; explicit owner acceptance is still required.

- Target: `reference/SCN-12_DistrictDetailActionsV3_Final_Target.png`
- Runtime proof: `district_detail_actions_v3_16x9.png` at 1920x1080
- Runtime proof: `district_detail_actions_v3_20x9.png` at 4800x2160
- Dedicated route: `UIRoute.DistrictDetail`
- Dedicated prefab: `Assets/Game/Prefabs/UI/Shell/Content/SCN12_DistrictDetailActionsContent.prefab`
- Five Operations district markers route to this screen.
- All structural panels use directional procedural gradients and independent 3px borders.
- The district map and ARIA portrait preserve their source aspect ratios.
- The threat marker, drone, aid, tank, clock, repair, raid, locked-action, and resource art reuse canonical sprites packed in the shared V3 atlases.
- Both runtime captures passed `CanvasRouteCaptureValidation` with the matching Game View preset selected before rendering.

Iteration 01 defects corrected before this freeze:

- crude skull/triangle marker replaced with a layered location pin
- truck-like armored icon replaced with the tank icon
- incorrect Drone Scan and Aid icons replaced with action-correct art
- locked footer title/icon/lock collisions removed
- red time glyph replaced with the neutral gold clock used by the target family

