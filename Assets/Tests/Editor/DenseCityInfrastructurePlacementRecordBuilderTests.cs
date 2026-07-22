using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityInfrastructurePlacementRecordBuilderTests
{
    private const string RoadPath =
        "Assets/Game/Prefabs/Roads/Road_Asphalt_With_Sidewalk/Road_Asphalt_With_Sidewalk_Straight.prefab";
    private const string GroundPrefabGuid = "87f34f6fda934c743bede9cef5dd324a";
    private const string GroundMaterialGuid = "e581a57183ed647799810867dc55e965";

    [Test]
    public void CreateVisualized_UsesPersistentProductionRoadIdentityAndMaterials()
    {
        GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPath);
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(30f, 1f, 50f),
            Quaternion.Euler(0f, 90f, 0f),
            Vector3.one);

        DenseCityInfrastructureRecordGroup group =
            DenseCityInfrastructurePlacementRecordBuilder.CreateVisualized(
                CreateRequest(
                    roadPrefab,
                    null,
                    "road",
                    DenseCitySurfaceRecordKind.Road,
                    matrix));

        Assert.That(group.Presentation.PrefabAssetGuid, Is.EqualTo(AssetDatabase.AssetPathToGUID(RoadPath)));
        Assert.That(group.Presentation.MaterialAssetGuids.Length, Is.GreaterThan(0));
        Assert.That(group.Presentation.WorldMatrix, Is.EqualTo(matrix));
        Assert.That(group.Surface.MovementMask,
            Is.EqualTo((uint)(MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded)));
    }

    [Test]
    public void CreateVisualized_RecordsResolvedPersistentGroundMaterialBeforeRealization()
    {
        string groundPrefabPath = AssetDatabase.GUIDToAssetPath(GroundPrefabGuid);
        GameObject groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPrefabPath);
        Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            AssetDatabase.GUIDToAssetPath(GroundMaterialGuid));

        DenseCityInfrastructureRecordGroup group =
            DenseCityInfrastructurePlacementRecordBuilder.CreateVisualized(
                CreateRequest(
                    groundPrefab,
                    _ => groundMaterial,
                    "road-terrain-patch",
                    DenseCitySurfaceRecordKind.Terrain,
                    Matrix4x4.identity));

        Assert.That(group.Presentation.PrefabAssetGuid, Is.EqualTo(GroundPrefabGuid));
        Assert.That(group.Presentation.MaterialAssetGuids.ToArray(), Is.EqualTo(new[] { GroundMaterialGuid }));
        Assert.That(group.Surface.Kind, Is.EqualTo(DenseCitySurfaceRecordKind.Terrain));
    }

    private static DenseCityInfrastructurePlacementRecordRequest CreateRequest(
        GameObject prefab,
        System.Func<Material, Material> materialResolver,
        string recordKind,
        DenseCitySurfaceRecordKind surfaceKind,
        Matrix4x4 worldMatrix) =>
        new(
            "dense-city-v1",
            42,
            3,
            10,
            recordKind,
            surfaceKind,
            prefab,
            materialResolver,
            worldMatrix,
            new Vector2(10f, 10f),
            1f,
            (uint)(MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded),
            0,
            new Vector2Int(1, 2),
            true,
            true,
            2);
}
