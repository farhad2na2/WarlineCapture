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
            #if UNITY_EDITOR
            S002PhysicalStartupObservation.StoragePoint(entityManager, building, "raw-ecs-before-sync", true, storage);
#endif
            SyncResourceStorageMetadata(entityManager, building, ref storage);
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StoragePoint(entityManager, building, "synchronized-local", true, storage);
#endif
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
#if UNITY_EDITOR
                S002PhysicalStartupObservation.StoragePoint(entityManager, building, "before-ecs-commit", true, storage);
#endif
                entityManager.SetComponentData(building.CombatEntity, storage);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.StoragePoint(entityManager, building, "after-ecs-commit", true, storage);
#endif
            }

#if UNITY_EDITOR
            S002PhysicalStartupObservation.StoragePoint(entityManager, building, "before-runtime-mirror", true, storage);
#endif
            ApplyResourceStorage(building, storage);
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StoragePoint(entityManager, building, "after-runtime-mirror", true, storage);
#endif
        }

        private static void SyncResourceStorageMetadata(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            ref BuildingResourceStorageComponent storage)
        {
            storage.RuntimeBuildingId = building.Id;
            storage.OwnerFactionId = building.OwnerFactionId;
            storage.OilStorageCapacity = Mathf.Max(0, building.OilStorageCapacity);
            storage.FuelStorageCapacity = Mathf.Max(0, building.FuelStorageCapacity);
            storage.OilBarrelsPerDay = Mathf.Max(0f, building.OilBarrelsPerDay);
            storage.FuelBarrelsPerDay = Mathf.Max(0f, building.FuelBarrelsPerDay);
            if (entityManager.HasComponent<BuildingResourceProductionProfileComponent>(building.CombatEntity))
            {
                var profile = entityManager.GetComponentData<BuildingResourceProductionProfileComponent>(building.CombatEntity);
                storage.OilBarrelsPerDay = profile.OilPerPeriod;
                storage.FuelBarrelsPerDay = profile.FuelPerPeriod;
            }
        }

        internal static void ApplyResourceStorage(
            FactionResourceCompositionSystemHelper.IResourceBuilding building,
            in BuildingResourceStorageComponent storage)
        {
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StorageMirror(building, "before-oil-mirror-write", storage);
#endif
            building.StoredOilBarrels = storage.StoredOilBarrels;
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StorageMirror(building, "after-oil-mirror-write", storage);
#endif
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StorageMirror(building, "before-fuel-mirror-write", storage);
#endif
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
#if UNITY_EDITOR
            S002PhysicalStartupObservation.StorageMirror(building, "after-fuel-mirror-write", storage);
#endif
        }
    }
}
