using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeQueueTickSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(TickQueue_CompletesOnceAndGrantsOutput),
                test => test.TickQueue_CompletesOnceAndGrantsOutput(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable),
                test => test.TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable(),
                ref passed);
            RunValidationStep(
                nameof(TickQueue_DefersOutputStorageValidationUntilArrival),
                test => test.TickQueue_DefersOutputStorageValidationUntilArrival(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_RefundsReservedInputBeforePresentation),
                test => test.CancelRequest_RefundsReservedInputBeforePresentation(),
                ref passed);
            RunValidationStep(
                nameof(CancelRequest_DoesNotRefundAfterPresentationStarted),
                test => test.CancelRequest_DoesNotRefundAfterPresentationStarted(),
                ref passed);
            RunValidationStep(
                nameof(MissionEnd_CancelsActiveJobsAndAppliesRefundPolicy),
                test => test.MissionEnd_CancelsActiveJobsAndAppliesRefundPolicy(),
                ref passed);

            Debug.Log($"[ResourceExchangeQueueTickValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeQueueTickValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void TickQueue_CompletesOnceAndGrantsOutput()
    {
        using World world = new(nameof(TickQueue_CompletesOnceAndGrantsOutput));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Oil,
            outputResource: ResourceExchangeResourceKind.Oil,
            reservedInputAmount: 0,
            outputAmount: 93,
            remainingSeconds: 0.25f));

        Tick(em, exchange, 0.5f);

        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(93, em.GetComponentData<FactionEconomy>(exchange).Money);
        Assert.AreEqual(0u, wallet.Version);
        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(1, queue[0].OutputApplied);
        Assert.AreEqual(0, queue[0].ReservedInputAmount);

        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCompleted, events[0].ResultKind);
        Assert.AreEqual(93, events[0].Amount);

        Tick(em, exchange, 1f);

        wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(93, em.GetComponentData<FactionEconomy>(exchange).Money);
        Assert.AreEqual(1, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);
    }

    [Test]
    public void TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable()
    {
        using World world = new(nameof(TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, fuel: 930, fuelCapacity: 1000);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        ResourceExchangeQueueComponent item = CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Oil,
            outputResource: ResourceExchangeResourceKind.Fuel,
            reservedInputAmount: 0,
            outputAmount: 50,
            remainingSeconds: 0.1f);
        ReservePhysicalResources(em, exchange, item);
        queue.Add(item);
        SetStoredFuel(em, 980f);

        Tick(em, exchange, 0.1f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Blocked, queue[0].State);
        Assert.AreEqual(ResourceExchangeReason.StorageFull, queue[0].StateReason);
        Assert.AreEqual(980f, GetStoredFuel(em), 0.001f);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueBlocked, events[0].ResultKind);
        Assert.AreEqual(ResourceExchangeResourceKind.Fuel, events[0].ResourceKind);
        Assert.AreEqual(0, events[0].Amount);

        SetStoredFuel(em, 940f);

        Tick(em, exchange, 0.1f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(990f, GetStoredFuel(em), 0.001f);
        events = em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(2, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCompleted, events[1].ResultKind);
        Assert.AreEqual(50, events[1].Amount);
    }

    [Test]
    public void TickQueue_DefersOutputStorageValidationUntilArrival()
    {
        using World world = new(nameof(TickQueue_DefersOutputStorageValidationUntilArrival));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, fuel: 930, fuelCapacity: 1000);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        ResourceExchangeQueueComponent item = CreateQueueItem(
            inputResource: ResourceExchangeResourceKind.Oil,
            outputResource: ResourceExchangeResourceKind.Fuel,
            reservedInputAmount: 0,
            outputAmount: 50,
            remainingSeconds: 10f);
        ReservePhysicalResources(em, exchange, item);
        queue.Add(item);
        SetStoredFuel(em, 980f);

        Tick(em, exchange, 0.5f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[0].State);
        Assert.AreEqual(9.5f, queue[0].RemainingSeconds, 0.001f);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);

        Tick(em, exchange, 9.5f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Blocked, queue[0].State);
        Assert.AreEqual(ResourceExchangeReason.StorageFull, queue[0].StateReason);
    }

    [Test]
    public void CancelRequest_RefundsReservedInputBeforePresentation()
    {
        using World world = new(nameof(CancelRequest_RefundsReservedInputBeforePresentation));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        ResourceExchangeQueueComponent item = CreateQueueItem(reservedInputAmount: 200);
        ReservePhysicalResources(em, exchange, item);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(item);

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 20);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCancelled, result.ResultKind);
        Assert.AreEqual(200, result.InputAmount);
        Assert.AreEqual(500f, GetStoredOil(em), 0.001f);
        Assert.AreEqual(ResourceExchangeQueueState.Cancelled, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);
        Assert.AreEqual(1, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);
    }

    [Test]
    public void CancelRequest_DoesNotRefundAfterPresentationStarted()
    {
        using World world = new(nameof(CancelRequest_DoesNotRefundAfterPresentationStarted));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 500);
        ResourceExchangeQueueComponent item = CreateQueueItem(
            reservedInputAmount: 200,
            presentationStarted: 1);
        ReservePhysicalResources(em, exchange, item);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(item);

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 20);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(0, result.InputAmount);
        Assert.AreEqual(300f, GetStoredOil(em), 0.001f);
        Assert.AreEqual(ResourceExchangeQueueState.Cancelled, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCancelled, events[0].ResultKind);
        Assert.AreEqual(ResourceExchangeResourceKind.Oil, events[0].ResourceKind);
        Assert.AreEqual(0, events[0].Amount);
    }

    [Test]
    public void MissionEnd_CancelsActiveJobsAndAppliesRefundPolicy()
    {
        using World world = new(nameof(MissionEnd_CancelsActiveJobsAndAppliesRefundPolicy));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, oil: 600);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        ResourceExchangeQueueComponent firstItem = CreateQueueItem(queueItemId: 1, reservedInputAmount: 200);
        ResourceExchangeQueueComponent secondItem = CreateQueueItem(
            queueItemId: 2,
            reservedInputAmount: 100,
            presentationStarted: 1);
        ReservePhysicalResources(em, exchange, firstItem);
        ReservePhysicalResources(em, exchange, secondItem);
        queue.Add(firstItem);
        queue.Add(secondItem);

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueMissionEndRequest(em, exchange, 1, 50);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeReason.MissionEnding, result.Reason);
        Assert.AreEqual(200, result.InputAmount);
        Assert.AreEqual(2, result.OutputAmount);
        Assert.AreEqual(500f, GetStoredOil(em), 0.001f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Cancelled, queue[0].State);
        Assert.AreEqual(ResourceExchangeReason.MissionEnding, queue[0].StateReason);
        Assert.AreEqual(ResourceExchangeQueueState.Cancelled, queue[1].State);
        Assert.AreEqual(ResourceExchangeReason.MissionEnding, queue[1].StateReason);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(2, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCancelled, events[0].ResultKind);
        Assert.AreEqual(200, events[0].Amount);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCancelled, events[1].ResultKind);
        Assert.AreEqual(0, events[1].Amount);
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
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

        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent { FactionId = 1 };
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
        em.AddBuffer<ResourceExchangePhysicalReservationComponent>(entity);
        Entity storage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = entity.Index + 1,
            OwnerFactionId = 1,
            StoredOilBarrels = oil,
            StoredFuelBarrels = fuel,
            OilStorageCapacity = oilCapacity,
            FuelStorageCapacity = fuelCapacity
        });
        return entity;
    }

    private static void ReservePhysicalResources(
        EntityManager em,
        Entity exchange,
        in ResourceExchangeQueueComponent item)
    {
        EntityQuery storageQuery = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        bool reserved = ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            em,
            storageQuery,
            em.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchange),
            item.QueueItemId,
            item.FactionId,
            item.InputResource,
            item.InputAmount,
            item.OutputResource,
            item.OutputAmount,
            out ResourceExchangeReason reason);
        storageQuery.Dispose();
        Assert.IsTrue(reserved, $"Physical reservation failed: {reason}");
    }

    private static float GetStoredOil(EntityManager em)
    {
        return em.GetComponentData<BuildingResourceStorageComponent>(GetStorageEntity(em)).StoredOilBarrels;
    }

    private static float GetStoredFuel(EntityManager em)
    {
        return em.GetComponentData<BuildingResourceStorageComponent>(GetStorageEntity(em)).StoredFuelBarrels;
    }

    private static void SetStoredFuel(EntityManager em, float amount)
    {
        Entity storageEntity = GetStorageEntity(em);
        BuildingResourceStorageComponent storage =
            em.GetComponentData<BuildingResourceStorageComponent>(storageEntity);
        storage.StoredFuelBarrels = amount;
        em.SetComponentData(storageEntity, storage);
    }

    private static Entity GetStorageEntity(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        Entity storage = query.GetSingletonEntity();
        query.Dispose();
        return storage;
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
        ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Oil,
        int reservedInputAmount = 200,
        int outputAmount = 93,
        float remainingSeconds = 1f,
        byte presentationStarted = 0)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.test"),
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmount = reservedInputAmount,
            ReservedInputAmount = reservedInputAmount,
            OutputAmount = outputAmount,
            State = ResourceExchangeQueueState.InProgress,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = 1f,
            RemainingSeconds = remainingSeconds,
            PresentationStarted = presentationStarted,
            Version = 1
        };
    }

    private static void Tick(EntityManager em, Entity exchange, float deltaSeconds)
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

    private static void UpdateValidationSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeQueueTickSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeQueueTickSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeQueueTickValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeQueueTickValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
