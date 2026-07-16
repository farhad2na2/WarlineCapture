using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class RuntimeGridDeduplicationSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeGridDeduplicationSystemTests();
            tests.RunWithFixture(tests.Deduplication_RemovesRuntimeGridWhenAuthoredGridExists);
            tests.RunWithFixture(tests.Deduplication_KeepsRuntimeGridWhenNoAuthoredGridExists);
            tests.RunWithFixture(tests.RuntimeGridBootstrapStartupSystemHelperCreatesRuntimeGridFromPlainHelper);
            tests.RunWithFixture(tests.ActiveOperationMapMetadataDrivesRuntimeGridBootstrap);
            Debug.Log("[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=4");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeGridDeduplicationFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("RuntimeGridDeduplicationSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void Deduplication_RemovesRuntimeGridWhenAuthoredGridExists()
    {
        Entity authored = CreateGrid(runtimeGenerated: false);
        Entity runtime = CreateGrid(runtimeGenerated: true);
        SystemHandle system = _world.CreateSystem<RuntimeGridDeduplicationSystem>();

        system.Update(_world.Unmanaged);

        Assert.IsTrue(_entityManager.Exists(authored));
        Assert.IsFalse(_entityManager.Exists(runtime));
    }

    [Test]
    public void Deduplication_KeepsRuntimeGridWhenNoAuthoredGridExists()
    {
        Entity runtime = CreateGrid(runtimeGenerated: true);
        SystemHandle system = _world.CreateSystem<RuntimeGridDeduplicationSystem>();

        system.Update(_world.Unmanaged);

        Assert.IsTrue(_entityManager.Exists(runtime));
    }

    [Test]
    public void RuntimeGridBootstrapStartupSystemHelperCreatesRuntimeGridFromPlainHelper()
    {
        var system = new RuntimeGridBootstrapStartupSystemHelper();

        Assert.IsTrue(system.Ensure(_entityManager, 12, 10, 1.5f, new Vector3(2f, 0f, 3f)));

        using EntityQuery query = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<RuntimeGridBootstrapGridTag>());
        Assert.IsFalse(query.IsEmptyIgnoreFilter);
        Entity gridEntity = query.GetSingletonEntity();
        GridConfig grid = _entityManager.GetComponentData<GridConfig>(gridEntity);
        Assert.AreEqual(12, grid.Width);
        Assert.AreEqual(10, grid.Height);
        Assert.AreEqual(1.5f, grid.CellSize);
        Assert.AreEqual(new float3(2f, 0f, 3f), grid.Origin);
        Assert.AreEqual(120, _entityManager.GetBuffer<GridWalkable>(gridEntity).Length);
        Assert.AreEqual(120, _entityManager.GetBuffer<GridRoad>(gridEntity).Length);
        Assert.AreEqual(120, _entityManager.GetBuffer<GridRoadSidewalk>(gridEntity).Length);
        Assert.AreEqual(120, _entityManager.GetBuffer<GridRoadDirt>(gridEntity).Length);
        Assert.IsTrue(_entityManager.HasComponent<DynamicBlockerComponent>(gridEntity));
        Assert.IsTrue(_entityManager.HasComponent<DynamicOccupancyComponent>(gridEntity));
    }

    [Test]
    public void ActiveOperationMapMetadataDrivesRuntimeGridBootstrap()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob metadata = ref builder.ConstructRoot<OperationMapBlob>();
        metadata.OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01");
        metadata.Grid = new OperationMapGridBlob
        {
            Origin = new float3(7f, 0f, 11f),
            Dimensions = new int2(24, 18),
            CellSize = 1.25f
        };
        BlobAssetReference<OperationMapBlob> blob =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
        Entity root = _entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        _entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = metadata.OperationMapId,
            Generation = 2
        });
        _entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 2
        });

        Assert.IsTrue(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            _entityManager,
            out GridConfig grid,
            out bool hasActiveMap,
            out string error), error);
        Assert.IsTrue(hasActiveMap);

        var system = new RuntimeGridBootstrapStartupSystemHelper();
        Assert.IsTrue(system.Ensure(
            _entityManager,
            grid.Width,
            grid.Height,
            grid.CellSize,
            (Vector3)grid.Origin));

        using EntityQuery gridQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<RuntimeGridBootstrapGridTag>());
        Entity gridEntity = gridQuery.GetSingletonEntity();
        Assert.AreEqual(24 * 18, _entityManager.GetBuffer<GridWalkable>(gridEntity).Length);
        Assert.AreEqual(new float3(7f, 0f, 11f), _entityManager.GetComponentData<GridConfig>(gridEntity).Origin);
        blob.Dispose();
    }

    private void RunWithFixture(Action test)
    {
        SetUp();
        try
        {
            test();
        }
        finally
        {
            TearDown();
        }
    }

    private Entity CreateGrid(bool runtimeGenerated)
    {
        Entity entity = runtimeGenerated
            ? _entityManager.CreateEntity(typeof(GridConfig), typeof(RuntimeGridBootstrapGridTag))
            : _entityManager.CreateEntity(typeof(GridConfig));
        _entityManager.SetComponentData(entity, new GridConfig
        {
            Width = 8,
            Height = 8,
            CellSize = 1f,
            Origin = float3.zero
        });
        return entity;
    }
}
#endif
