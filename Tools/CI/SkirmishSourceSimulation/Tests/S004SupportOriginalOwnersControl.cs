using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class S004SupportOriginalOwnersControl
{
    private static void Main()
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var root = em.CreateEntity();
        em.AddComponentData(root, new SupportSessionComponent { SessionToken = "current", AttemptOrdinal = 2, LastPlayerRequestId = 7 });
        em.AddBuffer<SupportRequestElement>(root).Add(new SupportRequestElement { SessionToken = "stale", AttemptOrdinal = 1, RequestId = 8 });
        var requestOwner = new SupportAbilityRequestSystem();
        var requestState = new SystemState { EntityManager = em, Enabled = true };
        requestState.Begin(); requestOwner.OnCreate(ref requestState); requestState.End();
        requestState.Begin(); requestOwner.OnUpdate(ref requestState); requestState.End();
        if (em.GetBuffer<SupportRequestElement>(root).Length != 0 || em.GetComponentData<SupportSessionComponent>(root).LastPlayerRequestId != 7 ||
            em.HasBuffer<SupportReceiptElement>(root)) throw new InvalidOperationException("Stale request changed active support state");
        var orphan = em.CreateEntity();
        em.AddComponentData(orphan, new SupportFlightComponent { Root = Entity.Null });
        var flight = new SupportFlightSystem(); var flightState = new SystemState { EntityManager = em, Enabled = true };
        flightState.Begin(); flight.OnUpdate(ref flightState); flightState.End();
        using var flights = em.CreateEntityQuery(typeof(SupportFlightComponent));
        if (flights.CalculateEntityCount() != 0 || em.Exists(orphan)) throw new InvalidOperationException("Original flight owner failed to retire an orphan flight");
        Console.WriteLine("[S004SupportOriginalOwners] result=Passed staleRequest=Ignored orphanFlight=Retired scope=SourceFixtureOnly");
    }
}
