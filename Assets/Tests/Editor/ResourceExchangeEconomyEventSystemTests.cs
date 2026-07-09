using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeEconomyEventSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(StartRequest_EmitsInputReserveEconomyEvent),
                test => test.StartRequest_EmitsInputReserveEconomyEvent(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_EmitsOutputGrantEconomyEvent),
                test => test.TickQueue_EmitsOutputGrantEconomyEvent(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_EmitsRefundEconomyEvent),
                test => test.CancelRequest_EmitsRefundEconomyEvent(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_NoRefundStillEmitsCancellationEconomyEvent),
                test => test.CancelRequest_NoRefundStillEmitsCancellationEconomyEvent(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_BlockedJobEmitsZeroAmountEconomyEvent),
                test => test.TickQueue_BlockedJobEmitsZeroAmountEconomyEvent(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_EmitsRushTicketSpendEconomyEvent),
                test => test.RushRequest_EmitsRushTicketSpendEconomyEvent(),
                ref passed);

            Debug.Log($"[ResourceExchangeEconomyEventValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeEconomyEventValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StartRequest_EmitsInputReserveEconomyEvent()
    {
        using World world = new(nameof(StartRequest_EmitsInputReserveEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 500
        });
        AddRecipe(em, exchange, ExportOilRecipe());

        ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.QueueStarted,
            ResourceExchangeResourceKind.Oil,
            -200);
    }

    [Test]
    public void TickQueue_EmitsOutputGrantEconomyEvent()
    {
        using World world = new(nameof(TickQueue_EmitsOutputGrantEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            outputResource: ResourceExchangeResourceKind.Credits,
            outputAmount: 75,
            remainingSeconds: 0.1f));

        Tick(em, exchange, 0.2f);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.QueueCompleted,
            ResourceExchangeResourceKind.Credits,
            75);
    }

    [Test]
    public void CancelRequest_EmitsRefundEconomyEvent()
    {
        using World world = new(nameof(CancelRequest_EmitsRefundEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 100
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(reservedInputAmount: 200));

        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 10);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.QueueCancelled,
            ResourceExchangeResourceKind.Oil,
            200);
    }

    [Test]
    public void CancelRequest_NoRefundStillEmitsCancellationEconomyEvent()
    {
        using World world = new(nameof(CancelRequest_NoRefundStillEmitsCancellationEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 100
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            reservedInputAmount: 200,
            presentationStarted: 1));

        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 10);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.QueueCancelled,
            ResourceExchangeResourceKind.Oil,
            0);
    }

    [Test]
    public void TickQueue_BlockedJobEmitsZeroAmountEconomyEvent()
    {
        using World world = new(nameof(TickQueue_BlockedJobEmitsZeroAmountEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Fuel = 980,
            FuelCapacity = 1000
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            outputResource: ResourceExchangeResourceKind.Fuel,
            outputAmount: 50,
            remainingSeconds: 0.1f));

        Tick(em, exchange, 0.1f);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.QueueBlocked,
            ResourceExchangeResourceKind.Fuel,
            0);
    }

    [Test]
    public void RushRequest_EmitsRushTicketSpendEconomyEvent()
    {
        using World world = new(nameof(RushRequest_EmitsRushTicketSpendEconomyEvent));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            RushTickets = 5
        });
        AddRecipe(em, exchange, ExportOilRecipe(rushTicketSecondsPerTicket: 10, maxRushTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(remainingSeconds: 30f));

        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 2, 1, 10);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        AssertEconomyEvent(
            events[0],
            queueItemId: 1,
            ResourceExchangeResultKind.RushAccepted,
            ResourceExchangeResourceKind.RushTickets,
            -2);
    }

    private static Entity CreateExchangeEntity(EntityManager em, ResourceExchangeWalletComponent wallet)
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
            MaxQueueItems = 4,
            ScenarioTag = new FixedString64Bytes("mission.active")
        });

        if (wallet.FactionId == 0)
            wallet.FactionId = 1;
        em.SetComponentData(entity, wallet);
        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeResultComponent>(entity);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(entity);
        return entity;
    }

    private static void AddRecipe(EntityManager em, Entity exchange, ResourceExchangeRecipeComponent recipe)
    {
        em.GetBuffer<ResourceExchangeRecipeComponent>(exchange).Add(recipe);
    }

    private static ResourceExchangeRecipeComponent ExportOilRecipe(
        int rushTicketSecondsPerTicket = 0,
        int maxRushTickets = 0)
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.export_oil_credits.standard"),
            DisplayName = new FixedString128Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 0.5f,
            FeePercent = 0f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 2f,
            RushTicketSecondsPerTicket = rushTicketSecondsPerTicket,
            MaxRushTickets = maxRushTickets,
            Enabled = 1
        };
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
        ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Credits,
        int reservedInputAmount = 200,
        int outputAmount = 75,
        float remainingSeconds = 1f,
        byte presentationStarted = 0)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.export_oil_credits.standard"),
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmount = reservedInputAmount,
            ReservedInputAmount = reservedInputAmount,
            OutputAmount = outputAmount,
            State = ResourceExchangeQueueState.InProgress,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = 30f,
            RemainingSeconds = remainingSeconds,
            PresentationStarted = presentationStarted,
            Version = 1
        };
    }

    private static void Tick(EntityManager em, Entity exchange, float deltaSeconds)
    {
        ResourceExchangeEnabledComponent enabled = em.GetComponentData<ResourceExchangeEnabledComponent>(exchange);
        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        ResourceExchangeSummaryComponent summary = em.GetComponentData<ResourceExchangeSummaryComponent>(exchange);
        ResourceExchangeQueueTickSystem.TickQueue(
            enabled,
            ref wallet,
            ref summary,
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            em.GetBuffer<ResourceExchangeResultComponent>(exchange),
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange),
            deltaSeconds);
        em.SetComponentData(exchange, wallet);
        em.SetComponentData(exchange, summary);
    }

    private static void UpdateValidationSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void AssertEconomyEvent(
        in ResourceExchangeEconomyEventComponent economyEvent,
        int queueItemId,
        ResourceExchangeResultKind resultKind,
        ResourceExchangeResourceKind resourceKind,
        int amount)
    {
        Assert.AreEqual(queueItemId, economyEvent.QueueItemId);
        Assert.AreEqual(1, economyEvent.FactionId);
        Assert.AreEqual(resultKind, economyEvent.ResultKind);
        Assert.AreEqual(resourceKind, economyEvent.ResourceKind);
        Assert.AreEqual(amount, economyEvent.Amount);
        Assert.AreEqual(new FixedString128Bytes("exchange.export_oil_credits.standard"), economyEvent.RecipeId);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeEconomyEventSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeEconomyEventSystemTests();
        action(test);
        passed++;
    }
}
#endif
