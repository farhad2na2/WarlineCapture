# WarlineCapture 3D Operation Map Texture/Mask Workflow

Date: 2026-05-24

This is the gameplay-facing workflow for turning authored or generated terrain map packs into a 2024x2024 WarlineCapture operation map. It applies to the active 3D single-map direction and replaces any old 2D/isometric macro-tile assumptions for new battlefield maps.

## Purpose

Each operation map should be built from a reviewed image pack:

- a visual terrain plate used as the ground/background reference
- gameplay masks used by editor/runtime tools to create blockers, terrain height, trees, rocks, and other props

The visible terrain image is not gameplay truth. Gameplay truth comes from masks and generated metadata.

## Current Candidate Pack

Current first candidate:

`Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/`

Files:

| File | Role |
|---|---|
| `base_visual.png` | Synty/POLYGON-style top-down terrain plate for the 2024x2024 operation map. |
| `blocker_mask.png` | Pathfinding blocker source. Black is walkable, white is blocked. |
| `tree_density_mask.png` | Runtime/editor tree placement density. Black is none, light values are denser. |
| `rock_density_mask.png` | Runtime/editor rock/boulder/cliff placement density. Black is none, light values are denser. |
| `height_mask.png` | Terrain elevation reference. Dark is low/flat, light is high/mountain/cliff. |
| `map_pack_manifest.json` | Machine-readable map-pack notes for generator tooling. |

All current images are normalized to 2024x2024 so one mask pixel can map directly to one gameplay grid cell when desired.

## Required Map Layout

For the first Synty-highlands candidate:

- The large city/town reserve is open and mostly flat near the center/center-left.
- Two military base/camp reserves are far apart, one in the northwest area and one in the southeast area.
- Open movement lanes connect both bases and the city reserve.
- Mountain, cliff, rock, and dense tree blockers sit inside the map boundary, not exactly on the outer edge.
- The outer edge still has visible terrain beyond the blocker belt so the map does not read as a hard square wall.
- No units, UI, objective markers, gameplay buildings, health bars, or roads with interactive meaning are baked into the visual plate.

## Sampling Rules

Use normalized map coordinates:

```text
u = gridX / 2023.0
v = gridZ / 2023.0
pixelX = round(u * (imageWidth - 1))
pixelY = round((1.0 - v) * (imageHeight - 1))
```

The `pixelY` flip is needed if Unity/world Z increases upward while image origin is top-left. If the tool uses bottom-left texture coordinates already, do not flip twice.

## Blocker Rules

`blocker_mask.png` is authoritative for pathfinding blockers.

Recommended first thresholds:

| Pixel Value | Meaning |
|---|---|
| `0..95` | Walkable/pathable. |
| `96..159` | Review/soft edge. Prefer blocked for pathfinding, but useful for smoothing/debug overlays. |
| `160..255` | Blocked/impassable. |

Gameplay implementation should:

1. Convert blocker pixels to a 2024x2024 blocked-cell grid.
2. Erode tiny one-cell holes inside blocked mountain/forest belts.
3. Remove tiny isolated blocker dots inside city/base reserve zones unless they are intentionally authored.
4. Generate a debug overlay showing blocked cells on top of `base_visual.png`.
5. Validate that city reserve, northwest base reserve, southeast base reserve, and main lanes are connected.
6. Validate that units cannot path beyond the internal natural blocker belt toward the visual map edge.

Do not infer blockers from the visible art when a blocker mask exists.

## Tree Density Rules

`tree_density_mask.png` drives tree placement candidates.

Recommended first interpretation:

| Pixel Value | Spawn Rule |
|---|---|
| `0..31` | No tree. |
| `32..95` | Sparse scrub/tree chance. |
| `96..175` | Medium tree clusters. |
| `176..255` | Dense tree/jungle belt. |

Gameplay implementation should:

