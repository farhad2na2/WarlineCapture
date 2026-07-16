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
            Debug.Log("[WorldScopedComponentQueryCache] result=Passed tests=3");
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

    private static void RunTest(Action<WorldScopedComponentQueryCacheTests> action)
    {
        action(new WorldScopedComponentQueryCacheTests());
    }
}
