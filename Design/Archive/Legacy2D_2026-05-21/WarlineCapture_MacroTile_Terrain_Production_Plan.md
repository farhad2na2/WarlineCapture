# WarlineCapture Macro-Tile Terrain Production Plan

Date: 2026-05-05
Status: Historical reference. Superseded on 2026-05-21 by `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`.

## Superseded Direction Notice

This macro-tile plan is no longer the active battlefield production path. WarlineCapture's active target is full 3D single-map mobile RTS. Do not start new 2.5D macro-tile terrain work from this plan unless PM explicitly reopens that direction.

## Decision

The former decision was to use large authored 2D isometric terrain macro tiles for the battlefield ground layer.

Gameplay objects remain separate runtime entities. Metadata drives gameplay. The terrain art provides visual quality.

This plan covers the tactical/zoomed-in playable map lane in `WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`. Strategic previews and minimaps are separate outputs that reference the same mission/map ids but are approved in UI/navigation context.

The preferred visual treatment is premium 2.5D isometric: macro tiles should have baked height, curb depth, wall depth, shadows, and material detail close to the high-quality references, while still behaving as terrain sprites plus metadata in Unity.

## Why This Direction

The previous road experiments showed a clear boundary:

- full AI references look good as complete compositions
- independent AI road chunks do not connect reliably
- procedural roads connect but do not match the premium references

Macro tiles keep the strength of full-scene art while keeping memory and map authoring manageable.

Chapter 1 2.5D visual references are stored at:

```text
Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/Chapter01_2_5DReferences
```

The first fictional Gulf large-map metadata package is stored at:

```text
Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps
```

## What Gets Baked

Baked into macro tile art:

- roads
- curbs
- plazas
- dirt/concrete/grass/terrain texture
- stains, cracks, road wear, decorative markings
- non-interactive visual dressing
- empty foundations/pads

Not baked:

- gameplay buildings
- units
- turrets
- resources
- destructible cover
- objectives
- health bars
- selection rings
- UI/HUD/minimap markers
- VFX

## Metadata Per Macro Tile

Each macro tile needs:

- `MacroTileId`
- art sprite path
- source/reference path
- tile size in pixels and world units
- edge connector definitions
- walkable polygons
- blocker polygons
- road graph nodes
- road graph edges
- building sockets
- defense sockets
- resource sockets
- spawn sockets
- objective sockets
- minimap mask/color data
- camera bounds contribution

## Building Placement

Use sockets/pads, not arbitrary visual placement.

Production buildings, defenses, and resource structures snap to approved sockets exposed by macro tile metadata. Temporary deployables can use constrained zones later if needed.

The existing gameplay rules remain, but the presentation rule changes:

- terrain art shows pads/foundations
- runtime building entity occupies the pad
- invalid placement is rejected by socket/zone metadata

## Building Destruction

Do not bake destructible gameplay buildings into terrain.

Runtime destruction flow:

1. Place intact building sprite/entity on a metadata socket.
2. Damage swaps runtime visual state:
   - intact
   - damaged
   - heavily damaged
   - destroyed/rubble
3. Spawn smoke/fire/dust VFX.
4. Update gameplay state.
5. Update blocker/pathfinding state if rubble blocks movement.
6. Optionally add a scorch/rubble decal above terrain.

Baked buildings are decorative only and cannot be selected, damaged, produced from, or used as gameplay truth.

## Memory Strategy

Start with `2048 x 2048` macro tiles.

Use:

- mobile texture compression
- sprite atlasing where it helps
- streaming/culling for off-screen chunks
- limited visible tile set around camera
- variants for repetition control

Avoid loading many `4096 x 4096` chunks at once until mobile memory is measured.

## Repetition Control

Avoid tiny tile repetition by using large macro tiles and variants:

- straight road A/B/C
- T-junction A/B/C
- intersection A/B/C
- base entrance A/B
- empty plaza A/B/C
- ruined district A/B
- supply yard A/B
- defensive choke A/B

Use decals/props/VFX as runtime overlays to vary repeated macro tiles.

## Step-By-Step Implementation

1. Define `MacroTileDefinition` data shape.
2. Define connector templates and naming.
3. Create the first two tile specs:
   - straight road
   - intersection
4. Generate/author the first two macro tile images.
5. Import the images into Unity as terrain sprites.
6. Create metadata assets for walkable zones, road graph, and sockets.
7. Build a 2-tile connection test scene.
8. Capture and compare at gameplay zoom.
9. Add T-junction and base/plaza entrance tiles.
10. Build a 2x2 or 3x3 scene.
11. Add editor-only metadata overlay view.
12. Feed metadata into a pathfinding/grid adapter.
13. Add socket-based building placement prototype.
14. Add runtime destructible building state swap on sockets.
15. Add HUD/minimap validation capture.
16. Measure texture memory and visible chunk count.
17. Generate variants only after the first four tiles pass.

## Acceptance Gate

Proceed to gameplay integration only when:

- macro tile edges connect cleanly
- road graph metadata aligns with visible roads
- building sockets align with visible pads
- existing movement/pathfinding can consume metadata
- buildings remain separate runtime entities
- no destructive gameplay object is baked into terrain
- memory target is plausible on mobile
