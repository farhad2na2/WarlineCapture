# Art/Atlas Handoff - M01 V32 Corrected Direction Read Assets

Date: 2026-05-19
Lane: Art/Atlas
Status: delivered to PM; implementation-ready
Priority: P0

## Summary

Delivered a new V32 soldier package after PM rejected the V31 runtime proof for incorrect `up_right` visual read.

V32 is not a label-only remap. Pose pixels were rebuilt from a new imagegen 8-direction source sheet, then alpha-cleaned, component-cleaned, converted to neutral white/gray, and repacked into a new POT atlas under `DirectionLockedV32`.

No baked faction color variants are included. The optional white/alpha mask atlas remains only as a technical companion with identical rects/pivots/UVs.

## Relevant Handoffs Assessed

- `Design/AgentReports/2026-05-19_gameplay_m01-v31-8dir-runtime-proof.md` - accepted for runtime binding proof; rejected for Art direction read.
- `Design/AgentReports/2026-05-19_pm_gameplay-v31-runtime-rejected-art-v32-direction-dispatch.md` - accepted as the active V32 dispatch.

## Direction Coverage

V32 includes all eight directions:

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

Fire frames intentionally use the clean aim/firing source row and skip the imagegen muzzle-flash row so the atlas remains neutral grayscale and avoids colored muzzle artifacts.

## Files Delivered

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v32.json`
- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v32_pot_4096x4096.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_clean_body_atlas_v32_pot_4096x4096.png`
- Optional technical white/alpha mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_faction_mask_atlas_v32_pot_4096x4096.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_idle_facings_body_shadow_atlas_v32.png`
- Optional technical idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_idle_facings_faction_mask_atlas_v32.png`

## Source And Proof

- Built-in imagegen original: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_06fbf57442e4395f016a0c9838f7a88191a977dd65f4f20f1a.png`
- Workspace imagegen source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV32/SharedSoldier/m01_v32_8dir_imagegen_source_attempt01.png`
- Alpha-cleaned source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV32/SharedSoldier/m01_v32_8dir_imagegen_source_attempt01_alpha.png`
- Selected source proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v32_selected_imagegen_source_alpha_proof.png`
- Full 224-frame neutral proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v32_corrected_8dir_full_224_frame_proof.png`
- 8-direction state proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v32_corrected_8dir_direction_state_proof.png`
- Runtime-orientation proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v32_corrected_up_right_runtime_orientation_proof.png`

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

## Direction Correction

- Critical key: `up_right`.
- Opposite key: `down_left`.
- Pose pixels changed from V31: yes.
- Label-only change: no.
- V32 uses new imagegen source pixels and component-cleaned pose extraction, not V31 relabeling.

## Validation

- Manifest JSON parse: passed.
- Body atlas size: `4096x4096`.
- Mask atlas size: `4096x4096`.
- Empty used cells: `[]`.
- Edge/clipping failures: `[]`.
- Small non-death body cells: `[]`.
- Transparent POT pad rows nonzero alpha pixels: `0`.
- Body atlas nontransparent pixels: `1375004`.
- Body atlas saturated visible pixels: `0`.
- Body atlas detected red/green/blue pixels: `0/0/0`.
- Optional mask nontransparent pixels: `148751`.
- Optional mask-to-body coverage ratio: `0.108`.

## Gameplay Binding Notes

Bind this V32 body atlas:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v32_pot_4096x4096.png`

Use this V32 manifest:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v32.json`

Runtime direction keys remain:

- player/bottom target-facing key: `up_right`
- enemy/opposite key: `down_left`

Do not bind V31 for the next runtime proof. Do not bind colored preview images. Do not apply whole-body tint to the neutral atlas.

Optional mask path, only if PM/user explicitly resumes faction mask tinting:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_faction_mask_atlas_v32_pot_4096x4096.png`

## Known Gaps

Art/Atlas has produced the corrected implementation-ready package and local proofs. Gameplay still needs to bind V32 and rerun the same runtime proof before PM can accept final visual direction in-engine.

Gameplay can resume with V32.
