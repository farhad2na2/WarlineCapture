# Art/Atlas M01 V29 Corrected All-Direction Cutoff Rejected

Date: 2026-05-19
Owner: Art/Atlas
Status: needs fixes
Priority: P0

## Summary

User review rejected the corrected all-direction V29 proof because several sprites are visibly cut off or fragmented.

The direction uniqueness issue was fixed, but the package is still not implementation-ready because the visual proof shows incomplete poses, especially in aim/fire/death rows.

## Rejected Proof

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_direction_uniqueness_proof.png`

## Assessment

Needs fixes.

The current V29 all-direction soldier package must not be treated as accepted or Gameplay-ready until every used atlas cell is visually verified to contain exactly one complete pose.

## Required Fix

Rebuild the V29 POT soldier package with a stricter visual gate:

- all four directions must be unique;
- all sequence cells must be populated;
- every cell must contain exactly one full pose;
- no cut-off torsos, heads, boots, weapons, or separated fragments;
- no colored faction pixels in the body atlas;
- maintain `4096x2048` POT layout, `16 x 7` used grid, `256x256` cells, transparent row 7.

## Routing

Art/Atlas remains owner. Gameplay remains held for soldier binding.
