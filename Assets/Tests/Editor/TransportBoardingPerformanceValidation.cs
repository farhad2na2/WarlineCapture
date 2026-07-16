using Game.Components;
using Game.Runtime;
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
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class TransportBoardingPerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-transport-boarding-performance.json";
    private const int WarmupScenarios = 16;
    private const int MeasuredScenarios = 64;
    private const int MeasuredBatches = 3;
    private const int PassengerCount = 8;
    private const int GridWidth = 64;
    private const int GridHeight = 64;
    private const int NonRegressionMarginPercent = 25;
    private const double Am012AverageTotalMsBaseline = 1.400d;
    private const double Am012P95TotalMsBaseline = 1.504d;
    private const double AverageTotalMsCeiling = 1.750d;
    private const double P95TotalMsCeiling = 1.880d;
    private const long AllocatedBytesCeiling = 0L;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new TransportBoardingPerformanceValidation();
            tests.TransportBoardAllAndDisembarkAllReportTiming();
            Debug.Log("[TransportBoardingPerformanceValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[TransportBoardingPerformanceValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void TransportBoardAllAndDisembarkAllReportTiming()
    {
        var batches = new BatchMetrics[MeasuredBatches];
        long allocatedBytes = 0;
        for (int i = 0; i < batches.Length; i++)
        {
            batches[i] = RunMeasuredBatch(i);
            allocatedBytes += batches[i].AllocatedBytes;
        }

        int selectedBatchIndex = SelectMedianBatchIndex(batches);
        BatchMetrics selectedBatch = batches[selectedBatchIndex];

        WriteReport(batches, selectedBatchIndex, allocatedBytes);

        Assert.AreEqual(
            AllocatedBytesCeiling,
            allocatedBytes,
            $"All {MeasuredBatches} measured batches must allocate exactly 0 bytes; the explicit allocation ceiling is {AllocatedBytesCeiling} bytes, observed {allocatedBytes} bytes. See {ReportPath}.");
        Assert.LessOrEqual(
            selectedBatch.AverageTotalMs,
            AverageTotalMsCeiling,
            $"Median-of-three selected batch {selectedBatchIndex} average total time must be <= 1.750 ms (AM-012 baseline {Am012AverageTotalMsBaseline.ToString("0.000", CultureInfo.InvariantCulture)} ms plus the {NonRegressionMarginPercent}% non-regression margin); the explicit ceiling is {AverageTotalMsCeiling.ToString("0.000", CultureInfo.InvariantCulture)} ms, observed {selectedBatch.AverageTotalMs.ToString("0.000", CultureInfo.InvariantCulture)} ms. See {ReportPath}.");
        Assert.LessOrEqual(
            selectedBatch.P95TotalMs,
            P95TotalMsCeiling,
            $"Median-of-three selected batch {selectedBatchIndex} P95 total time must be <= 1.880 ms (AM-012 baseline {Am012P95TotalMsBaseline.ToString("0.000", CultureInfo.InvariantCulture)} ms plus the {NonRegressionMarginPercent}% non-regression margin); the explicit ceiling is {P95TotalMsCeiling.ToString("0.000", CultureInfo.InvariantCulture)} ms, observed {selectedBatch.P95TotalMs.ToString("0.000", CultureInfo.InvariantCulture)} ms. See {ReportPath}.");
    }

    private static BatchMetrics RunMeasuredBatch(int batchIndex)
    {
        for (int i = 0; i < WarmupScenarios; i++)
            RunScenario();

        var totalSamples = new double[MeasuredScenarios];
        var boardCommandSamples = new double[MeasuredScenarios];
        var boardingUpdateSamples = new double[MeasuredScenarios];
        var disembarkCommandSamples = new double[MeasuredScenarios];
        long allocatedBytes = 0;
        int boardedCount = 0;
        int disembarkedCount = 0;

        for (int i = 0; i < MeasuredScenarios; i++)
        {
            ScenarioMetrics metrics = RunScenario();
            totalSamples[i] = metrics.TotalMs;
            boardCommandSamples[i] = metrics.BoardCommandMs;
            boardingUpdateSamples[i] = metrics.BoardingUpdateMs;
            disembarkCommandSamples[i] = metrics.DisembarkCommandMs;
            allocatedBytes += metrics.AllocatedBytes;
            boardedCount += metrics.BoardedCount;
            disembarkedCount += metrics.DisembarkedCount;
        }

        Array.Sort(totalSamples);
        Array.Sort(boardCommandSamples);
        Array.Sort(boardingUpdateSamples);
        Array.Sort(disembarkCommandSamples);

        return new BatchMetrics
        {
            BatchIndex = batchIndex,
            BoardedCount = boardedCount,
            DisembarkedCount = disembarkedCount,
            AverageTotalMs = Average(totalSamples),
            P95TotalMs = PercentileSorted(totalSamples, 0.95d),
            P99TotalMs = PercentileSorted(totalSamples, 0.99d),
            MaxTotalMs = totalSamples[totalSamples.Length - 1],
            AverageBoardCommandMs = Average(boardCommandSamples),
            P95BoardCommandMs = PercentileSorted(boardCommandSamples, 0.95d),
            AverageBoardingUpdateMs = Average(boardingUpdateSamples),
            P95BoardingUpdateMs = PercentileSorted(boardingUpdateSamples, 0.95d),
            AverageDisembarkCommandMs = Average(disembarkCommandSamples),
            P95DisembarkCommandMs = PercentileSorted(disembarkCommandSamples, 0.95d),
            AllocatedBytes = allocatedBytes
        };
    }

    private static ScenarioMetrics RunScenario()
    {
        using World world = new("TransportBoardingPerformanceValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, out blockerCounts, out blocked, out occupied, out friendlyPassFactionIds);
            Entity transport = CreateTransport(em, new int2(20, 20));
            Entity[] passengers = CreateSelectedPassengers(em);

            var boardingCommandSystem = new TransportBoardingCommandSystem();
            var airPickupSystem = new UnitTransportAirPickupSystem();
            var moveOrderSystem = new UnitMoveOrderSystem();
            var selectionStateSystem = new SelectionStateCompositionSystemHelper();
            SystemHandle boardingCommandEcsSystem = world.CreateSystem<TransportBoardingCommandSystem>();
            SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
            boardingCommandSystem.EnsureEntityQueries(em);

            Entity queue = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            em.AddBuffer<RtsSelectionCommandResultElement>(queue);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);

            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long boardStartTicks = Stopwatch.GetTimestamp();
            TransportBoardingCommandSystem.Result boardResult = boardingCommandSystem.TryRequestBoardTransportOrderToClickedUnit(
                em,
                Vector2.zero,
                airPickupSystem,
                moveOrderSystem,
                selectionStateSystem,
                (Vector2 screenPosition, EntityManager entityManager, out Entity clicked) =>
                {
                    clicked = transport;
                    return true;
                },
                TryGetNoClickedCell);
            long boardStopTicks = Stopwatch.GetTimestamp();

            Assert.IsTrue(boardResult.Accepted, "Batch transport boarding command should be accepted.");
            Assert.AreEqual(PassengerCount, CountBoardingTargets(em, passengers), "Every selected passenger should receive a boarding target.");

            long boardingStartTicks = Stopwatch.GetTimestamp();
            boardingSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            long boardingStopTicks = Stopwatch.GetTimestamp();

            DynamicBuffer<UnitTransportPassengerElement> transportPassengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            Assert.AreEqual(PassengerCount, transportPassengers.Length, "Boarding update should load all selected passengers.");
            int boarded = transportPassengers.Length;
            for (int i = 0; i < passengers.Length; i++)
            {
                Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passengers[i]), $"Passenger {i} should be marked boarded.");
                Assert.IsTrue(em.HasComponent<Disabled>(passengers[i]), $"Passenger {i} should be disabled while boarded.");
            }

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                HasTargetEntity = 1,
                RequestId = 1,
                Frame = 1
            });

            long disembarkStartTicks = Stopwatch.GetTimestamp();
            boardingCommandEcsSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            long disembarkStopTicks = Stopwatch.GetTimestamp();
            long allocationStop = GC.GetAllocatedBytesForCurrentThread();

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);
            bool processed = requests.Length == 0;
            Assert.IsTrue(processed, "Disembark transport request should be processed.");
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(1, results[0].Accepted, "Disembark-all request should be accepted.");
            Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "Disembark-all should empty the transport passenger buffer.");

            int disembarked = 0;
            for (int i = 0; i < passengers.Length; i++)
            {
                if (!em.HasComponent<UnitTransportPassenger>(passengers[i]) &&
                    !em.HasComponent<Disabled>(passengers[i]))
                {
                    disembarked++;
                }
            }

            Assert.AreEqual(PassengerCount, disembarked, "Every boarded passenger should disembark.");

            double boardCommandMs = TicksToMilliseconds(boardStopTicks - boardStartTicks);
            double boardingUpdateMs = TicksToMilliseconds(boardingStopTicks - boardingStartTicks);
            double disembarkCommandMs = TicksToMilliseconds(disembarkStopTicks - disembarkStartTicks);
            return new ScenarioMetrics
            {
                TotalMs = boardCommandMs + boardingUpdateMs + disembarkCommandMs,
                BoardCommandMs = boardCommandMs,
                BoardingUpdateMs = boardingUpdateMs,
                DisembarkCommandMs = disembarkCommandMs,
                BoardedCount = boarded,
                DisembarkedCount = disembarked,
                AllocatedBytes = allocationStop - allocationStart
            };
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
        }
    }

    private static Entity CreateTransport(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(LocalToWorld),
            typeof(LocalTransform));
        em.SetName(entity, "TransportPerfGroundTransport");
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = PassengerCount });
        float3 position = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);
        return entity;
    }

    private static Entity[] CreateSelectedPassengers(EntityManager em)
    {
        Entity[] passengers = new Entity[PassengerCount];
        int2[] cells =
        {
            new(16, 20),
            new(17, 20),
            new(18, 20),
            new(22, 20),
            new(23, 20),
            new(24, 20),
            new(20, 16),
            new(20, 24)
        };

        for (int i = 0; i < passengers.Length; i++)
        {
            int2 cell = cells[i];
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(SelectedUnitTag),
                typeof(LocalTransform));
            em.SetName(entity, $"TransportPerfPassenger_{i}");
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = cell });
            em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
            em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
            passengers[i] = entity;
        }

        return passengers;
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
            typeof(DynamicOccupancyComponent));
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

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static int CountBoardingTargets(EntityManager em, Entity[] passengers)
    {
        int count = 0;
        for (int i = 0; i < passengers.Length; i++)
        {
            if (em.HasComponent<UnitTransportBoardingTarget>(passengers[i]))
                count++;
        }

        return count;
    }

    private static bool TryGetNoClickedUnit(Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static bool TryGetNoClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        return false;
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

    private static double Average(double[] sortedSamples)
    {
        if (sortedSamples.Length == 0)
            return 0d;

        double total = 0d;
        for (int i = 0; i < sortedSamples.Length; i++)
            total += sortedSamples[i];
        return total / sortedSamples.Length;
    }

    private static double TicksToMilliseconds(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private static int SelectMedianBatchIndex(BatchMetrics[] batches)
    {
        if (batches == null || batches.Length != MeasuredBatches)
            throw new InvalidOperationException($"Median selection requires exactly {MeasuredBatches} measured batches.");

        if (CompareBatchPerformance(batches[0], batches[1]) <= 0)
        {
            if (CompareBatchPerformance(batches[2], batches[0]) < 0)
                return 0;
            return CompareBatchPerformance(batches[2], batches[1]) > 0 ? 1 : 2;
        }

        if (CompareBatchPerformance(batches[2], batches[1]) < 0)
            return 1;
        return CompareBatchPerformance(batches[2], batches[0]) > 0 ? 0 : 2;
    }

    private static int CompareBatchPerformance(BatchMetrics left, BatchMetrics right)
    {
        int comparison = left.AverageTotalMs.CompareTo(right.AverageTotalMs);
        if (comparison != 0)
            return comparison;

        comparison = left.P95TotalMs.CompareTo(right.P95TotalMs);
        return comparison != 0 ? comparison : left.BatchIndex.CompareTo(right.BatchIndex);
    }

    private static void WriteReport(
        BatchMetrics[] batches,
        int selectedBatchIndex,
        long allocatedBytes)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        BatchMetrics selectedBatch = batches[selectedBatchIndex];
        var builder = new StringBuilder(4096);
        builder.AppendLine("{");
        AppendJson(builder, "warmupScenarios", WarmupScenarios, trailingComma: true);
        AppendJson(builder, "measuredScenarios", MeasuredScenarios, trailingComma: true);
        AppendJson(builder, "measuredBatches", MeasuredBatches, trailingComma: true);
        AppendJson(builder, "passengerCount", PassengerCount, trailingComma: true);
        AppendJson(builder, "selectedBatchIndex", selectedBatchIndex, trailingComma: true);
        AppendJson(builder, "nonRegressionMarginPercent", NonRegressionMarginPercent, trailingComma: true);
        AppendJson(builder, "am012AverageTotalMsBaseline", Am012AverageTotalMsBaseline, trailingComma: true);
        AppendJson(builder, "am012P95TotalMsBaseline", Am012P95TotalMsBaseline, trailingComma: true);
        AppendJson(builder, "averageTotalMsCeiling", AverageTotalMsCeiling, trailingComma: true);
        AppendJson(builder, "p95TotalMsCeiling", P95TotalMsCeiling, trailingComma: true);
        AppendJson(builder, "allocatedBytesCeiling", AllocatedBytesCeiling, trailingComma: true);
        AppendJson(builder, "boardedCount", selectedBatch.BoardedCount, trailingComma: true);
        AppendJson(builder, "disembarkedCount", selectedBatch.DisembarkedCount, trailingComma: true);
        AppendJson(builder, "averageTotalMs", selectedBatch.AverageTotalMs, trailingComma: true);
        AppendJson(builder, "p95TotalMs", selectedBatch.P95TotalMs, trailingComma: true);
        AppendJson(builder, "p99TotalMs", selectedBatch.P99TotalMs, trailingComma: true);
        AppendJson(builder, "maxTotalMs", selectedBatch.MaxTotalMs, trailingComma: true);
        AppendJson(builder, "averageBoardCommandMs", selectedBatch.AverageBoardCommandMs, trailingComma: true);
        AppendJson(builder, "p95BoardCommandMs", selectedBatch.P95BoardCommandMs, trailingComma: true);
        AppendJson(builder, "averageBoardingUpdateMs", selectedBatch.AverageBoardingUpdateMs, trailingComma: true);
        AppendJson(builder, "p95BoardingUpdateMs", selectedBatch.P95BoardingUpdateMs, trailingComma: true);
        AppendJson(builder, "averageDisembarkCommandMs", selectedBatch.AverageDisembarkCommandMs, trailingComma: true);
        AppendJson(builder, "p95DisembarkCommandMs", selectedBatch.P95DisembarkCommandMs, trailingComma: true);
        AppendJson(builder, "allocatedBytesCurrentThread", allocatedBytes, trailingComma: true);
        builder.AppendLine("  \"batches\": [");
        for (int i = 0; i < batches.Length; i++)
        {
            builder.AppendLine("    {");
            AppendBatchMetrics(builder, batches[i], indent: 6);
            builder.Append("    }").AppendLine(i < batches.Length - 1 ? "," : string.Empty);
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"selectedBatch\": {");
        AppendBatchMetrics(builder, selectedBatch, indent: 4);
        builder.AppendLine("  }");
        builder.AppendLine("}");
        File.WriteAllText(ReportPath, builder.ToString());
    }

    private static void AppendBatchMetrics(StringBuilder builder, BatchMetrics batch, int indent)
    {
        AppendJson(builder, "batchIndex", batch.BatchIndex, trailingComma: true, indent: indent);
        AppendJson(builder, "boardedCount", batch.BoardedCount, trailingComma: true, indent: indent);
        AppendJson(builder, "disembarkedCount", batch.DisembarkedCount, trailingComma: true, indent: indent);
        AppendJson(builder, "averageTotalMs", batch.AverageTotalMs, trailingComma: true, indent: indent);
        AppendJson(builder, "p95TotalMs", batch.P95TotalMs, trailingComma: true, indent: indent);
        AppendJson(builder, "p99TotalMs", batch.P99TotalMs, trailingComma: true, indent: indent);
        AppendJson(builder, "maxTotalMs", batch.MaxTotalMs, trailingComma: true, indent: indent);
        AppendJson(builder, "averageBoardCommandMs", batch.AverageBoardCommandMs, trailingComma: true, indent: indent);
        AppendJson(builder, "p95BoardCommandMs", batch.P95BoardCommandMs, trailingComma: true, indent: indent);
        AppendJson(builder, "averageBoardingUpdateMs", batch.AverageBoardingUpdateMs, trailingComma: true, indent: indent);
        AppendJson(builder, "p95BoardingUpdateMs", batch.P95BoardingUpdateMs, trailingComma: true, indent: indent);
        AppendJson(builder, "averageDisembarkCommandMs", batch.AverageDisembarkCommandMs, trailingComma: true, indent: indent);
        AppendJson(builder, "p95DisembarkCommandMs", batch.P95DisembarkCommandMs, trailingComma: true, indent: indent);
        AppendJson(builder, "allocatedBytesCurrentThread", batch.AllocatedBytes, trailingComma: false, indent: indent);
    }

    private static void AppendJson(StringBuilder builder, string name, int value, bool trailingComma, int indent = 2)
    {
        builder.Append(' ', indent).Append('"').Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, long value, bool trailingComma, int indent = 2)
    {
        builder.Append(' ', indent).Append('"').Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, double value, bool trailingComma, int indent = 2)
    {
        builder.Append(' ', indent).Append('"').Append(name).Append("\": ");
        builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private struct BatchMetrics
    {
        public int BatchIndex;
        public int BoardedCount;
        public int DisembarkedCount;
        public double AverageTotalMs;
        public double P95TotalMs;
        public double P99TotalMs;
        public double MaxTotalMs;
        public double AverageBoardCommandMs;
        public double P95BoardCommandMs;
        public double AverageBoardingUpdateMs;
        public double P95BoardingUpdateMs;
        public double AverageDisembarkCommandMs;
        public double P95DisembarkCommandMs;
        public long AllocatedBytes;
    }

    private struct ScenarioMetrics
    {
        public double TotalMs;
        public double BoardCommandMs;
        public double BoardingUpdateMs;
        public double DisembarkCommandMs;
        public int BoardedCount;
        public int DisembarkedCount;
        public long AllocatedBytes;
    }
}
#endif
