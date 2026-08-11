using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RuntimeGridPersistentStorageUtilitySystemHelperTests
{
    private World _world;
    private Entity _grid;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(EnsureStorage_CreatesEveryContainerWithExpectedInitialization),
                test => test.EnsureStorage_CreatesEveryContainerWithExpectedInitialization(), ref passed);
            Run(nameof(EnsureStorage_SameSizePreservesLiveContainerContents),
                test => test.EnsureStorage_SameSizePreservesLiveContainerContents(), ref passed);
            Run(nameof(EnsureStorage_ResizeReplacesGridStorageAndClearsPathPool),
                test => test.EnsureStorage_ResizeReplacesGridStorageAndClearsPathPool(), ref passed);
            Run(nameof(DisposeStorage_IsIdempotentAndClearsComponentAliases),
                test => test.DisposeStorage_IsIdempotentAndClearsComponentAliases(), ref passed);
            Run(nameof(WorldReplacement_ReleasesOldStorageBeforeIndependentRecreation),
                test => test.WorldReplacement_ReleasesOldStorageBeforeIndependentRecreation(), ref passed);
            Run(nameof(OwnershipBoundary_HasOneAllocatorResizerAndDisposer),
                test => test.OwnershipBoundary_HasOneAllocatorResizerAndDisposer(), ref passed);
            Debug.Log($"[RuntimeGridPersistentStorageValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[RuntimeGridPersistentStorageValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

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

    [Test]
    public void WorldReplacement_ReleasesOldStorageBeforeIndependentRecreation()
    {
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 8);
        Assert.IsTrue(RuntimeGridPersistentStorageUtilitySystemHelper.IsStorageValid(
            _world.EntityManager, _grid, 8));

        RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(_world.EntityManager, _grid);
        Assert.IsFalse(RuntimeGridPersistentStorageUtilitySystemHelper.IsStorageValid(
            _world.EntityManager, _grid, 8));
        AssertStorageAliasesAreReleased(_world.EntityManager, _grid);
        _world.Dispose();

        _world = new World(nameof(WorldReplacement_ReleasesOldStorageBeforeIndependentRecreation) + ".Replacement");
        _grid = CreateStorageEntity(_world.EntityManager);
        RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(_world.EntityManager, _grid, 12);

        Assert.IsTrue(RuntimeGridPersistentStorageUtilitySystemHelper.IsStorageValid(
            _world.EntityManager, _grid, 12));
        DynamicBlockerComponent replacement =
            _world.EntityManager.GetComponentData<DynamicBlockerComponent>(_grid);
        Assert.AreEqual(12, replacement.Counts.Length);
        Assert.AreEqual(byte.MaxValue, replacement.FriendlyPassFactionIds[11]);
    }

    [Test]
    public void OwnershipBoundary_HasOneAllocatorResizerAndDisposer()
    {
        string systemsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "Game/Scripts/Systems"));
        string helperPath = Path.Combine(systemsRoot, "RuntimeGridPersistentStorageUtilitySystemHelper.cs");
        string helperSource = File.ReadAllText(helperPath);

        Assert.AreEqual(199, File.ReadAllLines(helperPath).Length);
        Assert.AreEqual(8699, new FileInfo(helperPath).Length);
        Assert.AreEqual(5, CountOccurrences(helperSource, "Allocator.Persistent"));
        Assert.AreEqual(1, CountOccurrences(helperSource, "private static DynamicBlockerComponent CreateBlockerStorage("));
        Assert.AreEqual(1, CountOccurrences(helperSource, "private static void DisposeBlocker("));
        Assert.AreEqual(1, CountOccurrences(helperSource, "private static void DisposeOccupancy("));
        Assert.That(helperSource, Does.Not.Contain("World.DefaultGameObjectInjectionWorld"));
        Assert.That(helperSource, Does.Not.Contain("static readonly"));
        Assert.That(helperSource, Does.Not.Contain("Update("));

        foreach (string sourcePath in Directory.GetFiles(systemsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(sourcePath, helperPath, StringComparison.OrdinalIgnoreCase))
                continue;
            string source = File.ReadAllText(sourcePath);
            Assert.That(source, Does.Not.Contain("new DynamicBlockerComponent"), sourcePath);
            Assert.That(source, Does.Not.Contain("new DynamicOccupancyComponent"), sourcePath);
            Assert.That(source, Does.Not.Contain("new PathPoolComponent"), sourcePath);
        }

        string lifecycleSource = File.ReadAllText(Path.Combine(systemsRoot, "DynamicBlockerInitSystem.cs"));
        int onDestroy = lifecycleSource.IndexOf("public void OnDestroy(ref SystemState state)", StringComparison.Ordinal);
        int dispose = lifecycleSource.IndexOf(
            "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(", StringComparison.Ordinal);
        int onUpdate = lifecycleSource.IndexOf("public void OnUpdate(ref SystemState state)", StringComparison.Ordinal);
        Assert.That(onDestroy, Is.GreaterThanOrEqualTo(0));
        Assert.That(dispose, Is.GreaterThan(onDestroy));
        Assert.That(dispose, Is.LessThan(onUpdate));
    }

    private static Entity CreateStorageEntity(EntityManager entityManager)
    {
        return entityManager.CreateEntity(
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent));
    }

    private static void AssertStorageAliasesAreReleased(EntityManager entityManager, Entity entity)
    {
        DynamicBlockerComponent blocker = entityManager.GetComponentData<DynamicBlockerComponent>(entity);
        DynamicOccupancyComponent occupancy = entityManager.GetComponentData<DynamicOccupancyComponent>(entity);
        PathPoolComponent pathPool = entityManager.GetComponentData<PathPoolComponent>(entity);
        Assert.IsFalse(blocker.Counts.IsCreated);
        Assert.IsFalse(blocker.Blocked.IsCreated);
        Assert.IsFalse(blocker.FriendlyPassFactionIds.IsCreated);
        Assert.IsFalse(occupancy.Occupied.IsCreated);
        Assert.IsFalse(pathPool.Cells.IsCreated);
    }

    private static int CountOccurrences(string source, string value)
    {
        return source.Split(new[] { value }, StringSplitOptions.None).Length - 1;
    }

    private static void Run(
        string name,
        Action<RuntimeGridPersistentStorageUtilitySystemHelperTests> action,
        ref int passed)
    {
        var tests = new RuntimeGridPersistentStorageUtilitySystemHelperTests();
        tests.SetUp();
        try
        {
            action(tests);
            passed++;
        }
        finally
        {
            tests.TearDown();
        }
    }
}
