# M01 V31 8-Direction Shared Soldier Source Notes

Date: 2026-05-19
Lane: Art/Atlas

## Source Inputs

- V29 cardinal neutral soldier package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- V30 diagonal neutral soldier package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v30.json`

Both source packages are imagegen-derived and already alpha-cleaned, normalized to `256x256` cells, neutral white/gray body color, baked shadows, and matching optional white/alpha mask atlases.

## V31 Build Method

V31 repacks the V29 cardinal directions and V30 diagonal directions into one neutral white/gray 8-direction POT atlas.

Direction order:

- `up`
- `up_right`
- `right`
- `down_right`
- `down`
- `down_left`
- `left`
- `up_left`

Fire frames are intentionally mapped to clean aim/firing source poses per direction to avoid inherited generated fragment cells.

No source docs or lane task files were modified. No colored faction body variants were generated.
