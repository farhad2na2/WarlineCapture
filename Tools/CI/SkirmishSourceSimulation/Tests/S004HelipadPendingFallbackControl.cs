using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Calls the original projected resolver; the reservation is fixture data.
internal static class S004HelipadPendingFallbackControl
{
    private static void Need(bool value, string why) { if (!value) throw new Exception(why); }
    private static void Main()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true }; int next = 1;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var gridEntity = em.CreateEntity(); var grid = new GridConfig { Width = 32, Height = 32, CellSize = 1, Origin = float3.zero };
        em.AddComponentData(gridEntity, grid); var cells = em.AddBuffer<GridWalkable>(gridEntity);
        for (int i = 0; i < 1024; i++) cells.Add(new GridWalkable { Value = 1 });
        var blocked = new NativeBitArray(1024, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        var occupied = new NativeBitArray(1024, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        try
        {
            em.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = 1024, Blocked = blocked });
            em.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = 1024, Occupied = occupied });
            var boundary = em.CreateEntity(); var points = em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
            var building = new RuntimeBuildingEntity { Id = 7, HasOwnerFaction = true, OwnerFactionId = 1,
                OriginCell = new Vector2Int(8,8), Definition = new RuntimeDefinition { DisplayName = "Building_Helipad" },
                ProducedUnitSlots = new Entity[1], PendingProductions = new List<SourcePendingProduction>() };
            points.Add(new BuildingFactionProductionSpawnPointReadModel { FactionId = 1, BuildingId = new FixedString128Bytes("building_helipad"), BuildingRuntimeId = 7, SlotIndex = 0, Cell = new int2(3,3), WorldPosition = new float3(3.5f,0,3.5f) });
            var world = new SourceProductionGroundWorld(em, gridEntity, boundary,
                new Dictionary<int,RuntimeBuildingEntity> { { 7, building } }, new Dictionary<int,Game.Runtime.TransportPose.PoseGraph>());
            bool Resolve(out int2 cell) { uint random = 11; return world.TryResolveAvailableFactionHelipadSpawn(world.SpawnContext, 1, building, em, gridEntity, grid, em.GetComponentData<DynamicBlockerComponent>(gridEntity), new int2(1,1), ref random, out cell, out _); }
            Need(Resolve(out var original) && original.Equals(new int2(3,3)), "unreserved slot not used");
            building.PendingProductions.Add(new SourcePendingProduction { ReceiptId = 10, ReservedProductionSlotIndex = 0 });
            Need(world.SpawnContext.ProductionSlotSystem.IsProductionSlotReservedByPending(building, 0), "pending slot not marked reserved");
            Need(Resolve(out var fallback) && !fallback.Equals(original), "reserved slot did not use distinct fallback");
            building.PendingProductions.Clear();
            Need(Resolve(out var restored) && restored.Equals(original), "slot did not restore after pending removal");
            Console.WriteLine("[S004HelipadPendingFallbackControl] result=Passed cases=3 slot=" + original + " fallback=" + fallback + " fixtureOnly=1 nativeParity=Unknown missionReady=0");
        }
        finally { blocked.Dispose(); occupied.Dispose(); }
    }
}
