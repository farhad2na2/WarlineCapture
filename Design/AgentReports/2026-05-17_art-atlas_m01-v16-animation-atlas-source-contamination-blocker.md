# Art/Atlas M01 V16 Animation Atlas Source Contamination Blocker

Date: 2026-05-17
Owner: Art/Atlas
Status: blocked; V16 not approved for Gameplay binding
Priority: P0

## Summary

User review identified merged/half-frame contamination in the soldier animation atlases. Audit confirms the artifact is already present in the accepted V5 source animation atlas before V16 shadow baking.

V16 baked shadows onto every animation cell, but it inherited body-frame contamination from V5. The V16 package must not be treated as final or approved.

## Evidence

- V5 source player atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v5.png`
- V5 source enemy atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/enemy_patrol_animation_atlas_v5.png`
- V16 player baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v16.png`
- V16 enemy baked atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v16.png`
- Audit sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v5_vs_v16_animation_half-frame_contamination_audit.png`

## Assessment

The issue is not only shadow placement. Several animation cells contain partial adjacent bodies or parts of a different pose. Since this contamination exists in the V5 source atlas, later baked-shadow packages V7 through V16 cannot produce a clean final animation atlas by shadow processing alone.

## Blocker

Art/Atlas needs a clean soldier animation body source before final baked-shadow atlases can be approved. Options:

- regenerate clean animation body frames with imagegen for the affected sequences, then reapply baked shadows, or
- receive a clean accepted body animation atlas from PM/user, then re-run the baked-shadow pass.

## Current Binding Guidance

- Do not bind V16 animation atlases for final approval.
- Do not route Gameplay to final runtime proof with V16.
- Static facing frames may still be reviewed separately, but the full animation package is blocked.
