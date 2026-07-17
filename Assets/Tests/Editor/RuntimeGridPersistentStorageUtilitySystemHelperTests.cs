using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class RuntimeGridPersistentStorageUtilitySystemHelperTests
{
    private World _world;
    private Entity _grid;

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(RuntimeGridPersistentStorageUtilitySystemHelperTests));
        _grid = _world.EntityManager.CreateEntity(
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent));
    }

    [TearDown]
    public void TearDown()
    {
        if (_world == null || !_world.IsCreated)
            return;
        RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(_world.EntityManager, _grid);
        _world.Dispose();
    }

    [Test]
    public void EnsureStorage_CreatesEveryContainerWithExpectedInitialization()
    {
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);

        DynamicBlockerComponent blocker = _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        DynamicOccupancyComponent occupancy = _world.EntityManager.GetComponentData<DynamicOccupancyComponent>(_grid);
        PathPoolComponent pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        Assert.AreEqual(8, blocker.GridSize);
        Assert.AreEqual(8, blocker.Counts.Length);
        Assert.AreEqual(8, blocker.Blocked.Length);
        Assert.AreEqual(8, blocker.FriendlyPassFactionIds.Length);
        Assert.AreEqual(8, occupancy.GridSize);
        Assert.AreEqual(8, occupancy.Occupied.Length);
        Assert.IsTrue(pathPool.Cells.IsCreated);
        for (int index = 0; index < 8; index++)
        {
            Assert.AreEqual(0, blocker.Counts[index]);
            Assert.IsFalse(blocker.Blocked.IsSet(index));
            Assert.AreEqual(byte.MaxValue, blocker.FriendlyPassFactionIds[index]);
            Assert.IsFalse(occupancy.Occupied.IsSet(index));
        }
    }

    [Test]
    public void EnsureStorage_SameSizePreservesLiveContainerContents()
    {
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);
        DynamicBlockerComponent blocker = _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        DynamicOccupancyComponent occupancy = _world.EntityManager.GetComponentData<DynamicOccupancyComponent>(_grid);
        PathPoolComponent pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        blocker.Counts[2] = 7;
        blocker.Blocked.Set(3, true);
        occupancy.Occupied.Set(4, true);
        pathPool.Cells.Add(new int2(5, 6));

        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);

        blocker = _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        occupancy = _world.EntityManager.GetComponentData<DynamicOccupancyComponent>(_grid);
        pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        Assert.AreEqual(7, blocker.Counts[2]);
        Assert.IsTrue(blocker.Blocked.IsSet(3));
        Assert.IsTrue(occupancy.Occupied.IsSet(4));
        Assert.AreEqual(1, pathPool.Cells.Length);
        Assert.AreEqual(new int2(5, 6), pathPool.Cells[0]);
    }

    [Test]
    public void EnsureStorage_ResizeReplacesGridStorageAndClearsPathPool()
    {
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);
        PathPoolComponent pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        pathPool.Cells.Add(new int2(5, 6));

        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 12);

        DynamicBlockerComponent blocker = _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        DynamicOccupancyComponent occupancy = _world.EntityManager.GetComponentData<DynamicOccupancyComponent>(_grid);
        pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        Assert.AreEqual(12, blocker.Counts.Length);
        Assert.AreEqual(12, blocker.Blocked.Length);
        Assert.AreEqual(12, blocker.FriendlyPassFactionIds.Length);
        Assert.AreEqual(12, occupancy.Occupied.Length);
        Assert.AreEqual(0, pathPool.Cells.Length);
    }

    [Test]
    public void DisposeStorage_IsIdempotentAndClearsComponentAliases()
    {
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);

        RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(_world.EntityManager, _grid);
        RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(_world.EntityManager, _grid);

        DynamicBlockerComponent blocker = _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        DynamicOccupancyComponent occupancy = _world.EntityManager.GetComponentData<DynamicOccupancyComponent>(_grid);
        PathPoolComponent pathPool = _world.EntityManager.GetComponentData<PathPoolComponent>(_grid);
        Assert.IsFalse(blocker.Counts.IsCreated);
        Assert.IsFalse(blocker.Blocked.IsCreated);
        Assert.IsFalse(blocker.FriendlyPassFactionIds.IsCreated);
        Assert.IsFalse(occupancy.Occupied.IsCreated);
        Assert.IsFalse(pathPool.Cells.IsCreated);
    }
}
