# Game_Terrain3 Island 2048 Source-Prefab Expansion

Date: 2026-05-25

Task: Expand the small `Game_Terrain3` island into a 2048x2048 island using the same prefab assets and more placements, without scaling the original island up.

Step 2 update: the island foundation now targets a compact green/dirt playable interior. The 2024 gameplay map footprint is `2023x2023` world units, and the builder fills green/dirt terrain across `2520x2480` before placing a slightly overlapping beach ring outside the playable area.

Outputs:
- `Assets/Game/Scenes/Game_Terrain4.unity` under root GameObject `Island`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048/game_terrain3_island2048_layout.json`
- Removed standalone generated-scene target: `Assets/Game/Scenes/Generated/Game_Terrain3_Island2048.unity`

Rules enforced:
- No generated island underlay mesh.
- No substitute terrain prefab set.
- Uses only beach/ground/detail grass prefab assets discovered in `Game_Terrain3`.
- Applies the same material-override pattern seen in `Game_Terrain3`: green grass uses `PolygonBattleRoyale_01_A`, dirt patches use `PolygonBattleRoyale_02_A`, and darker grass/beach areas use `PolygonBattleRoyale_03_A`.
- `SM_Env_Grass_*` prefabs are classified as terrain ground/fill.
- `SM_Generic_Grass_Patch_*` prefabs are classified as decoration/detail grass, not terrain fill; `SM_Generic_Grass_Patch_01` is preferred on green and darker grass areas.
- Ground fill places every valid interior cell with jittered rows; it no longer randomly skips coverage cells.
- Beach placement uses a denser two-band rim to reduce shoreline gaps.
- Green/dirt terrain is intentionally larger than the 2024 gameplay map target; beach/coast content is pushed to the outer island border.
- Detail grass is a separate sparse decoration pass on top of the ground, never the primary floor.
- Prefab Y scale is copied from source instances; X/Z scale is expanded per role so neighboring pieces touch instead of leaving holes.

Counts:
- Source beach prefab instances: 31
- Source ground prefab instances: 46
- Source detail grass prefab instances: 135
- Unique source prefab assets: 8
- Placed prefab instances: 22490
- Green material placements: 8024
- Dirt material placements: 6659
- Dark grass material placements: 3286
- Beach material placements: 4521
- Ground fill spacing: 18
- Gameplay map target extent: 2023
- Green playable half extent X/Z: 1260 / 1240
- Island radius X/Z: 1320 / 1300
- Shore ground spacing: 30
- Detail grass spacing: 78
- Ground X/Z scale multiplier: 2.15
- Shore ground X/Z scale multiplier: 1.45
- Detail grass X/Z scale multiplier: 0.95
- Beach X/Z scale multiplier: 2.5

Runtime transfer note: move the source-prefab collection into a prefab catalog, port `EvaluateIsland`, `BoundaryPoint`, and the placement loops to runtime, and instantiate pooled versions of the same source prefab ids.
