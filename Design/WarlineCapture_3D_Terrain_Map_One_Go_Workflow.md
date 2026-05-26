# WarlineCapture 3D Terrain Map One-Go Workflow

Date: 2026-05-26

Purpose: this is the single reference workflow for creating a new large 3D operation map from image inputs, generating the Unity scene from those images, and producing the optimized mobile visual terrain. This workflow intentionally excludes the rejected LOD/far-map step.

## Current Decision

- Use image/mask packs as the source of a map concept.
- Generate a readable source scene first: `Game_Terrain4`.
- Generate the optimized chunk-bake scene from that source: `Game_Terrain5`.
- Generate the final no-LOD gameplay-ready scene from the optimized bake: `Game_Terrain7`.
- Do not use live terrain LOD swapping for gameplay.
- Do not run `Game_Terrain6` for new maps.
- Treat `Game_Terrain7` as the final scene name, but remember the real renderer/material optimization happens in `Game_Terrain5`.

## Source Inputs

Every new map starts as a map pack under:

`Design/VisualTargets/Gameplay/MapPacks/<MapPackId>/`

Required files:

| File | Required | Role |
|---|---:|---|
| `base_visual.png` | Yes | Designer-facing top-down terrain reference in Synty/POLYGON style. |
| `blocker_mask.png` | Yes | Pathfinding blocker source. Black means walkable; white means blocked. |
| `tree_density_mask.png` | Yes | Tree/jungle/scrub placement density. |
| `rock_density_mask.png` | Yes | Rock, boulder, mountain, and cliff dressing density. |
| `height_mask.png` | Yes | Elevation and ridge reference for visual placement. |
| `map_pack_manifest.json` | Yes | Machine-readable notes: map id, grid size, reserve zones, seed, authoring notes. |

Current canonical pack:

`Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/`

## Image Generation Rules

Generate or author the image pack as a coordinated set, not as unrelated images.

Required image size:

- `2024 x 2024` pixels.
- One pixel can map to one gameplay grid cell.
- If a future map uses `2048 x 2048`, document the conversion and do not silently mix sizes.

Visual style:

- Synty/POLYGON-compatible military island terrain.
- Dirt, grass, rocks, cliffs, mountains, trees, and beach/coast border.
- No photorealistic satellite texture.
- No UI, unit icons, roads with gameplay meaning, markers, text, or baked objective symbols.

Layout requirements:

- One large city/town reserve.
- Two military base/camp reserves far apart.
- Clear movement lanes between city and bases.
- Mountains/jungle/blocker belts inside the playable footprint, not exactly on the map edge.
- Beach/coast outside the green/dirt gameplay footprint as visual border only.
- City/base reserves mostly flat and clear for later building/unit placement.

Mask consistency rules:

- `base_visual.png` should visually agree with all masks.
- `blocker_mask.png` is gameplay authority for pathing.
- `height_mask.png` supports visual height and ridges, but does not create blockers by itself.
- `tree_density_mask.png` and `rock_density_mask.png` are placement density inputs only.
- Reserved city/base areas must be mostly low density in tree/rock masks.

Source-scene fidelity rule:

- Current `Game_Terrain4` generation treats `base_visual.png` as a designer reference and mask companion, not as an exact projected terrain texture.
- `Game_Terrain4` should match the reference at the level of broad layout, reserves, blocker belts, and biome intent.
- Exact tree/bush canopy density, mountain footprint size, and dirt/grass paint pattern are not guaranteed unless a separate visual-fidelity pass is run.
- If exact visual matching is required, add a dedicated fidelity pass before optimization: clean non-debug top-down capture, density heatmap comparison, mountain footprint coverage check, and either projected ground texture/decal support or higher-density clustered prefab placement.
- Planned fidelity pass: `Design/AgentTasks/game_terrain4_reference_fidelity_pass_steps.md`.

## Coordinate Contract

Use normalized map coordinates:

