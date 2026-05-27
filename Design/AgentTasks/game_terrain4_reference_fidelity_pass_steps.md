# Game_Terrain4 Reference Fidelity Pass Plan

Date: 2026-05-26

Status: Complete after 2026-05-27 visual-rejection correction.

Goal: adjust the `Game_Terrain4` generation scripts so the generated 3D map reads like the `SyntyHighlands_01/base_visual.png` reference, while still using the existing Synty/POLYGON prefabs and keeping gameplay truth in grid, blocker, and height data.

Current mismatch:

- The foundation ground chooses dirt, green grass, and dark grass mostly from procedural noise, not from the reference image.
- Tree and bush placement uses the tree-density mask, but then rejects hard-blocker and soft-pathing cells. This removes much of the dense jungle/mountain-belt vegetation that the reference image uses as visual blockers.
- Mountain placement uses isolated spaced points, so it creates readable prefabs but not broad connected ridge masses like the reference.
- Rocks are placed as individual decorative points instead of transition skirts around ridges and cliffs.
- Proof captures use debug overlays, so they are good for validation but not good enough for visual comparison.

## Design Direction

Use the map pack as an art-directed source, not only as loose procedural input.

The generation should produce three separate layers:

1. Ground material layer: dirt, green grass, dark grass, beach/coast.
2. Blocker landscape layer: mountains, cliff rocks, dense forest, and jungle belts.
3. Detail dressing layer: bushes, grass patches, small rocks, scrub, and transition props.

The gameplay grid remains authoritative for pathing. Visual objects can exist on blocked cells as long as they do not add unexpected gameplay collision.

## Steps

- [x] 1. Add a reference analysis pass for `base_visual.png`.
  - Sample the same 2024x2024 coordinate contract already used by the mask builder.
  - Classify each sampled area into `greenGrass`, `darkGrass`, `dirt`, `rockMountain`, `forestCanopy`, and `reserveClear`.
  - Prefer an explicit optional `surface_material_mask.png` in the map pack. If it is missing, derive a best-effort material map from `base_visual.png`.

- [x] 2. Replace noise-first ground material selection with reference-driven material selection.
  - Update `WarlineCaptureGameTerrain3Island2048Builder.ChooseGroundMaterial`.
  - For each ground prefab center, map world position back to grid coordinates and sample the material classification.
  - Use `PolygonBattleRoyale_01_A` for green zones, `PolygonBattleRoyale_03_A` for dark grass, and `PolygonBattleRoyale_02_A` for dirt.
  - Keep procedural noise only as small breakup inside a matching material zone, not as the main source of truth.

- [x] 3. Make city and base reserves visually clean from the start.
  - Force reserve rectangles to mostly dirt or low grass, matching the reference clearings.
  - Allow sparse grass patches near reserve edges only.
  - Keep all generated blocker landscape props outside these reserves.

- [x] 4. Split vegetation into playable vegetation and blocker vegetation.
  - `Generated_Trees_Playable`: lower-density trees near walkable lanes and open groves.
  - `Generated_Trees_BlockerBelt`: dense visual forest on hard blockers, high terrain, and mountain edges.
  - `Generated_Bushes_Playable`: scrub and bushes on walkable terrain.
  - `Generated_Bushes_BlockerBelt`: dense low vegetation around mountain/forest blockers.
  - Remove colliders from visual blocker vegetation if gameplay blockers already come from the grid.

- [x] 5. Replace single-point tree/bush placement with cluster placement.
  - Use the density mask to select cluster centers.
  - In medium-density zones, spawn small clusters with spacing.
  - In dense zones, spawn multiple trees and bushes around each cluster center with jitter, rotation, and scale variation.
  - Keep movement lanes readable by preserving low-density corridors from the masks.

- [x] 6. Convert mountain placement from isolated points into ridge components.
  - Build connected components from `height_mask`, `blocker_mask`, and dense `rock_density_mask`.
  - For each large component, place overlapping mountain prefabs along the ridge direction.
  - Scale mountain prefabs by component size and local height value.
  - Use small rock prefabs around the edge as a transition from ridge to grass/dirt.

