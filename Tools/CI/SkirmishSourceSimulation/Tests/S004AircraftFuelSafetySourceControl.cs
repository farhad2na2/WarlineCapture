using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

// Isolated source-host fixture: exercises the original owner, not a mission or native parity.
internal static class S004AircraftFuelSafetySourceControl
{
    private static void Need(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Run(float fuel, bool alreadyReturning, bool enabled, bool expectReturn)
    {
        var em = new EntityManager(); int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var storage = em.CreateEntity();
        em.AddComponentData(storage, new BuildingResourceStorageComponent { OwnerFactionId = 1, FuelStorageCapacity = 100, StoredFuelBarrels = fuel });
        var aircraft = em.CreateEntity();
        em.AddComponentData(aircraft, new Faction { Id = 1 });
        em.AddComponentData(aircraft, new UnitAirMovement());
        em.AddComponentData(aircraft, new UnitFuelConsumption { Enabled = enabled ? (byte)1 : (byte)0, AirFuelPerCell = 1 });
        em.AddComponentData(aircraft, new UnitAirComponent { Airborne = 1, ReturningHome = alreadyReturning ? (byte)1 : (byte)0, AttackRunActive = 1, ReturnApproachInitialized = 1 });
        em.AddComponentData(aircraft, new UnitTarget());
        var owner = new AircraftFuelSafetyReturnSystem();
        var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); try { owner.OnCreate(ref state); } finally { state.End(); }
        state.Begin(); try { if (state.RequiredQueriesPresent) owner.OnUpdate(ref state); else throw new Exception("required source queries unavailable"); } finally { state.End(); }
        var actual = em.GetComponentData<UnitAirComponent>(aircraft);
        Need((actual.ReturningHome != 0) == expectReturn, "return state");
        bool shouldClear = enabled && fuel <= 0.001f;
        Need(em.HasComponent<UnitTarget>(aircraft) != shouldClear, "target removal");
        if (shouldClear && !alreadyReturning) Need(actual.AttackRunActive == 0 && actual.ReturnApproachInitialized == 0, "initial return transition");
        if (shouldClear && alreadyReturning) Need(actual.AttackRunActive == 1 && actual.ReturnApproachInitialized == 1, "existing return preserved");
    }
    private static void Main()
    {
        Run(0, false, true, true);
        Run(10, false, true, false);
        Run(0, true, true, true);
        Run(0, false, false, false);
        Console.WriteLine("[S004AircraftFuelSafetySourceControl] result=Passed cases=4 fixtureOnly=1 nativeParity=Unknown missionReady=0");
    }
}
