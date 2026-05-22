#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

public sealed class BuildingCombatSystemTests
{
    private World _world;

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        _world = null;
    }

    [Test]
    public void TryMarkDestroyed_SetsDestroyedStateAndCleanupDeadline()
    {
        var building = new TestRuntimeBuilding { Id = 7 };
        var system = new BuildingCombatSystem();

        Assert.IsTrue(system.TryMarkDestroyed(building, 10f, 5f));
        Assert.IsTrue(building.IsDestroyed);
        Assert.AreEqual(15f, building.DestroyedCleanupAt);
        Assert.IsFalse(system.TryMarkDestroyed(building, 20f, 5f));
        Assert.AreEqual(15f, building.DestroyedCleanupAt);
    }

    [Test]
    public void CollectDestroyedCleanupIds_ReturnsExpiredDestroyedBuildingsOnly()
    {
        var buildings = new Dictionary<int, TestRuntimeBuilding>
        {
            { 1, new TestRuntimeBuilding { Id = 1, IsDestroyed = true, DestroyedCleanupAt = 9f } },
            { 2, new TestRuntimeBuilding { Id = 2, IsDestroyed = true, DestroyedCleanupAt = 11f } },
            { 3, new TestRuntimeBuilding { Id = 3, IsDestroyed = false, DestroyedCleanupAt = 1f } }
        };

        var system = new BuildingCombatSystem();
        List<int> cleanupIds = system.CollectDestroyedCleanupIds(buildings, 10f);

        Assert.IsNotNull(cleanupIds);
        CollectionAssert.AreEqual(new[] { 1 }, cleanupIds);
    }

    [Test]
    public void ResolveRuntimeCombatState_DetectsMissingAndDeadCombatEntities()
    {
        _world = new World("BuildingCombatSystemTests");
        EntityManager em = _world.EntityManager;
        Entity live = em.CreateEntity(typeof(UnitHealth));
        em.SetComponentData(live, new UnitHealth { Current = 25, Max = 100 });
        Entity dead = em.CreateEntity(typeof(UnitHealth));
        em.SetComponentData(dead, new UnitHealth { Current = 0, Max = 100 });
        Entity missing = em.CreateEntity(typeof(UnitHealth));
        em.DestroyEntity(missing);

        var system = new BuildingCombatSystem();

        Assert.AreEqual(
            BuildingCombatSystem.RuntimeCombatState.Active,
            system.ResolveRuntimeCombatState(new TestRuntimeBuilding { CombatEntity = live }, em));
        Assert.AreEqual(
            BuildingCombatSystem.RuntimeCombatState.DeadCombatEntity,
            system.ResolveRuntimeCombatState(new TestRuntimeBuilding { CombatEntity = dead }, em));
        Assert.AreEqual(
            BuildingCombatSystem.RuntimeCombatState.MissingCombatEntity,
            system.ResolveRuntimeCombatState(new TestRuntimeBuilding { CombatEntity = missing }, em));
    }

    [Test]
    public void DestroyBlockerEntity_DestroysEntityAndClearsReference()
    {
        _world = new World("BuildingCombatSystemTests");
        EntityManager em = _world.EntityManager;
        Entity blocker = em.CreateEntity();
        var building = new TestRuntimeBuilding { BlockerEntity = blocker };

        var system = new BuildingCombatSystem();
        system.DestroyBlockerEntity(building, em);

        Assert.IsFalse(em.Exists(blocker));
        Assert.AreEqual(Entity.Null, building.BlockerEntity);
    }

    private sealed class TestRuntimeBuilding : BuildingCombatSystem.IRuntimeBuilding
    {
        public int Id { get; set; }
        public bool IsDestroyed { get; set; }
        public float DestroyedCleanupAt { get; set; }
        public Entity CombatEntity { get; set; }
        public Entity BlockerEntity { get; set; }
    }
}
#endif
