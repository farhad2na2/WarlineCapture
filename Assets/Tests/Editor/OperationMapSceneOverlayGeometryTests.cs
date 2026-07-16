using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapSceneOverlayGeometryTests
{
    [Test]
    public void BoundsGeometry_UsesExactCenterAndSize()
    {
        Assert.That(OperationMapSceneOverlayGeometry.TryCreateBounds(
            new Vector3(-10f, -2f, 5f),
            new Vector3(30f, 8f, 45f),
            out Bounds bounds), Is.True);
        Assert.That(bounds.center, Is.EqualTo(new Vector3(10f, 3f, 25f)));
        Assert.That(bounds.size, Is.EqualTo(new Vector3(40f, 10f, 40f)));
    }

    [Test]
    public void MinimapGeometry_ZeroAndNinetyDegreeOrientationsAreDeterministic()
    {
        Vector3[] corners = new Vector3[5];
        Assert.That(OperationMapSceneOverlayGeometry.TryCreateHorizontalRectangle(
            new Vector3(10f, 2f, 20f),
            new Vector2(8f, 4f),
            0f,
            corners), Is.True);
        Assert.That(corners[2], Is.EqualTo(new Vector3(18f, 2f, 24f)));
        Assert.That(corners[4], Is.EqualTo(corners[0]));

        Assert.That(OperationMapSceneOverlayGeometry.TryCreateHorizontalRectangle(
            new Vector3(10f, 2f, 20f),
            new Vector2(8f, 4f),
            90f,
            corners), Is.True);
        Assert.That(Vector3.Distance(corners[1], new Vector3(10f, 2f, 12f)), Is.LessThan(0.0001f));
        Assert.That(Vector3.Distance(corners[2], new Vector3(14f, 2f, 12f)), Is.LessThan(0.0001f));
    }

    [Test]
    public void InvalidGeometry_FailsClosedWithoutChangingCallerCapacity()
    {
        Assert.That(OperationMapSceneOverlayGeometry.TryCreateBounds(
            Vector3.zero,
            new Vector3(0f, 1f, 2f),
            out _), Is.False);
        Assert.That(OperationMapSceneOverlayGeometry.TryCreateHorizontalRectangle(
            Vector3.zero,
            new Vector2(10f, 0f),
            0f,
            new Vector3[5]), Is.False);
        Assert.That(OperationMapSceneOverlayGeometry.TryCreateHorizontalRectangle(
            Vector3.zero,
            Vector2.one,
            float.NaN,
            new Vector3[4]), Is.False);
    }

    [Test]
    public void LaneAndAircraftAnchorsUseDistinctDebugColors()
    {
        Color lane = OperationMapSceneViewEditor.ResolveAnchorColor(OperationMapAnchorKind.Lane);
        Color runway = OperationMapSceneViewEditor.ResolveAnchorColor(OperationMapAnchorKind.Runway);
        Color helipad = OperationMapSceneViewEditor.ResolveAnchorColor(OperationMapAnchorKind.Helipad);

        Assert.That(lane, Is.Not.EqualTo(runway));
        Assert.That(runway, Is.Not.EqualTo(helipad));
        Assert.That(helipad, Is.Not.EqualTo(lane));
    }
}
