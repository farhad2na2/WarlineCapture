#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementValidationSystemTests
{
    private World _world;

    public static void RunPlacementCommandRequestValidation()
    {
        try
        {
            var tests = new BuildingPlacementValidationSystemTests();
            tests.BuildingUiPlacementCommandRequest_RejectsMissingSession();
            tests.BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag();
            Debug.Log("[BuildingPlacementCommandRequestValidation] result=Passed tests=3");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingPlacementCommandRequestValidation] result=Failed");
            UnityEditor.EditorApplication.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _world = null;
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_RejectsMissingSession()
    {
        using World world = new("BuildingUiPlacementCommandMissingSessionTest");
        var commandSystem = new BuildingPlacementCommandSystem();
        BuildingPlacementCommandSystem.Context context = default;

        int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindConfirmPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.MissingSession, result.ResultCode);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandCancelTest");
        var commandSystem = new BuildingPlacementCommandSystem();
        bool commandModeCleared = false;
        BuildingPlacementCommandSystem.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionSystem(),
            clearCommandMode: () => commandModeCleared = true);

        int requestId = commandSystem.EnqueueCancelBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindCancelPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, result.ResultCode);
        Assert.IsTrue(commandModeCleared);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag()
    {
        using World world = new("BuildingUiPlacementCommandExitBuildModeTest");
        var commandSystem = new BuildingPlacementCommandSystem();
        int clearSelectionCount = 0;
        BuildingPlacementCommandSystem.Context context = CreatePlacementCommandContext(
            new BuildingPlacementSessionSystem(),
            clearSelectedBuilding: _ => clearSelectionCount++);

        int preservedSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: false);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            preservedSelectionRequestId,
            out BuildingUiPlacementCommandResultElement preservedSelectionResult));
        Assert.AreEqual(1, preservedSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, preservedSelectionResult.ResultCode);
        Assert.AreEqual(0, clearSelectionCount);

        int clearSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: true);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            clearSelectionRequestId,
            out BuildingUiPlacementCommandResultElement clearSelectionResult));
        Assert.AreEqual(1, clearSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, clearSelectionResult.ResultCode);
        Assert.AreEqual(1, clearSelectionCount);
    }

    [Test]
    public void PlacementRectValidation_RejectsRoadCellsAndRuntimeBuildingOverlap()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        roads[GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 1), grid.Width)] = new GridRoad { Value = 1 };
        DynamicBlockerComponent blockerData = default;

        Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
            new RectInt(1, 1, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            null));

        Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
            new RectInt(2, 2, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            rect => rect.position == new Vector2Int(2, 2)));
    }

    [Test]
    public void PlacementRectValidation_AllowsRuntimeBlockerCellsButRejectsStaticBlockers()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        int blockedIndex = GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 2), grid.Width);
        var blocked = new NativeBitArray(16, Allocator.Persistent);
        blocked.Set(blockedIndex, true);
        DynamicBlockerComponent blockerData = new() { GridSize = 16, Blocked = blocked };

        try
        {
            Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                null,
                null,
                null));

            Assert.IsTrue(BuildingPlacementValidationSystem.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                (x, y, _, _) => x == 1 && y == 2,
                null,
                null));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
        }
    }

    [Test]
    public void InvalidPrefix_DetectsRoadFootprintMaskAndOutOfBounds()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        bool[] roadFootprintMask = new bool[16];
        roadFootprintMask[GridUtils.CellToIndex(new Unity.Mathematics.int2(2, 1), grid.Width)] = true;
        int[] prefix = null;

        BuildingPlacementValidationSystem.RebuildInvalidPrefix(
            grid,
            roads,
            default,
            roadFootprintMask,
            null,
            ref prefix,
            out int prefixWidth,
            out int prefixHeight,
            out bool hasPrefix);

        Assert.IsTrue(hasPrefix);
        Assert.IsFalse(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(0, 0), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(2, 1), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(4, 4), new Vector2Int(1, 1)));
    }

    [Test]
    public void WallSegmentConflict_OnlyRejectsOverlappingSameAxisSegments()
    {
        Assert.IsTrue(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(1, 4),
            true));

        Assert.IsFalse(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(4, 1),
            false));

        Assert.IsFalse(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(3, 1),
            new Vector2Int(1, 4),
            true));
    }

    private static BuildingPlacementCommandSystem.Context CreatePlacementCommandContext(
        BuildingPlacementSessionSystem sessionSystem,
        Action<string> clearSelectedBuilding = null,
        Action clearCommandMode = null)
    {
        var runtimeStateSystem = new RuntimeGameplayStateSystem();
        var lifecycleSystem = new BuildingPlacementLifecycleSystem();
        BuildingPlacementSessionSystem.Context sessionContext = new(
            runtimeStateSystem,
            lifecycleSystem,
            null,
            null,
            () => new BuildingPlacementLifecycleSystem.CancelContext(null, null, null),
            () => default,
            () => default,
            () => default,
            null,
            null,
            clearSelectedBuilding,
            clearCommandMode);

        return new BuildingPlacementCommandSystem.Context(
            null,
            null,
            sessionSystem,
            sessionContext,
            null);
    }

    private void CreateRoadBuffer(int width, int height, out GridConfig grid, out DynamicBuffer<GridRoad> roads)
    {
        _world ??= new World("BuildingPlacementValidationSystemTests");
        Entity entity = _world.EntityManager.CreateEntity();
        roads = _world.EntityManager.AddBuffer<GridRoad>(entity);
        roads.ResizeUninitialized(width * height);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = default;

        grid = new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = default
        };
    }
}
#endif
