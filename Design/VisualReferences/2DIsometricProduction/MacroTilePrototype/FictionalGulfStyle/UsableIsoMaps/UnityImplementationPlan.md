# Unity Implementation Plan

Date: 2026-05-05

This plan converts the fictional Gulf usable iso map package into Unity runtime scenes.

## Goal

Build `FG-L01_CoastalCommand` first as a playable streamed iso map that can scale toward 1,000+ units without losing the premium 2.5D visual direction.

## Step 1 - Import Map Data

Create an editor importer that reads:

- `Schema/warlinecapture_iso_map_schema_v0_1.json`
- `MacroTiles/FG_MacroTile_Catalog_v0_1.json`
- `Maps/FG-L01_CoastalCommand.map.json`

The importer should create a Unity scene or ScriptableObject map asset with:

- grid size and origin
- macro-tile placements
- road graph nodes and edges
- blocked/water/road/sidewalk regions
- sockets
- spawn groups
- performance target metadata

## Step 2 - Build ECS Grid Buffers

Convert map regions into the current ECS grid data:

- `GridConfig`
- `GridWalkable`
- `GridRoad`
- `GridRoadSidewalk`
- `GridRoadDirt`
- dynamic blocker support for runtime buildings/destruction

The first pass can rasterize rectangular regions. Later passes can support polygons.

## Step 3 - Visual Chunks

Do not create one full-map sprite.

Use one GameObject per visible macro visual chunk:

- chunk size from `macroTileGrid.macroTileSizeCells`
- source art target `4096 x 4096` for final chunks
- lower-resolution placeholder chunks allowed for the first importer test
- stream by camera chunk radius
- keep chunk art passive: no gameplay buildings, units, VFX, UI, or health/state information baked in

Before real chunks exist, the validation scene may include the full-map preview image as a clearly named visual target plane. That plane is only for eye-test validation and alignment discussion. It is not a production terrain implementation and must be replaced by authored macro-tile sprites before runtime validation.

## Step 4 - Sockets And Runtime Objects

Convert sockets into editor-visible markers:

- command building sockets
- radar sockets
- defense sockets
- landing zones
- enemy objective sockets
- coastal battery sockets

Runtime buildings, turrets, aircraft, boats, VFX, and units must instantiate separately on top of the terrain.

After `FG-L01` visual direction is approved, start the first runtime asset request batch before the movement/combat probe:

- rifle soldier, rocket soldier, civilian
- APC, tank
- transport helicopter, attack helicopter
- patrol boat
- command post, barracks, helipad, guard tower
- construction, upgrade, damaged, destroyed, smoke/fire/scorch/wake/rotor-dust state overlays

Import these under:

```text
Assets/Game/Art/Generated/2DISO/Units
Assets/Game/Art/Generated/2DISO/Vehicles
Assets/Game/Art/Generated/2DISO/Air
Assets/Game/Art/Generated/2DISO/Sea
Assets/Game/Art/Generated/2DISO/Buildings
Assets/Game/Art/Generated/2DISO/VFX
Assets/Game/Art/Generated/2DISO/Manifests
```

Bind them through `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`; keep stats and unlock values in the balance config.

## Step 5 - Large Unit Movement

Do not path every unit across the full grid at the same moment.

For 1,000+ units:

- issue group orders to road graph nodes/corridors
- use cached corridor paths for the group
- request fine A* only for local segment endpoints
- stagger path requests using the existing request budget
- keep vehicles on vehicle-preferred road edges
- let infantry use sidewalk/flank edges
- validate movement using the first imported iso unit/vehicle/air/sea sprites, not placeholder capsules or metadata markers

## Step 6 - Validation Scene

First validation scenes:

```text
Assets/Game/Scenes/DesignTargets/FG01_CoastalCommand_UsableIsoMap.unity
Assets/Game/Scenes/DesignTargets/FG02_PortBreach_UsableIsoMap.unity
Assets/Game/Scenes/DesignTargets/FG03_AirNavalDefense_UsableIsoMap.unity
```

Editor builder:

```text
Assets/Game/Scripts/Editor/WarlineCaptureFictionalGulfIsoMapBuilder.cs
```

Unity menu:

```text
WarlineCapture/Design/Build FG-01 Usable Iso Map
WarlineCapture/Design/Build FG-02 Usable Iso Map
WarlineCapture/Design/Build FG-03 Usable Iso Map
WarlineCapture/Design/Build All Fictional Gulf Usable Iso Maps
```

Required captures:

- clean visual target view at gameplay zoom
- empty streamed terrain at gameplay zoom
- metadata overlay view
- gameplay visual slice with first soldier, vehicle, aircraft, ship, and building-state assets
- 200-unit movement smoke
- 1,000-unit stress movement
- socket placement view

Reports should stay under:

```text
Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports
```

## Acceptance

Proceed beyond `FG-L01` production art only when:

- the visual target can be judged directly in Unity without reading metadata colors
- the clean view looks close enough to the approved reference direction for manual signoff
- all map JSON files parse
- roads visually connect between macro chunks
- road graph overlays align to visible roads
- blockers and water prevent movement correctly
- command, radar, defense, LZ, and objective sockets align to visible pads
- 1,000-unit stress mode does not freeze pathing or rendering
- resident visual chunk memory stays under the map target
