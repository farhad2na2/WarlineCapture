# PM Message For Gameplay

Date: 2026-05-09
Priority: P0 audit Art/Atlas v2 soldier atlas runtime suitability before acceptance

The user sees the v2 soldiers as more premium, but has not accepted them for runtime use yet.

Audit:

- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`

Check scale consistency, atlas layout, frame rects, pivots/foot anchors, transparent padding, manifest completeness, ECS atlas animator fit, mobile memory/performance risk, and whether the atlases should stay split by faction or be split differently. Do not integrate the sprites yet.

Expected report:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`

Return a recommendation: accept, accept with import notes, needs Art fixes, needs manifest/layout fixes, or blocked. Do not commit or push.
