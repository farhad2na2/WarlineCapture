#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Combat;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
public sealed class DirectDamageSequencerTests
{
    public static void RunChecks()
    {
        var tests = new DirectDamageSequencerTests();
        tests.CoverAndArithmeticMatchOriginalBoundariesAndSeededSequences();
        tests.CommitDecisionsPreserveOverkillWindupAndMissingActors();
        tests.NativeOrderedOverkillPreservesFeedbackAndUnspentShot();
        tests.NativeMutuallyLethalActorsBothFireFromSnapshot();
        tests.NativeCoverEligibilityPreservesBypassRules();
        Debug.Log("[DirectDamageSequencerParity] result=Passed tests=5 missionReady=0");
    }

    [Test]
    public void CoverAndArithmeticMatchOriginalBoundariesAndSeededSequences()
    {
        foreach (int damage in new[] { int.MinValue, -1, 0, 1, 2, 3, 999, 1000, int.MaxValue })
        foreach (ushort permille in new ushort[] { 0, 1, 499, 500, 999, 1000, ushort.MaxValue })
        {
            int expected = damage <= 0 ? 0 : permille == 0 || permille >= 1000 ? damage :
                (int)Math.Max(1L, ((long)damage * permille + 500L) / 1000L);
            Assert.AreEqual(expected, DirectDamageSequencer.ApplyCover(damage, permille));
            Assert.AreEqual(damage, DirectDamageSequencer.ApplyToTarget(damage, false, permille));
        }
        var random = new System.Random(741029);
        var aggregate = new DirectDamageAggregate();
        int originalTotal = 0, predicted = int.MaxValue;
        DirectDamageSource first = default;
        byte mixed = 0, tracer = 0;
        for (int i = 0; i < 4096; i++)
        {
            bool exists = i % 17 != 0;
            int damage = i % 13 == 0 ? int.MaxValue : random.Next(1, 10000);
            var source = new DirectDamageSource { Index = i % 3, Version = i % 5, CellX = i, CellZ = -i,
                X = i * .25f, Y = 3f, Z = -i * .125f };
            byte show = (byte)(i % 2);
            if (!exists) { originalTotal = damage; first = source; mixed = 0; tracer = show; }
            else
            {
                originalTotal = unchecked(originalTotal + damage);
                if (first.Index != source.Index || first.Version != source.Version) mixed = 1;
                tracer |= show;
            }
            int expectedHealth = math.max(0, unchecked(predicted - damage));
            predicted = DirectDamageSequencer.Accumulate(ref aggregate, exists, predicted, damage, source, show);
            Assert.AreEqual(expectedHealth, predicted);
            Assert.AreEqual(originalTotal, aggregate.TotalDamage);
            Assert.AreEqual(first, aggregate.ObservationSource);
            Assert.AreEqual(source, aggregate.LastSource);
            Assert.AreEqual(mixed, aggregate.HasMixedObservationSources);
            Assert.AreEqual(tracer, aggregate.ShowImpactVfx);
            Assert.AreEqual(math.max(0, unchecked(100 - originalTotal)), DirectDamageSequencer.RemainingHealth(100, originalTotal));
        }
        // Same entity index with a new version is a different source; movement by one source is not.
        aggregate = default;
        var a = new DirectDamageSource { Index = 7, Version = 1, X = 2 };
        DirectDamageSequencer.Accumulate(ref aggregate, false, 100, 1, a, 0);
        a.X = 5;
        DirectDamageSequencer.Accumulate(ref aggregate, true, 99, 1, a, 1);
        Assert.AreEqual(0, aggregate.HasMixedObservationSources);
        Assert.AreEqual(2f, aggregate.ObservationSource.X);
        a.Version++;
        DirectDamageSequencer.Accumulate(ref aggregate, true, 98, 1, a, 0);
        Assert.AreEqual(1, aggregate.HasMixedObservationSources);
    }

    [Test]
    public void CommitDecisionsPreserveOverkillWindupAndMissingActors()
    {
        foreach (StandardAttackKind kind in Enum.GetValues(typeof(StandardAttackKind)))
        foreach (bool exists in new[] { false, true })
        foreach (int hp in new[] { -1, 0, 1, 100 })
        {
            var expected = !exists ? StandardAttackCommit.MissingAttacker :
                kind == StandardAttackKind.ClearTarget ? StandardAttackCommit.ClearTarget :
                kind == StandardAttackKind.None ? StandardAttackCommit.BaseOnly :
                kind == StandardAttackKind.Windup ? StandardAttackCommit.Windup :
                hp <= 0 ? StandardAttackCommit.ClearTarget : StandardAttackCommit.Shot;
            Assert.AreEqual(expected, DirectDamageSequencer.Decide(kind, exists, hp));
        }
        var original = new StandardAttackState { ShotCounter = 5, CooldownRemaining = .1f };
        var plan = new StandardAttackResult { BaseState = original, State = new StandardAttackState
            { ShotCounter = 6, CooldownRemaining = 1, AnimationTimeRemaining = 2, TraceTimeRemaining = 3 } };
        Assert.AreEqual(original, DirectDamageSequencer.CommittedState(original, plan, StandardAttackCommit.ClearTarget));
        var windup = DirectDamageSequencer.CommittedState(original, plan, StandardAttackCommit.Windup);
        Assert.AreEqual(5, windup.ShotCounter); Assert.AreEqual(0, windup.TraceTimeRemaining);
        Assert.AreEqual(1, windup.CooldownRemaining); Assert.AreEqual(2, windup.AnimationTimeRemaining);
    }

