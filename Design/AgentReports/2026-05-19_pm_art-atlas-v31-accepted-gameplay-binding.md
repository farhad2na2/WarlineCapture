# PM Review - Art/Atlas V31 Accepted For Gameplay Binding

Date: 2026-05-19
Lane: PM
Task: Review V31 8-direction neutral soldier package
Status: accepted for Gameplay implementation/testing

## Reviewed

Art/Atlas report:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v31-8dir-neutral-soldier-package.md`

Primary assets:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v31.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v31_pot_4096x4096.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_clean_body_atlas_v31_pot_4096x4096.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_faction_mask_atlas_v31_pot_4096x4096.png`

Proof sheets inspected:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_direction_state_proof.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_runtime_scale_proof.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v31_neutral_white_gray_8dir_full_224_frame_proof.png`

## Decision

Accept V31 for Gameplay implementation/testing.

V31 addresses the main V29 rejection by providing all eight directions in one POT package:

- up
- up_right
- right
- down_right
- down
- down_left
- left
- up_left

The proof sheets show a complete 224-frame package with no obvious cut-frame issue from PM visual inspection. Final visual approval still requires runtime proof in the actual M01 scene.

## Guardrails For Gameplay

Gameplay must:

- bind `m01_8dir_soldier_manifest_v31.json`;
- bind the V31 neutral body+shadow atlas for all M01 soldiers;
- use correct V31 direction keys instead of closest-direction fallback;
- keep the V29 overscan background bound;
- preserve actual M01 flow;
- run architecture contract validation;
- produce fresh runtime capture and player/enemy crops.

Gameplay must not:

- use the rejected V29 cardinal soldier package;
- use V30 unless V31 binding is technically blocked and reported;
- tint the whole body atlas;
- bind colored preview images;
- use placeholder enemy sprites or old V28/V5 substitutions;
- route QA before PM/user review of the V31 runtime proof.

## Routing

Gameplay is active.

Expected Gameplay report:

- `Design/AgentReports/2026-05-19_gameplay_m01-v31-8dir-runtime-proof.md`

Art/Atlas waits unless Gameplay proves an exact Art-owned V31 defect. QA remains held.
