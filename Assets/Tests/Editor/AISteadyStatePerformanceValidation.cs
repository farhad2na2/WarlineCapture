using Game.Components;
using Game.Runtime;
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

public sealed class AISteadyStatePerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-ai-steady-state-performance.json";
    private const int EnemyUnitCount = 160;
    private const int PlayerThreatCount = 48;
    private const int PlayerBuildingCount = 64;
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 180;
    private const float FrameDeltaSeconds = 0.016f;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new AISteadyStatePerformanceValidation();
            tests.AISteadyStateUpdatesProduceOrdersAndReportTiming();
            Debug.Log("[AISteadyStatePerformanceValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[AISteadyStatePerformanceValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AISteadyStateUpdatesProduceOrdersAndReportTiming()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("AISteadyStatePerformanceValidation");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;

        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateFactionControls(em);
            CreateSquadPlan(em);
            CreateEnemyUnits(em);
            CreatePlayerTargets(em);

            SystemHandle squadSystem = world.CreateSystem<AISquadSystem>();
            SystemHandle targetingSystem = world.CreateSystem<AITargetingSystem>();
            SystemHandle combatOrderSystem = world.CreateSystem<AICombatOrderSystem>();

            RunFrames(world, squadSystem, targetingSystem, combatOrderSystem, WarmupFrames, 0);

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            var totalStopwatch = Stopwatch.StartNew();
            RunFrames(world, squadSystem, targetingSystem, combatOrderSystem, MeasuredFrames, WarmupFrames, samples);
            totalStopwatch.Stop();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            int squadCount = Count(em, ComponentType.ReadOnly<AISquad>());
            int engageOrderCount = Count(em, ComponentType.ReadOnly<EngageTarget>());
            int aiCombatOrderCount = Count(em, ComponentType.ReadOnly<AICombatOrderTag>());
            Assert.GreaterOrEqual(squadCount, 8, "Steady-state AI fixture should form enough squads to exercise targeting/combat loops.");
            Assert.Greater(engageOrderCount, 0, "Steady-state AI fixture should issue engage orders.");
            Assert.Greater(aiCombatOrderCount, 0, "Steady-state AI fixture should tag AI combat orders.");

            Array.Sort(samples);
            double totalMs = totalStopwatch.Elapsed.TotalMilliseconds;
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
                squadCount,
                engageOrderCount,
                aiCombatOrderCount);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (world.IsCreated)
                world.Dispose();
        }
    }

    private static void RunFrames(
        World world,
        SystemHandle squadSystem,
        SystemHandle targetingSystem,
        SystemHandle combatOrderSystem,
        int frames,
        int frameOffset,
        double[] samples = null)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            double elapsed = (frameOffset + frame + 1) * FrameDeltaSeconds;
            world.SetTime(new TimeData(elapsed, FrameDeltaSeconds));
            var stopwatch = Stopwatch.StartNew();
            squadSystem.Update(world.Unmanaged);
            targetingSystem.Update(world.Unmanaged);
            combatOrderSystem.Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
            stopwatch.Stop();
            if (samples != null)
                samples[frame] = stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    private static void CreateFactionControls(EntityManager em)
    {
        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.PlayerFactionId, AIControlled = 0, IsPlayerFaction = 1 });
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId, AIControlled = 1, IsPlayerFaction = 0 });
    }

    private static void CreateSquadPlan(EntityManager em)
    {
        Entity planEntity = em.CreateEntity(typeof(AISquadPlan));
        em.SetComponentData(planEntity, new AISquadPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            MinUnits = 4,
            MaxUnits = 8,
            MaxActiveSquads = 16,
            NextSquadId = 1,
            LastLogTime = -999f
        });
    }

    private static void CreateEnemyUnits(EntityManager em)
    {
        for (int i = 0; i < EnemyUnitCount; i++)
        {
            int row = i / 20;
            int column = i % 20;
            int2 cell = new(8 + column, 12 + row);
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(AIControlledTag),
                typeof(LocalTransform));
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = cell });
            em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 12, ChaseBreakDistance = 24f });
            em.SetComponentData(entity, new UnitAttack { Range = 8f, CooldownSeconds = 1f, Damage = 10 });
            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        }
    }

    private static void CreatePlayerTargets(EntityManager em)
    {
        for (int i = 0; i < PlayerThreatCount; i++)
        {
            int row = i / 12;
            int column = i % 12;
            int2 cell = new(56 + column, 18 + row);
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(LocalTransform));
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = cell });
            em.SetComponentData(entity, new UnitHealth { Current = 150, Max = 150 });
            em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 12, ChaseBreakDistance = 24f });
            em.SetComponentData(entity, new UnitAttack { Range = 8f, CooldownSeconds = 1f, Damage = 12 });
            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        }

        for (int i = 0; i < PlayerBuildingCount; i++)
        {
            int row = i / 16;
            int column = i % 16;
            int2 origin = new(70 + column * 3, 34 + row * 3);
            int2 footprint = new(2, 2);
            int2 center = origin + footprint / 2;
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(StaticGridBlocker),
                typeof(RuntimeBuildingCombatTag),
                typeof(RuntimeBuildingCombatInfo),
                typeof(LocalTransform));
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = center });
            em.SetComponentData(entity, new UnitHealth { Current = 600, Max = 600 });
            em.SetComponentData(entity, new RuntimeBuildingCombatInfo
            {
                RuntimeBuildingId = i + 1,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = origin,
                FootprintCells = footprint
            });
            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(center.x, 0f, center.y)));
        }
    }

    private static int Count(EntityManager em, ComponentType componentType)
    {
        using EntityQuery query = em.CreateEntityQuery(componentType);
        return query.CalculateEntityCount();
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

    private static void WriteReport(
        double totalMs,
        double averageMs,
        double p95Ms,
        double p99Ms,
        double maxMs,
        long allocatedBytes,
        int squadCount,
        int engageOrderCount,
        int aiCombatOrderCount)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder(512);
        builder.AppendLine("{");
        AppendJson(builder, "warmupFrames", WarmupFrames, trailingComma: true);
        AppendJson(builder, "measuredFrames", MeasuredFrames, trailingComma: true);
        AppendJson(builder, "enemyUnitCount", EnemyUnitCount, trailingComma: true);
        AppendJson(builder, "playerThreatCount", PlayerThreatCount, trailingComma: true);
        AppendJson(builder, "playerBuildingCount", PlayerBuildingCount, trailingComma: true);
        AppendJson(builder, "squadCount", squadCount, trailingComma: true);
        AppendJson(builder, "engageOrderCount", engageOrderCount, trailingComma: true);
        AppendJson(builder, "aiCombatOrderCount", aiCombatOrderCount, trailingComma: true);
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
