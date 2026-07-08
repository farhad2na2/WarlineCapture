using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class ResourceExchangeRequestValidationSystemTests
{
    [Test]
    public void StartRequest_Accepted_ReservesInputAndCreatesQueueItem()
    {
        using World world = new(nameof(StartRequest_Accepted_ReservesInputAndCreatesQueueItem));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 500,
            FuelCapacity = 1000,
            MaterialsCapacity = 1000
        });
        AddRecipe(em, exchange, ExportOilRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
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
        Assert.AreEqual(300, wallet.Oil);
        Assert.AreEqual(1u, wallet.Version);

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
    public void StartRequest_Disabled_DoesNotSpendOrQueue()
    {
        using World world = new(nameof(StartRequest_Disabled_DoesNotSpendOrQueue));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(
            em,
            enabled: false,
            wallet: new ResourceExchangeWalletComponent
            {
                FactionId = 1,
                Oil = 500
            });
        AddRecipe(em, exchange, ExportOilRecipe());

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, requestId, ResourceExchangeReason.ExchangeUnavailable);
        Assert.AreEqual(500, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsMissingRecipeLockedMissionAndInvalidAmount()
    {
        using World world = new(nameof(StartRequest_RejectsMissingRecipeLockedMissionAndInvalidAmount));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 500
        });
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
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);
        int stepRequest = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            250,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, missingRecipeRequest, ResourceExchangeReason.RecipeLocked);
        AssertRejected(em, exchange, lockedRequest, ResourceExchangeReason.RecipeLocked);
        AssertRejected(em, exchange, stepRequest, ResourceExchangeReason.RecipeLocked);
        Assert.AreEqual(500, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsInsufficientInput()
    {
        using World world = new(nameof(StartRequest_RejectsInsufficientInput));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 100
        });
        AddRecipe(em, exchange, ExportOilRecipe());

        int insufficient = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            exchange,
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            200,
            1,
            0);
        UpdateSystem(world);

        AssertRejected(em, exchange, insufficient, ResourceExchangeReason.InsufficientOil);
        Assert.AreEqual(100, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsStorageFull()
    {
        using World world = new(nameof(StartRequest_RejectsStorageFull));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Credits = 1000,
            Fuel = 990,
            FuelCapacity = 1000
        });
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
        Assert.AreEqual(1000, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Credits);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
    }

    [Test]
    public void StartRequest_RejectsQueueFull()
    {
        using World world = new(nameof(StartRequest_RejectsQueueFull));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, wallet: new ResourceExchangeWalletComponent
        {
            FactionId = 1,
            Oil = 500
        });
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
            new FixedString128Bytes("exchange.export_oil_credits.standard"),
            100,
            1,
            0);

        UpdateSystem(world);

        AssertRejected(em, exchange, queueFull, ResourceExchangeReason.QueueFull);
        Assert.AreEqual(500, em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Oil);
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
        bool enabled = true,
        ResourceExchangeWalletComponent wallet = default)
    {
        Entity entity = em.CreateEntity(
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent));
        em.SetComponentData(entity, new ResourceExchangeEnabledComponent
        {
            Enabled = enabled ? (byte)1 : (byte)0,
            FactionId = 1,
            AllowRush = 1,
            AllowWorldPresentation = 1,
            MaxQueueItems = 1,
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

    private static ResourceExchangeRecipeComponent ExportOilRecipe(string missionTag = "")
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
            InputResource = ResourceExchangeResourceKind.Credits,
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
}
#endif