- Never spawn trees on blocked cliffs unless the tree prefab is part of a visual blocker layer.
- Never spawn trees inside the reserved city/base build pads except sparse edge scrub.
- Use deterministic seed-per-map for repeatable placement.
- Add minimum-distance spacing between tree prefabs to avoid unreadable clutter.
- Prefer Synty/POLYGON-compatible tree and scrub prefabs.

## Rock Density Rules

`rock_density_mask.png` drives rock, boulder, cliff, and outcrop placement candidates.

Recommended first interpretation:

| Pixel Value | Spawn Rule |
|---|---|
| `0..31` | No rocks. |
| `32..95` | Sparse small rocks. |
| `96..175` | Medium boulder/outcrop clusters. |
| `176..255` | Dense rock/cliff/mountain dressing. |

Gameplay implementation should:

- Prioritize high rock density near blocker belts and height-mask ridges.
- Keep city/base reserves mostly clear unless a designer-authored detail zone allows rocks.
- Treat decorative rocks as visual unless separately registered as path blockers.
- Keep gameplay blockers from `blocker_mask.png`, not from random decorative rock placement.

## Height Rules

`height_mask.png` is an elevation reference, not pathfinding truth by itself.

Recommended first interpretation:

| Pixel Value | Terrain Role |
|---|---|
| `0..63` | Flat lowland, city/base pads, main lanes. |
| `64..143` | Gentle slopes and foothills. |
| `144..207` | Raised terrain and rough hills. |
| `208..255` | Mountains, cliffs, high blocker terrain. |

Gameplay implementation should:

- Keep city/base reserve zones flat enough for future building placement.
- Use height only for terrain shaping, camera/minimap shading, prop alignment, and visual validation.
- Keep blocker decisions tied to `blocker_mask.png`.

## Editor-Time Generation Recommendation

Use editor-time tooling first:

1. Import the map pack into Unity as editor-only source textures.
2. Sample masks into a generated `OperationMapDefinition`.
3. Generate path blockers, terrain height, tree/rock placement candidates, reserve-zone metadata, minimap data, and debug overlays.
4. Save the generated metadata and prefab placements.
5. Review the result in editor captures before allowing it into Campaign/Skirmish.

Runtime random generation can come later for Skirmish, but it must use a saved seed and produce the same metadata every time for QA.

## Required Generated Outputs

A gameplay map generator should produce:

```text
OperationMapDefinition
  mapId
  gridSize = 2024 x 2024
  baseVisualTexture
  blockedCells
  heightSamples
  treeSpawnCandidates
  rockSpawnCandidates
  reserveZones
    CityReserve
    NorthwestBaseReserve
    SoutheastBaseReserve
  lanes / connectivity metadata
  cameraBounds
  minimapProjection
  debugOverlayTextures
```

Acceptance checks:

- City and both base reserves are reachable from each other.
- The blocker belt prevents pathing outside the intended operation area.
- Build reserve zones are mostly clear of trees/rocks/blockers.
- Tree and rock placement matches the Synty/POLYGON military art direction.
- The base visual remains a background/reference layer; runtime units/buildings/VFX/markers remain separate.

## Current Game_Terrain4 Handoff

Current scene:

`Assets/Game/Scenes/Game_Terrain4.unity`

The current `Game_Terrain4` implementation uses the larger source-prefab island regeneration pipeline. The 2024x2024 playable map footprint is now inside green/dirt playable land, with beach and coast retained as visual border content outside the gameplay contract.

Regeneration command:

`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate`

Implementation artifacts:

- Larger-island checklist: `Design/AgentTasks/game_terrain4_larger_island_regeneration_steps.md`
- Full regeneration pipeline report: `Design/AgentReports/2026-05-25_gameplay_game-terrain4-full-regeneration-pipeline.md`
- Step 5 final QA handoff: `Design/AgentReports/2026-05-25_gameplay_game-terrain4-step5-final-qa-handoff.md`
- Validation data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_validation_artifacts.json`
- Playable-land audit data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json`
- Top-down proof: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png`
- 16:9 playable-frame proof: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png`
