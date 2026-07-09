using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class CombatMotionAudioSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            CombatMotionAudioSystemTests tests = new();
            tests.UnitMotionAudioSystem_MovingVehicleEnqueuesEngineSfx();
            passed++;
            tests.UnitMotionAudioSystem_ActiveAircraftEnqueuesAircraftSfx();
            passed++;
            tests.MissileFlightAudioSystem_ProjectilesEnqueueFlightSfx();
            passed++;

            Debug.Log($"[CombatMotionAudioValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[CombatMotionAudioValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UnitMotionAudioSystem_MovingVehicleEnqueuesEngineSfx()
    {
        using World world = new("CombatMotionAudioSystemTests_Vehicle");
        EntityManager em = world.EntityManager;
        Entity vehicle = CreateMotionUnit(em, new float3(4f, 0f, 7f), vehicle: true, aircraft: false);

        SystemHandle system = world.CreateSystem<UnitMotionAudioSystem>();
        world.SetTime(new TimeData(2d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        AssertAudioRequest(
            requests[0],
            AudioEventIds.GameplayUnitEngineVehicleMove,
            AudioEventIds.GameplayUnitEngineVehicleMoveHash,
            vehicle,
            new float3(4f, 0f, 7f));
    }

    [Test]
    public void UnitMotionAudioSystem_ActiveAircraftEnqueuesAircraftSfx()
    {
        using World world = new("CombatMotionAudioSystemTests_Aircraft");
        EntityManager em = world.EntityManager;
        Entity aircraft = CreateMotionUnit(em, new float3(8f, 12f, 14f), vehicle: false, aircraft: true);
        em.SetComponentData(aircraft, new UnitAirComponent
        {
            Airborne = 1,
            HomeInitialized = 1,
            HomePosition = new float3(8f, 0f, 14f)
        });

        SystemHandle system = world.CreateSystem<UnitMotionAudioSystem>();
        world.SetTime(new TimeData(3d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        AssertAudioRequest(
            requests[0],
            AudioEventIds.GameplayUnitEngineAircraftFlight,
            AudioEventIds.GameplayUnitEngineAircraftFlightHash,
            aircraft,
            new float3(8f, 12f, 14f));
        AssertNoAudioEvent(requests, AudioEventIds.GameplayUnitAircraftFlyby);
    }

    [Test]
    public void MissileFlightAudioSystem_ProjectilesEnqueueFlightSfx()
    {
        using World world = new("CombatMotionAudioSystemTests_Missiles");
        EntityManager em = world.EntityManager;
        Entity groundMissile = em.CreateEntity(typeof(LocalTransform), typeof(GroundMissileProjectileComponent));
        em.SetComponentData(groundMissile, LocalTransform.FromPosition(new float3(3f, 5f, 9f)));
        em.SetComponentData(groundMissile, new GroundMissileProjectileComponent
        {
            TargetPosition = new float3(20f, 0f, 9f),
            DurationSeconds = 2f
        });

        Entity airMissile = em.CreateEntity(typeof(LocalTransform), typeof(AirMissileProjectileComponent));
        em.SetComponentData(airMissile, LocalTransform.FromPosition(new float3(6f, 8f, 14f)));
        em.SetComponentData(airMissile, new AirMissileProjectileComponent
        {
            Velocity = new float3(1f, 0f, 0f),
            Speed = 10f,
            LifetimeSeconds = 3f,
            ProximityFuseRadius = 2f
        });

        SystemHandle system = world.CreateSystem<MissileFlightAudioSystem>();
        world.SetTime(new TimeData(4d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(2, requests.Length);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileFlight, groundMissile);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileFlight, airMissile);
    }

    private static Entity CreateMotionUnit(EntityManager em, float3 position, bool vehicle, bool aircraft)
    {
        Entity entity = em.CreateEntity(
            typeof(LocalTransform),
            typeof(UnitMoveVisualComponent),
            typeof(UnitMovementBehavior),
            typeof(UnitHealth));
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 1 });
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = (byte)(vehicle ? 1 : 0) });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });

        if (aircraft)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 12f,
                RunwayTaxiSpeed = 5f
            });
            em.AddComponentData(entity, new UnitAirComponent());
        }

        return entity;
    }

    private static DynamicBuffer<AudioPlaybackRequestElement> GetAudioRequests(EntityManager em)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        return em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }

    private static void AssertAudioRequest(
        AudioPlaybackRequestElement request,
        string eventId,
        uint eventHash,
        Entity source,
        float3 position)
    {
        Assert.AreEqual(eventId, request.EventId.ToString());
        Assert.AreEqual(eventHash, request.EventHash);
        Assert.AreEqual(source, request.SourceEntity);
        Assert.AreEqual("SFX", request.BusId.ToString());
        Assert.AreEqual(1, request.Spatial);
        Assert.AreEqual(1, request.HasWorldPosition);
        Assert.AreEqual(position.x, request.WorldPosition.x, 0.001f);
        Assert.AreEqual(position.y, request.WorldPosition.y, 0.001f);
        Assert.AreEqual(position.z, request.WorldPosition.z, 0.001f);
    }

    private static void AssertHasAudioEvent(
        DynamicBuffer<AudioPlaybackRequestElement> requests,
        string eventId,
        Entity source)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            AudioPlaybackRequestElement request = requests[i];
            if (request.EventId.ToString() == eventId && request.SourceEntity == source)
            {
                Assert.AreEqual("SFX", request.BusId.ToString());
                Assert.AreEqual(1, request.Spatial);
                return;
            }
        }

        Assert.Fail($"Missing audio event {eventId} for {source}.");
    }

    private static void AssertNoAudioEvent(DynamicBuffer<AudioPlaybackRequestElement> requests, string eventId)
    {
        for (int i = 0; i < requests.Length; i++)
            Assert.AreNotEqual(eventId, requests[i].EventId.ToString());
    }
}
