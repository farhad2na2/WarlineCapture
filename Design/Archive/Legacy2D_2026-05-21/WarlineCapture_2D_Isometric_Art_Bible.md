# WarlineCapture 2D Isometric Art Bible

Date: 2026-05-05
Status: Historical reference. Superseded on 2026-05-21 by `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`.

## Superseded Direction Notice

This art bible is no longer the active production art target. The active direction is full 3D single-map mobile RTS with runtime units, civilians, vehicles, aircraft, buildings, VFX, markers, and metadata-backed command overlays. Retain this file only for historical lessons and terminology migration.

## Purpose

This document preserves the historical production art rules for WarlineCapture's former premium 2D isometric mobile RTS direction.

The former terrain approach was large authored macro tiles with separate gameplay metadata. The retained lesson is that gameplay truth must remain data-driven.

## Source References

- Production direction: `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- Macro-tile production plan: `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
- Implementation and validation plan: `Design/WarlineCapture_2D_Isometric_Implementation_Validation_Plan.md`
- Strategic/tactical map alignment: `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- Visual reference index: `Design/VisualReferences/README.md`
- ISO-01 production target: `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
- ISO-04 terrain visual reference: `Design/VisualReferences/2DIsometricProduction/TerrainVisualTarget/ISO04_TerrainVisualTarget.png`
- ISO-01 Unity spike report: `Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md`
- ISO-02 runtime prototype report: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Report.md`

## Direction Summary

The former 2D target was a premium isometric military RTS: tactical, clean, readable, and visually richer than the earlier prototype.

Terrain quality should come from authored 2.5D macro art, not from tiny road-tile assembly. Gameplay correctness should come from metadata, not from reading pixels.

Chapter 1 2.5D macro-tile references live at `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/Chapter01_2_5DReferences`.

Strategic/zoomed-out art and tactical/zoomed-in art are separate deliverables. Strategic art can be wider and contextual for previews/minimaps; tactical art must be validated at playable unit scale with runtime sprites and overlays on top.

## Layer Model

Use these layers:

1. terrain macro tile sprites
2. runtime terrain decals where needed
3. gameplay buildings and destructible props
4. units and vehicles
5. selection, health, command, objective, and threat overlays
6. HUD Canvas

Do not bake UI, selection markers, health bars, objectives, player-owned buildings, destructible buildings, resources, or units into terrain.

## Macro-Tile Art Rules

Macro tiles should be large enough to preserve cohesive AI/authored scene quality.

Recommended initial source size:

- `2048 x 2048` per macro tile
- fixed isometric camera angle
- fixed lighting direction
- baked 2.5D height through curbs, walls, pads, ramps, shadows, and surface thickness
- fixed connector positions on tile edges
- consistent road width and curb height
- no text or UI
- no gameplay-state objects baked in

Macro tile art may include:

- roads
- curbs
- plazas
- terrain wear
- dirt/concrete/grass
- empty pads/foundations
- non-interactive dressing

Macro tile art must not include:

- destructible gameplay buildings
- selectable units
- turrets
- resource nodes if they are interactive
- health bars
- objective icons
- command markers
- minimap/UI graphics

## Connector Rules

Each macro tile type must define edge connectors before art generation:

- connector side
- connector center position
- connector width
- road direction
- terrain edge treatment
- compatible connector types

The visible art must match the metadata connector. If the art and metadata disagree, metadata wins and the art must be corrected.

## Metadata Rules

Each macro tile needs metadata for:

- walkable polygons
- blocker polygons
- road graph nodes and edges
- building sockets
- defense sockets
- resource sockets
- spawn sockets
- objective sockets
- minimap mask/color
- camera bounds contribution

Metadata should be authored or generated alongside the art and inspected in an editor-only overlay.

## Building Art Rules

Gameplay buildings are separate runtime sprites/entities.

Terrain may show the pad/foundation. Runtime places the building on top.

Building state art must support:

- locked/unavailable
- construction
- intact
- upgraded/tier variants where the upgrade changes silhouette or readability
- damaged
- heavily damaged
- destroyed/rubble

Destroyed visuals can include separate rubble/scorch decals, but the terrain macro tile itself should not change.

Decorative baked buildings are allowed only if they are not selectable, destructible, productive, or gameplay-blocking without separate metadata.

Building visual ids, world asset paths, UI icon paths, and damage-state art paths must be authored in `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`. Building HP, costs, outputs, production lists, socket type, and upgrade stats stay in `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`.

## Unit And Vehicle Rules

Units and vehicles remain separate sprites/entities above terrain.

Requirements:

- strong silhouette differences between infantry, APC, tank, air, and support units
- faction accents without recoloring whole units
- sorting by world/cell position
- readability at mobile gameplay zoom
- selection/health overlays separate from unit sprites
- animation state sets for idle, move, attack/ability, hit/damaged, death/destroyed, and unit-specific states such as land/takeoff/hover, turret fire, boat wake, and construction/deploy
- consistent pivots, contact shadows, scale, and facing rules across soldiers, vehicles, aircraft, ships, and buildings

Unit, vehicle, air, and sea visual ids, portraits, icons, animation ids, and art briefs must be authored in `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`. Combat stats, production costs, unlock gates, transport capacity, and upgrade tracks stay in `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`.

## Gameplay Asset Production Lane

Gameplay assets are requested after the `FG-L01` visual target is approved and before movement/combat validation. This avoids validating units and buildings against placeholder terrain, but also avoids waiting until all maps are complete.

First production batch:

1. infantry: rifle soldier, rocket soldier, civilian
2. vehicles: APC, tank
3. air: transport helicopter, attack helicopter
4. sea: patrol boat
5. buildings: command post, barracks, helipad, guard tower
6. shared state overlays: construction scaffold, rubble, scorch, smoke, fire, muzzle flash, missile trail, rotor dust, boat wake

Each world asset request must include:

- target visual id and entity id
- gameplay role and silhouette priority
- faction accent rules
- required states and animations
- required facings or rotation strategy
- socket/footprint size for buildings
- sprite sheet or frame sequence naming convention
- pivot/contact-shadow requirement
- UI icon and portrait requirement when applicable
- explicit avoid list: no UI text, no flags, no real insignia, no readable political text, no health bars, no selection rings

Import targets:

- `Assets/Game/Art/Generated/2DISO/Units/`
- `Assets/Game/Art/Generated/2DISO/Vehicles/`
- `Assets/Game/Art/Generated/2DISO/Air/`
- `Assets/Game/Art/Generated/2DISO/Sea/`
- `Assets/Game/Art/Generated/2DISO/Buildings/`
- `Assets/Game/Art/Generated/2DISO/VFX/`
- `Assets/Game/Art/Generated/2DISO/Manifests/`

No gameplay asset is accepted until it can be previewed on the `FG-L01` visual target at gameplay zoom.

## UI Render Asset Production Lane

UI render assets are separate from world sprites. They must match the UI target guides and visual-lock screens while using the same WarlineCapture premium isometric identity.

UI render asset types:

- square unit and character portraits
- vehicle, aircraft, ship, and building thumbnails
- mode-card artwork
- mission briefing and district/key art
- reward unlock hero renders
- building/unit unlock renders
- ability icons, upgrade icons, tier badges, reward icons
- minimap and map-preview content art

UI render assets must not include:

- Canvas frames or chrome
- baked TMP text
- reward amounts, costs, stats, HP, cooldowns, or unlock labels
- selection rings, health bars, badge overlays, lock icons, or button states unless the asset is specifically an icon/badge layer

UI render assets must be composed for their target surface:

- portraits: square bust or hero-object crop, readable at squad-card size
- thumbnails: object-centered, clean silhouette, enough padding for frame masks
- mode-card art: landscape crop with clear first-read subject and no UI text
- mission art: cinematic map/unit context but no gameplay overlays or objective text
- map preview art: strategic context and mission layout readability, not a substitute for tactical gameplay ground
- minimap art: simplified navigation readability with marker/viewport contrast, not a screenshot-only crop unless it remains clear in HUD scale
- reward/unlock art: object or unit render on a UI-compatible background, with transparent variant when needed

Import targets:

- `Assets/Game/Textures/Generated/Portraits/`
- `Assets/Game/Art/UI/Generated/2DISO/Portraits/`
- `Assets/Game/Art/UI/Generated/2DISO/Thumbnails/`
- `Assets/Game/Art/UI/Generated/2DISO/ModeCards/`
- `Assets/Game/Art/UI/Generated/2DISO/MissionArt/`
- `Assets/Game/Art/UI/Generated/2DISO/Unlocks/`
- `Assets/Game/Art/UI/Generated/2DISO/Icons/`
- `Assets/Game/Art/UI/Generated/2DISO/Manifests/`

Portraits must follow `Design/WarlineCapture_Unit_Portrait_Art_Generation_Guide.md`. Screen content art must follow `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md` and `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`.

## UI And Gameplay Overlay Rules

All tactical overlays remain separate:

- selection rings
- move/attack markers
- health bars
- squad badges
- capture markers
- objective markers
- threat warning markers

No overlay may be baked into terrain or unit art.

## Color And Lighting

Use:

- warm directional key light
- readable cool/dark side
- crisp edges
- controlled surface noise
- faction accents: friendly blue/cyan, enemy red, neutral muted white/yellow/green

Avoid:

- chroma contamination
- heavy black crush
- one-note color palettes
- inconsistent camera angles
- inconsistent shadows between macro tiles

## Import Rules

Initial validation import settings:

- Texture Type: `Sprite`
- Sprite Mode: `Single`
- Alpha Is Transparency: `true` when needed
- Mip Maps: off for validation
- Filter Mode: `Bilinear`
- Compression: uncompressed during visual validation
- Production compression: decide after mobile memory tests

Large macro tiles should later use mobile texture compression and streaming/culling.

## Failed Direction Removed

Do not return to:

- one-diamond road tiles
- independently generated road chunks
- chroma-key full-road overlays as production implementation art
- procedural graph roads as final premium art

Those attempts proved the production rule: solve topology with metadata and solve visual quality with authored macro art.

## Current Next Art Task

Complete the `FG-L01` visual target approval, then request two parallel vertical slices:

1. terrain slice: straight road, T-junction, intersection, command plaza macro tiles
2. gameplay slice: rifle soldier, APC, transport helicopter, patrol boat, command post states
3. UI render slice: rifle soldier portrait, APC thumbnail, transport helicopter thumbnail, command post unlock render, FG-L01 mission key art, Saga/Persistent/Quick mode-card art

The terrain slice must have fixed edge connectors and matching metadata before generating variants. The gameplay slice must have manifests and Unity import paths before runtime binding.
The UI render slice must have target surface mappings and must be validated inside rendered Canvas captures, not only as standalone PNGs.
