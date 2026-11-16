using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Isolated source-host control. No mission input or native result is implied.
internal static class S004TransportDeployOwnerControl
{
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Main()
    {
        int next = 100;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var transport = em.CreateEntity();
        em.AddComponentData(transport, new UnitGrid { Cell = new int2(1, 1) });
        em.AddComponentData(transport, new UnitTransportDeployOrder { TargetCell = new int2(2, 2) });
        var owner = new UnitTransportDeployOrderSystem();
        var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); owner.OnCreate(ref state); state.End();
        state.Begin(); owner.OnUpdate(ref state); state.End();
        Require(em.HasComponent<UnitTransportDeployOrder>(transport), "Missing grid must leave the order untouched");

        var grid = em.CreateEntity();
        em.AddComponentData(grid, new GridConfig { Width = 4, Height = 4, CellSize = 1 });
        var walkable = em.AddBuffer<GridWalkable>(grid);
        for (int i = 0; i < 16; i++) walkable.Add(new GridWalkable { Value = 1 });
        em.AddComponentData(grid, new DynamicBlockerComponent { Blocked = new NativeBitArray(16, Allocator.Temp) });
        em.AddComponentData(grid, new DynamicOccupancyComponent { Occupied = new NativeBitArray(16, Allocator.Temp) });
        state.Begin(); owner.OnUpdate(ref state); state.End();
        Require(!em.HasComponent<UnitTransportDeployOrder>(transport), "Original owner must remove unloaded transport order");
        Console.WriteLine("[S004TransportDeployOwnerControl] result=Passed cases=2 scope=SourceFixtureOnly");
    }
}
