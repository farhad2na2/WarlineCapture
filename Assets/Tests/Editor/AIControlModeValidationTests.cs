using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIControlModeValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new AIControlModeValidationTests();
            tests.SetUp();
            try
            {
                RunTagsControlledFactionsAndClearsManualOrders(expectLogs: false);
            }
            finally
            {
                tests.TearDown();
            }

            Debug.Log("[AIControlModeFocusedValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AIControlModeFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
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
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [Test]
    public void AIFactionControlSystem_TagsControlledFactionsAndClearsManualOrders()
    {
        RunTagsControlledFactionsAndClearsManualOrders(expectLogs: true);
    }

    private static void RunTagsControlledFactionsAndClearsManualOrders(bool expectLogs)
    {
        using var world = new World("AIControlModeValidationTests");
        EntityManager em = world.EntityManager;

        Entity configEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(configEntity);
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.PlayerFactionId, AIControlled = 1, IsPlayerFaction = 1, LastLogTime = -999f });
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId, AIControlled = 1, IsPlayerFaction = 0, LastLogTime = -999f });
        controls.Add(new FactionControlEntry { FactionId = 3, AIControlled = 0, IsPlayerFaction = 0, LastLogTime = -999f });

        Entity playerAutoUnit = CreateFactionUnit(em, FactionIdentity.PlayerFactionId);
        em.AddComponent<ManualMoveOrderTag>(playerAutoUnit);
        em.AddComponent<ManualMoveGroupMemberTag>(playerAutoUnit);
        em.AddComponentData(playerAutoUnit, new UnitPathRequest { Goal = new int2(4, 4) });
        em.AddComponentData(playerAutoUnit, new UnitPathRetryCooldown { ResumeFrame = 10 });
        em.AddComponentData(playerAutoUnit, new EngageTarget { IsCommanded = 1 });

        Entity enemyUnit = CreateFactionUnit(em, FactionIdentity.EnemyFactionId);
        Entity manualUnit = CreateFactionUnit(em, 3);
        em.AddComponent<AIControlledTag>(manualUnit);
        em.AddComponent<AICombatOrderTag>(manualUnit);
        em.AddComponentData(manualUnit, new EngageTarget { IsCommanded = 1 });
        em.AddComponentData(manualUnit, new UnitPathRequest { Goal = new int2(8, 8) });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AIFactionControlSystem>();
        SystemHandle logFlushSystem = expectLogs ? world.CreateSystem<AIDiagnosticLogFlushSystem>() : default;

        if (expectLogs)
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=1 mode=Auto controlledUnits=1 controlledBuildings=0"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=2 mode=Auto controlledUnits=1 controlledBuildings=0"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[AIControlMode\] faction=3 mode=Manual controlledUnits=1 controlledBuildings=0"));
        }

        system.Update(world.Unmanaged);
        if (expectLogs)
        {
            logFlushSystem.Update(world.Unmanaged);
            LogAssert.NoUnexpectedReceived();
        }

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
