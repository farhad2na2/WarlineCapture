# Art/Atlas M01 V29 Complete-Pose Repack

Date: 2026-05-19
Owner: Art/Atlas
Status: corrected replacement delivered; visual review still recommended before Gameplay bind
Priority: P0

## Summary

Repacked the V29 all-direction soldier atlas after user review found cutoff/fragmented sprites in the prior proof.

The prior all-direction attempt is rejected. This package uses the same imagegen source but replaces the custom chroma cleanup with helper alpha extraction, then crops from full source grid slots to avoid cutting bodies, weapons, boots, or shadows.

## Corrected Runtime Files

- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- Idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`
- Soldier manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

## Source

- Built-in imagegen original: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_044c216e451e6f74016a0c3b297e308191b2b6022e7b1596d0.png`
- Workspace source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV29/SharedSoldier/m01_v29_shared_soldier_all_direction_imagegen_source.png`
- Workspace alpha source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV29/SharedSoldier/m01_v29_shared_soldier_all_direction_imagegen_source_alpha.png`

## Proof Files

- Direction/complete-pose proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_direction_uniqueness_proof.png`
- Neutral all-sequence proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_neutral_runtime_contact.png`
- Numbered neutral atlas grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_all_direction_pot_numbered_grid.png`
- Numbered mask atlas grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_all_direction_mask_numbered_grid.png`
- White/alpha mask proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_mask_white_only_contact.png`

The proof backgrounds are medium gray for review only, so dark legs and baked shadows are visible. Runtime atlas backgrounds remain transparent.

## Validation

- Texture size: `4096x2048`.
- Used grid: `16 x 7`.
- POT grid: `16 x 8`.
- Cell size: `256x256`.
- Used frames: `112`.
- Used cell failures: `[]`.
- Cell edge failures: `[]`.
- POT pad row nonzero alpha pixels: `0`.
- Runtime body atlas saturated visible pixels: `0`.
- Runtime body atlas detected red pixels: `0`.
- Runtime body atlas detected green pixels: `0`.
- Runtime body atlas detected blue pixels: `0`.
- Manifest parse: passed.

Representative direction pair pixel differences are all nonzero:

- idle0: `[2500, 2624, 2451, 2440, 2623, 2670]`
- run0: `[2472, 2508, 2510, 2752, 2656, 2771]`
- aim0: `[1851, 1935, 2219, 2003, 2270, 2314]`
- fire0: `[1960, 2052, 2190, 2046, 2260, 2310]`
- death0: `[1087, 1117, 1171, 1110, 1190, 1180]`

## Supersedes

This supersedes:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-corrected-all-direction-cutoff-rejected.md`
- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-corrected-all-direction-pot-soldier.md`
- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-direction-block-invalid.md`

## Gameplay Binding

Gameplay can bind only after PM/user visual approval of the new proof. Use the V29 manifest and corrected neutral atlas. If faction color is needed, draw the white/alpha mask atlas over the same frame rect/pivot/transform with runtime color; do not tint the body atlas.
