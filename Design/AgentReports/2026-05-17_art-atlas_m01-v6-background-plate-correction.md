# Art/Atlas M01 V6 Background Plate Correction

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Lane

Art/Atlas

## Task

Correct the M01-01 clean background/source plate after PM accepted v5 units, shadows, markers, and animation atlases as current candidates but rejected the v3 background plate.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_background_manifest_v6.json`
- Main production manifest pointer: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json` key `target_match_v6_background`
- Imagegen source copy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV6/m01_tactical_start_clean_plate_v6_imagegen_source_1672x941.png`
- Clean plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- POT plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_pot_2048x2048.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_background_v6_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV6_AssetPlacementReview_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV6_AssetPlacementReview_vs_Target_Comparison.png`
- Diff heatmap: `Design/AgentReports/Captures/M01_TargetMatchV6_AssetPlacementReview_vs_Target_DiffHeat.png`

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v6 plate source:

- `ig_035d3737ed5552e2016a09f165d51081988939fa87832a8b17.png`

Deterministic postprocess was limited to workspace copy, resize to `1920x1080`, POT padding to `2048x2048`, contact sheet generation, diagnostic placement/comparison packaging, metadata, and validation.

## Accepted V5 Assets Reused

- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v5.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/enemy_patrol_animation_atlas_v5.png`
- Strong shadow animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/AnimationV5/unit_shadow_animation_atlas_v5_strong.png`
- Marker family: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`

## Comparison Metrics

- Full-frame MSE: `1066.25`
- World crop MSE: `818.67`
- Player region MSE: `956.64`
- Enemy region MSE: `683.57`

The full-frame metric compares the target mockup with HUD against a clean no-HUD plate, so it is not a pure background score. The enemy region improved versus the v5 strong-shadow proof (`856.81` to `683.57`). The player/world numbers remain affected by clean-plate generation differences and the absence of HUD overlays.

## Background Assessment

Player region: v6 is closer than the rejected v3 plate for the lower-left road, sidewalk, low wall, ruined building mass, rubble field, and fire relationship. It is still not pixel-identical because the plate is newly generated art rather than a target crop.

Enemy region: v6 preserves the upper-right road, crosswalk, cover wall, right-side building edge, crates, and streetlight relationship well enough for placement review. The accepted v5 enemy overlays now sit on a more plausible matching ground plane.

## Gameplay Binding Checklist

- Bind plate source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- Optional POT import: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_pot_2048x2048.png`
- Keep unit/shadow/marker/animation bindings from `target_match_v5` and `soldier_animation_v5`.
- Do not import target mockups, target crops, comparison panels, diff heatmaps, or diagnostic composites as runtime art.

## Validation

- Parsed `m01_target_match_background_manifest_v6.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared review files exist: passed.
- PNG dimensions:
  - imagegen source `1672x941`
  - clean plate `1920x1080`
  - POT plate `2048x2048`
  - contact sheet `1920x1080`
  - placement proof `1920x1080`
  - comparison panel `3840x1126`
- `git diff --check` for the v6 package/report paths: passed.
- No Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports modified.
- Final replacement plate art is imagegen-sourced; no deterministic/vector/programmatic final art was used.
- No target mockup crop, pasted screenshot, comparison panel, or diagnostic composite is used as runtime art.

## Handoff Status

Needs PM/user art approval. If accepted, route Gameplay to bind the v6 plate plus accepted v5 units, shadows, markers, and animation atlases through the existing ECS/runtime presentation path and regenerate the M01-01 target-match proof.
