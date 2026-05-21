# Art/Atlas M01 V29 Direction Block Invalid

Date: 2026-05-19
Owner: Art/Atlas
Status: needs fixes
Priority: P0

## Summary

User review found that the V29 shared soldier atlas proof only shows one direction. Verification confirms this is a real defect.

The V29 shared soldier atlas has four labeled direction blocks, but the pixels are duplicated across the direction blocks. The package is therefore not a valid all-direction soldier atlas.

## Verification

Compared representative frame groups in:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`

Results:

- idle frame 0: cells `0`, `28`, `56`, `84` are identical.
- run frame 0: cells `4`, `32`, `60`, `88` are identical.
- aim frame 0: cells `12`, `40`, `68`, `96` are identical.
- fire frame 0: cells `16`, `44`, `72`, `100` are identical.
- death frame 0: cells `22`, `50`, `78`, `106` are identical.

Manifest labels exist for:

- `screen_locked_A`
- `screen_locked_B`
- `screen_locked_C`
- `screen_locked_D`

But those labels do not correspond to unique visual facings in the current V29 atlas.

## Assessment

Needs fixes.

The current neutral/faction-free color correction is valid, but the all-direction requirement is not met. Gameplay should not bind this V29 soldier package as a completed all-direction atlas.

## Required Fix

Regenerate or source real distinct facings for the neutral faction-free soldier atlas:

- up-screen / away
- down-screen / toward
- left
- right

Then rebuild:

- neutral body+shadow POT atlas
- clean body POT atlas
- white/alpha faction mask POT atlas
- idle/facing strip
- contact sheets proving the four unique directions
- manifest with the same `16 x 7` used grid, `256x256` cells, and `4096x2048` POT layout

## Routing

Art/Atlas remains owner. Gameplay remains held for soldier binding until a corrected all-direction package is delivered.
