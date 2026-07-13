using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeRushSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(RushRequest_Accepted_SpendsTicketsAndReducesRemainingTime),
                test => test.RushRequest_Accepted_SpendsTicketsAndReducesRemainingTime(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_RejectsInsufficientRushTickets),
                test => test.RushRequest_RejectsInsufficientRushTickets(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_RejectsWhenQueueItemCapWouldBeExceeded),
                test => test.RushRequest_RejectsWhenQueueItemCapWouldBeExceeded(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_RejectsBlockedQueueItem),
                test => test.RushRequest_RejectsBlockedQueueItem(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_CompletesImmediatelyWhenRemainingTimeReachesZero),
                test => test.RushRequest_CompletesImmediatelyWhenRemainingTimeReachesZero(),
                ref passed);
            RunValidationStep(
                nameof(RushAllRequest_SpendsBudgetAcrossEligibleQueueItems),
                test => test.RushAllRequest_SpendsBudgetAcrossEligibleQueueItems(),
                ref passed);

            Debug.Log($"[ResourceExchangeRushValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeRushValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void RushRequest_Accepted_SpendsTicketsAndReducesRemainingTime()
    {
        using World world = new(nameof(RushRequest_Accepted_SpendsTicketsAndReducesRemainingTime));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 5);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(remainingSeconds: 30f));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 2, 1, 10);

        UpdateValidationSystem(world);

        AssertRushAccepted(em, exchange, requestId, 2);
        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(3, wallet.RushTickets);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[0].State);
        Assert.AreEqual(10f, queue[0].RemainingSeconds);
        Assert.AreEqual(2, queue[0].RushTicketsSpent);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.RushAccepted, events[0].ResultKind);
        Assert.AreEqual(ResourceExchangeResourceKind.RushTickets, events[0].ResourceKind);
        Assert.AreEqual(-2, events[0].Amount);
    }

    [Test]
    public void RushRequest_RejectsInsufficientRushTickets()
    {
        using World world = new(nameof(RushRequest_RejectsInsufficientRushTickets));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 1);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(remainingSeconds: 30f));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 2, 1, 10);

        UpdateValidationSystem(world);

        AssertRushRejected(em, exchange, requestId, ResourceExchangeReason.InsufficientRushTickets);
        Assert.AreEqual(1, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).RushTickets);
        Assert.AreEqual(30f, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].RemainingSeconds);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);
    }

    [Test]
    public void RushRequest_RejectsWhenQueueItemCapWouldBeExceeded()
    {
        using World world = new(nameof(RushRequest_RejectsWhenQueueItemCapWouldBeExceeded));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 5);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            remainingSeconds: 30f,
            rushTicketsSpent: 2));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 2, 1, 10);

        UpdateValidationSystem(world);

        AssertRushRejected(em, exchange, requestId, ResourceExchangeReason.RushUnavailable);
        ResourceExchangeQueueComponent item = em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0];
        Assert.AreEqual(2, item.RushTicketsSpent);
        Assert.AreEqual(30f, item.RemainingSeconds);
        Assert.AreEqual(5, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).RushTickets);
    }

    [Test]
    public void RushRequest_RejectsBlockedQueueItem()
    {
        using World world = new(nameof(RushRequest_RejectsBlockedQueueItem));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 5);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            remainingSeconds: 30f,
            state: ResourceExchangeQueueState.Blocked));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 1, 1, 10);

        UpdateValidationSystem(world);

        AssertRushRejected(em, exchange, requestId, ResourceExchangeReason.RushUnavailable);
        Assert.AreEqual(5, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).RushTickets);
        Assert.AreEqual(ResourceExchangeQueueState.Blocked, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);
    }

    [Test]
    public void RushRequest_CompletesImmediatelyWhenRemainingTimeReachesZero()
    {
        using World world = new(nameof(RushRequest_CompletesImmediatelyWhenRemainingTimeReachesZero));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 3);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 30, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            remainingSeconds: 10f,
            outputAmount: 93));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 1, 1, 10);

        UpdateValidationSystem(world);

        AssertRushAccepted(em, exchange, requestId, 1);
        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(2, wallet.RushTickets);
        Assert.AreEqual(93, em.GetComponentData<FactionEconomy>(exchange).Money);

        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(1, queue[0].OutputApplied);
        Assert.AreEqual(1, queue[0].RushTicketsSpent);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(2, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.RushAccepted, events[0].ResultKind);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCompleted, events[1].ResultKind);
    }

    [Test]
    public void RushAllRequest_SpendsBudgetAcrossEligibleQueueItems()
    {
        using World world = new(nameof(RushAllRequest_SpendsBudgetAcrossEligibleQueueItems));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, rushTickets: 4);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(queueItemId: 1, remainingSeconds: 25f));
        queue.Add(CreateQueueItem(queueItemId: 2, remainingSeconds: 15f));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueRushAllRequest(em, exchange, 4, 1, 10);

        UpdateValidationSystem(world);

        AssertRushAccepted(em, exchange, requestId, 4);
        Assert.AreEqual(0, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).RushTickets);
        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[1].State);
        Assert.AreEqual(5f, queue[1].RemainingSeconds);
    }

    private static Entity CreateExchangeEntity(EntityManager em, int rushTickets)
    {
        Entity entity = em.CreateEntity(
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(ResourceExchangeEnabledComponent),
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent));
        em.SetComponentData(entity, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            MaxQueueItems = 3,
            ScenarioTag = new FixedString64Bytes("mission.active")
        });
        em.SetComponentData(entity, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            RushTickets = rushTickets
        });
        em.SetComponentData(entity, new FactionEconomy { FactionId = 1 });
        em.SetComponentData(entity, new FactionTacticalMaterialsComponent { FactionId = 1 });
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

    private static ResourceExchangeRecipeComponent RushableRecipe(int secondsPerTicket, int maxTickets)
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.rush.test"),
            DisplayName = new FixedString128Bytes("Rushable Test"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 1f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 0f,
            RushTicketSecondsPerTicket = secondsPerTicket,
            MaxRushTickets = maxTickets,
            Enabled = 1
        };
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        float remainingSeconds = 30f,
        int outputAmount = 100,
        int rushTicketsSpent = 0,
        ResourceExchangeQueueState state = ResourceExchangeQueueState.InProgress)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.rush.test"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmount = 100,
            ReservedInputAmount = 100,
            OutputAmount = outputAmount,
            State = state,
            StateReason = state == ResourceExchangeQueueState.Blocked
                ? ResourceExchangeReason.StorageFull
                : ResourceExchangeReason.None,
            DurationSeconds = 30f,
            RemainingSeconds = remainingSeconds,
            RushTicketsSpent = rushTicketsSpent,
            Version = 1
        };
    }

    private static void UpdateValidationSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void AssertRushAccepted(
        EntityManager em,
        Entity exchange,
        int requestId,
        int rushTicketsSpent)
    {
        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.RushAccepted, result.ResultKind);
        Assert.AreEqual(ResourceExchangeReason.None, result.Reason);
        Assert.AreEqual(rushTicketsSpent, result.RushTicketsSpent);
    }

    private static void AssertRushRejected(
        EntityManager em,
        Entity exchange,
        int requestId,
        ResourceExchangeReason reason)
    {
        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.RushRejected, result.ResultKind);
        Assert.AreEqual(reason, result.Reason);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeRushSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeRushSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeRushValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeRushValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
