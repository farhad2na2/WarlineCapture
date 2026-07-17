using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class InitialFactionSpawnCellSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new InitialFactionSpawnCellSystemTests();
            tests.TryGetConfiguredFactionSpawnCellPrefersActiveMapDeploymentAnchor();
            tests.TryGetConfiguredFactionSpawnCellUsesActiveMapSpawnAnchorWhenDeploymentIsAbsent();
            tests.TryGetConfiguredFactionSpawnCellRejectsAmbiguousActiveMapAnchors();
            tests.CurrentCompatibilityDefinitionResolvesFactionDeploymentCells();
            tests.TryGetConfiguredFactionSpawnCellPrefersBakedEcsSpawnBuffer();
            tests.TryGetConfiguredFactionSpawnCellFallsBackToSerializedConfig();
            Debug.Log("[InitialFactionSpawnCellFocusedValidation] result=Passed tests=6");
        }
        catch (Exception exception)
        {
            Debug.LogError("[InitialFactionSpawnCellFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellPrefersActiveMapDeploymentAnchor()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_ActiveDeployment");
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(
            world.EntityManager,
            new OperationMapAnchorBlob
            {
                Id = new FixedString64Bytes("anchor.test.deployment.faction3"),
                Kind = OperationMapAnchorKind.Deployment,
                Position = new float3(11f, 0f, 15f),
                FactionId = 3,
                LaneIndex = -1
            });
        try
        {
            AddBakedSpawn(world.EntityManager, 3, new int2(31, 47));
            InitialFactionSpawnCellSystem system = new();
            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(
                world.EntityManager, null, 3, out int2 spawnCell));
            Assert.AreEqual(new int2(5, 7), spawnCell);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellUsesActiveMapSpawnAnchorWhenDeploymentIsAbsent()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_ActiveSpawn");
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(
            world.EntityManager,
            new OperationMapAnchorBlob
            {
                Id = new FixedString64Bytes("anchor.test.spawn.faction4"),
                Kind = OperationMapAnchorKind.Spawn,
                Position = new float3(17f, 0f, 9f),
                FactionId = 4,
                LaneIndex = -1
            });
        try
        {
            InitialFactionSpawnCellSystem system = new();
            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(
                world.EntityManager, null, 4, out int2 spawnCell));
            Assert.AreEqual(new int2(8, 4), spawnCell);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellRejectsAmbiguousActiveMapAnchors()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_Ambiguous");
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(
            world.EntityManager,
            new OperationMapAnchorBlob
            {
                Id = new FixedString64Bytes("anchor.test.deployment.a"),
                Kind = OperationMapAnchorKind.Deployment,
                Position = new float3(11f, 0f, 15f),
                FactionId = 2,
                LaneIndex = -1
            },
            new OperationMapAnchorBlob
            {
                Id = new FixedString64Bytes("anchor.test.deployment.b"),
                Kind = OperationMapAnchorKind.Deployment,
                Position = new float3(21f, 0f, 25f),
                FactionId = 2,
                LaneIndex = -1
            });
        try
        {
            InitialFactionSpawnCellSystem system = new();
            Assert.Throws<InvalidOperationException>(() =>
                system.TryGetConfiguredFactionSpawnCell(world.EntityManager, null, 2, out _));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void CurrentCompatibilityDefinitionResolvesFactionDeploymentCells()
    {
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);
        Assert.That(definition, Is.Not.Null);
        Assert.That(
            definition.TryCreatePersistentMetadataBlob(
                out BlobAssetReference<OperationMapBlob> blob,
                out string error),
            Is.True,
            error);

        using var world = new World("InitialFactionSpawnCellSystemTests_CurrentCompatibility");
        try
        {
            AddActiveMap(world.EntityManager, blob);
            InitialFactionSpawnCellSystem system = new();
            Assert.That(
                system.TryGetConfiguredFactionSpawnCell(
                    world.EntityManager,
                    null,
                    1,
                    out int2 faction1Cell),
                Is.True);
            Assert.That(faction1Cell, Is.EqualTo(new int2(949, 344)));
            Assert.That(
                system.TryGetConfiguredFactionSpawnCell(
                    world.EntityManager,
                    null,
                    2,
                    out int2 faction2Cell),
                Is.True);
            Assert.That(faction2Cell, Is.EqualTo(new int2(1686, 108)));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void TryGetConfiguredFactionSpawnCellPrefersBakedEcsSpawnBuffer()
    {
        using var world = new World("InitialFactionSpawnCellSystemTests_Baked");
        EntityManager em = world.EntityManager;
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(em);
        Entity configEntity = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.AddBuffer<InitialUnitsFactionSpawnEntry>(configEntity);
        factionSpawns.Add(new InitialUnitsFactionSpawnEntry
        {
            FactionId = 3,
            SpawnCell = new int2(31, 47)
        });

        InitialFactionSpawnCellSystem system = new();
        InitialUnitsSpawnerAuthoringConfig fallbackConfig = CreateFallbackConfig(3, new Vector2Int(99, 100));
        try
        {
            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(
                em,
                CreateFallbackEntries(fallbackConfig),
                3,
                out int2 spawnCell));
            Assert.AreEqual(new int2(31, 47), spawnCell);
        }
        finally
        {
            blob.Dispose();
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
            InitialFactionSpawnCellSystem system = new();

            Assert.IsTrue(system.TryGetConfiguredFactionSpawnCell(
                world.EntityManager,
                CreateFallbackEntries(fallbackConfig),
                4,
                out int2 spawnCell));
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

    private static void AddBakedSpawn(EntityManager entityManager, byte factionId, int2 spawnCell)
    {
        Entity configEntity = entityManager.CreateEntity(typeof(InitialUnitsSpawnConfig));
        DynamicBuffer<InitialUnitsFactionSpawnEntry> spawns =
            entityManager.AddBuffer<InitialUnitsFactionSpawnEntry>(configEntity);
        spawns.Add(new InitialUnitsFactionSpawnEntry { FactionId = factionId, SpawnCell = spawnCell });
    }

    private static BlobAssetReference<OperationMapBlob> AddActiveMap(
        EntityManager entityManager,
        params OperationMapAnchorBlob[] sourceAnchors)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob metadata = ref builder.ConstructRoot<OperationMapBlob>();
        metadata.OperationMapId = new FixedString64Bytes("opmap.test.spawn");
        metadata.Grid = new OperationMapGridBlob
        {
            Origin = float3.zero,
            Dimensions = new int2(32, 32),
            CellSize = 2f
        };
        BlobBuilderArray<OperationMapAnchorBlob> anchors =
            builder.Allocate(ref metadata.Anchors, sourceAnchors.Length);
        for (int index = 0; index < sourceAnchors.Length; index++)
            anchors[index] = sourceAnchors[index];

        BlobAssetReference<OperationMapBlob> blob =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = metadata.OperationMapId,
            Generation = 1
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent { Blob = blob, Generation = 1 });
        return blob;
    }

    private static void AddActiveMap(
        EntityManager entityManager,
        BlobAssetReference<OperationMapBlob> blob)
    {
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = blob.Value.OperationMapId,
            Generation = 1
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 1
        });
    }

    private static InitialFactionSpawnCellFallbackEntry[] CreateFallbackEntries(InitialUnitsSpawnerAuthoringConfig config)
    {
        InitialFactionSpawnCellFallbackEntry[] entries = new InitialFactionSpawnCellFallbackEntry[config.Factions.Count];
        for (int i = 0; i < entries.Length; i++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[i];
            entries[i] = new InitialFactionSpawnCellFallbackEntry(
                (byte)Mathf.Clamp(faction.FactionId, 0, byte.MaxValue),
                new int2(faction.SpawnCell.x, faction.SpawnCell.y));
        }

        return entries;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
#endif
