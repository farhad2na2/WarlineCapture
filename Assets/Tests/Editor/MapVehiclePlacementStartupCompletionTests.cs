using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Runtime;

public sealed class MapVehiclePlacementStartupCompletionTests
{
    public static void RunFocusedValidation()
    {
        var tests = new MapVehiclePlacementStartupCompletionTests();
        tests.EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden();
        tests.PlayerPlacementFindsNeutralAuthoredVehicleForOwnershipAdoption();
        tests.AdoptionDoesNotClaimSpawnedOrDistantVehicles();
        UnityEngine.Debug.Log("[MapVehiclePlacementStartupCompletionValidation] result=Passed tests=3");
    }

    [Test]
    public void EmptyPlacementConfigCompletesAfterAuthoringRootIsHidden()
    {
        MapVehiclePlacementConfig config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        GameObject root = new("Vehicles");
        World world = new("MapVehiclePlacementStartupCompletionTests");
        try
        {
            var unitPrefabContext = new RuntimeUnitPrefabSystem.Context(
                spawnPrefabSystem: default,
                tryGetEntityManager: TryGetEntityManager,
                ensureEntityQueries: null,
                createSpawnPrefabContext: null);
            MapVehiclePlacementSpawnPrefabSystemHelper system = new();
            var context = new MapVehiclePlacementSpawnPrefabSystemHelper.Context(
                config,
                root.transform,
                unitPrefabSystem: default,
                unitPrefabContext,
                tryGetGridData: null,
                logWarning: null);

            Assert.IsFalse(system.IsCompleteFor(config, root.transform));

            system.Update(context);
            Assert.IsTrue(system.IsCompleteFor(config, root.transform));
            Assert.IsFalse(root.activeSelf);
        }
        finally
        {
            world.Dispose();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = world.EntityManager;
            return true;
        }
    }

    [Test]
    public void PlayerPlacementFindsNeutralAuthoredVehicleForOwnershipAdoption()
    {
        using World world = new("MapVehiclePlacementAuthoredAdoptionTests");
        EntityManager em = world.EntityManager;
        float3 position = new(842f, 1f, 378f);
        Entity authored = CreateVehicle(em, position, FactionIdentity.NeutralFactionId, Entity.Null);
        MapVehiclePlacementConfigEntry placement = CreatePlacement(position, FactionIdentity.PlayerFactionId);

        Assert.IsTrue(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            placement,
            default,
            out Entity resolved));
        Assert.AreEqual(authored, resolved);

        Entity prefab = em.CreateEntity();
        using (EntityCommandBuffer ecb = new(Allocator.Temp))
        {
            MapVehiclePlacementSpawnPrefabSystemHelper.ConfigureAdoptedVehicle(
                em,
                ecb,
                resolved,
                prefab,
                placement.FactionId,
                new int2(21, 9),
                position);
            ecb.Playback(em);
        }

        Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(resolved).Id);
        Assert.AreEqual(prefab, em.GetComponentData<UnitRespawnPrefab>(resolved).Prefab);
        Assert.AreEqual(new int2(21, 9), em.GetComponentData<UnitGrid>(resolved).Cell);
    }

    [Test]
    public void AdoptionDoesNotClaimSpawnedOrDistantVehicles()
    {
        using World world = new("MapVehiclePlacementAuthoredAdoptionGuardTests");
        EntityManager em = world.EntityManager;
        float3 target = new(842f, 1f, 378f);
        Entity prefab = em.CreateEntity();
        CreateVehicle(em, target, FactionIdentity.PlayerFactionId, prefab);
        CreateVehicle(em, target + new float3(3f, 0f, 0f), FactionIdentity.NeutralFactionId, Entity.Null);
        MapVehiclePlacementConfigEntry placement = CreatePlacement(target, FactionIdentity.PlayerFactionId);

        Assert.IsFalse(MapVehiclePlacementSpawnPrefabSystemHelper.TryFindAuthoredVehicleEntity(
            em,
            placement,
            default,
            out _));
    }

    private static Entity CreateVehicle(
        EntityManager em,
        float3 position,
        byte faction,
        Entity respawnPrefab)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitRespawnPrefab),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = faction });
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = respawnPrefab });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static MapVehiclePlacementConfigEntry CreatePlacement(float3 position, byte faction)
    {
        return new MapVehiclePlacementConfigEntry(
            "Map/Vehicles/Tank",
            "Unit_Veh_Tank_USA",
            null,
            faction,
            position,
            position,
            Vector3.zero,
            Vector3.one);
    }
}
