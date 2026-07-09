using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeGcAllocationValidationTests
{
    private const int WarmupFrames = 64;
    private const int MeasuredFrames = 512;
    private const float DeltaSeconds = 1f / 60f;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(QueueTickAndValidationSteadyState_DoNotAllocateManagedMemory),
                test => test.QueueTickAndValidationSteadyState_DoNotAllocateManagedMemory(),
                ref passed);
            RunValidationStep(
                nameof(ValidationSystemUpdate_MultipleEntities_DoesNotAllocateManagedMemoryAfterWarmup),
                test => test.ValidationSystemUpdate_MultipleEntities_DoesNotAllocateManagedMemoryAfterWarmup(),
                ref passed);

            Debug.Log($"[ResourceExchangeGcAllocationValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeGcAllocationValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void QueueTickAndValidationSteadyState_DoNotAllocateManagedMemory()
    {
        using World world = new(nameof(QueueTickAndValidationSteadyState_DoNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateSteadyStateExchangeEntity(em);

        ResourceExchangeRequestQueueComponent requestQueue =
            em.GetComponentData<ResourceExchangeRequestQueueComponent>(exchange);
        ResourceExchangeEnabledComponent enabled =
            em.GetComponentData<ResourceExchangeEnabledComponent>(exchange);
        ResourceExchangeWalletComponent wallet =
            em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        ResourceExchangeSummaryComponent summary =
            em.GetComponentData<ResourceExchangeSummaryComponent>(exchange);
        DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
            em.GetBuffer<ResourceExchangeRecipeComponent>(exchange);
        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            em.GetBuffer<ResourceExchangeRequestComponent>(exchange);
        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        DynamicBuffer<ResourceExchangeResultComponent> results =
            em.GetBuffer<ResourceExchangeResultComponent>(exchange);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);

        for (int i = 0; i < WarmupFrames; i++)
        {
            RunSteadyStateFrame(
                ref requestQueue,
                enabled,
                ref wallet,
                ref summary,
                recipes,
                requests,
                queue,
                results,
                economyEvents,
                elapsedSeconds: i * DeltaSeconds);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredFrames; i++)
        {
            RunSteadyStateFrame(
                ref requestQueue,
                enabled,
                ref wallet,
                ref summary,
                recipes,
                requests,
                queue,
                results,
                economyEvents,
                elapsedSeconds: (WarmupFrames + i) * DeltaSeconds);
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.AreEqual(0, requests.Length, "Steady-state validation should not leave pending exchange requests.");
        Assert.AreEqual(0, results.Length, "Steady-state queue ticking should not emit result rows before completion.");
        Assert.AreEqual(0, economyEvents.Length, "Steady-state queue ticking should not emit economy events before completion.");
        Assert.AreEqual(3, queue.Length, "The steady-state queue fixture should keep all measured jobs active.");
        for (int i = 0; i < queue.Length; i++)
        {
            Assert.AreEqual(
                ResourceExchangeQueueState.InProgress,
                queue[i].State,
                $"Queue item {i} should remain in progress during the no-allocation steady-state window.");
        }

        Assert.AreEqual(
            0,
            allocatedBytes,
            $"Resource Exchange queue tick and request validation steady state allocated {allocatedBytes} bytes over {MeasuredFrames} frames after {WarmupFrames} warmup frames.");

        Debug.Log(
            $"[ResourceExchangeGcAllocationValidation] measuredFrames={MeasuredFrames} warmupFrames={WarmupFrames} allocatedBytes={allocatedBytes}");
    }

    [Test]
    public void ValidationSystemUpdate_MultipleEntities_DoesNotAllocateManagedMemoryAfterWarmup()
    {
        using World world = new(nameof(ValidationSystemUpdate_MultipleEntities_DoesNotAllocateManagedMemoryAfterWarmup));
        EntityManager em = world.EntityManager;
        Entity primaryExchange = CreateSteadyStateExchangeEntity(em, factionId: 1);
        Entity secondaryExchange = CreateSteadyStateExchangeEntity(
            em,
            factionId: 2,
            allowAiExchange: true);
        SystemHandle validationSystem = world.CreateSystem<ResourceExchangeRequestValidationSystem>();

        for (int frame = 0; frame < WarmupFrames; frame++)
        {
            ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                em,
                primaryExchange,
                factionId: 1,
                frameCount: frame);
            ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                em,
                secondaryExchange,
                factionId: 2,
                frameCount: frame);
            validationSystem.Update(world.Unmanaged);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long productionUpdateAllocatedBytes = 0;
        int lastPrimaryRequestId = 0;
        int lastSecondaryRequestId = 0;
        long measurementWindowBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < MeasuredFrames; frame++)
        {
            int frameCount = WarmupFrames + frame;
            lastPrimaryRequestId = ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                em,
                primaryExchange,
                factionId: 1,
                frameCount: frameCount);
            lastSecondaryRequestId = ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(
                em,
                secondaryExchange,
                factionId: 2,
                frameCount: frameCount);

            long productionUpdateBefore = GC.GetAllocatedBytesForCurrentThread();
            validationSystem.Update(world.Unmanaged);
            productionUpdateAllocatedBytes +=
                GC.GetAllocatedBytesForCurrentThread() - productionUpdateBefore;
        }

        long measurementWindowAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - measurementWindowBefore;
        long harnessAllocatedBytes = measurementWindowAllocatedBytes - productionUpdateAllocatedBytes;

        DynamicBuffer<ResourceExchangeRequestComponent> primaryRequests =
            em.GetBuffer<ResourceExchangeRequestComponent>(primaryExchange);
        DynamicBuffer<ResourceExchangeRequestComponent> secondaryRequests =
            em.GetBuffer<ResourceExchangeRequestComponent>(secondaryExchange);
        DynamicBuffer<ResourceExchangeResultComponent> primaryResults =
            em.GetBuffer<ResourceExchangeResultComponent>(primaryExchange);
        DynamicBuffer<ResourceExchangeResultComponent> secondaryResults =
            em.GetBuffer<ResourceExchangeResultComponent>(secondaryExchange);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> primaryEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(primaryExchange);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> secondaryEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(secondaryExchange);
        ResourceExchangeSummaryComponent primarySummary =
            em.GetComponentData<ResourceExchangeSummaryComponent>(primaryExchange);
        ResourceExchangeSummaryComponent secondarySummary =
            em.GetComponentData<ResourceExchangeSummaryComponent>(secondaryExchange);

        Debug.Log(
            $"[ResourceExchangeGcAllocationValidation] updateOnly measuredFrames={MeasuredFrames} warmupFrames={WarmupFrames} productionUpdateAllocatedBytes={productionUpdateAllocatedBytes} measurementWindowAllocatedBytes={measurementWindowAllocatedBytes} harnessAllocatedBytes={harnessAllocatedBytes} primarySummaryVersion={primarySummary.Version} secondarySummaryVersion={secondarySummary.Version} primaryAllowAiExchange={primarySummary.AllowAiExchange} secondaryAllowAiExchange={secondarySummary.AllowAiExchange}");

        Assert.AreEqual(
            0,
            productionUpdateAllocatedBytes,
            $"Warmed ResourceExchangeRequestValidationSystem updates allocated {productionUpdateAllocatedBytes} managed bytes over {MeasuredFrames} updates; the complete measurement window allocated {measurementWindowAllocatedBytes} bytes, of which {harnessAllocatedBytes} bytes came from request setup and measurement harness work outside SystemHandle.Update.");
        Assert.GreaterOrEqual(
            harnessAllocatedBytes,
            0,
            "Harness allocation is the complete measurement window minus bytes allocated inside SystemHandle.Update and cannot be negative.");

        Assert.AreEqual(0, primaryRequests.Length);
        Assert.AreEqual(0, secondaryRequests.Length);
        Assert.AreEqual(1, primaryResults.Length);
        Assert.AreEqual(1, secondaryResults.Length);
        Assert.AreEqual(lastPrimaryRequestId, primaryResults[0].RequestId);
        Assert.AreEqual(lastSecondaryRequestId, secondaryResults[0].RequestId);
        Assert.AreEqual(1, primaryResults[0].Accepted);
        Assert.AreEqual(1, secondaryResults[0].Accepted);
        Assert.AreEqual(0, primaryEvents.Length);
        Assert.AreEqual(0, secondaryEvents.Length);

        uint expectedSummaryVersion = WarmupFrames + MeasuredFrames;
        Assert.AreEqual(expectedSummaryVersion, primarySummary.Version);
        Assert.AreEqual(expectedSummaryVersion, secondarySummary.Version);
        Assert.AreEqual(3, primarySummary.QueueCount);
        Assert.AreEqual(3, secondarySummary.QueueCount);
        Assert.AreEqual(3, primarySummary.ActiveCount);
        Assert.AreEqual(3, secondarySummary.ActiveCount);
        Assert.AreEqual(0, primarySummary.AllowAiExchange);
        Assert.AreEqual(
            1,
            secondarySummary.AllowAiExchange,
            "The second exchange summary must publish its entity's enabled AI exchange gate after every update.");
    }

    private static Entity CreateSteadyStateExchangeEntity(
        EntityManager em,
        byte factionId = 1,
        bool allowAiExchange = false)
    {
        Entity entity = em.CreateEntity(
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent));
        em.SetComponentData(entity, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = factionId,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            AllowAiExchange = allowAiExchange ? (byte)1 : (byte)0,
            MaxQueueItems = 4,
            ScenarioTag = new FixedString64Bytes("mission.performance.gc")
        });
        em.SetComponentData(entity, new ResourceExchangeWalletComponent
        {
            FactionId = factionId,
            Credits = 1000,
            Materials = 1000,
            Oil = 1000,
            Fuel = 1000,
            MaterialsCapacity = 2000,
            OilCapacity = 2000,
            FuelCapacity = 2000,
            RushTickets = 5
        });
        em.SetComponentData(entity, new ResourceExchangeSummaryComponent
        {
            FactionId = factionId,
            Enabled = 1,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            QueueCount = 3,
            ActiveCount = 3,
            MaxQueueItems = 4
        });

        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeResultComponent>(entity);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(entity);

        DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
            em.GetBuffer<ResourceExchangeRecipeComponent>(entity);
        recipes.EnsureCapacity(4);
        recipes.Add(new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.performance.export_oil"),
            DisplayName = new FixedString64Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmountMin = 100,
            InputAmountMax = 500,
            InputStep = 100,
            OutputPerInput = 0.5f,
            FeePercent = 0.15f,
            DurationSecondsBase = 60f,
            DurationSecondsPerStep = 2f,
            RushTicketSecondsPerTicket = 10,
            MaxRushTickets = 4,
            MissionTag = new FixedString64Bytes("mission.performance.gc")
        });

        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            em.GetBuffer<ResourceExchangeRequestComponent>(entity);
        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(entity);
        DynamicBuffer<ResourceExchangeResultComponent> results =
            em.GetBuffer<ResourceExchangeResultComponent>(entity);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(entity);
        requests.EnsureCapacity(4);
        queue.EnsureCapacity(4);
        results.EnsureCapacity(4);
        economyEvents.EnsureCapacity(4);

        queue.Add(CreateQueueItem(1, factionId, ResourceExchangeResourceKind.Oil, ResourceExchangeResourceKind.Credits, 100, 42));
        queue.Add(CreateQueueItem(2, factionId, ResourceExchangeResourceKind.Materials, ResourceExchangeResourceKind.Credits, 120, 50));
        queue.Add(CreateQueueItem(3, factionId, ResourceExchangeResourceKind.Credits, ResourceExchangeResourceKind.Fuel, 180, 60));

        return entity;
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId,
        byte factionId,
        ResourceExchangeResourceKind inputResource,
        ResourceExchangeResourceKind outputResource,
        int inputAmount,
        int outputAmount)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = factionId,
            RecipeId = new FixedString128Bytes("exchange.performance.export_oil"),
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmount = inputAmount,
            ReservedInputAmount = inputAmount,
            OutputAmount = outputAmount,
            State = ResourceExchangeQueueState.InProgress,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = 10000f,
            RemainingSeconds = 10000f,
            Version = 1
        };
    }

    private static void RunSteadyStateFrame(
        ref ResourceExchangeRequestQueueComponent requestQueue,
        in ResourceExchangeEnabledComponent enabled,
        ref ResourceExchangeWalletComponent wallet,
        ref ResourceExchangeSummaryComponent summary,
        DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
        DynamicBuffer<ResourceExchangeRequestComponent> requests,
        DynamicBuffer<ResourceExchangeQueueComponent> queue,
        DynamicBuffer<ResourceExchangeResultComponent> results,
        DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
        float elapsedSeconds)
    {
        ResourceExchangeRequestValidationSystem.ProcessRequests(
            ref requestQueue,
            enabled,
            ref wallet,
            ref summary,
            recipes,
            requests,
            queue,
            results,
            economyEvents,
            elapsedSeconds);
        ResourceExchangeQueueTickSystem.TickQueue(
            enabled,
            ref wallet,
            ref summary,
            queue,
            results,
            economyEvents,
            DeltaSeconds);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeGcAllocationValidationTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeGcAllocationValidationTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeGcAllocationValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeGcAllocationValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
