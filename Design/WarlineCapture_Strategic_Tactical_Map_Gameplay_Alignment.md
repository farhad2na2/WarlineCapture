# WarlineCapture Strategic And Tactical Map Gameplay Alignment

Date: 2026-05-07

## Purpose

This is the shared contract for how WarlineCapture uses strategic map views and tactical gameplay maps.

Use this document when updating:

- mission and level specs
- ScenarioSetup data
- FTUE and ARIA tutorial steps
- UI targets and HUD overlays
- audio events
- VFX and feedback assets
- art asset checklist rows
- map metadata and validation scenes

## Core Decision

WarlineCapture uses two map views with different jobs.

| Map View | Purpose | Player Sees It In | Data / Art IDs |
|---|---|---|---|
| Strategic / zoomed-out map | Mission choice, briefing context, minimap, threat route preview, objective jump context, Operation/Saga overview. | `SCN-05 Saga Map`, `SCN-06 Mission Briefing`, `SCN-08 minimap`, `POP-01 Threat Alert`, Operation screens. | `MapPreviewArtId`, `MinimapArtId`, district map ids, route preview ids. |
| Tactical / zoomed-in map | Real combat play where unit scale, camera, selection, movement, attack, build placement, objectives, VFX, and audio are validated. | Match scene, `SCN-08 Battle HUD`, Chapter tactical validation scenes. | `IsoMapId`, `TacticalMapDefinition`, POT ground plate, metadata asset. |

Do not use one large strategic image as the tactical gameplay map. Strategic art can look wider and more cinematic. Tactical art must be authored at the close-up unit scale, native resolution, with no upscaling and no baked gameplay entities.

## Required IDs

Every mission that launches tactical gameplay must resolve this chain:

```text
MissionId -> ScenarioSetupId -> LevelId -> IsoMapId -> TacticalMapDefinition
```

Every tactical mission also needs:

- `MapPreviewArtId` for Saga/Briefing/Quick Custom preview
- `MinimapArtId` for Battle HUD and threat/objective camera jumps
- named metadata anchors for spawns, routes, objectives, attack targets, build zones, camera bounds, and civilian zones

## Tactical Map Production Rules

Tactical gameplay maps must follow `WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md` and `WarlineCapture_Tactical_Map_AI_Workflow.md`.

For the first playable slice, use `WarlineCapture_M01_FirstContact_Production_Contract.md` as the concrete mission/map contract.

Rules:

- source ground image is native AI/generated output, not an upscale
- runtime ground texture is padded POT when needed, not stretched
- units, vehicles, buildings, aircraft, VFX, objective markers, health bars, selection rings, command markers, and UI overlays are separate runtime sprites/entities
- ground art can show roads, curbs, terrain wear, empty pads, plaza markings, and non-interactive dressing
- ground art must not bake player units, enemy units, interactive buildings, target icons, health, rewards, costs, or command feedback
- metadata is gameplay truth; terrain pixels are not gameplay truth

## Metadata Contract

Each tactical map package must include a `TacticalMapDefinition` or equivalent metadata asset with:

- walkable cells / polygons
- road cells / graph
- sidewalk or infantry-safe cells
- dirt/off-road cells where relevant
- blocked cells / polygons
- buildable cells, pads, sockets, and invalid zones
- spawn anchors
- route anchors
- objective anchors
- attack target anchors
- civilian zones
- camera bounds and default camera
- minimap mapping and viewport scale

The metadata feeds current runtime systems:

- pathfinding / movement
- attack approach cells
- build placement validation
- minimap camera jumps
- threat route jumps
- objective focus jumps
- FTUE highlights
- VFX/world marker anchors
- audio emitter positions

## Mission And Level Rules

Each mission spec must state:

- `MissionId`
- `ScenarioSetupId`
- `LevelId`
- `IsoMapId`
- `MapPreviewArtId`
- `MinimapArtId`
- required tactical anchors
- required UI surfaces
- required tutorial / assistant hooks when teaching a mechanic
- tactical validation scene name when the map enters production

Each level/map spec must state:

- what the strategic preview should communicate
- what the tactical close-up map must contain at playable scale
- which roads, sidewalks, blockers, build zones, objective zones, and routes need metadata
- which runtime units/buildings/VFX are intentionally not baked into the ground

## FTUE And ARIA Rules

FTUE must teach the difference implicitly:

- Saga Map and Mission Briefing are strategic planning views.
- Battle HUD is the tactical command view.
- ARIA highlights tactical targets using metadata anchors, not screen coordinates and not baked image details.
- ARIA can demonstrate select, move, attack, build placement, threat jump, minimap jump, and objective jump only through typed command intents.
- Every ARIA world highlight must resolve to a unit, building, metadata anchor, map zone, or UI element id.

## UI Rules

UI work follows `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`.

Required tactical UI feedback:

- selected entity panel
- command mode banner
- move destination marker
- attack target marker
- invalid command marker / toast
- build footprint overlay
- minimap viewport and jump feedback
- threat and objective camera jump feedback

Strategic map UI must not imply that the preview image is the exact combat-scale terrain. It should preview mission context, district consequence, routes, objectives, and risk.

## Audio Rules

Strategic and tactical map moments use different audio layers:

- strategic map / briefing: restrained command-room UI, city ambience, mission selection pings, route previews
- tactical map: unit selection, command confirms, invalid target rejects, combat, build placement, objective updates, threat warnings
- minimap/threat/objective jumps: short UI-to-world focus cue plus visual camera feedback
- ARIA tutorial prompts: voice or text cues must duck music/ambience and never be the only instruction source

Audio events must pair with visible feedback and should use world/map anchor positions when the event is tactical.

## VFX Rules

VFX and feedback must distinguish:

- strategic overlays: route lines, district warning pulses, mission node states, minimap pings
- tactical overlays: selection rings, path pips, target reticles, blocked-cell markers, build footprints, objective markers, impact VFX

Tactical VFX attach to runtime entities or metadata anchors. They are never baked into tactical ground art.

## Asset Checklist Rules

An art asset row is not complete unless it is approved at the correct view:

- strategic preview/key art: approved in UI surface context
- minimap art: approved in HUD/minimap context with marker readability
- tactical ground plate: approved at tactical camera scale with runtime units/buildings/VFX over it
- tactical metadata: validated through overlay and path/build/attack tests
- runtime sprites: approved over the tactical ground plate at final scale

For tactical maps, a ground image without matching metadata and validation scene is at most `exists_needs_review`, never `complete`.

## Validation Gate

A mission is production-ready only when all of this passes:

- ScenarioSetup resolves `LevelId`, `IsoMapId`, `MapPreviewArtId`, and `MinimapArtId`
- tactical map package has ground art, metadata, minimap, preview, and validation scene
- UI surfaces display strategic preview/minimap/tactical HUD correctly
- FTUE hooks resolve to typed UI/world/metadata targets
- audio and VFX event ids exist for select, move, attack, invalid command, threat jump, objective update, and build placement where used
- tactical validation scene proves selection, movement, attack, build placement when allowed, minimap jump, objective jump, and result flow
