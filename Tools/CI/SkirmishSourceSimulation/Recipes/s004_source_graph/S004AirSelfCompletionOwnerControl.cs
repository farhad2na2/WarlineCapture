using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004AirSelfCompletionOwnerControl
{
    static void Need(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    static void Main()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true }; int next = 1;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        Entity gridEntity = em.CreateEntity();
        var grid = new GridConfig { Width = 4, Height = 4, CellSize = 1, Origin = float3.zero };
        em.AddComponentData(gridEntity, grid); var walkable = em.AddBuffer<GridWalkable>(gridEntity);
        for (int i = 0; i < 16; i++) walkable.Add(new GridWalkable { Value = 1 });
        var blocked = new NativeBitArray(16, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        var occupied = new NativeBitArray(16, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        try
        {
            em.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = 16, Blocked = blocked });
            em.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = 16, Occupied = occupied });
            var b = new RuntimeBuildingEntity { Id = 7, InitialProducerCollectionsKnown = true, PendingProductions = new List<SourcePendingProduction>() };
            var buildings = new Dictionary<int, RuntimeBuildingEntity> { [7] = b };
            var world = new SourceProductionGroundWorld(em, gridEntity, Entity.Null, buildings, new Dictionary<int, Game.Runtime.TransportPose.PoseGraph>());
            var production = new BuildingProductionQueueCompositionSystemHelper();
            var owner = new SourceProductionDeliveryOwner(world, production, world.SpawnContext,
                new Dictionary<int, SourceProductionDeliveryRecipe[]>(), 0, 7);
            bool destroyed = false;
            var boundary = new SourceAirSelfCompletionOwnerBoundary(owner, b, ignored => destroyed = true);
            var prefab = new SourceUnitPrefab("fixture", em.CreateEntity());
            var pending = new SourcePendingProduction { ProductionIndex = 3, TransportPrefab = prefab, ReadyAt = 4, StartedAt = 0 };
            b.PendingProductions.Add(pending);
            Need(boundary.FindReady(b, prefab, 3) == null, "not yet ready");
            Need(ReferenceEquals(boundary.FindReady(b, prefab, 4), pending), "ready identity");
            Need(boundary.FindReady(b, new SourceUnitPrefab("other", em.CreateEntity()), 4) == null, "prefab identity");
            var cell = boundary.ResolveCell(new float3(2.5f, 0, 1.5f)); Need(cell.x == 2 && cell.y == 1, "grid mapping");
            bool rejected = false;
            try { boundary.FindReady(new RuntimeBuildingEntity { Id = 7 }, prefab, 4); } catch (InvalidOperationException) { rejected = true; }
            Need(rejected && b.PendingProductions.Count == 1, "foreign producer");
            Need(boundary.Remove(b, pending) && b.PendingProductions.Count == 0, "original removal");
            boundary.Release(pending); Need(pending.Prefab == null && pending.TransportPrefab == null, "original pool release");
            boundary.Destroy(b); Need(destroyed, "explicit teardown callback");
            Console.WriteLine("[S004AirSelfCompletionOwnerControl] result=Passed scope=SyntheticFixture_NoSpawnOrTransport");
        }
        finally { blocked.Dispose(); occupied.Dispose(); }
    }
}
