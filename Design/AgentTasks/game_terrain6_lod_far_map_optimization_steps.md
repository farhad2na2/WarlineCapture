# Game_Terrain6 LOD and Far-Map Optimization Steps

Date: 2026-05-26

Goal: create `Game_Terrain6` as the next mobile terrain optimization pass after `Game_Terrain5`. `Game_Terrain5` solved draw calls by combining `25,993` renderers into `296` chunk/material renderers, but it kept the full visual triangle count. `Game_Terrain6` should reduce GPU vertex/triangle cost while preserving the same gameplay contract: visual terrain has no gameplay authority; gameplay uses grid, blocker, and heightmap data.

## Source Chain

1. `Game_Terrain4` - readable, unoptimized generated source scene.
2. `Game_Terrain5` - shipping baseline with `256m` combined visual chunks.
3. `Game_Terrain6` - planned LOD/far-map terrain scene built from `Game_Terrain5`.

## Camera Basis

- Normal gameplay camera: height `34`, pitch `40`, FOV `36`.
- Build camera: height `90`, pitch `64`, FOV `52`.
- Wide mobile aspect target: `21:9`.
- Normal camera 21:9 ground footprint: roughly `129 x 78` world units.
- Build camera 21:9 ground footprint: roughly `295 x 155` world units.
- Current `Game_Terrain5` chunk size: `256` world units.

## Optimization Target

- Keep `LOD0` near-camera chunks visually equivalent to `Game_Terrain5`.
- Add `LOD1` simplified chunk meshes for mid distance.
- Add `LOD2` baked far-map visuals for high zoom-out / distant background.
- Reduce active far-view triangle cost before adding any runtime chunk streaming.
- Preserve material count where possible; current terrain uses `3` materials.
- Keep source gameplay data untouched.

## Step Plan

1. Complete - copy `Game_Terrain5` to `Game_Terrain6` as the LOD/far-map working scene.
2. Complete - create `WarlineCaptureGameTerrain6LodFarMapOptimizer` as the repeatable one-go optimizer.
3. Complete - read the `Game_Terrain5` combined chunk meshes and classify each chunk by bounds, material, and role:
   - `GroundSurface`
   - `BeachSurface`
   - `DressingAndRockMass`
4. Complete - split future combined output into two visual families:
   - flat ground/beach surface chunks
   - tall dressing chunks such as mountains, rocks, trees, and bushes
5. Complete - generate `LOD0` from current `Game_Terrain5` chunk meshes without visual change.
6. Complete - generate `LOD1` simplified meshes:
   - target about `45%` of original chunk triangles for ground/beach
   - target about `30%` of original chunk triangles for dressing-heavy chunks
   - target max chunk renderer near `40k` triangles
7. Complete - generate `LOD2` far-map visuals:
   - baked low-detail top-down color texture or vertex-color mesh derived from the current terrain proof
   - very low-poly large chunks or one/few far-map planes
   - target less than `250k` total far-map triangles
8. Complete - add `LODGroup` objects or a lightweight camera-height LOD switch:
   - camera height `0-70`: show `LOD0`
   - camera height `70-130`: show `LOD1`
   - camera height `130+`: show `LOD2`
9. Complete - disable shadows, probes, motion vectors, and visual colliders on all LOD terrain renderers.
10. Complete - generate metrics:
   - renderer count by LOD
   - material slots by LOD
   - total vertices/triangles by LOD
   - max vertices/triangles per renderer
   - generated mesh asset count
11. Complete - render top-down and 21:9 gameplay camera proof captures for `LOD0`, `LOD1`, and `LOD2`.
12. Complete - document visual differences and define acceptable degradation:
   - `LOD0` should match `Game_Terrain5`
   - `LOD1` may lose small rock/bush detail but must preserve terrain silhouette and playable visual readability
   - `LOD2` may be painterly/baked but must preserve island shape, city/camp clearings, beach border, and major mountain masses
13. Complete - run focused validation:
   - no old source prefab hierarchy remains
   - no terrain colliders are present
   - no gameplay grid/blocker/heightmap assets are modified
   - JSON summary validates
   - scene opens in Unity batchmode
14. Complete - if profiling shows far-view memory pressure, consider chunk activation after LOD is proven; do not add runtime streaming before LOD.

## Recommended Implementation Notes

- First implementation should prefer simple, deterministic editor-generated assets over runtime streaming.
- Avoid Entities/Jobs for this static visual terrain until profiling shows CPU-side terrain management is the bottleneck.
- Do not use occlusion culling as the primary solution; this top-down RTS camera usually sees open terrain.
- Mesh simplification should be conservative for the ground/beach outline and aggressive for dense dressing.
- If Unity mesh simplification quality is not good enough, generate `LOD2` as a baked texture/far-map plane first and defer `LOD1` simplification.

## Success Criteria

- `Game_Terrain6` exists and is visually acceptable from normal gameplay height.
- Draw calls remain close to `Game_Terrain5`.
- Far/high-zoom triangle cost is significantly lower than `Game_Terrain5`.
- No gameplay authority moves into visual meshes.
- Terrain generation remains a one-go workflow:
  - generate unoptimized `Game_Terrain4`
  - optimize draw-call baseline `Game_Terrain5`
  - generate LOD/far-map shipping variant `Game_Terrain6`
