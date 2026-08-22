using Game.Components;
using Game.Configs;
using Game.Runtime;
using SnivelerCode.GpuAnimation.Scripts.Components;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class UnitAnimationIndexSystemTests
{
    [MenuItem("Game/Validation/Run Unit Animation Index Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitAnimationIndexSystemTests();
            tests.ConfiguredRunShootAnimationResolvesAndAppliesToChildVisual();
            tests.EmptyConfiguredAnimationOrderDoesNotApplyInvalidAnimationIndex();
            tests.FallbackMovingAutoWanderResolvesWalkAnimation();
            tests.DeathAnimationAppliesToDetachedDetailedVisualOnFirstResolvedFrame();
            Debug.Log("[UnitAnimationIndexFocusedValidation] result=Passed tests=4");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitAnimationIndexFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ConfiguredRunShootAnimationResolvesAndAppliesToChildVisual()
    {
        using var world = new World(nameof(ConfiguredRunShootAnimationResolvesAndAppliesToChildVisual));
        EntityManager em = world.EntityManager;
        Entity unit = CreateUnit(em, moving: true, health: 100, attackSeconds: 0.25f, withAnimationOrder: true);
        DynamicBuffer<UnitAnimationOrderEntry> order = em.GetBuffer<UnitAnimationOrderEntry>(unit);
        order.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Idle });
        order.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.RunShoot });

        world.SetTime(new Unity.Core.TimeData(1d, 0.1f));
        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        byte expected = (byte)((byte)UnitAnimationKind.RunShoot + 1);
        Assert.AreEqual(expected, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Value);
        Assert.AreEqual(0, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Changed);
        Assert.That(em.GetComponentData<UnitAttackAnimationComponent>(unit).TimeRemaining, Is.EqualTo(0.15f).Within(0.0001f));
    }

    [Test]
    public void EmptyConfiguredAnimationOrderDoesNotApplyInvalidAnimationIndex()
    {
        using var world = new World(nameof(EmptyConfiguredAnimationOrderDoesNotApplyInvalidAnimationIndex));
        EntityManager em = world.EntityManager;
        Entity unit = CreateUnit(em, moving: false, health: 100, attackSeconds: 0.25f, withAnimationOrder: true);

        world.SetTime(new Unity.Core.TimeData(1d, 0.1f));
        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        UnitResolvedAnimationIndex resolved = em.GetComponentData<UnitResolvedAnimationIndex>(unit);
        Assert.AreEqual(byte.MaxValue, resolved.Value);
        Assert.AreEqual(0, resolved.Updated);
        Assert.That(em.GetComponentData<UnitAttackAnimationComponent>(unit).TimeRemaining, Is.EqualTo(0.15f).Within(0.0001f));
    }

    [Test]
    public void FallbackMovingAutoWanderResolvesWalkAnimation()
    {
        using var world = new World(nameof(FallbackMovingAutoWanderResolvesWalkAnimation));
        EntityManager em = world.EntityManager;
        Entity unit = CreateUnit(em, moving: true, health: 100, attackSeconds: 0f, withAnimationOrder: false);
        em.AddComponent<AutoWanderMoveTag>(unit);

        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        Assert.AreEqual(2, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Value);
    }

    [Test]
    public void DeathAnimationAppliesToDetachedDetailedVisualOnFirstResolvedFrame()
    {
        using var world = new World(nameof(DeathAnimationAppliesToDetachedDetailedVisualOnFirstResolvedFrame));
        EntityManager em = world.EntityManager;
        Entity unit = CreateUnit(em, moving: false, health: 0, attackSeconds: 0f, withAnimationOrder: true);
        DynamicBuffer<UnitAnimationOrderEntry> order = em.GetBuffer<UnitAnimationOrderEntry>(unit);
        order.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Idle });
        order.Add(new UnitAnimationOrderEntry { Kind = (byte)UnitAnimationKind.Death01 });
        em.AddComponentData(unit, new UnitDeathAnimationComponent { TimeRemaining = 0.25f });

        Entity detailedVisual = em.CreateEntity(typeof(MaterialAnimationIndex));
        em.SetComponentData(detailedVisual, new MaterialAnimationIndex { Value = 0 });
        em.AddComponentData(unit, new UnitDetailedVisualReference { Root = detailedVisual });

        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        byte expected = (byte)((byte)UnitAnimationKind.Death01 + 1);
        Assert.AreEqual(expected, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Value);
        Assert.AreEqual(expected, em.GetComponentData<MaterialAnimationIndex>(detailedVisual).Value,
            "A detached authored detailed visual must enter the death clip before its final pose is frozen.");
    }

    private static Entity CreateUnit(
        EntityManager em,
        bool moving,
        int health,
        float attackSeconds,
        bool withAnimationOrder)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitMoveVisualComponent),
            typeof(UnitHealth),
            typeof(UnitAttackAnimationComponent),
            typeof(UnitResolvedAnimationIndex),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = (byte)(moving ? 1 : 0) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = math.max(1, health) });
        em.SetComponentData(entity, new UnitAttackAnimationComponent { TimeRemaining = attackSeconds });
        em.SetComponentData(entity, new UnitResolvedAnimationIndex { Value = byte.MaxValue });
        em.SetComponentData(entity, LocalTransform.Identity);
        if (withAnimationOrder)
            em.AddBuffer<UnitAnimationOrderEntry>(entity);
        return entity;
    }
}
#endif
