# Game_Terrain4 Larger Island Regeneration Checklist

Date: 2026-05-25

Status: Steps 1-5 complete.

Goal: make `Game_Terrain4` large enough for the 2024x2024 playable map image/masks to sit inside green/dirt playable land, with beach/coast remaining as visual border outside the gameplay contract.

Source references:
- `Assets/Game/Scenes/Game_Terrain3.unity` - source island prefab examples.
- `Assets/Game/Scenes/Game_Terrain4.unity` - regenerated target scene.
- `Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01/` - current 2024x2024 map pack.
- `Design/WarlineCapture_3D_Operation_Map_Texture_Mask_Workflow.md` - sampling and gameplay rules.

## Steps

- [x] 1. Audit the current `Game_Terrain4` playable-land footprint and prove why the 2024 map image did not fit inside green/dirt land.
- [x] 2. Enlarge the source-prefab island foundation using `Game_Terrain3` beach, grass, dirt, and detail-grass prefabs without generating replacement terrain meshes.
- [x] 3. Remap mask dressing placement to the explicit 2024 playable-map footprint so generated mountains, trees, bushes, and rocks stay tied to gameplay cells rather than beach/coast bounds.
- [x] 4. Add a full regeneration pipeline that rebuilds the island, reapplies mask dressing, and reruns the playable-land audit in one Unity editor command.
- [x] 5. Run final QA, fix the misleading playable proof capture, confirm validation passes, and document the handoff for the gameplay agent.

## Final Run

Unity command:

`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate`

Final validation:
- Foundation source-prefab instances: `20440`
- Generated dressing prefabs: `3382`
- Mountains / trees / bushes / rocks: `238 / 976 / 1473 / 695`
- Playable map target: `2023 x 2023`
- Green/dirt foundation footprint: `2694.917 x 2614.862`
- Foundation shortfall against target: `0` west, east, south, and north
- Beach prefab centers inside 2024 gameplay map: `0 / 3780`
- Mask dressing validation: passed
- Proof captures: top-down proof and 16:9 playable-frame proof generated
