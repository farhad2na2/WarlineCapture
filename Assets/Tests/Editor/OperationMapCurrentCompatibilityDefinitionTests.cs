using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapCurrentCompatibilityDefinitionTests
{
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
        Assert.That(definition.Anchors.Length, Is.EqualTo(2));
        Assert.That(definition.GridMetadata.ContentHash, Is.EqualTo(ComputeFileHash(
            "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset")));
        Assert.That(definition.SurfaceMetadata.ContentHash, Is.EqualTo(ComputeFileHash(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset")));
    }

    private static string ComputeFileHash(string assetPath)
    {
        using FileStream stream = File.OpenRead(Path.GetFullPath(assetPath));
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(stream);
        const string digits = "0123456789abcdef";
        char[] result = new char[hash.Length * 2];
        for (int index = 0; index < hash.Length; index++)
        {
            result[index * 2] = digits[hash[index] >> 4];
            result[index * 2 + 1] = digits[hash[index] & 0x0f];
        }
        return new string(result);
    }
}
