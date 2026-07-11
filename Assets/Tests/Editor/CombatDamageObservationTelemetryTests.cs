#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Combat;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class CombatDamageObservationTelemetryTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.Queue_IsSingletonPreallocatedAndRetainsNewestSixtyFour());
            passed++;
            RunCase(test => test.DirectFire_RecordsClampedMixedSourceDamageWithoutLastSourceAttribution());
            passed++;
            RunCase(test => test.DirectFire_DamageDoesNotDependOnObservationQueue());
            passed++;
            RunCase(test => test.BuildingDefense_RecordsExactClampedDamageAndPositions());
            passed++;
            RunCase(test => test.GroundMissile_RecordsExactDirectAndSplashDamage());
            passed++;
            RunCase(test => test.AirMissile_RecordsExactClampedDamageAndPositions());
            passed++;
            RunCase(test => test.ThreatReadModel_ConsumesNewPlayerDamageCoalescesAndExpires());
            passed++;
            RunCase(test => test.ThreatReadModel_AcceptsObjectiveProtectedNeutralTarget());
            passed++;
            RunCase(test => test.ThreatReadModel_GatesInactiveRoutesAndSkipsRetainedEventsOnReentry());
            passed++;

            Debug.Log($"[CombatDamageObservationTelemetryValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[CombatDamageObservationTelemetryValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<CombatDamageObservationTelemetryTests> testCase)
    {
        testCase(new CombatDamageObservationTelemetryTests());
    }

    [Test]
    public void Queue_IsSingletonPreallocatedAndRetainsNewestSixtyFour()
    {
        using var world = new World(nameof(Queue_IsSingletonPreallocatedAndRetainsNewestSixtyFour));
        EntityManager em = world.EntityManager;
        using EntityQuery queueQuery = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));

        Entity queue = CombatDamageObservationUtility.EnsureQueue(em, queueQuery);
        Entity sameQueue = CombatDamageObservationUtility.EnsureQueue(em, queueQuery);
        Entity source = em.CreateEntity();
        Entity target = em.CreateEntity();

        Assert.AreEqual(queue, sameQueue);
        Assert.AreEqual(1, queueQuery.CalculateEntityCount());
        Assert.GreaterOrEqual(em.GetBuffer<CombatDamageObservationElement>(queue).Capacity, 64);

        for (int i = 1; i <= 70; i++)
        {
            Assert.IsTrue(CombatDamageObservationUtility.Append(
                em,
                queue,
                source,
                target,
                CombatDamageSourceKind.DirectFire,
                previousHealth: 11,
                currentHealth: 1,
                targetMaxHealth: 100,
                observedAt: i,
                sourceWorldPosition: new float3(i, 0f, 1f),
                targetWorldPosition: new float3(2f, 0f, i)));
        }

        CombatDamageObservationQueueComponent state =
            em.GetComponentData<CombatDamageObservationQueueComponent>(queue);
        DynamicBuffer<CombatDamageObservationElement> observations =
            em.GetBuffer<CombatDamageObservationElement>(queue);
        Assert.AreEqual(70, state.LastEventId);
        Assert.AreEqual(70u, state.Version);
        Assert.AreEqual(64, observations.Length);
        Assert.AreEqual(7, observations[0].EventId);
        Assert.AreEqual(70, observations[63].EventId);
        Assert.AreEqual(10, observations[63].DamageApplied);
        Assert.AreEqual(new float3(70f, 0f, 1f), observations[63].SourceWorldPosition);
        Assert.AreEqual(new float3(2f, 0f, 70f), observations[63].TargetWorldPosition);
    }

    [Test]
    public void DirectFire_RecordsClampedMixedSourceDamageWithoutLastSourceAttribution()
    {
        using var world = new World(nameof(DirectFire_RecordsClampedMixedSourceDamageWithoutLastSourceAttribution));
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        CreateQueue(em);
        Entity target = CreateDirectFireTarget(em, new float3(4f, 0f, 4f), health: 50);
        CreateDirectFireAttacker(em, new float3(4f, 0f, 5f), target, damage: 40);
        CreateDirectFireAttacker(em, new float3(5f, 0f, 4f), target, damage: 40);

        SystemHandle system = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(target).Current);
        DynamicBuffer<CombatDamageObservationElement> observations = GetObservations(em);
        Assert.AreEqual(1, observations.Length);
        CombatDamageObservationElement observation = observations[0];
        Assert.AreEqual(Entity.Null, observation.SourceEntity);
        Assert.AreEqual(target, observation.TargetEntity);
        Assert.AreEqual(CombatDamageSourceKind.DirectFire, observation.SourceKind);
        Assert.AreEqual(50, observation.DamageApplied);
        Assert.AreEqual(0, observation.TargetHealthAfter);
        Assert.AreEqual(50, observation.TargetMaxHealth);
        Assert.AreEqual(float3.zero, observation.SourceWorldPosition);
        Assert.AreEqual(new float3(4f, 0f, 4f), observation.TargetWorldPosition);
    }

    [Test]
    public void DirectFire_DamageDoesNotDependOnObservationQueue()
    {
        using var world = new World(nameof(DirectFire_DamageDoesNotDependOnObservationQueue));
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateDirectFireTarget(em, new float3(4f, 0f, 4f), health: 50);
        CreateDirectFireAttacker(em, new float3(4f, 0f, 5f), target, damage: 20);
        SystemHandle system = world.CreateSystem<UnitAttackSystem>();

        using (EntityQuery queueQuery = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent)))
            em.DestroyEntity(queueQuery);

        world.SetTime(new TimeData(1d, 0.1f));
        Assert.DoesNotThrow(() => system.Update(world.Unmanaged));
        Assert.AreEqual(30, em.GetComponentData<UnitHealth>(target).Current);
    }

    [Test]
    public void BuildingDefense_RecordsExactClampedDamageAndPositions()
    {
        using var world = new World(nameof(BuildingDefense_RecordsExactClampedDamageAndPositions));
        EntityManager em = world.EntityManager;
        CreateQueue(em);
        Entity tower = CreateDefenseTower(em, new float3(2f, 0f, 3f), damage: 10);
        Entity target = CreateHealthEntity(
            em,
            FactionIdentity.PlayerFactionId,
            new float3(6f, 0f, 3f),
            currentHealth: 5,
            maxHealth: 100);

        SystemHandle system = world.CreateSystem<BuildingDefenseAttackSystem>();
        world.SetTime(new TimeData(2d, 0.1f));
        system.Update(world.Unmanaged);

        CombatDamageObservationElement observation = GetSingleObservation(em);
        Assert.AreEqual(tower, observation.SourceEntity);
        Assert.AreEqual(target, observation.TargetEntity);
        Assert.AreEqual(CombatDamageSourceKind.BuildingDefense, observation.SourceKind);
        Assert.AreEqual(5, observation.DamageApplied);
        Assert.AreEqual(0, observation.TargetHealthAfter);
        Assert.AreEqual(100, observation.TargetMaxHealth);
        Assert.AreEqual(new float3(2f, 0f, 3f), observation.SourceWorldPosition);
        Assert.AreEqual(new float3(6f, 0f, 3f), observation.TargetWorldPosition);
    }

    [Test]
    public void GroundMissile_RecordsExactDirectAndSplashDamage()
    {
        using var world = new World(nameof(GroundMissile_RecordsExactDirectAndSplashDamage));
        EntityManager em = world.EntityManager;
        CreateQueue(em);
        Entity launcher = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(launcher, LocalTransform.FromPosition(new float3(1f, 0f, 1f)));
        Entity directTarget = CreateHealthEntity(
            em,
            FactionIdentity.PlayerFactionId,
            new float3(10f, 0f, 0f),
            currentHealth: 30,
            maxHealth: 100);
        Entity splashTarget = CreateHealthEntity(
            em,
            FactionIdentity.PlayerFactionId,
            new float3(11f, 0f, 0f),
            currentHealth: 80,
            maxHealth: 100);
        Entity impact = em.CreateEntity(typeof(GroundMissileImpactRequestComponent));
        em.SetComponentData(impact, new GroundMissileImpactRequestComponent
        {
            Source = launcher,
            TargetEntity = directTarget,
            TargetCell = new int2(10, 0),
            Position = new float3(10f, 0f, 0f),
            DamageRadius = 3f,
            Damage = 50,
            FactionId = FactionIdentity.EnemyFactionId
        });

        SystemHandle system = world.CreateSystem<GroundMissileImpactSystem>();
        world.SetTime(new TimeData(3d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(directTarget).Current);
        Assert.AreEqual(30, em.GetComponentData<UnitHealth>(splashTarget).Current);
        DynamicBuffer<CombatDamageObservationElement> observations = GetObservations(em);
        Assert.AreEqual(2, observations.Length);
        AssertObservationForTarget(observations, directTarget, launcher, CombatDamageSourceKind.GroundMissile, 30, 0);
        AssertObservationForTarget(observations, splashTarget, launcher, CombatDamageSourceKind.GroundMissile, 50, 30);
    }

    [Test]
    public void AirMissile_RecordsExactClampedDamageAndPositions()
    {
        using var world = new World(nameof(AirMissile_RecordsExactClampedDamageAndPositions));
        EntityManager em = world.EntityManager;
        CreateQueue(em);
        Entity launcher = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(launcher, LocalTransform.FromPosition(new float3(2f, 8f, 3f)));
        Entity target = CreateHealthEntity(
            em,
            FactionIdentity.PlayerFactionId,
            new float3(9f, 12f, 3f),
            currentHealth: 25,
            maxHealth: 100);
        Entity impact = em.CreateEntity(typeof(AirMissileImpactRequestComponent));
        em.SetComponentData(impact, new AirMissileImpactRequestComponent
        {
            Source = launcher,
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            FactionId = FactionIdentity.EnemyFactionId,
            Position = new float3(9f, 12f, 3f),
            Damage = 60
        });

        SystemHandle system = world.CreateSystem<AirMissileImpactSystem>();
        world.SetTime(new TimeData(4d, 0.1f));
        system.Update(world.Unmanaged);

        CombatDamageObservationElement observation = GetSingleObservation(em);
        Assert.AreEqual(launcher, observation.SourceEntity);
        Assert.AreEqual(target, observation.TargetEntity);
        Assert.AreEqual(CombatDamageSourceKind.AirMissile, observation.SourceKind);
        Assert.AreEqual(25, observation.DamageApplied);
        Assert.AreEqual(0, observation.TargetHealthAfter);
        Assert.AreEqual(new float3(2f, 8f, 3f), observation.SourceWorldPosition);
        Assert.AreEqual(new float3(9f, 12f, 3f), observation.TargetWorldPosition);
    }

    [Test]
    public void ThreatReadModel_ConsumesNewPlayerDamageCoalescesAndExpires()
    {
        using var world = new World(nameof(ThreatReadModel_ConsumesNewPlayerDamageCoalescesAndExpires));
        EntityManager em = world.EntityManager;
        Entity queue = CreateQueue(em);
        Entity source = CreateNamedHealthEntity(
            em,
            FactionIdentity.EnemyFactionId,
            new float3(0f, 0f, 0f),
            "Raider",
            100,
            100);
        Entity playerTarget = CreateNamedHealthEntity(
            em,
            FactionIdentity.PlayerFactionId,
            new float3(3f, 0f, 4f),
            "Alpha Squad",
            100,
            100);
        Entity enemyTarget = CreateNamedHealthEntity(
            em,
            FactionIdentity.EnemyFactionId,
            new float3(6f, 0f, 0f),
            "Enemy Squad",
            100,
            100);
        Entity boundary = CreateAssistantBoundary(em, active: true);
        CreateStartedMatch(em);

        CombatDamageObservationUtility.Append(
            em, queue, source, playerTarget, CombatDamageSourceKind.DirectFire,
            100, 90, 100, 0.25f, float3.zero, new float3(3f, 0f, 4f));
        SystemHandle system = world.CreateSystem<AssistantThreatReadModelSystem>();
        world.SetTime(new TimeData(0.5d, 0.1f));
        system.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetBuffer<AssistantThreatReadModelElement>(boundary).Length,
            "A new match must initialize its cursor past retained observations.");

        CombatDamageObservationUtility.Append(
            em, queue, source, playerTarget, CombatDamageSourceKind.DirectFire,
            90, 80, 100, 1f, float3.zero, new float3(3f, 0f, 4f));
        CombatDamageObservationUtility.Append(
            em, queue, source, enemyTarget, CombatDamageSourceKind.DirectFire,
            100, 50, 100, 1f, float3.zero, new float3(6f, 0f, 0f));
        CombatDamageObservationUtility.Append(
            em, queue, source, playerTarget, CombatDamageSourceKind.DirectFire,
            50, 20, 100, 1f, float3.zero, new float3(3f, 0f, 4f));

        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);
        DynamicBuffer<AssistantThreatReadModelElement> threats =
            em.GetBuffer<AssistantThreatReadModelElement>(boundary);
        Assert.AreEqual(1, threats.Length, "Repeated hits must coalesce and enemy-only damage must be ignored.");
        AssistantThreatReadModelElement threat = threats[0];
        Assert.AreEqual(playerTarget, threat.FriendlyTarget);
        Assert.AreEqual(source, threat.HostileSource);
        Assert.AreEqual(AssistantThreatKind.GroundAttack, threat.Kind);
        Assert.AreEqual(AssistantMessagePriority.Critical, threat.Priority);
        Assert.AreEqual("Alpha Squad", threat.FriendlyName.ToString());
        Assert.AreEqual("Raider", threat.HostileName.ToString());
        Assert.AreEqual(30, threat.Damage);
        Assert.AreEqual(20, threat.FriendlyHealth);
        Assert.AreEqual(5f, threat.Distance, 0.001f);
        Assert.AreEqual(7f, threat.ExpiresAt, 0.001f);

        uint version = em.GetComponentData<AssistantThreatReadModelStateComponent>(boundary).Version;
        world.SetTime(new TimeData(2d, 0.1f));
        system.Update(world.Unmanaged);
        Assert.AreEqual(version, em.GetComponentData<AssistantThreatReadModelStateComponent>(boundary).Version,
            "Unchanged queue and pre-expiry time must not republish threats.");

        world.SetTime(new TimeData(7d, 0.1f));
        system.Update(world.Unmanaged);
        Assert.AreEqual(0, threats.Length);
        Assert.Greater(em.GetComponentData<AssistantThreatReadModelStateComponent>(boundary).Version, version);
    }

    [Test]
    public void ThreatReadModel_GatesInactiveRoutesAndSkipsRetainedEventsOnReentry()
    {
        using var world = new World(nameof(ThreatReadModel_GatesInactiveRoutesAndSkipsRetainedEventsOnReentry));
        EntityManager em = world.EntityManager;
        Entity queue = CreateQueue(em);
        Entity source = CreateNamedHealthEntity(
            em, FactionIdentity.EnemyFactionId, float3.zero, "Hostile", 100, 100);
        Entity target = CreateNamedHealthEntity(
            em, FactionIdentity.PlayerFactionId, new float3(2f, 0f, 0f), "Friendly", 100, 100);
        Entity boundary = CreateAssistantBoundary(em, active: true);
        CreateStartedMatch(em);
        SystemHandle system = world.CreateSystem<AssistantThreatReadModelSystem>();

        world.SetTime(new TimeData(0.5d, 0.1f));
        system.Update(world.Unmanaged);
        CombatDamageObservationUtility.Append(
            em, queue, source, target, CombatDamageSourceKind.DirectFire,
            100, 90, 100, 1f, float3.zero, new float3(2f, 0f, 0f));
        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);
        Assert.AreEqual(1, em.GetBuffer<AssistantThreatReadModelElement>(boundary).Length);

        UiShellStateComponent shell = em.GetComponentData<UiShellStateComponent>(boundary);
        shell.IsTransitionRunning = 1;
        em.SetComponentData(boundary, shell);
        world.SetTime(new TimeData(1.5d, 0.1f));
        system.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetBuffer<AssistantThreatReadModelElement>(boundary).Length);

        CombatDamageObservationUtility.Append(
            em, queue, source, target, CombatDamageSourceKind.DirectFire,
            90, 80, 100, 2f, float3.zero, new float3(2f, 0f, 0f));
        shell.IsTransitionRunning = 0;
        em.SetComponentData(boundary, shell);
        world.SetTime(new TimeData(2d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<AssistantThreatReadModelElement>(boundary).Length,
            "Observations retained during transition must not replay when the active match route resumes.");
        AssistantThreatReadModelStateComponent state =
            em.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);
        Assert.AreEqual(em.GetComponentData<CombatDamageObservationQueueComponent>(queue).LastEventId,
            state.LastConsumedEventId);
    }

    [Test]
    public void ThreatReadModel_AcceptsObjectiveProtectedNeutralTarget()
    {
        using var world = new World(nameof(ThreatReadModel_AcceptsObjectiveProtectedNeutralTarget));
        EntityManager em = world.EntityManager;
        Entity queue = CreateQueue(em);
        Entity source = CreateNamedHealthEntity(
            em, FactionIdentity.EnemyFactionId, float3.zero, "Hostile", 100, 100);
        Entity protectedTarget = CreateNamedHealthEntity(
            em, FactionIdentity.NeutralFactionId, new float3(2f, 0f, 0f), "Civilian Relay", 100, 100);
        Entity boundary = CreateAssistantBoundary(em, active: true);
        CreateStartedMatch(em);
        SystemHandle system = world.CreateSystem<AssistantThreatReadModelSystem>();

        world.SetTime(new TimeData(0.5d, 0.1f));
        system.Update(world.Unmanaged);
        em.GetBuffer<MatchObjectiveRuntimeElement>(boundary).Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 41,
            ObjectiveId = new FixedString64Bytes("protect.civilian.relay"),
            State = MatchObjectiveState.Active,
            TargetEntity = protectedTarget,
            ProtectsTarget = 1,
            Title = new FixedString64Bytes("Protect the civilian relay")
        });
        CombatDamageObservationUtility.Append(
            em, queue, source, protectedTarget, CombatDamageSourceKind.DirectFire,
            100, 80, 100, 1f, float3.zero, new float3(2f, 0f, 0f));

        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);

        AssistantThreatReadModelElement threat =
            em.GetBuffer<AssistantThreatReadModelElement>(boundary)[0];
        Assert.AreEqual(protectedTarget, threat.FriendlyTarget);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, threat.FriendlyFactionId);
        Assert.AreEqual("Civilian Relay", threat.FriendlyName.ToString());
    }

    private static Entity CreateQueue(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
        return CombatDamageObservationUtility.EnsureQueue(em, query);
    }

    private static DynamicBuffer<CombatDamageObservationElement> GetObservations(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
        Assert.AreEqual(1, query.CalculateEntityCount());
        return em.GetBuffer<CombatDamageObservationElement>(query.GetSingletonEntity());
    }

    private static CombatDamageObservationElement GetSingleObservation(EntityManager em)
    {
        DynamicBuffer<CombatDamageObservationElement> observations = GetObservations(em);
        Assert.AreEqual(1, observations.Length);
        return observations[0];
    }

    private static void AssertObservationForTarget(
        DynamicBuffer<CombatDamageObservationElement> observations,
        Entity target,
        Entity source,
        CombatDamageSourceKind kind,
        int damage,
        int healthAfter)
    {
        for (int i = 0; i < observations.Length; i++)
        {
            if (observations[i].TargetEntity != target)
                continue;

            Assert.AreEqual(source, observations[i].SourceEntity);
            Assert.AreEqual(kind, observations[i].SourceKind);
            Assert.AreEqual(damage, observations[i].DamageApplied);
            Assert.AreEqual(healthAfter, observations[i].TargetHealthAfter);
            return;
        }

        Assert.Fail($"No damage observation found for target {target}.");
    }

    private static void CreateGrid(EntityManager em)
    {
        Entity grid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(grid, new GridConfig
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateDirectFireTarget(EntityManager em, float3 position, int health)
    {
        Entity target = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new UnitGrid { Cell = new int2(4, 4) });
        em.SetComponentData(target, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(target, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(target, LocalTransform.FromPosition(position));
        return target;
    }

    private static Entity CreateDirectFireAttacker(
        EntityManager em,
        float3 position,
        Entity target,
        int damage)
    {
        Entity attacker = em.CreateEntity(
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
        em.SetComponentData(attacker, new UnitGrid { Cell = new int2((int)position.x, (int)position.z) });
        em.SetComponentData(attacker, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(attacker, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(attacker, new UnitAttack
        {
            Range = 2f,
            CooldownSeconds = 1f,
            Damage = damage,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(attacker, new EngageTarget
        {
            Target = target,
            Cell = new int2(4, 4),
            Position = new float3(4f, 0f, 4f),
            IsCommanded = 1
        });
        em.SetComponentData(attacker, LocalTransform.FromPosition(position));
        return attacker;
    }

    private static Entity CreateDefenseTower(EntityManager em, float3 position, int damage)
    {
        Entity tower = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(BuildingDefenseWeapon),
            typeof(UnitHealth),
            typeof(Faction),
            typeof(LocalTransform),
            typeof(UnitAttackTraceComponent));
        em.SetComponentData(tower, new BuildingDefenseWeapon
        {
            Range = 100f,
            CooldownSeconds = 1f,
            Damage = damage,
            MaxConcurrentAttacks = 1,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(tower, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(tower, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(tower, LocalTransform.FromPosition(position));
        em.AddBuffer<BuildingDefenseAttackSlot>(tower);
        return tower;
    }

    private static Entity CreateHealthEntity(
        EntityManager em,
        byte factionId,
        float3 position,
        int currentHealth,
        int maxHealth)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = currentHealth, Max = maxHealth });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateNamedHealthEntity(
        EntityManager em,
        byte factionId,
        float3 position,
        string name,
        int currentHealth,
        int maxHealth)
    {
        Entity entity = CreateHealthEntity(em, factionId, position, currentHealth, maxHealth);
        em.AddComponentData(entity, new UnitDisplayInfo { Name = new FixedString64Bytes(name) });
        return entity;
    }

    private static Entity CreateAssistantBoundary(EntityManager em, bool active)
    {
        Entity boundary = em.CreateEntity(typeof(UiShellStateComponent), typeof(UiMatchHudHeaderComponent));
        em.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = active ? UIRoute.Match : UIRoute.MainMenu,
            CurrentMode = active ? UiShellMode.MatchHud : UiShellMode.MainMenu,
            IsTransitionRunning = 0
        });
        return boundary;
    }

    private static void CreateStartedMatch(EntityManager em)
    {
        Entity match = em.CreateEntity(typeof(MatchStartQueueComponent));
        em.SetComponentData(match, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
    }
}
#endif
