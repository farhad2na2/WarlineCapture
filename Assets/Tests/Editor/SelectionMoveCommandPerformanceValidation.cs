#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class SelectionMoveCommandPerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-selection-move-command-performance.json";
    private const int SelectedUnitCount = 8;
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 180;
    private const int GridWidth = 64;
    private const int GridHeight = 64;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new SelectionMoveCommandPerformanceValidation();
            tests.SelectedUnitsIssueMoveCommandsAndReportTiming();
            Debug.Log("[SelectionMoveCommandPerformanceValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[SelectionMoveCommandPerformanceValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void SelectedUnitsIssueMoveCommandsAndReportTiming()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("SelectionMoveCommandPerformanceValidation");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds);
            Entity[] selectedUnits = CreateSelectedUnits(em);
            using EntityQuery selectedMoveQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitGrid>());
            using EntityQuery gridQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            using EntityQuery mapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());

            var selectionStateSystem = new SelectionStateCompositionSystemHelper();
            var selectedMoveOrderCommandSystem = new SelectedMoveOrderCommandSystem();
            var moveOrderSystem = new UnitMoveOrderSystem();

            int acceptedWarmup = RunFrames(
                em,
                selectionStateSystem,
                selectedMoveOrderCommandSystem,
                moveOrderSystem,
                selectedMoveQuery,
                gridQuery,
                mapSurfaceQuery,
                selectedUnits,
                WarmupFrames,
                0,
                samples: null);

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long totalStartTicks = Stopwatch.GetTimestamp();
            int acceptedMeasured = RunFrames(
                em,
                selectionStateSystem,
                selectedMoveOrderCommandSystem,
                moveOrderSystem,
                selectedMoveQuery,
                gridQuery,
                mapSurfaceQuery,
                selectedUnits,
                MeasuredFrames,
                WarmupFrames,
                samples);
            long totalStopTicks = Stopwatch.GetTimestamp();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            Assert.AreEqual(WarmupFrames, acceptedWarmup, "Every warmup move command should be accepted.");
            Assert.AreEqual(MeasuredFrames, acceptedMeasured, "Every measured move command should be accepted.");
            for (int i = 0; i < selectedUnits.Length; i++)
            {
                Assert.IsTrue(em.HasComponent<UnitTarget>(selectedUnits[i]), $"Selected unit {i} should receive a target.");
                Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(selectedUnits[i]), $"Selected unit {i} should keep manual move order state.");
            }

            Array.Sort(samples);
            double totalMs = TicksToMilliseconds(totalStopTicks - totalStartTicks);
            double averageMs = totalMs / MeasuredFrames;
            double p95Ms = PercentileSorted(samples, 0.95d);
            double p99Ms = PercentileSorted(samples, 0.99d);
            double maxMs = samples[samples.Length - 1];

            WriteReport(
                totalMs,
                averageMs,
                p95Ms,
                p99Ms,
                maxMs,
                allocatedBytes,
                acceptedWarmup + acceptedMeasured);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();

            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (world.IsCreated)
                world.Dispose();
        }
    }

    private static int RunFrames(
        EntityManager em,
        SelectionStateCompositionSystemHelper selectionStateSystem,
        SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
        UnitMoveOrderSystem moveOrderSystem,
        EntityQuery selectedMoveQuery,
        EntityQuery gridQuery,
        EntityQuery mapSurfaceQuery,
        Entity[] selectedUnits,
        int frames,
        int frameOffset,
        double[] samples)
    {
        int accepted = 0;
        MoveCommandResolvers resolvers = new();
        SelectedMoveOrderCommandSystem.ClickedUnitResolver clickedUnitResolver = resolvers.TryGetClickedUnit;
        SelectedMoveOrderCommandSystem.ClickedCellResolver clickedCellResolver = resolvers.TryGetClickedCell;
        for (int frame = 0; frame < frames; frame++)
        {
            int currentFrame = frameOffset + frame;
            resolvers.CurrentGoal = ResolveGoal(currentFrame);

            long startTicks = Stopwatch.GetTimestamp();
            selectionStateSystem.CacheSelectedMoveEntities(em, selectedUnits);
            SelectedMoveOrderCommandSystem.Result result = selectedMoveOrderCommandSystem.TryIssueMoveOrder(
                em,
                Vector2.zero,
                selectedMoveQuery,
                gridQuery,
                mapSurfaceQuery,
                moveOrderSystem,
                clickedUnitResolver,
                clickedCellResolver,
                currentFrame,
                selectionStateSystem.CachedSelectedMoveEntities);
            em.CompleteAllTrackedJobs();
            long stopTicks = Stopwatch.GetTimestamp();

            if (result.CommandResult.Accepted)
                accepted++;
            if (samples != null)
                samples[frame] = TicksToMilliseconds(stopTicks - startTicks);
        }

        return accepted;
    }

    private sealed class MoveCommandResolvers
    {
        public int2 CurrentGoal;

        public bool TryGetClickedUnit(Vector2 screenPosition, EntityManager em, out Entity clicked)
        {
            clicked = Entity.Null;
            return false;
        }

        public bool TryGetClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
        {
            cell = CurrentGoal;
            worldPoint = new Vector3(CurrentGoal.x, 0f, CurrentGoal.y);
            return true;
        }
    }

    private static int2 ResolveGoal(int frame)
    {
        return new int2(30 + frame % 10, 34 + (frame / 10) % 10);
    }

    private static void CreateGrid(
        EntityManager em,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeBitArray occupied,
        out NativeArray<byte> friendlyPassFactionIds)
    {
        int gridSize = GridWidth * GridHeight;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(GridWalkable));
        em.SetComponentData(gridEntity, new GridConfig { Width = GridWidth, Height = GridHeight, CellSize = 1f, Origin = float3.zero });
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

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static Entity[] CreateSelectedUnits(EntityManager em)
    {
        Entity[] selectedUnits = new Entity[SelectedUnitCount];
        for (int i = 0; i < selectedUnits.Length; i++)
        {
            Entity unit = em.CreateEntity(
                typeof(SelectedUnitTag),
                typeof(Faction),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint));
            em.SetName(unit, $"SelectionMovePerfUnit_{i}");
            em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(unit, new UnitMove
            {
                Speed = 5f,
                WalkSpeed = 5f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(4 + i, 6) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            selectedUnits[i] = unit;
        }

        return selectedUnits;
    }

    private static double PercentileSorted(double[] sortedSamples, double percentile)
    {
        if (sortedSamples.Length == 0)
            return 0d;

        double position = (sortedSamples.Length - 1) * percentile;
        int lower = (int)math.floor(position);
        int upper = math.min(sortedSamples.Length - 1, lower + 1);
        double blend = position - lower;
        return sortedSamples[lower] + (sortedSamples[upper] - sortedSamples[lower]) * blend;
    }

    private static double TicksToMilliseconds(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private static void WriteReport(
        double totalMs,
        double averageMs,
        double p95Ms,
        double p99Ms,
        double maxMs,
        long allocatedBytes,
        int acceptedMoveCommands)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder(512);
        builder.AppendLine("{");
        AppendJson(builder, "warmupFrames", WarmupFrames, trailingComma: true);
        AppendJson(builder, "measuredFrames", MeasuredFrames, trailingComma: true);
        AppendJson(builder, "selectedUnitCount", SelectedUnitCount, trailingComma: true);
        AppendJson(builder, "acceptedMoveCommands", acceptedMoveCommands, trailingComma: true);
        AppendJson(builder, "totalMs", totalMs, trailingComma: true);
        AppendJson(builder, "averageMs", averageMs, trailingComma: true);
        AppendJson(builder, "p95Ms", p95Ms, trailingComma: true);
        AppendJson(builder, "p99Ms", p99Ms, trailingComma: true);
        AppendJson(builder, "maxMs", maxMs, trailingComma: true);
        AppendJson(builder, "allocatedBytesCurrentThread", allocatedBytes, trailingComma: false);
        builder.AppendLine("}");
        File.WriteAllText(ReportPath, builder.ToString());
    }

    private static void AppendJson(StringBuilder builder, string name, int value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, long value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, double value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ");
        builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }
}
#endif
