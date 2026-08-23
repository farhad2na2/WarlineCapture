using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactDenseCityReuseTests
{
    private const string Marker = "[M01FirstContactDenseCityReuseValidation] result=Passed tests=8";
    private const string M01MapPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
    private const string SourceDefinitionPath =
        "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
    private const string DatabaseConfigPath =
        "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset";
    private const string DatabaseReportPath =
        "Design/AgentReports/2026-07-28_dense_city_render_virtualization_database.json";
    private const string TransformParityPath =
        "Design/AgentReports/2026-07-24_dense_city_generated_transform_parity.json";
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_016_dense_city_reuse_gate.json";

    private static readonly ProtectedFile[] ProtectedFiles =
    {
        new(SourceDefinitionPath, "f91b737280d8950d97264b54589b963f605a8d8911a0f4e17397bef667e4eba6"),
        new(
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity",
            "5a15843d63868d639b88d2084ea41184af0cf5d6050b22b563a516ef13752b9c"),
        new(
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
            "52b30f735209fbde2911f6bebf7dd397ed66c2fd1355f420ce7db22c7ac85b4f"),
        new(
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
            "f58a73d0a8f3627d7ddf42f72b42a9db495d5139ba5da6768492ac36e671ade9"),
        new(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
            "1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d"),
        new(
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/MinimapRaster.png",
            "420d9f07fec2418fad279d5d104a2c3caa248ee5481c3658fb9b2b65b9afcc3f")
    };

    public static void RunFocusedValidation()
    {
        try
        {
            ValidateProtectedPhysicalFiles();
            (OperationMapDefinition map, OperationMapDefinition source) = ValidateLogicalSourceBinding();
            DenseCityDatabaseReport database = ValidateDatabaseReport();
            ValidateDatabaseConfig(database);
            DenseCityTransformParityReport parity = ValidateTransformParity();
            ValidateNoM01PermanentRenderAsset();
            ValidateNoM01RenderDatabase();
            ValidateAcceptedCounts(database, parity);
            WriteReport(map, source, database, parity);
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01FirstContactDenseCityReuseValidation] result=Failed");
            throw;
        }
    }

    private static void ValidateProtectedPhysicalFiles()
    {
        foreach (ProtectedFile file in ProtectedFiles)
        {
            Require(File.Exists(file.Path), $"Protected dense-city file is missing: {file.Path}");
            Require(Sha256File(file.Path) == file.Sha256,
                $"Protected dense-city file changed: {file.Path}");
        }
    }

    private static (OperationMapDefinition Map, OperationMapDefinition Source) ValidateLogicalSourceBinding()
    {
        OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(M01MapPath);
        OperationMapDefinition source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(SourceDefinitionPath);
        Require(map != null && source != null, "M01 or accepted source OperationMapDefinition is missing.");
        Require(map.OperationMapId == "opmap.ch01.district_edge_01", "M01 logical map identity drifted.");
        Require(map.SourceBinding.SourceOperationMapId == source.OperationMapId &&
                map.SourceBinding.SourceIdentityHash == source.SourceIdentityHash &&
                map.SourceBinding.SourceContentHash == source.ContentHash,
            "M01 no longer binds the exact accepted dense-city source identity.");
        Require(map.SourceSceneReference.AssetGUID == source.SourceSceneReference.AssetGUID &&
                map.MapSurfaceDataReference.AssetGUID == source.MapSurfaceDataReference.AssetGUID,
            "M01 introduced a separate physical scene or surface binding.");
        Require(map.SourceSceneReference.AssetGUID == "dad0bd13fb20943dfb2f881cbe225f05" &&
                map.NavigationMetadata.AuthoredSubSceneGuid == "c00140f2e94a04c3084c8dcb0c18cbd0",
            "Accepted runtime-binding or authored dense EntityScene GUID drifted.");
        return (map, source);
    }

    private static DenseCityDatabaseReport ValidateDatabaseReport()
    {
        DenseCityDatabaseReport report = JsonUtility.FromJson<DenseCityDatabaseReport>(
            File.ReadAllText(DatabaseReportPath));
        Require(report != null && report.result == "Passed", "Dense-city VRP database report is not accepted.");
        Require(report.contentHash == "ec96a423fac12973b06d579a4277a28da0234244630c4f3ffb518c37df293dac" &&
                report.recordOrderingSha256 == "7df0db01d72addb978d991aebcd8cf865a37308adc22e7a911cc525b4bbaa378" &&
                report.configSerializedSha256 == "aec499e27a121941f37cd345a13cc629f35ff14a34cac1a56015ff8fd76911e1",
            "Dense-city VRP database identity or ordering drifted.");
        Require(report.logicalParityResult == "Passed" && report.isolationResult == "Passed",
            "Dense-city logical parity or isolation is no longer accepted.");
        return report;
    }

    private static void ValidateDatabaseConfig(DenseCityDatabaseReport report)
    {
        Require(report.configPath == DatabaseConfigPath, "VRP database config path drifted.");
        Require(Sha256File(DatabaseConfigPath) == report.configSerializedSha256,
            "VRP database config bytes drifted from the accepted report.");
        Require(Sha256File(DatabaseReportPath) ==
                "8e7a432409c71a6d774a3f3434b969ed0d03b6cb46faf5c494de3edda52ac183",
            "Accepted VRP database report bytes drifted.");
    }

    private static DenseCityTransformParityReport ValidateTransformParity()
    {
        DenseCityTransformParityReport report = JsonUtility.FromJson<DenseCityTransformParityReport>(
            File.ReadAllText(TransformParityPath));
        Require(report != null && report.result == "DenseCityGeneratedTransformParityPassed",
            "Dense-city generated transform parity is not accepted.");
        Require(report.unresolvedGeneratedMeshCount == 0 && report.unresolvedGeneratedMaterialCount == 0 &&
                report.generatedMeshMismatchCount == 0 && report.generatedMaterialMismatchCount == 0 &&
                report.generatedBaseColorMismatchCount == 0 && report.missingBakedStableIdCount == 0 &&
                report.unexpectedBakedStableIdCount == 0 && report.rejectedRowCount == 0,
            "Dense-city transform/material parity contains a mismatch.");
        return report;
    }

    private static void ValidateNoM01PermanentRenderAsset()
    {
        string[] renderExtensions = { ".unity", ".entityscene", ".entities", ".bundle" };
        string[] hits = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.IndexOf("district_edge_01", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           path.IndexOf("m01_first_contact", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(path => renderExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .ToArray();
        Require(hits.Length == 0,
            "M01 must not own a permanent render scene/archive: " + string.Join(", ", hits));
    }

    private static void ValidateNoM01RenderDatabase()
    {
        string[] hits = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.IndexOf("district_edge_01", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           path.IndexOf("m01_first_contact", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(path => path.IndexOf("renderdatabase", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           path.IndexOf("virtualizedpresentation", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        Require(hits.Length == 0,
            "M01 must reuse, not duplicate, the accepted VRP database: " + string.Join(", ", hits));
    }

    private static void ValidateAcceptedCounts(
        DenseCityDatabaseReport database,
        DenseCityTransformParityReport parity)
    {
        Require(database.sourceRenderRowCount == 76517 && database.eligibleSourceRowCount == 61925 &&
                database.logicalRenderRowCount == 61925 && database.residentSourceRowCount == 14592 &&
                database.placementCount == 40460 && database.totalPoolSlotCapacity == 7784 &&
                database.sourceRowsRemoved == 0,
            "Accepted dense-city row, placement, residency, or fixed-slot capacity drifted.");
        Require(parity.candidateIdentityCount == 36304 && parity.bakedIdentityCount == 36304 &&
                parity.generatedCandidateRendererEntityCount == 62455 &&
                parity.generatedBakedRenderEntityCount == 62455,
            "Accepted dense-city identity or renderer parity counts drifted.");
    }

    private static void WriteReport(
        OperationMapDefinition map,
        OperationMapDefinition source,
        DenseCityDatabaseReport database,
        DenseCityTransformParityReport parity)
    {
        var report = new ReuseGateReport
        {
            artifactId = "m01dc-016-dense-city-reuse-gate-v1",
            taskId = "M01DC-016",
            result = "Passed",
            logicalOperationMapId = map.OperationMapId,
            physicalOperationMapId = source.OperationMapId,
            entitySceneGuid = map.NavigationMetadata.AuthoredSubSceneGuid,
            protectedPhysicalFileCount = ProtectedFiles.Length,
            databaseContentHash = database.contentHash,
            databaseRecordOrderingSha256 = database.recordOrderingSha256,
            databaseConfigSha256 = database.configSerializedSha256,
            sourceRenderRows = database.sourceRenderRowCount,
            eligibleLogicalRows = database.eligibleSourceRowCount,
            residentRows = database.residentSourceRowCount,
            placements = database.placementCount,
            fixedProxySlots = database.totalPoolSlotCapacity,
            candidateIdentities = parity.candidateIdentityCount,
            bakedIdentities = parity.bakedIdentityCount,
            candidateRenderers = parity.generatedCandidateRendererEntityCount,
            bakedRenderers = parity.generatedBakedRenderEntityCount,
            permanentM01RenderRepresentationCount = 0,
            permanentM01RenderDatabaseCount = 0,
            validation = Marker
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? string.Empty);
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n", new UTF8Encoding(false));
    }

    private static string Sha256File(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty).ToLowerInvariant();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private readonly struct ProtectedFile
    {
        public ProtectedFile(string path, string sha256) { Path = path; Sha256 = sha256; }
        public string Path { get; }
        public string Sha256 { get; }
    }

    [Serializable]
    private sealed class DenseCityDatabaseReport
    {
        public string result;
        public string contentHash;
        public string recordOrderingSha256;
        public string configPath;
        public string configSerializedSha256;
        public int placementCount;
        public int totalPoolSlotCapacity;
        public int sourceRenderRowCount;
        public int eligibleSourceRowCount;
        public int logicalRenderRowCount;
        public int residentSourceRowCount;
        public int sourceRowsRemoved;
        public string logicalParityResult;
        public string isolationResult;
    }

    [Serializable]
    private sealed class DenseCityTransformParityReport
    {
        public string result;
        public int candidateIdentityCount;
        public int bakedIdentityCount;
        public int generatedCandidateRendererEntityCount;
        public int generatedBakedRenderEntityCount;
        public int unresolvedGeneratedMeshCount;
        public int unresolvedGeneratedMaterialCount;
        public int generatedMeshMismatchCount;
        public int generatedMaterialMismatchCount;
        public int generatedBaseColorMismatchCount;
        public int missingBakedStableIdCount;
        public int unexpectedBakedStableIdCount;
        public int rejectedRowCount;
    }

    [Serializable]
    private sealed class ReuseGateReport
    {
        public string artifactId;
        public string taskId;
        public string result;
        public string logicalOperationMapId;
        public string physicalOperationMapId;
        public string entitySceneGuid;
        public int protectedPhysicalFileCount;
        public string databaseContentHash;
        public string databaseRecordOrderingSha256;
        public string databaseConfigSha256;
        public int sourceRenderRows;
        public int eligibleLogicalRows;
        public int residentRows;
        public int placements;
        public int fixedProxySlots;
        public int candidateIdentities;
        public int bakedIdentities;
        public int candidateRenderers;
        public int bakedRenderers;
        public int permanentM01RenderRepresentationCount;
        public int permanentM01RenderDatabaseCount;
        public string validation;
    }
}
