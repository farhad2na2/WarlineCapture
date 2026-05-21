# Art/Atlas M01 V29 PM Delivery - All-Faction Soldier Atlas And Overscan Background

Date: 2026-05-19
Owner: Art/Atlas
Status: delivered to PM; implementation-ready after user approval
Priority: P0

## Summary

Delivered the user-approved V29 Art/Atlas package for M01:

- one POT soldier atlas for all factions;
- optional matching faction mask atlas for runtime faction coloring;
- oversized no-building tactical background with aspect coverage for 16:9, 20:9, and 21:9.

Important PM note: the soldier atlas is intentionally **faction-free** and should be used for all factions. It replaces the earlier baked-red-only direction from the PM dispatch based on user instruction. Enemy red, player blue, or other faction colors must come from the faction mask/runtime color path, not from separate baked body atlases.

## Soldier Runtime Package

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- Idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`

## Soldier Proof

- Full 112-frame proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_full_112_frame_proof.png`
- Direction sample proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_direction_sample_no_cache.png`

## Soldier Validation

- Atlas size: `4096x2048`, POT.
- Cell size: `256x256`.
- Used grid: `16 x 7`.
- POT grid: `16 x 8`.
- Used frames: `112`.
- Direction blocks: `screen_locked_A`, `screen_locked_B`, `screen_locked_C`, `screen_locked_D`.
- Used cell failures: `[]`.
- Cell edge failures: `[]`.
- Transparent unused POT row: `0` nonzero alpha pixels.
- Body atlas saturated visible pixels: `0`.
- Body atlas detected red/green/blue pixels: `0/0/0`.
- User approved delivery after confirming POT and all directions.

## Background Runtime Package

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_background_manifest_v29.json`
- Runtime POT background: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`
- Runtime source background: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_source.png`
- Imagegen source copy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV29/m01_tactical_start_clean_plate_v29_overscan_imagegen_source.png`
- Aspect proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_overscan_no_buildings_aspect_crop_proof.png`

## Background Validation

- Runtime background size: `4096x2048`, POT.
- Background has no buildings/tall structures and no unit/UI content.
- Aspect crops generated for:
  - 16:9: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_16x9.png`
  - 20:9: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_20x9.png`
  - 21:9: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_21x9.png`
  - 2560x1080: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_2560x1080.png`
- Background manifest parse: passed.

## Gameplay Binding Notes

Bind the background:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`

Bind the soldier base atlas for every faction:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`

For faction color, bind the mask overlay using the same rect, pivot, transform, and UV scale/offset:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`

Do not tint the whole body atlas. Do not bind any old rejected proof/candidate image as runtime art.

## Supersedes

This delivery supersedes the earlier rejected V29 soldier attempts and the earlier baked-red enemy-only request. The current user-approved direction is a shared all-faction POT soldier atlas plus faction mask.

Gameplay can resume binding the V29 background and all-faction soldier package.
