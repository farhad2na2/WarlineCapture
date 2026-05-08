# FG01 Terrain Macro-Tile Request Brief

Date: 2026-05-05

## Purpose

This is the first production request package for WarlineCapture's FG-L01 terrain macro tiles. It covers terrain only: roads, curbs, plazas, empty foundations, seawalls, water-edge detail, and non-interactive dressing.

Runtime gameplay buildings, units, vehicles, aircraft, ships, VFX, selection rings, health bars, objective markers, and UI are separate lanes and should not be baked into these terrain chunks.

## Source Of Truth

- Request manifest: `Assets/Game/Art/Generated/IsometricMaps/MacroTiles/FG01_TerrainMacroTile_RequestManifest.json`
- Map source: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Maps/FG-L01_CoastalCommand.map.json`
- Tile catalog: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/MacroTiles/FG_MacroTile_Catalog_v0_1.json`
- Validation scene: `Assets/Game/Scenes/DesignTargets/FG01_CoastalCommand_UsableIsoMap.unity`
- Clean visual target: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports/FG-L01_CoastalCommand_CleanVisualTarget.png`
- Metadata overlay: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports/FG-L01_CoastalCommand_MetadataOverlay.png`

## First Requested Tiles

1. `fg.mt.urban_straight_road` variant `A`
   - Required files: `Shared/fg_mt_urban_straight_road_a.png`, `Shared/fg_mt_urban_straight_road_a_rot90.png`
   - Purpose: main city road corridor and rotated road segment coverage.

2. `fg.mt.urban_intersection` variant `A`
   - Required file: `Shared/fg_mt_urban_intersection_a.png`
   - Purpose: central battle decision point and road graph alignment test.

3. `fg.mt.command_plaza` variant `A`
   - Required file: `iso_fg_l01_coastal_command/fg_mt_command_plaza_a.png`
   - Purpose: friendly base terrain pads and command/radar socket alignment.

4. `fg.mt.port_edge` variant `A`
   - Required file: `Shared/fg_mt_port_edge_a.png`
   - Purpose: coastal road, seawall, loading pads, and water boundary validation.

5. `fg.mt.seawall_battery_pad` variant `A`
   - Required file: `iso_fg_l01_coastal_command/fg_mt_seawall_battery_pad_a.png`
   - Purpose: coastal-defense socket alignment without baking a runtime turret.

## Acceptance

- Road, curb, sidewalk, seawall, and plaza edges align at macro-tile boundaries.
- Empty building/defense pads line up with metadata sockets in the FG-L01 overlay.
- Terrain reads clearly at gameplay zoom without UI overlays.
- No runtime gameplay entities are baked into terrain.
- The Unity import report shows authored macro visual chunks replacing placeholders for the requested files.
