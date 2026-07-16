using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapGridStartupBindingTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new OperationMapGridStartupBindingTests();
            tests.ActiveMetadata_MatchingCompatibilityGridPublishesAuthoredBlockedCells();
            tests.ActiveMetadata_GridOrBlockedCountMismatchFailsClosed();
            tests.ActiveMetadata_ZeroBlockedCellsDoesNotRequireCompatibilityAsset();
            tests.ActiveMetadata_MissingRequiredCapabilityFailsClosed();
            tests.NoActiveMap_UsesCompatibilityGrid();
            Debug.Log("[OperationMapGridStartupBindingFocusedValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[OperationMapGridStartupBindingFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ActiveMetadata_MatchingCompatibilityGridPublishesAuthoredBlockedCells()
    {
        using World world = new("OperationMapGridStartupBindingTests.Matching");
        GridAuthoringConfig config = CreateConfig(8, 6, 2f, new Vector3(3f, 0f, 4f),
            new Vector2Int(1, 2), new Vector2Int(5, 4));
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(world.EntityManager, 8, 6, 2f, 2);
        try
        {
            Assert.That(OperationMapGridStartupBinding.TryResolve(
                world.EntityManager,
                config,
                out GridConfig grid,
                out Vector2Int[] blockedCells,
                out bool hasActiveMap,
                out string error), Is.True, error);
            Assert.That(hasActiveMap, Is.True);
            Assert.That(grid.Origin, Is.EqualTo(new float3(3f, 0f, 4f)));
            Assert.That(blockedCells, Is.EqualTo(config.BlockedCells));
        }
        finally
        {
            blob.Dispose();
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void ActiveMetadata_GridOrBlockedCountMismatchFailsClosed()
    {
        using World world = new("OperationMapGridStartupBindingTests.Mismatch");
        GridAuthoringConfig config = CreateConfig(9, 6, 2f, new Vector3(3f, 0f, 4f), new Vector2Int(1, 2));
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(world.EntityManager, 8, 6, 2f, 2);
        try
        {
            Assert.That(OperationMapGridStartupBinding.TryResolve(
                world.EntityManager, config, out _, out _, out bool hasActiveMap, out string error), Is.False);
            Assert.That(hasActiveMap, Is.True);
            Assert.That(error, Does.Contain("blocked-cell count"));
        }
        finally
        {
            blob.Dispose();
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void ActiveMetadata_ZeroBlockedCellsDoesNotRequireCompatibilityAsset()
    {
        using World world = new("OperationMapGridStartupBindingTests.NoCompatibility");
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(world.EntityManager, 8, 6, 2f, 0);
        try
        {
            Assert.That(OperationMapGridStartupBinding.TryResolve(
                world.EntityManager, null, out GridConfig grid, out Vector2Int[] blockedCells,
                out bool hasActiveMap, out string error), Is.True, error);
            Assert.That(hasActiveMap, Is.True);
            Assert.That(grid.Width, Is.EqualTo(8));
            Assert.That(blockedCells, Is.Empty);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void ActiveMetadata_MissingRequiredCapabilityFailsClosed()
    {
        using World world = new("OperationMapGridStartupBindingTests.Capability");
        BlobAssetReference<OperationMapBlob> blob = AddActiveMap(
            world.EntityManager, 8, 6, 2f, 0, supportsDynamicOccupancy: false);
        try
        {
            Assert.That(OperationMapGridStartupBinding.TryResolve(
                world.EntityManager, null, out _, out _, out bool hasActiveMap, out string error), Is.False);
            Assert.That(hasActiveMap, Is.True);
            Assert.That(error, Does.Contain("required surface, blocker, and occupancy capabilities"));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void NoActiveMap_UsesCompatibilityGrid()
    {
        using World world = new("OperationMapGridStartupBindingTests.Compatibility");
        GridAuthoringConfig config = CreateConfig(7, 5, 1.5f, Vector3.zero, new Vector2Int(2, 3));
        try
        {
            Assert.That(OperationMapGridStartupBinding.TryResolve(
                world.EntityManager, config, out GridConfig grid, out Vector2Int[] blockedCells,
                out bool hasActiveMap, out string error), Is.True, error);
            Assert.That(hasActiveMap, Is.False);
            Assert.That(grid.Width, Is.EqualTo(7));
            Assert.That(blockedCells, Is.EqualTo(config.BlockedCells));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private static BlobAssetReference<OperationMapBlob> AddActiveMap(
        EntityManager entityManager,
        int width,
        int height,
        float cellSize,
        int blockedCellCount,
        bool supportsDynamicOccupancy = true)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob metadata = ref builder.ConstructRoot<OperationMapBlob>();
        metadata.OperationMapId = new FixedString64Bytes("opmap.test.grid");
        metadata.Grid = new OperationMapGridBlob
        {
            Origin = new float3(3f, 0f, 4f),
            Dimensions = new int2(width, height),
            CellSize = cellSize,
            AuthoredBlockedCellCount = blockedCellCount
        };
        metadata.Navigation = new OperationMapNavigationMetadataBlob
        {
            UsesSurfaceMovementMetadata = 1,
            SupportsDynamicBlockers = 1,
            SupportsDynamicOccupancy = supportsDynamicOccupancy ? (byte)1 : (byte)0
        };
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

    private static GridAuthoringConfig CreateConfig(
        int width,
        int height,
        float cellSize,
        Vector3 origin,
        params Vector2Int[] blockedCells)
    {
        GridAuthoringConfig config = ScriptableObject.CreateInstance<GridAuthoringConfig>();
        SerializedObject serialized = new(config);
        serialized.FindProperty("width").intValue = width;
        serialized.FindProperty("height").intValue = height;
        serialized.FindProperty("cellSize").floatValue = cellSize;
        serialized.FindProperty("origin").vector3Value = origin;
        SerializedProperty cells = serialized.FindProperty("blockedCells");
        cells.arraySize = blockedCells.Length;
        for (int index = 0; index < blockedCells.Length; index++)
            cells.GetArrayElementAtIndex(index).vector2IntValue = blockedCells[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return config;
    }
}
