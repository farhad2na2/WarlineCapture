# PM Art/Atlas M01 V5 Partial Accept And Background Rework

Date: 2026-05-17
Lane: PM
Status: v5 units/animations accepted as current candidates; background/source plate rejected for rework

## Decision

Do not route Gameplay yet.

Art/Atlas delivered useful v5 assets, but the current background/source plate is still not acceptable as the final Gameplay binding source. The v5 comparison shows a large battlefield composition mismatch versus `M01-01_TacticalStart_1920x1080.png`, and Art's own v5 report identifies the same issue: the largest remaining target-match gap is the road/building/corner layout behind the player and enemy regions.

## Accepted As Current Candidates

These are accepted as the current Art candidate set for the next proof pass:

- v5 player readability units
- v5 enemy readability units
- v5 strong separate unit shadows
- v5 marker/readability assets
- full v5 player/enemy/shadow animation atlases

Accepted candidate reports:

- `Design/AgentReports/2026-05-17_art-atlas_m01-v5-readability-shadow-iteration.md`
- `Design/AgentReports/2026-05-17_art-atlas_m01-soldier-animation-v5.md`

Candidate binding paths:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v5.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/enemy_patrol_animation_atlas_v5.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/AnimationV5/unit_shadow_animation_atlas_v5_strong.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v5.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_asset_manifest_v5.json`

## Rejected For Gameplay Binding

Do not bind the current plate:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v3_source_1920x1080.png`

Reason:

- the clean plate/background composition still diverges from the target mockup
- player-region road corner, wall/building, debris, and fire layout do not align closely enough
- enemy-region road/building/cover/wall layout does not align closely enough
- routing this to Gameplay would preserve the same visual mismatch loop

## New Art/Atlas Task

Art/Atlas must deliver:

- `Design/AgentReports/2026-05-17_art-atlas_m01-v6-background-plate-correction.md`

Required output:

- corrected clean no-HUD/no-unit M01-01 tactical background/source plate
- runtime-ready POT candidate if needed
- updated manifest pointer and parse validation
- placement composite using the accepted v5 units/shadows/markers over the corrected plate
- side-by-side comparison against `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- written background match/mismatch assessment for player-region road corner and enemy-region cover/building layout
- Gameplay binding checklist with exact plate path and existing v5 unit/shadow/marker/animation ids

Rules:

- use imagegen for the replacement plate visual
- deterministic tooling only after imagegen source selection for cleanup, resizing, POT packaging, manifests, contact sheets, comparisons, and validation
- no target mockup crops, pasted screenshots, manual clone/paint patches, deterministic placeholder plates, or runtime art made from comparison images
- keep `IsoMapId: iso.ch01.district_edge_01`
- do not modify Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports

## Routing

Art/Atlas remains active for v6 background/source-plate correction.

Gameplay remains held until PM/user accepts the v6 corrected plate. If accepted, Gameplay resumes implementation using the v6 plate plus accepted v5 units/shadows/markers/animations through the existing ECS/runtime presentation path.
