using Game.Tactical.Contracts;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitMoveOrderSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private NativeList<int2> _pathPool;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.AttackMove_ResumesAfterCombatAndExplicitMoveCancelsDestination());
            RunCase(test => test.AttackMove_AcceptedTerrainCommandProjectsOnlyActuallyOrderedGroups());
            RunCase(test => test.AttackMove_RejectedFuelCommandPreservesGroupAndUnitHold());
            RunCase(test => test.AttackMove_PartialAcceptanceKeepsRejectedMemberAndGroupHonest());
            RunCase(test => test.AttackMove_GroupBookkeepingRespectsFactionAttemptAndLegacyScope());
            RunCase(test => test.RegistryAttackIntentLeavesTravelAndCombatToSharedOwners());
            RunCase(test => test.AttackMove_PublicEvidenceKeepsScopedAndUnscopedAcceptanceStrict());
            RunCase(test => test.AttackMove_AutonomousHomingKeepsNativeOrdersWithoutNewCommandEvidence());
            RunCase(test => test.AttackMove_RejectedAutonomousRefreshKeepsNativeHoldAndAuditUnchanged());
            RunCase(test => test.NativeAirAttackPreservesFuelRejectionAndExistingHold());
            RunCase(test => test.GetManualMoveFormationOffset_UsesPaddedFootprintStride());
            RunCase(test => test.BuildSelectedCurrentFootprintCells_UsesClampedFootprintsWithinGrid());
            RunCase(test => test.IssueImmediateMoveCommand_GroundUnitWritesTargetPathRequestAndManualTag());
            RunCase(test => test.IssueTargetOnlyMoveCommand_WritesTargetAndClearsConflictingOrders());
            RunCase(test => test.IssueGroupedManualMoveOrder_StaggeredGroundUnitUsesRetryCooldownInsteadOfPathRequest());
            RunCase(test => test.IssueGroupedManualMoveOrder_StaggeredGroundUnitReplacesExistingRetryCooldown());
            RunCase(test => test.UnitMoveOrderRequestSystem_GroupedManualRequestWritesResultAndMoveComponents());
            RunCase(test => test.UnitMoveOrderRequestSystem_RetainsEarlierResultAcrossLaterRequestBatch());
            RunCase(test => test.UnitMoveOrderRequestSystem_GroupedManualFuelConsumerRejectsAtZeroUsableFuel());
            RunCase(test => test.UnitMoveOrderRequestSystem_TargetPathRequestWritesOnlyTargetAndPath());
            RunCase(test => test.ClearMovementOrderComponents_RemovesSharedMoveOrderComponents());
            RunCase(test => test.UnitMoveOrderRequestSystem_ClearMovementRequestRemovesSharedMoveOrderComponents());
            RunCase(test => test.SelectedMoveOrderCommand_IssuesMoveOrderForSelectedUnit());
            RunCase(test => test.SelectedMoveOrderCommand_MoveModeIgnoresClickedFriendlyUnitAndUsesTerrainCell());
            RunCase(test => test.SelectedMoveOrderCommand_CommandResultDoesNotTreatFriendlyUnitClickAsAttackTarget());
            RunCase(test => test.SelectedMoveOrderCommand_ProcessesPreResolvedMoveRequestImmediately());
            RunCase(test => test.SelectedMoveOrderCommand_PreResolvedRequestPathfindsAndMovesSelectedUnit());
            RunCase(test => test.SelectedMoveOrderCommand_RefreshesCommandBuffersAfterStructuralMoveOrder());
            RunCase(test => test.BuildingTargetMoveOrder_IssuesApproachCellMoveOrderForSelectedUnit());
            UnityEngine.Debug.Log("[UnitMoveOrderFocusedValidation] result=Passed tests=29");
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
        if (_pathPool.IsCreated)
            _pathPool.Dispose();
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
    public void UnitMoveOrderRequestSystem_RetainsEarlierResultAcrossLaterRequestBatch()
    {
        Entity first = _entityManager.CreateEntity(typeof(UnitPathRequest));
        Entity second = _entityManager.CreateEntity(typeof(UnitPathRequest));
        SystemHandle requestSystem = _world.CreateSystem<UnitMoveOrderRequestSystem>();

        int firstRequest = UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(
            _entityManager, first, new int2(4, 5));
        requestSystem.Update(_world.Unmanaged);
        int secondRequest = UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(
            _entityManager, second, new int2(8, 9));
        requestSystem.Update(_world.Unmanaged);

        Assert.IsTrue(UnitMoveOrderRequestSystem.TryGetResult(
            _entityManager, firstRequest, out UnitMoveOrderResultElement firstResult));
        Assert.IsTrue(UnitMoveOrderRequestSystem.TryGetResult(
            _entityManager, secondRequest, out UnitMoveOrderResultElement secondResult));
        Assert.AreEqual(1, firstResult.Issued);
        Assert.AreEqual(1, secondResult.Issued);
    }

    [Test]
    public void UnitMoveOrderRequestSystem_GroupedManualFuelConsumerRejectsAtZeroUsableFuel()
    {
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitGrid),
            typeof(Faction),
            typeof(UnitMovementBehavior),
            typeof(UnitFuelConsumption),
            typeof(UnitFuelConsumptionState));
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(1, 1) });
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(unit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        _entityManager.SetComponentData(unit, new UnitFuelConsumption
        {
            Enabled = 1,
            GroundFuelPerCell = 1f,
            AirFuelPerCell = 2f
        });
        _entityManager.SetComponentData(unit, new UnitFuelConsumptionState());
        int2 goal = new(3, 1);
        SystemHandle requestSystem = _world.CreateSystem<UnitMoveOrderRequestSystem>();

        int requestId = UnitMoveOrderRequestSystem.EnqueueGroupedManualMoveOrder(
            _entityManager,
            unit,
            goal,
            issueGroundPathNow: true,
            useGroundPathRetryCooldown: false,
            resumeFrame: 0,
            currentFrame: 0);
        requestSystem.Update(_world.Unmanaged);

        using EntityQuery queueQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitMoveOrderRequestElement>(),
            ComponentType.ReadOnly<UnitMoveOrderResultElement>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        DynamicBuffer<UnitMoveOrderResultElement> results =
            _entityManager.GetBuffer<UnitMoveOrderResultElement>(queueEntity);

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(requestId, results[0].RequestId);
        Assert.AreEqual(0, results[0].Issued);
        Assert.AreEqual((int)TacticalCommandReasonCode.InsufficientFuel, results[0].RejectionReasonCode);
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
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
    public void RegistryAttackIntentLeavesTravelAndCombatToSharedOwners()
    {
        CreateGrid(32, 32);
        var em = _entityManager;
        var unit = em.CreateEntity(typeof(Faction), typeof(UnitMove), typeof(UnitGrid), typeof(UnitFootprint),
            typeof(UnitAttack), typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform),
            typeof(SkirmishVisualSpawnedComponent), typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
        em.SetComponentData(unit, new Faction { Id = 1 });
        em.SetComponentData(unit, new UnitMove { Speed = 5, WalkSpeed = 5, RoadSpeedMultiplier = 1 });
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1) });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(2.5f, 0, 2.5f)));
        em.SetComponentData(unit, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
        em.AddComponent<SkirmishSharedActorTag>(unit);
        var id = new FixedString64Bytes("command-owner-test");
        em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { SessionId = id, FactionId = 1 });
        em.SetComponentData(unit, new SkirmishUnitRoleComponent { Category = Game.Skirmish.Contracts.SkirmishPopulationCategory.Infantry });
        em.AddComponent<CampaignMissionCombatSuppressedTag>(unit);
        SkirmishSharedCombatBinding.Bind(em, unit, new Game.Skirmish.Contracts.SkirmishRoleOverlay
        { Damage = 10, RangeWorld = 3, TargetDomains = Game.Skirmish.Contracts.SkirmishTargetDomain.Infantry });
        Assert.IsFalse(em.HasComponent<CampaignMissionCombatSuppressedTag>(unit));
        Assert.IsTrue(SkirmishWorldMovementService.AssignIntent(em, unit, new float3(20, 0, 20),
            Game.Skirmish.Contracts.SkirmishGroupOrderKind.Attack));
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
        var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
        em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = id });
        em.RemoveComponent<UnitPathRequest>(unit);
        var enemy = em.CreateEntity();
        em.AddComponentData(unit, new EngageTarget { Target = enemy });
        SkirmishWorldMovementService.Step(em, session, .2f, false);
        Assert.IsTrue(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit), "The expansion must not resubmit a plain Move while shared combat is active.");
        SkirmishWorldMovementService.ClearIntent(em, unit);
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
    }

    [Test]
    public void NativeAirAttackPreservesFuelRejectionAndExistingHold()
    {
        CreateGrid(32, 32);
        var em = _entityManager;
        var id = new FixedString64Bytes("air-fuel-rejection");
        var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
        em.SetComponentData(session, new SkirmishExpandedSessionComponent
        { SessionId = id, Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Playing });
        em.AddBuffer<SkirmishArmyGroupRecord>(session);
        var unit = em.CreateEntity(typeof(Faction), typeof(UnitMove), typeof(UnitGrid), typeof(UnitFootprint),
            typeof(UnitAttack), typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform),
            typeof(UnitAirMovement), typeof(UnitAirComponent), typeof(UnitFuelConsumption),
            typeof(SkirmishSharedActorTag), typeof(CombatTargetPolicy), typeof(SkirmishAttemptOwnedComponent),
            typeof(SkirmishArmyGroupMembershipComponent), typeof(HoldPositionOrderTag), typeof(SkirmishMoveIntentComponent));
        em.SetComponentData(unit, new Faction { Id = 1 });
        em.SetComponentData(unit, new UnitMove { Speed = 5, WalkSpeed = 5, RoadSpeedMultiplier = 1 });
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1) });
        em.SetComponentData(unit, new UnitHealth { Current = 240, Max = 240 });
        em.SetComponentData(unit, new UnitAttack { Damage = 10, Range = 3 });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(2.5f, 0, 2.5f)));
        em.SetComponentData(unit, new UnitFuelConsumption { Enabled = 1, AirFuelPerCell = 2 });
        em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { SessionId = id, FactionId = 1 });
        var oldIntent = new SkirmishMoveIntentComponent
        { Order = Game.Skirmish.Contracts.SkirmishGroupOrderKind.Hold, DestinationX = 5, DestinationZ = 6 };
        em.SetComponentData(unit, oldIntent);
        var target = em.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(LocalTransform),
            typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishContactSightComponent));
        em.SetComponentData(target, new Faction { Id = 2 });
        em.SetComponentData(target, new UnitHealth { Current = 800, Max = 800 });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(20.5f, 0, 20.5f)));
        em.SetComponentData(target, new SkirmishAttemptOwnedComponent { SessionId = id, FactionId = 2, IsStructure = 1 });
        em.SetComponentData(target, new SkirmishContactSightComponent { Sight = Game.Skirmish.Contracts.SkirmishContactSight.Visible });
        using var selected = new NativeArray<Entity>(new[] { unit }, Allocator.Temp);
        var rejected = new UnitTargetOrderSystem().IssueAttackTarget(em, selected, target);
        Assert.IsFalse(rejected.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.InsufficientFuel, rejected.CommandResult.ReasonCode);
        Assert.AreEqual(oldIntent, em.GetComponentData<SkirmishMoveIntentComponent>(unit));
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        var supply = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(supply, new BuildingResourceStorageComponent
        { OwnerFactionId = 1, FuelStorageCapacity = 100, StoredFuelBarrels = 100 });
        var accepted = new UnitTargetOrderSystem().IssueAttackTarget(em, selected, target);
        Assert.IsTrue(accepted.CommandResult.Accepted);
        Assert.IsFalse(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
        Assert.IsTrue(em.HasComponent<UnitTarget>(unit));
    }

    [Test]
    public void AttackMove_AcceptedTerrainCommandProjectsOnlyActuallyOrderedGroups()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("terrain-groups");
        var fighter = AttackMoveActor("terrain-groups", 1, 1, SkirmishRoleKind.Rocketeer);
        var transport = AttackMoveActor("terrain-groups", 2, 1, SkirmishRoleKind.TransportPlane);
        em.SetComponentData(transport, new UnitCombat { CanAttack = 0 });
        em.SetComponentData(transport, new UnitAttack { Damage = 0 });
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Rocketeer);
        AddAttackMoveGroup(session, 2, 1, SkirmishRoleKind.TransportPlane);
        var unselected = AttackMoveActor("terrain-groups", 3, 1, SkirmishRoleKind.Tank);
        em.RemoveComponent<SelectedUnitTag>(unselected);
        AddAttackMoveGroup(session, 3, 1, SkirmishRoleKind.Tank);
        var groupBuffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
        var thirdGroup = groupBuffer[2]; thirdGroup.Selected = 0;
        groupBuffer[2] = thirdGroup;
        var otherAttempt = AttackMoveSession("other-attempt");
        AddAttackMoveGroup(otherAttempt, 1, 1, SkirmishRoleKind.Rocketeer);
        var result = ProcessTerrainAttackCommand();
        Assert.AreEqual(1, result.Accepted); Assert.AreEqual(0, result.HasTargetEntity);
        Assert.AreEqual(1, result.HasWorldPosition); Assert.AreEqual(71, result.RequestId);
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(fighter));
        Assert.AreEqual(em.GetComponentData<UnitTarget>(fighter).Cell, em.GetComponentData<AttackMoveOrder>(fighter).Destination);
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(fighter));
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(transport));
        Assert.IsFalse(em.HasComponent<UnitTarget>(transport));
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unselected)); Assert.IsFalse(em.HasComponent<UnitTarget>(unselected));
        Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.None, em.GetBuffer<SkirmishArmyGroupRecord>(session)[1].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.None, em.GetBuffer<SkirmishArmyGroupRecord>(session)[2].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.None, em.GetBuffer<SkirmishArmyGroupRecord>(otherAttempt)[0].LastOrder);
        SkirmishArmyDrawerProjection.Project(em, session);
        Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyDrawerSlot>(session)[0].LastOrder);
        Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(em, session,
            out _, out _, out _, out _, out _, out _, out _, out _, out var first, out var second, out var third, out _, out _));
        Assert.IsTrue(first.AttackOrdered); Assert.IsFalse(second.AttackOrdered); Assert.IsFalse(third.AttackOrdered);
    }

    [Test]
    public void AttackMove_RejectedFuelCommandPreservesGroupAndUnitHold()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("fuel-groups");
        var fighter = AttackMoveActor("fuel-groups", 1, 1, SkirmishRoleKind.Tank);
        em.AddComponentData(fighter, new UnitFuelConsumption { Enabled = 1, GroundFuelPerCell = 1 });
        em.AddComponent<HoldPositionOrderTag>(fighter);
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Tank, SkirmishGroupOrderKind.Hold);
        var result = ProcessTerrainAttackCommand();
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.InsufficientFuel, result.ReasonCode);
        Assert.AreEqual(SkirmishGroupOrderKind.Hold, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(fighter));
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(fighter)); Assert.IsFalse(em.HasComponent<UnitTarget>(fighter));
    }

    [Test]
    public void AttackMove_PartialAcceptanceKeepsRejectedMemberAndGroupHonest()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("partial-groups");
        var accepted = AttackMoveActor("partial-groups", 1, 1, SkirmishRoleKind.Rocketeer);
        var excludedSameGroup = AttackMoveActor("partial-groups", 1, 1, SkirmishRoleKind.Rocketeer);
        var excludedWholeGroup = AttackMoveActor("partial-groups", 2, 1, SkirmishRoleKind.Tank);
        foreach (var unit in new[] { excludedSameGroup, excludedWholeGroup })
        { em.AddComponentData(unit, new UnitFuelConsumption { Enabled = 1, GroundFuelPerCell = 1 }); em.AddComponent<HoldPositionOrderTag>(unit); }
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Rocketeer, SkirmishGroupOrderKind.Hold, 2);
        AddAttackMoveGroup(session, 2, 1, SkirmishRoleKind.Tank, SkirmishGroupOrderKind.Hold);
        Assert.AreEqual(1, ProcessTerrainAttackCommand().Accepted);
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(accepted));
        // Group LastOrder means last genuinely accepted group intent, not every member succeeded.
        Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.Hold, em.GetBuffer<SkirmishArmyGroupRecord>(session)[1].LastOrder);
        foreach (var unit in new[] { excludedSameGroup, excludedWholeGroup })
        { Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit)); Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unit)); Assert.IsFalse(em.HasComponent<UnitTarget>(unit)); }
    }

    [Test]
    public void AttackMove_GroupBookkeepingRespectsFactionAttemptAndLegacyScope()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var active = AttackMoveSession("enemy-groups");
        var legacy = AttackMoveSession("legacy-groups", 1);
        AddAttackMoveGroup(active, 7, 2, SkirmishRoleKind.Tank);
        AddAttackMoveGroup(active, 7, 1, SkirmishRoleKind.Tank);
        AddAttackMoveGroup(legacy, 7, 2, SkirmishRoleKind.Tank);
        var enemy = AttackMoveActor("enemy-groups", 7, 2, SkirmishRoleKind.Tank);
        var campaign = AttackMoveActor("legacy-groups", 7, 2, SkirmishRoleKind.Tank);
        using var units = new NativeArray<Entity>(new[] { enemy, campaign }, Allocator.Temp);
        Assert.IsTrue(AttackMoveSystem.IssueUnits(em, units, 2, new int2(20, 20), new float3(20.5f, 0, 20.5f), 2).Accepted);
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(enemy)); Assert.IsTrue(em.HasComponent<AttackMoveOrder>(campaign));
        Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyGroupRecord>(active)[0].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.None, em.GetBuffer<SkirmishArmyGroupRecord>(active)[1].LastOrder);
        Assert.AreEqual(SkirmishGroupOrderKind.None, em.GetBuffer<SkirmishArmyGroupRecord>(legacy)[0].LastOrder);
    }

    [Test]
    public void AttackMove_PublicEvidenceKeepsScopedAndUnscopedAcceptanceStrict()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("public-audit");
        var unit = AttackMoveActor("public-audit", 1, 1, SkirmishRoleKind.Rocketeer);
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Rocketeer);
        uint observedReceipt = 0; bool observedVerified = false; int events = 0;
        System.Action<string, uint, bool> observe = (operation, receipt, verified) =>
        { Assert.AreEqual("AttackMove", operation); observedReceipt = receipt; observedVerified = verified; events++; };
        AriaCommandEvidence.Begin(); AriaCommandEvidence.Recorded += observe;
        try
        {
            AriaCommandEvidence.ObserveRelease(40, Vector2.zero);
            uint receipt = AriaCommandEvidence.ClaimRelease(40, Vector2.zero); Assert.AreNotEqual(0u, receipt);
            using (AriaCommandEvidence.Enter(receipt))
                Assert.IsTrue(AttackMoveSystem.IssueSelected(em, new int2(20, 20), new float3(20.5f, 0, 20.5f), 40).Accepted);
            Assert.AreEqual(1, events); Assert.AreEqual(receipt, observedReceipt); Assert.IsTrue(observedVerified);
            Assert.AreEqual(1, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(0, AriaCommandEvidence.Violations);
            Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 0, out var violations)); Assert.AreEqual(0, violations);
            Assert.IsTrue(AttackMoveSystem.IssueSelected(em, new int2(21, 20), new float3(21.5f, 0, 20.5f), 41).Accepted);
            Assert.AreEqual(2, events); Assert.AreEqual(0u, observedReceipt); Assert.IsFalse(observedVerified);
            Assert.AreEqual(2, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(1, AriaCommandEvidence.Violations);
            Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
        }
        finally { AriaCommandEvidence.Recorded -= observe; AriaCommandEvidence.End(); }
    }

    [Test]
    public void AttackMove_AutonomousHomingKeepsNativeOrdersWithoutNewCommandEvidence()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("homing-audit");
        var unit = AttackMoveActor("homing-audit", 1, 1, SkirmishRoleKind.Rocketeer);
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Rocketeer);
        em.AddComponent<SkirmishSharedActorTag>(unit); em.AddComponent<CombatTargetPolicy>(unit);
        var target = em.CreateEntity(typeof(LocalTransform), typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishContactSightComponent));
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(20.5f, 0, 20.5f)));
        em.SetComponentData(target, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes("homing-audit"), FactionId = 2 });
        em.SetComponentData(target, new SkirmishContactSightComponent { Sight = SkirmishContactSight.Visible });
        int events = 0; System.Action<string, uint, bool> observe = (_, __, ___) => events++;
        AriaCommandEvidence.Begin(); AriaCommandEvidence.Recorded += observe;
        try
        {
            AriaCommandEvidence.ObserveRelease(50, Vector2.zero);
            uint receipt = AriaCommandEvidence.ClaimRelease(50, Vector2.zero);
            using (AriaCommandEvidence.Enter(receipt))
                Assert.IsTrue(SkirmishWorldMovementService.AssignIntent(em, unit, new float3(12.5f, 0, 12.5f), SkirmishGroupOrderKind.Attack));
            var intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit); intent.AttackTarget = target; em.SetComponentData(unit, intent);
            Assert.AreEqual(1, events); Assert.AreEqual(1, AriaCommandEvidence.AcceptedCommands);
            SkirmishWorldMovementService.Step(em, session, .2f, false);
            intent = em.GetComponentData<SkirmishMoveIntentComponent>(unit);
            Assert.AreEqual(20.5f, intent.DestinationX); Assert.AreEqual(20.5f, intent.DestinationZ); Assert.AreEqual(target, intent.AttackTarget);
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit)); Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
            Assert.AreEqual(em.GetComponentData<UnitTarget>(unit).Cell, em.GetComponentData<AttackMoveOrder>(unit).Destination);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
            em.SetComponentData(target, LocalTransform.FromPosition(new float3(22.5f, 0, 20.5f)));
            SkirmishWorldMovementService.Step(em, session, .2f, false);
            Assert.AreEqual(22.5f, em.GetComponentData<SkirmishMoveIntentComponent>(unit).DestinationX);
            Assert.AreEqual(new int2(22, 20), em.GetComponentData<AttackMoveOrder>(unit).Destination);

            // A moving visible target must not turn each homing refresh into a
            // fresh Move that removes the native EngageTarget before its shot.
            em.AddComponentData(unit, new EngageTarget { Target = target, IsCommanded = 1 });
            em.SetComponentData(target, LocalTransform.FromPosition(new float3(24.5f, 0, 20.5f)));
            SkirmishWorldMovementService.Step(em, session, .2f, false);
            Assert.AreEqual(target, em.GetComponentData<EngageTarget>(unit).Target);
            Assert.AreEqual(24.5f, em.GetComponentData<SkirmishMoveIntentComponent>(unit).DestinationX);
            Assert.AreEqual(new int2(22, 20), em.GetComponentData<AttackMoveOrder>(unit).Destination,
                "The combat motor owns pursuit while this exact target is engaged.");
            em.SetComponentData(target, LocalTransform.FromPosition(new float3(25.5f, 0, 20.5f)));
            SkirmishWorldMovementService.Step(em, session, .2f, false);
            Assert.AreEqual(target, em.GetComponentData<EngageTarget>(unit).Target);
            Assert.AreEqual(25.5f, em.GetComponentData<SkirmishMoveIntentComponent>(unit).DestinationX);

            // If combat loses the engagement at the latest target position,
            // the still-stale native travel goal must catch up without a new Touch.
            em.RemoveComponent<EngageTarget>(unit);
            SkirmishWorldMovementService.Step(em, session, .2f, false);
            Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
            Assert.AreEqual(new int2(25, 20), em.GetComponentData<AttackMoveOrder>(unit).Destination);
            Assert.AreEqual(new int2(25, 20), em.GetComponentData<UnitTarget>(unit).Cell);
            Assert.AreEqual(1, events); Assert.AreEqual(1, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(0, AriaCommandEvidence.Violations);
            Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 0, out var violations)); Assert.AreEqual(0, violations);
        }
        finally { AriaCommandEvidence.Recorded -= observe; AriaCommandEvidence.End(); }
    }

    [Test]
    public void AttackMove_RejectedAutonomousRefreshKeepsNativeHoldAndAuditUnchanged()
    {
        CreateGrid(32, 32); var em = _entityManager;
        var session = AttackMoveSession("refresh-fuel");
        var unit = AttackMoveActor("refresh-fuel", 1, 1, SkirmishRoleKind.Tank);
        AddAttackMoveGroup(session, 1, 1, SkirmishRoleKind.Tank, SkirmishGroupOrderKind.Hold);
        em.AddComponentData(unit, new UnitFuelConsumption { Enabled = 1, GroundFuelPerCell = 1 }); em.AddComponent<HoldPositionOrderTag>(unit);
        AriaCommandEvidence.Begin();
        try
        {
            using var units = new NativeArray<Entity>(new[] { unit }, Allocator.Temp);
            var result = AttackMoveSystem.RefreshUnits(em, units, 1, new int2(20, 20), new float3(20.5f, 0, 20.5f), 60);
            Assert.IsFalse(result.Accepted); Assert.AreEqual(TacticalCommandReasonCode.InsufficientFuel, result.ReasonCode);
            Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit)); Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unit));
            Assert.AreEqual(SkirmishGroupOrderKind.Hold, em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
            Assert.AreEqual(0, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(0, AriaCommandEvidence.Violations);
        }
        finally { AriaCommandEvidence.End(); }
    }

    private Entity AttackMoveSession(string id, byte legacy = 0)
    {
        var session = _entityManager.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmySelectionComponent));
        _entityManager.SetComponentData(session, new SkirmishExpandedSessionComponent
        { SessionId = new FixedString64Bytes(id), IsLegacy = legacy, Phase = SkirmishSessionPhase.Playing });
        _entityManager.SetComponentData(session, new SkirmishArmySelectionComponent { PageSize = 5 });
        _entityManager.AddBuffer<SkirmishArmyGroupRecord>(session);
        return session;
    }
    private void AddAttackMoveGroup(Entity session, uint id, byte faction, SkirmishRoleKind role,
        SkirmishGroupOrderKind last = SkirmishGroupOrderKind.None, int members = 1)
    {
        _entityManager.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord
        { GroupId = id, FactionId = faction, Role = role, Domain = AttackMoveDomain(role),
          MemberCount = members, AliveCount = members, Selected = 1, LastOrder = last });
    }
    private Entity AttackMoveActor(string attempt, uint group, byte faction, SkirmishRoleKind role)
    {
        var em = _entityManager;
        var unit = em.CreateEntity(typeof(SelectedUnitTag), typeof(Faction), typeof(UnitMove), typeof(UnitGrid),
            typeof(UnitFootprint), typeof(UnitAttack), typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform),
            typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishArmyGroupMembershipComponent), typeof(SkirmishUnitRoleComponent));
        em.SetComponentData(unit, new Faction { Id = faction });
        em.SetComponentData(unit, new UnitMove { Speed = 5, WalkSpeed = 5, RoadSpeedMultiplier = 1 });
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1) });
        em.SetComponentData(unit, new UnitAttack { Damage = 10, Range = 3 });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(2.5f, 0, 2.5f)));
        em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes(attempt), FactionId = faction });
        em.SetComponentData(unit, new SkirmishArmyGroupMembershipComponent { GroupId = group, Domain = AttackMoveDomain(role) });
        em.SetComponentData(unit, new SkirmishUnitRoleComponent { Role = role, Category = AttackMoveDomain(role) });
        if (role == SkirmishRoleKind.TransportPlane) em.AddComponent<UnitAirMovement>(unit);
        return unit;
    }
    private static SkirmishPopulationCategory AttackMoveDomain(SkirmishRoleKind role)
        => role == SkirmishRoleKind.TransportPlane ? SkirmishPopulationCategory.Air
            : role == SkirmishRoleKind.Tank ? SkirmishPopulationCategory.Ground : SkirmishPopulationCategory.Infantry;
    private RtsSelectionCommandResultElement ProcessTerrainAttackCommand()
    {
        var em = _entityManager;
        var queue = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue).Add(new RtsSelectionCommandIntentRequestElement
        { RequestId = 71, Kind = RtsSelectionCommandIntentKind.Attack, HasTargetCell = 1, TargetCell = new int2(20, 20),
          WorldPosition = new float3(20.5f, 0, 20.5f), Frame = 2 });
        em.AddBuffer<RtsSelectionCommandResultElement>(queue);
        _world.GetOrCreateSystem<AttackOrderCommandSystem>().Update(_world.Unmanaged);
        em.CompleteAllTrackedJobs();
        var results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
        Assert.AreEqual(1, results.Length, "Native Attack command owner must supply the single actual result.");
        return results[0];
    }

    [Test]
    public void AttackMove_ResumesAfterCombatAndExplicitMoveCancelsDestination()
    {
        CreateGrid(32, 32);
        var em = _entityManager;
        var unit = em.CreateEntity(typeof(SelectedUnitTag), typeof(Faction), typeof(UnitMove), typeof(UnitGrid),
            typeof(UnitFootprint), typeof(UnitAttack), typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(unit, new UnitMove { Speed = 5, WalkSpeed = 5, RoadSpeedMultiplier = 1 });
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(1) });
        em.SetComponentData(unit, new UnitAttack { Damage = 10, Range = 3 });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(2.5f, 0, 2.5f)));
        Assert.IsTrue(AttackMoveSystem.IssueSelected(em, new int2(20, 20), new float3(20, 0, 20), 1).Accepted);
        var destination = em.GetComponentData<AttackMoveOrder>(unit).Destination;
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit), "Advance retains player-order pathfinding priority.");
        em.RemoveComponent<UnitPathRequest>(unit);
        var enemy = em.CreateEntity();
        em.AddComponentData(unit, new EngageTarget { Target = enemy });
        AttackMoveSystem.ResumeTravel(em, 2);
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit), "Do not resume while fighting.");
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
        em.RemoveComponent<EngageTarget>(unit);
        AttackMoveSystem.ResumeTravel(em, 3);
        Assert.AreEqual(destination, em.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(unit));
        Assert.IsTrue(UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, new int2(5, 5)));
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unit), "Player Move replaces the advance permanently.");
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsTrue(AttackMoveSystem.IssueSelected(em, new int2(20, 20), new float3(20, 0, 20), 4).Accepted);
        em.AddComponent<HoldPositionOrderTag>(unit);
        AttackMoveSystem.ResumeTravel(em, 5);
        Assert.IsFalse(em.HasComponent<AttackMoveOrder>(unit), "Hold must cancel the old destination.");
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
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
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
    public void SelectedMoveOrderCommand_MoveModeIgnoresClickedFriendlyUnitAndUsesTerrainCell()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(unit, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        Entity friendlyPlane = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(friendlyPlane, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(friendlyPlane, new UnitGrid { Cell = new int2(8, 8) });
        _entityManager.SetComponentData(friendlyPlane, new UnitFootprint { Size = new int2(3, 3) });

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
            tryGetClickedUnit: (Vector2 screenPosition, EntityManager em, out Entity clicked) =>
            {
                clicked = friendlyPlane;
                return true;
            },
            tryGetClickedCell: (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = goal;
                worldPoint = new Vector3(goal.x + 0.5f, 0f, goal.y + 0.5f);
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
    public void SelectedMoveOrderCommand_CommandResultDoesNotTreatFriendlyUnitClickAsAttackTarget()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(unit, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
        _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });

        Entity friendlyPlane = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(friendlyPlane, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(friendlyPlane, new UnitGrid { Cell = new int2(8, 8) });
        _entityManager.SetComponentData(friendlyPlane, new UnitFootprint { Size = new int2(3, 3) });

        Entity commandEntity = _entityManager.CreateEntity();
        _entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        _entityManager.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 202,
            Frame = 30,
            ScreenPosition = new float2(4f, 5f),
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
        int2 goal = new(7, 8);

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
            tryGetClickedUnit: (Vector2 screenPosition, EntityManager em, out Entity clicked) =>
            {
                clicked = friendlyPlane;
                return true;
            },
            tryGetClickedCell: (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint) =>
            {
                cell = goal;
                worldPoint = new Vector3(goal.x + 0.5f, 0f, goal.y + 0.5f);
                return true;
            });

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            _entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(202, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreNotEqual((int)TacticalCommandReasonCode.TargetNotAttackable, results[0].ReasonCode);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
    }

    [Test]
    public void SelectedMoveOrderCommand_ProcessesPreResolvedMoveRequestImmediately()
    {
        CreateGrid(width: 16, height: 16);
        Entity unit = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
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
        int2 goal = new(9, 10);
        Vector3 worldPoint = new(goal.x + 0.5f, 0f, goal.y + 0.5f);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 303,
            Frame = 42,
            ScreenPosition = new float2(20f, 30f),
            TargetCell = goal,
            WorldPosition = new float3(worldPoint.x, worldPoint.y, worldPoint.z),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasScreenPosition = 1,
            HasTargetCell = 1,
            HasWorldPosition = 1
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
            tryGetClickedUnit: (Vector2 screenPosition, EntityManager em, out Entity clicked) =>
            {
                clicked = Entity.Null;
                return false;
            },
            tryGetClickedCell: (Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 point) =>
            {
                cell = default;
                point = default;
                return false;
            });

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            _entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(303, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(goal, results[0].TargetCell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveGroupMemberTag>(unit));
    }

    [Test]
    public void SelectedMoveOrderCommand_PreResolvedRequestPathfindsAndMovesSelectedUnit()
    {
        bool previousPlayRequested = RuntimeGameplayStateTestHelper.ReadGameplayState().PlayRequested != 0;
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(_entityManager, true);
            CreateGrid(width: 16, height: 16);
            Entity unit = _entityManager.CreateEntity(
                typeof(SelectedUnitTag),
                typeof(Faction),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(LocalTransform));
            _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
            _entityManager.SetComponentData(unit, new UnitMove
            {
                Speed = 4f,
                WalkSpeed = 4f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
            _entityManager.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 2) });
            _entityManager.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            _entityManager.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            _entityManager.SetComponentData(unit, new UnitVehicleMovement());
            _entityManager.SetComponentData(unit, new UnitVehicleKinematics());
            _entityManager.SetComponentData(unit, LocalTransform.FromPosition(new float3(2.5f, 0f, 2.5f)));

            Entity commandEntity = _entityManager.CreateEntity();
            _entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            _entityManager.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            int2 goal = new(6, 2);
            Vector3 worldPoint = new(goal.x + 0.5f, 0f, goal.y + 0.5f);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                RequestId = 404,
                Frame = 52,
                ScreenPosition = new float2(24f, 36f),
                TargetCell = goal,
                WorldPosition = new float3(worldPoint.x, worldPoint.y, worldPoint.z),
                TargetKind = RtsSelectionCommandTargetKind.Cell,
                HasScreenPosition = 1,
                HasTargetCell = 1,
                HasWorldPosition = 1
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
                null);

            Assert.IsTrue(handled);
            Assert.IsTrue(_entityManager.HasComponent<UnitPathRequest>(unit));
            Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);

            SystemHandle pathSystem = _world.CreateSystem<UnitPathfindingSystem>();
            for (int i = 0; i < 128 && !_entityManager.HasComponent<UnitPathRange>(unit); i++)
            {
                _world.SetTime(new TimeData((i + 1) * 0.016d, 0.016f));
                pathSystem.Update(_world.Unmanaged);
                _entityManager.CompleteAllTrackedJobs();
                System.Threading.Thread.Sleep(1);
            }

            Assert.IsTrue(_entityManager.HasComponent<UnitPathRange>(unit), "Resolved move command should produce a path range before visible movement.");
            UnitPathRange pathRange = _entityManager.GetComponentData<UnitPathRange>(unit);
            Assert.Greater(pathRange.Length, 0, "Resolved move command path should contain at least one movement cell.");
            NativeList<int2> pathCells = _entityManager.GetComponentData<PathPoolComponent>(gridQuery.GetSingletonEntity()).Cells;
            int2 firstPathCell = pathCells[pathRange.Start];
            int2 lastPathCell = pathCells[pathRange.Start + pathRange.Length - 1];
            Assert.AreEqual(goal, lastPathCell, "Resolved move command path should end at the requested move goal.");

            using EntityQuery movementQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadWrite<UnitGrid>(),
                ComponentType.ReadWrite<UnitPathFollow>(),
                ComponentType.ReadWrite<UnitVehicleKinematics>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitPathRange>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<UnitVehicleMovement>(),
                ComponentType.ReadOnly<Faction>());
            Assert.AreEqual(1, movementQuery.CalculateEntityCount(), "Resolved move command unit should match the UnitGridMoveJob component set before movement.");

            EndSimulationEntityCommandBufferSystem endSimulationEcbSystem =
                _world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = _world.CreateSystem<UnitGridMovementSystem>();
            float3 startPosition = _entityManager.GetComponentData<LocalTransform>(unit).Position;
            for (int i = 0; i < 8; i++)
            {
                _world.SetTime(new TimeData((i + 1) * 0.1d, 0.1f));
                movementSystem.Update(_world.Unmanaged);
                _entityManager.CompleteAllTrackedJobs();
                endSimulationEcbSystem.Update();
            }

            float3 movedPosition = _entityManager.GetComponentData<LocalTransform>(unit).Position;
            UnitPathFollow follow = _entityManager.HasComponent<UnitPathFollow>(unit)
                ? _entityManager.GetComponentData<UnitPathFollow>(unit)
                : default;
            Assert.Greater(
                math.distance(startPosition, movedPosition),
                0.05f,
                $"Resolved move command should advance the selected unit transform after pathfinding. pathStart={pathRange.Start} pathLength={pathRange.Length} firstPathCell={firstPathCell} lastPathCell={lastPathCell} followIndex={follow.PathIndex} start={startPosition} moved={movedPosition}");
        }
        finally
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(previousPlayRequested);
            RuntimeGameplayStateTestHelper.SetSimulationActive(previousPlayRequested);
        }
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
        _entityManager.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
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

        _pathPool = new NativeList<int2>(gridSize, Allocator.Persistent);
        Entity gridEntity = _entityManager.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent),
            typeof(GridWalkable),
            typeof(GridRoad),
            typeof(GridRoadSidewalk),
            typeof(GridRoadDirt));
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
        _entityManager.SetComponentData(gridEntity, new PathPoolComponent { Cells = _pathPool });

        DynamicBuffer<GridWalkable> walkable = _entityManager.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> roads = _entityManager.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = _entityManager.GetBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = _entityManager.GetBuffer<GridRoadDirt>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }
}
#endif
