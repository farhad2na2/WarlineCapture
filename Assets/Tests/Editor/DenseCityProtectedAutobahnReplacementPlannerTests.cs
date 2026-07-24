using System;
using System.Collections.Generic;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityProtectedAutobahnReplacementPlannerTests
{
    private static readonly string[] AcceptedSourceIds =
    {
        DenseCityProtectedAutobahnReplacementPlanner.AcceptedWestSourceGlobalObjectId,
        DenseCityProtectedAutobahnReplacementPlanner.AcceptedEastSourceGlobalObjectId
    };

    public static void RunFocusedValidation()
    {
        try
        {
            var suite = new DenseCityProtectedAutobahnReplacementPlannerTests();
            Action[] tests =
            {
                suite.Constants_MatchAcceptedAutobahnOwnersAndLegacyBounds,
                suite.TryCreate_QuantizesLegacyBoundsIntoSortedAdjacentLanes,
                suite.TryCreate_IsDeterministicAndDescriptorCollectionsAreReadOnly,
                suite.TryCreate_RejectsMissingSourceId,
                suite.TryCreate_RejectsWrongSourceId,
                suite.TryCreate_RejectsDuplicateSourceId,
                suite.TryValidate_RejectsNonAdjacentLaneRows,
                suite.TryValidate_RejectsDiscontinuousCellRange,
                suite.AddReplacement_ConnectsVerticalCrossingsAndIgnoresParallelRoads
            };
            for (int index = 0; index < tests.Length; index++)
                tests[index]();

            Debug.Log(
                $"[DenseCityProtectedAutobahnReplacementPlannerValidation] " +
                $"result=Passed tests={tests.Length}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[DenseCityProtectedAutobahnReplacementPlannerValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void Constants_MatchAcceptedAutobahnOwnersAndLegacyBounds()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.AcceptedWestSourceGlobalObjectId,
            Is.EqualTo(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1327110974329158-1224302320877551806"));
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.AcceptedEastSourceGlobalObjectId,
            Is.EqualTo(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1327110974329158-1314812446050087256"));
        Assert.That(DenseCityProtectedAutobahnReplacementPlanner.CellSize, Is.EqualTo(10f));
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.LegacyMinimumWorldX,
            Is.EqualTo(-1700f));
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.LegacyMaximumWorldX,
            Is.EqualTo(3100f));
    }

    [Test]
    public void TryCreate_QuantizesLegacyBoundsIntoSortedAdjacentLanes()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                AcceptedSourceIds,
                new Vector2(256f, 128f),
                out DenseCityProtectedAutobahnRouteDescriptor descriptor,
                out string error),
            Is.True,
            error);

        Assert.That(descriptor.LaneRanges, Has.Count.EqualTo(2));
        Assert.That(descriptor.LaneRanges[0].Row, Is.EqualTo(29));
        Assert.That(descriptor.LaneRanges[1].Row, Is.EqualTo(30));
        Assert.That(descriptor.LaneRanges[0].MinimumColumn, Is.EqualTo(-196));
        Assert.That(descriptor.LaneRanges[0].MaximumColumn, Is.EqualTo(284));
        Assert.That(descriptor.Cells, Has.Count.EqualTo(962));
        Assert.That(descriptor.Cells[0], Is.EqualTo(new Vector2Int(-196, 29)));
        Assert.That(descriptor.Cells[480], Is.EqualTo(new Vector2Int(284, 29)));
        Assert.That(descriptor.Cells[481], Is.EqualTo(new Vector2Int(-196, 30)));
        Assert.That(descriptor.Cells[961], Is.EqualTo(new Vector2Int(284, 30)));
        Assert.That(
            descriptor.GetWorldPlacement(descriptor.Cells[0]),
            Is.EqualTo(new Vector2(-1704f, 418f)));
        Assert.That(
            descriptor.GetWorldPlacement(descriptor.Cells[961]),
            Is.EqualTo(new Vector2(3096f, 428f)));
    }

    [Test]
    public void TryCreate_IsDeterministicAndDescriptorCollectionsAreReadOnly()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                new[]
                {
                    AcceptedSourceIds[1],
                    AcceptedSourceIds[0]
                },
                Vector2.zero,
                out DenseCityProtectedAutobahnRouteDescriptor first,
                out string error),
            Is.True,
            error);
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                AcceptedSourceIds,
                Vector2.zero,
                out DenseCityProtectedAutobahnRouteDescriptor second,
                out error),
            Is.True,
            error);

        CollectionAssert.AreEqual(first.SourceGlobalObjectIds, second.SourceGlobalObjectIds);
        CollectionAssert.AreEqual(first.LaneRanges, second.LaneRanges);
        CollectionAssert.AreEqual(first.Cells, second.Cells);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<Vector2Int>)first.Cells).Add(Vector2Int.zero));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)first.SourceGlobalObjectIds)[0] = "mutated");
    }

    [Test]
    public void TryCreate_RejectsMissingSourceId()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                new[] { AcceptedSourceIds[0] },
                Vector2.zero,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("exactly two"));
    }

    [Test]
    public void TryCreate_RejectsWrongSourceId()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                new[] { AcceptedSourceIds[0], "GlobalObjectId_V1-wrong" },
                Vector2.zero,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("Unexpected"));
    }

    [Test]
    public void TryCreate_RejectsDuplicateSourceId()
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                new[] { AcceptedSourceIds[0], AcceptedSourceIds[0] },
                Vector2.zero,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("duplicates"));
    }

    [Test]
    public void TryValidate_RejectsNonAdjacentLaneRows()
    {
        DenseCityProtectedAutobahnRouteDescriptor valid = CreateValidDescriptor();
        var ranges = new[]
        {
            valid.LaneRanges[0],
            new DenseCityProtectedAutobahnLaneRange(
                valid.LaneRanges[0].Row + 2,
                valid.LaneRanges[1].MinimumColumn,
                valid.LaneRanges[1].MaximumColumn)
        };
        var malformed = new DenseCityProtectedAutobahnRouteDescriptor(
            valid.GridOrigin,
            valid.SourceGlobalObjectIds,
            ranges,
            valid.Cells);

        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryValidate(
                malformed,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("adjacent"));
    }

    [Test]
    public void TryValidate_RejectsDiscontinuousCellRange()
    {
        DenseCityProtectedAutobahnRouteDescriptor valid = CreateValidDescriptor();
        var cells = new List<Vector2Int>(valid.Cells);
        cells.RemoveAt(12);
        var malformed = new DenseCityProtectedAutobahnRouteDescriptor(
            valid.GridOrigin,
            valid.SourceGlobalObjectIds,
            valid.LaneRanges,
            cells);

        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryValidate(
                malformed,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("cell count"));
    }

    [Test]
    public void AddReplacement_ConnectsVerticalCrossingsAndIgnoresParallelRoads()
    {
        DenseCityProtectedAutobahnRouteDescriptor descriptor = CreateValidDescriptor(
            new Vector2(256f, 128f));
        var network = new RoadNetworkCompositionSystemHelper();
        CreateStroke(network, column: 142, minimumRow: 25, maximumRow: 26);
        CreateStroke(network, column: 142, minimumRow: 34, maximumRow: 36);
        CreateStroke(network, column: 63, minimumRow: 34, maximumRow: 37);
        network.CreateStroke(
            CreateHorizontalStroke(40, 55, 90),
            false,
            false,
            false,
            out _);

        HashSet<Vector2Int> replacementCells =
            DenseMiddleEasternCityEditModeBuilder.AddProtectedAutobahnReplacement(
                network,
                descriptor);

        for (int column = -196; column <= 284; column++)
        {
            Assert.That(
                network.HasEdge(
                    new Vector2Int(column, 29),
                    new Vector2Int(column + 1, 29)),
                Is.EqualTo(column < 284));
            Assert.That(
                network.HasEdge(
                    new Vector2Int(column, 30),
                    new Vector2Int(column + 1, 30)),
                Is.EqualTo(column < 284));
        }

        Assert.That(network.GetMask(new Vector2Int(142, 29)).Count, Is.EqualTo(4));
        Assert.That(network.GetMask(new Vector2Int(142, 30)).Count, Is.EqualTo(4));
        Assert.That(network.GetMask(new Vector2Int(63, 29)).Count, Is.EqualTo(3));
        Assert.That(network.GetMask(new Vector2Int(63, 30)).Count, Is.EqualTo(4));
        Assert.That(network.StrokeIdsByCell.ContainsKey(new Vector2Int(64, 31)), Is.False);
        Assert.That(replacementCells.Contains(new Vector2Int(142, 27)), Is.True);
        Assert.That(replacementCells.Contains(new Vector2Int(63, 33)), Is.True);
        Assert.That(replacementCells.Contains(new Vector2Int(142, 26)), Is.False);
    }

    private static DenseCityProtectedAutobahnRouteDescriptor CreateValidDescriptor(
        Vector2? gridOrigin = null)
    {
        Assert.That(
            DenseCityProtectedAutobahnReplacementPlanner.TryCreate(
                AcceptedSourceIds,
                gridOrigin ?? Vector2.zero,
                out DenseCityProtectedAutobahnRouteDescriptor descriptor,
                out string error),
            Is.True,
            error);
        return descriptor;
    }

    private static void CreateStroke(
        RoadNetworkCompositionSystemHelper network,
        int column,
        int minimumRow,
        int maximumRow)
    {
        var cells = new List<Vector2Int>(maximumRow - minimumRow + 1);
        for (int row = minimumRow; row <= maximumRow; row++)
            cells.Add(new Vector2Int(column, row));
        Assert.That(
            network.CreateStroke(cells, false, false, false, out _),
            Is.True);
    }

    private static List<Vector2Int> CreateHorizontalStroke(
        int row,
        int minimumColumn,
        int maximumColumn)
    {
        var cells = new List<Vector2Int>(maximumColumn - minimumColumn + 1);
        for (int column = minimumColumn; column <= maximumColumn; column++)
            cells.Add(new Vector2Int(column, row));
        return cells;
    }
}
