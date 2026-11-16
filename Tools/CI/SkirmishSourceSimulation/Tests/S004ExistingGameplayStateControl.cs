using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class S004ExistingGameplayStateControl
{
    private static void Main()
    {
        var manager = new EntityManager();
        int next = 1;
        manager.LogicalAllocatorAdmitted = true;
        manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        void Rejected(string label) {
            try { AutonomousWorld.RequireExistingActiveSourceGameplay(manager); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Unexpected admission: " + label);
        }
        Rejected("missing");
        Entity first = manager.CreateEntity();
        manager.AddComponentData(first, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 0 });
        Rejected("inactive");
        manager.SetComponentData(first, new RuntimeGameplayStateComponent { PlayRequested = 0, SimulationActive = 1 });
        Rejected("not requested");
        manager.SetComponentData(first, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        AutonomousWorld.RequireExistingActiveSourceGameplay(manager);
        var state = manager.GetComponentData<RuntimeGameplayStateComponent>(first);
        if (state.PlayRequested != 1 || state.SimulationActive != 1) throw new Exception("Admission mutated state");
        Entity duplicate = manager.CreateEntity();
        manager.AddComponentData(duplicate, state);
        Rejected("duplicate");
        AutonomousWorld.RequireValidSourceDaySeconds(300f);
        foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
            bool rejected = false;
            try { AutonomousWorld.RequireValidSourceDaySeconds(value); } catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid day duration accepted: " + value);
        }
        Console.WriteLine("[S004ExistingGameplayStateControl] result=Passed missing=Rejected inactive=Rejected notRequested=Rejected duplicate=Rejected active=Accepted invalidDurations=5 stateMutated=0 fixtureOnly=1");
    }
}
