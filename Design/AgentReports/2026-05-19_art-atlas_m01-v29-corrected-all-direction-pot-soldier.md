# Art/Atlas M01 V29 Corrected All-Direction POT Soldier Atlas

Date: 2026-05-19
Owner: Art/Atlas
Status: corrected replacement delivered; Gameplay binding can resume for soldier atlas
Priority: P0

## Summary

Delivered a corrected V29 soldier package that replaces the invalid duplicate-direction atlas.

The previous V29 package had four direction labels but repeated one facing in every direction block. This package is rebuilt from a new imagegen source with four real facings and all sequence cells repacked into the required POT layout.

## Corrected Runtime Files

- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- Idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`
- Soldier manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

## Source

- Built-in imagegen original: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_044c216e451e6f74016a0c3b297e308191b2b6022e7b1596d0.png`
- Workspace source copy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV29/SharedSoldier/m01_v29_shared_soldier_all_direction_imagegen_source.png`

The source contains four visual columns:

- up-screen / away
- down-screen / toward
- left
- right

The source contains ten pose rows. Those rows are mapped into the existing V29 animation timing:

- source rows `0/1`: idle
- source rows `2/3`: run
- source row `4`: aim
- source rows `5/6`: fire
- source row `7`: damaged
- source rows `8/9`: death

Repeats are used only inside animation timing. Direction blocks are not duplicated.

## Layout

- Texture size: `4096x2048`
- Used grid: `16 x 7`
- POT grid: `16 x 8`
- Cell size: `256x256`
- Used frames: `112`
- Transparent pad row: row `7`, y `1792..2047`
- Pivot: `[128,212]`
- Direction keys: `screen_locked_A`, `screen_locked_B`, `screen_locked_C`, `screen_locked_D`

## Proof Files

- Neutral all-sequence proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_neutral_runtime_contact.png`
- Direction uniqueness proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_direction_uniqueness_proof.png`
- Numbered neutral atlas grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_all_direction_pot_numbered_grid.png`
- Numbered mask atlas grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_all_direction_mask_numbered_grid.png`
- White/alpha mask proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_mask_white_only_contact.png`

## Validation

- Manifest parse: passed.
- Used cell failures: `[]`.
- POT pad row nonzero alpha pixels: `0`.
- Runtime body atlas saturated visible pixels: `0`.
- Runtime body atlas detected red pixels: `0`.
- Runtime body atlas detected green pixels: `0`.
- Runtime body atlas detected blue pixels: `0`.
- Mask atlas saturated visible pixels: `0`.
- Mask atlas detected red pixels: `0`.
- Mask atlas detected green pixels: `0`.
- Mask atlas detected blue pixels: `0`.

Representative direction pair pixel differences are all nonzero:

- idle0: `[2368, 2460, 2463, 2314, 2551, 2525]`
- run0: `[2474, 2516, 2622, 2678, 2628, 2712]`
- aim0: `[1713, 1830, 2100, 1885, 2131, 2166]`
- fire0: `[1702, 1786, 2054, 1785, 2069, 2123]`
- death0: `[1200, 976, 1110, 1173, 1040, 1066]`

## Gameplay Binding

Use the V29 manifest and the corrected neutral body atlas.

If faction coloring is needed, draw the white/alpha mask atlas over the same frame rect, pivot, transform, and UV scale/offset using runtime faction color. Do not tint the whole body atlas.

## Supersedes

This report supersedes:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-direction-block-invalid.md`
- the earlier duplicate-direction V29 soldier package described in `Design/AgentReports/2026-05-19_art-atlas_m01-v29-faction-free-soldier-correction.md`
