# WarlineCapture Chapter 1 Tactical Production Implementation Plan

Date: 2026-05-07

## Purpose

This plan turns the accepted AI tactical-map workflow into a real production path for Saga Chapter 1.

It connects:

- Chapter 1 mission design
- AI-generated tactical ground plates
- sprite atlases
- road / walkable / blocker metadata
- current grid pathfinding
- unit selection, movement, attack, and animation systems
- Unity validation scenes

The goal is to avoid producing good-looking images that cannot run in the actual game.

## Source Of Truth

Read these documents before implementation:

- `Design/WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `Design/WarlineCapture_Level_And_Mission_Content_Plan.md`
- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `Design/WarlineCapture_Tactical_Map_AI_Workflow.md`
- `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
- `Design/BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
- `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`

## Alignment Audit

This plan was audited against the rest of the current design set on 2026-05-07.

### Gameplay Alignment

Aligned with:

- `WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `WarlineCapture_Level_And_Mission_Content_Plan.md`
- `SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`

Rules preserved:

- use `Mission -> ScenarioSetup -> Level / Map`
- do not use `Level` as a player-facing mission label
- ScenarioSetup references map/runtime art by IDs such as `LevelId`, `IsoMapId`, `MapPreviewArtId`, and `MinimapArtId`
- tactical gameplay remains simulation-first and data-driven
- spawn positions, objectives, enemy routes, command ranges, and camera defaults must be validated for 2D isometric readability
- large-scale grid movement is a staged promise: M01 proves readable select/move/attack, Chapter 1 proves distinct movement pressures, and Production Scale proves multi-squad/multi-route readability and performance
- Mission 1 is the first playable vertical slice before expanding to Missions 2-5

### Art And Asset Register Alignment

Aligned with:

- `WarlineCapture_Art_Asset_Requirements_Register.md`
- `WarlineCapture_Art_Asset_Requirements_Register.csv`
- `WarlineCapture_2D_Isometric_Production_Direction.md`
- `WarlineCapture_2D_Isometric_Art_Bible.md`
- `WarlineCapture_Tactical_Map_AI_Workflow.md`

Rules preserved:

- generated validation art is not final just because it exists
- final production assets require approval in the art register
- tactical map rows already exist for the five Chapter 1 levels
- close-up tactical maps must be authored at intended unit scale and must not be upscaled
- runtime units, vehicles, buildings, VFX, health bars, selection rings, and objective markers are separate from ground art
- terrain pixels are never gameplay truth; metadata is authoritative

The accepted close-up POT ground plate workflow is a production variant of the broader macro-tile direction. It does not revive the rejected tiny-road-tile approach. The ground plate is the visual terrain layer; metadata supplies walkability, roads, blockers, anchors, sockets, minimap data, and pathfinding inputs.

### UI/UX Alignment

Aligned with:

- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `WarlineCapture_UIUX_Implementation_Detailed_Spec.md`
- `WarlineCapture_UIUX_Screen_Popup_Implementation_Spec.md`

Rules preserved:

- Mission Briefing, Battle HUD, Command Wheel, Build Drawer, Threat Alert, Mission Result, map previews, and minimap must bind to real mission/map data
- UI maps and previews may show ground/roads/pads but must not bake interactive objectives, health, rewards, costs, or unit stats into images
- tactical markers, minimap markers, objective indicators, invalid placement feedback, command markers, selection rings, and health bars come from runtime state
- any not-yet-live UI route must use `Locked`, `DesignedUnavailable`, `DevOnly`, or `ReadOnly` states instead of silent inert controls

### Runtime System Alignment

Aligned with current code anchors:

- `GridConfig`, `GridWalkable`, `GridRoad`, `GridRoadSidewalk`, `GridRoadDirt`
- `UnitGrid`, `UnitPathRequest`, `UnitPathFollow`, `UnitTarget`
- `StaticGridBlocker`, `GridBlockerSize`, `DynamicBlockerData`, `DynamicOccupancyData`
- `UnitPathfindingSystem`
- `UnitGridMovementSystem`
- `UnitAttackSystem`
- `RTSSelectionSystem`
- `RuntimeGridBlockerSystem`

