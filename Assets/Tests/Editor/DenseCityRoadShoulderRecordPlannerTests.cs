using System;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityRoadShoulderRecordPlannerTests
{
    private const string SidewalkRoadPath =
        "Assets/Game/Prefabs/Roads/Road_Asphalt_With_Sidewalk/Road_Asphalt_With_Sidewalk_Straight.prefab";
    private const string DirtRoadPath =
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_Straight.prefab";

    [Test]
    public void Create_UsesTypedProductionFootprintsWithoutGeneratedHierarchyInspection()
    {
        GameObject sidewalkRoad = AssetDatabase.LoadAssetAtPath<GameObject>(SidewalkRoadPath);
        GameObject dirtRoad = AssetDatabase.LoadAssetAtPath<GameObject>(DirtRoadPath);
        Assert.That(sidewalkRoad, Is.Not.Null);
        Assert.That(dirtRoad, Is.Not.Null);

        using var world = new World("DenseCityRoadShoulderRecordPlannerTests");
        var sidewalkVariants = world.CreateSystemManaged<RoadVisualVariantSystem>();
        var dirtVariants = world.CreateSystemManaged<RoadVisualVariantSystem>();
        try
        {
            RoadVisualVariantSystem.Prefabs sidewalkPrefabs = Repeat(sidewalkRoad);
            RoadVisualVariantSystem.Prefabs dirtPrefabs = Repeat(dirtRoad);
            sidewalkVariants.CacheVariants(sidewalkPrefabs);
            dirtVariants.CacheVariants(dirtPrefabs);
            Matrix4x4 roadMatrix = Matrix4x4.TRS(
                new Vector3(30f, 2f, 50f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(-1f, 1f, 1f));

            DenseCityRoadShoulderRecordInput[] sidewalkInputs =
                DenseCityRoadShoulderRecordPlanner.Create(
                    sidewalkVariants.VisualData[RoadNetworkCompositionSystemHelper.RoadVisualType.Straight]
                        .FootprintBounds,
                    roadMatrix,
                    new Vector2Int(1, 2));
            DenseCityRoadShoulderRecordInput[] dirtInputs =
                DenseCityRoadShoulderRecordPlanner.Create(
                    dirtVariants.VisualData[RoadNetworkCompositionSystemHelper.RoadVisualType.Straight]
                        .FootprintBounds,
                    roadMatrix,
                    new Vector2Int(1, 2));

            Assert.That(sidewalkInputs, Has.Length.EqualTo(4));
            Assert.That(dirtInputs, Is.Empty);
            for (int index = 0; index < sidewalkInputs.Length; index++)
            {
                Assert.That(sidewalkInputs[index].SurfaceSize.x, Is.GreaterThan(0f));
                Assert.That(sidewalkInputs[index].SurfaceSize.y, Is.GreaterThan(0f));
                Assert.That(sidewalkInputs[index].Elevation, Is.InRange(2f, 2.5f));
                Assert.That(sidewalkInputs[index].Chunk, Is.EqualTo(new Vector2Int(1, 2)));
            }
        }
        finally
        {
            sidewalkVariants.DisposeCachedVisualData();
            dirtVariants.DisposeCachedVisualData();
        }
    }

    [Test]
    public void Create_SortsTypedBoundsBeforeAssigningSequenceOrder()
    {
        var footprints = new[]
        {
            Footprint(new Vector3(4f, 0f, 1f)),
            Footprint(new Vector3(-4f, 0f, 1f)),
            Footprint(new Vector3(-4f, 0f, -1f))
        };

        DenseCityRoadShoulderRecordInput[] result = DenseCityRoadShoulderRecordPlanner.Create(
            footprints,
            Matrix4x4.identity,
            Vector2Int.zero);

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[0].WorldMatrix.GetColumn(3).x, Is.EqualTo(-4f));
        Assert.That(result[0].WorldMatrix.GetColumn(3).z, Is.EqualTo(-1f));
        Assert.That(result[1].WorldMatrix.GetColumn(3).z, Is.EqualTo(1f));
        Assert.That(result[2].WorldMatrix.GetColumn(3).x, Is.EqualTo(4f));
    }

    private static RoadGridProjectionSystem.RoadFootprintBoundsData Footprint(Vector3 center) =>
        new()
        {
            Bounds = new Bounds(center, new Vector3(2f, 0.2f, 4f)),
            Kind = RoadGridProjectionSystem.RoadFootprintKind.Sidewalk
        };

    private static RoadVisualVariantSystem.Prefabs Repeat(GameObject prefab) =>
        new(prefab, prefab, prefab, prefab, prefab, null, null);
}
