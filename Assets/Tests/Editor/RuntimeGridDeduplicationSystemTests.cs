#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
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
            Debug.Log("[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=3");
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
