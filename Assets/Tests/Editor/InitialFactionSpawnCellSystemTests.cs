#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class InitialFactionSpawnCellSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new InitialFactionSpawnCellSystemTests();
            tests.TryGetConfiguredFactionSpawnCellPrefersBakedEcsSpawnBuffer();
            tests.TryGetConfiguredFactionSpawnCellFallsBackToSerializedConfig();
            Debug.Log("[InitialFactionSpawnCellFocusedValidation] result=Passed tests=2");
        }
        catch (Exception exception)
        {
            Debug.LogError("[InitialFactionSpawnCellFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellPrefersBakedEcsSpawnBuffer()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_Baked");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.AddBuffer<InitialUnitsFactionSpawnEntry>(configEntity);
        factionSpawns.Add(new InitialUnitsFactionSpawnEntry
        {
            FactionId = 3,
            SpawnCell = new int2(31, 47)
        });

        InitialFactionSpawnCellSystem system = world.GetOrCreateSystemManaged<InitialFactionSpawnCellSystem>();
        InitialUnitsSpawnerAuthoringConfig fallbackConfig = CreateFallbackConfig(3, new Vector2Int(99, 100));
        try
        {
            system.Configure(fallbackConfig);

            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(3, out int2 spawnCell));
            Assert.AreEqual(new int2(31, 47), spawnCell);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fallbackConfig);
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellFallsBackToSerializedConfig()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_Fallback");
        InitialUnitsSpawnerAuthoringConfig fallbackConfig = CreateFallbackConfig(4, new Vector2Int(44, 55));
        try
        {
            InitialFactionSpawnCellSystem system = world.GetOrCreateSystemManaged<InitialFactionSpawnCellSystem>();
            system.Configure(fallbackConfig);

            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(4, out int2 spawnCell));
            Assert.AreEqual(new int2(44, 55), spawnCell);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fallbackConfig);
        }
    }

    private static InitialUnitsSpawnerAuthoringConfig CreateFallbackConfig(int factionId, Vector2Int spawnCell)
    {
        InitialUnitsSpawnerAuthoringConfig config = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringConfig>();
        var factionEntry = new InitialUnitsSpawnerAuthoringConfig.FactionEntry();
        SetPrivateField(factionEntry, "factionId", factionId);
        SetPrivateField(factionEntry, "spawnCell", spawnCell);
        SetPrivateField(config, "factions", new List<InitialUnitsSpawnerAuthoringConfig.FactionEntry> { factionEntry });
        return config;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
#endif
