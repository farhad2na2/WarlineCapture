# Art/Atlas M01 V29 Overscan Background And Tintable POT Soldier Assets

Date: 2026-05-19
Owner: Art/Atlas
Status: delivered; Gameplay binding needed for mask tint path
Priority: P0

Correction note: the V29 runtime body atlas was corrected after handoff to remove residual baked faction hue. See `Design/AgentReports/2026-05-19_art-atlas_m01-v29-faction-free-soldier-correction.md`. The misleading colored tint preview was replaced with a neutral-only proof; package presentation should show only the gray faction-free body and the white/alpha mask.

Second correction note: user review found the first neutral V29 soldier package had one facing duplicated into every direction block. That package has now been replaced by `Design/AgentReports/2026-05-19_art-atlas_m01-v29-corrected-all-direction-pot-soldier.md`, with a rebuilt imagegen-source all-direction POT atlas.

## Summary

Delivered the updated V29 Art assets requested by the user:

- a no-buildings M01 tactical background package with painted overscan coverage for 16:9, 20:9, and 21:9;
- a single shared POT soldier atlas that is faction-free, plus a matching faction mask atlas so Gameplay can color factions without duplicating baked soldier atlases.

This supersedes the earlier baked-red-enemy-only direction. The red enemy color should now come from the V29 mask tint path, not from a separate red-only body atlas.

## Background Output

- Imagegen source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV29/m01_tactical_start_clean_plate_v29_overscan_imagegen_source.png`
- Runtime source plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_source.png`
- Runtime POT plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`
- Background manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_background_manifest_v29.json`
- Aspect proof sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_overscan_no_buildings_aspect_crop_proof.png`
- 16:9 proof: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_16x9.png`
- 20:9 proof: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_20x9.png`
- 21:9 proof: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_21x9.png`
- 2560x1080 proof: `Design/AgentReports/Captures/M01_TargetMatchV29OverscanNoBuildings_2560x1080.png`

Background dimensions:

- imagegen source: `1672x941`
- packaged runtime/POT plate: `4096x2048`

Background source notes:

- built-in imagegen original: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_0254c1f907ed28fc016a0c301916b88199b2ac98256f734ac1.png`
- visual constraint: no buildings, no tall structures, no UI, no units; ground-level roads, rubble, low cover, debris, fires, and smoke only
- no stretched edge extension, blurred edge fill, solid color fill, or pasted target pixels

## Background Crop Notes

Use center anchoring from the `4096x2048` POT plate.

- 16:9: source crop `[227, 0, 3641, 2048]`, proof output `1920x1080`
- 20:9: source crop `[0, 102, 4096, 1843]`, proof output `2400x1080`
- 21:9: source crop `[0, 146, 4096, 1755]`, proof output `2520x1080`
- 2560x1080: source crop `[0, 160, 4096, 1728]`, proof output `2560x1080`

## Soldier Output

- Soldier manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- Shared body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Shared clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- Shared faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- Idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`
- Body atlas grid proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_pot_numbered_grid.png`
- Mask atlas grid proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_faction_mask_numbered_grid.png`
- Tint preview contact: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_tint_preview_contact.png`

Soldier atlas contract:

- texture size: `4096x2048`
- used grid: `16 x 7`
- POT grid: `16 x 8`
- cell size: `256x256`
- used frames: `112`
- transparent pad row: row `7`, y `1792..2047`
- pivot: `[128,212]`
- direction keys: `screen_locked_A`, `screen_locked_B`, `screen_locked_C`, `screen_locked_D`
- frame order and animation timing: inherited from accepted V28 player atlas

## Soldier Source And Tint Contract

Source basis:

- V28 accepted imagegen-derived player soldier geometry/scale/facing package
- source manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v28.json`
- source atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`

Runtime tint contract:

- draw `soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png` normally;
- draw `soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png` over the same frame rect, pivot, transform, UV scale, and UV offset using faction color;
- do not material-tint the whole body atlas, because that would tint baked shadows, graphite shading, highlights, and muzzle flashes;
- mask is intentionally limited to accent panels and markings, not the entire body.

Suggested faction colors:

- player blue: `#2A82D2`
- enemy red: `#B41A16`
- ally green: `#369652`

## Validation

- Background manifest parse: passed.
- Soldier manifest parse: passed.
- Declared output files: present.
- Soldier used cell failures: `0`.
- POT pad row nonzero alpha pixels: `0`.
- Faction mask nonzero alpha pixels: `67636`.
- Background proofs generated for 16:9, 20:9, 21:9, and 2560x1080.
- No source docs or lane task files modified.

## Gameplay Handoff

Bind background:

- preferred runtime background: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`

Bind soldiers:

- base atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- mask atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

Gameplay must add or route through a mask tint draw path. If Gameplay cannot add the mask path in this pass, Art can still produce a baked red enemy fallback, but that is no longer the preferred user-requested contract.
