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
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class ResourceExchangeSteadyStatePerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-resource-exchange-steady-state-performance.json";
    private const int WarmupFrames = 64;
    private const int MeasuredFrames = 240;
    private const int ActiveQueueItems = 4;
    private const float FrameDeltaSeconds = 1f / 60f;
    private const double MaxP95Ms = 10d;
    private const double MaxP99Ms = 20d;
    private const long MaxAllocatedBytes = 0;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new ResourceExchangeSteadyStatePerformanceValidation();
            tests.ActiveExchangeSteadyStateStaysWithinPerformanceBudget();
            Debug.Log("[ResourceExchangeSteadyStatePerformanceValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ResourceExchangeSteadyStatePerformanceValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ActiveExchangeSteadyStateStaysWithinPerformanceBudget()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World world = new(nameof(ResourceExchangeSteadyStatePerformanceValidation));
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;

        try
        {
            Entity exchange = CreateActiveExchangeEntity(em);
            SystemHandle validationSystem = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
            SystemHandle queueTickSystem = world.CreateSystem<ResourceExchangeQueueTickSystem>();
            SystemHandle visualCueSystem = world.CreateSystem<ResourceExchangeVisualCueSystem>();

            RunFrames(world, validationSystem, queueTickSystem, visualCueSystem, WarmupFrames, 0);

            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests =
                em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
            DynamicBuffer<ResourceExchangeVfxMarkerComponent> vfxMarkers =
                em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);
            int warmupVisualRequestCount = visualRequests.Length;
            int warmupVfxMarkerCount = vfxMarkers.Length;

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long totalStartTicks = Stopwatch.GetTimestamp();
            RunFrames(world, validationSystem, queueTickSystem, visualCueSystem, MeasuredFrames, WarmupFrames, samples);
            long totalStopTicks = Stopwatch.GetTimestamp();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            DynamicBuffer<ResourceExchangeQueueComponent> queue =
                em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
            DynamicBuffer<ResourceExchangeResultComponent> results =
                em.GetBuffer<ResourceExchangeResultComponent>(exchange);
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
                em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
            visualRequests = em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
            vfxMarkers = em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);

            Assert.AreEqual(ActiveQueueItems, queue.Length, "Active logistics scenario should keep the configured queue item count.");
            Assert.AreEqual(0, results.Length, "Measured steady state should not complete or block exchange jobs.");
            Assert.AreEqual(0, economyEvents.Length, "Measured steady state should not emit economy events without queue state changes.");
            Assert.AreEqual(
                warmupVisualRequestCount,
                visualRequests.Length,
                "Measured steady state should not emit recurring visual requests after warmup cue flags are set.");
            Assert.AreEqual(
                warmupVfxMarkerCount,
                vfxMarkers.Length,
                "Measured steady state should not emit recurring VFX markers after warmup cue flags are set.");

            for (int i = 0; i < queue.Length; i++)
            {
                Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[i].State);
                Assert.AreEqual(1, queue[i].PresentationStarted);
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
                queue.Length,
                visualRequests.Length,
                vfxMarkers.Length);

            Assert.LessOrEqual(allocatedBytes, MaxAllocatedBytes);
            Assert.LessOrEqual(p95Ms, MaxP95Ms);
            Assert.LessOrEqual(p99Ms, MaxP99Ms);

            Debug.Log(
                $"[ResourceExchangeSteadyStatePerformanceValidation] report={ReportPath} measuredFrames={MeasuredFrames} averageMs={averageMs:F3} p95Ms={p95Ms:F3} p99Ms={p99Ms:F3} maxMs={maxMs:F3} allocatedBytes={allocatedBytes} visualRequests={visualRequests.Length} vfxMarkers={vfxMarkers.Length}");
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
        SystemHandle validationSystem,
        SystemHandle queueTickSystem,
        SystemHandle visualCueSystem,
        int frames,
        int frameOffset,
        double[] samples = null)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            double elapsed = (frameOffset + frame + 1) * FrameDeltaSeconds;
            world.SetTime(new TimeData(elapsed, FrameDeltaSeconds));
            long startTicks = Stopwatch.GetTimestamp();
            validationSystem.Update(world.Unmanaged);
            queueTickSystem.Update(world.Unmanaged);
            visualCueSystem.Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
            long stopTicks = Stopwatch.GetTimestamp();
            if (samples != null)
                samples[frame] = TicksToMilliseconds(stopTicks - startTicks);
        }
    }

    private static Entity CreateActiveExchangeEntity(EntityManager em)
    {
        Entity entity = em.CreateEntity(
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent));
        em.SetComponentData(entity, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            MaxQueueItems = ActiveQueueItems,
            ScenarioTag = new FixedString64Bytes("mission.performance.active_exchange")
        });
        em.SetComponentData(entity, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Credits = 5000,
            Materials = 4000,
            Oil = 4000,
            Fuel = 3000,
            MaterialsCapacity = 8000,
            OilCapacity = 8000,
            FuelCapacity = 8000,
            RushTickets = 8
        });
        em.SetComponentData(entity, new ResourceExchangeSummaryComponent
        {
            FactionId = 1,
            Enabled = 1,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            QueueCount = ActiveQueueItems,
            ActiveCount = ActiveQueueItems,
            MaxQueueItems = ActiveQueueItems
        });

        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeResultComponent>(entity);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(entity);
        em.AddBuffer<ResourceExchangeVisualRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeVfxMarkerComponent>(entity);
        em.AddBuffer<ResourceExchangePresentationAnchorComponent>(entity);

        DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
            em.GetBuffer<ResourceExchangeRecipeComponent>(entity);
        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            em.GetBuffer<ResourceExchangeRequestComponent>(entity);
        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(entity);
        DynamicBuffer<ResourceExchangeResultComponent> results =
            em.GetBuffer<ResourceExchangeResultComponent>(entity);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(entity);
        DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(entity);
        DynamicBuffer<ResourceExchangeVfxMarkerComponent> vfxMarkers =
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(entity);
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors =
            em.GetBuffer<ResourceExchangePresentationAnchorComponent>(entity);

        recipes.EnsureCapacity(4);
        requests.EnsureCapacity(4);
        queue.EnsureCapacity(ActiveQueueItems);
        results.EnsureCapacity(4);
        economyEvents.EnsureCapacity(4);
        visualRequests.EnsureCapacity(ActiveQueueItems * 4);
        vfxMarkers.EnsureCapacity(ActiveQueueItems * 4);
        anchors.EnsureCapacity(4);

        recipes.Add(CreateRecipe("exchange.performance.export_oil", ResourceExchangeRouteType.Export, ResourceExchangeResourceKind.Oil, ResourceExchangeResourceKind.Credits));
        recipes.Add(CreateRecipe("exchange.performance.export_materials", ResourceExchangeRouteType.Export, ResourceExchangeResourceKind.Materials, ResourceExchangeResourceKind.Credits));
        recipes.Add(CreateRecipe("exchange.performance.import_fuel", ResourceExchangeRouteType.Import, ResourceExchangeResourceKind.Credits, ResourceExchangeResourceKind.Fuel));
        recipes.Add(CreateRecipe("exchange.performance.import_materials", ResourceExchangeRouteType.Import, ResourceExchangeResourceKind.Credits, ResourceExchangeResourceKind.Materials));

        queue.Add(CreateQueueItem(1, "exchange.performance.export_oil", ResourceExchangeRouteType.Export, ResourceExchangeResourceKind.Oil, ResourceExchangeResourceKind.Credits, 400, 170));
        queue.Add(CreateQueueItem(2, "exchange.performance.export_materials", ResourceExchangeRouteType.Export, ResourceExchangeResourceKind.Materials, ResourceExchangeResourceKind.Credits, 300, 125));
        queue.Add(CreateQueueItem(3, "exchange.performance.import_fuel", ResourceExchangeRouteType.Import, ResourceExchangeResourceKind.Credits, ResourceExchangeResourceKind.Fuel, 500, 160));
        queue.Add(CreateQueueItem(4, "exchange.performance.import_materials", ResourceExchangeRouteType.Import, ResourceExchangeResourceKind.Credits, ResourceExchangeResourceKind.Materials, 450, 180));

        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.BaseDepot, "base_depot", new float3(0f, 0f, 0f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, "runway", new float3(18f, 0f, 4f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.Storage, "storage", new float3(-8f, 0f, 3f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.FallbackSafe, "fallback", new float3(0f, 0f, -10f)));

        return entity;
    }

    private static ResourceExchangeRecipeComponent CreateRecipe(
        string recipeId,
        ResourceExchangeRouteType routeType,
        ResourceExchangeResourceKind inputResource,
        ResourceExchangeResourceKind outputResource)
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes(recipeId),
            DisplayName = new FixedString64Bytes(recipeId),
            RouteType = routeType,
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 50,
            OutputPerInput = 0.5f,
            FeePercent = 0.1f,
            DurationSecondsBase = 9000f,
            DurationSecondsPerStep = 0f,
            RushTicketSecondsPerTicket = 30,
            MaxRushTickets = 4,
            MissionTag = new FixedString64Bytes("mission.performance.active_exchange")
        };
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId,
        string recipeId,
        ResourceExchangeRouteType routeType,
        ResourceExchangeResourceKind inputResource,
        ResourceExchangeResourceKind outputResource,
        int inputAmount,
        int outputAmount)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes(recipeId),
            RouteType = routeType,
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmount = inputAmount,
            ReservedInputAmount = inputAmount,
            OutputAmount = outputAmount,
            State = ResourceExchangeQueueState.InProgress,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = 10000f,
            RemainingSeconds = 6000f,
            Version = 1
        };
    }

    private static ResourceExchangePresentationAnchorComponent CreateAnchor(
        ResourceExchangePresentationAnchorKind anchorKind,
        string anchorId,
        float3 position)
    {
        return new ResourceExchangePresentationAnchorComponent
        {
            FactionId = 1,
            AnchorKind = anchorKind,
            AnchorId = new FixedString64Bytes(anchorId),
            Position = position,
            Rotation = quaternion.identity,
            Radius = 4f,
            IsValid = 1
        };
    }

    private static double PercentileSorted(double[] samples, double percentile)
    {
        int index = (int)math.clamp(
            math.ceil((float)(samples.Length * percentile)) - 1,
            0,
            samples.Length - 1);
        return samples[index];
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
        int queueCount,
        int visualRequestCount,
        int vfxMarkerCount)
    {
        string directory = Path.GetDirectoryName(ReportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder(512);
        builder.AppendLine("{");
        AppendJson(builder, "warmupFrames", WarmupFrames, trailingComma: true);
        AppendJson(builder, "measuredFrames", MeasuredFrames, trailingComma: true);
        AppendJson(builder, "activeQueueItems", queueCount, trailingComma: true);
        AppendJson(builder, "visualRequestCount", visualRequestCount, trailingComma: true);
        AppendJson(builder, "vfxMarkerCount", vfxMarkerCount, trailingComma: true);
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
        builder.Append("  \"").Append(name).Append("\": ")
            .Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }
}
#endif
