# PM Message For Gameplay

Date: 2026-05-09
Priority: P0 integrate v2 soldier atlas into M01 ECS runtime and capture proof

PM accepts the v2 soldier import-readiness cleanup for runtime integration. This is not final visual approval; final approval requires runtime capture/video.

Use:

- `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`

Integrate:

- use v2 manifest as the ECS soldier animation source,
- keep player/enemy atlases separate,
- use manifest fps, loop flags, facing ids, state ids, anchors, bounds, and frame order,
- no SpriteRenderer/MeshRenderer runtime soldier presentation,
- capture runtime proof at actual M01 camera scale showing selected player rifle squad idle/run and enemy patrol if reachable.

Expected report:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-runtime-integration.md`

Return whether v2 is ready for PM/user runtime review or needs Gameplay/Art fixes. Do not commit or push.
