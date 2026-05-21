# Art/Atlas Handoff - M01 V31 8-Direction Neutral Soldier Package

Date: 2026-05-19
Lane: Art/Atlas
Status: delivered to PM; implementation-ready
Priority: P0

## Summary

User asked whether all 8 directions were complete after the V30 disconnect. Answer: V30 was only the four required diagonal facings. V31 now delivers a single POT soldier package with all 8 directions and all animation sequences, in one neutral white/gray body+shadow atlas.

No colored faction body variants are included. The optional white/alpha mask atlas remains packaged only as a technical companion with identical rects/pivots/UVs, in case PM later routes mask tinting back to Gameplay.

## Direction Coverage

V31 includes 8 directions:

- `up`
- `up_right`
- `right`
- `down_right`
- `down`
- `down_left`
- `left`
- `up_left`

Each direction has 28 frames.

## Animation Coverage

Frame counts across the full atlas:

- idle: 32 frames
- run: 64 frames
- aim: 24 frames
- fire: 32 frames
- damaged: 24 frames
- death: 48 frames

Fire frames are intentionally remapped to clean aim/firing poses per direction to avoid inherited generated fragment/cut cells.

## Files Delivered

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v31.json`
- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v31_pot_4096x4096.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_clean_body_atlas_v31_pot_4096x4096.png`
- Optional technical white/alpha mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_faction_mask_atlas_v31_pot_4096x4096.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_idle_facings_body_shadow_atlas_v31.png`
- Optional technical idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_idle_facings_faction_mask_atlas_v31.png`
- Source notes: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV31/SharedSoldier/m01_v31_8dir_source_notes.md`

## Proofs

- Full 224-frame neutral proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_full_224_frame_proof.png`
- 8-direction state proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_direction_state_proof.png`
- Runtime-scale neutral proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_runtime_scale_proof.png`

## Source Method

V31 is a deterministic repack of existing imagegen-derived neutral packages:

- V29 cardinal package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- V30 diagonal package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v30.json`

V29 and V30 were not overwritten. V31 is delivered under `DirectionLockedV31`.

## Layout Contract

- Texture size: `4096x4096`, POT.
- Cell size: `256x256`.
- Used grid: `16 x 14`.
- POT grid: `16 x 16`.
- Used frames: `224`.
- Transparent unused POT rows: rows 14..15, `y=3584..4095`.
- Pivot: `[128, 212]`.
- Directions: 8.
- Frames per direction: 28.

## Validation

- Manifest JSON parse: passed.
- Body atlas size: `4096x4096`.
- Empty used cells: `[]`.
- Edge/clipping failures: `[]`.
- Transparent POT pad rows nonzero alpha pixels: `0`.
- Body atlas nontransparent pixels: `519843`.
- Body atlas saturated visible pixels: `0`.
- Body atlas detected red/green/blue pixels: `0/0/0`.
- Optional mask nontransparent pixels: `166024`.
- Optional mask-to-body coverage ratio: `0.319`.

## Gameplay Binding Notes

Bind this V31 atlas if Gameplay needs all 8 directions:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v31_pot_4096x4096.png`

Use this manifest:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v31.json`

Do not bind colored preview images. Do not apply whole-body tint to the neutral body atlas.

Optional mask path, only if PM/user explicitly resumes faction mask tinting:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_faction_mask_atlas_v31_pot_4096x4096.png`

## Background Note

No background change was requested in this continuation. The accepted no-building overscan background remains:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`

Gameplay can resume with V31 if PM/user wants the all-8-direction package rather than the V30 four-diagonal package.
