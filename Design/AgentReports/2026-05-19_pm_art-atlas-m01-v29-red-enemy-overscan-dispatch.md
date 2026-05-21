# PM Dispatch - M01 V29 Art Assets

Date: 2026-05-19
Lane: PM
Task: Route Gameplay V29 blocker to Art/Atlas
Status: routed to Art/Atlas

## Reviewed Handoff

Gameplay report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-final-recapture-proof.md`

## Decision

Gameplay is accepted as blocked on real Art-owned assets.

This is not a Unity blocker. Gameplay used the documented licensing workaround and produced a fresh runtime proof. This is not a Gameplay flow blocker. The report proves splash, main menu, quick custom, match launch, ECS soldier rendering, V28 atlas binding, and architecture tests.

## Art-Owned Blockers

Art/Atlas must provide:

- a V28-compatible red enemy patrol atlas matching the V28 player atlas in scale, projection, stance/facing family, pivot, baked shadow direction, frame layout, and silhouette readability;
- an oversized M01 tactical background plate with enough real painted bleed/overscan for 16:9, 20:9, and 21:9 runtime framing.

## Required Art Task File

Art/Atlas has been activated in:

- `Design/AgentTasks/art-atlas_current.md`

Direct PM message:

- `Design/AgentTasks/art-atlas_pm_message.md`

Expected Art report:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v29-red-enemy-overscan-assets.md`

## Required Output Structure

- Enemy atlas package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/EnemyPatrol/`
- Enemy source/proof package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/DirectionLockedV29/EnemyPatrol/`
- Background package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/`
- Background source/proof package: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV29/`
- Contact sheets/proof: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/`
- Soldier manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json`
- Background manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_background_manifest_v29.json`

## Cross-Lane Impacts

Gameplay is held until Art delivers the V29 package. QA remains held until Gameplay binds and recaptures. UI/HUD target-lock remains a separate UI lane and is not part of this Art task.

## Next Recommended Task

Art/Atlas creates the V29 red enemy atlas and oversized tactical background package, then reports exact paths and implementation notes for Gameplay binding.
