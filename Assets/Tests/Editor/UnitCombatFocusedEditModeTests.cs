using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitCombatFocusedEditModeTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitCombatFocusedEditModeTests();
            tests.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
            tests.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
            tests.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
            Debug.Log("[UnitCombatFocusedEditModeValidation] result=Passed tests=3");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitCombatFocusedEditModeValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState()
    {
        using var world = new World("StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
        Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(65, em.GetComponentData<UnitHealth>(target).Current);
        Assert.IsTrue(em.HasComponent<RecentAttacker>(target), "Damage should record who last attacked the target.");
        RecentAttacker recentAttacker = em.GetComponentData<RecentAttacker>(target);
        Assert.AreEqual(attacker, recentAttacker.Attacker);
        Assert.AreEqual(new int2(4, 5), recentAttacker.Cell);
        Assert.AreEqual(new float3(4f, 0f, 5f), recentAttacker.Position);

        Assert.IsTrue(em.HasComponent<RecentDamageHealthBarVisibility>(target), "Non-lethal combat damage should show the recent-damage health bar window.");
        Assert.AreEqual(2f, em.GetComponentData<RecentDamageHealthBarVisibility>(target).TimeRemaining, 0.001f);
        Assert.IsFalse(em.HasComponent<UnitDeathAnimationComponent>(target), "Non-lethal damage must not start death cleanup.");

        UnitAttackCooldownComponent cooldown = em.GetComponentData<UnitAttackCooldownComponent>(attacker);
        UnitAttackTraceComponent trace = em.GetComponentData<UnitAttackTraceComponent>(attacker);
        Assert.AreEqual(1f, cooldown.CooldownRemaining, 0.001f);
        Assert.AreEqual(1, trace.ShotCounter);
        Assert.Greater(trace.TimeRemaining, 0f);
    }

    [Test]
    public void AttackVfxRequestResolution_PopulatesMuzzlePlaybackData()
    {
        using var world = new World("AttackVfxRequestResolution_PopulatesMuzzlePlaybackData");
        EntityManager em = world.EntityManager;
        GameObject prefab = new("MuzzleVfxPrefab");
        try
        {
            Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
            Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
            Entity turret = em.CreateEntity(typeof(LocalToWorld));
            em.SetComponentData(turret, new LocalToWorld { Value = float4x4.Translate(new float3(4.25f, 1.5f, 5f)) });
            em.AddComponentData(attacker, new UnitTurretReference { Turret = turret });
            em.AddComponentData(attacker, new UnitMuzzleFlashVfxReference
            {
                Prefab = prefab,
                HeightOffset = 0.25f,
                ForwardOffset = 0.5f
            });
            em.AddComponentData(attacker, new UnitAttackTraceOriginPattern
            {
                OriginCount = 3,
                LateralOffset = 0.4f
            });

            bool built = UnitAttackSystem.TryBuildAttackVfxRequest(
                em,
                UnitAttackVfxRequestKind.MuzzleFlash,
                attacker,
                target,
                new float3(4f, 0f, 5f),
                new float3(4f, 0f, 4f),
                out UnitAttackVfxRequest request);

            Assert.IsTrue(built);
            Assert.AreSame(prefab, request.Prefab.Value);
            Assert.AreEqual((byte)UnitAttackVfxRequestKind.MuzzleFlash, request.Kind);
            Assert.AreEqual((byte)3, request.OriginCount);
            Assert.AreEqual(0.4f, request.LateralOffset, 0.001f);
            Assert.AreEqual(4.25f, request.PlaybackPosition.x, 0.001f);
            Assert.AreEqual(1.75f, request.PlaybackPosition.y, 0.001f);
            Assert.Less(request.PlaybackPosition.z, 5f, "Forward offset should move the muzzle toward the target.");
            Assert.Greater(math.lengthsq(request.SideRight), 0.9f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void AttackVfxRequestResolution_PopulatesImpactPlaybackData()
    {
        using var world = new World("AttackVfxRequestResolution_PopulatesImpactPlaybackData");
        EntityManager em = world.EntityManager;
        GameObject prefab = new("ImpactVfxPrefab");
        try
        {
            Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
            Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
            em.AddComponentData(attacker, new UnitAttackImpactVfxReference { Prefab = prefab });

            bool built = UnitAttackSystem.TryBuildAttackVfxRequest(
                em,
                UnitAttackVfxRequestKind.Impact,
                attacker,
                target,
                new float3(4f, 0f, 5f),
                new float3(4f, 0f, 4f),
                out UnitAttackVfxRequest request);

            Assert.IsTrue(built);
            Assert.AreSame(prefab, request.Prefab.Value);
            Assert.AreEqual((byte)UnitAttackVfxRequestKind.Impact, request.Kind);
            Assert.AreEqual(new float3(4f, 0f, 4f), request.PlaybackPosition);
            Assert.AreEqual((byte)1, request.OriginCount);
            Assert.AreEqual(0f, request.LateralOffset, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
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
#endif
