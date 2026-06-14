#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class UnitMoveOrderSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.GetManualMoveFormationOffset_UsesPaddedFootprintStride());
            RunCase(test => test.BuildSelectedCurrentFootprintCells_UsesClampedFootprintsWithinGrid());
            RunCase(test => test.IssueImmediateMoveCommand_GroundUnitWritesTargetPathRequestAndManualTag());
            RunCase(test => test.IssueTargetOnlyMoveCommand_WritesTargetAndClearsConflictingOrders());
            RunCase(test => test.IssueGroupedManualMoveOrder_StaggeredGroundUnitUsesRetryCooldownInsteadOfPathRequest());
            RunCase(test => test.IssueGroupedManualMoveOrder_StaggeredGroundUnitReplacesExistingRetryCooldown());
            RunCase(test => test.UnitMoveOrderRequestSystem_GroupedManualRequestWritesResultAndMoveComponents());
            RunCase(test => test.UnitMoveOrderRequestSystem_TargetPathRequestWritesOnlyTargetAndPath());
            RunCase(test => test.ClearMovementOrderComponents_RemovesSharedMoveOrderComponents());
            RunCase(test => test.UnitMoveOrderRequestSystem_ClearMovementRequestRemovesSharedMoveOrderComponents());
            RunCase(test => test.SelectedMoveOrderCommand_IssuesMoveOrderForSelectedUnit());
            RunCase(test => test.SelectedMoveOrderCommand_RefreshesCommandBuffersAfterStructuralMoveOrder());
            RunCase(test => test.BuildingTargetMoveOrder_IssuesApproachCellMoveOrderForSelectedUnit());
            UnityEngine.Debug.Log("[UnitMoveOrderFocusedValidation] result=Passed tests=13");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[UnitMoveOrderFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<UnitMoveOrderSystemTests> testCase)
    {
        UnitMoveOrderSystemTests tests = new();
        try
        {
            tests.SetUp();
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("UnitMoveOrderSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        _world?.Dispose();
    }

    [Test]
    public void GetManualMoveFormationOffset_UsesPaddedFootprintStride()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();

        Assert.AreEqual(new int2(0, 0), moveOrderSystem.GetManualMoveFormationOffset(0, new int2(1, 1), 1));
        Assert.AreEqual(new int2(-3, 3), moveOrderSystem.GetManualMoveFormationOffset(1, new int2(1, 1), 1));
        Assert.AreEqual(new int2(0, 3), moveOrderSystem.GetManualMoveFormationOffset(2, new int2(1, 1), 1));
        Assert.AreEqual(new int2(3, 3), moveOrderSystem.GetManualMoveFormationOffset(3, new int2(1, 1), 1));
    }

    [Test]
    public void BuildSelectedCurrentFootprintCells_UsesClampedFootprintsWithinGrid()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        GridConfig grid = new() { Width = 5, Height = 5, CellSize = 1f };
        Entity unit = _entityManager.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(2, 2) });

        NativeArray<Entity> entities = new(1, Allocator.Temp);
        try
        {
            entities[0] = unit;
            var cells = moveOrderSystem.BuildSelectedCurrentFootprintCells(_entityManager, grid, entities);

            CollectionAssert.AreEquivalent(new[] { 12, 13, 17, 18 }, cells);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [Test]
    public void IssueImmediateMoveCommand_GroundUnitWritesTargetPathRequestAndManualTag()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitTarget),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(UnitPathRange),
            typeof(EngageTarget),
            typeof(AutoWanderMoveTag),
            typeof(HoldPositionOrderTag),
            typeof(UnitPathRetryCooldown),
            typeof(UnitLongDistanceMove),
            typeof(BaseBreachOrder),
            typeof(UnitTransportBoardingTarget),
            typeof(UnitResourceHaulOrder));
        int2 goal = new(4, 5);

        moveOrderSystem.IssueImmediateMoveCommand(_entityManager, unit, goal);

        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitLongDistanceMove>(unit));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(unit));
    }

    [Test]
    public void IssueTargetOnlyMoveCommand_WritesTargetAndClearsConflictingOrders()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitPathRequest),
            typeof(HoldPositionOrderTag),
            typeof(BaseBreachOrder),
            typeof(UnitTransportBoardingTarget),
            typeof(UnitTransportRopeDisembarkRequest),
            typeof(UnitResourceHaulOrder));
        int2 goal = new(9, 10);
        int2 existingPathGoal = new(2, 3);
        _entityManager.SetComponentData(unit, new UnitPathRequest { Goal = existingPathGoal });

        moveOrderSystem.IssueTargetOnlyMoveCommand(_entityManager, unit, goal);

        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(existingPathGoal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportRopeDisembarkRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(unit));
    }

    [Test]
    public void IssueGroupedManualMoveOrder_StaggeredGroundUnitUsesRetryCooldownInsteadOfPathRequest()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(typeof(UnitPathRequest));
        int2 goal = new(7, 8);

        UnitMoveOrderSystem.MoveOrderCommandResult result = moveOrderSystem.IssueGroupedManualMoveOrder(
            _entityManager,
            unit,
            goal,
            issueGroundPathNow: false,
            useGroundPathRetryCooldown: true,
            resumeFrame: 22,
            currentFrame: 10);

        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(22, _entityManager.GetComponentData<UnitPathRetryCooldown>(unit).ResumeFrame);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(1, result.StaggeredPathRequests);
        Assert.AreEqual(12, result.MaxStaggerDelayFrames);
    }

    [Test]
    public void IssueGroupedManualMoveOrder_StaggeredGroundUnitReplacesExistingRetryCooldown()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(typeof(UnitPathRetryCooldown));
        _entityManager.SetComponentData(unit, new UnitPathRetryCooldown { ResumeFrame = 1 });
        int2 goal = new(11, 12);

        UnitMoveOrderSystem.MoveOrderCommandResult result = moveOrderSystem.IssueGroupedManualMoveOrder(
            _entityManager,
            unit,
            goal,
            issueGroundPathNow: false,
            useGroundPathRetryCooldown: true,
            resumeFrame: 30,
            currentFrame: 20);

        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(30, _entityManager.GetComponentData<UnitPathRetryCooldown>(unit).ResumeFrame);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(1, result.StructuralRemoves);
        Assert.AreEqual(4, result.StructuralAdds);
        Assert.AreEqual(1, result.StaggeredPathRequests);
        Assert.AreEqual(10, result.MaxStaggerDelayFrames);
    }

    [Test]
    public void UnitMoveOrderRequestSystem_GroupedManualRequestWritesResultAndMoveComponents()
    {
        Entity unit = _entityManager.CreateEntity(typeof(UnitPathRequest));
        int2 goal = new(13, 14);
        SystemHandle requestSystem = _world.CreateSystem<UnitMoveOrderRequestSystem>();

        int requestId = UnitMoveOrderRequestSystem.EnqueueGroupedManualMoveOrder(
            _entityManager,
            unit,
            goal,
            issueGroundPathNow: false,
            useGroundPathRetryCooldown: true,
            resumeFrame: 42,
            currentFrame: 30);
        requestSystem.Update(_world.Unmanaged);

        using EntityQuery queueQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitMoveOrderRequestElement>(),
            ComponentType.ReadOnly<UnitMoveOrderResultElement>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        DynamicBuffer<UnitMoveOrderRequestElement> requests =
            _entityManager.GetBuffer<UnitMoveOrderRequestElement>(queueEntity);
        DynamicBuffer<UnitMoveOrderResultElement> results =
            _entityManager.GetBuffer<UnitMoveOrderResultElement>(queueEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(requestId, results[0].RequestId);
        Assert.AreEqual(unit, results[0].Entity);
        Assert.AreEqual(goal, results[0].Goal);
        Assert.AreEqual(1, results[0].Issued);
        Assert.AreEqual(1, results[0].StaggeredPathRequests);
        Assert.AreEqual(12, results[0].MaxStaggerDelayFrames);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(42, _entityManager.GetComponentData<UnitPathRetryCooldown>(unit).ResumeFrame);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
    }

    [Test]
    public void UnitMoveOrderRequestSystem_TargetPathRequestWritesOnlyTargetAndPath()
    {
        Entity unit = _entityManager.CreateEntity(typeof(UnitResourceHaulOrder));
        int2 goal = new(21, 22);
        SystemHandle requestSystem = _world.CreateSystem<UnitMoveOrderRequestSystem>();

        int requestId = UnitMoveOrderRequestSystem.EnqueueTargetPathMoveOrder(_entityManager, unit, goal);
        requestSystem.Update(_world.Unmanaged);

        using EntityQuery queueQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitMoveOrderRequestElement>(),
            ComponentType.ReadOnly<UnitMoveOrderResultElement>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        DynamicBuffer<UnitMoveOrderRequestElement> requests =
            _entityManager.GetBuffer<UnitMoveOrderRequestElement>(queueEntity);
        DynamicBuffer<UnitMoveOrderResultElement> results =
            _entityManager.GetBuffer<UnitMoveOrderResultElement>(queueEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(requestId, results[0].RequestId);
        Assert.AreEqual(1, results[0].Issued);
        Assert.AreEqual(2, results[0].StructuralAdds);
        Assert.AreEqual(1, results[0].PathRequests);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<UnitResourceHaulOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
    }

    [Test]
    public void ClearMovementOrderComponents_RemovesSharedMoveOrderComponents()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitTarget),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(UnitPathRange),
            typeof(UnitPathRetryCooldown),
            typeof(UnitLongDistanceMove),
            typeof(ManualMoveOrderTag),
            typeof(ManualMoveGroupMemberTag),
            typeof(AutoWanderMoveTag),
            typeof(HoldPositionOrderTag),
            typeof(EngageTarget),
            typeof(BaseBreachOrder),
            typeof(UnitTransportBoardingTarget),
            typeof(UnitTransportRopeDisembarkRequest),
            typeof(UnitResourceHaulOrder));

        moveOrderSystem.ClearMovementOrderComponents(_entityManager, unit);

        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitLongDistanceMove>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportRopeDisembarkRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(unit));
    }

    [Test]
    public void UnitMoveOrderRequestSystem_ClearMovementRequestRemovesSharedMoveOrderComponents()
    {
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitTarget),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(UnitPathRange),
            typeof(UnitPathRetryCooldown),
            typeof(UnitLongDistanceMove),
            typeof(ManualMoveOrderTag),
            typeof(ManualMoveGroupMemberTag),
            typeof(AutoWanderMoveTag),
            typeof(HoldPositionOrderTag),
            typeof(EngageTarget),
            typeof(BaseBreachOrder),
            typeof(UnitTransportBoardingTarget),
            typeof(UnitTransportRopeDisembarkRequest),
            typeof(UnitResourceHaulOrder));
        SystemHandle requestSystem = _world.CreateSystem<UnitMoveOrderRequestSystem>();

        int requestId = UnitMoveOrderRequestSystem.EnqueueClearMovementOrder(_entityManager, unit);
        requestSystem.Update(_world.Unmanaged);

        using EntityQuery queueQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitMoveOrderRequestElement>(),
            ComponentType.ReadOnly<UnitMoveOrderResultElement>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        DynamicBuffer<UnitMoveOrderRequestElement> requests =
            _entityManager.GetBuffer<UnitMoveOrderRequestElement>(queueEntity);
        DynamicBuffer<UnitMoveOrderResultElement> results =
            _entityManager.GetBuffer<UnitMoveOrderResultElement>(queueEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(requestId, results[0].RequestId);
        Assert.AreEqual(unit, results[0].Entity);
        Assert.AreEqual(1, results[0].Issued);
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitLongDistanceMove>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportRopeDisembarkRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(unit));
    }

    [Test]
    public void SelectedMoveOrderCommand_IssuesMoveOrderForSelectedUnit()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        _entityManager.SetComponentData(unit, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        EntityQuery selectedMoveQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitGrid>());
        EntityQuery gridQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        EntityQuery mapSurfaceQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        int2 goal = new(7, 8);

        SelectedMoveOrderCommandSystem.Result result = new SelectedMoveOrderCommandSystem().TryIssueMoveOrder(
            _entityManager,
            Vector2.zero,
            selectedMoveQuery,
            gridQuery,
            mapSurfaceQuery,
            new UnitMoveOrderSystem(),
            new SelectionOrderMarkerSystem(),
            tryGetClickedUnit: (Vector2 screenPosition, EntityManager em, out Entity clicked) =>
            {
                clicked = Entity.Null;
                return false;
            },
            tryGetClickedCell: (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = goal;
                worldPoint = new Vector3(goal.x, 0f, goal.y);
                return true;
            },
            currentFrame: 12);

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
    }

    [Test]
    public void SelectedMoveOrderCommand_RefreshesCommandBuffersAfterStructuralMoveOrder()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        _entityManager.SetComponentData(unit, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        Entity commandEntity = _entityManager.CreateEntity();
        _entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        _entityManager.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 101,
            Frame = 20,
            ScreenPosition = new float2(1f, 0f),
            HasScreenPosition = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 102,
            Frame = 21,
            ScreenPosition = new float2(2f, 0f),
            HasScreenPosition = 1
        });

        EntityQuery selectedMoveQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitGrid>());
        EntityQuery gridQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        EntityQuery mapSurfaceQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        int2 firstGoal = new(7, 8);
        int2 secondGoal = new(8, 8);

        bool handled = new SelectedMoveOrderCommandSystem().ProcessCommandIntentRequests(
            _entityManager,
            commandEntity,
            requests,
            _entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity),
            selectedMoveQuery,
            gridQuery,
            mapSurfaceQuery,
            null,
            new UnitMoveOrderSystem(),
            null,
            tryGetClickedUnit: (Vector2 screenPosition, EntityManager em, out Entity clicked) =>
            {
                clicked = Entity.Null;
                return false;
            },
            tryGetClickedCell: (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = screenPosition.x < 1.5f ? firstGoal : secondGoal;
                worldPoint = new Vector3(cell.x, 0f, cell.y);
                return true;
            });

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            _entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(101, results[0].RequestId);
        Assert.AreEqual(102, results[1].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[1].Accepted);
        Assert.AreEqual(secondGoal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(secondGoal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
    }

    [Test]
    public void BuildingTargetMoveOrder_IssuesApproachCellMoveOrderForSelectedUnit()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitMove),
            typeof(UnitGrid));
        _entityManager.SetComponentData(unit, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });

        SystemHandle buildingTargetMoveOrderSystem = _world.CreateSystem<BuildingTargetMoveOrderSystem>();
        int requestId = BuildingTargetMoveOrderSystem.EnqueueMoveOrderToBuilding(
            _entityManager,
            new int2(6, 6),
            new int2(2, 2));
        buildingTargetMoveOrderSystem.Update(_world.Unmanaged);

        int2 expectedApproachCell = new(5, 5);
        using EntityQuery resultQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingTargetMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<BuildingTargetMoveOrderResultElement>());
        DynamicBuffer<BuildingTargetMoveOrderResultElement> results =
            _entityManager.GetBuffer<BuildingTargetMoveOrderResultElement>(resultQuery.GetSingletonEntity());
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(requestId, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(expectedApproachCell, results[0].GoalCell);
        Assert.AreEqual(1, results[0].IssuedUnitCount);
        Assert.AreEqual(expectedApproachCell, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(expectedApproachCell, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
    }

    private void CreateGrid(int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < _friendlyPassFactionIds.Length; i++)
            _friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = _entityManager.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(GridWalkable));
        _entityManager.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        _entityManager.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        _entityManager.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridWalkable> walkable = _entityManager.GetBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }
}
#endif
