# Game_Terrain5 One-Go Visual Optimizer

Date: 2026-05-26

Purpose: `Game_Terrain5` is the shipping visual-terrain scene. It is copied from `Game_Terrain4`, then optimized as decorative render-only terrain. Gameplay remains driven by grid, blocker, and heightmap data, not these meshes.

Chunk-size decision:
- Runtime config normal camera: height `34`, pitch `40`, FOV `36`.
- At 21:9, that sees roughly `129 x 78` world units.
- Build-mode camera: height `90`, pitch `64`, FOV `52`, about `295 x 155` world units at 21:9.
- Chosen chunk size: `256` world units. This avoids 128m over-fragmentation while still letting Unity cull large off-camera parts of the decorative island.

Optimization rules:
- Combine visual meshes by `256m chunk + material`.
- Delete the source prefab-instance visual hierarchy from `Game_Terrain5` after bake.
- Strip visual terrain colliders by not carrying source objects forward.
- Disable dynamic shadow casting, shadow receiving, reflection probes, light probes, and motion vectors on combined chunk renderers.
- Store generated chunk meshes in `Assets/Game/GeneratedTerrainOptimized/Game_Terrain5`.

Results:
- Source renderers: `25993`
- Source material slots: `25993`
- Source unique meshes: `27`
- Optimized renderers: `296`
- Optimized material slots: `296`
- Combined chunk/material buckets: `296`
- Optimized mesh vertices: `11454789`
- Optimized mesh triangles: `11220414`
- Max vertices in one optimized chunk renderer: `124379`
- Max triangles in one optimized chunk renderer: `111841`

Run commands:
- Optimize existing `Game_Terrain4`: `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.BuildOptimizedShippingTerrain`
- Full one-go workflow: `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize`
