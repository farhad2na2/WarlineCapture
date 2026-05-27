# Game_Terrain7 Non-LOD Mobile Optimizer

Date: 2026-05-26

Purpose: `Game_Terrain7` is the final no-LOD mobile terrain scene. It is copied from the optimized `Game_Terrain5` chunk bake, then certified to contain no live LOD behavior.

Implementation:
- Copy `Game_Terrain5` to `Game_Terrain7`.
- Rename the optimized chunk root to `GameTerrain7VisualTerrain_LOD0Only_256mChunks`.
- Reuse the accepted `Game_Terrain5` chunk mesh assets instead of duplicating another terrain mesh set.
- Remove `WarlineCaptureTerrainLodHeightSwitch`, LOD1, LOD2, colliders, debug/proof objects, and old wrapper hierarchy.
- Disable shadows, receive shadows, probes, motion vectors, and dynamic occlusion on all terrain renderers.
- Keep gameplay authority in grid, blocker, and heightmap data.

Results:
- Source LOD0 renderers/material slots/triangles: `378` / `378` / `32210455`
- Terrain7 renderers/material slots/triangles: `378` / `378` / `32210455`
- Terrain7 unique materials: `4`
- Terrain7 referenced mesh assets: `378`
- Terrain7 non-readable mesh assets: `0`
- Terrain7 readable mesh assets retained: `378`
- Terrain7 referenced mesh asset disk bytes: `8441061021`
- Removed LOD switchers: `0`
- Removed colliders: `0`
- Removed nonessential objects: `1`

Profiling note:
- This batchmode pass records static renderer, material, mesh, triangle, and disk-size metrics. Real GPU frame time, CPU render-thread time, and mobile memory pressure still need Unity Profiler/Frame Debugger validation on device or target editor quality settings.
- Generated `.asset` mesh readability is retained in this pass because Terrain7 reuses the accepted Game_Terrain5 chunk mesh assets. If runtime memory later requires non-readable meshes, test that change separately on device before accepting it.
- Runtime chunk activation is deferred until profiling proves renderer/culling cost is still a bottleneck. If added, it must disable whole same-mesh chunks only; it must not swap to different geometry.

Proof captures:
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain7_NonLodMobile/game_terrain7_source_topdown_proof.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain7_NonLodMobile/game_terrain7_source_playable_angle_proof.png`

Capture note:
- Terrain7 keeps the exact optimized Game_Terrain5 chunk mesh assets. The capture paths are optional visual references only; final visual QA should be run interactively or on device.

Run command:
- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain7NonLodMobileOptimizer.BuildNonLodMobileTerrain`
