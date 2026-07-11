using Game.Components;
using Game.Runtime;
using Game.Runtime.Combat;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class AssistantPerformanceDiagnosticsValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-aria-assistant-performance.json";
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 240;
    private const float FrameDeltaSeconds = 0.016f;
    private const double AverageBudgetMs = 0.25d;
    private const double P95BudgetMs = 0.75d;
    private const long AllocationBudgetBytes = 0;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new AssistantPerformanceDiagnosticsValidation();
            tests.AssistantSteadyStateUpdatesStayUnderTimingAndGcBudgets();
            Debug.Log("[AssistantPerformanceDiagnosticsValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AssistantPerformanceDiagnosticsValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AssistantSteadyStateUpdatesStayUnderTimingAndGcBudgets()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new("AssistantPerformanceDiagnosticsValidation");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;

        try
        {
            Entity boundary = CreateBoundary(em);
            CreateCombatEntities(em, out Entity[] friendlies, out Entity[] hostiles);
            CreateFocusedSelectedUnit(em, friendlies[0]);
            AddObjectives(em, boundary, hostiles[0]);
            Entity observationQueue = CreateObservationQueue(em);

            SystemHandle goalSystem = world.CreateSystem<AssistantGoalReadModelSystem>();
            SystemHandle threatSystem = world.CreateSystem<AssistantThreatReadModelSystem>();
            SystemHandle recommendationSystem = world.CreateSystem<AssistantRecommendationSystem>();
            SystemHandle targetLockSystem = world.CreateSystem<AssistantTargetLockReadModelSystem>();
            SystemHandle messageSystem = world.CreateSystem<AssistantMessagePrioritySystem>();
            SystemHandle narrationSystem = world.CreateSystem<AssistantNarrationRequestSystem>();
            SystemHandle commandIntentSystem = world.CreateSystem<AssistantCommandIntentSystem>();
            SystemHandle controlOwnerSystem = world.CreateSystem<AssistantControlOwnerSystem>();

            world.SetTime(new TimeData(FrameDeltaSeconds, FrameDeltaSeconds));
            goalSystem.Update(world.Unmanaged);
            threatSystem.Update(world.Unmanaged);
            AppendSaturatedObservations(em, observationQueue, friendlies, hostiles);

            RunFrames(
                world,
                goalSystem,
                threatSystem,
                recommendationSystem,
                targetLockSystem,
                messageSystem,
                narrationSystem,
                commandIntentSystem,
                controlOwnerSystem,
                WarmupFrames,
                1,
                samples: null);

            DynamicBuffer<AssistantGoalReadModelElement> goals =
                em.GetBuffer<AssistantGoalReadModelElement>(boundary);
            DynamicBuffer<AssistantThreatReadModelElement> threats =
                em.GetBuffer<AssistantThreatReadModelElement>(boundary);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                em.GetBuffer<AssistantRecommendationElement>(boundary);
            DynamicBuffer<AssistantMessageElement> messages =
                em.GetBuffer<AssistantMessageElement>(boundary);
            DynamicBuffer<AssistantNarrationRequestElement> narrationRequests =
                em.GetBuffer<AssistantNarrationRequestElement>(boundary);
            Assert.AreEqual(3, goals.Length, "The saturated fixture must retain all three bounded goal rows.");
            Assert.AreEqual(4, threats.Length, "The 64-row observation ring must coalesce to four visible threats.");
            Assert.AreEqual(1, recommendations.Length, "The saturated fixture must publish one top recommendation.");
            Assert.AreEqual(5, messages.Length, "Four threats plus one command report must fill the five visible message slots.");
            Assert.AreEqual(1, narrationRequests.Length, "Duplicate suppression must retain one narration row.");
            int goalCount = goals.Length;
            int threatCount = threats.Length;
            int recommendationCount = recommendations.Length;
            int messageCount = messages.Length;
            int narrationRequestCount = narrationRequests.Length;

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long totalStartTicks = Stopwatch.GetTimestamp();
            RunFrames(
                world,
                goalSystem,
                threatSystem,
                recommendationSystem,
                targetLockSystem,
                messageSystem,
                narrationSystem,
                commandIntentSystem,
                controlOwnerSystem,
                MeasuredFrames,
                WarmupFrames + 1,
                samples);
            long totalStopTicks = Stopwatch.GetTimestamp();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

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
                goalCount,
                threatCount,
                recommendationCount,
                messageCount,
                narrationRequestCount);

            Assert.LessOrEqual(averageMs, AverageBudgetMs, $"Assistant steady-state average update cost exceeded {AverageBudgetMs:0.###} ms. See {ReportPath}.");
            Assert.LessOrEqual(p95Ms, P95BudgetMs, $"Assistant steady-state p95 update cost exceeded {P95BudgetMs:0.###} ms. See {ReportPath}.");
            Assert.LessOrEqual(allocatedBytes, AllocationBudgetBytes, $"Assistant steady-state updates allocated too much managed memory. See {ReportPath}.");
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
        SystemHandle goalSystem,
        SystemHandle threatSystem,
        SystemHandle recommendationSystem,
        SystemHandle targetLockSystem,
        SystemHandle messageSystem,
        SystemHandle narrationSystem,
        SystemHandle commandIntentSystem,
        SystemHandle controlOwnerSystem,
        int frames,
        int frameOffset,
        double[] samples)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            double elapsed = (frameOffset + frame + 1) * FrameDeltaSeconds;
            world.SetTime(new TimeData(elapsed, FrameDeltaSeconds));
            long startTicks = Stopwatch.GetTimestamp();
            goalSystem.Update(world.Unmanaged);
            threatSystem.Update(world.Unmanaged);
            recommendationSystem.Update(world.Unmanaged);
            targetLockSystem.Update(world.Unmanaged);
            messageSystem.Update(world.Unmanaged);
            narrationSystem.Update(world.Unmanaged);
            commandIntentSystem.Update(world.Unmanaged);
            controlOwnerSystem.Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
            long stopTicks = Stopwatch.GetTimestamp();

            if (samples != null)
                samples[frame] = TicksToMilliseconds(stopTicks - startTicks);
        }
    }

    private static Entity CreateBoundary(EntityManager em)
    {
        Entity boundary = em.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent),
            typeof(AssistantSettingsComponent),
            typeof(MatchObjectiveRuntimeStateComponent));
        em.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match,
            Phase = UiShellTransitionPhase.MatchHudReady,
            TransitionSequenceId = 1
        });
        em.SetComponentData(boundary, new UiMatchHudStatusSurfacesComponent());
        em.SetComponentData(boundary, Header());
        em.SetComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = AssistantNarrationMode.Important,
            AllowTakeover = 1,
            SubtitlesEnabled = 1
        });
        em.SetComponentData(boundary, new MatchObjectiveRuntimeStateComponent
        {
            Version = 1,
            MissionId = new FixedString64Bytes("performance.fixture"),
            MatchStartedAt = 0f,
            MatchActive = 1
        });
        em.AddBuffer<MatchObjectiveRuntimeElement>(boundary);
        em.AddBuffer<AssistantCommandIntentRequestElement>(boundary);
        DynamicBuffer<AssistantCommandIntentResultElement> results =
            em.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        results.Add(new AssistantCommandIntentResultElement
        {
            RequestId = 99,
            Status = AssistantCommandIntentStatus.Completed,
            ReasonCode = (int)TacticalCommandReasonCode.None,
            Message = new FixedString64Bytes("Move order completed")
        });
        DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> fuelSummaries =
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
        fuelSummaries.Add(new BuildingRuntimeFactionUsableFuelSummary
        {
            FactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = 30f,
            StoredFuelBarrels = 500f,
            CurrentFuelBarrels = 500f,
            FuelProducedBarrels = 0f,
            FuelDeliveredBarrels = 0f,
            OilStorageCapacity = 1000,
            FuelStorageCapacity = 1000,
            Version = 17u
        });
        Entity matchStart = em.CreateEntity(
            typeof(MatchStartStateComponent),
            typeof(MatchStartQueueComponent));
        em.SetComponentData(matchStart, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
        return boundary;
    }

    private static void CreateFocusedSelectedUnit(EntityManager em, Entity unit)
    {
        em.AddComponent<SelectedUnitTag>(unit);
        Entity readModel = em.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
        em.SetComponentData(readModel, new FocusedUnitUiReadModelComponent
        {
            FocusedUnit = unit,
            HasFocusedUnit = 1,
            OwnedByPlayer = 1,
            CanAttack = 1,
            CanHold = 1,
            CanStop = 1,
            CanScan = 1,
            CommandStateVersion = 9,
            Label = new FixedString64Bytes("Rifle Squad"),
            Description = new FixedString128Bytes("Infantry")
        });
    }

    private static void AddObjectives(EntityManager em, Entity boundary, Entity hostileTarget)
    {
        DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
            em.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 1,
            ObjectiveId = new FixedString64Bytes("secure.relay"),
            State = MatchObjectiveState.Active,
            Priority = (byte)AssistantMessagePriority.High,
            IsPrimary = 1,
            Title = new FixedString64Bytes("Secure the relay"),
            Body = new FixedString128Bytes("Move to the verified relay position."),
            TargetCell = new int2(12, 8),
            WorldPosition = new float3(12f, 0f, 8f),
            HasTargetCell = 1,
            HasWorldPosition = 1
        });
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 2,
            ObjectiveId = new FixedString64Bytes("disable.hostile"),
            State = MatchObjectiveState.Warning,
            Priority = (byte)AssistantMessagePriority.Critical,
            Title = new FixedString64Bytes("Disable hostile armor"),
            Body = new FixedString128Bytes("Destroy the verified hostile target."),
            TargetEntity = hostileTarget,
            WorldPosition = new float3(18f, 0f, 4f),
            HasWorldPosition = 1
        });
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 3,
            ObjectiveId = new FixedString64Bytes("hold.position"),
            State = MatchObjectiveState.Active,
            Priority = (byte)AssistantMessagePriority.Normal,
            Title = new FixedString64Bytes("Hold the perimeter"),
            Body = new FixedString128Bytes("Maintain control of the protected area.")
        });
    }

    private static Entity CreateObservationQueue(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadWrite<CombatDamageObservationQueueComponent>());
        return CombatDamageObservationUtility.EnsureQueue(em, query);
    }

    private static void CreateCombatEntities(
        EntityManager em,
        out Entity[] friendlies,
        out Entity[] hostiles)
    {
        friendlies = new Entity[4];
        hostiles = new Entity[4];
        for (int i = 0; i < 4; i++)
        {
            friendlies[i] = CreateCombatEntity(
                em,
                FactionIdentity.PlayerFactionId,
                new FixedString64Bytes("Friendly " + (i + 1)),
                new float3(i * 3f, 0f, 0f));
            hostiles[i] = CreateCombatEntity(
                em,
                FactionIdentity.EnemyFactionId,
                new FixedString64Bytes("Hostile " + (i + 1)),
                new float3(16f + i * 2f, 0f, 4f));
        }
    }

    private static Entity CreateCombatEntity(
        EntityManager em,
        byte factionId,
        FixedString64Bytes name,
        float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(UnitDisplayInfo),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitDisplayInfo { Name = name });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static void AppendSaturatedObservations(
        EntityManager em,
        Entity queue,
        Entity[] friendlies,
        Entity[] hostiles)
    {
        for (int i = 0; i < CombatDamageObservationUtility.Capacity; i++)
        {
            int index = i & 3;
            float3 friendlyPosition = em.GetComponentData<LocalTransform>(friendlies[index]).Position;
            float3 hostilePosition = em.GetComponentData<LocalTransform>(hostiles[index]).Position;
            Assert.IsTrue(CombatDamageObservationUtility.Append(
                em,
                queue,
                hostiles[index],
                friendlies[index],
                index switch
                {
                    1 => CombatDamageSourceKind.BuildingDefense,
                    2 => CombatDamageSourceKind.GroundMissile,
                    3 => CombatDamageSourceKind.AirMissile,
                    _ => CombatDamageSourceKind.DirectFire
                },
                previousHealth: 100,
                currentHealth: 70 - index * 5,
                targetMaxHealth: 100,
                observedAt: 0.02f + i * 0.0001f,
                hostilePosition,
                friendlyPosition));
        }
    }

    private static UiMatchHudHeaderComponent Header()
    {
        return new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("MOVE ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("9,750"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        };
    }

    private static void WriteReport(
        double totalMs,
        double averageMs,
        double p95Ms,
        double p99Ms,
        double maxMs,
        long allocatedBytes,
        int goalCount,
        int threatCount,
        int recommendationCount,
        int messageCount,
        int narrationRequestCount)
    {
        string report =
            "{\n" +
            $"  \"warmupFrames\": {WarmupFrames},\n" +
            $"  \"measuredFrames\": {MeasuredFrames},\n" +
            $"  \"totalMs\": {Format(totalMs)},\n" +
            $"  \"averageMs\": {Format(averageMs)},\n" +
            $"  \"p95Ms\": {Format(p95Ms)},\n" +
            $"  \"p99Ms\": {Format(p99Ms)},\n" +
            $"  \"maxMs\": {Format(maxMs)},\n" +
            $"  \"allocatedBytes\": {allocatedBytes},\n" +
            $"  \"allocationBudgetBytes\": {AllocationBudgetBytes},\n" +
            $"  \"goalCount\": {goalCount},\n" +
            $"  \"threatCount\": {threatCount},\n" +
            $"  \"recommendationCount\": {recommendationCount},\n" +
            $"  \"messageCount\": {messageCount},\n" +
            $"  \"narrationRequestCount\": {narrationRequestCount}\n" +
            "}\n";
        File.WriteAllText(ReportPath, report);
        Debug.Log($"[AssistantPerformanceDiagnosticsValidation] report={ReportPath} averageMs={Format(averageMs)} p95Ms={Format(p95Ms)} allocatedBytes={allocatedBytes}");
    }

    private static double PercentileSorted(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
            return 0d;

        double position = (sorted.Length - 1) * percentile;
        int lower = Mathf.FloorToInt((float)position);
        int upper = Mathf.CeilToInt((float)position);
        if (lower == upper)
            return sorted[lower];

        double t = position - lower;
        return sorted[lower] + (sorted[upper] - sorted[lower]) * t;
    }

    private static double TicksToMilliseconds(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private static string Format(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
#endif
