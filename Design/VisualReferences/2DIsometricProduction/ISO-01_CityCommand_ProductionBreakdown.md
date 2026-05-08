# ISO-01 City Command 2D Isometric Production Breakdown

Date: 2026-05-05

## Goal

Test whether WarlineCapture can move toward a premium 2D-isometric mobile RTS direction before committing the full game.

Primary target:

- `ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`

Current terrain note:

- The original terrain/tilemap spike remains useful as a style and import baseline.
- The active terrain production path is now large macro tiles plus metadata, not tiny modular road tiles.
- Terrain-related `GA-*` assets below are historical spike assets unless explicitly reused as overlays or references.

## Production Asset Categories

### Terrain And Tilemap

- straight road macro tile
- intersection macro tile
- T-junction macro tile
- base/plaza entrance macro tile
- damaged road/rubble overlay decals
- non-interactive terrain dressing

### Buildings

- blue forward command HQ
- red enemy command HQ
- ruined city building
- small civilian block/building
- defensive tower
- satellite/radar structure

### Military Units

- friendly rifle squad sprite
- hostile infantry squad sprite
- APC sprite
- tank sprite
- helicopter sprite
- drone sprite

### Tactical Props

- barricade/sandbag row
- capture-point marker
- supply crate stack
- street light/tree/urban dressing
- smoke/fire VFX sprite

### UI/Overlay

- objective panel shell
- unit card shell
- action button shell
- minimap shell
- cyan route arrow
- blue shield marker
- red threat marker
- health bar sprites

## Golden Asset Spike

Generate small first-pass batches before expanding the full game. The current spike generated and imported the 23 assets below:

| ID | Asset | Purpose |
|---|---|---|
| `GA-01` | Asphalt road straight tile | Tilemap alignment and road readability |
| `GA-02` | Road intersection tile | Tilemap joins and path readability |
| `GA-03` | Concrete plaza tile | Base/building footprint support |
| `GA-04` | Forward command HQ | Friendly landmark scale |
| `GA-05` | Enemy command HQ | Enemy landmark scale |
| `GA-06` | Ruined city building | Cover and destroyed-district read |
| `GA-07` | Barricade row | Sorting and wall readability |
| `GA-08` | Rifle squad | Unit readability at gameplay scale |
| `GA-09` | APC | Vehicle scale and selection readability |
| `GA-10` | Tank | Heavy vehicle scale |
| `GA-11` | Road turn tile | Modular road layout and corner readability |
| `GA-12` | Road T-junction tile | Modular road branching |
| `GA-13` | Road end/cap tile | Road termination readability |
| `GA-14` | Curb/sidewalk transition tile | Road-to-plaza transition readability |
| `GA-15` | Damaged road overlay | Terrain damage/decal overlay test |
| `GA-16` | Alternate concrete plaza tile | Repetition reduction in large paved areas |
| `GA-17` | Selection ring | Friendly selected-unit readability |
| `GA-18` | Move marker | Command destination readability |
| `GA-19` | Attack marker | Hostile command/threat readability |
| `GA-20` | Health bar frame | Unit health readability |
| `GA-21` | Health bar fill | Unit damage-state readability |
| `GA-22` | Squad badge | Group identity/readability |
| `GA-23` | Capture point marker | Objective marker readability |

Deferred follow-up assets:

| ID | Asset | Purpose |
|---|---|---|
| `GA-24` | Helicopter | Air unit readability |
| `GA-25` | Hostile rifle squad | Enemy infantry readability |

Generated asset paths:

- Transparent design references: `GoldenAssets/Transparent`
- Chroma-key source renders: `GoldenAssets/SourceChroma`
- Unity imported sprites: `Assets/Game/Art/Generated/2DISO/GoldenAssets`

## Unity Spike Acceptance

The first Unity spike should answer:

- Can the generated assets sort correctly in isometric view?
- Are units readable at expected mobile gameplay size?
- Do vehicle/building scales feel coherent?
- Can generated/imported assets create a convincing 2D isometric base layout?
- Does a dense scene remain performant when repeated into a small battlefield?

This spike is exploratory. Do not wire it into Jenkins/build validation.

## Unity Spike Result

Manual Unity spike result: PASS.

- Scene: `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`
- Capture: `UnitySpike/ISO01_TilemapSpike_Capture.png`
- Report: `UnitySpike/ISO01_TilemapSpike_Report.md`
- Tested 221 terrain tiles, modular road/plaza terrain, damaged road overlays, tactical overlays, sorted buildings/props/units/vehicles, scale/readability, and a small repeated-unit performance smoke scene.
- Terrain implementation has since moved to macro tiles plus metadata; keep this result as a baseline import/readability smoke, not the current terrain assembly model.
