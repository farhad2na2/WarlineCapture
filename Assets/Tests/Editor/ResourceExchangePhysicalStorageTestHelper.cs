using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class ResourceExchangePhysicalStorageTestHelper
{
    public static Entity AddStorage(
        EntityManager entityManager,
        Entity exchange,
        byte factionId = 1,
        float oil = 0f,
        float fuel = 0f,
        int oilCapacity = 1000,
        int fuelCapacity = 1000)
    {
        if (!entityManager.HasBuffer<ResourceExchangePhysicalReservationComponent>(exchange))
            entityManager.AddBuffer<ResourceExchangePhysicalReservationComponent>(exchange);

        Entity storage = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
        entityManager.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = exchange.Index + 1,
            OwnerFactionId = factionId,
            StoredOilBarrels = oil,
            StoredFuelBarrels = fuel,
            OilStorageCapacity = oilCapacity,
            FuelStorageCapacity = fuelCapacity
        });
        return storage;
    }

    public static bool TryReserve(
        EntityManager entityManager,
        Entity exchange,
        in ResourceExchangeQueueComponent item,
        out ResourceExchangeReason reason)
    {
        EntityQuery storageQuery = entityManager.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        bool reserved = ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
            entityManager,
            storageQuery,
            entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchange),
            item.QueueItemId,
            item.FactionId,
            item.InputResource,
            item.InputAmount,
            item.OutputResource,
            item.OutputAmount,
            out reason);
        storageQuery.Dispose();
        return reserved;
    }

    public static BuildingResourceStorageComponent GetStorage(
        EntityManager entityManager,
        byte factionId = 1)
    {
        return entityManager.GetComponentData<BuildingResourceStorageComponent>(
            GetStorageEntity(entityManager, factionId));
    }

    public static void SetStorage(
        EntityManager entityManager,
        in BuildingResourceStorageComponent storage,
        byte factionId = 1)
    {
        entityManager.SetComponentData(GetStorageEntity(entityManager, factionId), storage);
    }

    public static void TickQueue(EntityManager entityManager, Entity exchange, float deltaSeconds)
    {
        ResourceExchangeEnabledComponent enabled =
            entityManager.GetComponentData<ResourceExchangeEnabledComponent>(exchange);
        FactionEconomy economy = entityManager.GetComponentData<FactionEconomy>(exchange);
        FactionTacticalMaterialsComponent materials =
            entityManager.GetComponentData<FactionTacticalMaterialsComponent>(exchange);
        ResourceExchangeWalletComponent wallet =
            entityManager.GetComponentData<ResourceExchangeWalletComponent>(exchange);
        ResourceExchangeSummaryComponent summary =
            entityManager.GetComponentData<ResourceExchangeSummaryComponent>(exchange);
        ResourceExchangeQueueTickSystem.TickQueue(
            enabled,
            ref economy,
            ref materials,
            ref wallet,
            ref summary,
            entityManager.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            entityManager.GetBuffer<ResourceExchangeResultComponent>(exchange),
            entityManager.GetBuffer<ResourceExchangeEconomyEventComponent>(exchange),
            default,
            false,
            default,
            false,
            default,
            false,
            deltaSeconds,
            entityManager,
            entityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchange),
            usePhysicalStorage: true);
        entityManager.SetComponentData(exchange, economy);
        entityManager.SetComponentData(exchange, materials);
        entityManager.SetComponentData(exchange, wallet);
        entityManager.SetComponentData(exchange, summary);
    }

    private static Entity GetStorageEntity(EntityManager entityManager, byte factionId)
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
        using Unity.Collections.NativeArray<Entity> entities =
            query.ToEntityArray(Unity.Collections.Allocator.Temp);
        Entity result = Entity.Null;
        for (int i = 0; i < entities.Length; i++)
        {
            BuildingResourceStorageComponent storage =
                entityManager.GetComponentData<BuildingResourceStorageComponent>(entities[i]);
            if (storage.OwnerFactionId != factionId)
                continue;

            result = entities[i];
            break;
        }

        query.Dispose();
        return result;
    }
}
