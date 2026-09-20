using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class MapSurfaceFloatingShelfCorrectionTests
{
    [Test]
    public void CollapsesUnsupportedRaisedShelfOntoAdjacentGrade()
    {
        const int width = 16;
        const int height = 12;
        var heights = new float[width * height];
        for (int y = 2; y <= 9; y++)
        for (int x = 2; x <= 12; x++)
            heights[x + y * width] = 5.85f;

        int flattened = MapSurfaceFloatingShelfCorrection.Apply(heights, width, height);

        Assert.GreaterOrEqual(flattened, 64);
        Assert.That(heights[7 + 9 * width], Is.EqualTo(0f).Within(0.001f));
        Assert.That(heights[2 + 2 * width], Is.EqualTo(0f).Within(0.001f));
        Assert.That(heights[0], Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void AbsorbsJustUnderThresholdRimBesideCollapsedShelf()
    {
        const int width = 16;
        const int height = 12;
        var heights = new float[width * height];
        for (int y = 2; y <= 9; y++)
        for (int x = 2; x <= 12; x++)
            heights[x + y * width] = 5.85f;
        for (int x = 2; x <= 12; x++)
            heights[x + 10 * width] = 3.7f;

        int flattened = MapSurfaceFloatingShelfCorrection.Apply(heights, width, height);

        Assert.GreaterOrEqual(flattened, 64 + 11);
        Assert.That(heights[7 + 10 * width], Is.EqualTo(0f).Within(0.001f));
        Assert.That(heights[2 + 10 * width], Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void AbsorbsTwoMetreLeftoverSkirtBesideCollapsedShelf()
    {
        const int width = 16;
        const int height = 12;
        var heights = new float[width * height];
        for (int y = 2; y <= 9; y++)
        for (int x = 2; x <= 12; x++)
            heights[x + y * width] = 5.85f;
        for (int x = 2; x <= 12; x++)
            heights[x + 10 * width] = 2.16f;

        int flattened = MapSurfaceFloatingShelfCorrection.Apply(heights, width, height);

        Assert.GreaterOrEqual(flattened, 64 + 11);
        Assert.That(heights[7 + 10 * width], Is.EqualTo(0f).Within(0.001f));
        Assert.That(heights[2 + 10 * width], Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void LowersStatueSideResidentPlinthAndHallOntoAuthoredGrade()
    {
        var hall = CityCrossroadsFloatingShelfBuildingCorrection.CivicHallPlate;
        var civicPlinth = CityCrossroadsFloatingShelfBuildingCorrection.CivicPlinthPlate;
        var northernPlinth = CityCrossroadsFloatingShelfBuildingCorrection.NorthernPlinthPlate;
        var mountain = new float3(768f, 9.1f, 550f);
        var gradeLot = new float3(1020f, 0.01f, 750f);

        Assert.IsTrue(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref hall));
        Assert.IsTrue(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref civicPlinth));
        Assert.IsTrue(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref northernPlinth));
        Assert.IsFalse(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref mountain));
        Assert.IsFalse(CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref gradeLot));

        Assert.That(hall.y, Is.EqualTo(CityCrossroadsFloatingShelfBuildingCorrection.TargetHeight).Within(0.001f));
        Assert.That(civicPlinth.y, Is.EqualTo(CityCrossroadsFloatingShelfBuildingCorrection.TargetHeight).Within(0.001f));
        Assert.That(northernPlinth.y, Is.EqualTo(CityCrossroadsFloatingShelfBuildingCorrection.TargetHeight).Within(0.001f));
        Assert.That(mountain.y, Is.EqualTo(9.1f).Within(0.001f));
        Assert.That(gradeLot.y, Is.EqualTo(0.01f).Within(0.001f));
        Assert.That(hall.x, Is.EqualTo(1031.03f).Within(0.001f));
        Assert.That(civicPlinth.z, Is.EqualTo(488.48f).Within(0.001f));
        Assert.That(northernPlinth.z, Is.EqualTo(687.84f).Within(0.001f));
    }

    [Test]
    public void PreservesGradualMountainRampAndPeak()
    {
        const int width = 16;
        const int height = 12;
        var heights = new float[width * height];
        for (int y = 2; y <= 9; y++)
        for (int x = 2; x <= 12; x++)
        {
            float tx = 1f - Mathf.Abs(x - 7) / 6f;
            float ty = 1f - Mathf.Abs(y - 5) / 5f;
            heights[x + y * width] = Mathf.Max(0f, tx * ty * 9f);
        }

        int flattened = MapSurfaceFloatingShelfCorrection.Apply(heights, width, height);

        Assert.Zero(flattened);
        Assert.Greater(heights[7 + 5 * width], 7f);
        Assert.Greater(heights[6 + 5 * width], 3f);
    }

    [Test]
    public void IgnoresSmallIsolatedSpikes()
    {
        const int width = 8;
        const int height = 8;
        var heights = new float[width * height];
        heights[3 + 4 * width] = 8f;
        heights[4 + 4 * width] = 8f;

        Assert.Zero(MapSurfaceFloatingShelfCorrection.Apply(heights, width, height));
        Assert.That(heights[3 + 4 * width], Is.EqualTo(8f).Within(0.001f));
    }

    [Test]
    public void CityCrossroadsNorthernShelfAgreesWithAuthoredGradeAfterLoad()
    {
        MapSurfaceDataAsset asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
        Assert.IsNotNull(asset);
        Assert.IsTrue(asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob));
        try
        {
            ref MapSurfaceBlob surface = ref blob.Value;
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1010, 710), out MapSurfaceSample shelfEdge));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1008, 708), out MapSurfaceSample shelfInterior));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1027, 481), out MapSurfaceSample civicShelf));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1035, 482), out MapSurfaceSample civicRim));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1031, 540), out MapSurfaceSample civicHall));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1043, 488), out MapSurfaceSample civicPlinth));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(965, 688), out MapSurfaceSample northernPlinth));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1000, 655), out MapSurfaceSample northSkirt));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1072, 520), out MapSurfaceSample civicSkirt));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1110, 600), out MapSurfaceSample eastSkirt));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1020, 750), out MapSurfaceSample northBase));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1100, 400), out MapSurfaceSample southBase));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(768, 550), out MapSurfaceSample mountain));

            Assert.Less(shelfEdge.Height, 1.5f, "Northern CC shelf edge must not remain a 5.85 m floating plateau.");
            Assert.Less(shelfInterior.Height, 1.5f, "Northern CC shelf interior must follow the authored city grade.");
            Assert.Less(civicShelf.Height, 1.5f, "Civic CC shelf must follow the authored city grade.");
            Assert.Less(civicRim.Height, 1.5f, "Civic CC 3.9 m leftover rim must collapse with the shelf.");
            Assert.Less(civicHall.Height, 1.5f, "Statue-side civic hall cell must follow authored grade.");
            Assert.Less(civicPlinth.Height, 1.5f, "Statue-side civic plinth cell must follow authored grade.");
            Assert.Less(northernPlinth.Height, 1.5f, "Northern statue-side plinth cell must follow authored grade.");
            Assert.Less(northSkirt.Height, 1.5f, "Northern 2 m leftover skirt must collapse with the shelf.");
            Assert.Less(civicSkirt.Height, 1.5f, "Civic 2 m leftover skirt must collapse with the shelf.");
            Assert.Less(eastSkirt.Height, 1.5f, "East 2 m leftover skirt must collapse with the shelf.");
            Assert.That(northBase.Height, Is.LessThan(0.75f));
            Assert.That(southBase.Height, Is.LessThan(0.75f));
            Assert.Greater(mountain.Height, 4f, "Skirmish mountain peak must remain raised.");

            var component = new MapSurfaceComponent
            {
                SurfaceBlob = blob,
                GridOrigin = asset.GridOrigin,
                CellSize = asset.CellSize,
                Dimensions = new int2(asset.Dimensions.x, asset.Dimensions.y),
                HasSurfaceData = 1
            };
            Assert.IsFalse(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                component, new RectInt(766, 550, 8, 8)));
            Assert.IsTrue(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                component, new RectInt(800, 600, 8, 8)));
            Assert.IsTrue(BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                component, new RectInt(1020, 730, 8, 8)),
                "City Crossroads northern barracks lot must remain a legal foundation.");
        }
        finally
        {
            blob.Dispose();
        }
    }
}
