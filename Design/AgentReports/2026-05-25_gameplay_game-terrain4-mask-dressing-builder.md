# Game_Terrain4 Mask Dressing Builder

Date: 2026-05-25

Steps complete: 1 - dedicated editor builder created; 2 - source prefab catalog generated; 3 - mask placement remapped to the explicit 2024 playable-map footprint on the enlarged green island foundation; 4 - generated child groups created under Island; 5 - map masks sampled with documented 2024x2024 coordinate rules; 6 - placement rejection rules generated for playable-map footprint, reserves, and blocker/pathing constraints; 7 - deterministic blue-noise-style spacing plan generated; 8 - generated mountain, rock, tree, and bush prefabs placed into scene groups; 9 - validation artifacts generated.

Purpose: prepare `Game_Terrain4` for mask-based map dressing using `SyntyHighlands_01` masks and the example model groups under `Game_Terrain3/Island`.

Validated references:
- Source scene: `Assets/Game/Scenes/Game_Terrain3.unity`
- Target scene: `Assets/Game/Scenes/Game_Terrain4.unity`
- Workflow doc: `Design/WarlineCapture_3D_Operation_Map_Texture_Mask_Workflow.md`
- Map pack: `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01`
- Prefab catalog: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_mask_dressing_prefab_catalog.json`
- Foundation snapshot: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_island_foundation_snapshot.json`
- Generated group manifest: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_generated_group_manifest.json`
- Mask sampling summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_mask_sampling_summary.json`
- Placement rejection summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_placement_rejection_summary.json`
- Spacing plan summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_spacing_plan_summary.json`
- Dressing placement summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_dressing_placement_summary.json`
- Validation artifact summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_validation_artifacts.json`
- Top-down proof capture: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png`
- Playable-frame proof capture: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png`
- Target Island active: True
- Target Island local position: (0, 0, 0)
- Target Island local scale: (1, 1, 1)
- Foundation base child: `ExpandedIsland_SourceGameTerrain3PrefabsOnly`
- Foundation child index: 0
- Foundation active in hierarchy: True
- Foundation transform count: 22478
- Foundation renderers: 22474
- Foundation mesh filters: 22474
- Foundation colliders: 22358
- Foundation world bounds center: (7.996, -2.635, -61.792)
- Foundation world bounds size: (3082.198, 7.829, 2975.169)

Generated child groups under `Island`:
- `Generated_Mountains`: present, child index 1, children 55, purpose: Mask-placed mountain and cliff blocker dressing.
- `Generated_Mountains_Dirt`: present, child index 2, children 40, purpose: Dirt-biome mountain and cliff dressing using DirthMountain source examples.
- `Generated_Trees_Playable`: present, child index 3, children 5755, purpose: Lower-density tree dressing near playable lanes and open groves.
- `Generated_Trees_Dirt`: present, child index 4, children 11556, purpose: Dirt-biome tree dressing using DirthTrees source examples.
- `Generated_Trees_BlockerBelt`: present, child index 5, children 7587, purpose: Dense tree dressing on visual blocker belts and mountain edges.
- `Generated_Bushes_Playable`: present, child index 6, children 1494, purpose: Playable-lane scrub and bush dressing.
- `Generated_Bushes_BlockerBelt`: present, child index 7, children 4966, purpose: Dense low vegetation around visual blocker belts and ridges.
- `Generated_Rocks`: present, child index 8, children 807, purpose: Mask-placed rock and rubble dressing.
- `Generated_BlockerDebug`: present, child index 9, children 0, purpose: Generated blocker/pathing proof markers and debug output.

Source example group child counts:
- Mountains: 12
- DirthMountain: 7
- Trees: 26
- DirthTrees: 18
- Bushes: 59
- Rocks: 175

Prefab catalog:
- Mountains: 3 unique prefab assets from 12 examples
- DirthMountain: 7 unique prefab assets from 7 examples
- Trees: 6 unique prefab assets from 26 examples
- DirthTrees: 18 unique prefab assets from 18 examples
- Bushes: 22 unique prefab assets from 59 examples
- Rocks: 26 unique prefab assets from 175 examples