Rules preserved:

- metadata must populate existing grid buffers instead of adding a second pathfinding model
- selected move orders produce `UnitPathRequest`
- selected attack orders produce `EngageTarget` and can move the unit into range
- building attacks and move-to-building behavior rely on footprint/approach-cell metadata
- blockers are represented through existing static/dynamic blocker systems

### Open Alignment Work

These are still required before real production implementation starts:

- use `WarlineCapture_M01_FirstContact_Production_Contract.md` as the first production target
- create Chapter 1 scale contract asset/doc from the accepted validation scene
- create Chapter 1 asset manifest rows and update art-register statuses for approved/needs-review assets
- decide final production folder layout for accepted assets, likely under `Assets/Game/Art/Generated/2DISO/Chapter01/...`, while keeping `Assets/Game/Art/Generated/IsometricMaps/...` as validation/prototype history
- define `TacticalMapDefinition` schema and editor metadata authoring workflow
- assign UI owners for the missing tactical controls in `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`
- add validation tests so map metadata, ScenarioSetup IDs, UI surface IDs, and art-register IDs cannot drift silently

Runtime systems already present:

- `GridConfig`, `GridWalkable`, `GridRoad`, `GridRoadSidewalk`, `GridRoadDirt`
- `UnitGrid`, `UnitPathRequest`, `UnitPathFollow`, `UnitTarget`
- `StaticGridBlocker`, `GridBlockerSize`, `DynamicBlockerData`, `DynamicOccupancyData`
- `UnitPathfindingSystem`
- `UnitGridMovementSystem`
- `UnitAttackSystem`
- `RTSSelectionSystem`
- `RuntimeGridBlockerSystem`

## Locked Production Assumptions

Use the current approved tactical visual scale:

- tactical ground source: native AI image, no upscaling
- Unity runtime texture: padded POT, usually `2048 x 1024`
- tactical camera: close gameplay camera around `0.6` orthographic size for validation
- infantry squad visual scale anchor: `0.10`
- vehicle scale anchor: tank around `0.085`, APC around `0.095`
- large industrial buildings are larger class objects, refinery currently validated around `0.30`
- visual entities are separate sprites, not baked into map images
- ground images must not contain gameplay soldiers, vehicles, aircraft, tents, or buildings

## Chapter 1 Map Targets

Each Chapter 1 Level / Map must become a production map package.

| LevelId | Mission | Production Map Package |
|---|---|---|
| `level.ch01.district_edge_01` | M01 First Contact | `iso.ch01.district_edge_01` |
| `level.ch01.forward_post_01` | M02 Establish The Base | `iso.ch01.forward_post_01` |
| `level.ch01.convoy_approach_01` | M03 Radar Warning | `iso.ch01.convoy_approach_01` |
| `level.ch01.landing_zone_01` | M04 Airlift | `iso.ch01.landing_zone_01` |
| `level.ch01.fortified_node_01` | M05 Breach Assault | `iso.ch01.fortified_node_01` |

Each map package contains:

- POT ground texture
- source AI ground image
- terrain metadata
- spawn anchors
- objective anchors
- enemy route anchors
- build zones
- blockers
- road / sidewalk / dirt-road mask
- minimap source
- preview source
- validation scene

## Metadata Contract

Every tactical ground plate needs a matching metadata asset. The metadata is not optional.

Recommended format for authoring:

```text
Assets/Game/Data/TacticalMaps/Chapter01/<map_id>.asset
```

The data can start as a ScriptableObject and may later be exported/imported from JSON.

Required fields:

- `MapId`
- `LevelId`
- `GroundTexture`
- `PreviewArtId`
- `MinimapArtId`
- `GridWidth`
- `GridHeight`
- `CellSize`
- `WorldOrigin`
- `CameraDefaultCenter`
- `CameraOrthographicSize`
- `WalkableCells`
- `RoadCells`
- `SidewalkCells`
- `DirtRoadCells`
- `BlockedCells`
- `BuildableCells`
- `CivilianZoneCells`
- `ObjectiveZones`
- `SpawnAnchors`
- `EnemyRouteAnchors`
- `AttackTargetAnchors`
- `BuildingFootprints`
- `ValidationNotes`

