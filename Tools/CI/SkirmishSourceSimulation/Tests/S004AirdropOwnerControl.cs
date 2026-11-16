using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class S004AirdropOwnerControl
{
    private static void Main()
    {
        int next = 100;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 1, DeltaTime = 1f / 60f };
        var grid = em.CreateEntity();
        em.AddComponentData(grid, new GridConfig { Width = 2, Height = 2, CellSize = 1 });
        var walkable = em.AddBuffer<GridWalkable>(grid);
        for (int i = 0; i < 4; i++) walkable.Add(new GridWalkable { Value = 1 });
        em.AddComponentData(grid, new DynamicBlockerComponent { Blocked = new NativeBitArray(4, Allocator.Temp) });
        em.AddComponentData(grid, new DynamicOccupancyComponent { Occupied = new NativeBitArray(4, Allocator.Temp) });
        var settle = em.CreateEntity(); var parachute = em.CreateEntity(); var cargo = em.CreateEntity();
        foreach (var entity in new[] { settle, parachute, cargo }) {
            em.AddComponentData(entity, LocalTransform.Identity);
            em.AddComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        }
        var start = new float3(0, 10, 0); var end = new float3(0, 0, 0);
        em.AddComponentData(settle, new UnitTransportAirdropSettleComponent { StartPosition = start, EndPosition = end, EndCell = new int2(1, 1), DurationSeconds = 2 });
        em.AddComponentData(parachute, new UnitTransportParachuteDropComponent { StartPosition = start, EndPosition = end, LandingCell = new int2(1, 0), DurationSeconds = 2 });
        em.AddComponentData(cargo, new UnitTransportCargoDropComponent { StartPosition = start, EndPosition = end, LandingCell = new int2(0, 1), DurationSeconds = 2 });
        var owner = new UnitTransportAirdropSystem(); var state = new SystemState { EntityManager = em, Enabled = true };
        state.Begin(); owner.OnCreate(ref state); state.End();
        state.Begin(); owner.OnUpdate(ref state); state.End();
        foreach (var entity in new[] { settle, parachute, cargo })
            if (math.abs(em.GetComponentData<LocalTransform>(entity).Position.y - 5f) > 0.0001f)
                throw new InvalidOperationException("Original airdrop transform did not reach midpoint");
        if (!em.HasComponent<UnitTransportAirdropSettleComponent>(settle) ||
            !em.HasComponent<UnitTransportParachuteDropComponent>(parachute) ||
            !em.HasComponent<UnitTransportCargoDropComponent>(cargo))
            throw new InvalidOperationException("Incomplete drops must remain active");
        em.WorldUnmanaged.Time = new TimeData { ElapsedTime = 2, DeltaTime = 1f / 60f };
        state.Begin(); owner.OnUpdate(ref state); state.End();
        if (em.HasComponent<UnitTransportAirdropSettleComponent>(settle) ||
            em.HasComponent<UnitTransportParachuteDropComponent>(parachute) ||
            em.HasComponent<UnitTransportCargoDropComponent>(cargo))
            throw new InvalidOperationException("Completed drops must remove their transit components");
        if (!em.GetComponentData<UnitGrid>(settle).Cell.Equals(new int2(1, 1)) ||
            !em.GetComponentData<UnitGrid>(parachute).Cell.Equals(new int2(1, 0)) ||
            !em.GetComponentData<UnitGrid>(cargo).Cell.Equals(new int2(0, 1)))
            throw new InvalidOperationException("Original completion did not publish landing cells");
        Console.WriteLine("[S004AirdropOwnerControl] result=Passed midpointJobs=3 completedDrops=3 scope=SourceFixtureOnly");
    }
}