```text
u = gridX / 2023.0
v = gridZ / 2023.0
pixelX = round(u * (imageWidth - 1))
pixelY = round((1.0 - v) * (imageHeight - 1))
```

Rules:

- Keep the 2024 playable map footprint inside the island green/dirt land.
- Beach/coast may overlap visually near the outer border, but beach prefab centers must not sit inside the gameplay footprint.
- Gameplay truth comes from generated grid/blocker/height data, not from the decorative visual mesh.

## One-Go Scene Generation

Source scene output:

`Assets/Game/Scenes/Game_Terrain4.unity`

Run command:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate
```

What this command does:

1. Rebuilds the larger island foundation from the `Game_Terrain3` source-prefab examples.
2. Keeps the island foundation as source-prefab placement, not generated replacement terrain meshes.
3. Samples the active map pack masks over the explicit 2024x2024 gameplay footprint.
4. Places mountains, trees, bushes, and rocks from the `Game_Terrain3/Island` example model groups.
5. Keeps generated dressing under `Game_Terrain4/Island`.
6. Validates playable-land coverage and reserve-zone clearances.
7. Writes reports, JSON summaries, and proof captures.
8. Run the editor warning contract after any terrain generator script change:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod EditorScriptWarningContractTests.RunEditorWarningContractBatchValidation
```

Do not hand off terrain generator script changes while this warning contract fails.

Current implementation:

- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain4FullRegenerationPipeline.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain3Island2048Builder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain4MaskDressingBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureGameTerrain4PlayableLandAudit.cs`

Expected source-scene structure:

```text
Game_Terrain4
└── Island
    ├── ExpandedIsland_SourceGameTerrain3PrefabsOnly
    ├── GeneratedMountains
    ├── GeneratedTrees
    ├── GeneratedBushes
    ├── GeneratedRocks
    └── Validation/Debug outputs
```

## One-Go Optimization

Optimized visual output:

`Assets/Game/Scenes/Game_Terrain5.unity`

Run command for current `Game_Terrain4`:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.BuildOptimizedShippingTerrain
```

Run command for full source generation, optimization, and final no-LOD scene generation:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize
```

What this optimization does:

1. Copies `Game_Terrain4` to `Game_Terrain5`.
2. Temporarily enables source mesh readability where Unity needs it for mesh combination.
3. Combines visual meshes by `256m chunk + material`.
4. Deletes the source prefab-instance visual hierarchy from the optimized scene.
5. Removes visual terrain colliders by not carrying source objects forward.
6. Disables terrain renderer costs:
   - shadows off
   - receive shadows off
   - light probes off
   - reflection probes off
   - motion vectors off
7. Writes generated chunk mesh assets to:

`Assets/Game/GeneratedTerrainOptimized/Game_Terrain5/`

8. Copies `Game_Terrain5` to `Game_Terrain7` as the final no-LOD gameplay-ready scene.
9. Verifies/removes any rejected LOD switcher or LOD1/LOD2 roots if they exist.
10. Leaves `Game_Terrain7` using the same optimized `Game_Terrain5` chunk mesh assets; it does not duplicate another terrain mesh set.

Current result from the first optimized map:

| Metric | Game_Terrain4 Source | Game_Terrain5 Optimized |
|---|---:|---:|
| Renderers | 25,993 | 296 |
| Material slots | 25,993 | 296 |
| Unique terrain materials | many source instances | 3 |
| Chunk mesh assets | n/a | 296 |

Important: `Game_Terrain5` is the real visual performance optimization stage. It reduces renderer/material submission count. It does not necessarily reduce triangles, because the visual shape is preserved.

## Final Scene To Use

For a new non-LOD mobile map, use:

`Assets/Game/Scenes/Game_Terrain7.unity`

Role split:

- `Game_Terrain4`: readable generated source scene.
- `Game_Terrain5`: optimized chunk bake and metrics source.
- `Game_Terrain7`: final no-LOD gameplay-ready scene copied from `Game_Terrain5`.

Do not use:

- `Game_Terrain4` as a shipping terrain scene; it is the readable source scene.
- `Game_Terrain5` as the final gameplay scene if `Game_Terrain7` has been generated; keep it as the optimization bake/source for metrics.
- `Game_Terrain6` for gameplay; live LOD/far-map swapping was rejected.

## Rejected LOD Path

Do not include these steps in the normal map pipeline:

- Generate `LOD1_Mid_SimplifiedChunks`.
- Generate `LOD2_FarMap_SimplifiedChunks`.
- Add `WarlineCaptureTerrainLodHeightSwitch`.
- Swap terrain geometry by camera height or distance during gameplay.
- Replace live gameplay terrain with a baked far-map plane.

Reason: the LOD swap changed visible terrain shape and was noticeable during play.

## Final No-LOD Cleanup Pass

`WarlineCaptureGameTerrain7NonLodMobileOptimizer.BuildNonLodMobileTerrain` now copies from:

`Assets/Game/Scenes/Game_Terrain5.unity`

It creates:

`Assets/Game/Scenes/Game_Terrain7.unity`

It guarantees:

- no live LOD switcher
- no `LOD1_Mid_SimplifiedChunks`
- no `LOD2_FarMap_SimplifiedChunks`
- no duplicate Terrain7 mesh asset set
- same optimized chunk meshes as `Game_Terrain5`

It does not improve triangles/vertices versus `Game_Terrain5`, because it keeps the optimized chunk mesh shape unchanged.

## Legacy Cleanup If A Terrain6 Scene Already Exists

If an older branch already has a `Game_Terrain6` scene and you need to remove the rejected LOD path, regenerate through the current one-go workflow instead of continuing from Terrain6:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize
```

