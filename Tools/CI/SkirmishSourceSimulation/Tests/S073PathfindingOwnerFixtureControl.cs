using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime;

// Isolated control: a fresh shim EntityManager, explicitly activated for this
// fixture only. This exercises the original owner lifecycle, not S073 admission.
internal static class S073PathfindingOwnerFixtureControl
{
    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 pathfinding fixture control: " + message);
    }

    internal static JObject Run()
    {
        const int width = 12, height = 8;
        int size = width * height;
        var em = new EntityManager();
        int nextEntityId = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextEntityId++, Version = 1 };
        var grid = em.CreateEntity();
        em.AddComponentData(grid, new GridConfig { Width = width, Height = height, CellSize = 1f });
        var blocked = new NativeBitArray(size, Allocator.Persistent);
        // A full-height wall separates the unreachable fixture request.
        for (int y = 0; y < height; y++) blocked.Set(5 + y * width, true);
        em.AddComponentData(grid, new DynamicBlockerComponent {
            GridSize = size, Counts = new NativeArray<int>(size, Allocator.Persistent),
            Blocked = blocked, FriendlyPassFactionIds = new NativeArray<byte>(size, Allocator.Persistent),
            StorageEpoch = 1, AppliedStorageEpoch = 1, AppliedRevision = 1,
            AppliedGrid = new GridConfig { Width = width, Height = height, CellSize = 1f }
        });
        em.AddComponentData(grid, new DynamicOccupancyComponent { GridSize = size, Occupied = new NativeBitArray(size, Allocator.Persistent) });
        em.AddComponentData(grid, new PathPoolComponent { Cells = new NativeList<int2>(64, Allocator.Persistent) });
        var walkable = em.AddBuffer<GridWalkable>(grid);
        var roads = em.AddBuffer<GridRoad>(grid);
        var sidewalks = em.AddBuffer<GridRoadSidewalk>(grid);
        var dirt = em.AddBuffer<GridRoadDirt>(grid);
        for (int i = 0; i < size; i++) {
            byte value = (byte)(i % width == 5 ? 0 : 1);
            walkable.Add(new GridWalkable { Value = value }); roads.Add(default);
            sidewalks.Add(default); dirt.Add(default);
        }
        var gameplay = em.CreateEntity();
        em.AddComponentData(gameplay, new RuntimeGameplayStateComponent { PlayRequested = 1, SimulationActive = 1 });
        var deferred = new OwnedEndSimulationBuffers();
        var deferredEntity = em.CreateEntity();
        em.AddComponentData(deferredEntity, new EndSimulationEntityCommandBufferSystem.Singleton { Owner = deferred });

        Entity reachable = AddRequest(em, new int2(1, 1), new int2(3, 1));
        Entity unreachable = AddRequest(em, new int2(1, 6), new int2(9, 6));
        using var owner = new OriginalGroundOwners(em, generatedFresh: true);
        owner.PathPhase();
        var pendingAfterSchedule = em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).GetSingleton<UnitPathfindingPendingStateComponent>();
        Need(pendingAfterSchedule.HasPendingPathJob != 0 && pendingAfterSchedule.RequestCount == 1,
            "original UnitPathfindingSystem did not admit the first budgeted fixture request");
        owner.PathPhase();
        var pendingBetweenRequests = em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).GetSingleton<UnitPathfindingPendingStateComponent>();
        Need(pendingBetweenRequests.HasPendingPathJob != 0 && pendingBetweenRequests.RequestCount == 1,
            "original owner did not continue with the remaining budgeted fixture request");
        owner.PathPhase();
        var pendingAfterApply = em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).GetSingleton<UnitPathfindingPendingStateComponent>();
        Need(pendingAfterApply.HasPendingPathJob == 0, "original owner's next update did not apply and clear pending job state");
        Need(em.HasComponent<UnitPathFollow>(reachable) && em.HasComponent<UnitPathRange>(reachable),
            "reachable fixture request did not receive an applied path");
        Need(!em.HasComponent<UnitPathFollow>(unreachable), "unreachable fixture unexpectedly received a path");
        var pool = em.GetComponentData<PathPoolComponent>(grid);
        Need(pool.Cells.Length > 0, "reachable path was not written to the original path pool");
        return new JObject {
            ["kind"] = "S073PathfindingOwnerFixtureControl", ["schema_version"] = 1,
            ["status"] = "Passed", ["fixture_entity_manager"] = "fresh isolated shim EntityManager",
            ["activation"] = "fixture RuntimeGameplayStateComponent.PlayRequested=1; not S073/player activation",
            ["original_system_owner_invoked"] = true, ["fixture_request_count"] = 2,
            ["pending_after_first_update"] = pendingAfterSchedule.HasPendingPathJob != 0,
            ["pending_between_budgeted_requests"] = pendingBetweenRequests.HasPendingPathJob != 0,
            ["pending_after_result_application"] = pendingAfterApply.HasPendingPathJob != 0,
            ["reachable_path_applied"] = true, ["unreachable_path_rejected"] = true,
            ["path_pool_cell_count"] = pool.Cells.Length,
            ["job_backend"] = "OwnedJobBackend executes original IJobFor synchronously; owner applies results on its next update",
            ["native_system_group_or_async_parity"] = false, ["scheduler_native_parity"] = false,
            ["s073_scheduler_admission"] = false, ["s073_command_or_movement"] = false, ["mission_ready"] = false
        };
    }

    static Entity AddRequest(EntityManager em, int2 start, int2 goal)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new UnitGrid { Cell = start });
        em.AddComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.AddComponentData(entity, new UnitMovementBehavior());
        em.AddComponentData(entity, new Faction { Id = 1 });
        em.AddComponentData(entity, new UnitPathRequest { Goal = goal });
        return entity;
    }
}

internal static class S073PathfindingFixtureEntry
{
    public static int Main()
    {
        Console.WriteLine(S073PathfindingOwnerFixtureControl.Run().ToString(Newtonsoft.Json.Formatting.None));
        return 0;
    }
}
