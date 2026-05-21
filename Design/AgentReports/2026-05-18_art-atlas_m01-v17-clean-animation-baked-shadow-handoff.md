# Art/Atlas M01 V17 Clean Animation Baked Shadow Handoff

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user approval
Priority: P0

## Summary

V17 addresses the user-reported merged/two-half-frame atlas issue by rebuilding the animation cells from the V5 imagegen source sheets instead of fixed-slicing the contaminated V5/V16 atlases.

Each clean body cell was validated to contain exactly one large full-pose component before shadows were rebaked into the animation atlases.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v17.json`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v17.png`
- Player baked shadow atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v17.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v17.png`
- Enemy baked shadow atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v17.png`
- Player numbered audit grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v17_player_animation_atlas_numbered_grid.png`
- Enemy numbered audit grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v17_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_baked_shadows_v17_clean_animation_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV17CleanAnimationBakedShadow_AssetPlacementReview_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV17CleanAnimationBakedShadow_vs_Target_Comparison.png`

## Imagegen Provenance

Source visuals remain the built-in-imagegen V5 soldier animation source sheets:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/AnimationV5/player_rifle_squad_animation_v5_source_alpha.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/AnimationV5/enemy_patrol_animation_v5_source_alpha.png`

Postprocess was limited to source-component extraction, alpha preservation, full-pose resampling into the required frame slots, pivot recentering, directional-dark body treatment, per-frame horizontal-right shadow baking, atlas packing, contact sheets, and validation. No target mockup crops were pasted into the delivered art.

## Important Source Note

The V5 source sheets do not contain 28 clean distinct full-pose components for every facing row:

- Player rows: `24`, `24`, `24`, `23` clean components
- Enemy rows: `26`, `26`, `26`, `26` clean components

V17 therefore resamples complete source poses into the required `112` frame slots per faction. This resolves merged/partial atlas cells, but PM/user should still approve the animation cadence because some slots necessarily reuse nearby clean poses.

## Dimensions

- Player clean body atlas: `4096x1792`
- Player baked shadow atlas: `4096x1792`
- Enemy clean body atlas: `4096x1792`
- Enemy baked shadow atlas: `4096x1792`
- Numbered audit grids: `2048x938`
- Placement proof: `1920x1080`
- Target comparison: `3840x1080`

## Validation

- Manifest parse: passed.
- Manifest-declared missing files: `0`.
- Alpha-missing declared PNGs: `0`.
- Animation atlas outer-border alpha: `0` files.
- Clean body cells checked: `112` player, `112` enemy.
- One-full-pose validation failures: `0`.
- Shadows are baked into the V17 atlases; no separate runtime shadow atlas is required.

## Binding Checklist

- Bind player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v17.png`
- Bind enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v17.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Frame count: `112` per faction.
- Pivot: `[128, 210]`.
- Per-frame foot anchors: declared in `m01_baked_soldier_shadow_manifest_v17.json`.
- Do not bind V7 through V16 animation atlases for final approval.

## Assessment

The merged/two-half-frame atlas blocker is resolved in V17 by validation and visual audit grids. The package is ready for PM/user review specifically for clean cells, baked shadow visibility, and animation cadence.
