# Art/Atlas M01 V12 Dark Blue Connected Shadow Correction

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Reason For Rework

User review rejected V11 because:

- soldiers were too bright
- player accents read light blue/cyan instead of dark blue
- shadows were too small, detached from the feet, and looked like compact under-foot blobs

V12 supersedes V11 for review.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v12.json`
- Player static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v12.png`
- Enemy static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v12.png`
- Player animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v12.png`
- Enemy animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v12.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v12_darkblue_connected_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV12DarkBlueConnectedShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV12DarkBlueConnectedShadow_AssetPlacementReview_vs_Target_Comparison.png`

## Imagegen Provenance

Body sources remain the accepted built-in-imagegen TargetMatchV5 static facings and soldier animation v5 frames.

Shadow source remains the built-in-imagegen V9 straight-right white-background source, then resized/reanchored into a longer connected cast:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_035d3737ed5552e2016a0a297766148198941ac16ce4001d9f.png`

The target mockup was used only for comparison and metrics. No target mockup crops were pasted into delivered art.

## What Changed

- Soldier brightness reduced from V11.
- Player cyan/light-blue accents remapped to dark blue/navy.
- Baked shadow extended into a longer straight-right cast.
- Shadow start point moved to the boot line so it reads connected to the soldier.
- Shadows remain baked into body atlases; no separate runtime shadow layer.

## Dimensions

- Static facing atlases: `1024x256`, four `256x256` cells.
- Animation atlases: `4096x1792`, `112` frames per faction, `256x256` cells.
- Per-facing shadow assets: `256x256`.
- Contact sheet: `1800x1500`.
- Placement proof: `1920x1080`.
- Side-by-side comparison: `3840x1126`.

## Binding Checklist

- Bind player static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v12.png`
- Bind enemy static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v12.png`
- Bind player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v12.png`
- Bind enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV12/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v12.png`
- Cell size: `256x256`.
- Pivot / foot anchor: `[128, 210]`.
- Recommended proof visible body height: `86px`.
- Do not bind separate shadow atlases.
- Do not bind V7, V8, V9, V10, or V11 if V12 is approved.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v12.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared missing files: `0`.
- Alpha missing files: `0`.
- Border alpha files: `0`.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.

## Assessment

V12 addresses the latest user feedback more directly than V11: the bodies are darker, player accents are dark blue, and shadows are longer, straight-right, and connected at the boot line. This is the current Art/Atlas review candidate.
