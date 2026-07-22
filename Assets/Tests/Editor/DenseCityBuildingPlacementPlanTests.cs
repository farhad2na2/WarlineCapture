using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

public sealed class DenseCityBuildingPlacementPlanTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new DenseCityBuildingPlacementPlanTests();
            tests.Create_ReproducesGridAndFrontagePlacementWithoutRealization();
            tests.Create_RejectsNonQuarterTurnAndOversizedFootprint();
            Debug.Log("[DenseCityBuildingPlacementPlanValidation] result=Passed tests=2");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DenseCityBuildingPlacementPlanValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Create_ReproducesGridAndFrontagePlacementWithoutRealization()
    {
        var grid = new GridConfig
        {
            Width = 256,
            Height = 256,
            CellSize = 1f,
            Origin = new float3(-128f, 0f, -128f)
        };

        DenseCityBuildingPlacementPlan plan = DenseCityBuildingPlacementPlanner.Create(
            new Vector3(10.2f, 0f, 20.3f),
            90f,
            8f,
            12f,
            6f,
            0.82f,
            2.035f,
            grid,
            DenseCityFrontageEdge.MinimumX,
            5f);

        Assert.That(plan.OriginCell, Is.EqualTo(new Vector2Int(132, 144)));
        Assert.That(plan.FootprintCells, Is.EqualTo(new Vector2Int(12, 8)));
        Assert.That(plan.WorldMatrix.GetColumn(3), Is.EqualTo(new Vector4(11f, 2.035f, 20f, 1f)));
        Assert.That(plan.BlockerBounds.min.x, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(plan.BlockerBounds.size, Is.EqualTo(new Vector3(12f, 6f, 8f)));
        Assert.That(plan.FootprintSize, Is.EqualTo(new Vector2(12f, 8f)));
        Assert.That(plan.FrontageDirection, Is.EqualTo(Vector3.left));
        Assert.That(plan.Chunk, Is.EqualTo(Vector2Int.zero));
    }

    [Test]
    public void Create_RejectsNonQuarterTurnAndOversizedFootprint()
    {
        var grid = new GridConfig
        {
            Width = 10,
            Height = 10,
            CellSize = 1f,
            Origin = float3.zero
        };

        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            DenseCityBuildingPlacementPlanner.Create(
                Vector3.zero, 45f, 3f, 3f, 3f, 1f, 0f, grid,
                DenseCityFrontageEdge.None, 0f));
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            DenseCityBuildingPlacementPlanner.Create(
                Vector3.zero, 0f, 11f, 3f, 3f, 1f, 0f, grid,
                DenseCityFrontageEdge.None, 0f));
    }
}
