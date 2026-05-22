#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class UnitMoveOrderSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("UnitMoveOrderSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
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
            typeof(AutoWanderMoveTag));
        int2 goal = new(4, 5);

        moveOrderSystem.IssueImmediateMoveCommand(_entityManager, unit, goal);

        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(goal, _entityManager.GetComponentData<UnitPathRequest>(unit).Goal);
        Assert.IsTrue(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(unit));
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
    public void ClearMovementOrderComponents_RemovesSharedMoveOrderComponents()
    {
        var moveOrderSystem = new UnitMoveOrderSystem();
        Entity unit = _entityManager.CreateEntity(
            typeof(UnitTarget),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(UnitPathRange),
            typeof(ManualMoveOrderTag),
            typeof(AutoWanderMoveTag),
            typeof(EngageTarget));

        moveOrderSystem.ClearMovementOrderComponents(_entityManager, unit);

        Assert.IsFalse(_entityManager.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(_entityManager.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(_entityManager.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<AutoWanderMoveTag>(unit));
        Assert.IsFalse(_entityManager.HasComponent<EngageTarget>(unit));
    }
}
#endif
