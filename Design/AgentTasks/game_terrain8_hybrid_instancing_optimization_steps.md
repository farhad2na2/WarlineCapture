# Game_Terrain8 Hybrid Instancing Optimization Steps

Date: 2026-05-27

Goal: create `Game_Terrain8` as a profiling candidate that keeps only the ground/foundation combined into 256m chunks while preserving generated trees, bushes, rocks, and mountains as GPU-instancing-friendly repeated renderers.

Reason: `Game_Terrain7` minimizes renderer count by combining all visual terrain into unique chunk meshes, but that makes generated mesh assets very large and removes most GPU-instancing opportunities. `Game_Terrain8` tests the opposite tradeoff for dressing: fewer generated mesh assets and repeated mesh/material batches, with more scene renderers.

Rules:

- Source scene is `Game_Terrain4`.
- Target scene is `Game_Terrain8`.
- Combine only `Island/ExpandedIsland_SourceGameTerrain3PrefabsOnly`.
- Keep generated dressing groups as renderers, not chunk-combined meshes.
- Assign instancing-enabled material copies to dressing renderers.
- Clear static flags on dressing so Unity can use GPU instancing.
- Strip visual colliders; gameplay remains grid/blocker/heightmap driven.
- Do not add LOD swapping.
- Do not modify source art prefabs or original material assets.

Steps:

1. Complete - add the `Game_Terrain8` workflow to the one-go terrain workflow document.
2. Complete - add an editor optimizer that copies `Game_Terrain4` to `Game_Terrain8`.
3. Complete - collect and combine only source foundation renderers into `256m` ground chunks.
4. Complete - delete the original foundation hierarchy after the chunk bake.
5. Complete - preserve generated dressing groups as repeated renderers.
6. Complete - create instancing-enabled material copies for dressing renderers.
7. Complete - assign those material copies to dressing renderers and clear static flags.
8. Complete - strip visual colliders and disable shadows/probes/motion vectors.
9. Complete - write metrics and proof reports for the hybrid scene.
10. Complete - run Unity batchmode and validate `Game_Terrain8`.

Acceptance:

- `Game_Terrain8.unity` exists.
- `Game_Terrain8` contains combined ground chunk renderers.
- `Game_Terrain8` still contains generated dressing groups.
- Dressing materials used in the scene have GPU instancing enabled.
- No visual colliders remain under `Island`.
- No LOD switchers or LOD1/LOD2 roots exist.
- Summary reports the instancing-eligible renderer count and repeated mesh/material group count.
