# Art/Atlas M01 V7 Map-Light-Matched Soldier Shadows

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Lane

Art/Atlas

## Task

Correct the rejected TargetMatchV5 soldier shadow direction for the accepted M01 v6 plate/runtime proof. User requested the final package use baked body+shadow atlases so Gameplay does not bind separate soldier shadows.

## Output

- Baked manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v7.json`
- Shadow source manifest/provenance: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_shadow_manifest_v7.json`
- Main production manifest pointer: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json` key `soldier_baked_shadows_v7`
- Player baked static facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v7.png`
- Enemy baked static facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v7.png`
- Player baked animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v7.png`
- Enemy baked animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v7.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v7_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV7BakedShadow_AssetPlacementReview_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV7BakedShadow_AssetPlacementReview_vs_Target_Comparison.png`
- Diff heatmap: `Design/AgentReports/Captures/M01_TargetMatchV7BakedShadow_AssetPlacementReview_vs_Target_DiffHeat.png`

## Binding Mode

Shadows are baked into the soldier atlases.

Gameplay should bind:

- player baked body+shadow atlas
- enemy baked body+shadow atlas
- v6 plate
- v5 markers/readability overlays

Gameplay should not bind a separate soldier shadow atlas for this package.

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v7 shadow source:

- `ig_035d3737ed5552e2016a0a23376c388198b4f9b5999b60c79e.png`

Body sources:

- accepted TargetMatchV5 static facing frames
- accepted soldier animation v5 frames

Deterministic postprocess was limited to chroma/alpha extraction from imagegen shadow art, compositing imagegen shadow under imagegen body frames, atlas packing, contact-sheet generation, placement/comparison packaging, metadata, and validation.

## Shadow Match Assessment

Player shadow: baked v7 player frames carry a lower-left / west-southwest map-light cast under the accepted TargetMatchV5 body. This replaces the rejected separate v5 shadow direction and needs no runtime shadow transform.

Enemy shadow: baked v7 enemy frames use the same map-light direction and contact point convention, so the shadows should no longer fight the v6 plate's baked wall/building shadows.

Opacity/contact: restrained dark asphalt opacity with a darker contact patch near foot anchor `[128,210]`; no compact oval/debug shadow.

## Gameplay Binding Checklist

- Player static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v7.png`
- Enemy static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v7.png`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v7.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV7/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v7.png`
- Cell size: `256x256`
- Static facing atlas rects: `NE [0,0,256,256]`, `SE [256,0,256,256]`, `SW [512,0,256,256]`, `NW [768,0,256,256]`
- Animation atlas: `4096x1792`, `16` columns, `112` frames per faction
- Pivot/foot anchor: `[128,210]`
- Z-order: ground plate, baked body+shadow unit sprite, markers/health bars
- Keep plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- Keep markers: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`
- Do not bind: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/TargetMatchV5/unit_shadow_facings_atlas_v5_strong.png`

## Comparison Metrics

- Full-frame MSE: `1066.51`
- World crop MSE: `819.19`
- Player region MSE: `967.06`
- Enemy region MSE: `682.53`

The metrics remain dominated by the clean-plate/HUD difference and unit style differences. This pass is about correcting shadow direction/contact, not claiming final pixel-perfect runtime lock.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v7.json`: passed.
- Parsed `m01_soldier_shadow_manifest_v7.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared review files exist: passed.
- Green residue scan on baked PNGs/atlases: `0`.
- Alpha channel present on baked PNGs/atlases: passed.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.
- `git diff --check` for the v7 package/report paths: passed.
- No Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports modified.
- Final visual components are imagegen-sourced; no deterministic/vector/programmatic placeholder shadow art was used.
- No target mockup crop, pasted screenshot, comparison panel, or diagnostic composite is used as runtime art.

## Handoff Status

Needs PM/user art approval. If accepted, route Gameplay to bind the baked v7 body+shadow atlases, keep the v6 plate and v5 markers, remove the separate soldier shadow layer, and regenerate M01 runtime proof.
