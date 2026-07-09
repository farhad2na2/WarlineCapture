using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using Game.Runtime;

public sealed class AutomaticCombatFactionTargetingTests
{
    [Test]
    public void UnitEngagementSystem_IgnoresNeutralCitizenTargets()
    {
        using World world = new("AutomaticCombatFactionTargetingTests_UnitEngagement");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity attacker = CreateCombatUnit(em, FactionIdentity.PlayerFactionId, new int2(10, 10), attackRange: 8f);
        Entity citizen = CreateCombatUnit(em, FactionIdentity.NeutralFactionId, new int2(11, 10), attackRange: 8f);

        EndSimulationEntityCommandBufferSystem endSimulation = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        SystemHandle engagementSystem = world.CreateSystem<UnitEngagementSystem>();

        world.SetTime(new TimeData(1d, 0.2f));
        engagementSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();
        endSimulation.Update();

        Assert.IsFalse(em.HasComponent<EngageTarget>(attacker), "Player units must not auto-acquire faction 0 citizen targets.");
    }

    [Test]
    public void BuildingDefenseAttackSystem_IgnoresNeutralCitizenTargetsAndAttacksHostileTargets()
    {
        using World world = new("AutomaticCombatFactionTargetingTests_BuildingDefense");
        EntityManager em = world.EntityManager;

        Entity tower = CreateDefenseBuilding(em, FactionIdentity.PlayerFactionId, new float3(0f, 0f, 0f));
        Entity citizenHouse = CreateHealthTarget(em, FactionIdentity.NeutralFactionId, new float3(2f, 0f, 0f));

        SystemHandle defenseSystem = world.CreateSystem<BuildingDefenseAttackSystem>();

        world.SetTime(new TimeData(1d, 0.2f));
        defenseSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(citizenHouse).Current, "Player towers must not auto-attack faction 0 citizen buildings.");
        DynamicBuffer<BuildingDefenseAttackSlot> neutralSlots = em.GetBuffer<BuildingDefenseAttackSlot>(tower);
        Assert.AreEqual(Entity.Null, neutralSlots[0].Target);

        Entity hostileBuilding = CreateHealthTarget(em, FactionIdentity.EnemyFactionId, new float3(3f, 0f, 0f));

        world.SetTime(new TimeData(1.2d, 0.2f));
        defenseSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(citizenHouse).Current);
        Assert.AreEqual(85, em.GetComponentData<UnitHealth>(hostileBuilding).Current);
        DynamicBuffer<BuildingDefenseAttackSlot> hostileSlots = em.GetBuffer<BuildingDefenseAttackSlot>(tower);
        Assert.AreEqual(hostileBuilding, hostileSlots[0].Target);
    }

    [Test]
    public void FactionIdentity_CanAutoTargetForCombat_ExcludesNeutralAndAlliedFactions()
    {
        Assert.IsFalse(FactionIdentity.CanAutoTargetForCombat(FactionIdentity.PlayerFactionId, FactionIdentity.NeutralFactionId));
        Assert.IsFalse(FactionIdentity.CanAutoTargetForCombat(FactionIdentity.NeutralFactionId, FactionIdentity.EnemyFactionId));
        Assert.IsFalse(FactionIdentity.CanAutoTargetForCombat(FactionIdentity.PlayerFactionId, FactionIdentity.PlayerFactionId));
        Assert.IsTrue(FactionIdentity.CanAutoTargetForCombat(FactionIdentity.PlayerFactionId, FactionIdentity.EnemyFactionId));
    }

    private static Entity CreateGrid(EntityManager em)
    {
        Entity grid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(grid, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });
        return grid;
    }

    private static Entity CreateCombatUnit(EntityManager em, byte factionId, int2 cell, float attackRange)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat
        {
            CanAttack = 1,
            AutoEngage = 1,
            AggroRangeCells = 8,
            ChaseBreakDistance = 16f
        });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = attackRange,
            CooldownSeconds = 1f,
            Damage = 10,
            TraceVisibleSeconds = 0.1f
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static Entity CreateDefenseBuilding(EntityManager em, byte factionId, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(UnitAttackTraceComponent),
            typeof(BuildingDefenseWeapon));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitAttackTraceComponent());
        em.SetComponentData(entity, new BuildingDefenseWeapon
        {
            Range = 10f,
            CooldownSeconds = 0.01f,
            Damage = 15,
            MaxConcurrentAttacks = 1,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 100
        });
        em.AddBuffer<BuildingDefenseAttackSlot>(entity);
        return entity;
    }

    private static Entity CreateHealthTarget(EntityManager em, byte factionId, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
