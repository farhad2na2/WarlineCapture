using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

public sealed class UiShellEcsGatewayResourceHeaderTests
{
    [Test]
    public void MatchHudHeader_UsesLivePlayerResourceStorage()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_UsesLivePlayerResourceStorage));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            em.CreateEntity(typeof(UiShellRootComponent));
            Entity storageEntity = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(storageEntity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(storageEntity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 7,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                StoredOilBarrels = 12.4f,
                StoredFuelBarrels = 3.6f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("12", header.OilText);
            Assert.AreEqual("4", header.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_IgnoresRefineryOutputUntilFuelIsDeliveredToStorage()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_IgnoresRefineryOutputUntilFuelIsDeliveredToStorage));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            em.CreateEntity(typeof(UiShellRootComponent));
            Entity refinery = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(refinery, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(refinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 21,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                FuelBarrelsPerDay = 40f,
                StoredFuelBarrels = 33f
            });
            Entity fuelStorage = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(fuelStorage, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(fuelStorage, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 22,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                StoredFuelBarrels = 8f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("8", header.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_PrefersVersionedUsableFuelSummary()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_PrefersVersionedUsableFuelSummary));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 15f,
                OilStorageCapacity = 200,
                FuelStorageCapacity = 100,
                Version = 7u
            });
            Entity refinery = em.CreateEntity(
                typeof(BuildingResourceStorageComponent),
                typeof(Faction));
            em.SetComponentData(refinery, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(refinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 41,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                FuelBarrelsPerDay = 40f,
                StoredFuelBarrels = 99f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("2", header.OilText);
            Assert.AreEqual("15", header.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MatchHudHeader_EmptyUsableFuelSummaryDoesNotFallbackToRefineryOutput()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(MatchHudHeader_EmptyUsableFuelSummaryDoesNotFallbackToRefineryOutput));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            EntityManager em = world.EntityManager;
            Entity boundary = em.CreateEntity(typeof(UiShellRootComponent));
            em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            DynamicBuffer<BuildingRuntimeFactionSummary> economySummaries =
                em.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
            economySummaries.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingCount = 1,
                StoredFuelBarrels = 77f,
                FuelBarrelsPerDay = 40f
            });

            Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header));
            Assert.AreEqual("0", header.OilText);
            Assert.AreEqual("0", header.FuelText);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }
}
