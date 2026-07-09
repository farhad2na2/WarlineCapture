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

public sealed class AlertObjectiveAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new AlertObjectiveAudioFeedbackTests();
            tests.ThreatWarningAudio_ResolvesAriaGroundAndAirWarnings();
            passed++;
            tests.TryEmitThreatWarningAudio_EnqueuesVoiceBusRequest();
            passed++;
            tests.ThreatDetectionWarningSystem_NewCloseAirThreatEnqueuesCriticalAriaVoice();
            passed++;
            tests.UnitUnderAttackAudio_ResolvesOnlyForPlayerControlledTargets();
            passed++;
            tests.UnitAttackSystem_DamagingPlayerTargetDoesNotEnqueueGenericUnderAttackAudio();
            passed++;
            tests.TryEmitUnitUnderAttackAudio_SuppressesGenericPlaceholderAlert();
            passed++;
            tests.BaseBreachedAudio_ResolvesOnlyForPlayerOwnedBarriers();
            passed++;
            tests.TryEmitBaseBreachedAudio_EnqueuesCriticalAlertRequest();
            passed++;
            tests.BeginDestroyedBuildingState_EmitsBaseBreachedAudioOnlyWhenCombatBoundaryRequestsIt();
            passed++;

            Debug.Log($"[AlertObjectiveAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AlertObjectiveAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ThreatWarningAudio_ResolvesAriaGroundAndAirWarnings()
    {
        AssertThreatAudio(
            ThreatWarningType.Ground,
            etaSeconds: 12f,
            threatCount: 1,
            AudioEventIds.VOARIAMessageWarningGroundAttackType,
            AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash,
            AudioPlaybackPriority.High,
            expectedCooldownSeconds: 3f);

        AssertThreatAudio(
            ThreatWarningType.Air,
            etaSeconds: 0f,
            threatCount: 1,
            AudioEventIds.VOARIAMessageWarningAirAttackType,
            AudioEventIds.VOARIAMessageWarningAirAttackTypeHash,
            AudioPlaybackPriority.Critical,
            expectedCooldownSeconds: 4f);

        AssertThreatAudio(
            ThreatWarningType.Ground,
            etaSeconds: 10f,
            threatCount: 2,
            AudioEventIds.VOARIAMessageWarningGroundAttackType,
            AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash,
            AudioPlaybackPriority.Critical,
            expectedCooldownSeconds: 4f);
    }

    [Test]
    public void TryEmitThreatWarningAudio_EnqueuesVoiceBusRequest()
    {
        using World world = new("AlertAudioFeedbackEmitTests");

        Assert.IsTrue(ThreatDetectionWarningSystem.TryEmitThreatWarningAudio(
            world.EntityManager,
            ThreatWarningType.Ground,
            etaSeconds: 5f,
            threatCount: 1,
            requestedAt: 1.25f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(world.EntityManager);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash, requests[0].EventHash);
        Assert.AreEqual("Voice", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.High, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
        Assert.That(requests[0].CooldownSeconds, Is.EqualTo(3f).Within(0.001f));
        Assert.That(requests[0].RequestedAt, Is.EqualTo(1.25f).Within(0.001f));
    }

    [Test]
    public void ThreatDetectionWarningSystem_NewCloseAirThreatEnqueuesCriticalAriaVoice()
    {
        using World world = new("AlertAudioFeedbackThreatDetectionTests");
        EntityManager em = world.EntityManager;

        CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), air: false, health: 100);
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), air: true, health: 100);

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            world.SetTime(new TimeData(0.1d, 0.1f));
            system.Update(world.Unmanaged);

            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Air, ThreatWarningRuntimeState.PendingType);

            DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(AudioEventIds.VOARIAMessageWarningAirAttackType, requests[0].EventId.ToString());
            Assert.AreEqual(AudioEventIds.VOARIAMessageWarningAirAttackTypeHash, requests[0].EventHash);
            Assert.AreEqual("Voice", requests[0].BusId.ToString());
            Assert.AreEqual(AudioPlaybackPriority.Critical, requests[0].Priority);
            Assert.That(requests[0].CooldownSeconds, Is.EqualTo(4f).Within(0.001f));
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    [Test]
    public void UnitUnderAttackAudio_ResolvesOnlyForPlayerControlledTargets()
    {
        using World world = new("UnitUnderAttackAudioResolveTests");
        EntityManager em = world.EntityManager;
        Entity playerTarget = CreateCombatTarget(em, FactionIdentity.PlayerFactionId, new int2(4, 4), new float3(4f, 0f, 4f), 100);
        Entity enemyTarget = CreateCombatTarget(em, FactionIdentity.EnemyFactionId, new int2(5, 4), new float3(5f, 0f, 4f), 100);

        Assert.IsTrue(UnitAttackSystem.TryResolveUnitUnderAttackAudioEvent(
            em,
            playerTarget,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(AudioEventIds.AlertUnitUnderAttack, eventId);
        Assert.AreEqual(AudioEventIds.AlertUnitUnderAttackHash, eventHash);

        Assert.IsFalse(UnitAttackSystem.TryResolveUnitUnderAttackAudioEvent(
            em,
            enemyTarget,
            out _,
            out _));
    }

    [Test]
    public void UnitAttackSystem_DamagingPlayerTargetDoesNotEnqueueGenericUnderAttackAudio()
    {
        using World world = new("UnitUnderAttackAudioSystemTests");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateCombatTarget(em, FactionIdentity.PlayerFactionId, new int2(4, 4), new float3(4f, 0f, 4f), 100);
        CreateCombatAttacker(em, FactionIdentity.EnemyFactionId, new int2(4, 5), new float3(4f, 0f, 5f), target, 25);

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(1.5d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(75, em.GetComponentData<UnitHealth>(target).Current);

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void TryEmitUnitUnderAttackAudio_SuppressesGenericPlaceholderAlert()
    {
        using World world = new("UnitUnderAttackAudioPlaceholderSuppressionTests");
        EntityManager em = world.EntityManager;
        Entity target = CreateCombatTarget(em, FactionIdentity.PlayerFactionId, new int2(4, 4), new float3(4f, 0f, 4f), 100);

        Assert.IsFalse(UnitAttackSystem.TryEmitUnitUnderAttackAudio(em, target, requestedAt: 2f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void BaseBreachedAudio_ResolvesOnlyForPlayerOwnedBarriers()
    {
        RuntimeBuildingEntity playerWall = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: true,
            displayName: "Perimeter Wall");
        RuntimeBuildingEntity playerGate = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: false,
            displayName: "Road_Barrier Gate");
        RuntimeBuildingEntity enemyWall = CreateRuntimeBuilding(
            FactionIdentity.EnemyFactionId,
            isWall: true,
            displayName: "Enemy Wall");
        RuntimeBuildingEntity playerBarracks = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: false,
            displayName: "Barracks");

        AssertBaseBreachedAudio(playerWall);
        AssertBaseBreachedAudio(playerGate);
        Assert.IsFalse(BuildingCombatUtilitySystemHelper.TryResolveBaseBreachedAudioEvent(
            enemyWall,
            out _,
            out _,
            out _,
            out _));
        Assert.IsFalse(BuildingCombatUtilitySystemHelper.TryResolveBaseBreachedAudioEvent(
            playerBarracks,
            out _,
            out _,
            out _,
            out _));
    }

    [Test]
    public void TryEmitBaseBreachedAudio_EnqueuesCriticalAlertRequest()
    {
        using World world = new("BaseBreachedAudioEmitTests");
        EntityManager em = world.EntityManager;
        Entity combatEntity = em.CreateEntity();
        RuntimeBuildingEntity playerWall = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: true,
            displayName: "Perimeter Wall",
            combatEntity: combatEntity);

        Assert.IsTrue(BuildingCombatUtilitySystemHelper.TryEmitBaseBreachedAudio(
            em,
            playerWall,
            requestedAt: 6.75f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.AlertBaseBreached, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.AlertBaseBreachedHash, requests[0].EventHash);
        Assert.AreEqual("Alerts", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.Critical, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
        Assert.AreEqual(combatEntity, requests[0].SourceEntity);
        Assert.That(requests[0].CooldownSeconds, Is.EqualTo(5f).Within(0.001f));
        Assert.That(requests[0].RequestedAt, Is.EqualTo(6.75f).Within(0.001f));
    }

    [Test]
    public void BeginDestroyedBuildingState_EmitsBaseBreachedAudioOnlyWhenCombatBoundaryRequestsIt()
    {
        using World world = new("BaseBreachedAudioDestroyedStateTests");
        EntityManager em = world.EntityManager;
        var helper = new BuildingCombatUtilitySystemHelper();
        BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> context =
            CreateBuildingCombatContext(em);

        RuntimeBuildingEntity manualDeleteWall = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: true,
            displayName: "Perimeter Wall",
            combatEntity: em.CreateEntity());
        Assert.IsTrue(helper.BeginDestroyedBuildingState(
            context,
            manualDeleteWall,
            now: 2f,
            destroyedLifetimeSeconds: 5f));
        Assert.AreEqual(0, GetAudioRequests(em).Length);

        RuntimeBuildingEntity combatDestroyedWall = CreateRuntimeBuilding(
            FactionIdentity.PlayerFactionId,
            isWall: true,
            displayName: "Perimeter Wall",
            combatEntity: em.CreateEntity());
        Assert.IsTrue(helper.BeginDestroyedBuildingState(
            context,
            combatDestroyedWall,
            now: 3f,
            destroyedLifetimeSeconds: 5f,
            emitBaseBreachedAudio: true));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.AlertBaseBreached, requests[0].EventId.ToString());
        Assert.AreEqual(combatDestroyedWall.CombatEntity, requests[0].SourceEntity);
    }

    private static void AssertThreatAudio(
        ThreatWarningType warningType,
        float etaSeconds,
        int threatCount,
        string expectedEventId,
        uint expectedEventHash,
        AudioPlaybackPriority expectedPriority,
        float expectedCooldownSeconds)
    {
        Assert.IsTrue(ThreatDetectionWarningSystem.TryResolveThreatWarningAudioEvent(
            warningType,
            etaSeconds,
            threatCount,
            out string eventId,
            out uint eventHash,
            out AudioPlaybackPriority priority,
            out float cooldownSeconds));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
        Assert.AreEqual(expectedPriority, priority);
        Assert.That(cooldownSeconds, Is.EqualTo(expectedCooldownSeconds).Within(0.001f));
    }

    private static void AssertBaseBreachedAudio(RuntimeBuildingEntity building)
    {
        Assert.IsTrue(BuildingCombatUtilitySystemHelper.TryResolveBaseBreachedAudioEvent(
            building,
            out string eventId,
            out uint eventHash,
            out AudioPlaybackPriority priority,
            out float cooldownSeconds));
        Assert.AreEqual(AudioEventIds.AlertBaseBreached, eventId);
        Assert.AreEqual(AudioEventIds.AlertBaseBreachedHash, eventHash);
        Assert.AreEqual(AudioPlaybackPriority.Critical, priority);
        Assert.That(cooldownSeconds, Is.EqualTo(5f).Within(0.001f));
    }

    private static RuntimeBuildingEntity CreateRuntimeBuilding(
        byte ownerFactionId,
        bool isWall,
        string displayName,
        Entity combatEntity = default)
    {
        return new RuntimeBuildingEntity
        {
            Definition = new BuildingDefinition
            {
                DisplayName = displayName,
                FootprintCells = new Vector2Int(1, 1),
                IsWall = isWall
            },
            HasOwnerFaction = true,
            OwnerFactionId = ownerFactionId,
            CombatEntity = combatEntity
        };
    }

    private static BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> CreateBuildingCombatContext(
        EntityManager em)
    {
        return new BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>(
            runtimeBuildingSystem: null,
            runtimeBuildings: null,
            tryGetEntityManager: TryGetEntityManager,
            rememberOpenBaseBreach: null,
            notifyHomeBuildingDestroyed: null,
            destroyedVisualPresentationHelper: null,
            destroyedVisualContext: default,
            destroyObject: null,
            refreshBuildingMarkerVisibility: null,
            notifyStaticMinimapChanged: null,
            log: null,
            enableDestroyDiagnostics: false);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, int2 cell, bool air, int health)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = factionId });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.AddComponentData(entity, new UnitMovementBehavior
        {
            AllowIdleWander = 0,
            UsesVehicleMotion = 0
        });
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 6f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }

    private static void CreateGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateCombatTarget(EntityManager em, byte factionId, int2 cell, float3 position, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateCombatAttacker(
        EntityManager em,
        byte factionId,
        int2 cell,
        float3 position,
        Entity target,
        int damage)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(EngageTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 2f,
            CooldownSeconds = 1f,
            Damage = damage,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(entity, new EngageTarget
        {
            Target = target,
            Cell = new int2(4, 4),
            Position = new float3(4f, 0f, 4f),
            IsCommanded = 1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static DynamicBuffer<AudioPlaybackRequestElement> GetAudioRequests(EntityManager em)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        return em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }
}
