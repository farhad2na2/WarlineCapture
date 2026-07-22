using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityBuildingRecordFactoryTests
{
    private const string IntactGuid = "0123456789abcdef0123456789abcdef";
    private const string DestroyedGuid = "fedcba9876543210fedcba9876543210";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void Create_ProducesLinkedStableFiveRecordGroup()
    {
        DenseCityBuildingRecordGroup group = DenseCityBuildingRecordFactory.Create(CreateInput());

        Assert.That(group.Building.Identity.DeterministicSequence, Is.EqualTo(20));
        Assert.That(group.Building.OriginCell, Is.EqualTo(new Vector2Int(12, 18)));
        Assert.That(group.Building.FootprintCells, Is.EqualTo(new Vector2Int(8, 6)));
        Assert.That(group.Foundation.Identity, Is.EqualTo(group.Building.FoundationSurfaceIdentity));
        Assert.That(group.Blocker.Identity, Is.EqualTo(group.Building.BlockerSurfaceIdentity));
        Assert.That(group.IntactPresentation.Identity, Is.EqualTo(group.Building.IntactPresentationIdentity));
        Assert.That(group.DestroyedPresentation.Identity, Is.EqualTo(group.Building.DestroyedPresentationIdentity));
        Assert.That(group.DestroyedPresentation.PrefabAssetGuid, Is.EqualTo(DestroyedGuid));
        Vector2 firstCorner = group.Foundation.Polygon.Span[0];
        Assert.That(firstCorner, Is.EqualTo(new Vector2(7f, 24f)).Using(Vector2Comparer));
    }

    [Test]
    public void Add_CommitsFactoryGroupAtomically()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 2);
        DenseCityBuildingRecordFactory.Add(records, DenseCityBuildingRecordFactory.Create(CreateInput()));
        records.Seal();

        Assert.That(records.Buildings, Has.Count.EqualTo(1));
        Assert.That(records.Surfaces, Has.Count.EqualTo(2));
        Assert.That(records.Presentations, Has.Count.EqualTo(2));
    }

    private static DenseCityBuildingRecordInput CreateInput() =>
        new(
            "dense-city-v1",
            42,
            3,
            20,
            IntactGuid,
            123,
            DestroyedGuid,
            456,
            new[] { MaterialGuid },
            new[] { MaterialGuid },
            Matrix4x4.TRS(new Vector3(10f, 2f, 20f), Quaternion.Euler(0f, 90f, 0f), Vector3.one),
            new Vector2Int(12, 18),
            new Vector2Int(8, 6),
            new Vector2(8f, 6f),
            2f,
            new Bounds(new Vector3(10f, 4f, 20f), new Vector3(6f, 4f, 8f)),
            Vector3.right,
            0,
            500f,
            1,
            0,
            new Vector2Int(1, 2));

    private static readonly IEqualityComparer<Vector2> Vector2Comparer =
        new Vector2EqualityComparer(0.0001f);

    private sealed class Vector2EqualityComparer : IEqualityComparer<Vector2>
    {
        private readonly float tolerance;

        internal Vector2EqualityComparer(float tolerance) => this.tolerance = tolerance;

        public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) <= tolerance;

        public int GetHashCode(Vector2 value) => value.GetHashCode();
    }
}
