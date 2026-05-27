#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WarlineCaptureGameTerrain4FullRegenerationPipeline
{
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string IslandBuilderReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain3-island2048-builder.md";
    private const string IslandBuilderLayoutJsonPath = "Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048/game_terrain3_island2048_layout.json";
    private const string MaskDressingReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain4-mask-dressing-builder.md";
    private const string MaskDressingValidationJsonPath = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_validation_artifacts.json";
    private const string ReferenceFidelityJsonPath = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_reference_fidelity_summary.json";
    private const string PlayableAuditReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain4-playable-land-audit.md";
    private const string PlayableAuditJsonPath = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain4_FullRegeneration";
    private const string SummaryJsonPath = DataRoot + "/game_terrain4_full_regeneration_summary.json";
    private const string ReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain4-full-regeneration-pipeline.md";

    [MenuItem("WarlineCapture/Design/Game Terrain4/Full Regenerate Larger Island")]
    public static void FullRegenerate()
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ReportPath)));

        WarlineCaptureGameTerrain3Island2048Builder.BuildScene();
        WarlineCaptureGameTerrain4MaskDressingBuilder.BuildMaskDressing();
        WarlineCaptureGameTerrain4MaskDressingBuilder.ValidateReferenceFidelity();
        WarlineCaptureGameTerrain4PlayableLandAudit.AuditPlayableLandFootprint();

        WriteSummaryJson();
        WriteReport();
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_GAME_TERRAIN4_FULL_REGENERATION_READY"
            + " scene=" + TargetScenePath
            + " report=" + ReportPath
            + " summary=" + SummaryJsonPath);
    }

    private static void WriteSummaryJson()
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"GameTerrain4_FullRegeneration_LargerIsland\",");
        json.AppendLine("  \"date\": \"2026-05-25\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"purpose\": \"Rebuild Game_Terrain4 from the enlarged source-prefab island foundation, apply 2024 playable-map mask dressing, then audit playable land coverage.\",");
        json.AppendLine("  \"contract\": {");
        json.AppendLine("    \"sourcePrefabOnlyFoundation\": true,");
        json.AppendLine("    \"generatedTerrainMeshes\": false,");
        json.AppendLine("    \"playableMapGrid\": \"2024x2024\",");
        json.AppendLine("    \"maskPlacementFootprint\": \"Explicit playable-map rect; beach and coast remain visual border outside gameplay contract\"");
        json.AppendLine("  },");
        json.AppendLine("  \"sequence\": [");
        json.AppendLine("    { \"step\": 1, \"method\": \"WarlineCaptureGameTerrain3Island2048Builder.BuildScene\", \"output\": \"" + IslandBuilderLayoutJsonPath + "\" },");
        json.AppendLine("    { \"step\": 2, \"method\": \"WarlineCaptureGameTerrain4MaskDressingBuilder.BuildMaskDressing\", \"output\": \"" + MaskDressingValidationJsonPath + "\" },");
        json.AppendLine("    { \"step\": 3, \"method\": \"WarlineCaptureGameTerrain4MaskDressingBuilder.ValidateReferenceFidelity\", \"output\": \"" + ReferenceFidelityJsonPath + "\" },");
        json.AppendLine("    { \"step\": 4, \"method\": \"WarlineCaptureGameTerrain4PlayableLandAudit.AuditPlayableLandFootprint\", \"output\": \"" + PlayableAuditJsonPath + "\" }");
        json.AppendLine("  ],");
        json.AppendLine("  \"validationArtifacts\": {");
        json.AppendLine("    \"islandBuilderReport\": \"" + IslandBuilderReportPath + "\",");
        json.AppendLine("    \"islandBuilderLayoutJson\": \"" + IslandBuilderLayoutJsonPath + "\",");
        json.AppendLine("    \"maskDressingReport\": \"" + MaskDressingReportPath + "\",");
        json.AppendLine("    \"maskDressingValidationJson\": \"" + MaskDressingValidationJsonPath + "\",");
        json.AppendLine("    \"referenceFidelityJson\": \"" + ReferenceFidelityJsonPath + "\",");
        json.AppendLine("    \"playableAuditReport\": \"" + PlayableAuditReportPath + "\",");
        json.AppendLine("    \"playableAuditJson\": \"" + PlayableAuditJsonPath + "\"");
        json.AppendLine("  }");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain4 Full Regeneration Pipeline");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-25");
        report.AppendLine();
        report.AppendLine("Step 4 complete: `Game_Terrain4` now has one repeatable editor pipeline for rebuilding the enlarged island foundation, remapping the 2024x2024 mask dressing, and auditing playable-land coverage.");
        report.AppendLine();
        report.AppendLine("Pipeline sequence:");
        report.AppendLine("- `WarlineCaptureGameTerrain3Island2048Builder.BuildScene()` rebuilds the `Island` root from the source `Game_Terrain3` beach, grass, dirt, and detail-grass prefabs.");
        report.AppendLine("- `WarlineCaptureGameTerrain4MaskDressingBuilder.BuildMaskDressing()` places mountains, trees, bushes, and rocks from the `Game_Terrain3` example groups using the 2024 playable-map masks.");
        report.AppendLine("- `WarlineCaptureGameTerrain4MaskDressingBuilder.ValidateReferenceFidelity()` fails the pipeline before optimization if clean captures, dense blocker-belt vegetation, connected mountain mass, reserve clearance, or foundation material variety are missing.");
        report.AppendLine("- `WarlineCaptureGameTerrain4PlayableLandAudit.AuditPlayableLandFootprint()` verifies the rebuilt green/dirt island foundation covers the gameplay map footprint.");
        report.AppendLine();
        report.AppendLine("Implementation contract:");
        report.AppendLine("- Do not generate replacement terrain meshes for this pass.");
        report.AppendLine("- Keep the expanded island as source-prefab placement under `Island/ExpandedIsland_SourceGameTerrain3PrefabsOnly`.");
        report.AppendLine("- Keep mountain, tree, bush, and rock dressing in generated sibling groups under `Island`.");
        report.AppendLine("- Split vegetation into playable and blocker-belt groups; blocker-belt vegetation is visual dressing and has colliders removed.");
        report.AppendLine("- Keep the mask grid mapped to the explicit 2024x2024 playable map footprint; beach and coast are only the visual border outside that footprint.");
        report.AppendLine();
        report.AppendLine("Outputs:");
        report.AppendLine("- Scene: `" + TargetScenePath + "`");
        report.AppendLine("- Pipeline summary: `" + SummaryJsonPath + "`");
        report.AppendLine("- Island builder report: `" + IslandBuilderReportPath + "`");
        report.AppendLine("- Island layout data: `" + IslandBuilderLayoutJsonPath + "`");
        report.AppendLine("- Mask dressing report: `" + MaskDressingReportPath + "`");
        report.AppendLine("- Mask dressing validation: `" + MaskDressingValidationJsonPath + "`");
        report.AppendLine("- Reference fidelity validation: `" + ReferenceFidelityJsonPath + "`");
        report.AppendLine("- Playable land audit: `" + PlayableAuditReportPath + "`");
        report.AppendLine("- Playable land audit data: `" + PlayableAuditJsonPath + "`");
        report.AppendLine();
        report.AppendLine("Run command:");
        report.AppendLine("`Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate`");

        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static string ProjectPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return Directory.GetCurrentDirectory();
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }
}
#endif
