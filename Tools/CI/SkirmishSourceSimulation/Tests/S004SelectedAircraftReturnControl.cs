using System;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Synthetic ECS fixture for the original return service; no mission input or native parity.
internal static class S004SelectedAircraftReturnControl
{
    private static void Need(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static void Run(bool spawnPoint, bool paused, byte actingFaction, bool expected, int selectionMode = 1)
    {
        var em = new EntityManager(); int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var session = em.CreateEntity();
        em.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("fixture"), Phase = SkirmishSessionPhase.Playing });
        em.AddComponentData(session, new SkirmishObjectiveClockComponent { Paused = paused ? (byte)1 : (byte)0 });
        var actor = em.CreateEntity();
        em.AddComponentData(actor, new Faction { Id = 1 });
        em.AddComponentData(actor, new UnitHealth { Current = 100, Max = 100 });
        em.AddComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes("fixture"), StableObjectId = new FixedString64Bytes("actor"), FactionId = 1 });
        em.AddComponentData(actor, new UnitGrid { Cell = new int2(8, 9) });
        em.AddComponentData(actor, new UnitAirComponent { Airborne = 1, HomeInitialized = 1, HomeCell = new int2(2, 3), AttackRunActive = 1 });
        em.AddComponentData(actor, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.AddComponentData(actor, new SelectedUnitTag());
        em.AddComponentData(actor, new UnitAirMovement());
        em.AddComponentData(actor, new SkirmishUnitRoleComponent { Category = SkirmishPopulationCategory.Air });
        if (spawnPoint)
        {
            var boundary = em.CreateEntity(); em.AddComponentData(boundary, new BuildingRuntimeStateTag());
            var points = em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
            points.Add(new BuildingFactionProductionSpawnPointReadModel { FactionId = 1, BuildingRuntimeId = 17, SlotIndex = 0, Cell = new int2(2, 3) });
        }
        if (selectionMode == 0) em.RemoveComponent<SelectedUnitTag>(actor);
        if (selectionMode == 2) { var extra = em.CreateEntity(); em.AddComponentData(extra, new SelectedUnitTag()); }
        bool accepted = SourceS004SelectedAircraftReturn.TryIssue(em, session, actingFaction, out var returned);
        Need(!accepted || returned == actor, "accepted wrong actor");
        Need(accepted == expected, "owned return acceptance");
        var air = em.GetComponentData<UnitAirComponent>(actor);
        var combat = em.GetComponentData<UnitCombat>(actor);
        Need((air.ReturningHome != 0) == expected, "return state");
        Need(combat.AutoEngage == (expected ? 0 : 1), "combat return state");
    }
    private static void Main()
    {
        Run(true, false, 1, true);
        Run(false, false, 1, false);
        Run(true, true, 1, false);
        Run(true, false, 2, false);
        Run(true, false, 1, false, 0);
        Run(true, false, 1, false, 2);
        Console.WriteLine("[S004SelectedAircraftReturnControl] result=Passed cases=6 fixtureOnly=1 nativeInput=0 missionReady=0");
    }
}
