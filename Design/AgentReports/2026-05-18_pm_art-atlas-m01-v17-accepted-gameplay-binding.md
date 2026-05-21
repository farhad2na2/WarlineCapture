# PM Art/Atlas M01 V17 Accepted For Gameplay Binding

Date: 2026-05-18
Lane: PM
Status: Art v17 accepted for binding; Gameplay dispatched

## Decision

Accept the M01 Art/Atlas v17 clean animation baked-shadow package for Gameplay binding.

This is not final runtime visual approval. It means the Art-owned shadow direction and merged/two-half-frame animation-cell blocker is sufficiently resolved for Gameplay to bind the package and produce a fresh runtime proof.

## Accepted Art Input

Accepted report:

- `Design/AgentReports/2026-05-18_art-atlas_m01-v17-clean-animation-baked-shadow-handoff.md`

Prior rejected packages that must not be bound:

- `Design/AgentReports/2026-05-17_art-atlas_m01-v16-animation-atlas-source-contamination-blocker.md`
- `Design/AgentReports/2026-05-17_art-atlas_m01-v16-latest-atlas-merged-frame-rejection.md`

Accepted binding paths:

- player baked animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v17.png`
- enemy baked animation atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v17.png`
- player clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_clean_body_atlas_v17.png`
- enemy clean body atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_clean_body_atlas_v17.png`
- manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v17.json`
- v6 plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- v5 markers/readability family: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`

## Review Notes

PM checked:

- v17 manifest parses
- player/enemy baked animation atlases are `4096x1792`
- contact and audit sheets exist with expected dimensions
- v17 report validates `112` clean player cells and `112` clean enemy cells
- one-full-pose validation failures are `0`
- shadows are baked into the v17 atlases, so Gameplay should not bind a separate soldier shadow atlas

Known caveat:

- V17 resamples clean source poses because the V5 source sheets did not contain enough distinct clean full-pose components for every row. Gameplay proof must therefore include animation-frame diagnostics and a written note on any cadence/stutter risk.

## Gameplay Dispatch

Gameplay now owns:

- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

Expected Gameplay report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v17-clean-animation-baked-shadow-runtime-proof.md`

Gameplay must:

- bind v17 baked body+shadow animation atlases through the existing ECS/runtime presentation path
- remove/avoid separate TargetMatchV5 soldier shadow atlas binding
- preserve loading/main menu/custom game/match flow
- preserve `Design/Architecture/gameplay_solid_ecs_contract.md`
- regenerate runtime capture and side-by-side comparison against `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- prove v17 atlas source, ECS soldier render path, animation frame advancement, and normal app flow
- report exact remaining visual mismatches instead of routing QA prematurely

## Routing

Art/Atlas is held unless Gameplay proof exposes a new exact Art-owned blocker.

UI continues separate main menu, match HUD, and result screen work.

QA/HCI remains held until Gameplay runtime proof exists and PM/user reviews it.
