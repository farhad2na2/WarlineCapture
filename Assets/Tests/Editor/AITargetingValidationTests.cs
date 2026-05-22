using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AITargetingValidationTests
{
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
        using var world = new World("AITargetingValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 7,
            FactionId = 1,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetFactionId = 0,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            TargetCell = int2.zero,
            TargetScore = 0,
            MinUnits = 3,
            MaxUnits = 8,
            LastLogTime = -999f
        });

        Entity lowValueUnit = CreateTarget(em, 0, new int2(11, 10), 100, false, false);
        Entity highValueThreat = CreateTarget(em, 0, new int2(18, 10), 500, true, true);
        CreateTarget(em, 1, new int2(9, 10), 500, true, true);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=1 squad=7 target=Threat score=\d+ reason=Threat targetFaction=0 targetCell=int2\(18, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(highValueThreat, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Threat, squad.TargetKind);
        Assert.AreEqual(new int2(18, 10), squad.TargetCell);
        Assert.AreEqual(0, squad.TargetFactionId);
        Assert.Greater(squad.TargetScore, 0);
        Assert.AreNotEqual(lowValueUnit, squad.TargetEntity);
    }

    [Test]
    public void AITargetingSystem_EconomyPriorityPrefersResourceHauler()
    {
        using var world = new World("AITargetingPriorityValidationTests");
        EntityManager em = world.EntityManager;

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 8,
            FactionId = 1,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetKind = (byte)AITargetKind.None,
            TargetEntity = Entity.Null,
            RallyCell = new int2(10, 10),
            LastLogTime = -999f
        });
        Entity priority = em.CreateEntity(typeof(AITargetPrioritySetting));
        em.SetComponentData(priority, new AITargetPrioritySetting { FactionId = 1, Priority = (byte)AITargetPriority.Economy });

        Entity threat = CreateTarget(em, 0, new int2(11, 10), 500, true, false);
        Entity hauler = CreateTarget(em, 0, new int2(12, 10), 100, false, false);
        em.AddComponentData(hauler, new UnitResourceHauler { BarrelCapacity = 8 });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AITargetingSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        LogAssert.Expect(LogType.Log, new Regex(@"\[AITarget\] faction=1 squad=8 target=Unit score=\d+ reason=Economy targetFaction=0 targetCell=int2\(12, 10\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.AreEqual(hauler, squad.TargetEntity);
        Assert.AreNotEqual(threat, squad.TargetEntity);
        Assert.AreEqual((byte)AITargetKind.Unit, squad.TargetKind);
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
