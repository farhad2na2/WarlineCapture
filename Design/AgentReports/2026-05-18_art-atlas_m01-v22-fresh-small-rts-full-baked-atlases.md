# Art/Atlas M01 V22 Fresh Small RTS Full Baked Atlases

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user visual approval
Priority: P0

## Summary

V22 replaces the rejected V18/V19/V20 direction attempts with a fresh small RTS-scale imagegen source and full baked-shadow atlases.

The package includes:

- fresh target-oriented player and enemy soldier sprites generated at small RTS scale
- direction-locked idle placement proof on the v6 plate
- full `112` frame player and enemy animation atlases
- baked horizontal-right shadows in every frame
- clean-cell validation with zero failures

This is not marked as user-approved final art. It is the first V21/V22 candidate that passes the internal target-aligned visual proof well enough to send for review.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v22.json`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v22.png`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v22.png`
- Player idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v22.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v22.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v22.png`
- Enemy idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v22.png`
- Player numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v22_player_animation_atlas_numbered_grid.png`
- Enemy numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v22_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v22_full_animation_baked_shadow_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV22FullAtlasIdlePlacement_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV22FullAtlasIdlePlacement_vs_Target_Comparison.png`
- Target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV22FullAtlasIdlePlacement_TargetAlignedCropReview.png`

## Imagegen Provenance

Fresh built-in imagegen source:

- Original generated file: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0192f4db49eec6d5016a0af5394734819b9d8a27690f1c8ad8.png`
- Workspace chromakey source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV21/m01_v21_direction_locked_animation_source_chromakey.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV21/m01_v21_direction_locked_animation_source_alpha.png`

The generated sheet contains player and enemy pose variants for idle, run, aim, fire, damaged, and death states. V22 postprocess scales and tones those imagegen-authored poses for the target crop, then packs them into the standard atlas layout. No target mockup crops were pasted into delivered art.

## Dimensions

- Player animation atlas: `4096x1792`
- Enemy animation atlas: `4096x1792`
- Player idle/facing atlas: `1024x256`
- Enemy idle/facing atlas: `1024x256`
- Player numbered grid: `2048x938`
- Enemy numbered grid: `2048x938`
- Target-aligned crop review: `1604x1080`
- Target comparison: `3840x1080`

## Direction Mapping

V22 is screen-space locked. The standard four-direction layout is filled for ECS compatibility, but each row preserves the M01 screen-space read instead of attempting compass-facing semantics.

| Direction key | Faction | Screen-space read |
| --- | --- | --- |
| `screen_locked_A` | player rifle squad | `player_bottom_faces_up_screen` |
| `screen_locked_B` | player rifle squad | `player_bottom_faces_up_screen` |
| `screen_locked_C` | player rifle squad | `player_bottom_faces_up_screen` |
| `screen_locked_D` | player rifle squad | `player_bottom_faces_up_screen` |
| `screen_locked_A` | enemy patrol | `enemy_top_faces_down_screen` |
| `screen_locked_B` | enemy patrol | `enemy_top_faces_down_screen` |
| `screen_locked_C` | enemy patrol | `enemy_top_faces_down_screen` |
| `screen_locked_D` | enemy patrol | `enemy_top_faces_down_screen` |

## Animation Coverage

Each faction has a full `112` frame atlas:

- `idle`: `4` frames
- `run`: `8` frames
- `aim`: `3` frames
- `fire`: `4` frames
- `damaged`: `3` frames
- `death`: `6` frames
- repeated across four ECS-compatible screen-locked rows

## Validation

- Manifest parse: passed.
- Manifest-declared missing files: `0`.
- Alpha-missing declared unit PNGs: `0`.
- Clean body cell failures: `0`.
- Player source pose count: `12`.
- Enemy source pose count: `12`.
- Shadows are baked into every animation frame; no separate runtime shadow atlas is required.

## Visual Assessment

V22 is a significant improvement over V18/V19/V20:

- no fused/doubled bodies
- no cutout construction
- smaller target-crop scale
- darker player tone with less bright blue
- enemy reads as down-screen without oversized closeup proportions
- placement proof uses the v6 plate and target-aligned crops

Remaining review risk:

- PM/user still needs to approve whether the soldier silhouettes match the target closely enough.
- Health bars/selection rings are not part of this Art/Atlas package, so the enemy target crop includes UI elements that V22 does not include.

## Gameplay Binding Checklist

- Bind player animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v22.png`
- Bind enemy animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v22.png`
- Optional idle/facing atlases for M01-01 no-selection proof:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v22.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV22/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v22.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Pivot: `[128,210]`.
- Per-frame frame rects and anchors: declared in `m01_direction_locked_soldier_manifest_v22.json`.
- Shadows are integrated/baked; do not bind separate shadow atlases.
- Do not bind V7 through V16 animation atlases.
- Do not treat V18/V19/V20 as valid.

## Routing

V22 is ready for PM/user visual review. If accepted, route Gameplay to bind V22 and regenerate runtime proof.