The metadata must feed the existing buffers:

- `WalkableCells` -> `GridWalkable`
- `RoadCells` -> `GridRoad`
- `SidewalkCells` -> `GridRoadSidewalk`
- `DirtRoadCells` -> `GridRoadDirt`
- `BlockedCells` and `BuildingFootprints` -> `StaticGridBlocker` / `GridBlockerSize`

## Step-By-Step Implementation

### Step 0: Lock UI / Gameplay Command Contracts

Deliverables:

- use `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md` as the active UI handoff
- use `WarlineCapture_M01_FirstContact_Production_Contract.md` as the concrete M01 implementation handoff
- confirm UI element ids for selection, move, attack, invalid command feedback, build placement, minimap jump, threat jump, objective jump, and mission result
- define gameplay reason codes that UI can display for invalid actions
- confirm strategic / zoomed-out art is only preview/minimap and tactical / zoomed-in art is the real gameplay map

Validation:

- the UI agent can implement or validate every missing tactical control without needing new gameplay art generation
- gameplay code can return typed command results instead of silent success/failure
- M01 can be validated with the same select/move/attack/build/minimap/mission-result UI contracts that later missions will use

Exit criteria:

- every UI element needed by M01 has an owner in the UI phase docs
- every gameplay command needed by M01 has a matching UI feedback path
- no production gameplay work depends on a blurred strategic map image

### Step 1: Lock The Scale Contract

Deliverables:

- create `Chapter01TacticalScaleContract` doc/asset
- record approved camera size, PPU, unit scale, vehicle scale, and building class scales
- update validation builders to read these constants instead of hard-coded local values

Validation:

- infantry, tank, APC, command building, tent, and refinery appear at approved scale
- no unit needs manual rescaling in scene to look correct

Exit criteria:

- `TacticalProductionBatch_A.unity` remains the scale reference
- scale values are documented and reusable

### Step 2: Define Chapter 1 Asset IDs

Deliverables:

- create an asset manifest for Chapter 1
- list every sprite required by M01-M05
- classify each asset as unit, vehicle, building, industrial building, ground plate, VFX, decal, UI preview, or minimap

Initial required asset groups:

- infantry squad
- enemy patrol infantry
- battle tank
- APC
- transport helicopter or transport marker for M04
- command point
- barracks / tent structure
- guard tower
- road barrier
- radar / satellite dish
- gate / wall breach target
- enemy core
- fuel / industrial module
- explosion / hit / selection / destination decals

Validation:

- every generated asset maps to at least one Chapter 1 mission requirement
- no random extra asset enters runtime without a manifest ID

Exit criteria:

- Chapter 1 asset manifest can answer: "which mission needs this asset?"

### Step 3: Define Atlas Contracts

Deliverables:

- `chapter01_units_atlas`
- `chapter01_vehicles_atlas`
- `chapter01_buildings_atlas`
- `chapter01_ground_plates`
- `chapter01_vfx_decals`

Each atlas entry must define:

- `SpriteId`
- `AtlasId`
- `SourcePath`
- `Rect`
- `Pivot`
- `PixelsPerUnit`
- `DefaultScale`
- `SortingClass`
- `SelectionBounds`
- `ColliderFootprintCells`
- `GameplayClass`
- `UsedByMissionIds`

Important:

- validation sprites can stay loose during art review
- production runtime should not depend on unnamed loose files
- final sprites must be named by stable IDs, not prompt names

Validation:

- Unity imports atlases with expected compression
- all Chapter 1 sprite IDs resolve from metadata
- no scene depends on generated filename hashes

Exit criteria:

- a prefab or runtime resolver can spawn by `SpriteId`, not by a loose path

### Step 4: Build The Map Metadata Authoring Tool

Deliverables:

