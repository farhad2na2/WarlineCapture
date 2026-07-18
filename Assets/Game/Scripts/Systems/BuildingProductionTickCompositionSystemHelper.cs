using System;
using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class BuildingProductionTickCompositionSystemHelper
    {
        public BuildingProductionRuntimeTickCompositionSystemHelper.Context Create(
            BuildingGameplaySourceCompositionSystemHelper source,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingProductionContextCompositionSystemHelper.Source> createProductionRuntimeContextSource,
            float oilBarrelsPerFuelBarrel)
        {
            BuildingProductionContextCompositionSystemHelper.Source productionSource = createProductionRuntimeContextSource(source);
            return new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingGameplayDependencyCompositionSystemHelper.DayNightSystem,
                source.FactionResourceCompositionSystemHelper,
                source.BuildingProductionUpdateCompositionSystemHelper,
                source.BuildingProductionContextCompositionSystemHelper.CreateProductionUpdateContext(productionSource),
                source.BuildingResourceHaulerBridgeCompositionSystemHelper,
                source.BuildingProductionContextCompositionSystemHelper.CreateResourceHaulerBridgeContext(productionSource),
                source.BuildingSpawnCompositionSystemHelper,
                () => source.BuildingSpawnRandomState,
                value => source.BuildingSpawnRandomState = value,
                null,
                null,
                source.UnitPathfindingPendingStateReader.HasPendingPathJob,
                oilBarrelsPerFuelBarrel,
                building => SyncBuildingResourceStorage(source, building),
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager);
        }

        private static void SyncBuildingResourceStorage(
            BuildingGameplaySourceCompositionSystemHelper source,
            RuntimeBuildingEntity building)
        {
            if (source == null ||
                building == null ||
                building.CombatEntity == Entity.Null ||
                !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager) ||
                !entityManager.Exists(building.CombatEntity))
            {
                return;
            }

            SyncBuildingResourceStorageFromEcs(entityManager, building);
        }

        internal static void SyncBuildingResourceStorageFromEcs(
            EntityManager entityManager,
            RuntimeBuildingEntity building)
        {
            if (building == null ||
                building.CombatEntity == Entity.Null ||
                !entityManager.Exists(building.CombatEntity) ||
                !entityManager.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
            {
                return;
            }

            BuildingResourceStorageComponent storage =
                entityManager.GetComponentData<BuildingResourceStorageComponent>(building.CombatEntity);
            storage.RuntimeBuildingId = building.Id;
            storage.OwnerFactionId = building.OwnerFactionId;
            storage.OilStorageCapacity = Math.Max(0, building.OilStorageCapacity);
            storage.FuelStorageCapacity = Math.Max(0, building.FuelStorageCapacity);
            storage.OilBarrelsPerDay = Math.Max(0f, building.OilBarrelsPerDay);
            storage.FuelBarrelsPerDay = Math.Max(0f, building.FuelBarrelsPerDay);
            entityManager.SetComponentData(building.CombatEntity, storage);

            building.StoredOilBarrels = Math.Max(0f, storage.StoredOilBarrels);
            building.StoredFuelBarrels = Math.Max(0f, storage.StoredFuelBarrels);
        }
    }
}
