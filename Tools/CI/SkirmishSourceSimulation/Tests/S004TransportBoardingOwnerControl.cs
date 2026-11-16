using System;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class S004TransportBoardingOwnerControl
{
    private static void Main()
    {
        int next = 100;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var grid = em.CreateEntity();
        em.AddComponentData(grid, new GridConfig { Width = 8, Height = 8, CellSize = 1 });
        var carrier = em.CreateEntity();
        em.AddComponentData(carrier, new UnitTransportCapacity { SoldierCapacity = 1 });
        em.AddComponentData(carrier, new UnitGrid { Cell = new int2(3, 3) });
        em.AddComponentData(carrier, new UnitFootprint { Size = new int2(1, 1) });
        em.AddComponentData(carrier, LocalTransform.FromPosition(new float3(3.5f, 0, 3.5f)));
        em.AddBuffer<UnitTransportPassengerElement>(carrier);
        var passenger = em.CreateEntity();
        em.AddComponentData(passenger, new UnitGrid { Cell = new int2(3, 3) });
        em.AddComponentData(passenger, LocalTransform.FromPosition(new float3(3.5f, 0, 3.5f)));
        em.AddComponentData(passenger, new UnitTransportBoardingTarget { Transport = carrier, Goal = new int2(3, 3) });
        var owner = new UnitTransportBoardingSystem(); var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); owner.OnCreate(ref state); state.End();
        state.Begin(); owner.OnUpdate(ref state); state.End();
        var rows = em.GetBuffer<UnitTransportPassengerElement>(carrier);
        if (rows.Length != 1 || rows[0].Passenger != passenger || em.HasComponent<UnitTransportBoardingTarget>(passenger) ||
            !em.HasComponent<UnitTransportPassenger>(passenger) || em.GetComponentData<UnitTransportPassenger>(passenger).Transport != carrier)
            throw new InvalidOperationException("Original boarding owner did not register the passenger");
        var full = em.CreateEntity(); var missing = em.CreateEntity();
        foreach (var entity in new[] { full, missing }) {
            em.AddComponentData(entity, new UnitGrid { Cell = new int2(3, 3) });
            em.AddComponentData(entity, LocalTransform.FromPosition(new float3(3.5f, 0, 3.5f)));
        }
        em.AddComponentData(full, new UnitTransportBoardingTarget { Transport = carrier, Goal = new int2(3, 3) });
        em.AddComponentData(missing, new UnitTransportBoardingTarget { Transport = Entity.Null, Goal = new int2(3, 3) });
        state.Begin(); owner.OnUpdate(ref state); state.End();
        if (em.HasComponent<UnitTransportBoardingTarget>(full) || em.HasComponent<UnitTransportBoardingTarget>(missing) ||
            em.HasComponent<UnitTransportPassenger>(full) || em.HasComponent<UnitTransportPassenger>(missing) ||
            em.GetBuffer<UnitTransportPassengerElement>(carrier).Length != 1)
            throw new InvalidOperationException("Full and missing transports must reject boarding without changing cargo");
        Console.WriteLine("[S004TransportBoardingOwnerControl] result=Passed boarded=1 full=Rejected missing=Rejected scope=SourceFixtureOnly");
    }
}
