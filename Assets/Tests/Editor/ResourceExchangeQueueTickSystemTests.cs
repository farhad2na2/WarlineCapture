using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class ResourceExchangeQueueTickSystemTests
{
    [Test]
    public void TickQueue_CompletesOnceAndGrantsOutput()
    {
        using World world = new(nameof(TickQueue_CompletesOnceAndGrantsOutput));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1
        });
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(outputResource: ResourceExchangeResourceKind.Credits, outputAmount: 93, remainingSeconds: 0.25f));

        Tick(em, exchange, 0.5f);

        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        Assert.AreEqual(93, wallet.Credits);
        Assert.AreEqual(1u, wallet.Version);
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
        Assert.AreEqual(93, wallet.Credits);
        Assert.AreEqual(1, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);
    }

    [Test]
    public void TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable()
    {
        using World world = new(nameof(TickQueue_BlocksWhenOutputStorageFullAndResumesWhenAvailable));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Fuel = 980,
            FuelCapacity = 1000
        });
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(outputResource: ResourceExchangeResourceKind.Fuel, outputAmount: 50, remainingSeconds: 0.1f));

        Tick(em, exchange, 0.1f);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Blocked, queue[0].State);
        Assert.AreEqual(ResourceExchangeReason.StorageFull, queue[0].StateReason);
        Assert.AreEqual(980, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Fuel);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> events =
            em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(1, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueBlocked, events[0].ResultKind);
        Assert.AreEqual(ResourceExchangeResourceKind.Fuel, events[0].ResourceKind);
        Assert.AreEqual(0, events[0].Amount);

        ResourceExchangeWalletComponent wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        wallet.Fuel = 940;
        em.SetComponentData(exchange, wallet);

        Tick(em, exchange, 0.1f);

        wallet = em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(990, wallet.Fuel);
        events = em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        Assert.AreEqual(2, events.Length);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCompleted, events[1].ResultKind);
        Assert.AreEqual(50, events[1].Amount);
    }

    [Test]
    public void CancelRequest_RefundsReservedInputBeforePresentation()
    {
        using World world = new(nameof(CancelRequest_RefundsReservedInputBeforePresentation));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 300
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(reservedInputAmount: 200));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 20);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeResultKind.QueueCancelled, result.ResultKind);
        Assert.AreEqual(200, result.InputAmount);
        Assert.AreEqual(500, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
        Assert.AreEqual(ResourceExchangeQueueState.Cancelled, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);
        Assert.AreEqual(1, em.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange).Length);
    }

    [Test]
    public void CancelRequest_DoesNotRefundAfterPresentationStarted()
    {
        using World world = new(nameof(CancelRequest_DoesNotRefundAfterPresentationStarted));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 300
        });
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(reservedInputAmount: 200, presentationStarted: 1));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueCancelRequest(em, exchange, 1, 1, 20);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(0, result.InputAmount);
        Assert.AreEqual(300, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
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
        Entity exchange = CreateExchangeEntity(em, new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 300
        });
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(queueItemId: 1, reservedInputAmount: 200));
        queue.Add(CreateQueueItem(queueItemId: 2, reservedInputAmount: 100, presentationStarted: 1));

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueMissionEndRequest(em, exchange, 1, 50);

        UpdateValidationSystem(world);

        Assert.IsTrue(ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(ResourceExchangeReason.MissionEnding, result.Reason);
        Assert.AreEqual(200, result.InputAmount);
        Assert.AreEqual(2, result.OutputAmount);
        Assert.AreEqual(500, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);

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
        return entity;
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
        ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Credits,
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
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(handle).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));
    }
}
#endif
