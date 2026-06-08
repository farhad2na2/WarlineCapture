#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class UnitPathfindingFocusedPerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-unit-pathfinding-focused-performance.json";
    private const int GridWidth = 240;
    private const int GridHeight = 240;
    private const int ManualInfantryCount = 4;
    private const int MaxPathfindingUpdates = 48;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UnitPathfindingFocusedPerformanceValidation();
            tests.ManualGroupAndLongDistanceRequestsCompleteWithoutPathfindingDiagnostics();
            Debug.Log("[UnitPathfindingFocusedPerformanceValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitPathfindingFocusedPerformanceValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void ManualGroupAndLongDistanceRequestsCompleteWithoutPathfindingDiagnostics()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("UnitPathfindingFocusedPerformanceValidation");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        var capturedDiagnostics = new List<string>();
        Application.LogCallback logCapture = (condition, _, _) =>
        {
            if (condition.Contains("[FreezeDetect:ECS]", StringComparison.Ordinal) ||
                condition.Contains("[PathDiag", StringComparison.Ordinal) ||
                condition.Contains("[HierPathValidate]", StringComparison.Ordinal))
            {
                capturedDiagnostics.Add(condition);
            }
        };

        try
        {
            Application.logMessageReceived += logCapture;
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            Entity gridEntity = CreateGrid(em, GridWidth, GridHeight, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            var manualUnits = new NativeArray<Entity>(ManualInfantryCount, Allocator.Temp);
            for (int i = 0; i < ManualInfantryCount; i++)
            {
                manualUnits[i] = CreatePathfindingUnit(
                    em,
                    factionId: 0,
                    startCell: new int2(12, 24 + i),
                    goalCell: new int2(185, 168 + i),
                    usesVehicleMotion: false,
                    manualGroupMember: true);
            }

            Entity longDistanceVehicle = CreatePathfindingUnit(
                em,
                factionId: 0,
                startCell: new int2(20, 20),
                goalCell: new int2(220, 220),
                usesVehicleMotion: true,
                manualGroupMember: false);

            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            var stopwatch = Stopwatch.StartNew();
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            int updates = RunPathfindingUntilComplete(world, em, pathSystem, manualUnits, longDistanceVehicle);
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            stopwatch.Stop();

            AssertPathCreated(em, manualUnits, "Manual infantry group pathfinding should produce paths for every selected unit.");
            AssertPathCreated(em, longDistanceVehicle, "Long-distance vehicle pathfinding should produce a first path segment.");
            Assert.IsTrue(
                em.HasComponent<UnitLongDistanceMove>(longDistanceVehicle),
                "The long-distance vehicle request should stay segmented after the first pathfinding pass.");
            Assert.IsEmpty(capturedDiagnostics, "Focused pathfinding validation should not emit freeze/path diagnostics with default disabled flags.");
            Assert.Greater(em.GetComponentData<PathPoolComponent>(gridEntity).Cells.Length, 0, "Pathfinding should write path cells into the shared path pool.");

            WriteReport(
                updates,
                stopwatch.Elapsed.TotalMilliseconds,
                allocatedBytes,
                em.GetComponentData<PathPoolComponent>(gridEntity).Cells.Length,
                CountPathRequests(em),
                capturedDiagnostics.Count);
        }
        finally
        {
            Application.logMessageReceived -= logCapture;
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (world.IsCreated)
                world.Dispose();
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    private static int RunPathfindingUntilComplete(World world, EntityManager em, SystemHandle pathSystem, NativeArray<Entity> manualUnits, Entity longDistanceVehicle)
    {
        for (int update = 1; update <= MaxPathfindingUpdates; update++)
        {
            world.SetTime(new TimeData(update * 0.016d, 0.016f));
            pathSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            if (AllHavePaths(em, manualUnits, longDistanceVehicle))
                return update;
        }

        Assert.Fail($"Pathfinding did not complete focused requests within {MaxPathfindingUpdates} updates. remainingRequests={CountPathRequests(em)}");
        return MaxPathfindingUpdates;
    }

    private static bool AllHavePaths(EntityManager em, NativeArray<Entity> manualUnits, Entity longDistanceVehicle)
    {
        for (int i = 0; i < manualUnits.Length; i++)
        {
            if (!em.HasComponent<UnitPathRange>(manualUnits[i]))
                return false;
        }

        return em.HasComponent<UnitPathRange>(longDistanceVehicle);
    }

    private static void AssertPathCreated(EntityManager em, NativeArray<Entity> units, string message)
    {
        for (int i = 0; i < units.Length; i++)
            AssertPathCreated(em, units[i], message);
    }

    private static void AssertPathCreated(EntityManager em, Entity unit, string message)
    {
        Assert.IsTrue(em.HasComponent<UnitPathRange>(unit), message);
        UnitPathRange range = em.GetComponentData<UnitPathRange>(unit);
        Assert.Greater(range.Length, 0, message);
    }

    private static Entity CreateGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeBitArray occupied,
        out NativeArray<byte> friendlyPassFactionIds,
        out NativeList<int2> pathPool)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        pathPool = new NativeList<int2>(1024, Allocator.Persistent);
        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent),
            typeof(GridWalkable),
            typeof(GridRoad),
            typeof(GridRoadSidewalk),
            typeof(GridRoadDirt));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds,
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = occupied,
        });
        em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

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

        return gridEntity;
    }

    private static Entity CreatePathfindingUnit(
        EntityManager em,
        byte factionId,
        int2 startCell,
        int2 goalCell,
        bool usesVehicleMotion,
        bool manualGroupMember)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitPathRequest),
            typeof(UnitTarget),
            typeof(ManualMoveOrderTag));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = startCell });
        em.SetComponentData(entity, new UnitFootprint { Size = usesVehicleMotion ? new int2(2, 2) : new int2(1, 1) });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = usesVehicleMotion ? (byte)1 : (byte)0 });
        em.SetComponentData(entity, new UnitPathRequest { Goal = goalCell });
        em.SetComponentData(entity, new UnitTarget { Cell = goalCell });
        if (manualGroupMember)
            em.AddComponent<ManualMoveGroupMemberTag>(entity);
        return entity;
    }

    private static int CountPathRequests(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitPathRequest>());
        return query.CalculateEntityCount();
    }

    private static void WriteReport(int updates, double elapsedMs, long allocatedBytes, int pathPoolCells, int remainingRequests, int diagnosticsCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var json = new StringBuilder();
        json.AppendLine("{");
        AppendJson(json, "result", "completed", comma: true);
        AppendJson(json, "updates", updates, comma: true);
        AppendJson(json, "elapsedMs", elapsedMs, comma: true);
        AppendJson(json, "allocatedBytesCurrentThread", allocatedBytes, comma: true);
        AppendJson(json, "manualInfantryRequests", ManualInfantryCount, comma: true);
        AppendJson(json, "longDistanceVehicleRequests", 1, comma: true);
        AppendJson(json, "pathPoolCells", pathPoolCells, comma: true);
        AppendJson(json, "remainingRequests", remainingRequests, comma: true);
        AppendJson(json, "pathDiagnosticsCount", diagnosticsCount, comma: false);
        json.AppendLine("}");
        File.WriteAllText(ReportPath, json.ToString());
        Debug.Log($"[UnitPathfindingFocusedPerformanceValidation] report={ReportPath} updates={updates} elapsedMs={elapsedMs:F2} allocatedBytes={allocatedBytes} pathPoolCells={pathPoolCells} remainingRequests={remainingRequests}");
    }

    private static void AppendJson(StringBuilder json, string name, string value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": \"").Append(value).Append(comma ? "\"," : "\"").AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, int value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, long value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, double value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString("F2", CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }
}
#endif
