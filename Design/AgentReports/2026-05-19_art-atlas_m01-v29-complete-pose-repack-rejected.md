# Art/Atlas M01 V29 Complete-Pose Repack Rejected

Date: 2026-05-19
Owner: Art/Atlas
Status: rejected; needs rebuild
Priority: P0

## Summary

User review rejected the latest V29 all-direction soldier proof because it still contains visibly cut or fragmented frames.

This invalidates the current V29 soldier package for Gameplay binding.

## Rejected Proof

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_direction_uniqueness_proof.png`

## Assessment

Needs rebuild.

The current package has unique directions and passes mechanical cell-edge checks, but the visual result still fails the actual art requirement: every cell must contain one complete, readable pose.

## Required Gate Before Next Handoff

Do not present the next package as complete unless the visual proof shows:

- four unique directions;
- all required sequences;
- one full soldier pose per cell;
- no cut torsos, heads, boots, weapons, or separated fragments;
- neutral gray/white body only;
- no baked red/green/blue faction colors;
- `4096x2048` POT atlas, `16 x 7` used grid, `256x256` cells, transparent unused row.

## Routing

Art/Atlas remains owner. Gameplay remains held for soldier binding.
