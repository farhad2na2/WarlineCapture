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
    public void AttackMove_AcquiresHostileBuildingButIgnoresNeutralDeadAndSuppressedBuildings()
    {
        using var world = new World("AttackMove building acquisition");
        var em = world.EntityManager; CreateGrid(em);
        var attacker = CreateCombatUnit(em, FactionIdentity.PlayerFactionId, new int2(10, 10), 8);
        var enemy = CreateDefenseBuilding(em, FactionIdentity.EnemyFactionId, new float3(15, 0, 10));
        CreateDefenseBuilding(em, FactionIdentity.NeutralFactionId, new float3(11, 0, 10));
        CreateDefenseBuilding(em, FactionIdentity.PlayerFactionId, new float3(11, 0, 11));
        var dead = CreateDefenseBuilding(em, FactionIdentity.EnemyFactionId, new float3(12, 0, 10));
        em.SetComponentData(dead, new UnitHealth { Current = 0, Max = 100 });
        var hidden = CreateDefenseBuilding(em, FactionIdentity.EnemyFactionId, new float3(13, 0, 10));
        em.AddComponent<CampaignMissionCombatSuppressedTag>(hidden);
        var end = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var engagement = world.CreateSystem<UnitEngagementSystem>();
        world.SetTime(new TimeData(1, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.IsFalse(em.HasComponent<EngageTarget>(attacker), "Ordinary idle acquisition is unchanged.");
        em.AddComponentData(attacker, new AttackMoveOrder { Destination = new int2(40, 40) });
        world.SetTime(new TimeData(2, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.AreEqual(enemy, em.GetComponentData<EngageTarget>(attacker).Target);
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(attacker));
    }

    [Test]
    public void AttackMove_InterruptsCommandedPathForHostileButPlainMoveDoesNot()
    {
        using var world = new World("AttackMove combat interruption");
        var em = world.EntityManager; CreateGrid(em);
        var attacker = CreateCombatUnit(em, FactionIdentity.PlayerFactionId, new int2(10, 10), 8);
        var enemy = CreateCombatUnit(em, FactionIdentity.EnemyFactionId, new int2(13, 10), 8);
        CreateCombatUnit(em, FactionIdentity.NeutralFactionId, new int2(11, 10), 8);
        em.AddComponent<ManualMoveOrderTag>(attacker);
        em.AddComponentData(attacker, new UnitPathRequest { Goal = new int2(40, 40) });
        var end = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var engagement = world.CreateSystem<UnitEngagementSystem>();
        world.SetTime(new TimeData(1, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.IsFalse(em.HasComponent<EngageTarget>(attacker), "Plain Move keeps moving past enemies.");
        em.AddComponentData(attacker, new AttackMoveOrder { Destination = new int2(40, 40) });
        world.SetTime(new TimeData(2, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.AreEqual(enemy, em.GetComponentData<EngageTarget>(attacker).Target);
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(attacker), "Combat interrupts travel.");
        Assert.IsTrue(em.HasComponent<AttackMoveOrder>(attacker), "Destination survives the fight.");
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(attacker), "Commanded pathfinding priority survives.");
    }

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
    public void UnitEngagementSystem_HoldAcquiresHostileAndHonorsSuppressionWithRealEcbSingleton()
    {
        using World world = new("AutomaticCombatFactionTargetingTests_HoldAcquisition");
        EntityManager em = world.EntityManager; CreateGrid(em);
        Entity attacker = CreateCombatUnit(em, FactionIdentity.PlayerFactionId, new int2(10,10), 8f);
        em.AddComponent<HoldPositionOrderTag>(attacker); em.AddComponent<ManualMoveOrderTag>(attacker);
        Entity hostile = CreateCombatUnit(em, FactionIdentity.EnemyFactionId, new int2(12,10), 8f);
        Entity suppressed = CreateCombatUnit(em, FactionIdentity.EnemyFactionId, new int2(11,10), 8f);
        em.AddComponent<CampaignMissionCombatSuppressedTag>(suppressed);
        EndSimulationEntityCommandBufferSystem endSimulation = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        SystemHandle engagement = world.CreateSystem<UnitEngagementSystem>();
        world.SetTime(new TimeData(1d,0.2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); endSimulation.Update();
        Assert.IsTrue(em.HasComponent<EngageTarget>(attacker), "Hold must actually acquire a target; enabled flags alone are insufficient.");
        Assert.AreEqual(hostile,em.GetComponentData<EngageTarget>(attacker).Target);
        Assert.IsFalse(em.HasComponent<EngageTarget>(suppressed), "A scheduled hidden convoy member cannot acquire targets early.");
        em.RemoveComponent<EngageTarget>(attacker);
        em.SetComponentData(hostile,LocalTransform.FromPosition(new float3(30.5f,0,10.5f)));
        world.SetTime(new TimeData(1.3d,0.2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); endSimulation.Update();
        Assert.IsFalse(em.HasComponent<EngageTarget>(attacker), "Hold cannot acquire a target outside its weapon range.");
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
