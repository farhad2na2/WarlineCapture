# Fictional Gulf Usable Iso Maps

Date: 2026-05-05

This folder starts the usable large-map direction based on `WarlineCapture_FictionalGulf_PremiumBattle_Target.png`.

The maps are not single giant generated images. They are production map definitions: large visual macro tiles plus explicit gameplay metadata. This is the path that can keep the premium 2.5D/3D look while supporting large battlefields and thousands of units.

## Source Style

- `../WarlineCapture_FictionalGulf_PremiumBattle_Target.png`
- `../README.md`
- `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
- `Design/WarlineCapture_2D_Isometric_Art_Bible.md`

## Files

- `Schema/warlinecapture_iso_map_schema_v0_1.json`
  - Machine-readable map metadata shape.

- `MacroTiles/FG_MacroTile_Catalog_v0_1.json`
  - First fictional Gulf macro-tile catalog for high-quality streamed terrain chunks.

- `Assets/Game/Art/Generated/IsometricMaps/MacroTiles/FG01_TerrainMacroTile_RequestManifest.json`
  - First Unity-facing terrain macro-tile request/import manifest.

- `Maps/FG-L01_CoastalCommand.map.json`
  - First vertical-slice large map: command base, civic district, port edge, air/naval pressure.

- `Maps/FG-L02_PortBreach.map.json`
  - Large port assault map: seawall lanes, dockyard sectors, fortified port node.

- `Maps/FG-L03_AirNavalDefense.map.json`
  - Wide air/naval defense map: multiple LZs, long roads, port batteries, and dispersed bases.

- `Previews/`
  - Visual previews for the three map definitions.

- `UnityImplementationPlan.md`
  - Step-by-step plan for importing these map definitions into Unity scenes and ECS grid data.

- Unity editor builder:
  - `Assets/Game/Scripts/Editor/WarlineCaptureFictionalGulfIsoMapBuilder.cs`
  - Menu: `WarlineCapture/Design/Build FG-01 Usable Iso Map`
  - Scene output: `Assets/Game/Scenes/DesignTargets/FG01_CoastalCommand_UsableIsoMap.unity`
  - Report output: `Reports/FG-L01_CoastalCommand_UnityImport_Report.md`
  - Capture outputs:
    - `Reports/FG-L01_CoastalCommand_CleanVisualTarget.png`
    - `Reports/FG-L01_CoastalCommand_PlaceholderTerrain.png`
    - `Reports/FG-L01_CoastalCommand_MetadataOverlay.png`
  - Authored macro-tile art root: `Assets/Game/Art/Generated/IsometricMaps/MacroTiles`
  - Missing macro-tile art falls back to placeholder chunks and is listed in the report.

## Large-Map Rule

Visual quality comes from authored macro tiles. Simulation quality comes from metadata.

Do not use one huge full-scene image for a playable map. For large battles, use:

- streamed `2048 x 2048` or `4096 x 4096` macro visual chunks
- fixed connector templates
- grid and road-graph metadata
- sector/corridor pathing for long-distance movement
- socket-based buildings and objectives
- runtime units, vehicles, aircraft, boats, VFX, and destruction

## Unit Scale Target

The initial map definitions target these simulation bands:

| Band | Target |
|---|---:|
| Playable units | 1,000-3,000 live entities |
| Visible detailed units | 80 near camera |
| Mid/low LOD units | 1,800 budgeted by current render systems |
| Far/offscreen units | abstract/impostor/sim-only |
| Path requests | group/corridor based, not all units at once |

## Implementation Order

1. Import the map schema and one map JSON into a Unity editor importer.
2. Build macro-tile sprite placeholders first, then replace with authored high-quality chunks.
3. Convert `blockedRegions`, `roadCorridors`, and `sidewalkCorridors` into ECS `GridWalkable`, `GridRoad`, `GridRoadSidewalk`, and `GridRoadDirt` buffers.
4. Convert `roadGraph` into long-distance group/corridor movement metadata.
5. Convert `sockets` into valid build/objective/spawn placement anchors.
6. Stream visible visual chunks around the camera.
7. Stress test `FG-L01` with 1,000 units before scaling to `FG-L02` and `FG-L03`.

## Acceptance

A map is usable only when:

- roads and macro-tile edges visibly connect
- grid metadata agrees with visible roads and blockers
- sockets line up with visible pads
- units can move from all spawn zones to all objective zones
- 1,000 unit stress mode keeps pathing and rendering stable
- no real-country flags, landmarks, or readable political text are present
