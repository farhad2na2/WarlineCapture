using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityInfrastructureRecordFactoryTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void CreateVisualized_ProducesLinkedRoadSurfaceAndInfrastructurePresentation()
    {
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(20f, 3f, 40f),
            Quaternion.Euler(0f, 90f, 0f),
            new Vector3(2f, 1f, 3f));

        DenseCityInfrastructureRecordGroup group =
            DenseCityInfrastructureRecordFactory.CreateVisualized(
                CreateInput(
                    10,
                    "road",
                    DenseCitySurfaceRecordKind.Road,
                    matrix,
                    new Vector2(12f, 8f)));

        Assert.That(group.Surface.Kind, Is.EqualTo(DenseCitySurfaceRecordKind.Road));
        Assert.That(group.Surface.Identity.DeterministicSequence, Is.EqualTo(10));
        Assert.That(group.Presentation.Identity.DeterministicSequence, Is.EqualTo(11));
        Assert.That(group.Presentation.Category, Is.EqualTo(DenseCityPresentationCategory.Infrastructure));
        Assert.That(group.Presentation.WorldMatrix, Is.EqualTo(matrix));
        Assert.That(group.Surface.Polygon.Length, Is.EqualTo(4));
        Assert.That(group.Surface.Polygon.Span[0], Is.EqualTo(new Vector2(16f, 46f)).Using(Vector2Comparer));
        Assert.That(group.Surface.Polygon.Span[2], Is.EqualTo(new Vector2(24f, 34f)).Using(Vector2Comparer));
    }

    [Test]
    public void CreateSurfaceOnlyRamp_ProducesNoInventedPresentation()
    {
        DenseCitySurfaceBakeRecord ramp = DenseCityInfrastructureRecordFactory.CreateSurfaceOnlyRamp(
            CreateInput(
                20,
                "bridge-ramp-west",
                DenseCitySurfaceRecordKind.Ramp,
                Matrix4x4.identity,
                new Vector2(10f, 14f)));

        Assert.That(ramp.Kind, Is.EqualTo(DenseCitySurfaceRecordKind.Ramp));
        Assert.That(ramp.Identity.DeterministicSequence, Is.EqualTo(20));
        Assert.That(ramp.Polygon.Length, Is.EqualTo(4));
    }

    [Test]
    public void CreateSurfaceOnlyRamp_RejectsVisualizedSurfaceKinds()
    {
        Assert.That(
            () => DenseCityInfrastructureRecordFactory.CreateSurfaceOnlyRamp(
                CreateInput(
                    20,
                    "road",
                    DenseCitySurfaceRecordKind.Road,
                    Matrix4x4.identity,
                    Vector2.one)),
            Throws.ArgumentException);
    }

    private static DenseCityInfrastructureRecordInput CreateInput(
        int sequence,
        string recordKind,
        DenseCitySurfaceRecordKind surfaceKind,
        Matrix4x4 worldMatrix,
        Vector2 surfaceSize) =>
        new(
            "dense-city-v1",
            42,
            3,
            sequence,
            recordKind,
            surfaceKind,
            SourceGuid,
            123,
            new[] { MaterialGuid },
            worldMatrix,
            surfaceSize,
            3f,
            1,
            0,
            new Vector2Int(1, 2),
            true,
            true,
            2);

    private static readonly IEqualityComparer<Vector2> Vector2Comparer =
        new ApproximateVector2Comparer();

    private sealed class ApproximateVector2Comparer : IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) < 0.0001f;
        public int GetHashCode(Vector2 value) => value.GetHashCode();
    }
}