Texture inputs:
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/base_visual.png`: 2024x2024
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/blocker_mask.png`: 2024x2024
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/tree_density_mask.png`: 2024x2024
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/rock_density_mask.png`: 2024x2024
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/height_mask.png`: 2024x2024

Mask sampling:
- Grid size: 2024x2024
- Sampling rule: u=gridX/2023.0; v=gridZ/2023.0; pixelX=round(u*(imageWidth-1)); pixelY=round((1.0-v)*(imageHeight-1))
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/base_visual.png`: avg 102.72, min 9, max 225, classes visualBright=289, visualDark=598941, visualHighMid=1010742, visualLowMid=2486604
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/blocker_mask.png`: avg 84.92, min 0, max 255, classes blocked=1338433, softEdgeReview=53317, walkable=2704826
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/tree_density_mask.png`: avg 36.58, min 0, max 254, classes treeDense=105570, treeMedium=332296, treeNone=2559266, treeSparse=1099444
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/rock_density_mask.png`: avg 36.65, min 0, max 255, classes rockDense=90889, rockMedium=320979, rockNone=2558644, rockSparse=1126064
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/height_mask.png`: avg 74.34, min 8, max 247, classes flatLowland=2221047, gentleSlope=1409287, mountainCliffHigh=84120, raisedRoughHill=382122

Placement rejection:
- Playable map footprint grid rect: 0,0 2024x2024
- Valid-any placement cells after global rejection: 1275780
- Global rejection counts: hardBlocked=1338433, reserveZone=712800, softPathingEdge=53317
- Trees_Playable: raw 1537310, valid 635521, rejected 901789 (hardBlocked=819444, reserveZone=180329, softPathingEdge=28375)
- Trees_BlockerBelt: raw 437866, valid 289629, rejected 148237 (notBlockerBelt=94072, reserveZone=60799)
- Bushes_Playable: raw 1537310, valid 635521, rejected 901789 (hardBlocked=819444, reserveZone=180329, softPathingEdge=28375)
- Bushes_BlockerBelt: raw 437866, valid 289629, rejected 148237 (notBlockerBelt=94072, reserveZone=60799)
- Rocks: raw 1537932, valid 649284, rejected 888648 (hardBlocked=819396, reserveZone=193547)
- Mountains: raw 202453, valid 168240, rejected 34213 (reserveZone=34213)

Spacing plan:
- Seed: 913742
- Method: Deterministic stratified dart pass: choose one best mask-weighted candidate per tile, then enforce minimum distance against nearby accepted points.
- Total accepted spaced points: 8242
- Mountains: accepted 55 / tile candidates 137, min distance 118, rejected by spacing 82
- Mountains_Dirt: accepted 40 / tile candidates 76, min distance 132, rejected by spacing 36
- Trees_Playable: accepted 2644 / tile candidates 5709, min distance 14, rejected by spacing 3065
- Trees_Dirt: accepted 1663 / tile candidates 3215, min distance 14, rejected by spacing 1552
- Trees_BlockerBelt: accepted 938 / tile candidates 1972, min distance 14, rejected by spacing 1034
- Bushes_Playable: accepted 1403 / tile candidates 3017, min distance 24, rejected by spacing 1614
- Bushes_BlockerBelt: accepted 804 / tile candidates 1676, min distance 16, rejected by spacing 872
- Rocks: accepted 695 / tile candidates 1537, min distance 42, rejected by spacing 842

Dressing placement:
- Total placed prefabs: 32260
- Mountains: group `Generated_Mountains` contains 55 placed prefabs.
- Mountains_Dirt: group `Generated_Mountains_Dirt` contains 40 placed prefabs.
- Trees_Playable: group `Generated_Trees_Playable` contains 5755 placed prefabs.
- Trees_Dirt: group `Generated_Trees_Dirt` contains 11556 placed prefabs.
- Trees_BlockerBelt: group `Generated_Trees_BlockerBelt` contains 7587 placed prefabs.
- Bushes_Playable: group `Generated_Bushes_Playable` contains 1494 placed prefabs.
- Bushes_BlockerBelt: group `Generated_Bushes_BlockerBelt` contains 4966 placed prefabs.
- Rocks: group `Generated_Rocks` contains 807 placed prefabs.

