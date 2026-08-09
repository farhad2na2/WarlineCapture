using System.IO;
using System.Security.Cryptography;
using Exception = System.Exception;
using Game.Configs;
using Game.Editor;
using Game.Composition;
using Game.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapCurrentCompatibilityDefinitionTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var definitionTests = new OperationMapCurrentCompatibilityDefinitionTests();
            definitionTests.CommittedDefinition_MatchesCurrentCompatibilityIdentitiesAndValidates();
            definitionTests.CurrentInfrastructureAuthoring_IsDeterministicAndMatchesCommittedDefinition();
            definitionTests.MatchScene_BindsCurrentCompatibilityCatalogAndIdentity();
            new InitialFactionSpawnCellSystemTests()
                .CurrentCompatibilityDefinitionResolvesFactionDeploymentCells();
            Debug.Log("[OperationMapCurrentCompatibilityDefinitionValidation] result=Passed tests=4");
        }
        catch (Exception exception)
        {
            Debug.LogError("[OperationMapCurrentCompatibilityDefinitionValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void CommittedDefinition_MatchesCurrentCompatibilityIdentitiesAndValidates()
    {
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);

        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.TryValidateMetadata(out string error), Is.True, error);
        Assert.That(definition.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(definition.GridMetadata.AssetGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(
                "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset")));
        Assert.That(definition.GridMetadata.Dimensions, Is.EqualTo(new Vector2Int(2048, 1024)));
        Assert.That(definition.GridMetadata.CellSize, Is.EqualTo(1f));
        Assert.That(definition.SurfaceMetadata.AssetGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(
                "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset")));
        Assert.That(definition.SurfaceMetadata.SurfaceCount, Is.EqualTo(2097152));
        Assert.That(definition.SurfaceMetadata.MaximumHeight,
            Is.GreaterThanOrEqualTo(definition.SurfaceMetadata.MinimumHeight));
        Assert.That(definition.Bounds.WorldMin.x, Is.EqualTo(0f));
        Assert.That(definition.Bounds.WorldMin.z, Is.EqualTo(0f));
        Assert.That(definition.Bounds.WorldMax.x, Is.EqualTo(2048f));
        Assert.That(definition.Bounds.WorldMax.z, Is.EqualTo(1024f));
        Assert.That(definition.Cameras.Length, Is.EqualTo(1));
        Assert.That(Vector3.Distance(
            definition.Cameras[0].Position,
            new Vector3(870.0283f, 42.030247f, 325.60086f)), Is.LessThan(0.001f));
        Assert.That(definition.PlanningCameraId, Is.EqualTo(definition.BattleCameraId));
        Assert.That(definition.Minimap.ProjectionOrigin, Is.EqualTo(Vector3.zero));
        Assert.That(definition.Minimap.ProjectionSize, Is.EqualTo(new Vector2(2048f, 1024f)));
        Assert.That(definition.Anchors.Length, Is.EqualTo(8));
        AssertDeploymentAnchor(
            definition.Anchors[2],
            "anchor.skirmish.desert_base_01.deployment.faction_1",
            1,
            new Vector3(949f, 23f, 344.7f),
            102.1f);
        AssertDeploymentAnchor(
            definition.Anchors[3],
            "anchor.skirmish.desert_base_01.deployment.faction_2",
            2,
            new Vector3(1686f, 23f, 108f),
            102.1f);
        AssertInfrastructureAnchor(
            definition.Anchors[4],
            "anchor.skirmish.desert_base_01.runway.faction_1.lane_0",
            OperationMapAnchorKind.Runway,
            1,
            0);
        for (int index = 0; index < 3; index++)
        {
            AssertInfrastructureAnchor(
                definition.Anchors[index + 5],
                $"anchor.skirmish.desert_base_01.helipad.faction_1.lane_{index}",
                OperationMapAnchorKind.Helipad,
                1,
                index);
        }
        Assert.That(definition.NavigationMetadata.AuthoredSubSceneGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(
                "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity")));
        Assert.That(definition.NavigationMetadata.GridAuthoringLocalId, Is.EqualTo(146043441));
        Assert.That(definition.NavigationMetadata.StaticGridBlockerCount, Is.EqualTo(0));
        Assert.That(definition.NavigationMetadata.UsesSurfaceMovementMetadata, Is.True);
        Assert.That(definition.NavigationMetadata.SupportsDynamicBlockers, Is.True);
        Assert.That(definition.NavigationMetadata.SupportsDynamicOccupancy, Is.True);
        Assert.That(definition.GridMetadata.ContentHash, Is.EqualTo(ComputeFileHash(
            "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset")));
        Assert.That(definition.SurfaceMetadata.ContentHash, Is.EqualTo(ComputeFileHash(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset")));
    }

    [Test]
    public void CurrentInfrastructureAuthoring_IsDeterministicAndMatchesCommittedDefinition()
    {
        MapBuildingPlacementConfig placements = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(
            OperationMapCurrentCompatibilityPlacementStager.SourceBuildingConfigPath);
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);

        OperationMapAnchorConfig[] first =
            OperationMapCurrentInfrastructureAnchorAuthoring.BuildInfrastructureAnchors(placements);
        OperationMapAnchorConfig[] second =
            OperationMapCurrentInfrastructureAnchorAuthoring.BuildInfrastructureAnchors(placements);

        Assert.That(first.Length, Is.EqualTo(4));
        Assert.That(second.Length, Is.EqualTo(first.Length));
        for (int index = 0; index < first.Length; index++)
        {
            Assert.That(second[index].AnchorId, Is.EqualTo(first[index].AnchorId));
            Assert.That(second[index].Kind, Is.EqualTo(first[index].Kind));
            Assert.That(second[index].Position, Is.EqualTo(first[index].Position));
            Assert.That(second[index].EulerAngles, Is.EqualTo(first[index].EulerAngles));
            Assert.That(second[index].Radius, Is.EqualTo(first[index].Radius));
            Assert.That(second[index].FactionId, Is.EqualTo(first[index].FactionId));
            Assert.That(second[index].LaneIndex, Is.EqualTo(first[index].LaneIndex));

            OperationMapAnchorConfig committed = definition.Anchors[index + 4];
            Assert.That(committed.AnchorId, Is.EqualTo(first[index].AnchorId));
            Assert.That(committed.Position, Is.EqualTo(first[index].Position));
            Assert.That(committed.EulerAngles, Is.EqualTo(first[index].EulerAngles));
            Assert.That(committed.Radius, Is.EqualTo(first[index].Radius));
        }
    }

    [Test]
    public void MatchScene_BindsCurrentCompatibilityCatalogAndIdentity()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(
                OperationMapCompatibilityRuntimeBindingBuilder.MatchScenePath,
                OpenSceneMode.Single);
            MatchSceneView view = Object.FindAnyObjectByType<MatchSceneView>(
                FindObjectsInactive.Include);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.gameObject.scene, Is.EqualTo(scene));
            Assert.That(AssetDatabase.GetAssetPath(view.OperationMapCatalog),
                Is.EqualTo(OperationMapCompatibilityRuntimeBindingBuilder.CatalogPath));
            Assert.That(view.OperationMapId,
                Is.EqualTo(OperationMapCompatibilityRuntimeBindingBuilder.OperationMapId));
            Assert.That(view.ScenarioId,
                Is.EqualTo(OperationMapCompatibilityRuntimeBindingBuilder.ScenarioId));
            Assert.That(view.MissionId,
                Is.EqualTo(OperationMapCompatibilityRuntimeBindingBuilder.MissionId));
        }
        finally
        {
            if (setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static string ComputeFileHash(string assetPath)
    {
        byte[] source = File.ReadAllBytes(Path.GetFullPath(assetPath));
        int crlfCount = 0;
        for (int index = 0; index + 1 < source.Length; index++)
        {
            if (source[index] == '\r' && source[index + 1] == '\n')
                crlfCount++;
        }

        byte[] canonical = source;
        if (crlfCount > 0)
        {
            canonical = new byte[source.Length - crlfCount];
            int outputIndex = 0;
            for (int inputIndex = 0; inputIndex < source.Length; inputIndex++)
            {
                if (source[inputIndex] == '\r' &&
                    inputIndex + 1 < source.Length &&
                    source[inputIndex + 1] == '\n')
                    continue;
                canonical[outputIndex++] = source[inputIndex];
            }
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(canonical);
        const string digits = "0123456789abcdef";
        char[] result = new char[hash.Length * 2];
        for (int index = 0; index < hash.Length; index++)
        {
            result[index * 2] = digits[hash[index] >> 4];
            result[index * 2 + 1] = digits[hash[index] & 0x0f];
        }
        return new string(result);
    }

    private static void AssertInfrastructureAnchor(
        OperationMapAnchorConfig anchor,
        string expectedId,
        OperationMapAnchorKind expectedKind,
        int expectedFactionId,
        int expectedLaneIndex)
    {
        Assert.That(anchor.AnchorId, Is.EqualTo(expectedId));
        Assert.That(anchor.Kind, Is.EqualTo(expectedKind));
        Assert.That(anchor.FactionId, Is.EqualTo(expectedFactionId));
        Assert.That(anchor.LaneIndex, Is.EqualTo(expectedLaneIndex));
        Assert.That(anchor.Radius, Is.GreaterThan(0f));
        Assert.That(anchor.TryValidate(out string error), Is.True, error);
    }

    private static void AssertDeploymentAnchor(
        OperationMapAnchorConfig anchor,
        string expectedId,
        int expectedFactionId,
        Vector3 expectedPosition,
        float expectedRadius)
    {
        Assert.That(anchor.AnchorId, Is.EqualTo(expectedId));
        Assert.That(anchor.Kind, Is.EqualTo(OperationMapAnchorKind.Deployment));
        Assert.That(anchor.FactionId, Is.EqualTo(expectedFactionId));
        Assert.That(anchor.LaneIndex, Is.EqualTo(-1));
        Assert.That(Vector3.Distance(anchor.Position, expectedPosition), Is.LessThan(0.001f));
        Assert.That(anchor.Radius, Is.EqualTo(expectedRadius).Within(0.001f));
        Assert.That(anchor.TryValidate(out string error), Is.True, error);
    }
}
