# Art/Atlas M01 V16 Boot-Anchored Shadow Correction

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Reason For Rework

User review noted that when the soldier looks left, the shadow was not under the boot. V14 used a wider shadow, but it was still anchored from a fixed pivot. V16 computes the boot/contact anchor from the actual lower-body alpha for each facing/frame, so the wider shadow sits under the boots before extending straight right.

V15 proved the boot-anchor approach, but validation caught alpha on some cell borders. V16 clamps the widened shadow inside the `256x256` cell.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v16.json`
- Player static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v16.png`
- Enemy static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v16.png`
- Player animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v16.png`
- Enemy animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v16.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v16_boot_anchored_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV16BootAnchoredShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV16BootAnchoredShadow_AssetPlacementReview_vs_Target_Comparison.png`

## Imagegen Provenance

Body sources remain the accepted built-in-imagegen TargetMatchV5 static facings and soldier animation v5 frames, processed through the V13 directional-dark lighting correction.

Shadow source remains imagegen-derived from the V9/V12 straight-right shadow texture. V16 reshapes and positions that texture under each actual boot anchor. No target mockup crops were pasted into delivered art.

## What Changed

- Retained V13 darker directional soldier lighting.
- Retained dark blue player accents.
- Kept the wider V14-style foot shadow.
- Replaced fixed-pivot shadow placement with per-facing/per-frame boot anchors.
- Clamped the widened shadow inside each `256x256` cell.
- Shadows remain baked into the unit atlases; no separate runtime shadow layer.

## Static Boot Anchors

- Player: `NE [98,202]`, `SE [141,203]`, `SW [130,204]`, `NW [124,204]`
- Enemy: `NE [104,200]`, `SE [143,201]`, `SW [136,203]`, `NW [122,202]`

## Binding Checklist

- Bind player static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v16.png`
- Bind enemy static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v16.png`
- Bind player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v16.png`
- Bind enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v16.png`
- Cell size: `256x256`.
- Sprite pivot remains `[128, 210]`.
- Foot anchors are per-frame in `m01_baked_soldier_shadow_manifest_v16.json`.
- Recommended proof visible body height: `86px`.
- Do not bind separate shadow atlases.
- Do not bind V7 through V15 if V16 is approved.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v16.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared missing files: `0`.
- Alpha missing files: `0`.
- Border alpha files: `0`.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.

## Assessment

V16 is the current review candidate. It specifically corrects the left-facing boot issue by anchoring each baked shadow from the actual boot area rather than the generic sprite pivot, while keeping the darker directional body treatment and wider shadow footprint.
