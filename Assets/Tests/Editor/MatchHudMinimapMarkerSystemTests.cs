using Game.Components;
using Game.Runtime;
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
            tests.MinimapMarkerSystemPublishesScanRevealedHostileLastSeenMarkers();
            tests.MinimapMarkerSystemPublishesSelectedPlayerUnitsAndScanRevealedHostilesTogether();
            tests.MinimapMarkerSystemRetainsMarkersBetweenBoundedRefreshes();
            Debug.Log("[MatchHudMinimapMarkerFocusedValidation] result=Passed tests=4");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudMinimapMarkerFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MinimapMarkerSystemPublishesLiveCombatMarkersWithPlayerPriorityAndCap()
    {
        using var world = new World(nameof(MinimapMarkerSystemPublishesLiveCombatMarkersWithPlayerPriorityAndCap));
        EntityManager em = world.EntityManager;
        float3 deadPosition = new(999f, 0f, 999f);
        for (int i = 0; i < 32; i++)
            CreateUnit(em, FactionIdentity.NeutralFactionId, new float3(-100f - i, 0f, -100f - i), 100);

        for (int i = 0; i < 600; i++)
            CreateUnit(em, FactionIdentity.EnemyFactionId, new float3(i, 0f, i + 1), 100);

        for (int i = 0; i < 600; i++)
            CreateUnit(em, FactionIdentity.PlayerFactionId, new float3(2000f + i, 0f, 2000f + i), 100);

        CreateUnit(em, FactionIdentity.EnemyFactionId, deadPosition, 0);

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
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
            if (markers[i].FactionId == FactionIdentity.PlayerFactionId)
                playerCount++;
            else if (FactionIdentity.IsHostileToPlayer(markers[i].FactionId))
                enemyCount++;
            else if (FactionIdentity.IsNeutral(markers[i].FactionId))
                neutralCount++;
        }

        Assert.AreEqual(600, playerCount);
        Assert.AreEqual(424, enemyCount);
        Assert.AreEqual(0, neutralCount);
    }

    [Test]
    public void MinimapMarkerSystemPublishesScanRevealedHostileLastSeenMarkers()
    {
        using var world = new World(nameof(MinimapMarkerSystemPublishesScanRevealedHostileLastSeenMarkers));
        EntityManager em = world.EntityManager;
        float3 revealedBuildingPosition = new(40f, 0f, 55f);
        float3 friendlyIntelPosition = new(80f, 0f, 90f);
        float3 liveScannedEnemyPosition = new(100f, 0f, 110f);

        CreateScanIntelContact(em, FactionIdentity.EnemyFactionId, revealedBuildingPosition);
        CreateScanIntelContact(em, FactionIdentity.PlayerFactionId, friendlyIntelPosition);

        Entity liveScannedEnemy = em.CreateEntity(
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(Faction),
            typeof(ScanIntelRevealedTag),
            typeof(ScanIntelLastSeen));
        em.SetComponentData(liveScannedEnemy, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(liveScannedEnemy, LocalTransform.FromPosition(liveScannedEnemyPosition));
        em.SetComponentData(liveScannedEnemy, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(liveScannedEnemy, new ScanIntelLastSeen
        {
            Position = liveScannedEnemyPosition,
            FactionId = FactionIdentity.EnemyFactionId
        });

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        Entity markerEntity = markerQuery.GetSingletonEntity();
        DynamicBuffer<MatchHudMinimapMarkerElement> markers = em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity);

        Assert.AreEqual(2, markers.Length);
        AssertMarkerCount(markers, revealedBuildingPosition, 1);
        AssertMarkerCount(markers, liveScannedEnemyPosition, 1);
        AssertMarkerCount(markers, friendlyIntelPosition, 0);
    }

    [Test]
    public void MinimapMarkerSystemPublishesSelectedPlayerUnitsAndScanRevealedHostilesTogether()
    {
        using var world = new World(nameof(MinimapMarkerSystemPublishesSelectedPlayerUnitsAndScanRevealedHostilesTogether));
        EntityManager em = world.EntityManager;
        float3 selectedSoldierPosition = new(12f, 0f, 14f);
        float3 selectedVehiclePosition = new(18f, 0f, 22f);
        float3 revealedHostilePosition = new(90f, 0f, 96f);
        float3 friendlyIntelPosition = new(100f, 0f, 106f);

        Entity selectedSoldier = CreateUnit(em, FactionIdentity.PlayerFactionId, selectedSoldierPosition, 100);
        Entity selectedVehicle = CreateUnit(em, FactionIdentity.PlayerFactionId, selectedVehiclePosition, 100);
        em.AddComponent<SelectedUnitTag>(selectedSoldier);
        em.AddComponent<SelectedUnitTag>(selectedVehicle);
        CreateScanIntelContact(em, FactionIdentity.EnemyFactionId, revealedHostilePosition);
        CreateScanIntelContact(em, FactionIdentity.PlayerFactionId, friendlyIntelPosition);

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        Entity markerEntity = markerQuery.GetSingletonEntity();
        DynamicBuffer<MatchHudMinimapMarkerElement> markers = em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity);

        Assert.AreEqual(3, markers.Length);
        AssertMarkerCount(markers, selectedSoldierPosition, 1);
        AssertMarkerCount(markers, selectedVehiclePosition, 1);
        AssertMarkerCount(markers, revealedHostilePosition, 1);
        AssertMarkerCount(markers, friendlyIntelPosition, 0);
    }

    [Test]
    public void MinimapMarkerSystemRetainsMarkersBetweenBoundedRefreshes()
    {
        using var world = new World(nameof(MinimapMarkerSystemRetainsMarkersBetweenBoundedRefreshes));
        EntityManager em = world.EntityManager;
        float3 playerPosition = new(12f, 0f, 18f);
        CreateUnit(em, FactionIdentity.PlayerFactionId, playerPosition, 100);

        SystemHandle system = world.CreateSystem<MatchHudMinimapMarkerSystem>();
        system.Update(world.Unmanaged);
        system.Update(world.Unmanaged);

        using EntityQuery markerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        Entity markerEntity = markerQuery.GetSingletonEntity();
        DynamicBuffer<MatchHudMinimapMarkerElement> markers =
            em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity);

        Assert.AreEqual(1, markers.Length);
        AssertMarkerCount(markers, playerPosition, 1);
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, float3 position, int health)
    {
        Entity unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
        em.SetComponentData(unit, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(unit, LocalTransform.FromPosition(position));
        em.SetComponentData(unit, new Faction { Id = factionId });
        return unit;
    }

    private static void CreateScanIntelContact(EntityManager em, byte factionId, float3 position)
    {
        Entity contact = em.CreateEntity(typeof(ScanIntelRevealedTag), typeof(ScanIntelLastSeen));
        em.SetComponentData(contact, new ScanIntelLastSeen
        {
            Position = position,
            FactionId = factionId
        });
    }

    private static void AssertMarkerCount(DynamicBuffer<MatchHudMinimapMarkerElement> markers, float3 position, int expectedCount)
    {
        int count = 0;
        for (int i = 0; i < markers.Length; i++)
        {
            if (math.distance(markers[i].Position, position) < 0.001f)
                count++;
        }

        Assert.AreEqual(expectedCount, count);
    }
}
#endif
