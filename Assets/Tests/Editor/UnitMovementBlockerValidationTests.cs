using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class UnitMovementBlockerValidationTests
{
    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UnitMovementBlockerValidationTests();
            tests.UnitMovementTargetRejectsBuildingBlockerCells();
            tests.EngagedCombatMovementStopsBeforeBuildingBlocker();
            tests.InfantryMovementDoesNotStallOnOwnPreviousOccupancySnapshot();
            tests.InfantryOpenPathMovementAdvancesEveryFrame();
            Debug.Log("[UnitMovementBlockerValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitMovementBlockerValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void UnitMovementTargetRejectsBuildingBlockerCells()
    {
        var grid = new GridConfig
        {
            Width = 5,
            Height = 5,
            CellSize = 1f,
            Origin = float3.zero
        };

        var walkable = new NativeArray<GridWalkable>(grid.Width * grid.Height, Allocator.Temp);
        var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var friendlyPassFactionIds = new NativeArray<byte>(grid.Width * grid.Height, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                friendlyPassFactionIds[i] = byte.MaxValue;
            }

            int buildingIndex = GridUtils.CellToIndex(new int2(2, 2), grid.Width);
            blocked.Set(buildingIndex, true);

            Assert.IsFalse(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(2, 2),
                    new int2(1, 1),
                    new int2(1, 2),
                    factionId: 1),
                "Units must not move their path target onto a building or wall blocker cell.");

            Assert.IsTrue(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(1, 1),
                    new int2(1, 1),
                    new int2(1, 2),
                    factionId: 1),
                "Units should still accept an adjacent walkable, unblocked target cell.");

            int gateIndex = GridUtils.CellToIndex(new int2(3, 2), grid.Width);
            blocked.Set(gateIndex, true);
            friendlyPassFactionIds[gateIndex] = 0;

            Assert.IsTrue(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(3, 2),
                    new int2(1, 1),
                    new int2(3, 1),
                    factionId: 0),
                "A gate blocker should allow only its configured friendly faction through.");

            Assert.IsFalse(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(3, 2),
                    new int2(1, 1),
                    new int2(3, 1),
                    factionId: 1),
                "Enemy units must not pass through another faction's gate blocker.");
        }
        finally
        {
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void EngagedCombatMovementStopsBeforeBuildingBlocker()
    {
        using var world = new World("UnitMovementBlockerValidationTests");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 6, 3, out blockerCounts, out blocked, out friendlyPassFactionIds);
            blocked.Set(GridUtils.CellToIndex(new int2(2, 1), 6), true);

            Entity target = em.CreateEntity(
                typeof(UnitHealth),
                typeof(UnitFootprint));
            em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(target, new UnitFootprint { Size = new int2(1, 1) });

            Entity attacker = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(EngageTarget),
                typeof(LocalTransform));
            em.SetComponentData(attacker, new Faction { Id = 1 });
            em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitMove { Speed = 2f, WalkSpeed = 2f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(attacker, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(attacker, new UnitVehicleMovement());
            em.SetComponentData(attacker, new UnitVehicleKinematics());
            em.SetComponentData(attacker, new UnitCombat { AggroRangeCells = 8, ChaseBreakDistance = 20f, CanAttack = 1, AutoEngage = 1 });
            em.SetComponentData(attacker, new UnitAttack { Range = 0.1f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(attacker, new EngageTarget
            {
                Target = target,
                Cell = new int2(4, 1),
                Position = new float3(4.5f, 0f, 1.5f),
                IsCommanded = 1
            });
            em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

            SystemHandle engagedMoveSystem = world.CreateSystem<UnitEngagedMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            engagedMoveSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(attacker).Position;
            Assert.AreEqual(1.5f, position.x, 0.001f, "Engaged combat movement must not step into a blocked building/wall cell.");
            Assert.AreEqual(new int2(1, 1), em.GetComponentData<UnitGrid>(attacker).Cell);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InfantryMovementDoesNotStallOnOwnPreviousOccupancySnapshot()
    {
        using var world = new World("UnitMovementSelfOccupancyValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 4;
            const int height = 1;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            pathPool.Add(new int2(1, 0));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            roads.ResizeUninitialized(gridSize);
            sidewalks.ResizeUninitialized(gridSize);
            dirtRoads.ResizeUninitialized(gridSize);
            for (int i = 0; i < gridSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            int targetIndex = GridUtils.CellToIndex(new int2(1, 0), width);
            occupied.Set(targetIndex, true);

            Entity unit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(unit, new Faction { Id = 0 });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(1, 0) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(unit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(unit, new UnitVehicleMovement());
            em.SetComponentData(unit, new UnitVehicleKinematics());
            em.SetComponentData(unit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(unit, new UnitPathRange { Start = 0, Length = 1 });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(0.5f, 0f, 0.5f)));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            movementSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(unit).Position;
            Assert.Greater(position.x, 0.5f, "Infantry must not spend a frame stalled when the only occupant in the next path cell is itself from the previous occupancy snapshot.");
            Assert.Less(position.x, 1.5f, "This validation should exercise normal in-flight movement, not path completion.");
        }
        finally
        {
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InfantryOpenPathMovementAdvancesEveryFrame()
    {
        using var world = new World("UnitMovementOpenPathContinuityValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 12;
            const int height = 1;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            for (int x = 1; x <= 10; x++)
                pathPool.Add(new int2(x, 0));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            roads.ResizeUninitialized(gridSize);
            sidewalks.ResizeUninitialized(gridSize);
            dirtRoads.ResizeUninitialized(gridSize);
            for (int i = 0; i < gridSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            Entity unit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(unit, new Faction { Id = 0 });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(0, 0) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(unit, new UnitMove { Speed = 3f, WalkSpeed = 3f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(unit, new UnitVehicleMovement());
            em.SetComponentData(unit, new UnitVehicleKinematics());
            em.SetComponentData(unit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(unit, new UnitPathRange { Start = 0, Length = pathPool.Length });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(0.5f, 0f, 0.5f)));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            float previousX = em.GetComponentData<LocalTransform>(unit).Position.x;
            for (int frame = 1; frame <= 40; frame++)
            {
                world.SetTime(new TimeData(frame / 60d, 1f / 60f));
                movementSystem.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();

                float currentX = em.GetComponentData<LocalTransform>(unit).Position.x;
                Assert.Greater(
                    currentX,
                    previousX + 0.0001f,
                    $"Infantry ECS position must advance every frame on an open path before visual animation is considered. frame={frame}");
                previousX = currentX;
            }
        }
        finally
        {
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    private static void CreateGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeArray<byte> friendlyPassFactionIds)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });

        em.AddBuffer<GridWalkable>(gridEntity);
        em.AddBuffer<GridRoad>(gridEntity);
        em.AddBuffer<GridRoadSidewalk>(gridEntity);
        em.AddBuffer<GridRoadDirt>(gridEntity);

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }
}
