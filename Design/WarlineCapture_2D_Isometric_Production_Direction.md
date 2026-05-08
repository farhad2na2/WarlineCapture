# WarlineCapture 2D Isometric Production Direction

Date: 2026-05-05

## Decision

WarlineCapture's active battlefield art direction is premium 2.5D isometric mobile RTS using large authored terrain macro tiles.

The terrain/background layer should be high-quality authored or AI-assisted macro art. Gameplay truth remains separate: ECS/grid/pathfinding, road graphs, building sockets, blockers, objectives, spawn points, and minimap data are metadata, not pixels.

Current WarlineCapture art assets, mockups, target locks, tactical map concepts, unit sprite concepts, and generated UI production images should be treated as AI image-generation outputs unless a specific asset row or handoff explicitly documents another source. AI-generated status does not make an asset approved: every asset still needs WarlineCapture style review, runtime validation, metadata alignment, and asset-register approval before it can be considered final.

This replaces the abandoned tiny modular road-tile/chunk direction. The failed attempts have been removed from the active design folder so future work does not continue that loop.

## Source Of Truth

- Product/gameplay/UI design source of truth: `Design`
- Macro-tile implementation plan: `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
- 2D isometric art bible: `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
- Implementation and validation plan: `Design/WarlineCapture_2D_Isometric_Implementation_Validation_Plan.md`
- Strategic/tactical map gameplay alignment: `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- Visual reference index: `Design/VisualReferences/README.md`
- Active production references: `Design/VisualReferences/2DIsometricProduction`

## Active Terrain Model

Use large terrain macro tiles rather than small road tiles:

- baked visual terrain: roads, curbs, plazas, dirt, terrain wear, non-interactive dressing
- baked 2.5D depth: raised curbs, walls, pads, ramps, shadows, and surface thickness
- runtime entities: buildings, units, turrets, resources, destructible cover, objectives, VFX, UI overlays
- metadata: walkable polygons, road graph nodes, road exits, building sockets, blocker zones, camera/minimap bounds, spawn/objective anchors

Macro tiles should be large enough to hide repetition and preserve the quality of full-scene generated references.

Strategic map art, mission previews, and minimap art are separate from tactical terrain production. Do not upscale or crop a strategic view and call it the playable tactical map unless it passes the tactical unit-scale validation and has matching metadata.

Recommended first size: `2048 x 2048` source chunks, validated in Unity as a 2x2 or 3x3 connected map before considering larger chunks.

Chapter 1 2.5D reference images are stored at:

```text
Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/Chapter01_2_5DReferences
```

## Gameplay Rule

Bake terrain. Do not bake stateful gameplay.

Gameplay buildings must not be part of the terrain image. The background may show empty pads, foundations, parking marks, curbs, or decorative ruins. Runtime places the actual interactive building sprite/entity on the pad.

If a building can take damage, produce units, block pathfinding, be selected, or be destroyed, it is a separate runtime entity.

## Placement Rule

Base construction should use approved sockets/pads rather than arbitrary free placement.

Each macro tile can expose:

- production building sockets
- defense sockets
- resource sockets
- objective sockets
- spawn sockets
- optional temporary deployable zones

The UI must clearly show valid/invalid placement states. This preserves current gameplay systems while avoiding visual mismatch on baked terrain art.

## First Macro-Tile Batch

The first batch should be small and validation-driven:

1. straight road macro tile
2. T-junction macro tile
3. intersection macro tile
4. base/plaza entrance macro tile

Each tile needs:

- source art
- Unity imported sprite
- walkable polygon metadata
- road graph exits
- placement sockets where relevant
- minimap preview crop
- capture/report from a Unity 2x2 or 3x3 assembly scene

## Removed Direction

Do not continue:

- one-diamond modular road tiles
- independently generated road chunks
- chroma-key road-network overlays as implementation assets
- procedural graph roads as final visual art

The lesson from those attempts is retained in the docs: topology and gameplay metadata must be explicit, while high-quality visuals should come from authored macro art.
