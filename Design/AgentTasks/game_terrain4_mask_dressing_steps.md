# Game_Terrain4 Mask-Based Map Dressing Checklist

Date: 2026-05-25

Status: Steps 1-9 complete.

Goal: Build a high-quality large `Game_Terrain4` operation map by using the existing 2048 island base, the `SyntyHighlands_01` image/mask pack, and the example mountain/tree/bush/rock prefabs placed under `Game_Terrain3/Island`.

Source references:
- `Assets/Game/Scenes/Game_Terrain3.unity` - source example model groups under `Island`.
- `Assets/Game/Scenes/Game_Terrain4.unity` - target scene; generated content belongs under root `Island`.
- `Design/WarlineCapture_3D_Operation_Map_Texture_Mask_Workflow.md` - mask sampling and gameplay rules.
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/` - `base_visual`, `blocker_mask`, `tree_density_mask`, `rock_density_mask`, and `height_mask`.

Completion rule: leave each step unchecked until the implementation for that step is completed and validated. When the whole task is complete, every item below should be checked.

## Steps

- [x] 1. Create a dedicated editor builder for `Game_Terrain4` mask-based map dressing.
- [x] 2. Read the example prefabs from `Game_Terrain3/Island` and build a prefab catalog for mountains, trees, bushes, and rocks.
- [x] 3. Preserve the current `Game_Terrain4/Island/ExpandedIsland_SourceGameTerrain3PrefabsOnly` island base as the terrain foundation.
- [x] 4. Add generated child groups under `Game_Terrain4/Island` for mountains, trees, bushes, rocks, and blocker/debug output.
- [x] 5. Sample the `SyntyHighlands_01` mask images using the documented 2024x2024 map-coordinate rules.
- [x] 6. Reject placements outside the island, inside city/base reserve zones, or in areas that violate blocker/pathing rules.
- [x] 7. Use deterministic blue-noise or Poisson-style spacing so model placement reads as authored instead of random spam.
- [x] 8. Place mountains, rocks, trees, and bushes from the sampled masks using the `Game_Terrain3` example model set.
- [x] 9. Generate validation artifacts: top-down proof, playable-angle proof, prefab counts, reserve-zone clear checks, blocker-belt checks, and a short implementation report.
