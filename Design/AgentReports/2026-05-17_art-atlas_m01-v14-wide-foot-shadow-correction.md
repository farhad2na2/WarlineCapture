# Art/Atlas M01 V14 Wide Foot Shadow Correction

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Reason For Rework

User review requested the shadows under the soldiers' feet to be wider and bigger. V14 keeps the V13 dark directional soldier lighting and replaces the smaller foot shadow with a wider boot-contact mass plus a straight-right cast.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v14.json`
- Player static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v14.png`
- Enemy static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v14.png`
- Player animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v14.png`
- Enemy animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v14.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v14_wide_foot_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV14WideFootShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV14WideFootShadow_AssetPlacementReview_vs_Target_Comparison.png`

## Imagegen Provenance

Body sources remain the accepted built-in-imagegen TargetMatchV5 static facings and soldier animation v5 frames, processed through the V13 directional-dark lighting correction.

The shadow source remains imagegen-derived from the V9/V12 straight-right shadow texture, reshaped into a wider under-foot contact mass and straight-right cast. No target mockup crops were pasted into delivered art.

## What Changed

- Retained V13 darker body lighting with right-side shadow.
- Retained dark blue player accents.
- Replaced the smaller V13 boot shadow with a wider, larger foot-contact shadow.
- Kept the straight-right cast direction.
- Shadows remain baked into the unit atlases; no separate runtime shadow layer.

## Dimensions

- Static facing atlases: `1024x256`, four `256x256` cells.
- Animation atlases: `4096x1792`, `112` frames per faction, `256x256` cells.
- Per-facing shadow assets: `256x256`.
- Contact sheet: `1800x1500`.
- Placement proof: `1920x1080`.
- Side-by-side comparison: `3840x1126`.

## Binding Checklist

- Bind player static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v14.png`
- Bind enemy static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v14.png`
- Bind player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v14.png`
- Bind enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV14/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v14.png`
- Cell size: `256x256`.
- Pivot / foot anchor: `[128, 210]`.
- Recommended proof visible body height: `86px`.
- Do not bind separate shadow atlases.
- Do not bind V7 through V13 if V14 is approved.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v14.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared missing files: `0`.
- Alpha missing files: `0`.
- Border alpha files: `0`.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.
- `git diff --check` for V14 manifest/report paths: passed.

## Assessment

V14 is the current review candidate. It keeps the darker directional soldier body from V13 and makes the boot-contact shadow wider and larger while preserving the straight-right cast and baked-atlas binding model.
