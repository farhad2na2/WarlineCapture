using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityBuildingRoadClearanceTests
{
    public static void RunFocusedValidation()
    {
        var suite = new DenseCityBuildingRoadClearanceTests();
        suite.StandardRoadBounds_AreCenteredOnTheRoadCellVisual();
        suite.DirtRoadBounds_AreCenteredOnTheRoadCellVisual();
        suite.RoadBounds_RejectInvalidExtents();
        Debug.Log("[DenseCityBuildingRoadClearanceValidation] result=Passed tests=3");
    }

    [Test]
    public void StandardRoadBounds_AreCenteredOnTheRoadCellVisual()
    {
        Rect bounds = DenseMiddleEasternCityEditModeBuilder.CreateRoadVisualBounds(
            new Vector2Int(3, 7),
            new Vector3(100f, 4f, -20f),
            false,
            9f,
            5f);

        Assert.That(bounds.center.x, Is.EqualTo(135f).Within(0.0001f));
        Assert.That(bounds.center.y, Is.EqualTo(55f).Within(0.0001f));
        Assert.That(bounds.size, Is.EqualTo(new Vector2(18f, 18f)));
    }

    [Test]
    public void DirtRoadBounds_AreCenteredOnTheRoadCellVisual()
    {
        Rect bounds = DenseMiddleEasternCityEditModeBuilder.CreateRoadVisualBounds(
            new Vector2Int(-2, 1),
            new Vector3(10f, 0f, 30f),
            true,
            9f,
            5f);

        Assert.That(bounds.center.x, Is.EqualTo(-5f).Within(0.0001f));
        Assert.That(bounds.center.y, Is.EqualTo(45f).Within(0.0001f));
        Assert.That(bounds.size, Is.EqualTo(new Vector2(10f, 10f)));
    }

    [Test]
    public void RoadBounds_RejectInvalidExtents()
    {
        Assert.That(
            () => DenseMiddleEasternCityEditModeBuilder.CreateRoadVisualBounds(
                Vector2Int.zero,
                Vector3.zero,
                false,
                0f,
                5f),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => DenseMiddleEasternCityEditModeBuilder.CreateRoadVisualBounds(
                Vector2Int.zero,
                Vector3.zero,
                true,
                9f,
                float.NaN),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
