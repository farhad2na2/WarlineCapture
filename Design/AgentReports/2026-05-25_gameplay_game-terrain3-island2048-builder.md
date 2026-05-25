# Game_Terrain3 Island 2048 Source-Prefab Expansion

Date: 2026-05-25

Task: Expand the small `Game_Terrain3` island into a 2048x2048 island using the same prefab assets and more placements, without scaling the original island up.

Outputs:
- `Assets/Game/Scenes/Generated/Game_Terrain3_Island2048.unity`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048/game_terrain3_island2048_layout.json`

Rules enforced:
- No generated island underlay mesh.
- No substitute terrain prefab set.
- Uses only beach/ground/detail grass prefab assets discovered in `Game_Terrain3`.
- Applies the same material-override pattern seen in `Game_Terrain3`: green grass uses `PolygonBattleRoyale_01_A`, dirt patches use `PolygonBattleRoyale_02_A`, and darker grass/beach areas use `PolygonBattleRoyale_03_A`.
- `SM_Env_Grass_*` prefabs are classified as terrain ground/fill.
- `SM_Generic_Grass_Patch_*` prefabs are classified as decoration/detail grass, not terrain fill; `SM_Generic_Grass_Patch_01` is preferred on green and darker grass areas.
- Ground fill places every valid interior cell with jittered rows; it no longer randomly skips coverage cells.
- Beach placement uses a denser two-band rim to reduce shoreline gaps.
- Detail grass is a separate sparse decoration pass on top of the ground, never the primary floor.
- Prefab Y scale is copied from source instances; X/Z scale is expanded per role so neighboring pieces touch instead of leaving holes.

Counts:
- Source beach prefab instances: 31
- Source ground prefab instances: 46
- Source detail grass prefab instances: 135
- Unique source prefab assets: 8
- Placed prefab instances: 9105
- Green material placements: 4676
- Dirt material placements: 895
- Dark grass material placements: 834
- Beach material placements: 2700
- Ground fill spacing: 18
- Shore ground spacing: 30
- Detail grass spacing: 78
- Ground X/Z scale multiplier: 2.15
- Shore ground X/Z scale multiplier: 1.12
- Detail grass X/Z scale multiplier: 0.95
- Beach X/Z scale multiplier: 2.35

Runtime transfer note: move the source-prefab collection into a prefab catalog, port `EvaluateIsland`, `BoundaryPoint`, and the placement loops to runtime, and instantiate pooled versions of the same source prefab ids.
