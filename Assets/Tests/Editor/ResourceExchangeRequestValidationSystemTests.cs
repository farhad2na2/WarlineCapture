using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeRequestValidationSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(StartRequest_Accepted_ReservesInputAndCreatesQueueItem),
                test => test.StartRequest_Accepted_ReservesInputAndCreatesQueueItem(),
                ref passed);
            RunValidationStep(
                nameof(Update_TwoExchangeEntities_ProcessesEachQueueInOrder),
                test => test.Update_TwoExchangeEntities_ProcessesEachQueueInOrder(),
                ref passed);
            RunValidationStep(
                nameof(ClearCompleted_RemovesOnlyCompletedRowsForFaction),
                test => test.ClearCompleted_RemovesOnlyCompletedRowsForFaction(),
                ref passed);
            RunValidationStep(
                nameof(Summary_CarriesAiExchangeGateFromRuntimeState),
                test => test.Summary_CarriesAiExchangeGateFromRuntimeState(),
                ref passed);

            Debug.Log($"[ResourceExchangeRequestValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeRequestValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StartRequest_Accepted_ReservesInputAndCreatesQueueItem()
    {
        using World world = new(nameof(StartRequest_Accepted_ReservesInputAndCreatesQueueItem));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            200,
            1,
            42);

        UpdateSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeReason.None, result.Reason);
        Assert.AreEqual(ResourceExchangeResultKind.RequestAccepted, result.ResultKind);
        Assert.AreEqual(200, result.InputAmount);
        Assert.AreEqual(93, result.OutputAmount);

        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(500f, GetStoredOil(em, 1), 0.001f);
        Assert.AreEqual(200f, GetReservedOil(em, 1), 0.001f);
        Assert.AreEqual(0u, wallet.Version);

        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(1, queue.Length);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[0].State);
        Assert.AreEqual(200, queue[0].ReservedInputAmount);
        Assert.AreEqual(93, queue[0].OutputAmount);
        Assert.AreEqual(32f, queue[0].DurationSeconds);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events = em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(-200, events[0].Amount);
        Assert.AreEqual(ResourceExchangeResourceKind.Oil, events[0].ResourceKind);
    }

    [Test]
    public void Update_TwoExchangeEntities_ProcessesEachQueueInOrder()
    {
        using World world = new(nameof(Update_TwoExchangeEntities_ProcessesEachQueueInOrder));
        EntityManager em = world.EntityManager;
        Entity exportExchange = CreateExchangeEntity(
            em,
            factionId: 1,
            oil: 700,
            maxQueueItems: 2);
        Entity importExchange = CreateExchangeEntity(
            em,
            allowAiExchange: true,
            factionId: 2,
            oil: 1000,
            fuel: 100,
            maxQueueItems: 2,
            credits: 1000);
        AddRecipe(em, exportExchange, ExportOilRecipe());
        AddRecipe(em, importExchange, ImportFuelRecipe());

        FixedString128Bytes exportRecipeId = new("exchange.convert_oil_materials.test");
        FixedString128Bytes importRecipeId = new("exchange.import_fuel_credits.standard");
        int firstExportRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exportExchange,
            exportRecipeId,
            200,
            1,
            10);
        int firstImportRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            importExchange,
            importRecipeId,
            100,
            2,
            11);
        int secondExportRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exportExchange,
            exportRecipeId,
            100,
            1,
            12);
        int secondImportRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            importExchange,
            importRecipeId,
            300,
            2,
            13);

        UpdateSystem(world);

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRequestComponent>(exportExchange).Length);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRequestComponent>(importExchange).Length);

        ResourceExchangeRequestQueueComponent exportRequestQueue =
            em.GetComponentData<ResourceExchangeRequestQueueComponent>(exportExchange);
        Assert.AreEqual(2, exportRequestQueue.LastRequestId);
        Assert.AreEqual(2, exportRequestQueue.LastQueueItemId);
        ResourceExchangeRequestQueueComponent importRequestQueue =
            em.GetComponentData<ResourceExchangeRequestQueueComponent>(importExchange);
        Assert.AreEqual(2, importRequestQueue.LastRequestId);
        Assert.AreEqual(2, importRequestQueue.LastQueueItemId);

        ResourceExchangeWalletComponent exportWallet =
            em.GetComponentData<ResourceExchangeWalletComponent>(exportExchange);
        Assert.AreEqual(1, exportWallet.FactionId);
        Assert.AreEqual(700f, GetStoredOil(em, 1), 0.001f);
        Assert.AreEqual(300f, GetReservedOil(em, 1), 0.001f);
        Assert.AreEqual(0, em.GetComponentData<FactionEconomy>(exportExchange).Money);
        Assert.AreEqual(0u, exportWallet.Version);
        ResourceExchangeWalletComponent importWallet =
            em.GetComponentData<ResourceExchangeWalletComponent>(importExchange);
        Assert.AreEqual(2, importWallet.FactionId);
        Assert.AreEqual(1000, em.GetComponentData<FactionEconomy>(importExchange).Money);
        Assert.AreEqual(1000f, GetStoredOil(em, 2), 0.001f);
        Assert.AreEqual(400f, GetReservedOil(em, 2), 0.001f);
        Assert.AreEqual(100f, GetStoredFuel(em, 2), 0.001f);
        Assert.AreEqual(0u, importWallet.Version);

        DynamicBuffer<ResourceExchangeQueueComponent> exportQueue =
            em.GetBuffer<ResourceExchangeQueueComponent>(exportExchange);
        Assert.AreEqual(2, exportQueue.Length);
        AssertQueueItem(exportQueue[0], 1, 1, exportRecipeId, 200, 93, 32f);
        AssertQueueItem(exportQueue[1], 2, 1, exportRecipeId, 100, 46, 30f);
        DynamicBuffer<ResourceExchangeQueueComponent> importQueue =
            em.GetBuffer<ResourceExchangeQueueComponent>(importExchange);
        Assert.AreEqual(2, importQueue.Length);
        AssertQueueItem(importQueue[0], 1, 2, importRecipeId, 100, 50, 30f);
        AssertQueueItem(importQueue[1], 2, 2, importRecipeId, 300, 150, 34f);

        DynamicBuffer<ResourceExchangeResultComponent> exportResults =
            em.GetBuffer<ResourceExchangeResultComponent>(exportExchange);
        Assert.AreEqual(2, exportResults.Length);
        AssertAcceptedResult(exportResults[0], firstExportRequest, 1, 1, exportRecipeId, 200, 93);
        AssertAcceptedResult(exportResults[1], secondExportRequest, 2, 1, exportRecipeId, 100, 46);
        DynamicBuffer<ResourceExchangeResultComponent> importResults =
            em.GetBuffer<ResourceExchangeResultComponent>(importExchange);
        Assert.AreEqual(2, importResults.Length);
        AssertAcceptedResult(importResults[0], firstImportRequest, 1, 2, importRecipeId, 100, 50);
        AssertAcceptedResult(importResults[1], secondImportRequest, 2, 2, importRecipeId, 300, 150);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> exportEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exportExchange);
        Assert.AreEqual(2, exportEvents.Length);
        AssertEconomyEvent(exportEvents[0], 1, 1, exportRecipeId, ResourceExchangeResourceKind.Oil, -200);
        AssertEconomyEvent(exportEvents[1], 2, 1, exportRecipeId, ResourceExchangeResourceKind.Oil, -100);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> importEvents =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(importExchange);
        Assert.AreEqual(2, importEvents.Length);
        AssertEconomyEvent(importEvents[0], 1, 2, importRecipeId, ResourceExchangeResourceKind.Oil, -100);
        AssertEconomyEvent(importEvents[1], 2, 2, importRecipeId, ResourceExchangeResourceKind.Oil, -300);

        ResourceExchangeSummaryComponent exportSummary =
            em.GetComponentData<ResourceExchangeSummaryComponent>(exportExchange);
        AssertSummary(exportSummary, factionId: 1, allowAiExchange: 0);
        ResourceExchangeSummaryComponent importSummary =
            em.GetComponentData<ResourceExchangeSummaryComponent>(importExchange);
        AssertSummary(importSummary, factionId: 2, allowAiExchange: 1);
    }

    [Test]
    public void StartRequest_Disabled_DoesNotSpendOrQueue()
    {
        using World world = new(nameof(StartRequest_Disabled_DoesNotSpendOrQueue));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(
            em,
            enabled: false,
            oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            200,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, requestId, ResourceExchangeReason.ExchangeUnavailable);
        Assert.AreEqual(500f, GetStoredOil(em, 1), 0.001f);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void Summary_CarriesAiExchangeGateFromRuntimeState()
    {
        using World world = new(nameof(Summary_CarriesAiExchangeGateFromRuntimeState));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(
            em,
            allowAiExchange: true,
            oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            200,
            1,
            0);

        UpdateSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);

        ResourceExchangeSummaryComponent summary = em.GetComponentData<ResourceExchangeSummaryComponent>(exchange);
        Assert.AreEqual(1, summary.AllowAiExchange);
        Assert.AreEqual(1, summary.AllowRush);
        Assert.AreEqual(1, summary.AllowWorldPresentation);
    }

    [Test]
    public void StartRequest_RejectsMissingRecipeLockedMissionAndInvalidAmount()
    {
        using World world = new(nameof(StartRequest_RejectsMissingRecipeLockedMissionAndInvalidAmount));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe(missionTag: "mission.other"));

        int missingRecipeRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.missing"),
            200,
            1,
            0);
        int lockedRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            200,
            1,
            0);
        int stepRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            250,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, missingRecipeRequest, ResourceExchangeReason.RecipeLocked);
        AssertRejected(em, exchange, lockedRequest, ResourceExchangeReason.RecipeLocked);
        AssertRejected(em, exchange, stepRequest, ResourceExchangeReason.RecipeLocked);
        Assert.AreEqual(500f, GetStoredOil(em, 1), 0.001f);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsInsufficientInput()
    {
        using World world = new(nameof(StartRequest_RejectsInsufficientInput));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 100);
        AddRecipe(em, exchange, ExportOilRecipe());

        int insufficient = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            200,
            1,
            0);
        UpdateSystem(world);

        AssertRejected(em, exchange, insufficient, ResourceExchangeReason.InsufficientOil);
        Assert.AreEqual(100f, GetStoredOil(em, 1), 0.001f);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsStorageFull()
    {
        using World world = new(nameof(StartRequest_RejectsStorageFull));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, fuel: 990, fuelCapacity: 1000, credits: 1000);
        AddRecipe(em, exchange, ImportFuelRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.import_fuel_credits.standard"),
            100,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, requestId, ResourceExchangeReason.StorageFull);
        Assert.AreEqual(1000, em.GetComponentData<FactionEconomy>(exchange).Money);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsQueueFull()
    {
        using World world = new(nameof(StartRequest_RejectsQueueFull));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        AddRecipe(em, exchange, ExportOilRecipe());
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 12,
            FactionId = 1,
            State = ResourceExchangeQueueState.InProgress
        });
        int queueFull = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.convert_oil_materials.test"),
            100,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, queueFull, ResourceExchangeReason.QueueFull);
        Assert.AreEqual(500f, GetStoredOil(em, 1), 0.001f);
    }

    [Test]
    public void ClearCompleted_RemovesOnlyCompletedRowsForFaction()
    {
        using World world = new(nameof(ClearCompleted_RemovesOnlyCompletedRowsForFaction));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 1,
            FactionId = 1,
            State = ResourceExchangeQueueState.Completed
        });
        queue.Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 2,
            FactionId = 1,
            State = ResourceExchangeQueueState.InProgress
        });
        queue.Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 3,
            FactionId = 2,
            State = ResourceExchangeQueueState.Completed
        });

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueClearCompletedRequest(em, exchange, 1, 0);
        UpdateSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(
            em,
            exchange,
            requestId,
            out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.RequestAccepted, result.ResultKind);
        Assert.AreEqual(1, result.InputAmount);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(2, queue.Length);
        Assert.AreEqual(2, queue[0].QueueItemId);
        Assert.AreEqual(3, queue[1].QueueItemId);
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
        bool enabled = true,
        bool allowAiExchange = false,
        ResourceExchangeWalletComponent wallet = default,
        byte factionId = 1,
        int maxQueueItems = 1,
        int credits = 0,
        int materials = 0,
        int materialsCapacity = 1000,
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
            Enabled = enabled ? (byte)1 : (byte)0,
            FactionId = factionId,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            AllowAiExchange = allowAiExchange ? (byte)1 : (byte)0,
            MaxQueueItems = maxQueueItems,
            ScenarioTag = new FixedString64Bytes("mission.active")
        });

        if (wallet.FactionId == 0)
            wallet.FactionId = factionId;
        em.SetComponentData(entity, wallet);
        em.SetComponentData(entity, new FactionEconomy { FactionId = wallet.FactionId, Money = credits });
        em.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = wallet.FactionId,
            Current = materials,
            Capacity = materialsCapacity
        });
        em.AddBuffer<ResourceExchangeRecipeComponent>(entity);
        em.AddBuffer<ResourceExchangeRequestComponent>(entity);
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeResultComponent>(entity);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(entity);
        em.AddBuffer<ResourceExchangePhysicalReservationComponent>(entity);
        Entity storage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = entity.Index + 1,
            OwnerFactionId = factionId,
            StoredOilBarrels = oil,
            StoredFuelBarrels = fuel,
            OilStorageCapacity = oilCapacity,
            FuelStorageCapacity = fuelCapacity
        });
        return entity;
    }

    private static float GetStoredOil(EntityManager em, byte factionId)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        using NativeArray<BuildingResourceStorageComponent> storages = query
            .ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
        float total = 0f;
        for (int i = 0; i < storages.Length; i++)
        {
            if (storages[i].OwnerFactionId == factionId)
                total += storages[i].StoredOilBarrels;
        }

        query.Dispose();
        return total;
    }

    private static float GetStoredFuel(EntityManager em, byte factionId)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        using NativeArray<BuildingResourceStorageComponent> storages = query
            .ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
        float total = 0f;
        for (int i = 0; i < storages.Length; i++)
        {
            if (storages[i].OwnerFactionId == factionId)
                total += storages[i].StoredFuelBarrels;
        }

        query.Dispose();
        return total;
    }

    private static float GetReservedOil(EntityManager em, byte factionId)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        using NativeArray<BuildingResourceStorageComponent> storages =
            query.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
        float total = 0f;
        for (int i = 0; i < storages.Length; i++)
        {
            if (storages[i].OwnerFactionId == factionId)
                total += storages[i].ReservedOilOutboundBarrels;
        }

        query.Dispose();
        return total;
    }

    private static void AddRecipe(EntityManager em, Entity exchange, ResourceExchangeRecipeComponent recipe)
    {
        em.GetBuffer<ResourceExchangeRecipeComponent>(exchange).Add(recipe);
    }

    private static void AssertQueueItem(
        ResourceExchangeQueueComponent item,
        int queueItemId,
        byte factionId,
        FixedString128Bytes recipeId,
        int inputAmount,
        int outputAmount,
        float durationSeconds)
    {
        Assert.AreEqual(queueItemId, item.QueueItemId);
        Assert.AreEqual(factionId, item.FactionId);
        Assert.AreEqual(recipeId, item.RecipeId);
        Assert.AreEqual(inputAmount, item.InputAmount);
        Assert.AreEqual(inputAmount, item.ReservedInputAmount);
        Assert.AreEqual(outputAmount, item.OutputAmount);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, item.State);
        Assert.AreEqual(ResourceExchangeReason.None, item.StateReason);
        Assert.AreEqual(durationSeconds, item.DurationSeconds);
        Assert.AreEqual(durationSeconds, item.RemainingSeconds);
        Assert.AreEqual(1u, item.Version);
    }

    private static void AssertAcceptedResult(
        ResourceExchangeResultComponent result,
        int requestId,
        int queueItemId,
        byte factionId,
        FixedString128Bytes recipeId,
        int inputAmount,
        int outputAmount)
    {
        Assert.AreEqual(requestId, result.RequestId);
        Assert.AreEqual(queueItemId, result.QueueItemId);
        Assert.AreEqual(factionId, result.FactionId);
        Assert.AreEqual(ResourceExchangeResultKind.RequestAccepted, result.ResultKind);
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeReason.None, result.Reason);
        Assert.AreEqual(recipeId, result.RecipeId);
        Assert.AreEqual(inputAmount, result.InputAmount);
        Assert.AreEqual(outputAmount, result.OutputAmount);
    }

    private static void AssertEconomyEvent(
        ResourceExchangeEconomyEventComponent economyEvent,
        int queueItemId,
        byte factionId,
        FixedString128Bytes recipeId,
        ResourceExchangeResourceKind resourceKind,
        int amount)
    {
        Assert.AreEqual(queueItemId, economyEvent.QueueItemId);
        Assert.AreEqual(factionId, economyEvent.FactionId);
        Assert.AreEqual(ResourceExchangeResultKind.QueueStarted, economyEvent.ResultKind);
        Assert.AreEqual(resourceKind, economyEvent.ResourceKind);
        Assert.AreEqual(amount, economyEvent.Amount);
        Assert.AreEqual(recipeId, economyEvent.RecipeId);
    }

    private static void AssertSummary(
        ResourceExchangeSummaryComponent summary,
        byte factionId,
        byte allowAiExchange)
    {
        Assert.AreEqual(factionId, summary.FactionId);
        Assert.AreEqual(1, summary.Enabled);
        Assert.AreEqual(1, summary.AllowRush);
        Assert.AreEqual(1, summary.AllowWorldPresentation);
        Assert.AreEqual(allowAiExchange, summary.AllowAiExchange);
        Assert.AreEqual(2, summary.QueueCount);
        Assert.AreEqual(2, summary.ActiveCount);
        Assert.AreEqual(0, summary.CompletedCount);
        Assert.AreEqual(2, summary.MaxQueueItems);
        Assert.AreEqual(ResourceExchangeReason.None, summary.LastReason);
        Assert.AreEqual(2u, summary.Version);
    }

    private static ResourceExchangeRecipeComponent ExportOilRecipe(string missionTag = "")
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.convert_oil_materials.test"),
            DisplayName = new FixedString128Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Oil,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 0.55f,
            FeePercent = 0.15f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 2f,
            Enabled = 1,
            MissionTag = new FixedString64Bytes(missionTag)
        };
    }

    private static ResourceExchangeRecipeComponent ImportFuelRecipe()
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.import_fuel_credits.standard"),
            DisplayName = new FixedString128Bytes("Import Fuel"),
            RouteType = ResourceExchangeRouteType.Import,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Fuel,
            InputAmountMin = 100,
            InputAmountMax = 1000,
            InputStep = 100,
            OutputPerInput = 0.5f,
            FeePercent = 0f,
            DurationSecondsBase = 30f,
            DurationSecondsPerStep = 2f,
            RequiresStorage = 1,
            Enabled = 1
        };
    }

    private static void UpdateSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void AssertRejected(
        EntityManager em,
        Entity exchange,
        int requestId,
        ResourceExchangeReason reason)
    {
        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.RequestRejected, result.ResultKind);
        Assert.AreEqual(reason, result.Reason);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeRequestValidationSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeRequestValidationSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeRequestValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeRequestValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
