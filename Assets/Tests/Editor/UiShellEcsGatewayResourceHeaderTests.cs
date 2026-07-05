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
                OilBarrelsPerDay = 50f,
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
}