- Unity editor tool or scene overlay for painting metadata over the ground image
- modes:
  - walkable
  - road
  - sidewalk
  - dirt road
  - blocked
  - buildable
  - civilian zone
  - spawn anchor
  - objective zone
  - enemy route
  - attack target

The authoring view must show the ground plate plus grid overlay.

Controls:

- paint cells
- erase cells
- rectangle fill
- route waypoint placement
- footprint placement
- export/save metadata asset
- validate metadata

Validation:

- blocked cells visually match walls, large props, buildings, cliffs, closed curbs
- roads match asphalt / road surfaces
- sidewalks/walking cells match pedestrian or safe side zones
- dirt roads match off-road movement surfaces
- objective anchors are visible on the map

Exit criteria:

- a designer can mark roads, walking places, blockers, and objectives without editing code

### Step 5: Runtime Map Loader

Deliverables:

- `TacticalMapDefinition` runtime asset
- loader that creates/updates:
  - ground sprite renderer
  - `GridAuthoring` / baked `GridConfig`
  - `GridWalkable`
  - `GridRoad`
  - `GridRoadSidewalk`
  - `GridRoadDirt`
  - static blockers
  - spawn anchors
  - objective anchors

The map loader must support Chapter 1 ScenarioSetup IDs:

- `scenario.ch01.m01.first_contact`
- `scenario.ch01.m02.establish_base`
- `scenario.ch01.m03.radar_warning`
- `scenario.ch01.m04.airlift`
- `scenario.ch01.m05.breach_assault`

Validation:

- loading a map creates the same visible ground and grid every time
- no metadata is inferred from pixels at runtime
- pathfinding buffers are populated before units spawn

Exit criteria:

- ScenarioSetup can select a map package by `IsoMapId`

### Step 6: Build M01 Production Map First

Start with M01 only:

- LevelId: `level.ch01.district_edge_01`
- IsoMapId: `iso.ch01.district_edge_01`
- teaching goal: select, move, attack, objective tracker, result flow

Required map metadata:

- player spawn near command point
- enemy patrol spawn
- visible road route
- walkable road / sidewalk area
- blocker cells for walls, large props, and map edge
- objective target group anchor
- camera default center
- mission bounds

Required runtime entities:

- friendly infantry squad
- enemy patrol infantry
- optional small command point visual
- selection marker
- move marker
- attack marker

Validation:

- tap/select friendly unit
- tap walkable road or sidewalk -> unit receives `UnitPathRequest`
- unit path avoids blockers
- selected unit + tap enemy -> `EngageTarget`
- unit moves into range if needed
- attack animation state triggers
- enemy death completes objective

Exit criteria:

- M01 can be played from start to result on the new tactical map path

### Step 7: Add Movement Surface Rules

Use current buffers:

- road cells get preferred movement cost
- sidewalk cells get preferred or normal infantry movement
- dirt road cells get relevant movement cost
- blocked cells are never valid goals

Required decisions:

- infantry can walk on roads and sidewalks
- vehicles prefer roads and dirt roads
- vehicles may avoid narrow sidewalk-only cells
- buildings block all normal ground movement
- selected move target on blocked cell should resolve to nearest valid approach cell

Validation:

- road movement is visibly faster or preferred
- infantry can reach sidewalks / safe zones
- vehicles avoid inappropriate narrow zones
- blocked targets do not create stuck orders

Exit criteria:

- metadata surfaces affect path choice, not just debug color

### Step 8: Build Building And Blocker Footprints

Each building sprite needs:

- visual sprite
- gameplay footprint
- selection bounds
- attackable health target
- approach cells around footprint
- optional friendly-pass behavior

Important existing behavior:

- `RTSSelectionSystem.TryIssueMoveOrderToBuilding` searches for approach cells around building footprints
- `UnitAttackSystem` can move attacker into range before engaging
- static blockers already use `StaticGridBlocker` and `GridBlockerSize`

Validation:

- tapping enemy building issues attack order
- units path to approach cell, not into the building
- building blocks movement
- destroyed building updates blocker behavior if needed

Exit criteria:

- command building, barracks/tent, guard tower, refinery, enemy core, wall/gate all have metadata footprints

