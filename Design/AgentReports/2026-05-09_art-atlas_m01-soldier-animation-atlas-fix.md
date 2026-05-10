# Art/Atlas M01 Soldier Animation Atlas Fix

## Lane

Art/Atlas

## Task

Fix the rejected M01 soldier sprite outputs by adding true multi-frame animation sequences for player rifle squad and enemy patrol, while leaving the approved strategic map unchanged.

## Handoff assessment

- `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-approved-soldier-animation-rejected.md`: accepted and fixed for soldier animation coverage.
- Strategic map status: accepted by PM/user and unchanged in this pass.
- Existing one-frame soldier state poses are retained as source/static reference files, but the deliverable now includes separate multi-frame animation sequences and animation atlases.

## Files changed

- Added runtime soldier animation frames and atlases under `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/` and `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/`.
- Added review mirror soldier animation frames, atlases, and contact sheet under `Design/VisualLock/Gameplay/M01_AIProductionAssets/`.
- Added soldier animation manifests under both runtime and review manifest folders.
- Updated `m01_ai_production_asset_manifest.json` and `.md` with soldier animation fix references.
- Completion report: `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix.md`.

## Contracts touched

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-approved-soldier-animation-rejected.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`

## User-visible behavior

No runtime behavior changed. This handoff supplies the missing animated soldier atlas assets for downstream Gameplay import after PM/user approval.

## Frame-count summary

- `idle`: 4 frames per facing, suggested 6 fps, loop=true
- `run`: 8 frames per facing, suggested 12 fps, loop=true
- `aim`: 3 frames per facing, suggested 10 fps, loop=false
- `fire`: 4 frames per facing, suggested 12 fps, loop=false
- `damaged`: 3 frames per facing, suggested 10 fps, loop=false
- `death`: 6 frames per facing, suggested 8 fps, loop=false

## Faction summary

- `player_rifle_squad`: 112 frames total, 24 animation sequences, atlas `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas.png`
- `enemy_patrol`: 112 frames total, 24 animation sequences, atlas `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas.png`

## Manifest paths

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json`

## Runtime soldier paths changed

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_00.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_01.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_02.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_03.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_04.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_05.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_06.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_07.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.md`

## Review mirror paths changed

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/aim/player_rifle_squad_ne_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/damaged/player_rifle_squad_ne_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/death/player_rifle_squad_ne_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/fire/player_rifle_squad_ne_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/idle/player_rifle_squad_ne_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/ne/run/player_rifle_squad_ne_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/aim/player_rifle_squad_nw_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/damaged/player_rifle_squad_nw_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/death/player_rifle_squad_nw_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/fire/player_rifle_squad_nw_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/idle/player_rifle_squad_nw_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/nw/run/player_rifle_squad_nw_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/aim/player_rifle_squad_se_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/damaged/player_rifle_squad_se_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/death/player_rifle_squad_se_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/fire/player_rifle_squad_se_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/idle/player_rifle_squad_se_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/se/run/player_rifle_squad_se_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/aim/player_rifle_squad_sw_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/damaged/player_rifle_squad_sw_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/death/player_rifle_squad_sw_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/fire/player_rifle_squad_sw_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/idle/player_rifle_squad_sw_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/Animations/sw/run/player_rifle_squad_sw_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/aim/enemy_patrol_ne_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/damaged/enemy_patrol_ne_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/death/enemy_patrol_ne_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/fire/enemy_patrol_ne_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/idle/enemy_patrol_ne_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/ne/run/enemy_patrol_ne_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/aim/enemy_patrol_nw_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/damaged/enemy_patrol_nw_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/death/enemy_patrol_nw_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/fire/enemy_patrol_nw_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/idle/enemy_patrol_nw_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/nw/run/enemy_patrol_nw_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/aim/enemy_patrol_se_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/damaged/enemy_patrol_se_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/death/enemy_patrol_se_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/fire/enemy_patrol_se_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/idle/enemy_patrol_se_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/se/run/enemy_patrol_se_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/aim/enemy_patrol_sw_aim_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/damaged/enemy_patrol_sw_damaged_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/death/enemy_patrol_sw_death_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/fire/enemy_patrol_sw_fire_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/idle/enemy_patrol_sw_idle_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_00.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_01.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_02.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_03.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_04.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_05.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_06.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/Animations/sw/run/enemy_patrol_sw_run_07.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/enemy_patrol_animation_atlas.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact.png`

## Generation/source notes

- Soldier animation frames are AI-assisted: existing AI-generated player/enemy source poses were locally expanded into per-state frame sequences with controlled pose timing, recoil, impact, run-cycle, idle-breathing, aim-settle, and death-settle variations.
- Player and enemy factions remain separate in both frame folders and atlas sheets.
- Frame cells remain 256x256 transparent RGBA PNGs.
- Runtime animation atlases are 4096x1792 PNGs with 16 columns and 7 rows, storing 112 frames per faction.
- Full frame order, frame count, facing id, state id, suggested fps, loop flag, runtime/review frame paths, and atlas rect metadata are in `m01_soldier_animation_manifest.json`.
- Strategic map is approved and was not changed in this soldier-animation pass.

## Validation run

- Read `Design/AgentTasks/art-atlas_current.md` and `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read and accepted `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-approved-soldier-animation-rejected.md`.
- Generated player rifle squad and enemy patrol animation frame sequences for every required state/facing pair.
- Wrote `m01_soldier_animation_manifest.json` and `.md` in runtime and review manifest folders.
- Updated the main M01 AI production manifest with soldier-animation references.
- Opened and visually inspected `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact.png`.
- Ran `identify` on both animation atlases and the soldier animation contact sheet.
- Checked soldier animation manifest paths: 453 paths checked, 0 missing.
- Verified player rifle squad frame counts: idle 4, run 8, aim 3, fire 4, damaged 3, death 6 for each NE/SE/SW/NW facing.
- Verified enemy patrol frame counts: idle 4, run 8, aim 3, fire 4, damaged 3, death 6 for each NE/SE/SW/NW facing.
- Did not run Unity import or Gameplay wiring; approval and consumption are downstream.

## Validation result

Ready for PM/user review.

The soldier animation blocker is addressed with separate player/enemy multi-frame animation atlases and per-frame manifests. The strategic map remains approved and unchanged.

## User Review Steps

1. Open `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact.png` and verify every player/enemy facing has multi-frame idle, run, aim, fire, damaged, and death sequences.
2. Open `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest.json` to verify per-frame paths, atlas rects, suggested fps, and loop flags.
3. If acceptable, answer `approve M01 soldier animation atlas fix`; otherwise answer `reject M01 soldier animation atlas fix with notes`.

## Known gaps

- Assets are not yet PM/user approved.
- Unity import settings and Gameplay wiring were not performed in this Art/Atlas pass.
- The animated frames are AI-assisted expansions of the approved AI-generated source poses; PM/user should review motion quality before Gameplay consumes them.

## Cross-lane impacts

- Gameplay can consume the soldier animation manifest and atlases only after PM/user approval.
- QA/HCI should reject static one-frame soldier state usage once this fix is approved.
- Strategic map remains approved and available for downstream use.

## Next recommended task

PM/user should approve or reject the M01 soldier animation atlas fix with notes.
