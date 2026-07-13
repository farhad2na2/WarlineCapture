using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeToastSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(StartRequest_Accepted_EmitsQueueStartedToast),
                test => test.StartRequest_Accepted_EmitsQueueStartedToast(),
                ref passed);
            RunValidationStep(
                nameof(StartRequest_Rejected_UsesTypedReasonText),
                test => test.StartRequest_Rejected_UsesTypedReasonText(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_Completed_EmitsCompletionToast),
                test => test.TickQueue_Completed_EmitsCompletionToast(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_RefundsReservedInput_EmitsCancelledToast),
                test => test.CancelRequest_RefundsReservedInput_EmitsCancelledToast(),
                ref passed);
            RunValidationStep(
                nameof(RushRequest_CompletesImmediately_EmitsCompletionAndRushToasts),
                test => test.RushRequest_CompletesImmediately_EmitsCompletionAndRushToasts(),
                ref passed);
            RunValidationStep(
                nameof(ReasonTextUtility_MapsCommonTypedReasons),
                test => test.ReasonTextUtility_MapsCommonTypedReasons(),
                ref passed);

            Debug.Log($"[ResourceExchangeToastValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeToastValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StartRequest_Accepted_EmitsQueueStartedToast()
    {
        using World world = new(nameof(StartRequest_Accepted_EmitsQueueStartedToast));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe());

        ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeToastComponent> toasts = em.GetBuffer<ResourceExchangeToastComponent>(exchange);
        Assert.AreEqual(1, toasts.Length);
        AssertToast(
            toasts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeToastKind.QueueStarted,
            ResourceExchangeToastSeverity.Info,
            ResourceExchangeResultKind.RequestAccepted,
            ResourceExchangeReason.None,
            "EXCHANGE QUEUED",
            "Exchange route queued.");
    }

    [Test]
    public void StartRequest_Rejected_UsesTypedReasonText()
    {
        using World world = new(nameof(StartRequest_Rejected_UsesTypedReasonText));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 50);
        AddRecipe(em, exchange, ExportOilRecipe());

        ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);

        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeToastComponent> toasts = em.GetBuffer<ResourceExchangeToastComponent>(exchange);
        Assert.AreEqual(1, toasts.Length);
        AssertToast(
            toasts[0],
            sequenceId: 1,
            queueItemId: 0,
            ResourceExchangeToastKind.Rejected,
            ResourceExchangeToastSeverity.Error,
            ResourceExchangeResultKind.RequestRejected,
            ResourceExchangeReason.InsufficientOil,
            "EXCHANGE BLOCKED",
            "Not enough Oil.");
    }

    [Test]
    public void TickQueue_Completed_EmitsCompletionToast()
    {
        using World world = new(nameof(TickQueue_Completed_EmitsCompletionToast));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Credits,
            reservedInputAmount: 0,
            outputAmount: 93,
            remainingSeconds: 0.1f));

        TickQueue(em, exchange, 0.2f);

        DynamicBuffer<ResourceExchangeToastComponent> toasts = em.GetBuffer<ResourceExchangeToastComponent>(exchange);
        Assert.AreEqual(1, toasts.Length);
        AssertToast(
            toasts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeToastKind.QueueCompleted,
            ResourceExchangeToastSeverity.Success,
            ResourceExchangeResultKind.QueueCompleted,
            ResourceExchangeReason.None,
            "EXCHANGE COMPLETE",
            "Exchange output received.");
        Assert.AreEqual(93, toasts[0].OutputAmount);
    }

    [Test]
    public void CancelRequest_RefundsReservedInput_EmitsCancelledToast()
    {
        using World world = new(nameof(CancelRequest_RefundsReservedInput_EmitsCancelledToast));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        ResourceExchangeQueueComponent item = CreateQueueItem(reservedInputAmount: 200);
        Assert.IsTrue(ResourceExchangePhysicalStorageTestHelper.TryReserve(em, exchange, item, out _));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(item);

        ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 0);
        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeToastComponent> toasts = em.GetBuffer<ResourceExchangeToastComponent>(exchange);
        Assert.AreEqual(1, toasts.Length);
        AssertToast(
            toasts[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeToastKind.QueueCancelled,
            ResourceExchangeToastSeverity.Warning,
            ResourceExchangeResultKind.QueueCancelled,
            ResourceExchangeReason.None,
            "EXCHANGE CANCELLED",
            "Exchange cancelled. Reserved resources refunded.");
        Assert.AreEqual(200, toasts[0].InputAmount);
    }

    [Test]
    public void RushRequest_CompletesImmediately_EmitsCompletionAndRushToasts()
    {
        using World world = new(nameof(RushRequest_CompletesImmediately_EmitsCompletionAndRushToasts));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(
            em,
            wallet: new ResourceExchangeWalletComponent { RushTickets = 3 },
            oil: 100);
        AddRecipe(em, exchange, RushableRecipe(secondsPerTicket: 30, maxTickets: 3));
        ResourceExchangeQueueComponent item = CreateQueueItem(remainingSeconds: 10f, outputAmount: 93);
        Assert.IsTrue(ResourceExchangePhysicalStorageTestHelper.TryReserve(em, exchange, item, out _));
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(item);

        ResourceExchangeRequestValidationSystem.EnqueueRushRequest(em, exchange, 1, 1, 1, 0);
        UpdateValidationSystem(world);

        DynamicBuffer<ResourceExchangeToastComponent> toasts = em.GetBuffer<ResourceExchangeToastComponent>(exchange);
        Assert.AreEqual(2, toasts.Length);
        AssertHasToast(toasts, ResourceExchangeToastKind.QueueCompleted, ResourceExchangeResultKind.QueueCompleted);
        AssertHasToast(toasts, ResourceExchangeToastKind.RushAccepted, ResourceExchangeResultKind.RushAccepted);
    }

    [Test]
    public void ReasonTextUtility_MapsCommonTypedReasons()
    {
        Assert.AreEqual("Not enough Credits.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.InsufficientCredits).ToString());
        Assert.AreEqual("Not enough Materials.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.InsufficientMaterials).ToString());
        Assert.AreEqual("Not enough Oil.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.InsufficientOil).ToString());
        Assert.AreEqual("Not enough Fuel.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.InsufficientFuel).ToString());
        Assert.AreEqual("Not enough Rush Tickets.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.InsufficientRushTickets).ToString());
        Assert.AreEqual("Exchange queue is full.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.QueueFull).ToString());
        Assert.AreEqual("Output storage is full.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.StorageFull).ToString());
        Assert.AreEqual("Mission is ending.", ResourceExchangeToastTextUtility.ResolveReasonBody(ResourceExchangeReason.MissionEnding).ToString());
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
        ResourceExchangeWalletComponent wallet = default,
        float oil = 0f,
        float fuel = 0f,
        int oilCapacity = 1000,
        int fuelCapacity = 1000)
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

        if (wallet.FactionId == 0)
            wallet.FactionId = 1;
        em.SetComponentData(entity, wallet);
        em.SetComponentData(entity, new FactionEconomy { FactionId = wallet.FactionId });
        em.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = wallet.FactionId,
            Capacity = 1000
        });
        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeResultComponent>(entity);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(entity);
        em.AddBuffer<ResourceExchangeDeltaFlyoutComponent>(entity);
        em.AddBuffer<ResourceExchangeToastComponent>(entity);
        ResourceExchangePhysicalStorageTestHelper.AddStorage(
            em,
            entity,
            wallet.FactionId,
            oil,
            fuel,
            oilCapacity,
            fuelCapacity);
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
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
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
            InputResource = inputResource,
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
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(exchange);
        FactionTacticalMaterialsComponent materials = em.GetComponentData<FactionTacticalMaterialsComponent>(exchange);
        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        ResourceExchangeSummaryComponent summary = em.GetComponentData<ResourceExchangeSummaryComponent>(exchange);
        ResourceExchangeQueueTickSystem.TickQueue(
            enabled,
            ref economy,
            ref materials,
            ref wallet,
            ref summary,
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            em.GetBuffer<ResourceExchangeResultComponent>(exchange),
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange),
            em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchange),
            true,
            em.GetBuffer<ResourceExchangeToastComponent>(exchange),
            true,
            default,
            false,
            deltaSeconds,
            em,
            em.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchange),
            usePhysicalStorage: true);
        em.SetComponentData(exchange, economy);
        em.SetComponentData(exchange, materials);
        em.SetComponentData(exchange, wallet);
        em.SetComponentData(exchange, summary);
    }

    private static void AssertToast(
        in ResourceExchangeToastComponent toast,
        int sequenceId,
        int queueItemId,
        ResourceExchangeToastKind toastKind,
        ResourceExchangeToastSeverity severity,
        ResourceExchangeResultKind resultKind,
        ResourceExchangeReason reason,
        string title,
        string body)
    {
        Assert.AreEqual(sequenceId, toast.SequenceId);
        Assert.AreEqual(queueItemId, toast.QueueItemId);
        Assert.AreEqual(1, toast.FactionId);
        Assert.AreEqual(toastKind, toast.ToastKind);
        Assert.AreEqual(severity, toast.Severity);
        Assert.AreEqual(resultKind, toast.ResultKind);
        Assert.AreEqual(reason, toast.Reason);
        Assert.AreEqual(title, toast.Title.ToString());
        Assert.AreEqual(body, toast.Body.ToString());
    }

    private static void AssertHasToast(
        DynamicBuffer<ResourceExchangeToastComponent> toasts,
        ResourceExchangeToastKind toastKind,
        ResourceExchangeResultKind resultKind)
    {
        for (int i = 0; i < toasts.Length; i++)
        {
            if (toasts[i].ToastKind == toastKind && toasts[i].ResultKind == resultKind)
                return;
        }

        Assert.Fail($"Expected toast kind={toastKind} result={resultKind}.");
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeToastSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeToastSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeToastValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeToastValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
