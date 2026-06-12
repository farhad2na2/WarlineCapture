#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class MatchHudMinimapMarkerSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MatchHudMinimapMarkerSystemTests();
            tests.MinimapMarkerSystemPublishesLiveUnitMarkersWithCap();
            Debug.Log("[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=1");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudMinimapMarkerFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void MinimapMarkerSystemPublishesLiveUnitMarkersWithCap()
    {
        using var world = new World(nameof(MinimapMarkerSystemPublishesLiveUnitMarkersWithCap));
        EntityManager em = world.EntityManager;
        float3 deadPosition = new(999f, 0f, 999f);
        for (int i = 0; i < 300; i++)
        {
            Entity unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
            em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(i, 0f, i + 1)));
            em.SetComponentData(unit, new Faction { Id = (byte)((i % 2) + 1) });
        }

        Entity deadUnit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
        em.SetComponentData(deadUnit, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(deadUnit, LocalTransform.FromPosition(deadPosition));
        em.SetComponentData(deadUnit, new Faction { Id = 2 });

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        using NativeArray<Entity> markerEntities = markerQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, markerEntities.Length);

        DynamicBuffer<MatchHudMinimapMarkerElement> markers = em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntities[0]);
        Assert.AreEqual(256, markers.Length);
        for (int i = 0; i < markers.Length; i++)
            Assert.AreNotEqual(deadPosition, markers[i].Position);
    }
}
#endif
