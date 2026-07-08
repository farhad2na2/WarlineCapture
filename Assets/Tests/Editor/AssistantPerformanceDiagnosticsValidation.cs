using Game.Components;
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
    private const long AllocationBudgetBytes = 4096;

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
            CreateFocusedSelectedUnit(em);

            SystemHandle goalSystem = world.CreateSystem<AssistantGoalReadModelSystem>();
            SystemHandle recommendationSystem = world.CreateSystem<AssistantRecommendationSystem>();
            SystemHandle messageSystem = world.CreateSystem<AssistantMessagePrioritySystem>();
            SystemHandle narrationSystem = world.CreateSystem<AssistantNarrationRequestSystem>();
            SystemHandle commandIntentSystem = world.CreateSystem<AssistantCommandIntentSystem>();
            SystemHandle controlOwnerSystem = world.CreateSystem<AssistantControlOwnerSystem>();

            RunFrames(
                world,
                goalSystem,
                recommendationSystem,
                messageSystem,
                narrationSystem,
                commandIntentSystem,
                controlOwnerSystem,
                WarmupFrames,
                0,
                samples: null);

            DynamicBuffer<AssistantRecommendationElement> recommendations =
                em.GetBuffer<AssistantRecommendationElement>(boundary);
            DynamicBuffer<AssistantMessageElement> messages =
                em.GetBuffer<AssistantMessageElement>(boundary);
            DynamicBuffer<AssistantNarrationRequestElement> narrationRequests =
                em.GetBuffer<AssistantNarrationRequestElement>(boundary);
            Assert.Greater(recommendations.Length, 0, "Warmup should publish a recommendation so the measured path covers populated assistant read models.");
            Assert.Greater(messages.Length, 0, "Warmup should publish assistant messages so the measured path covers message priority checks.");
            Assert.Greater(narrationRequests.Length, 0, "Warmup should publish one narration request so duplicate/coalescing checks are exercised.");
            int recommendationCount = recommendations.Length;
            int messageCount = messages.Length;
            int narrationRequestCount = narrationRequests.Length;

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long totalStartTicks = Stopwatch.GetTimestamp();
            RunFrames(
                world,
                goalSystem,
                recommendationSystem,
                messageSystem,
                narrationSystem,
                commandIntentSystem,
                controlOwnerSystem,
                MeasuredFrames,
                WarmupFrames,
                samples);
            long totalStopTicks = Stopwatch.GetTimestamp();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            Array.Sort(samples);
            double totalMs = TicksToMilliseconds(totalStopTicks - totalStartTicks);
            double averageMs = totalMs / MeasuredFrames;
            double p95Ms = PercentileSorted(samples, 0.95d);
            double p99Ms = PercentileSorted(samples, 0.99d);
            double maxMs = samples[samples.Length - 1];

            WriteReport(totalMs, averageMs, p95Ms, p99Ms, maxMs, allocatedBytes, recommendationCount, messageCount, narrationRequestCount);

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
        SystemHandle recommendationSystem,
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
            recommendationSystem.Update(world.Unmanaged);
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
            typeof(AssistantSettingsComponent));
        em.SetComponentData(boundary, StatusWithThreatAndFeedback());
        em.SetComponentData(boundary, Header());
        em.SetComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = AssistantNarrationMode.Important,
            AllowTakeover = 1,
            SubtitlesEnabled = 1
        });
        em.AddBuffer<AssistantCommandIntentRequestElement>(boundary);
        em.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> fuelSummaries =
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
        fuelSummaries.Add(new BuildingRuntimeFactionUsableFuelSummary
        {
            FactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = 30f,
            StoredFuelBarrels = 0f,
            CurrentFuelBarrels = 0f,
            FuelProducedBarrels = 0f,
            FuelDeliveredBarrels = 0f,
            OilStorageCapacity = 1000,
            FuelStorageCapacity = 1000,
            Version = 17u
        });
        return boundary;
    }

    private static void CreateFocusedSelectedUnit(EntityManager em)
    {
        Entity unit = em.CreateEntity(typeof(SelectedUnitTag));
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

    private static UiMatchHudStatusSurfacesComponent StatusWithThreatAndFeedback()
    {
        return new UiMatchHudStatusSurfacesComponent
        {
            ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
            Objective0Text = new FixedString64Bytes("Neutralize hostile patrol"),
            Objective1Text = new FixedString64Bytes("Protect civilians"),
            Objective2Text = new FixedString64Bytes("Keep losses low"),
            Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
            Objective1IconKind = UiMatchHudObjectiveIconKind.Checked,
            Objective2IconKind = UiMatchHudObjectiveIconKind.Star,
            ElapsedText = new FixedString32Bytes("00:30"),
            ThreatVisible = 1,
            ThreatTitle = new FixedString64Bytes("Hostile patrol"),
            ThreatSubtitle = new FixedString64Bytes("North gate"),
            FeedbackVisible = 1,
            FeedbackText = new FixedString64Bytes("Blocked: civilian zone")
        };
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
