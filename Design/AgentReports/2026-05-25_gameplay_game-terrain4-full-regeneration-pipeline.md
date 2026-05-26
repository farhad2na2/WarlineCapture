# Game_Terrain4 Full Regeneration Pipeline

Date: 2026-05-25

Step 4 complete: `Game_Terrain4` now has one repeatable editor pipeline for rebuilding the enlarged island foundation, remapping the 2024x2024 mask dressing, and auditing playable-land coverage.

Pipeline sequence:
- `WarlineCaptureGameTerrain3Island2048Builder.BuildScene()` rebuilds the `Island` root from the source `Game_Terrain3` beach, grass, dirt, and detail-grass prefabs.
- `WarlineCaptureGameTerrain4MaskDressingBuilder.BuildMaskDressing()` places mountains, trees, bushes, and rocks from the `Game_Terrain3` example groups using the 2024 playable-map masks.
- `WarlineCaptureGameTerrain4PlayableLandAudit.AuditPlayableLandFootprint()` verifies the rebuilt green/dirt island foundation covers the gameplay map footprint.

Implementation contract:
- Do not generate replacement terrain meshes for this pass.
- Keep the expanded island as source-prefab placement under `Island/ExpandedIsland_SourceGameTerrain3PrefabsOnly`.
- Keep mountain, tree, bush, and rock dressing in generated sibling groups under `Island`.
- Keep the mask grid mapped to the explicit 2024x2024 playable map footprint; beach and coast are only the visual border outside that footprint.

Outputs:
- Scene: `Assets/Game/Scenes/Game_Terrain4.unity`
- Pipeline summary: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_FullRegeneration/game_terrain4_full_regeneration_summary.json`
- Island builder report: `Design/AgentReports/2026-05-25_gameplay_game-terrain3-island2048-builder.md`
- Island layout data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048/game_terrain3_island2048_layout.json`
- Mask dressing report: `Design/AgentReports/2026-05-25_gameplay_game-terrain4-mask-dressing-builder.md`
- Mask dressing validation: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_validation_artifacts.json`
- Playable land audit: `Design/AgentReports/2026-05-25_gameplay_game-terrain4-playable-land-audit.md`
- Playable land audit data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json`

Run command:
`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate`