### Step 9: Connect ScenarioSetup To Map Anchors

Each ScenarioSetup must reference named anchors, not raw scene objects.

Examples:

- `player_spawn.command_squad`
- `enemy_spawn.patrol_start`
- `route.enemy_patrol_01`
- `objective.destroy_patrol_group`
- `build_zone.forward_lot`
- `attack_target.enemy_core`
- `zone.landing_zone`
- `zone.civilian_edge`

Validation:

- missing anchor fails config validation
- renamed anchor fails loudly
- mission spawn code does not depend on hand-placed Unity hierarchy names

Exit criteria:

- M01-M05 can each define starts/objectives from metadata

### Step 10: Build Production Validation Scenes

Create one validation scene per map package:

- `Chapter01_M01_TacticalValidation.unity`
- `Chapter01_M02_TacticalValidation.unity`
- `Chapter01_M03_TacticalValidation.unity`
- `Chapter01_M04_TacticalValidation.unity`
- `Chapter01_M05_TacticalValidation.unity`

Each scene must include:

- ground plate
- metadata grid overlay
- walkable/road/sidewalk/blocker debug toggles
- required units/buildings at real scale
- active close gameplay camera
- disabled full review camera
- one-click scene builder

Validation:

- screenshot of art-only view
- screenshot of metadata overlay
- play-mode movement test
- play-mode attack target test

Exit criteria:

- visual and gameplay metadata pass before mission wiring begins

### Step 11: Implement M01 End-To-End

Wire the first real playable slice only after M01 validation passes.

M01 implementation order:

1. create M01 map metadata
2. create M01 map validation scene
3. create M01 scenario runtime mapping
4. spawn friendly infantry at metadata anchor
5. spawn enemy patrol at metadata anchor
6. enemy patrol follows metadata route
7. selected friendly unit can move
8. selected friendly unit can attack enemy
9. objective completes when patrol destroyed
10. mission result shows stars/rewards

Validation:

- config sanity test
- map metadata test
- pathfinding test
- selection/move test
- attack/order test
- objective completion test
- result/reward smoke test

Exit criteria:

- M01 is playable using the production tactical-map pipeline

### Step 12: Expand Mission By Mission

Do not generate all Chapter 1 maps up front.

Order:

1. M01 First Contact
2. M02 Establish The Base
3. M03 Radar Warning
4. M04 Airlift
5. M05 Breach Assault

Only move to the next mission when the previous mission passes:

- art scale
- metadata overlay
- pathfinding
- selection
- movement
- attack
- objective
- result flow

## Acceptance Checklist

The production pipeline is ready when:

- every tactical map has POT ground texture and metadata
- every runtime sprite has stable ID and atlas entry
- roads/walkable/blockers are authored explicitly
- pathfinding consumes metadata buffers
- units can move without relying on visual pixels
- attack targets use footprints and approach cells
- Chapter 1 ScenarioSetup references map anchors by ID
- validation scenes show art and metadata together
- M01 is playable end-to-end before M02 work starts

## Immediate Next Step

Start with Step 0, Step 1, and Step 2, plus the cross-document tracking gates:

1. use `WarlineCapture_M01_FirstContact_Production_Contract.md` as the first production contract
2. use `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md` as the UI/gameplay handoff before coding M01 select, move, attack, build placement, threat jump, minimap jump, or objective jump behavior
3. create the scale contract asset/doc from the accepted `TacticalProductionBatch_A`
4. create the Chapter 1 tactical asset manifest
5. define the `TacticalMapDefinition` metadata schema
6. build the first metadata authoring overlay for `iso.ch01.district_edge_01`
7. update `WarlineCapture_Art_Asset_Requirements_Register.csv` rows for the accepted validation assets as `exists_needs_review`, not `complete`
8. add or confirm UI element contracts for M01 Mission Briefing preview, Battle HUD minimap/objective rows, selected-unit panel, command markers, and Mission Result
9. confirm M01 ScenarioSetup references only stable IDs from the Chapter 1 mission doc, combat balance config, visual config, and map metadata

No more broad art generation should happen until this contract layer exists.
