using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class ResourceHaulerStorageAccessSystemHelper
    {
        internal static byte ToStorageResourceKind(ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            return resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel
                ? (byte)ResourceKind.Fuel
                : (byte)ResourceKind.Oil;
        }

        internal static BuildingResourceStorageComponent CreateResourceStorage(
            FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            return new BuildingResourceStorageComponent
            {
                OwnerFactionId = building.OwnerFactionId,
                OilStorageCapacity = Mathf.Max(0, building.OilStorageCapacity),
                FuelStorageCapacity = Mathf.Max(0, building.FuelStorageCapacity),
                OilBarrelsPerDay = Mathf.Max(0f, building.OilBarrelsPerDay),
                FuelBarrelsPerDay = Mathf.Max(0f, building.FuelBarrelsPerDay),
                StoredOilBarrels = Mathf.Max(0f, building.StoredOilBarrels),
                StoredFuelBarrels = Mathf.Max(0f, building.StoredFuelBarrels)
            };
        }

        internal static bool TryGetEntityResourceStorage(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            out BuildingResourceStorageComponent storage)
        {
            storage = default;
            if (building == null ||
                building.CombatEntity == Entity.Null ||
                !entityManager.Exists(building.CombatEntity) ||
                !entityManager.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
            {
                return false;
            }

            storage = entityManager.GetComponentData<BuildingResourceStorageComponent>(building.CombatEntity);
            SyncResourceStorageMetadata(building, ref storage);
            return true;
        }

        internal static void CommitEntityResourceStorage(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            in BuildingResourceStorageComponent storage)
        {
            if (building == null)
                return;

            if (building.CombatEntity != Entity.Null &&
                entityManager.Exists(building.CombatEntity) &&
                entityManager.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
            {
                entityManager.SetComponentData(building.CombatEntity, storage);
            }

            ApplyResourceStorage(building, storage);
        }

        private static void SyncResourceStorageMetadata(
            RuntimeBuildingEntity building,
            ref BuildingResourceStorageComponent storage)
        {
            storage.RuntimeBuildingId = building.Id;
            storage.OwnerFactionId = building.OwnerFactionId;
            storage.OilStorageCapacity = Mathf.Max(0, building.OilStorageCapacity);
            storage.FuelStorageCapacity = Mathf.Max(0, building.FuelStorageCapacity);
            storage.OilBarrelsPerDay = Mathf.Max(0f, building.OilBarrelsPerDay);
            storage.FuelBarrelsPerDay = Mathf.Max(0f, building.FuelBarrelsPerDay);
        }

        internal static void ApplyResourceStorage(
            FactionResourceCompositionSystemHelper.IResourceBuilding building,
            in BuildingResourceStorageComponent storage)
        {
            building.StoredOilBarrels = storage.StoredOilBarrels;
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
        }
    }
}