Validation artifacts:
- Captures rendered this run: True
- Top-down proof: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png`
- Playable-frame proof: `Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png`
- Overall validation pass: True
- PASS `totalPlacedPrefabs.minimum`: expected 8242, actual 32260. Scene placed-prefab total must be at least the deterministic spacing plan because fidelity clusters may expand accepted points.
- PASS `count.Mountains.minimum`: expected 55, actual 55. Generated group count for Mountains must be at least accepted spacing points after cluster expansion.
- PASS `count.Mountains_Dirt.minimum`: expected 40, actual 40. Generated group count for Mountains_Dirt must be at least accepted spacing points after cluster expansion.
- PASS `count.Trees_Playable.minimum`: expected 2644, actual 5755. Generated group count for Trees_Playable must be at least accepted spacing points after cluster expansion.
- PASS `count.Trees_Dirt.minimum`: expected 1663, actual 11556. Generated group count for Trees_Dirt must be at least accepted spacing points after cluster expansion.
- PASS `count.Trees_BlockerBelt.minimum`: expected 938, actual 7587. Generated group count for Trees_BlockerBelt must be at least accepted spacing points after cluster expansion.
- PASS `count.Bushes_Playable.minimum`: expected 1403, actual 1494. Generated group count for Bushes_Playable must be at least accepted spacing points after cluster expansion.
- PASS `count.Bushes_BlockerBelt.minimum`: expected 804, actual 4966. Generated group count for Bushes_BlockerBelt must be at least accepted spacing points after cluster expansion.
- PASS `count.Rocks.minimum`: expected 695, actual 807. Generated group count for Rocks must be at least accepted spacing points after cluster expansion.
- PASS `reserveClear.CityReserve`: expected 0, actual 0. No generated dressing may occupy reserved city/base placement space.
- PASS `reserveClear.NorthwestBaseReserve`: expected 0, actual 0. No generated dressing may occupy reserved city/base placement space.
- PASS `reserveClear.SoutheastBaseReserve`: expected 0, actual 0. No generated dressing may occupy reserved city/base placement space.
- PASS `playableMapFootprint.containment`: expected 0, actual 0. Generated dressing must remain inside the 2024 playable map footprint, which is now fully covered by green/dirt terrain.
- PASS `pathing.playableVegetationClear`: expected 0, actual 0. Playable vegetation must not sit on soft-edge or hard-blocker pathing cells; blocker-belt vegetation is visual dressing and has colliders removed.
- PASS `pathing.rocksClear`: expected 0, actual 0. Decorative rocks must not sit on hard-blocked pathing cells.
- PASS `blockerBelt.mountainsNaturalTerrain`: expected 95, actual 95. Mountain dressing must stay tied to blocker, high-terrain, or dense-rock mask cells.
- PASS `fidelity.vegetationNotBuriedUnderMountains`: expected 0, actual 0. Trees and bushes must keep a visible separation from mountain anchor prefabs.
- PASS `capture.topDownProof`: expected 1, actual 1. Top-down proof image must exist and be non-empty.
- PASS `capture.playableAngleProof`: expected 1, actual 1. Playable-frame proof image must exist and be non-empty.
- PASS `capture.playableAngleContent`: expected 1, actual 1. Playable-frame proof must show readable terrain content, not only sky/background.
- PASS `fidelity.cleanTopDownCapture`: expected 1, actual 1. Clean top-down scene capture must exist without debug overlays.
- PASS `fidelity.blockerVegetation`: expected 900, actual 12553. Dense blocker-belt vegetation should be visibly present.
- PASS `fidelity.mountainOpenLayoutMinimum`: expected 50, actual 95. Mountain pass needs enough anchors to read as terrain framing.
- PASS `fidelity.mountainOpenLayoutMaximum`: expected 380, actual 95. Mountain pass must stay open and must not create connected walls across movement lanes.
- PASS `fidelity.vegetationMountainOverlap`: expected 0, actual 0. Vegetation must not be hidden underneath mountain prefabs.
- PASS `fidelity.reservePollution`: expected 0, actual 0. City/base reserve areas must remain clear of generated dressing.

Implementation report: `Game_Terrain4/Island` now contains the enlarged green/dirt island foundation plus generated sibling dressing groups. The validation pass confirms prefab counts, reserve-zone clearance, playable-map containment, vegetation pathing rules, rock blocker rules, mountain blocker-belt rules, and proof captures.
