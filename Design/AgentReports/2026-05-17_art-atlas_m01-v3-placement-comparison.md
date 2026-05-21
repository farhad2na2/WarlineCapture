# Art/Atlas M01 V3 Placement Comparison

Date: 2026-05-17
Owner: Art/Atlas
Status: diagnostic comparison complete
Priority: P0

## Lane

Art/Atlas

## Task

Place the generated M01 v3 assets over the generated v3 clean tactical plate at the same target mockup screen sizes/anchors, then compare against `M01-01_TacticalStart_1920x1080.png`.

This is diagnostic review evidence only. It is not runtime art and is not a substitute for Gameplay binding the assets through ECS/runtime presentation after PM/user approval.

## Output

- Composite: `Design/AgentReports/Captures/M01_TargetMatchV3_AssetPlacementReview_1920x1080.png`
- Side-by-side comparison: `Design/AgentReports/Captures/M01_TargetMatchV3_AssetPlacementReview_vs_Target_Comparison.png`
- Difference heatmap: `Design/AgentReports/Captures/M01_TargetMatchV3_AssetPlacementReview_vs_Target_DiffHeat.png`

## Placement Rules Used

- Plate: `1920x1080`, full-screen.
- Player squad: target mockup player foot anchors from the locked M01 selected-state metadata, with M01-01 selection rings hidden.
- Enemy patrol: target mockup enemy foot-ring and health-bar rects from `M01-01_TacticalStart_layers.json`.
- Unit scale: same player/enemy target render height and same v3 unit pivot policy.
- Enemy overlays: v3 red foot ring and v3 segmented health bar scaled into the target mockup overlay rects.

## Numeric Comparison

- Full-frame MSE: `906.70`
- World-focused crop MSE `(320,80)-(1545,815)`: `662.68`
- Player region MSE `(390,540)-(650,760)`: `862.85`
- Enemy region MSE `(1330,235)-(1605,430)`: `768.48`

The full-frame metric is inflated by target HUD/UI, which the generated Art/Atlas plate intentionally does not include.

## Assessment

Accepted for diagnostic placement review, but not target-perfect.

What is close:

- The generated clean plate has the right dark isometric urban battlefield language, damaged roads, rubble density, smoke/fire accents, and no baked HUD/units.
- The player and enemy assets can be placed at the target footprint without needing different player/enemy scales.
- Enemy red foot rings and health bars align to the target overlay rects and remain readable.

Remaining mismatches:

- The clean plate composition is shifted versus the target: building massing, road/wall positions, and the bottom-right/minimap-side visual structure do not land exactly on the target.
- Player and enemy sprites are still chunkier/more upright than the target mockup soldiers, even when scaled to the target footprint.
- Unit lighting/contrast is slightly brighter and more cutout-like than the baked target mockup soldiers.
- Since this diagnostic has no HUD, a full-frame comparison cannot represent final runtime target match until Gameplay binds the plate and UI/HCI owns HUD chrome.

## Next Action

PM/user should review whether this v3 asset package is good enough for Gameplay binding proof. If exact target match is required before Gameplay resumes, Art/Atlas should run another imagegen iteration focused on:

- closer plate composition to the M01-01 target crop, while still avoiding target crops or pasted mockup pixels
- smaller, more top-down infantry silhouettes
- lower-contrast unit integration against the dark road surface
