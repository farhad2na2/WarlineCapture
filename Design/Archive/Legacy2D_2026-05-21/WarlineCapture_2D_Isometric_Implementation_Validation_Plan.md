# WarlineCapture 2D Isometric Implementation And Validation Plan

Date: 2026-05-05
Status: Historical reference. Superseded on 2026-05-21 by `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`.

## Superseded Direction Notice

This implementation plan is no longer the active validation path. Future implementation planning should validate large 3D operation maps, many units, 3D camera states, prefab-catalog unit/building usage, and mobile performance budgets instead of 2D isometric macro tiles.

## Purpose

This plan preserves the former implementation path for the 2D isometric battlefield direction.

The former path was large authored terrain macro tiles plus deterministic gameplay metadata. The retained lesson is that visual changes must not break the existing RTS simulation, pathfinding, building placement, combat, HUD, or mission systems.

## Source References

- Production direction: `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
- Macro-tile production plan: `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
- Art bible: `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
- Visual reference index: `Design/VisualReferences/README.md`
- ISO-01 production target: `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
- ISO-01 Unity spike report: `Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md`
- ISO-02 runtime prototype report: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Report.md`
- ISO-04 terrain visual reference: `Design/VisualReferences/2DIsometricProduction/TerrainVisualTarget/ISO04_TerrainVisualTarget.png`
- Visual feedback and VFX recommendations: `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md`
- Strategic/tactical map gameplay alignment: `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`

## Core Rules

- Do not build the terrain from tiny road tiles.
- Do not compose independently generated road chunks.
- Do not use chroma-key generated road overlays as production terrain assets.
- Do not bake gameplay buildings or destructible gameplay objects into terrain art.
- Keep the existing ECS/grid/pathfinding gameplay truth separate from the terrain image.
- Use macro-tile metadata for walkability, blockers, road graph, sockets, spawns, objectives, minimap, and camera bounds.
- Keep strategic preview/minimap validation separate from tactical gameplay validation. A good strategic image does not validate combat-scale map quality.
- Visual validation must happen before map-scale expansion. A scene that only shows metadata overlays or colored placeholder chunks is not visually accepted.
- Every visual milestone must provide a clean terrain/gameplay-style capture that can be judged against the reference images, plus a metadata overlay capture for alignment.
- Terrain, buildings, units, vehicles, air units, ships, VFX, and tactical overlays are separate asset lanes. Do not wait until the whole map is built to request gameplay art.
- Tactical overlays and VFX should follow `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md` for move/attack markers, invalid target feedback, scan effects, reward/resource feedback, and critical combat warnings.
- UI renders, portraits, mode-card art, mission key art, thumbnails, unlock art, reward art, and minimap/district preview art are a separate UI asset lane from world sprites.
- Gameplay entity art enters the project after `FG-L01` visual direction is accepted and before the first movement/combat compatibility probe.
- UI render assets enter the project alongside the gameplay vertical slice so Canvas surfaces do not continue using placeholder or mismatched content art.
- Keep these visual/balancing probes manual. Do not wire them into Jenkins/build validation.

## Phase 0 - Baseline Reference Lock

Status: keep.

Implementation:

1. Keep ISO-01 as the general 2D isometric battlefield direction reference.
2. Keep ISO-02 as the isolated runtime movement/sorting prototype.
3. Keep ISO-04 as a terrain-quality reference.
4. Do not use the removed ISO-03 and ISO-05 through ISO-11 experiments as active implementation guidance.

Validation:

1. Confirm active visual references are listed in `Design/VisualReferences/README.md`.
2. Confirm removed experiments are not referenced by active docs.

## Phase 1 - Macro-Tile Schema

Implementation:

1. Define `MacroTileDefinition`.
2. Define a stable `MacroTileId`.
3. Define fixed chunk size, starting with `2048 x 2048` source art.
4. Define tile edge connectors:
   - north-east road exit
   - north-west road exit
   - south-east road exit
   - south-west road exit
   - no-exit/filler edge
5. Define metadata:
   - walkable polygons
   - blocker polygons
   - road graph nodes
   - road graph edges
   - building sockets
   - spawn sockets
   - objective sockets
   - minimap color/mask
   - preview crop

Validation:

1. Metadata can represent a straight road, T-junction, intersection, and base entrance.
2. Metadata can generate or feed the existing pathfinding grid.
3. Building sockets can map to the existing `BuildingPlacementSystem` without arbitrary free placement.

## Phase 2 - First Four Macro Tiles

Implementation:

Create a visual vertical slice for `FG-L01_CoastalCommand` before scaling to other maps. Use the existing full-map preview as the eye-test target, then replace it with authored macro tiles in this order:

1. straight road block
2. T-junction block
3. intersection block
4. base/plaza entrance block

Each output needs:

- source art
- Unity imported sprite
- metadata asset
- preview image
- minimap source
- manual validation notes

Validation:

1. The clean capture looks close enough to the approved premium isometric references for manual review.
2. Road exits align at macro-tile boundaries.
3. Terrain scale, road width, plaza size, waterfront edge, and building density match the `FG-L01` preview direction.
4. No visible text, UI, objectives, health bars, selection rings, or gameplay buildings are baked into the final terrain chunks.
5. Decorative baked objects are clearly non-interactive.

## Phase 2A - Visual Validation Gate

Status: active next step.

Implementation:

1. Build `FG-L01_CoastalCommand` as the first visual-validation scene.
2. Include the full-map preview image in the Unity scene as a visual target plane.
3. Add a clean visual capture path that can be compared by eye against the reference image.
4. Keep metadata overlays available as a separate alignment/debug view.
5. Replace preview-plane validation with real macro-tile sprites only after the visual target is approved.

Validation:

1. The scene can be opened and immediately judged visually, without interpreting metadata colors.
2. The visual target reads as WarlineCapture's fictional Gulf premium isometric battlefield direction.
3. The scene clearly distinguishes visual target, placeholder chunks, and gameplay metadata.
4. Do not proceed to FG-L02/FG-L03 production art until FG-L01 passes this eye test.

## Phase 2B - Gameplay Asset Request Gate

Status: starts immediately after `FG-L01` visual target approval.

Purpose:

The project needs production art for runtime gameplay objects, not only terrain. Request these assets as soon as the visual direction is approved, because movement/combat validation needs units and buildings that match the map style.

First request batch:

1. Infantry vertical slice:
   - rifle soldier idle/walk/run/aim/fire/death
   - rocket soldier idle/walk/aim/fire/death
   - civilian idle/walk/panic/death or downed state
2. Vehicle vertical slice:
   - APC idle/move/fire/damaged/destroyed
   - tank idle/move/turret-fire/damaged/destroyed
3. Air vertical slice:
   - transport helicopter idle hover/move/land/takeoff/rotor states/damaged/destroyed
   - attack helicopter hover/move/fire/damaged/destroyed
4. Sea vertical slice:
   - patrol boat move/turn/fire/damaged/destroyed
5. Building vertical slice:
   - command post locked/construction/intact/upgraded/damaged/heavily damaged/destroyed
   - barracks locked/construction/intact/upgraded/damaged/destroyed
   - helipad locked/construction/intact/upgraded/damaged/destroyed
   - guard tower locked/construction/intact/upgraded/damaged/destroyed
6. Shared overlays and effects:
   - construction scaffold/crane/decal set
   - rubble/scorch/fire/smoke state decals
   - muzzle flashes, hit sparks, missile trails, rotor dust, boat wake

Project import target:

- `Assets/Game/Art/Generated/2DISO/Units/`
- `Assets/Game/Art/Generated/2DISO/Vehicles/`
- `Assets/Game/Art/Generated/2DISO/Air/`
- `Assets/Game/Art/Generated/2DISO/Sea/`
- `Assets/Game/Art/Generated/2DISO/Buildings/`
- `Assets/Game/Art/Generated/2DISO/VFX/`
- `Assets/Game/Art/Generated/2DISO/Manifests/`

Validation:

1. Each asset has a manifest entry linking visual id, source prompt/brief, sprite path, animation clips, state names, footprint/socket size, and expected sorting pivot.
2. Unit and vehicle sprites read clearly at mobile gameplay zoom on the `FG-L01` target.
3. Building states align to sockets and do not bake gameplay stats, UI, or health values into the art.
4. Destroyed/construction/upgrade visuals are swappable runtime layers, not changes to the terrain macro tile.
5. Assets are added to `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json` before runtime binding.

## Phase 2C - UI Render Asset Request Gate

Status: starts with Phase 2B after `FG-L01` visual target approval.

Purpose:

WarlineCapture also needs high-quality UI content art that matches the visual-lock targets and UI target guides. These assets are not the same as world sprites. They are rendered portraits, thumbnails, card art, mission art, reward/unlock art, and cropped preview art used inside Canvas frames.

First request batch:

1. Unit portraits:
   - rifle soldier portrait
   - rocket soldier portrait
   - civilian portrait
   - pilot/aircrew portrait
2. Vehicle and air thumbnails:
   - APC card thumbnail
   - tank card thumbnail
   - transport helicopter card thumbnail
   - attack helicopter card thumbnail
   - patrol boat card thumbnail
3. Building thumbnails and unlock renders:
   - command post
   - barracks
   - helipad
   - guard tower
   - locked/unavailable building render treatment
4. Screen content art:
   - Saga mode card image
   - Persistent Operation mode card image
   - Quick Custom mode card image
   - mission briefing key art for `FG-L01`
   - district/raid thumbnail for port/coastal map content
   - reward unlock hero art for first unit/building unlock
5. UI icons and badges:
   - ability icons for the first gameplay slice
   - upgrade tier badges for the first unit/building/vehicle tracks
   - resource/reward icons needed by the target screens

Project import target:

- `Assets/Game/Textures/Generated/Portraits/`
- `Assets/Game/Art/UI/Generated/2DISO/Portraits/`
- `Assets/Game/Art/UI/Generated/2DISO/Thumbnails/`
- `Assets/Game/Art/UI/Generated/2DISO/ModeCards/`
- `Assets/Game/Art/UI/Generated/2DISO/MissionArt/`
- `Assets/Game/Art/UI/Generated/2DISO/Unlocks/`
- `Assets/Game/Art/UI/Generated/2DISO/Icons/`
- `Assets/Game/Art/UI/Generated/2DISO/Manifests/`

Rules:

1. Follow `Design/WarlineCapture_Unit_Portrait_Art_Generation_Guide.md` for portraits.
2. Follow `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md` and `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md` for target-to-canvas content layers.
3. UI content art must be separate from frames, chrome, TMP text, badges, counters, health bars, lock icons, and button states.
4. Do not bake gameplay values, costs, cooldowns, HP, unlock text, reward amounts, or labels into generated UI art.
5. Each UI render asset must map to a `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json` id or a screen-specific `MapPreviewArtId`/content art id.

Validation:

1. Portraits read clearly in squad trays, reward unlocks, profile cards, and unit cards at target UI size.
2. Thumbnails fit target frame crops without stretching, opaque gutters, baked borders, or duplicated icons.
3. Mission/mode/key art matches the premium isometric direction and the target screen content contract.
4. Target-vs-capture comparisons prove the UI art integrates with the visual-lock frame/chrome layers.

## Phase 3 - Unity Assembly Scene

Implementation:

1. Build a 2x2 or 3x3 macro-tile assembly scene.
2. Place macro tile sprites in an isometric world plane.
3. Load and display metadata overlays in editor-only mode:
   - road graph nodes
   - road graph edges
   - sockets
   - blockers
   - walkable zones
4. Capture a clean view without metadata overlays.
5. Write a manual report under `Design/VisualReferences/2DIsometricProduction/MacroTilePrototype`.

Validation:

1. Macro tiles visually connect at edges.
2. Repetition is not obvious at normal gameplay zoom.
3. Metadata aligns with visible roads, pads, and blockers.
4. The scene remains independent from Jenkins/build validation.

## Phase 4 - Gameplay Compatibility Probe

Implementation:

1. Add a prototype adapter that maps macro-tile metadata to the existing grid/pathfinding layer.
2. Spawn units on metadata-defined spawn points.
3. Place buildings only on metadata-defined sockets.
4. Route units along metadata road graph/walkable zones.
5. Bind the Phase 2B visual asset batch to the spawned unit/building prototypes.
6. Keep buildings, units, objectives, VFX, selection, health, and UI as runtime layers.

Validation:

1. Existing unit movement works.
2. Existing selection and combat still work.
3. Building placement rejects non-socket areas.
4. Destructible buildings swap runtime visual states without modifying the baked terrain.
5. Pathfinding does not use terrain pixels as truth.

## Phase 5 - HUD And Minimap Probe

Implementation:

1. Compose a macro-tile battlefield behind SCN-08 Battle HUD.
2. Generate minimap from metadata, not from a screenshot.
3. Verify objective markers, squad markers, threat warnings, and build placement feedback.

Validation:

1. HUD does not hide critical roads/sockets/objectives.
2. Minimap roads/objectives match metadata.
3. Build placement popup/drawer communicates sockets clearly.

## Phase 6 - Production Expansion

Implementation:

1. Add variants only after the first four macro tiles pass.
2. Add 2-3 variants per common tile type.
3. Add biome/color-grade variants later.
4. Add performance checks for texture memory, streaming, and mobile compression.

Validation:

1. 2x2 and 3x3 assemblies remain coherent.
2. Texture memory is measured with compressed import settings.
3. Streaming/unloading strategy is tested before campaign-scale maps.

## Immediate Next Step

Reach visual validation for `FG-L01`:

1. Put the `FG-L01` visual preview into the Unity scene as the eye-test target.
2. Generate a clean capture and metadata-overlay capture from Unity.
3. Review whether the target visual direction is accepted.
4. If accepted, request/import the Phase 2B gameplay vertical-slice assets in parallel with terrain macro-tile production.
5. Request/import the Phase 2C UI render vertical-slice assets in parallel with Phase 2B.
6. Generate or author the first four macro-tile sprites from the same direction.
7. Replace the preview plane with real macro-tile sprites and repeat visual validation.
8. Bind the first unit/building/vehicle/air/sea assets into the `FG-L01` movement/combat probe before scaling to `FG-L02` or `FG-L03`.
9. Bind the first portraits, thumbnails, and mission/mode art into the relevant UI targets before claiming UI visual acceptance.
