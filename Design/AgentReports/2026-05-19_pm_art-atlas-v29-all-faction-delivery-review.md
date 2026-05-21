# PM Review - Art/Atlas V29 All-Faction Delivery

Date: 2026-05-19
Lane: PM
Task: Review Art/Atlas claim that one soldier atlas can serve all M01 factions
Status: accepted for Gameplay implementation/testing

## Reviewed

Art/Atlas delivery:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-pm-delivery-all-factions-background.md`

Primary assets:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29_pot_4096x2048.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29_pot_4096x2048.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_background_manifest_v29.json`

Proof sheets inspected:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_full_112_frame_proof.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_REBUILD_direction_sample_no_cache.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_shared_soldier_mask_white_only_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v29_overscan_no_buildings_aspect_crop_proof.png`

## PM Assessment

The one-atlas approach is acceptable to test because it is not whole-body tinting. The package provides a neutral body/shadow atlas plus a separate white/alpha faction mask atlas. This can support player/enemy/future faction coloring if Gameplay renders the mask as an aligned overlay and applies faction color only to that mask.

This supersedes the earlier baked-red-enemy-only request for the next implementation pass, but it does not grant final visual approval yet.

## Guardrails For Gameplay

Gameplay must:

- draw the neutral body atlas normally;
- draw the faction mask over the same frame rect, pivot, transform, UV scale, and UV offset;
- apply faction color only to the mask overlay;
- prove enemy red and player blue in runtime crops;
- prove the whole body atlas is not material-tinted;
- bind the V29 overscan background and prove no solid fill at the tested aspect;
- preserve ECS/runtime presentation architecture.

Gameplay must not:

- tint the whole soldier body material;
- use placeholder enemy sprites;
- use old V28 or V5 enemy substitutions;
- stretch or solid-fill the tactical background;
- route to QA before runtime proof.

## Routing

Gameplay is active now.

Expected Gameplay report:

- `Design/AgentReports/2026-05-19_gameplay_m01-v29-all-faction-mask-runtime-proof.md`

Art/Atlas waits unless the Gameplay proof exposes an exact Art-owned defect.

QA remains held until Gameplay delivers the runtime proof and PM/user routes QA.
