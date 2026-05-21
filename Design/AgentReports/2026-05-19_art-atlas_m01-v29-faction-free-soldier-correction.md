# Art/Atlas M01 V29 Faction-Free Soldier Atlas Correction

Date: 2026-05-19
Owner: Art/Atlas
Status: corrected; implementation-ready with Gameplay mask binding
Priority: P0

## Summary

The V29 shared soldier package has been corrected so the runtime body atlas is genuinely faction-free.

The colored red/blue/green rows are preview-only contact sheet examples and are not the runtime atlas. Runtime faction color must come only from the matching white/alpha faction mask overlay.

## Corrected Runtime Files

- Neutral body+shadow atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Neutral idle/facing strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- White/alpha idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`
- Updated manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

## Proof Files

- Neutral runtime body proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_neutral_runtime_contact.png`
- White/alpha mask proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_mask_white_only_contact.png`
- Body atlas numbered proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_pot_numbered_grid.png`
- Mask atlas numbered proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_faction_mask_numbered_grid.png`

The earlier colored preview sheet was misleading and has been replaced with a neutral-only proof. Runtime package presentation should show only the neutral gray soldier body and the white/alpha faction mask.

## Validation

- Body+shadow atlas dimensions: `4096x2048`, RGBA.
- Clean body atlas dimensions: `4096x2048`, RGBA.
- Faction mask atlas dimensions: `4096x2048`, RGBA.
- Used grid: `16 x 7`.
- POT grid: `16 x 8`.
- Cell size: `256x256`.
- Used frames: `112`.
- Body+shadow atlas saturated visible pixels after correction: `0`.
- Body+shadow atlas detected red pixels after correction: `0`.
- Body+shadow atlas detected green pixels after correction: `0`.
- Body+shadow atlas detected blue pixels after correction: `0`.
- Faction mask saturated visible pixels: `0`.
- Faction mask nontransparent pixels: `67636`.
- Manifest parse: passed.

## Gameplay Binding Rule

Draw the neutral body atlas normally, then draw the faction mask atlas over the same frame rect, pivot, transform, and UV scale/offset using the faction color.

Do not tint the whole body atlas. Do not use the tint preview contact sheet as an atlas.