- [x] 7. Add reference-driven detail grass placement.
  - Place `SM_Generic_Grass_Patch_01` and related grass patch prefabs mostly on green/dark grass cells.
  - Avoid dirt-heavy reserve interiors.
  - Use higher detail density in the foreground/playable camera band, lower density in far background.

- [x] 8. Add clean visual comparison captures.
  - Generate a clean top-down capture without debug dots, red rectangles, or grid overlays.
  - Generate a playable-camera capture from the real game camera.
  - Keep current debug captures as separate validation artifacts.

- [x] 9. Add visual fidelity metrics before optimization.
  - Compare generated ground material percentages against the reference classification.
  - Compare tree/bush density heatmaps against `tree_density_mask.png`.
  - Compare mountain footprint coverage against `height_mask.png` and `rock_density_mask.png`.
  - Fail the pass if reserves are polluted, blocker belts are sparse, or the clean capture is missing.

- [x] 10. Run the existing one-go optimization only after the fidelity pass.
  - Regenerate `Game_Terrain4`.
  - Validate visual fidelity.
  - Build `Game_Terrain5`.
  - Build `Game_Terrain7`.
  - Do not run LOD generation for this terrain path.

## Script Changes Required

- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain3Island2048Builder.cs`
  - Add map-pack material sampler.
  - Replace noise-primary material selection with reference-primary material selection.
  - Add reserve-aware ground material overrides.
  - Make detail grass density map-driven.

- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain4MaskDressingBuilder.cs`
  - Split vegetation groups into playable and blocker-belt groups.
  - Allow visual trees/bushes on hard blocker cells when they are part of the blocker-belt layer.
  - Add cluster placement for vegetation.
  - Add ridge/component placement for mountains.
  - Add clean visual captures and fidelity JSON.

- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain4FullRegenerationPipeline.cs`
  - Insert the new reference-fidelity validation after `Game_Terrain4` generation and before optimization.

## Acceptance Target

`Game_Terrain4` should no longer be judged only by prefab counts. It should pass if:

- The dirt/green/dark grass pattern follows the reference image.
- Dense forests appear where the reference and masks show dense vegetation.
- Mountains read as connected ridge masses, not scattered single rocks.
- City and base pads remain clear and buildable.
- The clean top-down capture looks visually aligned with `base_visual.png` before `Game_Terrain5` and `Game_Terrain7` are built.

## Completion Evidence

- Unity validation command: `WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize`
- Validation workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity2`
- Final log: `/private/tmp/warlinecapture-codexunity2-reference-fidelity-revision-one-go-r2.log`
- `Game_Terrain4` mask dressing validation: passed.
- `Game_Terrain4` reference fidelity summary: passed.
- `Game_Terrain4` generated dressing: 6,427 prefabs.
- Mountains: 55 open-layout anchors. Mountain clustering was removed because the previous 716-mountain pass connected too many blockers and visually closed movement lanes.
- Playable trees / blocker-belt trees: 1,178 / 1,609.
- Playable bushes / blocker-belt bushes: 1,048 / 1,730.
- Rocks: 807.
- Vegetation under mountain anchors: 0.
- Reserve pollution: 0.
- Clean captures:
  - `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_clean_topdown_scene.png`
  - `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_clean_playable_scene.png`

## 2026-05-27 Visual-Rejection Correction

The first completed pass still failed the intended art read: trees were not visible enough, dirt road/path corridors did not read clearly, mountains connected into movement-blocking chains, and some vegetation was hidden under mountain prefabs.

Correction applied:

- Mountain candidates now require true high/rock terrain: high height mask, dense rock peak, or hard raised rocky terrain. Hard blocker value alone is no longer enough.
- Mountain spacing is widened to 118 cells and cluster count is forced to one prefab per anchor.
- Trees and bushes receive a 56-cell mountain keep-out mask so they cannot be generated under large mountain prefabs.
- Visible tree spacing is tightened for playable groves and forest belts, while blocker-belt clusters are reduced so forest reads clearly without becoming hidden clutter.
- Ground material classification treats warm, low-height, low-rock, low-tree corridors as dirt paths, improving the visual road/path read.
- Reference fidelity validation now fails if mountain count exceeds the open-layout maximum or if any vegetation is placed under mountain anchors.
