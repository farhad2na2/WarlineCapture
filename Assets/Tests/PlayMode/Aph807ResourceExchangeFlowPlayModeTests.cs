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
            ResourceExchangeWalletComponent reservedWallet =
                em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
            Assert.AreEqual(300, reservedWallet.Oil, "The accepted export must reserve oil immediately.");
            Assert.AreEqual(0, reservedWallet.Credits);
            Assert.AreEqual(1, em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Length);
            Assert.AreEqual(
                ResourceExchangeQueueState.InProgress,
                em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);

            SystemHandle queueTick = world.CreateSystem<ResourceExchangeQueueTickSystem>();
            world.SetTime(new TimeData(2.1d, 2f));
            queueTick.Update(world.Unmanaged);

            ResourceExchangeWalletComponent settledWallet =
                em.GetComponentData<ResourceExchangeWalletComponent>(exchange);
            Assert.AreEqual(300, settledWallet.Oil);
            Assert.AreEqual(100, settledWallet.Credits);
            Assert.AreEqual(
                ResourceExchangeQueueState.Completed,
                em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].State);

            world.SetTime(new TimeData(4.1d, 2f));
            queueTick.Update(world.Unmanaged);
            Assert.AreEqual(
                100,
                em.GetComponentData<ResourceExchangeWalletComponent>(exchange).Credits,
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
            FactionId = FactionIdentity.PlayerFactionId,
            Oil = 500,
            OilCapacity = 1000,
            MaterialsCapacity = 1000,
            FuelCapacity = 1000
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
        return exchange;
    }
}
