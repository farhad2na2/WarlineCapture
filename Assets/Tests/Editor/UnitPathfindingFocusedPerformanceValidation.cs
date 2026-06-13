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
    private const int MixedInfantryCount = 2;
    private const int MixedVehicleCount = 2;
    private const int MixedGroupUnitCount = MixedInfantryCount + MixedVehicleCount;
    private const int MaxPathfindingUpdates = 128;
    private const int WarmupScenarioCount = 4;
    private const int MeasuredScenarioCount = 16;
    private const double P95BudgetMs = 60d;
    private const double P99BudgetMs = 90d;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UnitPathfindingFocusedPerformanceValidation();
            tests.ManualGroupAndLongDistanceRequestsCompleteWithoutPathfindingDiagnostics();
            tests.MixedInfantryVehicleGroupPathingCompletes();
            tests.RepeatedFocusedPathfindingDoesNotAllocateOrRegress();
            Debug.Log("[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3");
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
        ScenarioResult result = RunFocusedScenario(captureDiagnostics: true);
        WriteReport(result);
    }

    [Test]
    public void MixedInfantryVehicleGroupPathingCompletes()
    {
        ScenarioResult result = RunMixedInfantryVehicleScenario(captureDiagnostics: true);
        Assert.Zero(result.RemainingRequests, "Mixed infantry/vehicle pathfinding should consume all focused path requests.");
    }

    [Test]
    public void RepeatedFocusedPathfindingDoesNotAllocateOrRegress()
    {
        for (int i = 0; i < WarmupScenarioCount; i++)
            RunFocusedScenario(captureDiagnostics: false);

        var elapsedSamples = new double[MeasuredScenarioCount];
        long maxAllocatedBytes = 0;
        int maxUpdates = 0;
        int maxPathPoolCells = 0;
        for (int i = 0; i < MeasuredScenarioCount; i++)
        {
            ScenarioResult result = RunFocusedScenario(captureDiagnostics: false);
            elapsedSamples[i] = result.ElapsedMs;
            maxAllocatedBytes = math.max(maxAllocatedBytes, result.AllocatedBytes);
            maxUpdates = math.max(maxUpdates, result.Updates);
            maxPathPoolCells = math.max(maxPathPoolCells, result.PathPoolCells);
        }

        Array.Sort(elapsedSamples);
        double averageMs = CalculateAverage(elapsedSamples);
        double p95Ms = PercentileSorted(elapsedSamples, 0.95d);
        double p99Ms = PercentileSorted(elapsedSamples, 0.99d);
        double maxMs = elapsedSamples[elapsedSamples.Length - 1];

        Assert.Zero(maxAllocatedBytes, "Focused pathfinding should not allocate on the measured update path after warmup.");
        Assert.LessOrEqual(p95Ms, P95BudgetMs, "Focused pathfinding p95 regressed beyond the current safety budget.");
        Assert.LessOrEqual(p99Ms, P99BudgetMs, "Focused pathfinding p99 regressed beyond the current safety budget.");

        WritePerformanceReport(averageMs, p95Ms, p99Ms, maxMs, maxAllocatedBytes, maxUpdates, maxPathPoolCells);
    }

    private static ScenarioResult RunFocusedScenario(bool captureDiagnostics)
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
        NativeArray<Entity> manualUnits = default;
        var capturedDiagnostics = captureDiagnostics ? new List<string>() : null;
        Application.LogCallback logCapture = (condition, _, _) =>
        {
            if (capturedDiagnostics == null)
                return;

            if (condition.Contains("[FreezeDetect:ECS]", StringComparison.Ordinal) ||
                condition.Contains("[PathDiag", StringComparison.Ordinal) ||
                condition.Contains("[HierPathValidate]", StringComparison.Ordinal))
            {
                capturedDiagnostics.Add(condition);
            }
        };

        try
        {
            if (captureDiagnostics)
                Application.logMessageReceived += logCapture;

            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            Entity gridEntity = CreateGrid(em, GridWidth, GridHeight, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            manualUnits = new NativeArray<Entity>(ManualInfantryCount, Allocator.Temp);
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
            if (captureDiagnostics)
                Assert.IsEmpty(capturedDiagnostics, "Focused pathfinding validation should not emit freeze/path diagnostics with default disabled flags.");

            Assert.Greater(em.GetComponentData<PathPoolComponent>(gridEntity).Cells.Length, 0, "Pathfinding should write path cells into the shared path pool.");

            return new ScenarioResult(
                updates,
                stopwatch.Elapsed.TotalMilliseconds,
                allocatedBytes,
                em.GetComponentData<PathPoolComponent>(gridEntity).Cells.Length,
                CountPathRequests(em),
                capturedDiagnostics?.Count ?? 0);
        }
        finally
        {
            if (captureDiagnostics)
                Application.logMessageReceived -= logCapture;
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (manualUnits.IsCreated)
                manualUnits.Dispose();
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

    private static ScenarioResult RunMixedInfantryVehicleScenario(bool captureDiagnostics)
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("UnitPathfindingMixedGroupFocusedValidation");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeList<int2> pathPool = default;
        NativeArray<Entity> mixedUnits = default;
        var capturedDiagnostics = captureDiagnostics ? new List<string>() : null;
        Application.LogCallback logCapture = (condition, _, _) =>
        {
            if (capturedDiagnostics == null)
                return;

            if (condition.Contains("[FreezeDetect:ECS]", StringComparison.Ordinal) ||
                condition.Contains("[PathDiag", StringComparison.Ordinal) ||
                condition.Contains("[HierPathValidate]", StringComparison.Ordinal))
            {
                capturedDiagnostics.Add(condition);
            }
        };

        try
        {
            if (captureDiagnostics)
                Application.logMessageReceived += logCapture;

            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            Entity gridEntity = CreateGrid(em, GridWidth, GridHeight, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds, out pathPool);
            mixedUnits = new NativeArray<Entity>(MixedGroupUnitCount, Allocator.Temp);
            int index = 0;
            for (int i = 0; i < MixedInfantryCount; i++)
            {
                mixedUnits[index++] = CreatePathfindingUnit(
                    em,
                    factionId: 0,
                    startCell: new int2(18, 34 + i),
                    goalCell: new int2(172, 156 + i),
                    usesVehicleMotion: false,
                    manualGroupMember: true);
            }

            for (int i = 0; i < MixedVehicleCount; i++)
            {
                mixedUnits[index++] = CreatePathfindingUnit(
                    em,
                    factionId: 0,
                    startCell: new int2(24, 42 + i * 4),
                    goalCell: new int2(178, 164 + i * 4),
                    usesVehicleMotion: true,
                    manualGroupMember: true);
            }

            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            var stopwatch = Stopwatch.StartNew();
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            int updates = RunPathfindingUntilComplete(world, em, pathSystem, mixedUnits);
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            stopwatch.Stop();

            AssertPathCreated(em, mixedUnits, "Mixed infantry/vehicle group pathfinding should produce paths for every selected unit.");
            if (captureDiagnostics)
                Assert.IsEmpty(capturedDiagnostics, "Mixed infantry/vehicle pathfinding should not emit freeze/path diagnostics with default disabled flags.");

            int pathPoolCells = em.GetComponentData<PathPoolComponent>(gridEntity).Cells.Length;
            Assert.Greater(pathPoolCells, 0, "Mixed infantry/vehicle pathfinding should write path cells into the shared path pool.");

            ScenarioResult result = new(
                updates,
                stopwatch.Elapsed.TotalMilliseconds,
                allocatedBytes,
                pathPoolCells,
                CountPathRequests(em),
                capturedDiagnostics?.Count ?? 0);
            Debug.Log($"[UnitPathfindingFocusedPerformanceValidation] mixedGroup updates={result.Updates} elapsedMs={result.ElapsedMs:F2} allocatedBytes={result.AllocatedBytes} pathPoolCells={result.PathPoolCells} remainingRequests={result.RemainingRequests}");
            return result;
        }
        finally
        {
            if (captureDiagnostics)
                Application.logMessageReceived -= logCapture;
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (mixedUnits.IsCreated)
                mixedUnits.Dispose();
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

            // UnitPathfindingSystem schedules a detached job that is intentionally
            // not chained into state.Dependency. Give editor batchmode worker
            // threads a tiny window so this validation measures current path
            // behavior instead of a tight-loop starvation artifact.
            System.Threading.Thread.Sleep(1);
        }

        Assert.Fail($"Pathfinding did not complete focused requests within {MaxPathfindingUpdates} updates. remainingRequests={CountPathRequests(em)}");
        return MaxPathfindingUpdates;
    }

    private static int RunPathfindingUntilComplete(World world, EntityManager em, SystemHandle pathSystem, NativeArray<Entity> units)
    {
        for (int update = 1; update <= MaxPathfindingUpdates; update++)
        {
            world.SetTime(new TimeData(update * 0.016d, 0.016f));
            pathSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            if (AllHavePaths(em, units))
                return update;

            // UnitPathfindingSystem schedules a detached job that is intentionally
            // not chained into state.Dependency. Give editor batchmode worker
            // threads a tiny window so this validation measures current path
            // behavior instead of a tight-loop starvation artifact.
            System.Threading.Thread.Sleep(1);
        }

        Assert.Fail($"Pathfinding did not complete focused requests within {MaxPathfindingUpdates} updates. remainingRequests={CountPathRequests(em)}");
        return MaxPathfindingUpdates;
    }

    private static bool AllHavePaths(EntityManager em, NativeArray<Entity> manualUnits, Entity longDistanceVehicle)
    {
        if (!AllHavePaths(em, manualUnits))
            return false;

        return em.HasComponent<UnitPathRange>(longDistanceVehicle);
    }

    private static bool AllHavePaths(EntityManager em, NativeArray<Entity> units)
    {
        for (int i = 0; i < units.Length; i++)
        {
            if (!em.HasComponent<UnitPathRange>(units[i]))
                return false;
        }

        return true;
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

    private readonly struct ScenarioResult
    {
        public readonly int Updates;
        public readonly double ElapsedMs;
        public readonly long AllocatedBytes;
        public readonly int PathPoolCells;
        public readonly int RemainingRequests;
        public readonly int DiagnosticsCount;

        public ScenarioResult(int updates, double elapsedMs, long allocatedBytes, int pathPoolCells, int remainingRequests, int diagnosticsCount)
        {
            Updates = updates;
            ElapsedMs = elapsedMs;
            AllocatedBytes = allocatedBytes;
            PathPoolCells = pathPoolCells;
            RemainingRequests = remainingRequests;
            DiagnosticsCount = diagnosticsCount;
        }
    }

    private static double CalculateAverage(double[] sortedSamples)
    {
        double total = 0d;
        for (int i = 0; i < sortedSamples.Length; i++)
            total += sortedSamples[i];
        return sortedSamples.Length > 0 ? total / sortedSamples.Length : 0d;
    }

    private static double PercentileSorted(double[] sortedSamples, double percentile)
    {
        if (sortedSamples == null || sortedSamples.Length == 0)
            return 0d;

        int index = (int)math.ceil(percentile * sortedSamples.Length) - 1;
        index = math.clamp(index, 0, sortedSamples.Length - 1);
        return sortedSamples[index];
    }

    private static void WriteReport(ScenarioResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var json = new StringBuilder();
        json.AppendLine("{");
        AppendJson(json, "result", "completed", comma: true);
        AppendJson(json, "updates", result.Updates, comma: true);
        AppendJson(json, "elapsedMs", result.ElapsedMs, comma: true);
        AppendJson(json, "allocatedBytesCurrentThread", result.AllocatedBytes, comma: true);
        AppendJson(json, "manualInfantryRequests", ManualInfantryCount, comma: true);
        AppendJson(json, "longDistanceVehicleRequests", 1, comma: true);
        AppendJson(json, "pathPoolCells", result.PathPoolCells, comma: true);
        AppendJson(json, "remainingRequests", result.RemainingRequests, comma: true);
        AppendJson(json, "pathDiagnosticsCount", result.DiagnosticsCount, comma: false);
        json.AppendLine("}");
        File.WriteAllText(ReportPath, json.ToString());
        Debug.Log($"[UnitPathfindingFocusedPerformanceValidation] report={ReportPath} updates={result.Updates} elapsedMs={result.ElapsedMs:F2} allocatedBytes={result.AllocatedBytes} pathPoolCells={result.PathPoolCells} remainingRequests={result.RemainingRequests}");
    }

    private static void WritePerformanceReport(double averageMs, double p95Ms, double p99Ms, double maxMs, long maxAllocatedBytes, int maxUpdates, int maxPathPoolCells)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var json = new StringBuilder();
        json.AppendLine("{");
        AppendJson(json, "result", "completed", comma: true);
        AppendJson(json, "warmupScenarios", WarmupScenarioCount, comma: true);
        AppendJson(json, "measuredScenarios", MeasuredScenarioCount, comma: true);
        AppendJson(json, "averageMs", averageMs, comma: true);
        AppendJson(json, "p95Ms", p95Ms, comma: true);
        AppendJson(json, "p99Ms", p99Ms, comma: true);
        AppendJson(json, "maxMs", maxMs, comma: true);
        AppendJson(json, "maxAllocatedBytesCurrentThread", maxAllocatedBytes, comma: true);
        AppendJson(json, "maxUpdates", maxUpdates, comma: true);
        AppendJson(json, "maxPathPoolCells", maxPathPoolCells, comma: true);
        AppendJson(json, "manualInfantryRequests", ManualInfantryCount, comma: true);
        AppendJson(json, "longDistanceVehicleRequests", 1, comma: false);
        json.AppendLine("}");
        File.WriteAllText(ReportPath, json.ToString());
        Debug.Log($"[UnitPathfindingFocusedPerformanceValidation] report={ReportPath} averageMs={averageMs:F2} p95Ms={p95Ms:F2} p99Ms={p99Ms:F2} maxMs={maxMs:F2} maxAllocatedBytes={maxAllocatedBytes}");
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
