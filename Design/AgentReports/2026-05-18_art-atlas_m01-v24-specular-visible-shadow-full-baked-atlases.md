# Art/Atlas M01 V24 Specular Visible Shadow Full Baked Atlases

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user visual approval
Priority: P0

## Summary

V24 addresses user feedback on V23:

- do not make soldiers bright
- make them shiny like the mockup
- make baked shadows visible on the background

V24 rebuilds from the V22 clean bodies, not from V23. It adds localized specular/rim highlights only, then rebakes larger darker horizontal-right shadows under every frame.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v24.json`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v24.png`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v24.png`
- Player idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v24.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v24.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v24.png`
- Enemy idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v24.png`
- Player numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v24_player_animation_atlas_numbered_grid.png`
- Enemy numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v24_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v24_specular_shadow_full_animation_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV24SpecularShadowIdlePlacement_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV24SpecularShadowIdlePlacement_vs_Target_Comparison.png`
- Target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV24SpecularShadowIdlePlacement_TargetAlignedCropReview.png`

## Imagegen Provenance

V24 uses the same fresh built-in-imagegen small RTS source chain as V22:

- Original generated file: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0192f4db49eec6d5016a0af5394734819b9d8a27690f1c8ad8.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV21/m01_v21_direction_locked_animation_source_alpha.png`

Postprocess:

- localized helmet/shoulder/rifle specular pixels
- no broad brightness lift
- stronger wider baked horizontal-right foot shadows
- atlas packing and validation

No target mockup crops were pasted into delivered art.

## Validation

- Manifest parse: passed.
- Top-level manifest-declared missing files: `0`.
- Alpha-missing declared unit PNGs: `0`.
- Clean body cell failures: `0`.
- Player animation atlas: `4096x1792`.
- Enemy animation atlas: `4096x1792`.
- Shadows are baked into every animation frame; no separate runtime shadow atlas is required.

## Visual Assessment

Compared with V23:

- shine is more specular and less broad-bright
- player remains dark navy/charcoal
- enemy remains dark charcoal/black
- helmet and upper armor highlights are more target-like
- foot shadows are visibly darker and wider on the placement proof

Review risk:

- The target mockup also has health bars/rings on enemies; those are outside this Art/Atlas soldier package.
- PM/user should approve whether the shadow strength now matches the plate, or if it should be one step darker.

## Gameplay Binding Checklist

- Bind player animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v24.png`
- Bind enemy animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v24.png`
- Optional idle/facing atlases:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v24.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV24/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v24.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Pivot: `[128,210]`.
- Direction mapping remains screen-space locked as in V22/V23.
- Do not bind V18/V19/V20.
- Do not bind V7 through V16 animation atlases.

## Routing

V24 is ready for PM/user visual review specifically for non-bright specular shine and visible baked shadows.
