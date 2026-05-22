#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class UnitTargetOrderSystemTests
{
    private World _world;
    private EntityManager _entityManager;

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
        _entityManager.AddComponent<UnitPathFollow>(attacker);
        _entityManager.AddComponent<UnitPathRange>(attacker);
        _entityManager.AddComponentData(attacker, new UnitPathRequest { Goal = new int2(1, 1) });
        _entityManager.AddComponentData(attacker, new UnitTarget { Cell = new int2(1, 1) });

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
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(attacker));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(attacker));
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
    public void IssueDirectAttackTarget_WritesCommandedEngageTarget()
    {
        var targetOrderSystem = new UnitTargetOrderSystem();
        Entity attacker = CreateAttacker();
        Entity target = CreateTarget(new int2(2, 3), new float3(2.5f, 0f, 3.5f));

        targetOrderSystem.IssueDirectAttackTarget(_entityManager, attacker, target, new int2(2, 3), new float3(2.5f, 0f, 3.5f));

        EngageTarget engageTarget = _entityManager.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(2, 3), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    private Entity CreateAttacker()
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitCombat));
        _entityManager.SetComponentData(entity, new Faction { Id = 0 });
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
        _entityManager.SetComponentData(entity, new Faction { Id = 1 });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
#endif
