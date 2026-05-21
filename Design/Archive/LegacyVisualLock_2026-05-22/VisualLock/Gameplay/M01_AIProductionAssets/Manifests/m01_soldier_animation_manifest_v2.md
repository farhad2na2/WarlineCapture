# M01 Soldier Animation Manifest V2

Status: needs PM/user review
Strategic map status: approved unchanged
Prior animation handoff status: rejected because repeated-pose sequences were visible

## Summary

- Player rifle squad and enemy patrol are separate v2 atlases.
- Each faction has 4 facings: NE, SE, SW, NW.
- Each facing has idle 4, run 8, aim 3, fire 4, damaged 3, death 6 frames.
- Each faction contains 112 transparent 256x256 frame PNGs plus a 4096x1792 atlas.
- Source sheets are AI-generated v2 sprite-strip sheets copied into `Sources/AnimationV2`.

## Runtime Atlases

- Player Rifle Squad: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- Enemy Patrol: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`

## Validation

- Zero adjacent duplicate pairs: 0
- Minimum adjacent RGBA mean delta: 8.599
- Average adjacent RGBA mean delta: 63.651

## Source Sheets

### Player Rifle Squad
- idle: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_idle_source_v2.png`
- run: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_run_source_v2.png`
- aim: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_aim_source_v2.png`
- fire: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_fire_source_v2.png`
- damaged: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_damaged_source_v2.png`
- death: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/player_rifle_squad_death_source_v2.png`

### Enemy Patrol
- idle: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_idle_source_v2.png`
- run: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_run_source_v2.png`
- aim: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_aim_source_v2.png`
- fire: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_fire_source_v2.png`
- damaged: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_damaged_source_v2.png`
- death: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Sources/AnimationV2/enemy_patrol_death_source_v2.png`
