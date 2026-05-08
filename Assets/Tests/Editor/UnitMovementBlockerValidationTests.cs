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

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerData));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerData
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
