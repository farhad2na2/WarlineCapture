#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class CustomGameStartupSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CustomGameStartupSystemTests();
            tests.InitializeFromLegacyConfigsCreatesStartupEntityAndBuffers();
            Debug.Log("[CustomGameStartupFocusedValidation] result=Passed tests=1");
        }
        catch (Exception exception)
        {
            Debug.LogError("[CustomGameStartupFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void InitializeFromLegacyConfigsCreatesStartupEntityAndBuffers()
    {
        using var world = new World("CustomGameStartupSystemTests");
        CustomGameStartupSystem system = world.GetOrCreateSystemManaged<CustomGameStartupSystem>();

        CustomGameStartupSystem.Result result = system.InitializeFromLegacyConfigs(null, null);

        Assert.IsTrue(result.Initialized);
        Assert.AreEqual(0, result.FactionCount);
        Assert.AreEqual(0, result.InitialUnitEntryCount);
        Assert.AreEqual(0, result.InitialBuildingEntryCount);
        Assert.AreEqual(0, result.UnitRegistryEntryCount);
        Assert.AreEqual(0, result.VisualEntryCount);

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        Assert.AreEqual(1, query.CalculateEntityCount());

        Entity entity = query.GetSingletonEntity();
        Assert.IsTrue(em.HasBuffer<UnitPrefabRegistryEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameUnitSourceRegistryEntry>(entity));
        Assert.IsTrue(em.HasBuffer<CustomGameVisualRegistryEntry>(entity));

        CustomGameStartupStateComponent state = em.GetComponentData<CustomGameStartupStateComponent>(entity);
        Assert.AreEqual(new FixedString64Bytes("custom.skirmish.legacy"), state.GameModeId);
    }
}
#endif
