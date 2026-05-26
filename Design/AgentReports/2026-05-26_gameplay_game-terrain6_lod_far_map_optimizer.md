# Game_Terrain6 LOD and Far-Map Optimizer

Date: 2026-05-26

Purpose: `Game_Terrain6` is the next mobile visual-terrain pass after `Game_Terrain5`. `Game_Terrain5` reduced draw calls by combining renderers; `Game_Terrain6` keeps that near-camera quality and adds lower-triangle mid/far terrain layers.

Implementation:
- Copy `Game_Terrain5` to `Game_Terrain6`.
- Keep the existing combined chunks as `LOD0_Near_GameTerrain5Chunks`.
- Generate `LOD1_Mid_SimplifiedChunks` from compact triangle-reduced meshes.
- Generate `LOD2_FarMap_SimplifiedChunks` as a baked top-down far-map plane.
- Add `WarlineCaptureTerrainLodHeightSwitch` using camera height thresholds `70` and `130`.
- Disable shadows, probes, motion vectors, dynamic occlusion, and colliders.

Results:
- Source chunks: `296`
- Role counts: ground `192`, beach `49`, dressing `55`
- LOD0 renderers/material slots/triangles: `296` / `296` / `11220414`
- LOD1 renderers/material slots/triangles: `296` / `296` / `4431028`
- LOD2 renderers/material slots/triangles: `1` / `1` / `2`
- LOD2 budget: `< 250000` triangles.
- Removed visual colliders: `0`

Proof captures:
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod0_topdown_1024.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod0_gameplay_1600x900.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod1_topdown_1024.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod1_gameplay_1600x900.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod2_topdown_1024.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap/game_terrain6_lod2_gameplay_1600x900.png`

Run command:
- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain6LodFarMapOptimizer.BuildLodFarMapTerrain`
