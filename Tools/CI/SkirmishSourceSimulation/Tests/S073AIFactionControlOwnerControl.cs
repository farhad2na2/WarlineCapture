using System;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073AIFactionControlOwnerControl
{
    private static void Need(bool value, string detail)
    {
        if (!value) throw new InvalidOperationException(detail);
    }

    private static void Main()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        var controlsEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        var controls = em.AddBuffer<FactionControlEntry>(controlsEntity);
        controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 0, IsPlayerFaction = 1 });
        controls.Add(new FactionControlEntry { FactionId = 2, AIControlled = 1, IsPlayerFaction = 0 });

        var gameplay = em.CreateEntity();
        em.AddComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        var diagnostics = em.CreateEntity();
        em.AddComponentData(diagnostics, new RuntimeDiagnosticsStateComponent { VerboseAILogs = 1 });

        var manualUnit = em.CreateEntity();
        em.AddComponentData(manualUnit, new Faction { Id = 1 });
        em.AddComponentData(manualUnit, new UnitGrid { Cell = new int2(3, 4) });
        em.AddComponentData(manualUnit, new AIControlledTag());
        em.AddComponentData(manualUnit, new AICombatOrderTag());
        em.AddComponentData(manualUnit, new EngageTarget { IsCommanded = 1 });
        em.AddComponentData(manualUnit, new UnitPathRequest());
        em.AddComponentData(manualUnit, new UnitPathRetryCooldown());
        em.AddComponentData(manualUnit, new UnitPathFollow());
        em.AddComponentData(manualUnit, new UnitPathRange());
        em.AddComponentData(manualUnit, new AutoWanderMoveTag());

        var aiUnit = em.CreateEntity();
        em.AddComponentData(aiUnit, new Faction { Id = 2 });
        em.AddComponentData(aiUnit, new UnitGrid { Cell = new int2(7, 8) });
        em.AddComponentData(aiUnit, new ManualControlledTag());
        em.AddComponentData(aiUnit, new ManualMoveOrderTag());
        em.AddComponentData(aiUnit, new ManualMoveGroupMemberTag());
        em.AddComponentData(aiUnit, new UnitPathRequest());
        em.AddComponentData(aiUnit, new UnitPathRetryCooldown());
        em.AddComponentData(aiUnit, new EngageTarget { IsCommanded = 1 });

        var blocker = em.CreateEntity();
        em.AddComponentData(blocker, new Faction { Id = 2 });
        em.AddComponentData(blocker, new UnitGrid { Cell = new int2(9, 10) });
        em.AddComponentData(blocker, new StaticGridBlocker());

        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 11, DeltaTime = 0.5f };
        var state = new SystemState { EntityManager = em, Enabled = true };
        var system = new AIFactionControlSystem();
        state.Begin(); system.OnCreate(ref state); state.End();
        Need(state.RequiredQueriesPresent, "Original owner prerequisites missing");
        state.Begin(); system.OnUpdate(ref state); state.End();

        Need(em.HasComponent<ManualControlledTag>(manualUnit) && !em.HasComponent<AIControlledTag>(manualUnit),
            "Manual faction ownership tags differ from original owner");
        Need(!em.HasComponent<AICombatOrderTag>(manualUnit) && !em.HasComponent<EngageTarget>(manualUnit) &&
             !em.HasComponent<UnitPathRequest>(manualUnit) &&
             !em.HasComponent<UnitPathFollow>(manualUnit) && !em.HasComponent<UnitPathRange>(manualUnit) &&
             !em.HasComponent<AutoWanderMoveTag>(manualUnit), "Manual transition did not clear AI-owned state");

        Need(em.HasComponent<AIControlledTag>(aiUnit) && !em.HasComponent<ManualControlledTag>(aiUnit) &&
             !em.HasComponent<ManualMoveOrderTag>(aiUnit) && !em.HasComponent<ManualMoveGroupMemberTag>(aiUnit) &&
             !em.HasComponent<UnitPathRequest>(aiUnit) && !em.HasComponent<UnitPathRetryCooldown>(aiUnit) &&
             !em.HasComponent<EngageTarget>(aiUnit), "AI transition did not clear commanded/manual state");
        Need(em.HasComponent<AIControlledTag>(blocker), "Faction control must still apply to static blockers");

        using var queueQuery = em.CreateEntityQuery(typeof(AIDiagnosticLogQueueComponent), typeof(AIDiagnosticLogComponent));
        Need(queueQuery.CalculateEntityCount() == 1, "Original diagnostics queue was not created");
        var logs = em.GetBuffer<AIDiagnosticLogComponent>(queueQuery.GetSingletonEntity());
        var text = logs.Select(row => row.Message.ToString()).ToArray();
        Need(text.Length == 2 && text.Any(row => row.Contains("faction=1 mode=Manual controlledUnits=1")) &&
             text.Any(row => row.Contains("faction=2 mode=Auto controlledUnits=1")),
            "Original job controlled-unit counts or diagnostic ownership differ");

        Console.WriteLine("[S073AIFactionControlOwnerControl] result=Passed scope=OriginalSystemAndIJobEntity_BoundedSurrogateFixture_NoSchedulerOrMissionReadiness");
    }
}
