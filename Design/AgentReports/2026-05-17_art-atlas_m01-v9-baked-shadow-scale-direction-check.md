# Art/Atlas M01 V9 Baked Shadow Scale/Direction Check

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Reason For Rework

User review rejected the prior baked shadow proof because:

- shadow direction was wrong
- shadow/contact read detached from the soldier
- a square matte/box was visible around the soldier
- soldiers in the placement proof were too small

Root cause:

- v7/v8 shadow extraction came from green-screen translucent shadow art, which polluted alpha and caused box artifacts.
- The proof scale was inherited from the earlier 64px optimization pass, which made the soldiers too small for visual review against the target mockup.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v9.json`
- Shadow source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV9/unit_shadow_facings_v9_straight_right_white_source.png`
- Player static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV9/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v9.png`
- Enemy static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV9/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v9.png`
- Player animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV9/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v9.png`
- Enemy animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV9/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v9.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v9_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV9BakedShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV9BakedShadow_AssetPlacementReview_vs_Target_Comparison.png`
- Diagnostics:
  - `Design/AgentReports/Captures/M01_Target_PlayerShadowDirection_Diagnostic.png`
  - `Design/AgentReports/Captures/M01_V9Baked_PlayerShadowDirection_Diagnostic.png`
  - `Design/AgentReports/Captures/M01_Target_EnemyShadowDirection_Diagnostic.png`
  - `Design/AgentReports/Captures/M01_V9Baked_EnemyShadowDirection_Diagnostic.png`

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v9 shadow source:

- `ig_035d3737ed5552e2016a0a297766148198941ac16ce4001d9f.png`

The v9 shadow source was generated on pure white and converted with luminance-to-alpha extraction to avoid green-screen square matte artifacts. Body sprites remain the accepted imagegen-sourced TargetMatchV5 / soldier animation v5 frames.

## What Changed

- Shadow direction corrected to straight right/east from the foot contact.
- Shadow is baked into body atlases; no separate runtime shadow layer.
- Proof scale increased from `64px` to `82px` unit height.
- Alpha border validation added to catch visible box/matte artifacts.
- V9 supersedes v7 and v8 baked shadow atlases.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v9.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared review files exist: passed.
- Green residue files: `0`.
- Alpha missing files: `0`.
- Border alpha files: `0`.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.
- `git diff --check` for the v9 package/report paths: passed.

## Assessment

V9 fixes the visible box artifact and corrects the cast direction to straight right. The larger placement proof makes the soldiers easier to compare, but they may still read smaller/slimmer than the target mockup because the accepted v5 body source sprites are slim. Further soldier scale/body-shape changes would require a separate Art revision to the accepted body frames, not just shadow baking.
