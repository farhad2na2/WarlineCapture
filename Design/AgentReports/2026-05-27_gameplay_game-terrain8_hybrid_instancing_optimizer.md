# Game_Terrain8 Hybrid Ground-Chunk GPU Instancing Optimizer

Date: 2026-05-27

Purpose: `Game_Terrain8` is a profiling candidate that combines only the ground/foundation into 256m chunks and preserves generated dressing as repeated GPU-instancing-friendly renderers.

Implementation:
- Copy `Game_Terrain4` to `Game_Terrain8`.
- Combine only `Island/ExpandedIsland_SourceGameTerrain3PrefabsOnly` into `GameTerrain8GroundChunks_256m`.
- Keep generated mountains, trees, bushes, and rocks as scene renderers.
- Assign material copies from `Assets/Game/GeneratedTerrainOptimized/Game_Terrain8/InstancedMaterials` with GPU instancing enabled.
- Remove visual colliders and disable shadows, probes, motion vectors, and static flags.

Results:
- Source island renderers/material slots/triangles: `52165` / `52165` / `32210455`
- Source foundation renderers/material slots/triangles: `22474` / `22474` / `10147121`
- Source dressing renderers/material slots/triangles: `29691` / `29691` / `22063334`
- Combined ground renderers/material slots/triangles: `317` / `317` / `10147121`
- Final island renderers/material slots/triangles: `30008` / `30008` / `32210455`
- Ground chunk/material buckets: `317`
- Ground mesh asset disk bytes: `1083346069`
- Instanced material copies/enabled: `2` / `2`
- Dressing renderers eligible for repeated mesh/material instancing: `29691`
- Repeated instancing groups: `82`
- Largest repeated instancing group: `1187`
- Removed colliders / LOD switchers / nonessential objects: `39867` / `0` / `2`

Profiling note:
- This pass prepares Unity GPU-instancing conditions for dressing, but actual instanced draw calls must still be verified in Frame Debugger or Profiler on the target renderer/device.
- Compared with `Game_Terrain7`, this scene is expected to have more renderers but much less generated unique mesh data because trees, bushes, rocks, and mountains are not baked into unique chunk meshes.

Proof captures:
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain8_HybridInstancing/game_terrain8_source_topdown_proof.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain8_HybridInstancing/game_terrain8_source_playable_angle_proof.png`

Run command:
- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain8HybridInstancingOptimizer.BuildHybridInstancedTerrain`
