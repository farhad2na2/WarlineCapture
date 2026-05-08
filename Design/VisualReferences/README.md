# WarlineCapture Visual References

Date: 2026-05-05

This folder holds visual references that support the original WarlineCapture design documents.

## Active Production Direction

WarlineCapture's gameplay-facing art direction is premium 2D isometric mobile RTS using large terrain macro tiles.

Macro tiles provide the terrain visuals. Separate metadata provides gameplay truth: walkable zones, blockers, road graph, sockets, spawns, objectives, minimap, and camera bounds.

## Folders

- `2DIsometricConcepts`
  - Exploratory 2D isometric direction references.
  - These are composition and style references, not direct Unity import assets.

- `2DIsometricProduction`
  - Active 2D isometric production references and validation outputs.
  - Keeps ISO-01/ISO-02 baseline references, ISO-04 terrain-quality reference, Chapter 1 2.5D macro-tile references, and future macro-tile outputs.

## Active Reference Set

| ID | Direction | Target | Report |
|---|---|---|---|
| `ISO-01` | City Command 2D isometric mobile RTS static Tilemap spike | `2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png` | `2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md` |
| `ISO-02` | City Command 2D isometric runtime movement prototype | `2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Mid.png` | `2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Report.md` |
| `ISO-04` | Polished terrain-only visual reference | `2DIsometricProduction/TerrainVisualTarget/ISO04_TerrainVisualTarget.png` | `2DIsometricProduction/TerrainVisualTarget/README.md` |
| `CH01-2.5D` | Chapter 1 2.5D macro-tile mission references | `2DIsometricProduction/MacroTilePrototype/Chapter01_2_5DReferences` | `2DIsometricProduction/MacroTilePrototype/Chapter01_2_5DReferences/README.md` |

Unity-side baseline outputs:

- ISO-01 scene: `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`
- ISO-01 builder: `Assets/Game/Scripts/Editor/WarlineCaptureIso2DSpikeBuilder.cs`
- ISO-02 scene: `Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity`
- ISO-02 runtime scripts: `Assets/Game/Scripts/Iso2D`
- ISO-02 builder: `Assets/Game/Scripts/Editor/WarlineCaptureIso2DRuntimePrototypeBuilder.cs`

## Removed Attempts

The ISO-03 and ISO-05 through ISO-11 road/tile/chunk/procedural attempts were removed from the active design folder because they do not match the selected macro-tile production direction.

Do not recreate those paths unless a future decision explicitly reopens them.

## Rule

Generated visual references are direction locks, not automatic production assets. Production acceptance requires Unity validation for scale, readability, metadata alignment, memory, and gameplay compatibility.
