# M01 Soldier Animation Manifest
Status: needs PM/user review
Strategic map status: approved and unchanged

## Frame Requirements
- `idle`: 4 frames per facing, suggested 6 fps, loop=true
- `run`: 8 frames per facing, suggested 12 fps, loop=true
- `aim`: 3 frames per facing, suggested 10 fps, loop=false
- `fire`: 4 frames per facing, suggested 12 fps, loop=false
- `damaged`: 3 frames per facing, suggested 10 fps, loop=false
- `death`: 6 frames per facing, suggested 8 fps, loop=false

## Atlases
- `player_rifle_squad`: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas.png` (112 frames, 24 sequences)
- `enemy_patrol`: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas.png` (112 frames, 24 sequences)

## Review
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact.png`

Full per-frame paths and atlas rects are in `m01_soldier_animation_manifest.json`.
