# Art/Atlas M01 V11 Readable Baked Shadow Correction

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Reason For Rework

User review rejected the V9 placement proof because the soldiers were hard to see compared with the target mockup. V9 fixed the square matte artifact and straight-right shadow direction, but the unit bodies still read too small/dark against the v6 tactical plate.

V10 was generated as an aggressive readability pass, but internal review found it overshot the target: bodies became too bright and too large. V11 is the moderated candidate.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v11.json`
- Player static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v11.png`
- Enemy static baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v11.png`
- Player animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v11.png`
- Enemy animation baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v11.png`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v11_readable_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV11ReadableBakedShadow_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV11ReadableBakedShadow_AssetPlacementReview_vs_Target_Comparison.png`
- Diagnostics:
  - `Design/AgentReports/Captures/M01_V11Readable_PlayerShadowDirection_Diagnostic.png`
  - `Design/AgentReports/Captures/M01_V11Readable_EnemyShadowDirection_Diagnostic.png`

## Imagegen Provenance

Body sources remain the accepted built-in-imagegen TargetMatchV5 static facings and soldier animation v5 frames.

Shadow source remains the built-in-imagegen V9 straight-right white-background shadow source:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_035d3737ed5552e2016a0a297766148198941ac16ce4001d9f.png`

The target mockup was used only for visual comparison and metrics. No target mockup crops were pasted into the delivered art.

## What Changed

- Baked body+shadow atlases supersede V9 for review.
- Body readability uses anchored 1.10 scale inside the same 256px cells.
- Moderate value/contrast lift and subtle edge separation improve visibility without the over-bright V10 result.
- Shadow remains baked, connected at the foot anchor, and cast straight right.
- No separate runtime shadow layer is required.

## Dimensions

- Static facing atlases: `1024x256`, four `256x256` cells.
- Animation atlases: `4096x1792`, `112` frames per faction, `256x256` cells.
- Contact sheet: `1800x1500`.
- Placement proof: `1920x1080`.
- Side-by-side comparison: `3840x1126`.

## Binding Checklist

- Bind player static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/PlayerRifleSquad/player_rifle_squad_idle_facings_body_shadow_atlas_v11.png`
- Bind enemy static atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/EnemyPatrol/enemy_patrol_idle_facings_body_shadow_atlas_v11.png`
- Bind player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v11.png`
- Bind enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV11/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v11.png`
- Cell size: `256x256`.
- Pivot / foot anchor: `[128, 210]`.
- Recommended proof visible body height: `88px`.
- Do not bind separate shadow atlases.
- Do not bind V7, V8, V9, or V10 if V11 is approved.
- Keep v6 plate and TargetMatchV5 marker family.

## Validation

- Parsed `m01_baked_soldier_shadow_manifest_v11.json`: passed.
- Parsed `m01_ai_production_asset_manifest.json`: passed.
- Manifest-declared missing files: `0`.
- Alpha missing files: `0`.
- Border alpha files: `0`.
- Static facing frames per faction: `4`.
- Animation frames per faction: `112`.
- `git diff --check` for V11 manifest/report paths: passed.

## Assessment

V11 directly addresses the user feedback: V9 soldiers were too hard to see against the background. V11 improves silhouette and armor separation while preserving the straight-right baked shadow direction and avoiding the square matte artifact. It is not as bright or oversized as V10.

This is ready for PM/user visual approval. If accepted, Gameplay should bind V11 baked atlases and regenerate the runtime proof against the accepted v6 plate.
