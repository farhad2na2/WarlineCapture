# Art/Atlas M01 V29 Full 112-Frame Rebuild Review Candidate

Date: 2026-05-19
Owner: Art/Atlas
Status: review candidate; not final until user/PM visual approval
Priority: P0

## Summary

Art/Atlas continued after user rejected the prior cut-frame candidate.

This pass rebuilds the V29 shared soldier POT atlas and removes the problematic generated fire-fragment rows. Fire frames are mapped to a complete aim/firing stance so every used frame has one complete soldier pose.

## Runtime Files

- Neutral body+shadow POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- Neutral clean body POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_clean_body_atlas_v29_pot_4096x2048.png`
- White/alpha faction mask POT atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`

## Proof

- Full 112-frame proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_full_112_frame_proof.png`
- Direction sample proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_direction_sample_no_cache.png`

## Validation

- Used cell failures: `[]`
- Cell edge failures: `[]`
- POT pad row nonzero alpha pixels: `0`
- Body atlas saturated visible pixels: `0`
- Body atlas detected red/green/blue pixels: `0/0/0`
- Direction pair pixel diffs are nonzero for representative idle/run/aim/fire/death samples.

## Status

Review candidate only. Do not bind in Gameplay until user/PM approval.