    [Test]
    public void NativeOrderedOverkillPreservesFeedbackAndUnspentShot()
    {
        using var world = new World(nameof(NativeOrderedOverkillPreservesFeedbackAndUnspentShot));
        var em = world.EntityManager; CreateGrid(em);
        var target = CreateTarget(em, new int2(4, 4), new float3(4, 0, 4), 100);
        for (int i = 0; i < 3; i++) CreateAttacker(em, new int2(4, 5), new float3(4, 0, 5), target, 120);
        // Cover halves every120 point shot; the third shot must be skipped once predicted health is zero.
        em.AddComponentData(target, new SupportRangedCoverComponent { DirectDamagePermille = 500 });
        using var attackersQuery = em.CreateEntityQuery(typeof(UnitAttack), typeof(EngageTarget));
        using var attackers = attackersQuery.ToEntityArray(Allocator.Temp);
        world.CreateSystem<CombatDamageObservationBootstrapSystem>();
        var system = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(.1, .1f)); system.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(target).Current);
        Assert.AreEqual(1, em.GetComponentData<UnitAttackTraceComponent>(attackers[0]).ShotCounter);
        Assert.AreEqual(1, em.GetComponentData<UnitAttackTraceComponent>(attackers[1]).ShotCounter);
        Assert.AreEqual(0, em.GetComponentData<UnitAttackTraceComponent>(attackers[2]).ShotCounter);
        Assert.AreEqual(-.1f, em.GetComponentData<UnitAttackCooldownComponent>(attackers[2]).CooldownRemaining);
        Assert.AreEqual(Entity.Null, em.GetComponentData<EngageTarget>(attackers[2]).Target);
        var recent = em.GetComponentData<RecentAttacker>(target);
        Assert.AreEqual(attackers[1], recent.Attacker);
        Assert.AreEqual(new int2(4, 5), recent.Cell);
        Assert.AreEqual(new float3(4, 0, 5), recent.Position);
        using var queueQuery = em.CreateEntityQuery(typeof(CombatDamageObservationElement));
        var events = em.GetBuffer<CombatDamageObservationElement>(queueQuery.GetSingletonEntity());
        Assert.AreEqual(1, events.Length); Assert.AreEqual(100, events[0].DamageApplied);
        Assert.AreEqual(Entity.Null, events[0].SourceEntity, "Mixed attackers cannot claim a single observation source.");
        Assert.AreEqual(float3.zero, events[0].SourceWorldPosition);
        Assert.AreEqual(target, events[0].TargetEntity);
        Assert.AreEqual(CombatDamageSourceKind.DirectFire, events[0].SourceKind);
        Assert.AreEqual(0, events[0].TargetHealthAfter);
        Assert.AreEqual(2f, em.GetComponentData<RecentDamageHealthBarVisibility>(target).TimeRemaining);
    }

    [Test]
    public void NativeMutuallyLethalActorsBothFireFromSnapshot()
    {
        using var world = new World(nameof(NativeMutuallyLethalActorsBothFireFromSnapshot));
        var em = world.EntityManager; CreateGrid(em);
        var a = CreateAttacker(em, new int2(4, 4), new float3(4, 0, 4), Entity.Null, 200);
        var b = CreateAttacker(em, new int2(4, 5), new float3(4, 0, 5), a, 200);
        em.SetComponentData(a, new EngageTarget { Target = b, Cell = new int2(4, 5), Position = new float3(4, 0, 5), IsCommanded = 1 });
        var system = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(.1, .1f)); system.Update(world.Unmanaged);
        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(a).Current);
        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(b).Current);
        Assert.AreEqual(1, em.GetComponentData<UnitAttackTraceComponent>(a).ShotCounter);
        Assert.AreEqual(1, em.GetComponentData<UnitAttackTraceComponent>(b).ShotCounter);
    }

    [Test]
    public void NativeCoverEligibilityPreservesBypassRules()
    {
        using var world = new World(nameof(NativeCoverEligibilityPreservesBypassRules));
        var em = world.EntityManager;
        var target = CreateTarget(em, new int2(1), float3.zero, 100);
        em.AddComponentData(target, new SupportRangedCoverComponent { DirectDamagePermille = 500 });
        Assert.AreEqual(18, SupportDamageUtilitySystemHelper.ApplyToTarget(em, target, 35));
        foreach (ComponentType type in new[] { ComponentType.ReadWrite<UnitAirMovement>(),
            ComponentType.ReadWrite<UnitAirComponent>(), ComponentType.ReadWrite<RuntimeBuildingCombatInfo>() })
        {
            em.AddComponent(target, type);
            Assert.AreEqual(35, SupportDamageUtilitySystemHelper.ApplyToTarget(em, target, 35));
            Assert.AreEqual(-1, SupportDamageUtilitySystemHelper.ApplyToTarget(em, target, -1));
            em.RemoveComponent(target, type);
        }
        em.RemoveComponent<UnitGrid>(target);
        Assert.AreEqual(35, SupportDamageUtilitySystemHelper.ApplyToTarget(em, target, 35));
        em.RemoveComponent<SupportRangedCoverComponent>(target);
        Assert.AreEqual(35, SupportDamageUtilitySystemHelper.ApplyToTarget(em, target, 35));
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

    private static Entity CreateTarget(EntityManager em, int2 cell, float3 position, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateAttacker(EntityManager em, int2 cell, float3 position, Entity target, int damage)
    {
        Entity entity = em.CreateEntity(
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
}
}
#endif
