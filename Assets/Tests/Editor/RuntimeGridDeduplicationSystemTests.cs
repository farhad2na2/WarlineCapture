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
            Debug.Log("[RuntimeGridDeduplicationFocusedValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeGridDeduplicationFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
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
