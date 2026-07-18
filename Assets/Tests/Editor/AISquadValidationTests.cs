using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Components;
using Game.Runtime;

public sealed class AISquadValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            InitialUnitsRuntimeState.VerboseAILogs = true;
            AssertGroupsIdleAIControlledUnitsIntoSquad(assertDiagnosticLog: false);
            UnityEngine.Debug.Log("[AISquadFocusedValidation] result=Passed tests=1");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[AISquadFocusedValidation] result=Failed");
            throw;
        }
        finally
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(false);
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
        RuntimeGameplayStateTestHelper.SetPlayRequested(false);
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [Test]
    public void AISquadSystem_GroupsIdleAIControlledUnitsIntoSquad()
    {
        AssertGroupsIdleAIControlledUnitsIntoSquad(assertDiagnosticLog: true);
    }

    private static void AssertGroupsIdleAIControlledUnitsIntoSquad(bool assertDiagnosticLog)
    {
        using var world = new World("AISquadValidationTests");
        EntityManager em = world.EntityManager;

        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId, AIControlled = 1 });

        Entity planEntity = em.CreateEntity(typeof(AISquadPlan));
        em.SetComponentData(planEntity, new AISquadPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            MinUnits = 3,
            MaxUnits = 4,
            MaxActiveSquads = 2,
            NextSquadId = 1,
            LastLogTime = -999f
        });

        Entity playerUnit = CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), false);
        Entity unitA = CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(4, 4), true);
        Entity unitB = CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(5, 4), true);
        Entity unitC = CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(6, 4), true);
        Entity unitD = CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(7, 4), true);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AISquadSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        if (assertDiagnosticLog)
            LogAssert.Expect(LogType.Log, new Regex(@"\[AISquad\] faction=2 squad=1 purpose=Attack units=4 targetFaction=1 targetCell=int2\(20, 20\)"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        EntityQuery squadQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>(), ComponentType.ReadOnly<AISquadUnit>());
        using NativeArray<Entity> squads = squadQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, squads.Length);

        AISquad squad = em.GetComponentData<AISquad>(squads[0]);
        Assert.AreEqual(1, squad.SquadId);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, squad.FactionId);
        Assert.AreEqual((byte)AISquadPurpose.Attack, squad.Purpose);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, squad.TargetFactionId);
        Assert.AreEqual(new int2(20, 20), squad.TargetCell);

        DynamicBuffer<AISquadUnit> members = em.GetBuffer<AISquadUnit>(squads[0]);
        Assert.AreEqual(4, members.Length);
        AssertMember(em, unitA, squads[0], 1);
        AssertMember(em, unitB, squads[0], 1);
        AssertMember(em, unitC, squads[0], 1);
        AssertMember(em, unitD, squads[0], 1);
        Assert.IsFalse(em.HasComponent<AISquadMember>(playerUnit));
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, int2 cell, bool aiControlled)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitHealth));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        if (aiControlled)
            em.AddComponent<AIControlledTag>(entity);
        return entity;
    }

    private static void AssertMember(EntityManager em, Entity unit, Entity squad, int squadId)
    {
        Assert.IsTrue(em.HasComponent<AISquadMember>(unit));
        AISquadMember member = em.GetComponentData<AISquadMember>(unit);
        Assert.AreEqual(squad, member.Squad);
        Assert.AreEqual(squadId, member.SquadId);
    }
}
