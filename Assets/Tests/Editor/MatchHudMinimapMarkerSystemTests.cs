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
            tests.MinimapMarkerSystemPublishesLiveCombatMarkersWithPlayerPriorityAndCap();
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
    public void MinimapMarkerSystemPublishesLiveCombatMarkersWithPlayerPriorityAndCap()
    {
        using var world = new World(nameof(MinimapMarkerSystemPublishesLiveCombatMarkersWithPlayerPriorityAndCap));
        EntityManager em = world.EntityManager;
        float3 deadPosition = new(999f, 0f, 999f);
        for (int i = 0; i < 32; i++)
            CreateUnit(em, FactionIdentitySystem.NeutralFactionId, new float3(-100f - i, 0f, -100f - i), 100);

        for (int i = 0; i < 600; i++)
            CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(i, 0f, i + 1), 100);

        for (int i = 0; i < 600; i++)
            CreateUnit(em, FactionIdentitySystem.PlayerFactionId, new float3(2000f + i, 0f, 2000f + i), 100);

        CreateUnit(em, FactionIdentitySystem.EnemyFactionId, deadPosition, 0);

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        using NativeArray<Entity> markerEntities = markerQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, markerEntities.Length);

        DynamicBuffer<MatchHudMinimapMarkerElement> markers = em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntities[0]);
        Assert.AreEqual(1024, markers.Length);
        int playerCount = 0;
        int enemyCount = 0;
        int neutralCount = 0;
        for (int i = 0; i < markers.Length; i++)
        {
            Assert.AreNotEqual(deadPosition, markers[i].Position);
            if (markers[i].FactionId == FactionIdentitySystem.PlayerFactionId)
                playerCount++;
            else if (FactionIdentitySystem.IsHostileToPlayer(markers[i].FactionId))
                enemyCount++;
            else if (FactionIdentitySystem.IsNeutral(markers[i].FactionId))
                neutralCount++;
        }

        Assert.AreEqual(600, playerCount);
        Assert.AreEqual(424, enemyCount);
        Assert.AreEqual(0, neutralCount);
    }

    private static void CreateUnit(EntityManager em, byte factionId, float3 position, int health)
    {
        Entity unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
        em.SetComponentData(unit, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(unit, LocalTransform.FromPosition(position));
        em.SetComponentData(unit, new Faction { Id = factionId });
    }
}
#endif
