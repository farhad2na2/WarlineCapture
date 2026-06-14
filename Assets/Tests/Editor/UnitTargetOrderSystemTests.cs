#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class UnitTargetOrderSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitTargetOrderSystemTests();
            tests.RunWithFixture(tests.ChebyshevDistance_ReturnsLargestAxisDelta);
            tests.RunWithFixture(tests.IsBuildingEntity_DetectsRespawnlessHealthEntityWithoutUnitMove);
            tests.RunWithFixture(tests.IssueAttackTarget_WritesEngageTargetAndClearsMoveOrderComponents);
            tests.RunWithFixture(tests.IssueAttackTarget_WithBreachResolverWritesBaseBreachMoveOrder);
            tests.RunWithFixture(tests.IssueAttackTarget_WithExistingBaseBreachOrderReplacesItAfterClear);
            tests.RunWithFixture(tests.IssueDirectAttackTarget_WritesCommandedEngageTarget);
            tests.RunWithFixture(tests.AttackOrderCommandSystem_FallbackQueryIssuesAttackForSelectedSource);
            tests.RunWithFixture(tests.UnitAttackOrderRequestSystem_ConsumesRequestAndWritesResult);
            tests.RunWithFixture(tests.UnitAttackOrderRequestSystem_ConsumesDirectRequestAndWritesResult);
            tests.RunWithFixture(tests.UnitAttackOrderRequestSystem_ConsumesSourceBaseBreachRequestAndWritesMoveOutput);
            tests.RunWithFixture(tests.UnitAttackOrderRequestSystem_ConsumesClearRequestAndWritesResult);
            tests.RunWithFixture(tests.UnitAttackOrderRequestSystem_ConsumesClearAccidentalAirSelectionMoveRequest);
            Debug.Log("[UnitTargetOrderFocusedValidation] result=Passed tests=12");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitTargetOrderFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private void RunWithFixture(Action test)
    {
        SetUp();
        try
        {
            test();
        }
        finally
        {
            TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("UnitTargetOrderSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void ChebyshevDistance_ReturnsLargestAxisDelta()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();

        Assert.AreEqual(7, targetOrderSystem.ChebyshevDistance(new int2(2, 3), new int2(9, 5)));
        Assert.AreEqual(6, targetOrderSystem.ChebyshevDistance(new int2(9, 5), new int2(4, -1)));
    }

    [Test]
    public void IsBuildingEntity_DetectsRespawnlessHealthEntityWithoutUnitMove()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity building = _entityManager.CreateEntity(typeof(UnitHealth), typeof(UnitRespawnPrefab));
        _entityManager.SetComponentData(building, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(building, new UnitRespawnPrefab { Prefab = Entity.Null });
        Entity unit = _entityManager.CreateEntity(typeof(UnitHealth), typeof(UnitRespawnPrefab), typeof(UnitMove));
        _entityManager.SetComponentData(unit, new UnitRespawnPrefab { Prefab = Entity.Null });

        Assert.IsTrue(targetOrderSystem.IsBuildingEntity(_entityManager, building));
        Assert.IsFalse(targetOrderSystem.IsBuildingEntity(_entityManager, unit));
    }

    [Test]
    public void IssueAttackTarget_WritesEngageTargetAndClearsMoveOrderComponents()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));
        _entityManager.AddComponent<ManualMoveOrderTag>(attacker);
        _entityManager.AddComponent<AutoWanderMoveTag>(attacker);
        _entityManager.AddComponent<HoldPositionOrderTag>(attacker);
        _entityManager.AddComponent<UnitPathFollow>(attacker);
        _entityManager.AddComponent<UnitPathRange>(attacker);
        _entityManager.AddComponentData(attacker, new UnitPathRequest { Goal = new int2(1, 1) });
        _entityManager.AddComponentData(attacker, new UnitTarget { Cell = new int2(1, 1) });
        _entityManager.AddComponentData(attacker, new UnitPathRetryCooldown { ResumeFrame = 9 });
        _entityManager.AddComponentData(attacker, new UnitLongDistanceMove { FinalGoal = new int2(2, 2) });
        _entityManager.AddComponent<ManualMoveGroupMemberTag>(attacker);
        _entityManager.AddComponentData(attacker, new BaseBreachOrder { FinalTarget = Entity.Null, FinalCell = new int2(3, 3) });
        _entityManager.AddComponentData(attacker, new UnitTransportBoardingTarget { Transport = Entity.Null, Goal = new int2(4, 4) });
        _entityManager.AddComponentData(attacker, new UnitTransportRopeDisembarkRequest { ReferenceCell = new int2(5, 5) });
        _entityManager.AddComponentData(attacker, new UnitResourceHaulOrder { SourceBuildingId = 1, DestinationBuildingId = 2, TargetCell = new int2(6, 6), Phase = 1 });

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = attacker;
            result = targetOrderSystem.IssueAttackTarget(_entityManager, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.IsTrue(_entityManager.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRetryCooldown>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitLongDistanceMove>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveGroupMemberTag>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportBoardingTarget>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitTransportRopeDisembarkRequest>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(attacker));
    }

    [Test]
    public void IssueAttackTarget_WithBreachResolverWritesBaseBreachMoveOrder()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));
        Entity breach = CreateTarget(new int2(4, 5), new float3(4.5f, 0f, 5.5f));

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = attacker;
            result = targetOrderSystem.IssueAttackTarget(
                _entityManager,
                selected,
                target,
                (
                    byte factionId,
                    Entity finalTarget,
                    int2 finalTargetCell,
                    int2 attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition) =>
                {
                    breachTarget = breach;
                    breachCell = new int2(4, 5);
                    breachPosition = new float3(4.5f, 0f, 5.5f);
                    return true;
                });
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(attacker));
        Assert.AreEqual(new int2(4, 5), _entityManager.GetComponentData<UnitTarget>(attacker).Cell);
        Assert.AreEqual(new int2(4, 5), _entityManager.GetComponentData<UnitPathRequest>(attacker).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(attacker));
        BaseBreachOrder breachOrder = _entityManager.GetComponentData<BaseBreachOrder>(attacker);
        Assert.AreEqual(target, breachOrder.FinalTarget);
        Assert.AreEqual(breach, breachOrder.BreachTarget);
        Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, breachOrder.Stage);
    }

    [Test]
    public void IssueAttackTarget_WithExistingBaseBreachOrderReplacesItAfterClear()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity attacker = CreateAttacker();
        Entity oldTarget = CreateTarget(new int2(1, 2), new float3(1.5f, 0f, 2.5f));
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));
        Entity breach = CreateTarget(new int2(4, 5), new float3(4.5f, 0f, 5.5f));
        _entityManager.AddComponentData(attacker, new BaseBreachOrder
        {
            FinalTarget = oldTarget,
            FinalCell = new int2(1, 2),
            BreachTarget = oldTarget,
            BreachCell = new int2(1, 2),
            Stage = 99,
            IsCommanded = 1
        });

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        try
        {
            selected[0] = attacker;
            UnitTargetOrderSystem.AttackOrderIssueResult result = targetOrderSystem.IssueAttackTarget(
                _entityManager,
                selected,
                target,
                (
                    byte factionId,
                    Entity finalTarget,
                    int2 finalTargetCell,
                    int2 attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition) =>
                {
                    breachTarget = breach;
                    breachCell = new int2(4, 5);
                    breachPosition = new float3(4.5f, 0f, 5.5f);
                    return true;
                });

            Assert.IsTrue(result.CommandResult.Accepted);
        }
        finally
        {
            selected.Dispose();
        }

        BaseBreachOrder breachOrder = _entityManager.GetComponentData<BaseBreachOrder>(attacker);
        Assert.AreEqual(target, breachOrder.FinalTarget);
        Assert.AreEqual(new int2(7, 8), breachOrder.FinalCell);
        Assert.AreEqual(breach, breachOrder.BreachTarget);
        Assert.AreEqual(new int2(4, 5), breachOrder.BreachCell);
        Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, breachOrder.Stage);
    }

    [Test]
    public void IssueDirectAttackTarget_WritesCommandedEngageTarget()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(2, 3), new float3(2.5f, 0f, 3.5f));
        _entityManager.AddComponent<HoldPositionOrderTag>(attacker);
        _entityManager.AddComponentData(attacker, new UnitResourceHaulOrder { SourceBuildingId = 1, DestinationBuildingId = 2, TargetCell = new int2(6, 6), Phase = 1 });

        targetOrderSystem.IssueDirectAttackTarget(_entityManager, attacker, target, new int2(2, 3), new float3(2.5f, 0f, 3.5f));

        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(2, 3), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitResourceHaulOrder>(attacker));
    }

    [Test]
    public void AttackOrderCommandSystem_FallbackQueryIssuesAttackForSelectedSource()
    {
        Entity attacker = CreateAttacker();
        _entityManager.AddComponent<SelectedUnitTag>(attacker);
        _entityManager.AddComponentData(attacker, new UnitAttack { Range = 20f, CooldownSeconds = 1f, Damage = 10 });
        _entityManager.AddComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));

        var commandSystem = new AttackOrderCommandSystem();
        AttackOrderCommandSystem.Result result = commandSystem.IssueAttackTarget(
            _entityManager,
            target);

        Assert.IsTrue(result.Issued);
        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(target, result.TargetEntity);
        Assert.IsTrue(_entityManager.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void UnitAttackOrderRequestSystem_ConsumesRequestAndWritesResult()
    {
        Entity attacker = CreateAttacker();
        _entityManager.AddComponent<SelectedUnitTag>(attacker);
        _entityManager.AddComponentData(attacker, new UnitAttack { Range = 20f, CooldownSeconds = 1f, Damage = 10 });
        _entityManager.AddComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));

        int requestId = UnitAttackOrderRequestSystem.EnqueueSelectedAttackTarget(_entityManager, target);
        SystemHandle system = _world.CreateSystem<UnitAttackOrderRequestSystem>();
        system.Update(_world.Unmanaged);

        Assert.IsTrue(UnitAttackOrderRequestSystem.TryGetResult(_entityManager, requestId, out UnitAttackOrderResultElement result));
        Assert.AreEqual(target, result.TargetEntity);
        Assert.AreEqual(1, result.Issued);
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(1, result.IssuedCount);

        using EntityQuery queueQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackOrderQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, _entityManager.GetBuffer<UnitAttackOrderRequestElement>(queueEntity).Length);

        Assert.IsTrue(_entityManager.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void UnitAttackOrderRequestSystem_ConsumesDirectRequestAndWritesResult()
    {
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));

        int requestId = UnitAttackOrderRequestSystem.EnqueueDirectAttackTarget(
            _entityManager,
            attacker,
            target,
            new int2(7, 8),
            new float3(7.5f, 0f, 8.5f));
        SystemHandle system = _world.CreateSystem<UnitAttackOrderRequestSystem>();
        system.Update(_world.Unmanaged);

        Assert.IsTrue(UnitAttackOrderRequestSystem.TryGetResult(_entityManager, requestId, out UnitAttackOrderResultElement result));
        Assert.AreEqual(target, result.TargetEntity);
        Assert.AreEqual(1, result.Issued);
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(1, result.IssuedCount);

        Assert.IsTrue(_entityManager.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void UnitAttackOrderRequestSystem_ConsumesSourceBaseBreachRequestAndWritesMoveOutput()
    {
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(7, 8), new float3(7.5f, 0f, 8.5f));
        Entity breach = CreateTarget(new int2(4, 5), new float3(4.5f, 0f, 5.5f));

        int requestId = UnitAttackOrderRequestSystem.EnqueueSourceBaseBreachAttackTarget(
            _entityManager,
            attacker,
            target,
            breach,
            new int2(4, 5),
            new float3(4.5f, 0f, 5.5f));
        SystemHandle system = _world.CreateSystem<UnitAttackOrderRequestSystem>();
        system.Update(_world.Unmanaged);

        Assert.IsTrue(UnitAttackOrderRequestSystem.TryGetResult(_entityManager, requestId, out UnitAttackOrderResultElement result));
        Assert.AreEqual(target, result.TargetEntity);
        Assert.AreEqual(1, result.Issued);
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(attacker));
        Assert.AreEqual(new int2(4, 5), _entityManager.GetComponentData<UnitTarget>(attacker).Cell);
        Assert.AreEqual(new int2(4, 5), _entityManager.GetComponentData<UnitPathRequest>(attacker).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(attacker));
        BaseBreachOrder breachOrder = _entityManager.GetComponentData<BaseBreachOrder>(attacker);
        Assert.AreEqual(target, breachOrder.FinalTarget);
        Assert.AreEqual(breach, breachOrder.BreachTarget);
        Assert.AreEqual(BaseBreachOrder.StageMovingToEnemyBreach, breachOrder.Stage);
    }

    [Test]
    public void UnitAttackOrderRequestSystem_ConsumesClearRequestAndWritesResult()
    {
        Entity attacker = CreateAttacker();
        _entityManager.AddComponentData(attacker, new EngageTarget { Target = Entity.Null, Cell = new int2(2, 3), IsCommanded = 1 });
        _entityManager.AddComponentData(attacker, new BaseBreachOrder { FinalTarget = Entity.Null, FinalCell = new int2(4, 5) });
        _entityManager.AddComponentData(attacker, new UnitTarget { Cell = new int2(6, 7) });
        _entityManager.AddComponentData(attacker, new UnitPathRequest { Goal = new int2(6, 7) });

        int requestId = UnitAttackOrderRequestSystem.EnqueueClearCommandedAttackOrder(_entityManager, attacker);
        SystemHandle system = _world.CreateSystem<UnitAttackOrderRequestSystem>();
        system.Update(_world.Unmanaged);

        Assert.IsTrue(UnitAttackOrderRequestSystem.TryGetResult(_entityManager, requestId, out UnitAttackOrderResultElement result));
        Assert.AreEqual(1, result.Issued);
        Assert.AreEqual(1, result.Accepted);
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(attacker));
    }

    [Test]
    public void UnitAttackOrderRequestSystem_ConsumesClearAccidentalAirSelectionMoveRequest()
    {
        Entity attacker = CreateAttacker();
        _entityManager.AddComponent<UnitAirMovement>(attacker);
        _entityManager.AddComponent<ManualMoveOrderTag>(attacker);
        _entityManager.AddComponentData(attacker, new UnitTarget { Cell = new int2(2, 1) });
        _entityManager.AddComponentData(attacker, new UnitPathRequest { Goal = new int2(2, 1) });
        _entityManager.AddComponentData(attacker, new UnitPathFollow { PathIndex = 1 });
        _entityManager.AddComponentData(attacker, new UnitPathRange { Start = 0, Length = 2 });

        int requestId = UnitAttackOrderRequestSystem.EnqueueClearAccidentalAirSelectionMove(_entityManager, attacker);
        SystemHandle system = _world.CreateSystem<UnitAttackOrderRequestSystem>();
        system.Update(_world.Unmanaged);

        Assert.IsTrue(UnitAttackOrderRequestSystem.TryGetResult(_entityManager, requestId, out UnitAttackOrderResultElement result));
        Assert.AreEqual(1, result.Issued);
        Assert.AreEqual(1, result.Accepted);
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(attacker));
    }

    private Entity CreateAttacker()
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitCombat));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = new int2(1, 1) });
        _entityManager.SetComponentData(entity, new UnitCombat { CanAttack = 1 });
        return entity;
    }

    private Entity CreateTarget(int2 cell, float3 position)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
#endif
