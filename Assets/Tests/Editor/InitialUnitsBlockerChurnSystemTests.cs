using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class InitialUnitsBlockerChurnSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new InitialUnitsBlockerChurnSystemTests();
            tests.ChurnIntervalReplacesBlockerInsideGrid();
            Debug.Log("[InitialUnitsBlockerChurnFocusedValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[InitialUnitsBlockerChurnFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ChurnIntervalReplacesBlockerInsideGrid()
    {
        using World world = new("InitialUnitsBlockerChurnSystemTests");
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 8,
            Height = 8,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity prefab = em.CreateEntity(
            typeof(Prefab),
            typeof(StaticGridBlocker),
            typeof(UnitGrid),
            typeof(LocalTransform));
        em.SetComponentData(prefab, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(prefab, LocalTransform.FromPosition(float3.zero));

        Entity existing = em.CreateEntity(
            typeof(StaticGridBlocker),
            typeof(UnitGrid),
            typeof(LocalTransform));
        em.SetComponentData(existing, new UnitGrid { Cell = new int2(2, 2) });
        em.SetComponentData(existing, LocalTransform.FromPosition(new float3(2.5f, 0f, 2.5f)));

        Entity churnEntity = em.CreateEntity(
            typeof(InitialUnitsBlockerChurnConfig),
            typeof(InitialUnitsBlockerChurnComponent));
        em.SetComponentData(churnEntity, new InitialUnitsBlockerChurnConfig
        {
            Enabled = true,
            IntervalSeconds = 0.1f,
            AddRemovePerInterval = 1
        });
        em.SetComponentData(churnEntity, new InitialUnitsBlockerChurnComponent
        {
            Timer = 0f,
            RandomState = 123u,
            BlockerPrefab = prefab
        });

        SystemHandle system = world.CreateSystem<InitialUnitsBlockerChurnSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        InitialUnitsBlockerChurnComponent churn = em.GetComponentData<InitialUnitsBlockerChurnComponent>(churnEntity);
        Assert.AreEqual(0f, churn.Timer, 0.0001f);
        Assert.AreNotEqual(123u, churn.RandomState);
        Assert.IsFalse(em.Exists(existing), "The existing runtime blocker should be removed during churn.");

        using EntityQuery blockerQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<UnitGrid>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<Prefab>()
            }
        });
        using var blockers = blockerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        Assert.AreEqual(1, blockers.Length);

        UnitGrid spawnedGrid = em.GetComponentData<UnitGrid>(blockers[0]);
        Assert.GreaterOrEqual(spawnedGrid.Cell.x, 0);
        Assert.GreaterOrEqual(spawnedGrid.Cell.y, 0);
        Assert.Less(spawnedGrid.Cell.x, 8);
        Assert.Less(spawnedGrid.Cell.y, 8);
    }
}
#endif
