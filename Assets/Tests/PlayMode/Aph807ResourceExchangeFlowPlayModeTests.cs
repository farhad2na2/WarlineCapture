using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

public sealed class Aph807ResourceExchangeFlowPlayModeTests
{
    [Test]
    public void ProductionRequestGateway_ConfirmsExportAndSettlesCreditsExactlyOnce()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World(nameof(ProductionRequestGateway_ConfirmsExportAndSettlesCreditsExactlyOnce));
        World.DefaultGameObjectInjectionWorld = world;

        try
        {
            EntityManager em = world.EntityManager;
            Entity exchange = CreateExchangeScenario(em);

            int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
                em,
                exchange,
                new FixedString128Bytes("exchange.export_oil_credits.standard"),
                inputAmount: 200,
                factionId: FactionIdentity.PlayerFactionId,
                frameCount: 10);
            world.SetTime(new TimeData(0.1d, 0.1f));
            SystemHandle validation = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
            validation.Update(world.Unmanaged);

            Assert.IsTrue(
                ResourceExchangeRequestValidationSystem.TryGetResult(em, exchange, requestId, out ResourceExchangeResultComponent result));
            Assert.AreEqual(1, result.Accepted);
            BuildingResourceStorageComponent reservedStorage = GetStorage(em);
            Assert.AreEqual(500f, reservedStorage.StoredOilBarrels, 0.001f);
            Assert.AreEqual(
                200f,
                reservedStorage.ReservedOilOutboundBarrels,
                0.001f,
                "The accepted export must reserve physical oil immediately.");
            Assert.AreEqual(0, em.GetComponentData<FactionEconomy>(exchange).Money);
            Assert.AreEqual(1, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
            Assert.AreEqual(
                ResourceExchangeQueueState.InProgress,
                em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);

            SystemHandle queueTick = world.CreateSystem<ResourceExchangeQueueTickSystem>();
            world.SetTime(new TimeData(2.1d, 2f));
            queueTick.Update(world.Unmanaged);

            BuildingResourceStorageComponent settledStorage = GetStorage(em);
            Assert.AreEqual(300f, settledStorage.StoredOilBarrels, 0.001f);
            Assert.AreEqual(0f, settledStorage.ReservedOilOutboundBarrels, 0.001f);
            Assert.AreEqual(100, em.GetComponentData<FactionEconomy>(exchange).Money);
            Assert.AreEqual(
                ResourceExchangeQueueState.Completed,
                em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);

            world.SetTime(new TimeData(4.1d, 2f));
            queueTick.Update(world.Unmanaged);
            Assert.AreEqual(
                100,
                em.GetComponentData<FactionEconomy>(exchange).Money,
                "A completed exchange must not apply its output twice.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static Entity CreateExchangeScenario(EntityManager em)
    {
        Entity exchange = em.CreateEntity(
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent));
        em.SetComponentData(exchange, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = FactionIdentity.PlayerFactionId,
            AllowRush = 1,
            MaxQueueItems = 4,
            ScenarioTag = new FixedString64Bytes("mission.active")
        });
        em.SetComponentData(exchange, new ResourceExchangeWalletComponent
        {
            FactionId = FactionIdentity.PlayerFactionId
        });
        em.SetComponentData(exchange, new FactionEconomy { FactionId = FactionIdentity.PlayerFactionId });
        em.SetComponentData(exchange, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Capacity = 1000
        });
        em.SetComponentData(exchange, new ResourceExchangeSummaryComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Enabled = 1,
            MaxQueueItems = 4
        });
        em.AddBuffer<ResourceExchangeRecipeComponent>(exchange).Add(new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.export_oil_credits.standard"),
            DisplayName = new FixedString128Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmountMin = 100,
            InputAmountMax = 500,
            InputStep = 100,
            OutputPerInput = 0.5f,
            DurationSecondsBase = 1f,
            Enabled = 1
        });
        em.AddBuffer<ResourceExchangeRequestComponent>(exchange);
        em.AddBuffer<ResourceExchangeQueueComponent>(exchange);
        em.AddBuffer<ResourceExchangeResultComponent>(exchange);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        em.AddBuffer<ResourceExchangePhysicalReservationComponent>(exchange);
        Entity storage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 1,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = 500f,
            OilStorageCapacity = 1000,
            FuelStorageCapacity = 1000
        });
        return exchange;
    }

    private static BuildingResourceStorageComponent GetStorage(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        BuildingResourceStorageComponent storage =
            em.GetComponentData<BuildingResourceStorageComponent>(query.GetSingletonEntity());
        query.Dispose();
        return storage;
    }
}
