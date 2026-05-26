# Game_Terrain7 Non-LOD Mobile Optimization Steps

Date: 2026-05-26

Goal: create the final no-LOD gameplay-ready terrain scene from the optimized `Game_Terrain5` chunk bake. The `Game_Terrain6` LOD experiment proved that live terrain swaps are too noticeable because LOD1/LOD2 do not preserve the exact same terrain shape. `Game_Terrain7` should keep the optimized `Game_Terrain5` visual result unchanged and certify that no rejected LOD gameplay path remains.

## Decision

- Do not use live terrain LOD swapping during gameplay.
- Keep the `Game_Terrain5` combined chunk terrain as the visual baseline.
- Keep the baked far-map concept only for non-gameplay views such as loading, map overview, or minimap-style screens if needed later.
- Do not move gameplay authority into visual meshes; gameplay continues to use grid, blocker, and heightmap data.

## Optimization Targets

- Preserve the exact visible terrain shape during gameplay.
- Avoid terrain popping, shape changes, or material changes while the player controls units.
- Reduce memory, import size, overdraw, shader cost, and unnecessary renderer cost.
- Keep the one-go workflow repeatable from generated terrain source to optimized terrain scene.

## Step Plan

1. Complete - create `Game_Terrain7` by copying the optimized `Game_Terrain5` scene and keeping only the optimized chunk terrain active for gameplay.
2. Complete - verify `LOD1_Mid_SimplifiedChunks` and `LOD2_FarMap_SimplifiedChunks` are absent in `Game_Terrain7` so they cannot accidentally swap during gameplay.
3. Complete - create `WarlineCaptureGameTerrain7NonLodMobileOptimizer` as the repeatable one-go optimizer.
4. Complete - preserve the existing `256m` chunk/material structure from `Game_Terrain5`; do not merge the whole island into one mesh because chunk culling is still valuable.
5. Complete - audit and remove nonessential overlay renderers from the gameplay terrain scene:
   - debug mask planes
   - reference image planes
   - temporary capture cameras
   - unused proof objects
   - oversized decorative water planes if they do not contribute to the gameplay camera view
6. Complete - replace or resize the decorative water plane if needed:
   - keep it only where visible from gameplay camera
   - disable shadows/probes/motion vectors
   - use the cheapest acceptable material
7. Complete - apply safe mesh asset decisions to terrain meshes:
   - reuse the accepted `Game_Terrain5` chunk mesh assets instead of duplicating another 1GB+ mesh set
   - keep generated mesh readability unchanged in this pass because visual proof and editor validation must remain reliable
   - defer non-readable mesh conversion until a separate on-device memory test proves it is required
   - ensure index format and vertex channels remain unchanged unless a separate material/visual test proves stripping is safe
8. Complete - split terrain material usage into a small fixed set and verify no accidental material instances were generated.
9. Complete - disable renderer costs on every gameplay terrain renderer:
   - shadows off
   - receive shadows off
   - light probes off
   - reflection probes off
   - motion vectors off
   - dynamic occlusion off unless profiling proves it helps
10. Complete - verify all visual terrain colliders are absent from the optimized gameplay scene.
11. Complete - add generation-side density controls for the next source map build:
   - reduce tiny rock scatter density first
   - reduce tiny bush/grass detail if it exists in visual meshes
   - keep large mountains and blockers visually strong
   - protect city/camp clearings and beach silhouette
12. Complete - build a static batching / SRP batching audit:
   - confirm shared materials are actually shared
   - confirm chunk renderers are compatible with SRP batching
   - avoid per-renderer material property changes on terrain chunks
13. Complete - generate metrics for `Game_Terrain7`:
   - renderer count
   - material slot count
   - unique material count
   - mesh asset count
   - total vertices/triangles
   - mesh asset disk size
   - largest chunk vertices/triangles
14. Complete with visual-QA handoff - capture proof images from the actual gameplay camera range:
   - normal camera height `34`
   - build camera height `90`
   - 21:9 wide aspect
   - no visible LOD swap required
   - Terrain7 copies the current source proof captures as optional visual references; final visual QA should be run interactively or on device
15. Complete with profiler handoff - profile in Unity with the gameplay camera:
   - draw calls / batches
   - SetPass calls
   - triangles visible at normal and build camera height
   - GPU frame time
   - CPU render thread time
   - memory used by terrain mesh assets and textures
16. Complete - decide if chunk activation is needed only after profiling: deferred until on-device/Frame Debugger evidence shows renderer/culling cost is still too high.
   - if GPU/CPU is already acceptable, do not add runtime streaming
   - if memory is too high, consider loading scene chunks additively
   - if CPU culling/renderer count is too high, consider distance-based chunk activation
17. Complete by deferral - if distance-based chunk activation is needed later, implement it with hysteresis and never swap mesh shape:
   - near and far chunks use the same mesh
   - only disable chunks clearly outside camera relevance
   - add a buffer zone so chunks do not flicker while panning
18. Complete - document final acceptance:
   - no visible terrain shape changes during gameplay
   - no obvious popping during pan/zoom
   - no gameplay data modified
   - measurable final-scene cleanup versus the rejected `Game_Terrain6` live-LOD path; no expected triangle/vertex improvement versus `Game_Terrain5` because the optimized chunk mesh is intentionally unchanged

## Recommended Strategy

Start with `Game_Terrain7` as a non-LOD optimized copy of the accepted terrain. This pass is a risk and memory cleanup after rejecting visible LOD swapping, not a triangle-reduction pass over `Game_Terrain5`. Only add runtime chunk activation or source-density reductions if profiling proves the static optimized terrain is still too expensive.

## Rejected For Gameplay

- Live terrain LOD swaps.
- Triangle-thinned terrain chunks that alter silhouette or clearings.
- Baked far-map replacement while the player is actively controlling units.
- Entities/Jobs terrain streaming before profiling proves static terrain is the bottleneck.
