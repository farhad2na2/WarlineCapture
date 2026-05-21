# Art/Atlas Handoff - M01 V30 Diagonal Neutral Soldier Package

Date: 2026-05-19
Lane: Art/Atlas
Status: delivered to PM; implementation-ready
Priority: P0

## Summary

Delivered the V30 shared soldier package for M01 with real isometric diagonal facings and a neutral white/gray body+shadow atlas. This package supersedes the V29 soldier atlas for runtime soldier binding because V29 only had cardinal/screen-locked facings.

Latest user direction is applied: present and deliver one neutral white/gray soldier look only. Do not use baked faction color variants or colored preview sheets for approval. The matching mask atlas remains packaged as an optional technical file with identical rects/pivots/UVs if PM later routes Gameplay to use mask tinting, but the visual approval target here is the neutral white/gray atlas.

## Relevant Handoffs Assessed

- `Design/AgentReports/2026-05-19_gameplay_m01-v29-all-faction-mask-runtime-proof.md` - accepted for technical runtime progress, rejected for final soldier art due cardinal-only directions.
- `Design/AgentReports/2026-05-19_pm_gameplay-v29-mask-runtime-rejected-art-v30-dispatch.md` - accepted as the active V30 dispatch.

## Files Delivered

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v30.json`
- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_animation_body_shadow_atlas_v30_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_animation_clean_body_atlas_v30_pot_4096x2048.png`
- Optional technical white/alpha mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_animation_faction_mask_atlas_v30_pot_4096x2048.png`
- Idle/facing body strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_idle_facings_body_shadow_atlas_v30.png`
- Optional technical idle/facing mask strip: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_idle_facings_faction_mask_atlas_v30.png`

## Source And Proof

- Imagegen source copy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV30/SharedSoldier/m01_v30_diagonal_shared_soldier_imagegen_source.png`
- Alpha-cleaned source: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV30/SharedSoldier/m01_v30_diagonal_shared_soldier_imagegen_source_alpha.png`
- Neutral full 112-frame proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v30_neutral_white_gray_full_112_frame_proof.png`
- Neutral diagonal direction proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v30_neutral_white_gray_direction_proof.png`
- Neutral runtime-scale proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v30_neutral_white_gray_runtime_scale_proof.png`
- Optional technical white/alpha mask grid: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v30_diagonal_full_112_frame_mask_proof.png`

## Layout Contract

- Texture size: `4096x2048`, POT.
- Cell size: `256x256`.
- Used grid: `16 x 7`.
- POT grid: `16 x 8`.
- Used frames: `112`.
- Transparent unused POT row: row 7, `y=1792..2047`.
- Pivot: `[128, 212]`.
- Directions: `diag_up_right`, `diag_up_left`, `diag_down_right`, `diag_down_left`.
- Direction counts: 28 frames per diagonal direction.
- State counts: idle 16, run 32, aim 12, fire 16, damaged 12, death 24.

## Validation

- Manifest JSON parse: passed.
- Body atlas size: `4096x2048`.
- Mask atlas size: `4096x2048`.
- Empty used cells: `[]`.
- Edge/clipping failures: `[]`.
- Transparent POT pad row nonzero alpha pixels: `0`.
- Body atlas nontransparent pixels: `261060`.
- Body atlas saturated visible pixels: `0`.
- Body atlas detected red/green/blue pixels: `0/0/0`.
- Optional mask nontransparent pixels: `85677`.
- Optional mask-to-body coverage ratio: `0.328`.

## Gameplay Binding Notes

Bind the V30 neutral atlas for every soldier:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_animation_body_shadow_atlas_v30_pot_4096x2048.png`

Use the V30 manifest for frame rects, pivots, direction keys, state names, and sequence order:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v30.json`

Do not bind any old V29 soldier atlas for M01 final soldier direction proof. Do not bind colored preview images. Do not apply whole-body material tint to the neutral atlas.

If PM later reopens faction color readability, use the optional mask atlas with identical rect/pivot/transform/UV; do not tint the base body atlas:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV30/SharedSoldier/soldier_diagonal_animation_faction_mask_atlas_v30_pot_4096x2048.png`

## Background Note

No V30 background change was requested in the current Art task. The existing accepted no-building overscan background remains:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`

## Known Gaps

The PM dispatch originally requested blue/red mask-only preview proof. The latest user direction supersedes that visual presentation request for this handoff: only the neutral white/gray soldier look should be reviewed now. If PM wants colored mask preview again, route that explicitly back to Art/Atlas.

Gameplay can resume binding the V30 diagonal neutral soldier package and produce a fresh runtime proof.