## Validation Checklist

Before accepting a generated optimized map:

- `Game_Terrain4` contains the readable source island and generated dressing.
- `Game_Terrain5` contains combined chunk meshes, not the full source prefab hierarchy.
- `Game_Terrain5` renderer count is near the current target range, around hundreds, not tens of thousands.
- `Game_Terrain5` material slots are near the renderer count.
- `Game_Terrain7` exists and references the optimized chunk mesh assets from `Game_Terrain5`.
- Unique terrain materials stay small; current target is `3`.
- No visual terrain colliders are present in the optimized visual scene.
- Shadows, probes, and motion vectors are disabled on visual terrain renderers.
- City and both base reserves remain clear enough for gameplay placement.
- Pathing blockers come from `blocker_mask.png`, not from decorative mesh assumptions.
- No LOD switcher or LOD1/LOD2 roots exist in the final gameplay terrain scene.

## Required Reports And Artifacts

Generation reports:

- `Design/AgentReports/2026-05-25_gameplay_game-terrain4-full-regeneration-pipeline.md`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_FullRegeneration/game_terrain4_full_regeneration_summary.json`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_validation_artifacts.json`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json`

Optimization reports:

- `Design/AgentReports/2026-05-26_gameplay_game-terrain5_one_go_optimizer.md`
- `Design/AgentReports/Data/GeneratedScenes/GameTerrain5_Optimization/game_terrain5_optimization_summary.json`

Visual proof:

- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png`
- `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png`

## Fast Repeat Workflow

When creating a new map pack:

1. Generate/review the five image inputs and manifest under `Design/VisualTargets/Gameplay/MapPacks/<MapPackId>/`.
2. Point the Terrain4 mask dressing builder at that map pack.
3. Run:

```bash
Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize
```

4. Review `Game_Terrain4` proof captures for source quality.
5. Review `Game_Terrain5` stats for renderer/material reduction.
6. Use `Game_Terrain7` as the final non-LOD optimized visual terrain scene.

If optimization is still not enough after profiling, do not reintroduce LOD. Use one of these next passes instead:

- reduce source prop density before `Game_Terrain4` generation
- simplify selected prefab meshes offline while preserving silhouette
- simplify terrain materials/shaders
- add same-mesh chunk activation only after profiling proves renderer/culling cost is the bottleneck
