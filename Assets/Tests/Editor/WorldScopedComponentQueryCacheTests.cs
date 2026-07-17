using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Runtime;

public sealed class WorldScopedComponentQueryCacheTests
{
    public static void RunArchitectureQueryCacheValidation()
    {
        try
        {
            RunTest(test => test.ReadOnlyCache_MatchesOnlyRequestedComponent());
            RunTest(test => test.ReadWriteCache_MatchesOnlyRequestedComponent());
            RunTest(test => test.Cache_RebuildsAgainstDifferentWorld());
            RunTest(test => test.SingletonCache_ReusesPositiveLookup());
            RunTest(test => test.SingletonCache_CachesNegativeLookupUntilInvalidated());
            RunTest(test => test.SingletonCache_FailsClosedAfterPositiveCardinalityChanges());
            RunTest(test => test.SingletonCache_RecoversAfterResolvedEntityIsDestroyed());
            RunTest(test => test.SingletonCache_RecoversAfterResolvedEntityLosesComponent());
            RunTest(test => test.SingletonCache_RejectsEnableableComponentTypes());
            RunTest(test => test.Dispose_IsIdempotentAndRejectsFurtherUse());
            RunTest(test => test.Dispose_IsSafeAfterBoundWorldIsDestroyed());
            Debug.Log("[WorldScopedComponentQueryCache] result=Passed tests=11");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[WorldScopedComponentQueryCache] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ReadOnlyCache_MatchesOnlyRequestedComponent()
    {
        using World world = new(nameof(ReadOnlyCache_MatchesOnlyRequestedComponent));
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        entityManager.CreateEntity(typeof(UnitGrid));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);

        Assert.AreEqual(1, cache.Get(entityManager).CalculateEntityCount());
        Assert.AreEqual(1, cache.Get(entityManager).CalculateEntityCount());
    }

    [Test]
    public void ReadWriteCache_MatchesOnlyRequestedComponent()
    {
        using World world = new(nameof(ReadWriteCache_MatchesOnlyRequestedComponent));
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
        entityManager.CreateEntity(typeof(UnitGrid));
        var cache = new WorldScopedComponentQueryCache<BuildingResourceStorageComponent>(readOnly: false);

        Assert.AreEqual(1, cache.Get(entityManager).CalculateEntityCount());
    }

    [Test]
    public void Cache_RebuildsAgainstDifferentWorld()
    {
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        using (World firstWorld = new("WorldScopedComponentQueryCache_First"))
        {
            firstWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            Assert.AreEqual(1, cache.Get(firstWorld.EntityManager).CalculateEntityCount());
        }

        using World secondWorld = new("WorldScopedComponentQueryCache_Second");
        secondWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        secondWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        Assert.AreEqual(2, cache.Get(secondWorld.EntityManager).CalculateEntityCount());
    }

    [Test]
    public void SingletonCache_ReusesPositiveLookup()
    {
        using World world = new(nameof(SingletonCache_ReusesPositiveLookup));
        Entity expected = world.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);

        Assert.IsTrue(cache.TryGetSingleton(world.EntityManager, out Entity first));
        Assert.IsTrue(cache.TryGetSingleton(world.EntityManager, out Entity second));
        Assert.AreEqual(expected, first);
        Assert.AreEqual(expected, second);
    }

    [Test]
    public void SingletonCache_CachesNegativeLookupUntilInvalidated()
    {
        using World world = new(nameof(SingletonCache_CachesNegativeLookupUntilInvalidated));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);

        Assert.IsFalse(cache.TryGetSingleton(world.EntityManager, out Entity missing));
        Assert.AreEqual(Entity.Null, missing);
        Entity expected = world.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        Assert.IsFalse(cache.TryGetSingleton(world.EntityManager, out _));

        cache.Invalidate();

        Assert.IsTrue(cache.TryGetSingleton(world.EntityManager, out Entity resolved));
        Assert.AreEqual(expected, resolved);
    }

    [Test]
    public void SingletonCache_RecoversAfterResolvedEntityIsDestroyed()
    {
        using World world = new(nameof(SingletonCache_RecoversAfterResolvedEntityIsDestroyed));
        EntityManager entityManager = world.EntityManager;
        Entity first = entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        Assert.IsTrue(cache.TryGetSingleton(entityManager, out Entity resolvedFirst));
        Assert.AreEqual(first, resolvedFirst);

        entityManager.DestroyEntity(first);
        Entity replacement = entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));

        Assert.IsTrue(cache.TryGetSingleton(entityManager, out Entity resolvedReplacement));
        Assert.AreEqual(replacement, resolvedReplacement);
    }

    [Test]
    public void SingletonCache_FailsClosedAfterPositiveCardinalityChanges()
    {
        using World world = new(nameof(SingletonCache_FailsClosedAfterPositiveCardinalityChanges));
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        Assert.IsTrue(cache.TryGetSingleton(entityManager, out _));

        entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));

        Assert.Throws<InvalidOperationException>(() => cache.TryGetSingleton(entityManager, out _));
    }

    [Test]
    public void SingletonCache_RecoversAfterResolvedEntityLosesComponent()
    {
        using World world = new(nameof(SingletonCache_RecoversAfterResolvedEntityLosesComponent));
        EntityManager entityManager = world.EntityManager;
        Entity first = entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        Assert.IsTrue(cache.TryGetSingleton(entityManager, out Entity resolvedFirst));
        Assert.AreEqual(first, resolvedFirst);

        entityManager.RemoveComponent<UnitMoveOrderQueueComponent>(first);
        Entity replacement = entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));

        Assert.IsTrue(cache.TryGetSingleton(entityManager, out Entity resolvedReplacement));
        Assert.AreEqual(replacement, resolvedReplacement);
    }

    [Test]
    public void SingletonCache_RejectsEnableableComponentTypes()
    {
        using World world = new(nameof(SingletonCache_RejectsEnableableComponentTypes));
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateEntity(typeof(EnableableCacheComponent));
        var cache = new WorldScopedComponentQueryCache<EnableableCacheComponent>(readOnly: true);

        Assert.AreEqual(1, cache.Get(entityManager).CalculateEntityCount());
        Assert.Throws<NotSupportedException>(() => cache.TryGetSingleton(entityManager, out _));
    }

    [Test]
    public void Dispose_IsIdempotentAndRejectsFurtherUse()
    {
        using World world = new(nameof(Dispose_IsIdempotentAndRejectsFurtherUse));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        cache.Get(world.EntityManager);

        cache.Dispose();
        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cache.Get(world.EntityManager));
        Assert.Throws<ObjectDisposedException>(() => cache.TryGetSingleton(world.EntityManager, out _));
        Assert.Throws<ObjectDisposedException>(() => cache.Invalidate());
    }

    [Test]
    public void Dispose_IsSafeAfterBoundWorldIsDestroyed()
    {
        var world = new World(nameof(Dispose_IsSafeAfterBoundWorldIsDestroyed));
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        cache.Get(world.EntityManager);
        world.Dispose();

        Assert.DoesNotThrow(cache.Dispose);
        Assert.Throws<ObjectDisposedException>(() => cache.Invalidate());
    }

    private struct EnableableCacheComponent : IComponentData, IEnableableComponent
    {
    }

    private static void RunTest(Action<WorldScopedComponentQueryCacheTests> action)
    {
        action(new WorldScopedComponentQueryCacheTests());
    }
}
