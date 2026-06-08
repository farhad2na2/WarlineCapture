# WarlineCapture Tactical Map AI Workflow

Accepted: 2026-05-07

This note records the validated workflow for close-up 2D isometric tactical maps generated with AI art and tested in Unity.

This workflow is the tactical/zoomed-in side of `Strategic_Tactical_Map_Gameplay_Alignment.md`. Strategic previews and minimaps are separate deliverables; they do not replace the close-up playable tactical ground plate.

## Decision

Use AI-generated tactical ground images as close-up gameplay plates, with gameplay entities kept as separate sprites.

The accepted test scene is:

- `Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalGroundQualityTest_A.unity`

The accepted validation pattern is:

- close-up tactical ground image is separate from soldiers, vehicles, tents, and buildings
- no gameplay units or buildings are baked into the ground image
- AI image is not upscaled
- image is placed into a power-of-two Unity texture for better compression
- camera uses a smaller close-up view instead of forcing one image to cover a huge world area
- unit and vehicle sprites are scaled against the map in Unity before producing more assets

## Current Validated Setup

Unity scene:

- `TacticalGroundQualityTest_A.unity`

Ground source:

- `Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_source_a.png`
- AI native size: `1672 x 941`

Unity-ready ground texture:

- `Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a.png`
- texture size: `2048 x 1024`
- source art is padded into the POT texture, not stretched

Camera:

- active camera: `TacticalGroundQualityTest_A_CloseCamera_CloseScale`
- orthographic size: about `0.6`

Entity scale validation:

- infantry squad scale anchor is `0.10` in Unity
- current vehicle anchors are battle tank around `0.085` and APC around `0.095`
- current medium building anchors are command building around `0.14` and tent cluster around `0.13`
- current large industrial building anchor is fuel/refinery module around `0.30`
- avoid returning to oversized infantry/vehicle scales; if infantry must be much larger than `0.10`, the map likely represents too small an area or the sprite source needs regeneration

## Prompt Rules

For tactical ground maps, request:

- close-up 45-degree isometric tactical ground plate
- smaller gameplay area, not a huge strategic map
- premium realistic 2.5D/isometric mobile RTS quality
- roads, curbs, concrete pads, dust, markings, walls, drainage, small environmental detail
- clear open placement zones for separate units/buildings
- no soldiers, vehicles, aircraft, tents, buildings, labels, UI, watermarks, or text baked into the ground

Avoid asking for:

- one huge 8K map
- seamless edge-to-edge AI tiles as a hard requirement
- a strategic overview map for close soldier gameplay
- non-POT final texture dimensions
- upscaled tactical art

## Production Rules

1. Generate the clean ground plate first.
2. Review the image visually before Unity work when the visual direction is uncertain.
3. Copy the accepted generated image into the project.
4. Pad it into a POT texture such as `2048 x 1024` without stretching.
5. Import the POT texture as a sprite.
6. Build a Unity validation scene with the active close camera.
7. Place separate soldiers, vehicles, tents, and buildings on top.
8. Validate the scale before generating more assets.

## Why This Workflow Was Chosen

Strategic maps look good zoomed out, but become blurry when the camera zooms in to soldier scale.

Small AI tactical plates can look sharp close-up, but if they represent too much world area, units must be scaled too small.

The accepted balance is:

- smaller close-up AI ground plate
- POT texture for Unity compression
- camera around `0.6`
- separate gameplay entities on top

This gives a practical visual validation path before committing to a larger battlefield system.

## Next Asset Set

After this workflow is accepted, produce a small controlled batch:

- 2 close-up tactical ground plates using this same camera scale
- 1 infantry squad sprite set
- 1 tank sprite
- 1 APC sprite
- 1 tent cluster
- 1 command building
- 1 fuel/refinery module

Each asset must be tested in the same Unity scale scene before expanding the batch.

## First Controlled Batch

Created: 2026-05-07

Unity scene:

- `Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalProductionBatch_A.unity`

Batch contents:

- accepted close ground A reused as the baseline
- 2 new close-up POT tactical ground plates
- 6 separate transparent gameplay sprites: infantry squad, battle tank, APC, command building, tent cluster, fuel/refinery module

The active scene camera is:

- `TacticalProductionBatch_A_ActiveCloseCamera_MapB`

The disabled review cameras let Map A, Map C, or the full three-map layout be inspected without rebuilding the scene.
