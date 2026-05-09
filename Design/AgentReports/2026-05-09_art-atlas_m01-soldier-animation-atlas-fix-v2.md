# Art/Atlas M01 Soldier Animation Atlas Fix V2

## Lane

Art/Atlas

## Task

Replace the rejected soldier animation handoff with v2 player rifle squad and enemy patrol animation atlases that show real frame-by-frame pose progression. Leave the approved strategic map unchanged.

## Handoff assessment

- `Design/AgentReports/2026-05-09_pm_art-atlas-animation-not-approved.md`: accepted.
- Prior Art/Atlas handoff `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix.md`: rejected because user/PM saw repeated or near-identical poses, especially in run sequences.
- Strategic map status: approved by PM/user and unchanged in this pass.
- Gameplay remains blocked on soldier animation integration until PM/user approves this v2 package.

## Files changed

- Added AI-generated v2 source sheets under `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/`.
- Added review source mirrors under `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/AnimationV2/`.
- Added transparent v2 runtime frames under `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/AnimationsV2/` and `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/AnimationsV2/`.
- Added review frame mirrors under `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/AnimationsV2/` and `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/AnimationsV2/`.
- Added v2 runtime atlases:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`
- Added v2 manifests:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.md`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v2.json`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v2.md`
- Added review contact sheets:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_sources_v2_contact.png`
- Updated `m01_ai_production_asset_manifest.json` and `.md` with v2 soldier animation pointers.

## Contracts touched

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-animation-not-approved.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`

## User-visible behavior

No runtime behavior changed. This handoff supplies review-ready and runtime-ready v2 soldier animation assets for downstream Gameplay import after PM/user approval.

## Frame-count summary

- `idle`: 4 frames per facing, suggested 6 fps, loop=true
- `run`: 8 frames per facing, suggested 12 fps, loop=true
- `aim`: 3 frames per facing, suggested 10 fps, loop=false
- `fire`: 4 frames per facing, suggested 12 fps, loop=false
- `damaged`: 3 frames per facing, suggested 10 fps, loop=false
- `death`: 6 frames per facing, suggested 8 fps, loop=false

## Faction summary

- `player_rifle_squad`: 112 frames total, 24 animation sequences, separate 4096x1792 v2 atlas.
- `enemy_patrol`: 112 frames total, 24 animation sequences, separate 4096x1792 v2 atlas.
- Facings: NE, SE, SW, NW.
- Player and enemy remain separate sheets/manifests.

## Validation run

- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest relevant PM handoff in `Design/AgentReports/2026-05-09_pm_art-atlas-animation-not-approved.md`.
- Generated v2 source sheets with the built-in image generation path and copied them into the workspace under `Sources/AnimationV2`.
- Cropped v2 sources into transparent 256x256 runtime frames.
- Created v2 atlases and manifests.
- Visual reviewed `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`.
- Validated all manifest-referenced runtime/review frame paths exist.
- Validated frame counts: 112 per faction, 24 sequences per faction.
- Validated atlas sizes: 4096x1792 for player and enemy.
- Validated sampled transparent frames have alpha-transparent corners.
- Adjacent-frame image-delta check: zero duplicate adjacent pairs; average RGBA mean delta 63.651.

## Validation result

Ready for PM/user art approval. The v2 run, aim, fire, damaged, and death strips now show visible adjacent pose changes in the review contact sheet. Idle remains intentionally subtle but still has non-identical adjacent frames.

## Known gaps

- This is not imported into gameplay runtime yet.
- PM/user approval is still required before Gameplay should integrate the v2 soldier atlases.
- Source sheets are AI-generated and then grid-cropped/keyed locally; any desired pose-polish notes should be routed back to Art/Atlas before runtime integration.

## Next recommended task

PM/user should review `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`. If accepted, route Gameplay to integrate `m01_soldier_animation_manifest_v2.json` and ignore the rejected v1 manifest for runtime soldier animation.
