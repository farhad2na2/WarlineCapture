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
    public void AcquisitionAndRetaliationRespectAuthoredDomainsAndVisibility()
    {
        using var world = new World(nameof(AcquisitionAndRetaliationRespectAuthoredDomainsAndVisibility));
        var em = world.EntityManager; CreateGrid(em);
        var attacker = CreateCombatUnit(em, 1, new int2(10, 10), 8);
        var aircraft = CreateCombatUnit(em, 2, new int2(11, 10), 8);
        var ground = CreateCombatUnit(em, 2, new int2(13, 10), 8);
        em.AddComponentData(attacker, new CombatTargetPolicy
        { AllowedTargets = CombatTargetDomain.Ground, Domain = CombatTargetDomain.Infantry, Visible = 1 });
        em.AddComponentData(aircraft, new CombatTargetPolicy { Domain = CombatTargetDomain.Air, Visible = 1 });
        em.AddComponentData(ground, new CombatTargetPolicy { Domain = CombatTargetDomain.Ground, Visible = 1 });
        em.RemoveComponent<UnitAttack>(ground);
        em.RemoveComponent<UnitCombat>(ground); // An unarmed logistics vehicle remains a valid hostile target.
        var end = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var engagement = world.CreateSystem<UnitEngagementSystem>();
        world.SetTime(new TimeData(1, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.AreEqual(ground, em.GetComponentData<EngageTarget>(attacker).Target);
        em.RemoveComponent<EngageTarget>(attacker);
        em.SetComponentData(ground, new CombatTargetPolicy { Domain = CombatTargetDomain.Ground, Visible = 0 });
        em.AddComponentData(attacker, new RecentAttacker { Attacker = aircraft });
        world.SetTime(new TimeData(2, .2f)); engagement.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs(); end.Update();
        Assert.IsFalse(em.HasComponent<EngageTarget>(attacker));
    }

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

    // Diagnostic characterization of the observed S005 completion/acquisition seam.
    // This isolated world does not reproduce Campaign geometry, fog, or live damage.
    public static void RunS005RocketeerAcquisitionSeamValidation()
    {
        var suite = new AutomaticCombatFactionTargetingTests();
        suite.S005RocketeerObservedArrivalAcquisitionMatrix();
        UnityEngine.Debug.Log("[S005RocketeerAcquisitionSeam] result=Collected controls=8 nativeGeometry=0 missionReady=0");
    }

    [Test]
    public void S005RocketeerObservedArrivalAcquisitionMatrix()
    {
        var config = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.UnitGridAuthoringPrefabConfigAsset>(
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset");
        Assert.IsNotNull(config);
        Assert.AreEqual(16, config.AggroRangeCells);
        Assert.AreEqual(new UnityEngine.Vector2Int(1, 1), config.FootprintCells);
        Assert.IsTrue(Game.Configs.SkirmishRoleOverlayCatalog.TryGet(
            Game.Configs.SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
            Game.Skirmish.Contracts.SkirmishRoleKind.Rocketeer, out var overlay));
        Assert.AreEqual(24f, overlay.RangeWorld);
        Assert.AreEqual(22, overlay.Damage);
        var origins = new[]
        {
            new float3(1341.5f, 0, 681.5f), // production.1002.0: observed idle
            new float3(1344.5f, 0, 678.5f), // production.1002.1: observed idle
            new float3(1344.5f, 0, 681.5f), // production.1002.3: observed idle
            new float3(1347.5f, 0, 681.5f)  // production.1002.2: observed engaged
        };
        for (int i = 0; i < origins.Length; i++)
            S005ExerciseArrival(config, overlay, origins[i], completeFirst: false,
                expectedAcquire: i == 3, "observed-" + i);
        for (int i = 0; i < 3; i++)
            S005ExerciseArrival(config, overlay, origins[i], completeFirst: true,
                expectedAcquire: false, "completed-outside-" + i);
        S005ExerciseArrival(config, overlay, origins[3], completeFirst: true,
            expectedAcquire: false, "completed-inside");
    }

    private static void S005ExerciseArrival(
        Game.Configs.UnitGridAuthoringPrefabConfigAsset config,
        Game.Skirmish.Contracts.SkirmishRoleOverlay overlay,
        float3 origin, bool completeFirst, bool expectedAcquire, string label)
    {
        using var world = new World("S005 rocketeer seam " + label);
        var em = world.EntityManager;
        var grid = CreateGrid(em);
        em.SetComponentData(grid, new GridConfig
        { Width = 2048, Height = 1024, CellSize = 1f, Origin = float3.zero });
        var attacker = CreateCombatUnit(em, FactionIdentity.PlayerFactionId,
            new int2((int)math.floor(origin.x), (int)math.floor(origin.z)), overlay.RangeWorld);
        em.SetComponentData(attacker, LocalTransform.FromPosition(origin));
        em.SetComponentData(attacker, new UnitCombat
        { CanAttack = 1, AutoEngage = 1, AggroRangeCells = config.AggroRangeCells,
          ChaseBreakDistance = config.ChaseBreakDistance });
        Game.Runtime.SkirmishSharedCombatBinding.Bind(em, attacker, overlay);
        Assert.AreEqual(overlay.RangeWorld, em.GetComponentData<UnitAttack>(attacker).Range);
        var target = CreateDefenseBuilding(em, FactionIdentity.EnemyFactionId,
            new float3(1370f, 0, 679.5f));
        em.AddComponentData(target, new CombatTargetPolicy
        { Domain = CombatTargetDomain.Structure, Visible = 1 });
        em.AddComponentData(attacker, new AttackMoveOrder
        { Position = origin, Destination = new int2((int)math.floor(origin.x), (int)math.floor(origin.z)) });
        if (completeFirst)
        {
            AttackMoveSystem.ResumeTravel(em, 1);
            Assert.IsFalse(em.HasComponent<AttackMoveOrder>(attacker));
        }
        var end = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        var acquisition = world.CreateSystem<UnitEngagementSystem>();
        world.SetTime(new TimeData(1, .2f));
        acquisition.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();
        end.Update();
        Assert.AreEqual(expectedAcquire, em.HasComponent<EngageTarget>(attacker), label);
        if (expectedAcquire) Assert.AreEqual(target, em.GetComponentData<EngageTarget>(attacker).Target);
        UnityEngine.Debug.Log("[S005RocketeerAcquisitionCase] label=" + label +
            " distance=" + math.distance(origin.xz, new float2(1370f, 679.5f)) +
            " completedFirst=" + completeFirst + " acquired=" + expectedAcquire);
    }
}
