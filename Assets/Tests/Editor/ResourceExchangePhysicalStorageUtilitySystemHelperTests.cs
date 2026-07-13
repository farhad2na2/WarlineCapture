using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

public sealed class ResourceExchangePhysicalStorageUtilitySystemHelperTests
{
    private World _world;
    private EntityManager _entityManager;
    private EntityQuery _storageQuery;
    private Entity _exchangeEntity;

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(ResourceExchangePhysicalStorageUtilitySystemHelperTests));
        _entityManager = _world.EntityManager;
        _storageQuery = _entityManager.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        _exchangeEntity = _entityManager.CreateEntity();
        _entityManager.AddBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);
    }

    [TearDown]
    public void TearDown()
    {
        _storageQuery.Dispose();
        _world.Dispose();
    }

    [Test]
    public void ReserveAndComplete_UsesStableBuildingOrderAndMutatesPhysicalStorage()
    {
        Entity laterSource = CreateStorage(runtimeBuildingId: 20, storedOil: 30f);
        Entity firstSource = CreateStorage(runtimeBuildingId: 10, storedOil: 25f);
        Entity destination = CreateStorage(
            runtimeBuildingId: 30,
            storedFuel: 10f,
            fuelCapacity: 100);
        DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
            _entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);

        bool reserved = ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            _entityManager,
            _storageQuery,
            reservations,
            queueItemId: 7,
            factionId: 1,
            ResourceExchangeResourceKind.Oil,
            inputAmount: 40,
            ResourceExchangeResourceKind.Fuel,
            outputAmount: 50,
            out ResourceExchangeReason reserveReason);

        Assert.IsTrue(reserved);
        Assert.AreEqual(ResourceExchangeReason.None, reserveReason);
        Assert.AreEqual(3, reservations.Length);
        Assert.AreEqual(firstSource, reservations[0].StorageEntity);
        Assert.AreEqual(25f, reservations[0].Amount, 0.001f);
        Assert.AreEqual(laterSource, reservations[1].StorageEntity);
        Assert.AreEqual(15f, reservations[1].Amount, 0.001f);
        Assert.AreEqual(destination, reservations[2].StorageEntity);

        ResourceExchangeQueueComponent item = QueueItem(
            queueItemId: 7,
            inputResource: ResourceExchangeResourceKind.Oil,
            inputAmount: 40,
            outputResource: ResourceExchangeResourceKind.Fuel,
            outputAmount: 50);
        bool completed = ResourceExchangePhysicalStorageUtilitySystemHelper.TryCompleteQueueItem(
            _entityManager,
            reservations,
            item,
            out ResourceExchangeReason completionReason);

        Assert.IsTrue(completed);
        Assert.AreEqual(ResourceExchangeReason.None, completionReason);
        Assert.AreEqual(0, reservations.Length);
        AssertStorage(firstSource, storedOil: 0f, storedFuel: 0f);
        AssertStorage(laterSource, storedOil: 15f, storedFuel: 0f);
        AssertStorage(destination, storedOil: 0f, storedFuel: 60f);
    }

    [Test]
    public void Reserve_MissingOutputStorageRollsBackInputReservation()
    {
        Entity source = CreateStorage(runtimeBuildingId: 10, storedOil: 50f);
        DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
            _entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);

        bool reserved = ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            _entityManager,
            _storageQuery,
            reservations,
            queueItemId: 9,
            factionId: 1,
            ResourceExchangeResourceKind.Oil,
            inputAmount: 40,
            ResourceExchangeResourceKind.Fuel,
            outputAmount: 50,
            out ResourceExchangeReason reason);

        Assert.IsFalse(reserved);
        Assert.AreEqual(ResourceExchangeReason.StorageMissing, reason);
        Assert.AreEqual(0, reservations.Length);
        BuildingResourceStorageComponent storage =
            _entityManager.GetComponentData<BuildingResourceStorageComponent>(source);
        Assert.AreEqual(50f, storage.StoredOilBarrels, 0.001f);
        Assert.AreEqual(0f, storage.ReservedOilOutboundBarrels, 0.001f);
    }

    [TestCase(true, 50f)]
    [TestCase(false, 20f)]
    public void Cancel_ReleasesOutputAndAppliesInputRefundPolicy(bool refundInput, float expectedOil)
    {
        Entity source = CreateStorage(runtimeBuildingId: 10, storedOil: 50f);
        Entity destination = CreateStorage(runtimeBuildingId: 20, fuelCapacity: 100);
        DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
            _entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);
        Assert.IsTrue(ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            _entityManager,
            _storageQuery,
            reservations,
            queueItemId: 11,
            factionId: 1,
            ResourceExchangeResourceKind.Oil,
            inputAmount: 30,
            ResourceExchangeResourceKind.Fuel,
            outputAmount: 40,
            out _));

        ResourceExchangePhysicalStorageUtilitySystemHelper.CancelQueueItem(
            _entityManager,
            reservations,
            queueItemId: 11,
            refundInput);

        Assert.AreEqual(0, reservations.Length);
        BuildingResourceStorageComponent sourceStorage =
            _entityManager.GetComponentData<BuildingResourceStorageComponent>(source);
        BuildingResourceStorageComponent destinationStorage =
            _entityManager.GetComponentData<BuildingResourceStorageComponent>(destination);
        Assert.AreEqual(expectedOil, sourceStorage.StoredOilBarrels, 0.001f);
        Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels, 0.001f);
        Assert.AreEqual(0f, destinationStorage.StoredFuelBarrels, 0.001f);
        Assert.AreEqual(0f, destinationStorage.ReservedFuelInboundBarrels, 0.001f);
    }

    [Test]
    public void ExchangeSystems_WithReservationBufferIgnoreWalletOilAndFuel()
    {
        Entity source = CreateStorage(runtimeBuildingId: 10, storedOil: 60f);
        Entity destination = CreateStorage(runtimeBuildingId: 20, storedFuel: 5f, fuelCapacity: 100);
        _entityManager.AddBuffer<ResourceExchangeRecipeComponent>(_exchangeEntity);
        _entityManager.AddBuffer<ResourceExchangeRequestComponent>(_exchangeEntity);
        _entityManager.AddBuffer<ResourceExchangeQueueComponent>(_exchangeEntity);
        _entityManager.AddBuffer<ResourceExchangeResultComponent>(_exchangeEntity);
        _entityManager.AddBuffer<ResourceExchangeEconomyEventComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
            _entityManager.GetBuffer<ResourceExchangeRecipeComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            _entityManager.GetBuffer<ResourceExchangeRequestComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            _entityManager.GetBuffer<ResourceExchangeQueueComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangeResultComponent> results =
            _entityManager.GetBuffer<ResourceExchangeResultComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
            _entityManager.GetBuffer<ResourceExchangeEconomyEventComponent>(_exchangeEntity);
        DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
            _entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);
        recipes.Add(new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("OilToFuel"),
            Enabled = 1,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Fuel,
            InputAmountMin = 40,
            InputAmountMax = 40,
            InputStep = 1,
            OutputPerInput = 1.25f,
            DurationSecondsBase = 1f,
            RequiresStorage = 1
        });
        requests.Add(new ResourceExchangeRequestComponent
        {
            RequestId = 1,
            RequestKind = ResourceExchangeRequestKind.Start,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("OilToFuel"),
            InputAmount = 40
        });
        ResourceExchangeRequestQueueComponent requestQueue = default;
        ResourceExchangeEnabledComponent enabled = new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            MaxQueueItems = 2
        };
        FactionEconomy economy = new FactionEconomy { FactionId = 1, Money = 1000 };
        FactionTacticalMaterialsComponent materials = new FactionTacticalMaterialsComponent
        {
            FactionId = 1,
            Capacity = 100
        };
        ResourceExchangeWalletComponent wallet = new ResourceExchangeWalletComponent
        {
            Oil = 999,
            Fuel = 888,
            OilCapacity = 1000,
            FuelCapacity = 1000
        };
        ResourceExchangeSummaryComponent summary = default;

        ResourceExchangeRequestValidationSystem.ProcessRequests(
            ref requestQueue,
            enabled,
            ref economy,
            ref materials,
            ref wallet,
            ref summary,
            recipes,
            requests,
            queue,
            results,
            economyEvents,
            elapsedSeconds: 0f,
            _entityManager,
            _storageQuery,
            reservations,
            usePhysicalStorage: true);

        Assert.AreEqual(1, queue.Length);
        Assert.AreEqual(999, wallet.Oil);
        Assert.AreEqual(888, wallet.Fuel);
        Assert.AreEqual(40f, _entityManager
            .GetComponentData<BuildingResourceStorageComponent>(source)
            .ReservedOilOutboundBarrels, 0.001f);
        Assert.AreEqual(50f, _entityManager
            .GetComponentData<BuildingResourceStorageComponent>(destination)
            .ReservedFuelInboundBarrels, 0.001f);

        ResourceExchangeQueueTickSystem.TickQueue(
            enabled,
            ref economy,
            ref materials,
            ref wallet,
            ref summary,
            queue,
            results,
            economyEvents,
            default,
            false,
            default,
            false,
            default,
            false,
            deltaSeconds: 1f,
            _entityManager,
            reservations,
            usePhysicalStorage: true);

        Assert.AreEqual(ResourceExchangeQueueState.Completed, queue[0].State);
        Assert.AreEqual(999, wallet.Oil);
        Assert.AreEqual(888, wallet.Fuel);
        Assert.AreEqual(20f, _entityManager
            .GetComponentData<BuildingResourceStorageComponent>(source)
            .StoredOilBarrels, 0.001f);
        Assert.AreEqual(55f, _entityManager
            .GetComponentData<BuildingResourceStorageComponent>(destination)
            .StoredFuelBarrels, 0.001f);
        Assert.AreEqual(0, reservations.Length);
    }

    [Test]
    public void ValidateCompletion_SteadyStateDoesNotAllocateManagedMemory()
    {
        CreateStorage(runtimeBuildingId: 10, storedOil: 60f);
        CreateStorage(runtimeBuildingId: 20, fuelCapacity: 100);
        DynamicBuffer<ResourceExchangePhysicalReservationComponent> reservations =
            _entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(_exchangeEntity);
        Assert.IsTrue(ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            _entityManager,
            _storageQuery,
            reservations,
            queueItemId: 13,
            factionId: 1,
            ResourceExchangeResourceKind.Oil,
            inputAmount: 40,
            ResourceExchangeResourceKind.Fuel,
            outputAmount: 50,
            out _));
        ResourceExchangeQueueComponent item = QueueItem(
            queueItemId: 13,
            inputResource: ResourceExchangeResourceKind.Oil,
            inputAmount: 40,
            outputResource: ResourceExchangeResourceKind.Fuel,
            outputAmount: 50);

        ResourceExchangeReason observed = ResourceExchangeReason.None;
        for (int i = 0; i < 64; i++)
        {
            observed = ResourceExchangePhysicalStorageUtilitySystemHelper.ValidateCompletion(
                _entityManager,
                reservations,
                item);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
        {
            observed = ResourceExchangePhysicalStorageUtilitySystemHelper.ValidateCompletion(
                _entityManager,
                reservations,
                item);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(ResourceExchangeReason.None, observed);
        Assert.AreEqual(0L, allocated);
    }

    private Entity CreateStorage(
        int runtimeBuildingId,
        float storedOil = 0f,
        float storedFuel = 0f,
        int oilCapacity = 0,
        int fuelCapacity = 0)
    {
        Entity entity = _entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
        _entityManager.SetComponentData(entity, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = runtimeBuildingId,
            OwnerFactionId = 1,
            OilStorageCapacity = oilCapacity,
            FuelStorageCapacity = fuelCapacity,
            StoredOilBarrels = storedOil,
            StoredFuelBarrels = storedFuel
        });
        return entity;
    }

    private void AssertStorage(Entity entity, float storedOil, float storedFuel)
    {
        BuildingResourceStorageComponent storage =
            _entityManager.GetComponentData<BuildingResourceStorageComponent>(entity);
        Assert.AreEqual(storedOil, storage.StoredOilBarrels, 0.001f);
        Assert.AreEqual(storedFuel, storage.StoredFuelBarrels, 0.001f);
        Assert.AreEqual(0f, storage.ReservedOilInboundBarrels, 0.001f);
        Assert.AreEqual(0f, storage.ReservedOilOutboundBarrels, 0.001f);
        Assert.AreEqual(0f, storage.ReservedFuelInboundBarrels, 0.001f);
        Assert.AreEqual(0f, storage.ReservedFuelOutboundBarrels, 0.001f);
    }

    private static ResourceExchangeQueueComponent QueueItem(
        int queueItemId,
        ResourceExchangeResourceKind inputResource,
        int inputAmount,
        ResourceExchangeResourceKind outputResource,
        int outputAmount)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            InputResource = inputResource,
            InputAmount = inputAmount,
            OutputResource = outputResource,
            OutputAmount = outputAmount
        };
    }
}
