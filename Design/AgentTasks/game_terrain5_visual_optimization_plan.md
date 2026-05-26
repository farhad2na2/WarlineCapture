# Game_Terrain5 Visual Optimization Plan

Date: 2026-05-26

Goal: keep `Game_Terrain4` as the readable, unoptimized generated terrain source and create `Game_Terrain5` as the mobile shipping visual scene. Gameplay must continue to use grid, blocker, and heightmap data; the visual terrain meshes have no gameplay authority.

## Camera Basis

- Normal gameplay camera: height `34`, pitch `40`, FOV `36`.
- Build camera: height `90`, pitch `64`, FOV `52`.
- Wide mobile aspect target: `21:9`.
- Normal camera 21:9 ground footprint is roughly `129 x 78` world units.
- Build camera 21:9 ground footprint is roughly `295 x 155` world units.
- Selected chunk size: `256` world units.

## Completed Steps

1. Complete - keep `Game_Terrain4` as the unoptimized generated source scene.
2. Complete - create `WarlineCaptureGameTerrain5OneGoOptimizer` as the repeatable editor pipeline.
3. Complete - copy `Game_Terrain4` to `Game_Terrain5` before optimization.
4. Complete - temporarily enable source mesh Read/Write during bake for non-readable Synty meshes, then restore importer settings.
5. Complete - combine render meshes by `256m chunk + material`.
6. Complete - delete the source prefab-instance visual hierarchy from `Game_Terrain5`.
7. Complete - strip visual colliders by not carrying source objects into the shipping scene.
8. Complete - disable dynamic shadow casting, receiving, probes, and motion vectors on combined chunk renderers.
9. Complete - write generated chunk meshes to `Assets/Game/GeneratedTerrainOptimized/Game_Terrain5`.
10. Complete - write optimization report and JSON summary.
11. Complete - verify `Game_Terrain5` no longer contains source terrain object names like `GroundFill_`, `BeachCoast_`, `Trees_`, `Rocks_`, `Bushes_`, or `Mountains_`.

## Run Commands

Optimize the current unoptimized terrain:

`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.BuildOptimizedShippingTerrain`

Run the full one-go workflow:

`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize`

## Current Result

- Source renderers: `25,993`
- Optimized renderers: `296`
- Source material slots: `25,993`
- Optimized material slots: `296`
- Combined mesh materials: `3`
- Combined mesh assets: `296`
- Max optimized chunk renderer vertices: `124,379`
- Max optimized chunk renderer triangles: `111,841`
