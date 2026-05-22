#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class SelectionStateSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("SelectionStateSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void FocusedUnit_CanBeSetAndCleared()
    {
        var selectionState = new SelectionStateSystem();
        Entity unit = _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove));

        selectionState.SetFocusedUnit(unit);
        Assert.AreEqual(unit, selectionState.FocusedUnit);

        selectionState.ClearFocusedUnit();
        Assert.AreEqual(Entity.Null, selectionState.FocusedUnit);
    }

    [Test]
    public void CacheSelectedMoveEntity_KeepsOnlyPlayerMoveUnits()
    {
        var selectionState = new SelectionStateSystem();
        Entity playerUnit = CreateMoveUnit(0);
        Entity enemyUnit = CreateMoveUnit(1);
        Entity passengerUnit = CreateMoveUnit(0);
        _entityManager.AddComponentData(passengerUnit, new UnitTransportPassenger { Transport = Entity.Null });

        selectionState.CacheSelectedMoveEntity(_entityManager, playerUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, playerUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, enemyUnit);
        selectionState.CacheSelectedMoveEntity(_entityManager, passengerUnit);

        Assert.AreEqual(1, selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(playerUnit, selectionState.CachedSelectedMoveEntities[0]);
    }

    private Entity CreateMoveUnit(byte factionId)
    {
        Entity entity = _entityManager.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove));
        _entityManager.SetComponentData(entity, new Faction { Id = factionId });
        _entityManager.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        _entityManager.SetComponentData(entity, new UnitMove { Speed = 1f });
        return entity;
    }
}
#endif
