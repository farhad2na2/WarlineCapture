# Terrain7 vs Terrain8 Performance Comparison

Date: 2026-05-28

Purpose: compare the full chunk-combined `Game_Terrain7` path against the hybrid ground-chunk plus GPU-instancing-prepared `Game_Terrain8` path.

Method:
- Open each scene in the same editor process.
- Disable fog.
- Render the same offscreen camera route at `1600x900`.
- Use `8` warmup frames and `48` measured frames.
- Record static scene metrics, instancing eligibility, and editor offscreen render-call wall time.

Results:

`Game_Terrain7`:
- Renderers/material slots/triangles: `378` / `378` / `32210455`
- Unique meshes/materials: `378` / `4`
- Mesh asset disk bytes: `8441061021`
- Colliders / LOD switchers: `0` / `0`
- Instancing-eligible renderers / repeated groups / largest group: `0` / `0` / `0`
- Average render ms / p95 render ms / estimated FPS: `0.641` / `2.060` / `1559.8`
- Profiler counters, avg draw/batch/setpass/tris/verts: `0` / `0` / `0` / `0` / `0`

`Game_Terrain8`:
- Renderers/material slots/triangles: `30008` / `30008` / `32210455`
- Unique meshes/materials: `399` / `5`
- Mesh asset disk bytes: `1087091013`
- Colliders / LOD switchers: `0` / `0`
- Instancing-eligible renderers / repeated groups / largest group: `29691` / `82` / `1187`
- Average render ms / p95 render ms / estimated FPS: `0.700` / `0.947` / `1428.1`
- Profiler counters, avg draw/batch/setpass/tris/verts: `0` / `0` / `0` / `0` / `0`

Interpretation:
- Faster editor offscreen route: `Game_Terrain7`.
- Lower generated mesh asset memory: `Game_Terrain8`.
- Better GPU-instancing setup: `Game_Terrain8`.

Caveat:
- This is not a final mobile FPS verdict. Unity GPU instancing should be confirmed in Frame Debugger or Profiler on the target device/player because editor offscreen rendering, batchmode, and mobile GPU drivers can produce different bottlenecks.

Output JSON:
- `Design/AgentReports/Data/GeneratedScenes/TerrainOptimizationComparison/terrain7_vs_terrain8_comparison.json`
