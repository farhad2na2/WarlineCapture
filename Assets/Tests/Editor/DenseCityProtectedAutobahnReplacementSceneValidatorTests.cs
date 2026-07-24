using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityProtectedAutobahnReplacementSceneValidatorTests
{
    [TestCase("095cd66c53a054737955d9773c3d4060")]
    [TestCase("fa0a16026cf90474c84e43de668567d7")]
    [TestCase("8a34e9514dfe04fd7a308e8dded1b154")]
    [TestCase("65241ad8beab543e589a7e3c7334b214")]
    [TestCase("b4e31794b94814524a6f32f65cdd82d4")]
    public void ApprovedPrefabGuid_AcceptsDenseCityAsphaltFamily(string prefabGuid)
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator
                .IsApprovedRoadPrefabGuid(prefabGuid),
            Is.True);
        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator
                .IsApprovedRoadPrefabGuid("00000000000000000000000000000000"),
            Is.False);
    }

    [Test]
    public void Occupancy_AcceptsContinuousLanesAndConnectedCrossings()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateDescriptor();
        var cells = new List<Vector2Int>(descriptor.Cells)
        {
            new(63, 28),
            new(63, 27),
            new(142, 31),
            new(142, 32)
        };

        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator.TryValidateOccupancy(
                descriptor,
                cells,
                out DenseCityProtectedAutobahnOccupancyStats stats,
                out string error),
            Is.True,
            error);
        Assert.That(stats.LaneCellCount, Is.EqualTo(descriptor.Cells.Count));
        Assert.That(stats.ConnectorCellCount, Is.EqualTo(4));
        Assert.That(stats.ConnectorColumnCount, Is.EqualTo(2));
    }

    [Test]
    public void Occupancy_RejectsDuplicateCellOwners()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateDescriptor();
        var cells = new List<Vector2Int>(descriptor.Cells)
        {
            descriptor.Cells[0]
        };

        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator.TryValidateOccupancy(
                descriptor,
                cells,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("Duplicate"));
    }

    [Test]
    public void Occupancy_RejectsMissingLaneCell()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateDescriptor();
        var cells = new List<Vector2Int>(descriptor.Cells);
        cells.RemoveAt(cells.Count / 2);

        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator.TryValidateOccupancy(
                descriptor,
                cells,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("missing cell"));
    }

    [Test]
    public void Occupancy_RejectsConnectorGap()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateDescriptor();
        var cells = new List<Vector2Int>(descriptor.Cells)
        {
            new(142, 32)
        };

        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator.TryValidateOccupancy(
                descriptor,
                cells,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("gap"));
    }

    [Test]
    public void Occupancy_RejectsCellOutsideRouteSpan()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateDescriptor();
        var cells = new List<Vector2Int>(descriptor.Cells)
        {
            new(
                descriptor.LaneRanges[0].MaximumColumn + 1,
                descriptor.LaneRanges[0].Row)
        };

        Assert.That(
            DenseCityProtectedAutobahnReplacementSceneValidator.TryValidateOccupancy(
                descriptor,
                cells,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("exceeds"));
    }

    private static DenseCityProtectedAutobahnRouteDescriptor CreateDescriptor()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                new[]
                {
                    DenseCityProtectedAutobahnReplacementPlanner
                        .AcceptedWestSourceGlobalObjectId,
                    DenseCityProtectedAutobahnReplacementPlanner
                        .AcceptedEastSourceGlobalObjectId
                },
                new Vector2(256f, 128f),
                out DenseCityProtectedAutobahnRouteDescriptor descriptor,
                out string error),
            Is.True,
            error);
        return descriptor;
    }
}
