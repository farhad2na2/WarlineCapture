using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class BuildingDefenseAttackSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new BuildingDefenseAttackSystemTests();
            tests.GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget();
            passed++;

            Debug.Log($"[BuildingDefenseAttackSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[BuildingDefenseAttackSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget()
    {
        using World world = new("GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(em, new float3(0f, 0f, 0f));
        Entity airTarget = CreateTarget(em, new float3(2f, 12f, 0f), health: 100, air: true);
        Entity groundTarget = CreateTarget(em, new float3(10f, 0f, 0f), health: 100, air: false);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(airTarget).Current, "Guard towers must not use ground-defense fire against aircraft.");
        Assert.AreEqual(90, em.GetComponentData<UnitHealth>(groundTarget).Current, "Guard towers should still attack in-range ground units.");
        Assert.IsTrue(em.HasComponent<RecentAttacker>(groundTarget));
        Assert.AreEqual(tower, em.GetComponentData<RecentAttacker>(groundTarget).Attacker);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(airTarget));
    }

    private static Entity CreateGuardTower(EntityManager em, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(BuildingDefenseWeapon),
            typeof(UnitHealth),
            typeof(Faction),
            typeof(LocalTransform),
            typeof(UnitAttackTraceComponent));

        em.SetComponentData(entity, new BuildingDefenseWeapon
        {
            Range = 100f,
            CooldownSeconds = 0.3f,
            Damage = 10,
            MaxConcurrentAttacks = 1,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(entity, new UnitHealth { Current = 700, Max = 700 });
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitAttackTraceComponent());
        em.AddBuffer<BuildingDefenseAttackSlot>(entity);
        return entity;
    }

    private static Entity CreateTarget(EntityManager em, float3 position, int health, bool air)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitHealth),
            typeof(Faction),
            typeof(LocalTransform));

        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        if (air)
            em.AddComponentData(entity, new UnitAirMovement { CruiseHeight = math.max(1f, position.y), RunwayTaxiSpeed = 5f });
        return entity;
    }
}
