using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Components;
using Game.Configs;
using Game.Runtime;

public sealed class AITargetingValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            InitialUnitsRuntimeState.VerboseAILogs = true;
            AssertHighestScoredEnemyTargetToSquad(assertDiagnosticLog: false);
            AssertEconomyPriorityPrefersResourceHauler(assertDiagnosticLog: false);
            AssertProductionPriorityPrefersEnemyBuilding(assertDiagnosticLog: false);
            AssertEconomyPriorityPrefersFuelLogisticsInfrastructure(assertDiagnosticLog: false);
            UnityEngine.Debug.Log("[AITargetingFocusedValidation] result=Passed tests=4");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[AITargetingFocusedValidation] result=Failed");
            throw;
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            InitialUnitsRuntimeState.VerboseAILogs = false;
        }
    }

    [SetUp]
    public void SetUp()
    {
        InitialUnitsRuntimeState.VerboseAILogs = true;
    }

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [Test]
    public void AITargetingSystem_AssignsHighestScoredEnemyTargetToSquad()
    {
        AssertHighestScoredEnemyTargetToSquad(assertDiagnosticLog: true);
    }

    [Test]
    public void AITargetingSystem_EconomyPriorityPrefersResourceHauler()
    {
        AssertEconomyPriorityPrefersResourceHauler(assertDiagnosticLog: true);
    }

    [Test]
    public void AITargetingSystem_ProductionPriorityPrefersEnemyBuilding()
    {
        AssertProductionPriorityPrefersEnemyBuilding(assertDiagnosticLog: true);
    }

    [Test]
    public void AITargetingSystem_EconomyPriorityPrefersFuelLogisticsInfrastructure()
    {
        AssertEconomyPriorityPrefersFuelLogisticsInfrastructure(assertDiagnosticLog: true);
    }

    private static void AssertHighestScoredEnemyTargetToSquad(bool assertDiagnosticLog)
    {
        using var world = new World("AITargetingValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 7,
            FactionId = FactionIdentity.EnemyFactionId,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetFactionId = FactionIdentity.PlayerFactionId,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            TargetCell = int2.zero,
            TargetScore = 0,
            MinUnits = 3,
            MaxUnits = 8,
            LastLogTime = -999f
        });

        Entity lowValueUnit = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(11, 10), 100, false, false);
        Entity highValueThreat = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(18, 10), 500, true, true);
        CreateTarget(em, FactionIdentity.EnemyFactionId, new int2(9, 10), 500, true, true);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=2 squad=7 target=Threat score=\d+ reason=Threat targetFaction=1 targetCell=int2\(18, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(highValueThreat, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Threat, squad.TargetKind);
        Assert.AreEqual(new int2(18, 10), squad.TargetCell);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, squad.TargetFactionId);
        Assert.Greater(squad.TargetScore, 0);
        Assert.AreNotEqual(lowValueUnit, squad.TargetEntity);
    }

    private static void AssertEconomyPriorityPrefersResourceHauler(bool assertDiagnosticLog)
    {
        using var world = new World("AITargetingPriorityValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 8,
            FactionId = FactionIdentity.EnemyFactionId,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            LastLogTime = -999f
        });
        Entity priority = em.CreateEntity(typeof(AITargetPrioritySetting));
        em.SetComponentData(priority, new AITargetPrioritySetting { FactionId = FactionIdentity.EnemyFactionId, Priority = (byte)AITargetPriority.Economy });

        Entity threat = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(11, 10), 500, true, false);
        Entity hauler = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(12, 10), 100, false, false);
        em.AddComponentData(hauler, new UnitResourceHauler { BarrelCapacity = 8 });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=2 squad=8 target=Unit score=\d+ reason=Economy targetFaction=1 targetCell=int2\(12, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(hauler, squad.TargetEntity);
        Assert.AreNotEqual(threat, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Unit, squad.TargetKind);
    }

    private static void AssertProductionPriorityPrefersEnemyBuilding(bool assertDiagnosticLog)
    {
        using var world = new World("AITargetingBuildingValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 9,
            FactionId = FactionIdentity.EnemyFactionId,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            LastLogTime = -999f
        });
        Entity priority = em.CreateEntity(typeof(AITargetPrioritySetting));
        em.SetComponentData(priority, new AITargetPrioritySetting { FactionId = FactionIdentity.EnemyFactionId, Priority = (byte)AITargetPriority.Production });

        Entity threat = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(11, 10), 500, true, false);
        Entity building = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(12, 10), 100, false, true);
        CreateTarget(em, FactionIdentity.EnemyFactionId, new int2(8, 10), 1000, false, true);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=2 squad=9 target=Building score=\d+ reason=Production targetFaction=1 targetCell=int2\(12, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(building, squad.TargetEntity);
        Assert.AreNotEqual(threat, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Building, squad.TargetKind);
        Assert.AreEqual(new int2(12, 10), squad.TargetCell);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, squad.TargetFactionId);
    }

    private static void AssertEconomyPriorityPrefersFuelLogisticsInfrastructure(bool assertDiagnosticLog)
    {
        using var world = new World("AITargetingFuelLogisticsValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 10,
            FactionId = FactionIdentity.EnemyFactionId,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            LastLogTime = -999f
        });
        Entity priority = em.CreateEntity(typeof(AITargetPrioritySetting));
        em.SetComponentData(priority, new AITargetPrioritySetting { FactionId = FactionIdentity.EnemyFactionId, Priority = (byte)AITargetPriority.Economy });

        Entity genericBuilding = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(11, 10), 200, false, true);
        Entity refinery = CreateTarget(em, FactionIdentity.PlayerFactionId, new int2(16, 10), 100, false, true);
        em.AddComponent<FuelLogisticsRefineryInputTag>(refinery);
        em.AddComponent<FuelLogisticsRefineryOutputTag>(refinery);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=2 squad=10 target=Building score=\d+ reason=Economy targetFaction=1 targetCell=int2\(16, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(refinery, squad.TargetEntity);
        Assert.AreNotEqual(genericBuilding, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Building, squad.TargetKind);
        Assert.AreEqual(new int2(16, 10), squad.TargetCell);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, squad.TargetFactionId);
    }

    private static Entity CreateTarget(EntityManager em, byte factionId, int2 cell, int maxHealth, bool attackCapable, bool building)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitHealth));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = maxHealth, Max = maxHealth });

        if (attackCapable)
        {
            em.AddComponentData(entity, new UnitAttack { Damage = 10, CooldownSeconds = 1f, Range = 3f });
            em.AddComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        }

        if (building)
            em.AddComponentData(entity, new StaticGridBlocker());

        return entity;
    }
}
