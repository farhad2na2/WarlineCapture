using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIControlModeValidationTests
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
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [Test]
    public void AIFactionControlSystem_TagsControlledFactionsAndClearsManualOrders()
    {
        using var world = new World("AIControlModeValidationTests");
        EntityManager em = world.EntityManager;

        Entity configEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(configEntity);
        controls.Add(new FactionControlEntry { FactionId = 0, AIControlled = 1, IsPlayerFaction = 1, LastLogTime = -999f });
        controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 1, IsPlayerFaction = 0, LastLogTime = -999f });
        controls.Add(new FactionControlEntry { FactionId = 2, AIControlled = 0, IsPlayerFaction = 0, LastLogTime = -999f });

        Entity playerAutoUnit = CreateFactionUnit(em, 0);
        em.AddComponent<ManualMoveOrderTag>(playerAutoUnit);
        em.AddComponent<ManualMoveGroupMemberTag>(playerAutoUnit);
        em.AddComponentData(playerAutoUnit, new UnitPathRequest { Goal = new int2(4, 4) });
        em.AddComponentData(playerAutoUnit, new UnitPathRetryCooldown { ResumeFrame = 10 });
        em.AddComponentData(playerAutoUnit, new EngageTarget { IsCommanded = 1 });

        Entity enemyUnit = CreateFactionUnit(em, 1);
        Entity manualUnit = CreateFactionUnit(em, 2);
        em.AddComponent<AIControlledTag>(manualUnit);
        em.AddComponent<AICombatOrderTag>(manualUnit);
        em.AddComponentData(manualUnit, new EngageTarget { IsCommanded = 1 });
        em.AddComponentData(manualUnit, new UnitPathRequest { Goal = new int2(8, 8) });

        InitialUnitsRuntimeState.PlayRequested = true;
        SystemHandle system = world.CreateSystem<AIFactionControlSystem>();

        LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=0 mode=Auto controlledUnits=1 controlledBuildings=0"));
        LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=1 mode=Auto controlledUnits=1 controlledBuildings=0"));
        LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=2 mode=Manual controlledUnits=1 controlledBuildings=0"));

        system.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        Assert.IsTrue(em.HasComponent<AIControlledTag>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<ManualControlledTag>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<UnitPathRetryCooldown>(playerAutoUnit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(playerAutoUnit));

        Assert.IsTrue(em.HasComponent<AIControlledTag>(enemyUnit));
        Assert.IsTrue(em.HasComponent<ManualControlledTag>(manualUnit));
        Assert.IsFalse(em.HasComponent<AIControlledTag>(manualUnit));
        Assert.IsFalse(em.HasComponent<AICombatOrderTag>(manualUnit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(manualUnit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(manualUnit));
    }

    private static Entity CreateFactionUnit(EntityManager em, byte factionId)
    {
        Entity entity = em.CreateEntity(typeof(Faction), typeof(UnitGrid));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        return entity;
    }
}
