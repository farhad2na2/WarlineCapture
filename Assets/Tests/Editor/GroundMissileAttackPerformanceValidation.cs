#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class GroundMissileAttackPerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-ground-missile-attack-performance.json";
    private const int WarmupScenarios = 16;
    private const int MeasuredScenarios = 64;
    private const int MaxFramesPerScenario = 64;
    private const float FrameDeltaSeconds = 0.1f;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new GroundMissileAttackPerformanceValidation();
            tests.GroundMissileAttackCommandLaunchesImpactsAndReportsTiming();
            Debug.Log("[GroundMissileAttackPerformanceValidation] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[GroundMissileAttackPerformanceValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void GroundMissileAttackCommandLaunchesImpactsAndReportsTiming()
    {
        for (int i = 0; i < WarmupScenarios; i++)
            RunScenario();

        var totalSamples = new double[MeasuredScenarios];
        var orderSamples = new double[MeasuredScenarios];
        var simulationSamples = new double[MeasuredScenarios];
        long allocatedBytes = 0;
        int totalFrames = 0;
        int maxFrames = 0;
        for (int i = 0; i < MeasuredScenarios; i++)
        {
            ScenarioMetrics metrics = RunScenario();
            totalSamples[i] = metrics.TotalMs;
            orderSamples[i] = metrics.OrderMs;
            simulationSamples[i] = metrics.SimulationMs;
            allocatedBytes += metrics.AllocatedBytes;
            totalFrames += metrics.Frames;
            maxFrames = math.max(maxFrames, metrics.Frames);
        }

        Array.Sort(totalSamples);
        Array.Sort(orderSamples);
        Array.Sort(simulationSamples);
        WriteReport(
            totalSamples,
            orderSamples,
            simulationSamples,
            allocatedBytes,
            totalFrames,
            maxFrames);
    }

    private static ScenarioMetrics RunScenario()
    {
        using World world = new("GroundMissileAttackPerformanceValidation");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity target = CreateRuntimeBuildingTarget(
            em,
            originCell: new int2(120, 8),
            footprintCells: new int2(4, 4),
            position: new float3(122f, 0f, 10f),
            factionId: FactionIdentitySystem.EnemyFactionId,
            health: 250);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        em.AddComponent<SelectedUnitTag>(launcher);

        var attackOrderCommandSystem = new AttackOrderCommandSystem();
        attackOrderCommandSystem.EnsureEntityQueries(em);

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
        SystemHandle flightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
        SystemHandle impactSystem = world.CreateSystem<GroundMissileImpactSystem>();
        em.CompleteAllTrackedJobs();

        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        long orderStartTicks = Stopwatch.GetTimestamp();
        AttackOrderCommandSystem.Result orderResult =
            attackOrderCommandSystem.IssueAttackTarget(em, target);
        long orderStopTicks = Stopwatch.GetTimestamp();

        Assert.IsTrue(orderResult.Issued, "Attack command should issue a ground missile order.");
        Assert.IsTrue(orderResult.CommandResult.Accepted, "Ground missile attack command should be accepted.");
        Assert.AreEqual("Missile launched.", orderResult.CommandResult.Message);
        Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));

        int frames = 0;
        double simulationMs = 0d;
        double elapsed = 0d;
        while (frames < MaxFramesPerScenario && em.GetComponentData<UnitHealth>(target).Current > 0)
        {
            frames++;
            elapsed += FrameDeltaSeconds;
            world.SetTime(new TimeData(elapsed, FrameDeltaSeconds));

            long frameStartTicks = Stopwatch.GetTimestamp();
            attackSystem.Update(world.Unmanaged);
            fireSystem.Update(world.Unmanaged);
            flightSystem.Update(world.Unmanaged);
            impactSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            long frameStopTicks = Stopwatch.GetTimestamp();
            simulationMs += TicksToMilliseconds(frameStopTicks - frameStartTicks);
        }

        long allocationStop = GC.GetAllocatedBytesForCurrentThread();
        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(target).Current, "Ground missile impact should destroy the target.");
        Assert.IsFalse(em.HasComponent<GroundMissileInFlightComponent>(launcher), "Launcher should clear in-flight state after impact.");
        Assert.Greater(frames, 0, "Scenario should run at least one simulation frame.");
        Assert.Less(frames, MaxFramesPerScenario, "Ground missile should impact before the scenario guard expires.");

        double orderMs = TicksToMilliseconds(orderStopTicks - orderStartTicks);
        return new ScenarioMetrics
        {
            TotalMs = orderMs + simulationMs,
            OrderMs = orderMs,
            SimulationMs = simulationMs,
            Frames = frames,
            AllocatedBytes = allocationStop - allocationStart
        };
    }

    private static Entity CreateLauncher(EntityManager em, float3 position, float prepareSeconds, float reloadSeconds)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(GroundMissileLauncherComponent),
            typeof(GroundMissileLauncherStateComponent),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = GridUtils.WorldToCell(CreateGridConfig(), position) });
        em.SetComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 2f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(entity, new UnitHealth { Current = 450, Max = 450 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 120, ChaseBreakDistance = 120f });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 600f,
            CooldownSeconds = 3f,
            Damage = 100,
            TraceVisibleSeconds = 0.08f
        });
        em.SetComponentData(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        em.SetComponentData(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        em.SetComponentData(entity, new UnitAttackAnimationComponent { TimeRemaining = 0f });
        em.SetComponentData(entity, new GroundMissileLauncherComponent
        {
            MinRange = 5f,
            MaxRange = 600f,
            PrepareSeconds = prepareSeconds,
            ReloadSeconds = reloadSeconds,
            BatteryElevatedAngleDegrees = -30f,
            RocketSpeed = 100f,
            ArcHeight = 10f,
            DamageRadius = 5f,
            Damage = 300
        });
        em.SetComponentData(entity, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetCell = default,
            TargetWorldPosition = default,
            Timer = 0f,
            SelectedRocketSlot = -1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateRuntimeBuildingTarget(
        EntityManager em,
        int2 originCell,
        int2 footprintCells,
        float3 position,
        byte factionId,
        int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitRespawnPrefab),
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = originCell + footprintCells / 2 });
        em.SetComponentData(entity, new UnitFootprint { Size = footprintCells });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.SetComponentData(entity, new RuntimeBuildingCombatInfo
        {
            OwnerFactionId = factionId,
            OriginCell = originCell,
            FootprintCells = footprintCells,
            IsWall = 0,
            IsGate = 0
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static GridConfig CreateGridConfig()
    {
        return new GridConfig
        {
            Width = 256,
            Height = 256,
            CellSize = 1f,
            Origin = float3.zero
        };
    }

    private static void CreateGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, CreateGridConfig());
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
        double[] orderSamples,
        double[] simulationSamples,
        long allocatedBytes,
        int totalFrames,
        int maxFrames)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder(768);
        builder.AppendLine("{");
        AppendJson(builder, "warmupScenarios", WarmupScenarios, trailingComma: true);
        AppendJson(builder, "measuredScenarios", MeasuredScenarios, trailingComma: true);
        AppendJson(builder, "averageFrames", (double)totalFrames / MeasuredScenarios, trailingComma: true);
        AppendJson(builder, "maxFrames", maxFrames, trailingComma: true);
        AppendJson(builder, "averageTotalMs", Average(totalSamples), trailingComma: true);
        AppendJson(builder, "p95TotalMs", PercentileSorted(totalSamples, 0.95d), trailingComma: true);
        AppendJson(builder, "p99TotalMs", PercentileSorted(totalSamples, 0.99d), trailingComma: true);
        AppendJson(builder, "maxTotalMs", totalSamples[totalSamples.Length - 1], trailingComma: true);
        AppendJson(builder, "averageOrderMs", Average(orderSamples), trailingComma: true);
        AppendJson(builder, "p95OrderMs", PercentileSorted(orderSamples, 0.95d), trailingComma: true);
        AppendJson(builder, "averageSimulationMs", Average(simulationSamples), trailingComma: true);
        AppendJson(builder, "p95SimulationMs", PercentileSorted(simulationSamples, 0.95d), trailingComma: true);
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
        public double OrderMs;
        public double SimulationMs;
        public int Frames;
        public long AllocatedBytes;
    }
}
#endif
