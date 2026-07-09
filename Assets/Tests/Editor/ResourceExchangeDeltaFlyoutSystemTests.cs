using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeDeltaFlyoutSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(StartRequest_Accepted_EmitsInputReservedFlyout),
                test => test.StartRequest_Accepted_EmitsInputReservedFlyout(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_Completed_EmitsOutputGrantedFlyout),
                test => test.TickQueue_Completed_EmitsOutputGrantedFlyout(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_RefundsReservedInput_EmitsInputRefundedFlyout),
                test => test.CancelRequest_RefundsReservedInput_EmitsInputRefundedFlyout(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_Accepted_EmitsRushTicketsSpentFlyout),
                test => test.RushRequest_Accepted_EmitsRushTicketsSpentFlyout(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_CompletesImmediately_EmitsRushSpendAndOutputGrantFlyouts),
                test => test.RushRequest_CompletesImmediately_EmitsRushSpendAndOutputGrantFlyouts(),
                ref passed);

            Debug.Log($"[ResourceExchangeDeltaFlyoutValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeDeltaFlyoutValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StartRequest_Accepted_EmitsInputReservedFlyout()
    {
        using World world = new(nameof(StartRequest_Accepted_EmitsInputReservedFlyout));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
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

        DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> flyouts =
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange);
        Assert.AreEqual(1, flyouts.Length);
        AssertFlyout(
            flyouts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.InputReserved,
            ResourceExchangeResultKind.QueueStarted,
            ResourceExchangeResourceKind.Oil,
            -200);
    }

    [Test]
    public void TickQueue_Completed_EmitsOutputGrantedFlyout()
    {
        using World world = new(nameof(TickQueue_Completed_EmitsOutputGrantedFlyout));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(outputAmount: 93, remainingSeconds: 0.1f));

        TickQueue(em, exchange, 0.2f);

        DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> flyouts =
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange);
        Assert.AreEqual(1, flyouts.Length);
        AssertFlyout(
            flyouts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.OutputGranted,
            ResourceExchangeResultKind.QueueCompleted,
            ResourceExchangeResourceKind.Credits,
            93);
    }

    [Test]
    public void CancelRequest_RefundsReservedInput_EmitsInputRefundedFlyout()
    {
        using World world = new(nameof(CancelRequest_RefundsReservedInput_EmitsInputRefundedFlyout));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            Oil = 300
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(reservedInputAmount: 200));

        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 0);
        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> flyouts =
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange);
        Assert.AreEqual(1, flyouts.Length);
        AssertFlyout(
            flyouts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.InputRefunded,
            ResourceExchangeResultKind.QueueCancelled,
            ResourceExchangeResourceKind.Oil,
            200);
    }

    [Test]
    public void RushRequest_Accepted_EmitsRushTicketsSpentFlyout()
    {
        using World world = new(nameof(RushRequest_Accepted_EmitsRushTicketsSpentFlyout));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            RushTickets = 5
        });
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 10, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(remainingSeconds: 30f));

        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 2, 1, 0);
        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> flyouts =
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange);
        Assert.AreEqual(1, flyouts.Length);
        AssertFlyout(
            flyouts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.RushTicketsSpent,
            ResourceExchangeResultKind.RushAccepted,
            ResourceExchangeResourceKind.RushTickets,
            -2);
    }

    [Test]
    public void RushRequest_CompletesImmediately_EmitsRushSpendAndOutputGrantFlyouts()
    {
        using World world = new(nameof(RushRequest_CompletesImmediately_EmitsRushSpendAndOutputGrantFlyouts));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            RushTickets = 3
        });
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 30, maxTickets: 3));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(remainingSeconds: 10f, outputAmount: 93));

        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 1, 1, 0);
        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> flyouts =
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange);
        Assert.AreEqual(2, flyouts.Length);
        AssertFlyout(
            flyouts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.RushTicketsSpent,
            ResourceExchangeResultKind.RushAccepted,
            ResourceExchangeResourceKind.RushTickets,
            -1);
        AssertFlyout(
            flyouts[1],
            sequenceId: 2,
            queueItemId: 1,
            ResourceExchangeDeltaFlyoutKind.OutputGranted,
            ResourceExchangeResultKind.QueueCompleted,
            ResourceExchangeResourceKind.Credits,
            93);
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
        ResourceExchangeWalletComponent wallet = default)
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
            MaxQueueItems = 3,
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
        em.AddBuffer<ResourceExchangeDeltaFlyoutComponent>(entity);
        return entity;
    }

    private static void AddRecipe(EntityManager em, Entity exchange, ResourceExchangeRecipeComponent recipe)
    {
        em.GetBuffer<ResourceExchangeRecipeComponent>(exchange).Add(recipe);
    }

    private static ResourceExchangeRecipeComponent ExportOilRecipe()
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
            OutputPerInput = 0.55f,
            FeePercent = 0.15f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 2f,
            Enabled = 1
        };
    }

    private static ResourceExchangeRecipeComponent RushableRecipe(int secondsPerTicket, int maxTickets)
    {
        ResourceExchangeRecipeComponent recipe = ExportOilRecipe();
        recipe.RecipeId = new FixedString128Bytes("exchange.rush.test");
        recipe.DisplayName = new FixedString128Bytes("Rushable Test");
        recipe.RushTicketSecondsPerTicket = secondsPerTicket;
        recipe.MaxRushTickets = maxTickets;
        return recipe;
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        int reservedInputAmount = 100,
        int outputAmount = 100,
        float remainingSeconds = 30f)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.rush.test"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmount = reservedInputAmount,
            ReservedInputAmount = reservedInputAmount,
            OutputAmount = outputAmount,
            State = ResourceExchangeQueueState.InProgress,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = 30f,
            RemainingSeconds = remainingSeconds,
            Version = 1
        };
    }

    private static void UpdateValidationSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void TickQueue(EntityManager em, Entity exchange, float deltaSeconds)
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
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange),
            true,
            deltaSeconds);
        em.SetComponentData(exchange, wallet);
        em.SetComponentData(exchange, summary);
    }

    private static void AssertFlyout(
        in ResourceExchangeDeltaFlyoutComponent flyout,
        int sequenceId,
        int queueItemId,
        ResourceExchangeDeltaFlyoutKind flyoutKind,
        ResourceExchangeResultKind resultKind,
        ResourceExchangeResourceKind resourceKind,
        int amount)
    {
        Assert.AreEqual(sequenceId, flyout.SequenceId);
        Assert.AreEqual(queueItemId, flyout.QueueItemId);
        Assert.AreEqual(1, flyout.FactionId);
        Assert.AreEqual(flyoutKind, flyout.FlyoutKind);
        Assert.AreEqual(resultKind, flyout.ResultKind);
        Assert.AreEqual(resourceKind, flyout.ResourceKind);
        Assert.AreEqual(amount, flyout.Amount);
        Assert.IsFalse(flyout.RecipeId.IsEmpty);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeDeltaFlyoutSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeDeltaFlyoutSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeDeltaFlyoutValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeDeltaFlyoutValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
