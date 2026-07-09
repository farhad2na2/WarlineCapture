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

public sealed class GameplayAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new GameplayAudioFeedbackTests();
            tests.UnitMotionAudioSystem_MovingVehicleEnqueuesEngineSfx();
            passed++;
            tests.UnitMotionAudioSystem_TakeoffAircraftEnqueuesTakeoffAndFlightSfx();
            passed++;
            tests.UnitMotionAudioSystem_AttackRunAircraftEnqueuesFlightSfxWhenVisualMovementIsIdle();
            passed++;
            tests.UnitMotionAudioSystem_HelicopterSourceEnqueuesHelicopterFlightSfx();
            passed++;
            tests.UnitAttackSystem_StandardShotEnqueuesWeaponFireSfx();
            passed++;
            tests.UnitAttackSystem_AircraftShotEnqueuesMissileSfx();
            passed++;
            tests.MissileFlightAudioSystem_ProjectilesEnqueueFlightSfx();
            passed++;
            tests.AirMissileLaunchAndImpactSystems_EnqueueMissileSfx();
            passed++;

            Debug.Log($"[GameplayAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[GameplayAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UnitMotionAudioSystem_MovingVehicleEnqueuesEngineSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_Vehicle");
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
    public void UnitMotionAudioSystem_TakeoffAircraftEnqueuesTakeoffAndFlightSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_Aircraft");
        EntityManager em = world.EntityManager;
        Entity aircraft = CreateMotionUnit(em, new float3(8f, 2f, 12f), vehicle: true, aircraft: true);
        em.SetComponentData(aircraft, new UnitAirComponent
        {
            Airborne = 0,
            TakeoffRolling = 1,
            HomeInitialized = 1,
            HomePosition = new float3(8f, 0f, 12f)
        });

        SystemHandle system = world.CreateSystem<UnitMotionAudioSystem>();
        world.SetTime(new TimeData(4d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(2, requests.Length);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayUnitEngineAircraftTakeoff, aircraft);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayUnitEngineAircraftFlight, aircraft);
        AssertNoAudioEvent(requests, AudioEventIds.GameplayUnitAircraftFlyby);
    }

    [Test]
    public void UnitMotionAudioSystem_AttackRunAircraftEnqueuesFlightSfxWhenVisualMovementIsIdle()
    {
        using World world = new("GameplayAudioFeedbackTests_AircraftAttackRun");
        EntityManager em = world.EntityManager;
        Entity aircraft = CreateMotionUnit(em, new float3(14f, 8f, 22f), vehicle: true, aircraft: true);
        em.SetComponentData(aircraft, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 1f });
        em.SetComponentData(aircraft, new UnitAirComponent
        {
            AttackRunActive = 1,
            HomeInitialized = 1,
            HomePosition = new float3(14f, 0f, 22f)
        });

        SystemHandle system = world.CreateSystem<UnitMotionAudioSystem>();
        world.SetTime(new TimeData(5d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        AssertAudioRequest(
            requests[0],
            AudioEventIds.GameplayUnitEngineAircraftFlight,
            AudioEventIds.GameplayUnitEngineAircraftFlightHash,
            aircraft,
            new float3(14f, 8f, 22f));
        AssertNoAudioEvent(requests, AudioEventIds.GameplayUnitEngineAircraftTakeoff);
        AssertNoAudioEvent(requests, AudioEventIds.GameplayUnitAircraftFlyby);
    }

    [Test]
    public void UnitMotionAudioSystem_HelicopterSourceEnqueuesHelicopterFlightSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_HelicopterFlight");
        EntityManager em = world.EntityManager;
        Entity helicopter = CreateMotionUnit(em, new float3(6f, 5f, 18f), vehicle: true, aircraft: true);
        em.AddComponentData(helicopter, new UnitSourcePrefabKey
        {
            Value = new Unity.Collections.FixedString64Bytes("unit_veh_helicopter_attack")
        });
        em.SetComponentData(helicopter, new UnitAirComponent
        {
            Airborne = 1,
            HomeInitialized = 1,
            HomePosition = new float3(6f, 0f, 18f)
        });

        SystemHandle system = world.CreateSystem<UnitMotionAudioSystem>();
        world.SetTime(new TimeData(6d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        AssertAudioRequest(
            requests[0],
            AudioEventIds.GameplayUnitEngineHelicopterFlight,
            AudioEventIds.GameplayUnitEngineHelicopterFlightHash,
            helicopter,
            new float3(6f, 5f, 18f));
        AssertNoAudioEvent(requests, AudioEventIds.GameplayUnitEngineAircraftFlight);
    }

    [Test]
    public void UnitAttackSystem_StandardShotEnqueuesWeaponFireSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_WeaponFire");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateCombatTarget(em, new float3(5f, 0f, 0f), FactionIdentity.EnemyFactionId);
        Entity attacker = CreateCombatAttacker(em, new float3(0f, 0f, 0f), target);

        SystemHandle system = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        AssertAudioRequest(
            requests[0],
            AudioEventIds.GameplayWeaponFireSmallArms,
            AudioEventIds.GameplayWeaponFireSmallArmsHash,
            attacker,
            new float3(0f, 0f, 0f));
        Assert.AreEqual(40, em.GetComponentData<UnitHealth>(target).Current);
    }

    [Test]
    public void UnitAttackSystem_AircraftShotEnqueuesMissileSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_AircraftWeaponFire");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateCombatTarget(em, new float3(12f, 0f, 0f), FactionIdentity.EnemyFactionId);
        Entity aircraft = CreateCombatAttacker(em, new float3(0f, 8f, 0f), target, aircraft: true);

        SystemHandle system = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(3, requests.Length);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileLaunch, aircraft);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileFlight, aircraft);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileImpact, aircraft);
        AssertNoAudioEvent(requests, AudioEventIds.GameplayWeaponFireSmallArms);
        Assert.AreEqual(40, em.GetComponentData<UnitHealth>(target).Current);
    }

    [Test]
    public void MissileFlightAudioSystem_ProjectilesEnqueueFlightSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_MissileFlight");
        EntityManager em = world.EntityManager;
        Entity groundMissile = em.CreateEntity(typeof(LocalTransform), typeof(GroundMissileProjectileComponent));
        em.SetComponentData(groundMissile, LocalTransform.FromPosition(new float3(3f, 5f, 9f)));
        em.SetComponentData(groundMissile, new GroundMissileProjectileComponent
        {
            Source = Entity.Null,
            TargetPosition = new float3(20f, 0f, 9f),
            DurationSeconds = 2f,
            FactionId = FactionIdentity.PlayerFactionId
        });

        Entity airMissile = em.CreateEntity(typeof(LocalTransform), typeof(AirMissileProjectileComponent));
        em.SetComponentData(airMissile, LocalTransform.FromPosition(new float3(6f, 8f, 14f)));
        em.SetComponentData(airMissile, new AirMissileProjectileComponent
        {
            Source = Entity.Null,
            Target = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.None,
            FactionId = FactionIdentity.PlayerFactionId,
            Velocity = new float3(1f, 0f, 0f),
            Speed = 10f,
            LifetimeSeconds = 3f,
            ProximityFuseRadius = 2f
        });

        SystemHandle system = world.CreateSystem<MissileFlightAudioSystem>();
        world.SetTime(new TimeData(3d, 0.1f));
        system.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(2, requests.Length);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileFlight, groundMissile);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileFlight, airMissile);
    }

    [Test]
    public void AirMissileLaunchAndImpactSystems_EnqueueMissileSfx()
    {
        using World world = new("GameplayAudioFeedbackTests_AirMissile");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateAirMissileLauncher(em, new float3(0f, 0f, 0f));
        Entity target = CreateCombatTarget(em, new float3(30f, 10f, 0f), FactionIdentity.EnemyFactionId);
        em.AddComponentData(target, new UnitAirMovement
        {
            CruiseHeight = 12f,
            RunwayTaxiSpeed = 5f
        });
        em.SetComponentData(launcher, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Locked,
            TargetEntity = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Timer = 0f,
            EffectiveRange = 120f,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 1f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
        em.AddComponentData(launcher, new AirMissileLauncherTargetComponent
        {
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Score = 100f
        });

        SystemHandle fireControl = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        fireControl.Update(world.Unmanaged);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileLaunch, launcher);

        Entity impactEntity = em.CreateEntity(typeof(AirMissileImpactRequestComponent));
        em.SetComponentData(impactEntity, new AirMissileImpactRequestComponent
        {
            Source = launcher,
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            FactionId = FactionIdentity.PlayerFactionId,
            Position = new float3(30f, 10f, 0f),
            VisualSeparation = 0f,
            Damage = 25
        });

        SystemHandle impact = world.CreateSystem<AirMissileImpactSystem>();
        world.SetTime(new TimeData(2d, 0.1f));
        impact.Update(world.Unmanaged);

        requests = GetAudioRequests(em);
        AssertHasAudioEvent(requests, AudioEventIds.GameplayWeaponMissileImpact, launcher);
        Assert.AreEqual(75, em.GetComponentData<UnitHealth>(target).Current);
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

    private static void CreateGrid(EntityManager em)
    {
        Entity grid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(grid, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateCombatTarget(EntityManager em, float3 position, byte faction)
    {
        Entity entity = em.CreateEntity(typeof(UnitHealth), typeof(Faction), typeof(LocalTransform));
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new Faction { Id = faction });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateCombatAttacker(EntityManager em, float3 position, Entity target, bool aircraft = false)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(EngageTarget));
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 20f,
            CooldownSeconds = 0.5f,
            Damage = 60,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(entity, new UnitAttackCooldownComponent());
        em.SetComponentData(entity, new UnitAttackTraceComponent());
        em.SetComponentData(entity, new UnitAttackAnimationComponent());
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new EngageTarget
        {
            Target = target,
            Cell = new int2(5, 0),
            Position = new float3(5f, 0f, 0f),
            IsCommanded = 1
        });
        if (aircraft)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 12f,
                RunwayTaxiSpeed = 5f
            });
        }
        return entity;
    }

    private static Entity CreateAirMissileLauncher(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new AirMissileLauncherComponent
        {
            MinRange = 4f,
            BaseDetectionRange = 120f,
            MaxDetectionRange = 260f,
            AirTargetPriority = 25f,
            IncomingMissilePriority = 100f,
            AimToleranceDegrees = 5f,
            LockSeconds = 1f,
            LaunchDelaySeconds = 0.1f,
            ReloadSeconds = 1.5f,
            MissileSpeed = 95f,
            MissileAcceleration = 0f,
            MissileTurnRateDegreesPerSecond = 120f,
            MissileLifetimeSeconds = 5f,
            ProximityFuseRadius = 4f,
            AirTargetDamage = 120,
            IncomingMissileDamage = 9999,
            TrackingQuality = 1f,
            MaxSupportRangeBonus = 180f,
            MaxSupportTrackingBonus = 0.3f
        });
        em.SetComponentData(entity, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.None,
            EffectiveRange = 120f,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 1f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
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

    private static void AssertNoAudioEvent(
        DynamicBuffer<AudioPlaybackRequestElement> requests,
        string eventId)
    {
        for (int i = 0; i < requests.Length; i++)
            Assert.AreNotEqual(eventId, requests[i].EventId.ToString());
    }
}
