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
    private const int PassengerCount = 8;
    private const int GridWidth = 64;
    private const int GridHeight = 64;

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

        WriteReport(
            totalSamples,
            boardCommandSamples,
            boardingUpdateSamples,
            disembarkCommandSamples,
            allocatedBytes,
            boardedCount,
            disembarkedCount);
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

    private static void WriteReport(
        double[] totalSamples,
        double[] boardCommandSamples,
        double[] boardingUpdateSamples,
        double[] disembarkCommandSamples,
        long allocatedBytes,
        int boardedCount,
        int disembarkedCount)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder(1024);
        builder.AppendLine("{");
        AppendJson(builder, "warmupScenarios", WarmupScenarios, trailingComma: true);
        AppendJson(builder, "measuredScenarios", MeasuredScenarios, trailingComma: true);
        AppendJson(builder, "passengerCount", PassengerCount, trailingComma: true);
        AppendJson(builder, "boardedCount", boardedCount, trailingComma: true);
        AppendJson(builder, "disembarkedCount", disembarkedCount, trailingComma: true);
        AppendJson(builder, "averageTotalMs", Average(totalSamples), trailingComma: true);
        AppendJson(builder, "p95TotalMs", PercentileSorted(totalSamples, 0.95d), trailingComma: true);
        AppendJson(builder, "p99TotalMs", PercentileSorted(totalSamples, 0.99d), trailingComma: true);
        AppendJson(builder, "maxTotalMs", totalSamples[totalSamples.Length - 1], trailingComma: true);
        AppendJson(builder, "averageBoardCommandMs", Average(boardCommandSamples), trailingComma: true);
        AppendJson(builder, "p95BoardCommandMs", PercentileSorted(boardCommandSamples, 0.95d), trailingComma: true);
        AppendJson(builder, "averageBoardingUpdateMs", Average(boardingUpdateSamples), trailingComma: true);
        AppendJson(builder, "p95BoardingUpdateMs", PercentileSorted(boardingUpdateSamples, 0.95d), trailingComma: true);
        AppendJson(builder, "averageDisembarkCommandMs", Average(disembarkCommandSamples), trailingComma: true);
        AppendJson(builder, "p95DisembarkCommandMs", PercentileSorted(disembarkCommandSamples, 0.95d), trailingComma: true);
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
