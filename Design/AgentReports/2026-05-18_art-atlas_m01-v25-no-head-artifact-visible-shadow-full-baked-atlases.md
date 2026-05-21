# Art/Atlas M01 V25 No Head Artifact Visible Shadow Full Baked Atlases

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user visual approval
Priority: P0

## Summary

V25 replaces rejected V24. It removes the artificial helmet/head glint artifact and keeps the stronger visible baked shadows.

V25 is rebuilt from V22 clean bodies, not from the rejected V24 atlas. It uses only source-derived sparse edge/detail highlights, with no manually drawn head dots or ellipses.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v25.json`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v25.png`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v25.png`
- Player idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v25.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v25.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v25.png`
- Enemy idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v25.png`
- Player numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v25_player_animation_atlas_numbered_grid.png`
- Enemy numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v25_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v25_no_head_artifact_full_animation_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV25NoHeadArtifactIdlePlacement_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV25NoHeadArtifactIdlePlacement_vs_Target_Comparison.png`
- Target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV25NoHeadArtifactIdlePlacement_TargetAlignedCropReview.png`

## Imagegen Provenance

V25 uses the same fresh built-in-imagegen small RTS source chain as V22:

- Original generated file: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0192f4db49eec6d5016a0af5394734819b9d8a27690f1c8ad8.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV21/m01_v21_direction_locked_animation_source_alpha.png`

Postprocess:

- removed V24 artificial head-glint drawing
- source-only sparse edge/specular highlights
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

Compared with V24:

- visible head artifact removed
- no manual dot/ellipse highlight on helmets
- shadows remain visible under boots
- soldiers remain dark rather than broadly bright

Review risk:

- The shine is now more conservative because artificial head highlights were removed.
- PM/user should approve whether this balance is acceptable or whether a new imagegen source with naturally shinier helmets is needed.

## Gameplay Binding Checklist

- Bind player animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v25.png`
- Bind enemy animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v25.png`
- Optional idle/facing atlases:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v25.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV25/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v25.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Pivot: `[128,210]`.
- Direction mapping remains screen-space locked as in V22/V23.
- Do not bind V18/V19/V20.
- Do not bind rejected V24.
- Do not bind V7 through V16 animation atlases.

## Routing

V25 is ready for PM/user visual review specifically for removed head artifact and retained visible baked shadows.
