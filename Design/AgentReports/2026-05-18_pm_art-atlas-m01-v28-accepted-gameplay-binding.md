# PM Review - M01 V28 Art Accepted For Gameplay Binding

Date: 2026-05-18
Owner: PM
Status: Art V28 accepted as runtime implementation candidate; Gameplay dispatched
Priority: P0

## Reviewed

Art/Atlas report:

- `Design/AgentReports/2026-05-18_art-atlas_m01-v28-target-scale-hard-shadow-full-baked-atlases.md`

Key proof files:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v28.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v28.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_v28_target_scale_hard_shadow_contact.png`
- `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_1920x1080.png`
- `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01_TargetMatchV28TargetScaleHardShadowIdlePlacement_TargetAlignedCropReview.png`

## Decision

V28 is accepted as the current Gameplay implementation candidate.

This is not final visual approval. The next gate is runtime proof from the actual Unity Game scene after V28 is bound through the existing ECS/runtime presentation path.

## Accepted From Art

- Full player and enemy body+shadow animation atlases exist.
- Player and enemy idle/facing atlases exist.
- Manifest and binding checklist are present.
- Contact sheets and placement proofs were generated.
- Art states the screen-space direction read is:
  - player: `player_bottom_faces_up_screen`
  - enemy: `enemy_top_faces_down_screen`
- Art states no target mockup crops were pasted into the delivered art.

## Remaining Runtime Gate

Gameplay must prove whether V28 actually matches the target inside Unity runtime:

- target zoom level
- camera angle/framing
- player soldier positions and formation spacing
- enemy soldier positions and formation spacing
- soldier facing/angles
- selected/no-selected state
- HUD panels and command state
- minimap/squad cards/threat log
- approved background source
- ECS render path and animation

## Gameplay Dispatch

Gameplay now owns:

- bind V28 body+shadow atlases through the existing ECS/runtime presentation path
- preserve loading screen, main menu, custom game mode, and normal mission launch flow
- preserve `Design/Architecture/gameplay_solid_ecs_contract.md`
- produce `Design/AgentReports/2026-05-18_gameplay_m01-v28-runtime-target-match-proof.md`

Gameplay must not use placeholders, pasted target pixels, flattened mockup screenshots, or transform-only hacks to fake a target match.

## Current Routing

Current owner:
Gameplay

Expected report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v28-runtime-target-match-proof.md`

Held:

- Art/Atlas waits unless Gameplay produces an exact Art-owned blocker.
- UI/HCI and QA remain held for this M01 slice until runtime proof is accepted.
