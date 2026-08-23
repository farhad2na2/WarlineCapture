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
            tests.FullCanonicalAnimationOrderResolvesRunToEighthGpuClip();
            tests.ProductionCivilianConfigsPlaceRunInSecondGpuClip();
            tests.DeathAnimationAppliesToDetachedDetailedVisualOnFirstResolvedFrame();
            tests.GameplayInertResolvedAnimationAppliesToModelVisual();
            Debug.Log("[UnitAnimationIndexFocusedValidation] result=Passed tests=7");
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

        byte expected = 2;
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
    public void FullCanonicalAnimationOrderResolvesRunToEighthGpuClip()
    {
        using var world = new World(nameof(FullCanonicalAnimationOrderResolvesRunToEighthGpuClip));
        EntityManager em = world.EntityManager;
        Entity unit = CreateUnit(em, moving: true, health: 100, attackSeconds: 0f, withAnimationOrder: true);
        DynamicBuffer<UnitAnimationOrderEntry> order = em.GetBuffer<UnitAnimationOrderEntry>(unit);
        for (byte kind = (byte)UnitAnimationKind.Idle; kind <= (byte)UnitAnimationKind.Death03; kind++)
            order.Add(new UnitAnimationOrderEntry { Kind = kind });

        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        Assert.AreEqual(8, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Value);
    }

    [Test]
    public void ProductionCivilianConfigsPlaceRunInSecondGpuClip()
    {
        string[] configPaths =
        {
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_01_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Male_02_Config.asset",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_02_Config.asset"
        };

        foreach (string path in configPaths)
        {
            UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(path);
            Assert.That(config, Is.Not.Null, path);
            Assert.That(config.AnimationOrder, Has.Count.EqualTo(7), path);
            Assert.That(config.AnimationOrder[0], Is.EqualTo(UnitAnimationKind.Idle), path);
            Assert.That(config.AnimationOrder[1], Is.EqualTo(UnitAnimationKind.Run), path);
        }
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

        byte expected = 2;
        Assert.AreEqual(expected, em.GetComponentData<UnitResolvedAnimationIndex>(unit).Value);
        Assert.AreEqual(expected, em.GetComponentData<MaterialAnimationIndex>(detailedVisual).Value,
            "A detached authored detailed visual must enter the death clip before its final pose is frozen.");
    }

    [Test]
    public void GameplayInertResolvedAnimationAppliesToModelVisual()
    {
        using var world = new World(nameof(GameplayInertResolvedAnimationAppliesToModelVisual));
        EntityManager em = world.EntityManager;
        Entity visual = em.CreateEntity(typeof(MaterialAnimationIndex));
        em.SetComponentData(visual, new MaterialAnimationIndex { Value = 1 });
        Entity presentation = em.CreateEntity(
            typeof(UnitMoveVisualComponent),
            typeof(UnitResolvedAnimationIndex),
            typeof(UnitModelInstanceReference));
        byte runIndex = 2;
        em.SetComponentData(presentation, new UnitMoveVisualComponent { IsMoving = 1 });
        em.SetComponentData(presentation, new UnitResolvedAnimationIndex
        {
            Value = runIndex,
            Changed = 1,
            Updated = 1
        });
        em.SetComponentData(presentation, new UnitModelInstanceReference { Instance = visual });

        SystemHandle system = world.CreateSystem<UnitAnimationIndexSystem>();
        system.Update(world.Unmanaged);

        Assert.AreEqual(runIndex, em.GetComponentData<MaterialAnimationIndex>(visual).Value);
        Assert.AreEqual(0, em.GetComponentData<UnitResolvedAnimationIndex>(presentation).Changed);
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
