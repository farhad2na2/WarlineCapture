#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class FocusedUnitCommandSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("FocusedUnitCommandSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
    }

    [Test]
    public void IssueImmediateSelectedUnitOrder_HoldStopsMovementAndEnablesAutoEngage()
    {
        Entity unit = CreateSelectedMoveUnit();
        _entityManager.AddComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        _entityManager.AddComponentData(unit, new EngageTarget { Target = Entity.Null, Cell = new int2(2, 3), IsCommanded = 1 });
        _entityManager.AddComponentData(unit, new UnitTarget { Cell = new int2(4, 5) });
        _entityManager.AddComponentData(unit, new UnitPathRequest { Goal = new int2(4, 5) });
        _entityManager.AddComponentData(unit, new UnitPathFollow { PathIndex = 3 });
        _entityManager.AddComponentData(unit, new UnitPathRange { Start = 1, Length = 2 });

        bool issued = new FocusedUnitCommandSystem().IssueImmediateSelectedUnitOrder(
            _entityManager,
            clearEngageTarget: true,
            holdPosition: true,
            new UnitMoveOrderSystem());

        Assert.IsTrue(issued);
        Assert.IsTrue(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.AreEqual(1, _entityManager.GetComponentData<UnitCombat>(unit).AutoEngage);
    }

    [Test]
    public void IssueImmediateSelectedUnitOrder_StopClearsHoldAndDisablesAutoEngage()
    {
        Entity unit = CreateSelectedMoveUnit();
        _entityManager.AddComponentData(unit, new HoldPositionOrderTag());
        _entityManager.AddComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });

        bool issued = new FocusedUnitCommandSystem().IssueImmediateSelectedUnitOrder(
            _entityManager,
            clearEngageTarget: true,
            holdPosition: false,
            new UnitMoveOrderSystem());

        Assert.IsTrue(issued);
        Assert.IsFalse(_entityManager.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(0, _entityManager.GetComponentData<UnitCombat>(unit).AutoEngage);
    }

    [Test]
    public void EnableFocusedUnitAutoAttack_ClearsCommandedAttackOrderThroughRequest()
    {
        Entity unit = CreateSelectedMoveUnit();
        _entityManager.AddComponentData(unit, new EngageTarget { Target = Entity.Null, Cell = new int2(2, 3), IsCommanded = 1 });
        _entityManager.AddComponentData(unit, new BaseBreachOrder { FinalTarget = Entity.Null, FinalCell = new int2(4, 5) });
        _entityManager.AddComponentData(unit, new UnitTarget { Cell = new int2(6, 7) });
        _entityManager.AddComponentData(unit, new UnitPathRequest { Goal = new int2(6, 7) });
        _entityManager.AddComponentData(unit, new UnitPathFollow { PathIndex = 1 });
        _entityManager.AddComponentData(unit, new UnitPathRange { Start = 0, Length = 2 });

        new FocusedUnitCommandSystem().EnableFocusedUnitAutoAttack(
            _entityManager,
            unit);

        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
    }

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.IssueImmediateSelectedUnitOrder_HoldStopsMovementAndEnablesAutoEngage());
            RunCase(test => test.IssueImmediateSelectedUnitOrder_StopClearsHoldAndDisablesAutoEngage());
            RunCase(test => test.EnableFocusedUnitAutoAttack_ClearsCommandedAttackOrderThroughRequest());
            Debug.Log("[FocusedUnitCommandFocusedValidation] result=Passed tests=3");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[FocusedUnitCommandFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<FocusedUnitCommandSystemTests> testCase)
    {
        FocusedUnitCommandSystemTests tests = new();
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

    private Entity CreateSelectedMoveUnit()
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitGrid),
            typeof(UnitMove));
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = new int2(1, 1) });
        _entityManager.SetComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        return entity;
    }
}
#endif
