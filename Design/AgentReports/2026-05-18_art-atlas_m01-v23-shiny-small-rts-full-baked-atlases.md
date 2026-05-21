# Art/Atlas M01 V23 Shiny Small RTS Full Baked Atlases

Date: 2026-05-18
Owner: Art/Atlas
Status: review candidate; needs PM/user visual approval
Priority: P0

## Summary

V23 is a restrained shine/highlight pass over the V22 fresh small RTS-scale atlases. It keeps the V22 scale, direction lock, and baked shadows, but lifts existing helmet, shoulder, armor, and rifle highlights so the soldiers pop more against the M01 asphalt like the target mockup.

This is not marked as user-approved final art.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v23.json`
- Player animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v23.png`
- Player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v23.png`
- Player idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v23.png`
- Enemy animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v23.png`
- Enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v23.png`
- Enemy idle/facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v23.png`
- Player numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v23_player_animation_atlas_numbered_grid.png`
- Enemy numbered grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v23_enemy_animation_atlas_numbered_grid.png`
- Combined contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v23_shiny_full_animation_baked_shadow_contact.png`
- Placement proof: `Design/AgentReports/Captures/M01_TargetMatchV23ShinyFullAtlasIdlePlacement_1920x1080.png`
- Target comparison: `Design/AgentReports/Captures/M01_TargetMatchV23ShinyFullAtlasIdlePlacement_vs_Target_Comparison.png`
- Target-aligned crop review: `Design/AgentReports/Captures/M01_TargetMatchV23ShinyFullAtlasIdlePlacement_TargetAlignedCropReview.png`

## Imagegen Provenance

V23 keeps the same fresh built-in-imagegen source as V22:

- Original generated file: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0192f4db49eec6d5016a0af5394734819b9d8a27690f1c8ad8.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV21/m01_v21_direction_locked_animation_source_alpha.png`

Postprocess is a selective highlight pass over imagegen-derived V22 bodies. It lifts existing bright pixels and small metal/helmet/rifle details while preserving dark base tone and baked horizontal-right shadows. No target mockup crops were pasted into delivered art.

## Validation

- Manifest parse: passed during generation.
- Clean body cell failures: `0`.
- Shadows remain baked into every animation frame.
- No separate runtime shadow atlas is required.

## Visual Assessment

Compared with V22:

- more visible helmet/shoulder/rifle highlights
- player remains dark navy/charcoal, not light blue
- enemy remains dark charcoal/black with small red accents
- target-aligned proof remains at the same scale/positions for direct comparison

Review risk:

- The shine is intentionally restrained. If the target needs stronger sparkle/specular pop, the next pass should increase highlights further while guarding against the earlier “too bright/light blue” problem.

## Gameplay Binding Checklist

- Bind player animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v23.png`
- Bind enemy animation atlas:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v23.png`
- Optional idle/facing atlases:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/PlayerRifleSquad/player_rifle_squad_idle_direction_locked_facings_body_shadow_atlas_v23.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV23/EnemyPatrol/enemy_patrol_idle_direction_locked_facings_body_shadow_atlas_v23.png`
- Cell size: `256x256`.
- Atlas columns: `16`.
- Pivot: `[128,210]`.
- Direction mapping remains screen-space locked as in V22.
- Do not bind V18/V19/V20.
- Do not bind V7 through V16 animation atlases.

## Routing

V23 is ready for PM/user visual review specifically for the added target-style shine.
