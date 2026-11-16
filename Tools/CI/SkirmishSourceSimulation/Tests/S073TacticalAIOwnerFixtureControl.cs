using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Synthetic isolated exercise of the original tactical AI owner sequence.
// This fixture creates only the gameplay, control, squad-plan and unit data
// needed to observe squad -> target -> combat-order behavior.
internal static class S073TacticalAIOwnerFixtureControl
{
    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 tactical AI fixture: " + message);
    }

    public static int Main()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };

        var gameplay = em.CreateEntity();
        em.AddComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        var controlsEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        var controls = em.AddBuffer<FactionControlEntry>(controlsEntity);
        controls.Add(new FactionControlEntry { FactionId = 1, AIControlled = 0, IsPlayerFaction = 1 });
        controls.Add(new FactionControlEntry { FactionId = 2, AIControlled = 1, IsPlayerFaction = 0 });

        var planEntity = em.CreateEntity();
        em.AddComponentData(planEntity, new AISquadPlan {
            FactionId = 2, Enabled = 1, MinUnits = 1, MaxUnits = 2,
            MaxActiveSquads = 1, NextSquadId = 7, LastLogTime = -999f
        });

        var attacker = em.CreateEntity();
        em.AddComponentData(attacker, new Faction { Id = 2 });
        em.AddComponentData(attacker, new UnitGrid { Cell = new int2(2, 3) });
        em.AddComponentData(attacker, new UnitHealth { Current = 100, Max = 100 });
        em.AddComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.AddComponentData(attacker, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1f });
        em.AddComponentData(attacker, new UnitAttack { Damage = 10, Range = 1f });

        var enemy = em.CreateEntity();
        em.AddComponentData(enemy, new Faction { Id = 1 });
        em.AddComponentData(enemy, new UnitGrid { Cell = new int2(8, 3) });
        em.AddComponentData(enemy, new UnitHealth { Current = 100, Max = 100 });
        em.AddComponentData(enemy, new UnitAttack { Damage = 10, Range = 1f });

        var state = new SystemState { EntityManager = em, Enabled = true };
        state.WorldUnmanaged.Time = new TimeData { ElapsedTime = 10, DeltaTime = 0.25f };

        var factionControlOwner = new AIFactionControlSystem();
        state.Begin(); factionControlOwner.OnCreate(ref state); state.End();
        state.Begin(); factionControlOwner.OnUpdate(ref state); state.End();
        Need(em.HasComponent<AIControlledTag>(attacker),
            "original faction-control owner did not assign AI ownership from the synthetic faction-control buffer");

        var squadOwner = new AISquadSystem();
        state.Begin(); squadOwner.OnCreate(ref state); state.End();
        state.Begin(); squadOwner.OnUpdate(ref state); state.End();

        using (var squads = em.CreateEntityQuery(typeof(AISquad), typeof(AISquadUnit)))
        {
            Need(squads.CalculateEntityCount() == 1, "squad owner did not create exactly one squad");
            var squadEntity = squads.GetSingletonEntity();
            var squad = em.GetComponentData<AISquad>(squadEntity);
            var members = em.GetBuffer<AISquadUnit>(squadEntity);
            Need(squad.SquadId == 7 && squad.FactionId == 2 && squad.RallyCell.x == 2 && squad.RallyCell.y == 3,
                "squad identity, faction or rally cell differs");
            Need(members.Length == 1 && members[0].Unit == attacker &&
                 em.GetComponentData<AISquadPlan>(planEntity).NextSquadId == 8,
                "controlled synthetic unit was not assigned to the squad");
        }

        var targetingOwner = new AITargetingSystem();
        state.Begin(); targetingOwner.OnCreate(ref state); state.End();
        state.Begin(); targetingOwner.OnUpdate(ref state); state.End();
        using (var squads = em.CreateEntityQuery(typeof(AISquad), typeof(AISquadUnit)))
        {
            var squadEntity = squads.GetSingletonEntity();
            var squad = em.GetComponentData<AISquad>(squadEntity);
            Need(squad.TargetEntity == enemy && squad.TargetFactionId == 1 &&
                 squad.TargetKind == (byte)AITargetKind.Threat && squad.TargetCell.x == 8 && squad.TargetCell.y == 3,
                "original target owner did not select the synthetic enemy threat");
        }

        var combatOwner = new AICombatOrderSystem();
        state.Begin(); combatOwner.OnCreate(ref state); state.End();
        state.Begin(); combatOwner.OnUpdate(ref state); state.End();
        Need(em.HasComponent<EngageTarget>(attacker) && em.HasComponent<AICombatOrderTag>(attacker),
            "combat owner did not issue an AI engage order");
        var order = em.GetComponentData<EngageTarget>(attacker);
        Need(order.Target == enemy && order.Cell.x == 8 && order.Cell.y == 3 && order.IsCommanded != 0,
            "issued engage order does not match the selected target");

        Console.WriteLine("[S073TacticalAIOwnerFixtureControl] result=Passed scope=OriginalAIFactionControl_AISquad_AITargeting_AICombatOrder_SyntheticControlledInputs_NoNativeSchedulerOrMissionReadiness");
        return 0;
    }
}
