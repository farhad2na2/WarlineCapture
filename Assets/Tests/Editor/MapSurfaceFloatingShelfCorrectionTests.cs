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
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1020, 750), out MapSurfaceSample northBase));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(1100, 400), out MapSurfaceSample southBase));
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(768, 550), out MapSurfaceSample mountain));

            Assert.Less(shelfEdge.Height, 1.5f, "Northern CC shelf edge must not remain a 5.85 m floating plateau.");
            Assert.Less(shelfInterior.Height, 1.5f, "Northern CC shelf interior must follow the authored city grade.");
            Assert.Less(civicShelf.Height, 1.5f, "Civic CC shelf must follow the authored city grade.");
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
