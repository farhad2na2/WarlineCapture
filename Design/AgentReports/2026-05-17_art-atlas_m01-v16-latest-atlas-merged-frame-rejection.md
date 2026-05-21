# Art/Atlas M01 V16 Latest Atlas Merged-Frame Rejection

Date: 2026-05-17
Owner: Art/Atlas
Status: blocked; latest V16 atlases rejected
Priority: P0

## Summary

User review confirms the latest delivered animation atlases still show merged/two-half-frame cells. V16 remains rejected and must not be routed as an approved Gameplay binding package.

## Rejected Latest Atlases

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v16.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV16/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v16.png`

## Evidence

- Prior blocker: `Design/AgentReports/2026-05-17_art-atlas_m01-v16-animation-atlas-source-contamination-blocker.md`
- Audit sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v5_vs_v16_animation_half-frame_contamination_audit.png`

## Assessment

The latest V16 animation atlases still carry body-frame contamination from the V5 animation source. The shadow bake and per-frame boot anchoring do not solve the merged-frame body issue.

The animation package is blocked until Art/Atlas produces or receives clean animation body frames, then rebakes shadows into those clean cells.

## Binding Guidance

- Do not bind V16 animation atlases.
- Do not route Gameplay for final runtime proof using V16.
- Treat V16 static facings as separately reviewable only; the full soldier animation package is not approved.

## Required Next Step

Regenerate clean soldier animation body frames with imagegen, verify every atlas cell contains only one complete intended pose, and then rebake M01-matched shadows into those clean animation atlases.
