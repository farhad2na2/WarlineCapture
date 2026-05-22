using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AICombatOrderValidationTests
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
    public void AICombatOrderSystem_IssuesCommandedEngageOrdersToSquadMembers()
    {
        using var world = new World("AICombatOrderValidationTests");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, 0, new int2(20, 20), new float3(20f, 0f, 20f));
        Entity memberA = CreateAttacker(em, 1, new int2(5, 5), new float3(5f, 0f, 5f));
        Entity memberB = CreateAttacker(em, 1, new int2(6, 5), new float3(6f, 0f, 5f));
        em.AddComponentData(memberA, new UnitPathRequest { Goal = new int2(9, 9) });
        em.AddComponent<ManualMoveOrderTag>(memberA);
        em.AddComponent<AutoWanderMoveTag>(memberB);

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 2,
            FactionId = 1,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetFactionId = 0,
            TargetKind = (byte)AITargetKind.Threat,
            TargetEntity = target,
            RallyCell = new int2(5, 5),
            TargetCell = new int2(20, 20),
            TargetScore = 150,
            MinUnits = 2,
            MaxUnits = 4,
            LastOrderTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AISquadUnit> members = em.AddBuffer<AISquadUnit>(squadEntity);
        members.Add(new AISquadUnit { Unit = memberA });
        members.Add(new AISquadUnit { Unit = memberB });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AICombatOrderSystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        LogAssert.Expect(LogType.Log, new Regex(@"\[AICombat\] faction=1 squad=2 order=Attack target=Entity\(\d+:\d+\) units=2"));
        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        AssertEngageOrder(em, memberA, target, new int2(20, 20), new float3(20f, 0f, 20f));
        AssertEngageOrder(em, memberB, target, new int2(20, 20), new float3(20f, 0f, 20f));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(memberA));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(memberA));
        Assert.IsTrue(em.HasComponent<AICombatOrderTag>(memberA));
        Assert.IsTrue(em.HasComponent<AICombatOrderTag>(memberB));
        Assert.IsFalse(em.HasComponent<AutoWanderMoveTag>(memberB));

        AISquad squad = em.GetComponentData<AISquad>(squadEntity);
        Assert.Greater(squad.LastOrderTime, -1f);
    }

    [Test]
    public void AICombatOrderSystem_DoesNotIssueOrdersForManualPlayerFaction()
    {
        using var world = new World("AICombatOrderManualModeValidationTests");
        EntityManager em = world.EntityManager;

        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = 0, AIControlled = 0, IsPlayerFaction = 1 });

        Entity target = CreateTarget(em, 1, new int2(20, 20), new float3(20f, 0f, 20f));
        Entity playerMember = CreateAttacker(em, 0, new int2(5, 5), new float3(5f, 0f, 5f));

        Entity squadEntity = em.CreateEntity(typeof(AISquad));
        em.SetComponentData(squadEntity, new AISquad
        {
            SquadId = 3,
            FactionId = 0,
            Purpose = (byte)AISquadPurpose.Attack,
            TargetFactionId = 1,
            TargetKind = (byte)AITargetKind.Threat,
            TargetEntity = target,
            RallyCell = new int2(5, 5),
            TargetCell = new int2(20, 20),
            TargetScore = 150,
            MinUnits = 1,
            MaxUnits = 4,
            LastOrderTime = -999f,
            LastLogTime = -999f
        });
        DynamicBuffer<AISquadUnit> members = em.AddBuffer<AISquadUnit>(squadEntity);
        members.Add(new AISquadUnit { Unit = playerMember });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AICombatOrderSystem>();

        system.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        Assert.IsFalse(em.HasComponent<EngageTarget>(playerMember));
        Assert.IsFalse(em.HasComponent<AICombatOrderTag>(playerMember));
    }

    private static Entity CreateAttacker(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(AIControlledTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(entity, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateTarget(EntityManager em, byte factionId, int2 cell, float3 position)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitHealth), typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static void AssertEngageOrder(EntityManager em, Entity member, Entity target, int2 cell, float3 position)
    {
        Assert.IsTrue(em.HasComponent<EngageTarget>(member));
        EngageTarget order = em.GetComponentData<EngageTarget>(member);
        Assert.AreEqual(target, order.Target);
        Assert.AreEqual(cell, order.Cell);
        Assert.AreEqual(1, order.IsCommanded);
        Assert.AreEqual(position.x, order.Position.x, 0.001f);
        Assert.AreEqual(position.z, order.Position.z, 0.001f);
    }
}
