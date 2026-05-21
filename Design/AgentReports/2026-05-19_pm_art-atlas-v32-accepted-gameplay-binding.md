# PM Review - Art/Atlas V32 Accepted For Gameplay Binding

Date: 2026-05-19
Lane: PM
Status: accepted for Gameplay implementation proof; not final runtime visual approval
Priority: P0

## Reviewed Art Delivery

- `Design/AgentReports/2026-05-19_art-atlas_m01-v32-corrected-direction-read-assets.md`

## Decision

Art/Atlas V32 is accepted for the next Gameplay binding pass.

This is not final M01 visual approval. It only means the V32 package is complete enough for Gameplay to bind it in the actual Game scene and produce fresh proof.

## Why V32 Is Accepted For Binding

The V32 handoff addresses the V31 rejection criteria:

- it delivers a new V32 package under `DirectionLockedV32`, without overwriting V31;
- it uses a new imagegen-derived source and states pose pixels changed from V31;
- it includes all eight directions with 28 frames per direction;
- it provides a corrected `up_right` runtime-orientation proof for the bottom/player squad;
- it provides a matching `down_left` opposite direction for enemy-facing use;
- it keeps the atlas neutral body+shadow, with no baked faction color variants;
- it includes a manifest and POT atlas paths for implementation.

## Gameplay Assignment

Gameplay owns the next action.

Expected Gameplay report:

- `Design/AgentReports/2026-05-19_gameplay_m01-v32-direction-runtime-proof.md`

Gameplay must bind:

- manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v32.json`
- body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v32_pot_4096x4096.png`

Gameplay must keep:

- V29 overscan M01 background unless PM/user routes a newer background;
- player/bottom target-facing key as `up_right`;
- enemy/opposite key as `down_left`;
- normal Game scene flow intact;
- ECS animation path intact;
- architecture contract rules intact.

Gameplay must not use:

- V31/V30/V29/V28/V5 soldier atlas substitutions;
- label-only direction remaps;
- closest-direction fallback as a visual fix;
- whole-body tint on the neutral atlas;
- placeholder sprites;
- flattened mockup overlays.

## Required Runtime Proof

The Gameplay report must include:

- fresh 16:9 runtime capture from the real Game scene;
- player crop and enemy crop;
- direction audit proving V32 `up_right` and V32 `down_left` keys are active in runtime;
- visual comparison note against the M01 target for soldier positions, angles, selected states, zoom level, and background;
- body material proof showing no whole-body tint;
- ECS animation proof for the soldiers;
- V29 overscan background proof with no solid-fill or old-background fallback;
- normal flow proof that loading/menu/custom game routing was not replaced;
- architecture test result or exact blocker.

## QA Routing

QA/HCI remains blocked.

QA starts only after PM/user accepts Gameplay's V32 runtime proof as visually close enough to audit.
