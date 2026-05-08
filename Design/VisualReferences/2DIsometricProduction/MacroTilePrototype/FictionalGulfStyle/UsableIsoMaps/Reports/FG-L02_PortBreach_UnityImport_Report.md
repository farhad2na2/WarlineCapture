# Fictional Gulf L02 - Port Breach Unity Import Report

Date: 2026-05-05

## Source

- Map: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Maps/FG-L02_PortBreach.map.json`
- Scene: `Assets/Game/Scenes/DesignTargets/FG02_PortBreach_UsableIsoMap.unity`
- Clean visual target capture: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports/FG-L02_PortBreach_CleanVisualTarget.png`
- Placeholder terrain capture: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports/FG-L02_PortBreach_PlaceholderTerrain.png`
- Metadata overlay capture: `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports/FG-L02_PortBreach_MetadataOverlay.png`
- Visual validation target: `Assets/Game/Art/Generated/IsometricMaps/Previews/FG-L02_PortBreach_Preview.png`
- Macro tile art root: `Assets/Game/Art/Generated/IsometricMaps/MacroTiles`

## Parsed Data

- Map id: `iso.fg.l02.port_breach`
- Grid: `640 x 512` cells
- Macro chunks: `20`
- Road graph nodes: `10`
- Road graph edges: `10`
- Blocked regions: `5` area `43408` cells
- Road regions: `5` area `56800` cells
- Water regions: `1` area `40960` cells
- Sockets: `8`
- Spawn groups: `6`
- Authored macro visual chunks: `0 / 20`
- Placeholder macro visual chunks: `20`

## Validation

- Road graph connected: `PASS`
- Road graph edges resolve: `PASS`
- Macro tile coverage complete: `PASS`
- Sockets in bounds: `PASS`
- Spawn cells in bounds: `PASS`
- Target live units: `1800`
- Stress live units: `2600`
- Max path requests per frame: `32`

## Visual Validation Status

- Current Unity scene includes the full-map visual preview as a reference plane.
- Authored macro tile PNGs are used when available; otherwise placeholder macro chunks remain on top for alignment checks.
- Clean target, placeholder terrain, and metadata overlay captures are separate artifacts for manual review.
- Gate status: `AWAITING_FG_L01_VISUAL_APPROVAL`.
- This is ready for first eye-test validation of the map direction, but not yet a production macro-tile assembly.

## Missing Macro Tile Art

- `fg.mt.damaged_civic_block` variant `A` rotation `0` at macro `0,0`
- `fg.mt.rooftop_lz` variant `A` rotation `0` at macro `1,0`
- `fg.mt.command_plaza` variant `B` rotation `0` at macro `2,0`
- `fg.mt.urban_straight_road` variant `A` rotation `90` at macro `3,0`
- `fg.mt.port_edge` variant `A` rotation `0` at macro `4,0`
- `fg.mt.arcade_market_block` variant `B` rotation `0` at macro `0,1`
- `fg.mt.urban_intersection` variant `A` rotation `0` at macro `1,1`
- `fg.mt.service_yard` variant `A` rotation `0` at macro `2,1`
- `fg.mt.service_yard` variant `B` rotation `0` at macro `3,1`
- `fg.mt.port_edge` variant `B` rotation `0` at macro `4,1`
- `fg.mt.urban_straight_road` variant `B` rotation `0` at macro `0,2`
- `fg.mt.urban_intersection` variant `B` rotation `0` at macro `1,2`
- `fg.mt.damaged_civic_block` variant `B` rotation `90` at macro `2,2`
- `fg.mt.seawall_battery_pad` variant `A` rotation `0` at macro `3,2`
- `fg.mt.port_edge` variant `C` rotation `0` at macro `4,2`
- `fg.mt.arcade_market_block` variant `C` rotation `180` at macro `0,3`
- `fg.mt.urban_straight_road` variant `C` rotation `90` at macro `1,3`
- `fg.mt.enemy_assault_node` variant `A` rotation `0` at macro `2,3`
- `fg.mt.enemy_assault_node` variant `B` rotation `0` at macro `3,3`
- `fg.mt.port_edge` variant `D` rotation `0` at macro `4,3`

## Next Step

Replace placeholder macro chunk colors with authored visual chunk sprites, then feed the same regions into ECS grid buffers for movement validation.
