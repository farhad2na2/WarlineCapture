using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeAriaAnnouncementSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(StartRequest_Accepted_EmitsExchangeStartedAnnouncement),
                test => test.StartRequest_Accepted_EmitsExchangeStartedAnnouncement(),
                ref passed);
            RunValidationStep(
                nameof(StartRequest_Rejected_EmitsInsufficientResourceAnnouncement),
                test => test.StartRequest_Rejected_EmitsInsufficientResourceAnnouncement(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_Completed_EmitsExchangeCompleteAnnouncement),
                test => test.TickQueue_Completed_EmitsExchangeCompleteAnnouncement(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_StorageFull_EmitsExchangeBlockedAnnouncement),
                test => test.TickQueue_StorageFull_EmitsExchangeBlockedAnnouncement(),
                ref passed);
            RunValidationStep(
                nameof(AriaTextUtility_MapsRequiredAnnouncementStrings),
                test => test.AriaTextUtility_MapsRequiredAnnouncementStrings(),
                ref passed);

            Debug.Log($"[ResourceExchangeAriaAnnouncementValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAriaAnnouncementValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StartRequest_Accepted_EmitsExchangeStartedAnnouncement()
    {
        using World world = new(nameof(StartRequest_Accepted_EmitsExchangeStartedAnnouncement));
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

        DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements =
            em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchange);
        Assert.AreEqual(1, announcements.Length);
        AssertAnnouncement(
            announcements[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeAriaAnnouncementKind.ExchangeStarted,
            AssistantMessagePriority.Low,
            ResourceExchangeResultKind.RequestAccepted,
            ResourceExchangeReason.None,
            "Exchange queued. Logistics timer started.");
    }

    [Test]
    public void StartRequest_Rejected_EmitsInsufficientResourceAnnouncement()
    {
        using World world = new(nameof(StartRequest_Rejected_EmitsInsufficientResourceAnnouncement));
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

        DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements =
            em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchange);
        Assert.AreEqual(1, announcements.Length);
        AssertAnnouncement(
            announcements[0],
            sequenceId: 1,
            queueItemId: 0,
            ResourceExchangeAriaAnnouncementKind.InsufficientResources,
            AssistantMessagePriority.High,
            ResourceExchangeResultKind.RequestRejected,
            ResourceExchangeReason.InsufficientOil,
            "Not enough Oil for this exchange.");
    }

    [Test]
    public void TickQueue_Completed_EmitsExchangeCompleteAnnouncement()
    {
        using World world = new(nameof(TickQueue_Completed_EmitsExchangeCompleteAnnouncement));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Credits,
            reservedInputAmount: 0,
            outputAmount: 93,
            remainingSeconds: 0.1f));

        TickQueue(em, exchange, 0.2f);

        DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements =
            em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchange);
        Assert.AreEqual(1, announcements.Length);
        AssertAnnouncement(
            announcements[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeAriaAnnouncementKind.ExchangeComplete,
            AssistantMessagePriority.Normal,
            ResourceExchangeResultKind.QueueCompleted,
            ResourceExchangeReason.None,
            "Exchange complete. Resources received.");
    }

    [Test]
    public void TickQueue_StorageFull_EmitsExchangeBlockedAnnouncement()
    {
        using World world = new(nameof(TickQueue_StorageFull_EmitsExchangeBlockedAnnouncement));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, fuel: 65, fuelCapacity: 100);
        ResourceExchangeQueueComponent item = CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Credits,
            outputResource: ResourceExchangeResourceKind.Fuel,
            reservedInputAmount: 0,
            outputAmount: 25,
            remainingSeconds: 10f);
        Assert.IsTrue(ResourceExchangePhysicalStorageTestHelper.TryReserve(em, exchange, item, out _));
        BuildingResourceStorageComponent storage = ResourceExchangePhysicalStorageTestHelper.GetStorage(em);
        storage.StoredFuelBarrels = 90f;
        ResourceExchangePhysicalStorageTestHelper.SetStorage(em, storage);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(item);

        TickQueue(em, exchange, 0.2f);

        DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements =
            em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchange);
        Assert.AreEqual(1, announcements.Length);
        AssertAnnouncement(
            announcements[0],
            sequenceId: 1,
            queueItemId: 1,
            ResourceExchangeAriaAnnouncementKind.ExchangeBlocked,
            AssistantMessagePriority.High,
            ResourceExchangeResultKind.QueueBlocked,
            ResourceExchangeReason.StorageFull,
            "Exchange blocked. Output storage is full.");
    }

    [Test]
    public void AriaTextUtility_MapsRequiredAnnouncementStrings()
    {
        ResourceExchangeResultComponent insufficientOil = Result(ResourceExchangeReason.InsufficientOil);
        ResourceExchangeResultComponent started = Result(
            ResourceExchangeReason.None,
            ResourceExchangeResultKind.RequestAccepted,
            accepted: 1,
            queueItemId: 7,
            recipeId: new FixedString128Bytes("exchange.export_oil_credits.standard"));
        ResourceExchangeResultComponent complete = Result(
            ResourceExchangeReason.None,
            ResourceExchangeResultKind.QueueCompleted,
            accepted: 1,
            queueItemId: 7,
            recipeId: new FixedString128Bytes("exchange.export_oil_credits.standard"));
        ResourceExchangeResultComponent blocked = Result(ResourceExchangeReason.StorageMissing);

        Assert.AreEqual(
            "Not enough Oil for this exchange.",
            ResourceExchangeAriaTextUtility.ResolveAnnouncementText(
                insufficientOil,
                ResourceExchangeAriaAnnouncementKind.InsufficientResources).ToString());
        Assert.AreEqual(
            "Exchange queued. Logistics timer started.",
            ResourceExchangeAriaTextUtility.ResolveAnnouncementText(
                started,
                ResourceExchangeAriaAnnouncementKind.ExchangeStarted).ToString());
        Assert.AreEqual(
            "Exchange complete. Resources received.",
            ResourceExchangeAriaTextUtility.ResolveAnnouncementText(
                complete,
                ResourceExchangeAriaAnnouncementKind.ExchangeComplete).ToString());
        Assert.AreEqual(
            "Exchange blocked. Required storage is missing.",
            ResourceExchangeAriaTextUtility.ResolveAnnouncementText(
                blocked,
                ResourceExchangeAriaAnnouncementKind.ExchangeBlocked).ToString());
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
        em.AddBuffer<ResourceExchangeAriaAnnouncementComponent>(entity);
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

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
        ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Credits,
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
            OutputResource = outputResource,
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

    private static ResourceExchangeResultComponent Result(
        ResourceExchangeReason reason,
        ResourceExchangeResultKind resultKind = ResourceExchangeResultKind.RequestRejected,
        byte accepted = 0,
        int queueItemId = 0,
        FixedString128Bytes recipeId = default)
    {
        return new ResourceExchangeResultComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            ResultKind = resultKind,
            Accepted = accepted,
            Reason = reason,
            RecipeId = recipeId
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
            default,
            false,
            default,
            false,
            em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchange),
            true,
            deltaSeconds,
            em,
            em.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchange),
            usePhysicalStorage: true);
        em.SetComponentData(exchange, economy);
        em.SetComponentData(exchange, materials);
        em.SetComponentData(exchange, wallet);
        em.SetComponentData(exchange, summary);
    }

    private static void AssertAnnouncement(
        in ResourceExchangeAriaAnnouncementComponent announcement,
        int sequenceId,
        int queueItemId,
        ResourceExchangeAriaAnnouncementKind kind,
        AssistantMessagePriority priority,
        ResourceExchangeResultKind resultKind,
        ResourceExchangeReason reason,
        string text)
    {
        Assert.AreEqual(sequenceId, announcement.SequenceId);
        Assert.AreEqual(queueItemId, announcement.QueueItemId);
        Assert.AreEqual(1, announcement.FactionId);
        Assert.AreEqual(kind, announcement.AnnouncementKind);
        Assert.AreEqual(priority, announcement.Priority);
        Assert.AreEqual(resultKind, announcement.ResultKind);
        Assert.AreEqual(reason, announcement.Reason);
        Assert.AreEqual(text, announcement.Text.ToString());
        Assert.IsFalse(announcement.SuppressionKey.IsEmpty);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeAriaAnnouncementSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeAriaAnnouncementSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeAriaAnnouncementValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAriaAnnouncementValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
