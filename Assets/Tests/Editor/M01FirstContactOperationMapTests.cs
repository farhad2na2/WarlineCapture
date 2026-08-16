using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactOperationMapTests
{
    private const string Marker = "[M01FirstContactOperationMapValidation] result=Passed tests=13";
    private const string CatalogPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset";
    private const string MapPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
    private const string ScenarioPath =
        "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";
    private const string SourcePath = "Assets/Game/Configs/OperationMaps/Candidates/" +
        "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
    private const string SkirmishCatalogPath =
        "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset";
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_014_operation_map.json";
    private const string SourceAssetHash =
        "f91b737280d8950d97264b54589b963f605a8d8911a0f4e17397bef667e4eba6";
    private const string SkirmishCatalogHash =
        "dd0f215650aab0648e9aa48fff37d957cc5552d2e19d2d72a759a0ec050d69d0";

    public static void RunFocusedValidation()
    {
        try
        {
            OperationMapCatalogConfig persistedCatalog = Load<OperationMapCatalogConfig>(CatalogPath);
            OperationMapDefinition persistedMap = Load<OperationMapDefinition>(MapPath);
            Require(persistedCatalog != null && persistedMap != null,
                "Persisted M01 catalog or logical map is missing before regeneration.");
            Require(persistedCatalog.TryResolveEntry(
                        persistedMap.OperationMapId,
                        out OperationMapCatalogEntryConfig persistedEntry) &&
                    persistedEntry.ContentPack.ContentVersion == persistedMap.ContentVersion &&
                    persistedEntry.ContentPack.ContentHash == persistedMap.ContentHash,
                "Persisted M01 content-pack version/hash drifted from the logical definition.");
            Game.Editor.M01FirstContactConfigBuilder.Build();
            OperationMapCatalogConfig catalog = Load<OperationMapCatalogConfig>(CatalogPath);
            OperationMapDefinition map = Load<OperationMapDefinition>(MapPath);
            OperationMapDefinition source = Load<OperationMapDefinition>(SourcePath);
            ScenarioSetupConfig scenario = Load<ScenarioSetupConfig>(ScenarioPath);
            Require(catalog != null && map != null && source != null && scenario != null, "Required M01 assets are missing.");
            Require(catalog.TryValidate(out string catalogError), catalogError);
            Require(catalog.Definitions.Length == 1 && catalog.Entries.Length == 1,
                "Chapter 01 catalog must contain exactly the M01 logical map.");
            Require(catalog.TryResolve("opmap.ch01.district_edge_01", out OperationMapDefinition resolved) &&
                    ReferenceEquals(resolved, map), "M01 catalog did not resolve its exact logical map.");
            ValidateFreshBootstrap(catalog, map, scenario);
            Require(catalog.TryResolveEntry(map.OperationMapId, out OperationMapCatalogEntryConfig entry) &&
                    entry.ContentPack.DeliveryKind == OperationMapDeliveryKind.BuiltInLocal,
                "M01 catalog entry did not resolve as built-in local content.");
            Require(entry.ContentPack.ContentVersion == map.ContentVersion &&
                    entry.ContentPack.ContentHash == map.ContentHash,
                "M01 content-pack version/hash drifted from the logical definition.");
            Require(OperationMapContractValidation.TryValidate(
                new[] { source, map }, new[] { scenario },
                new[] { Evidence(source), Evidence(map) }, out string contractError), contractError);
            Require(map.SourceBinding.SourceOperationMapId == source.OperationMapId &&
                    map.SourceBinding.SourceIdentityHash == source.SourceIdentityHash &&
                    map.SourceBinding.SourceContentHash == source.ContentHash,
                "M01 physical-source binding drifted.");
            Require(map.SourceSceneReference.AssetGUID == source.SourceSceneReference.AssetGUID &&
                    map.MapSurfaceDataReference.AssetGUID == source.MapSurfaceDataReference.AssetGUID,
                "M01 source scene or accepted surface binding drifted.");
            Require(map.Bounds.PlayableMin == new Vector3(1672f, -6.9545445f, 680f) &&
                    map.Bounds.PlayableMax == new Vector3(1912f, 40.375454f, 856f),
                "M01 Old Market playable bounds drifted.");
            Require(map.Cameras.Length == 2 && map.Anchors.Length == 11 &&
                    map.Minimap.MinimapId == "minimap.ch01.m01.projection",
                "M01 camera, minimap, or anchor publication is incomplete.");
            Require(Sha256File(SourcePath) == SourceAssetHash, "Accepted physical-source asset changed.");
            Require(Sha256File(SkirmishCatalogPath) == SkirmishCatalogHash,
                "Existing Skirmish catalog changed.");
            WriteReport(catalog, map, source, entry);
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactOperationMapValidation] result=Failed");
            throw;
        }
    }

    private static OperationMapContractEvidence Evidence(OperationMapDefinition map) => new(
        map.OperationMapId, map.SchemaVersion, map.ContentVersion, map.SourceIdentityHash,
        map.ContentHash, map.GeneratedMetadataHash);

    private static void ValidateFreshBootstrap(
        OperationMapCatalogConfig catalog,
        OperationMapDefinition map,
        ScenarioSetupConfig scenario)
    {
        using World world = new("M01DC014FreshCatalogBootstrap");
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        FixedString64Bytes scenarioId = new(scenario.ScenarioId);
        FixedString64Bytes missionId = new(Game.Editor.M01FirstContactConfigBuilder.MissionId);
        Require(bootstrap.TryPublish(
            catalog, map.OperationMapId, in scenarioId, in missionId, 1,
            OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata,
            out Entity root, out string error), error);
        ActiveOperationMapComponent active =
            world.EntityManager.GetComponentData<ActiveOperationMapComponent>(root);
        Require(active.OperationMapId.ToString() == map.OperationMapId &&
                active.ScenarioId == scenarioId && active.MissionId == missionId,
            "Fresh catalog bootstrap published the wrong map, scenario, or mission identity.");
    }

    private static void WriteReport(
        OperationMapCatalogConfig catalog,
        OperationMapDefinition map,
        OperationMapDefinition source,
        OperationMapCatalogEntryConfig entry)
    {
        string json = "{\n" +
            "  \"artifactId\":\"m01dc-014-operation-map-v1\", \"taskId\":\"M01DC-014\", \"result\":\"Passed\",\n" +
            $"  \"catalogPath\":\"{CatalogPath}\", \"definitionCount\":{catalog.Definitions.Length},\n" +
            $"  \"operationMapId\":\"{map.OperationMapId}\", \"contentVersion\":{map.ContentVersion},\n" +
            $"  \"contentHash\":\"{map.ContentHash}\", \"generatedMetadataHash\":\"{map.GeneratedMetadataHash}\",\n" +
            $"  \"contentPackId\":\"{entry.ContentPack.ContentPackId}\", \"deliveryKind\":\"{entry.ContentPack.DeliveryKind}\",\n" +
            $"  \"physicalSourceId\":\"{source.OperationMapId}\", \"physicalSourceAssetSha256\":\"{SourceAssetHash}\",\n" +
            $"  \"sourceSceneGuid\":\"{map.SourceSceneReference.AssetGUID}\", \"surfaceGuid\":\"{map.MapSurfaceDataReference.AssetGUID}\",\n" +
            "  \"playableBounds\":[1672,-6.955,680,1912,40.375,856], \"cameraCount\":2, \"anchorCount\":11,\n" +
            $"  \"skirmishCatalogSha256\":\"{SkirmishCatalogHash}\", \"validation\":\"{Marker}\"\n" +
            "}\n";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath)) ?? string.Empty);
        File.WriteAllText(ReportPath, json, new UTF8Encoding(false));
    }

    private static T Load<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path);
    private static string Sha256File(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
