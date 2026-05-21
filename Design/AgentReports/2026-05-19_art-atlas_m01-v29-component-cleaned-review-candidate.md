# Art/Atlas M01 V29 Component-Cleaned Review Candidate

Date: 2026-05-19
Owner: Art/Atlas
Status: review candidate; not accepted until user/PM approval
Priority: P0

## Summary

After user rejected the previous proof for cut frames, Art/Atlas rebuilt the V29 all-direction soldier atlas using connected-component filtering to remove detached fragments and avoid cut/cropped body pieces.

This is a review candidate, not an accepted handoff, until the proof is visually approved.

## Runtime Files Updated

- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Soldier manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

## Proof Files

- Direction proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_direction_uniqueness_proof.png`
- Full neutral atlas proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_neutral_runtime_contact.png`
- Numbered grid proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_all_direction_pot_numbered_grid.png`

## Validation

- Used cell failures: `[]`.
- Cell edge failures: `[]`.
- POT pad row nonzero alpha pixels: `0`.
- Body atlas saturated visible pixels: `0`.
- Body atlas detected red pixels: `0`.
- Body atlas detected green pixels: `0`.
- Body atlas detected blue pixels: `0`.
- Direction pixel differences are nonzero for representative idle/run/aim/fire/death samples.

## Notes

This candidate intentionally removes detached muzzle/shadow fragments from the source. Gameplay should not bind this candidate until user/PM confirms the visual proof has no cut or fragmented frames.
