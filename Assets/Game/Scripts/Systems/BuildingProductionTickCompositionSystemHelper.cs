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
#if UNITY_EDITOR
            bool continuousSyncActive = S002PhysicalStartupObservation.ContinuousSync != null && S002PhysicalStartupObservation.ContinuousRead(40);
            var continuousBeforeStorage = continuousSyncActive ? storage : default;
            var continuousProfile = default(BuildingResourceProductionProfileComponent);
            bool continuousHasProfile = false;
#endif
            storage.RuntimeBuildingId = building.Id;
            storage.OwnerFactionId = building.OwnerFactionId;
            storage.OilStorageCapacity = Math.Max(0, building.OilStorageCapacity);
            storage.FuelStorageCapacity = Math.Max(0, building.FuelStorageCapacity);
            storage.OilBarrelsPerDay = Math.Max(0f, building.OilBarrelsPerDay);
            storage.FuelBarrelsPerDay = Math.Max(0f, building.FuelBarrelsPerDay);
            if (entityManager.HasComponent<BuildingResourceProductionProfileComponent>(building.CombatEntity))
            {
                var profile = entityManager.GetComponentData<BuildingResourceProductionProfileComponent>(building.CombatEntity);
#if UNITY_EDITOR
                if (continuousSyncActive) { continuousProfile = profile; continuousHasProfile = true; }
#endif
                storage.OilBarrelsPerDay = profile.OilPerPeriod;
                storage.FuelBarrelsPerDay = profile.FuelPerPeriod;
            }
#if UNITY_EDITOR
            if (continuousSyncActive) S002PhysicalStartupObservation.ContinuousSend(S002PhysicalStartupObservation.ContinuousSync, new S002PhysicalStartupObservation.ContinuousSyncFacts { Manager = entityManager, Building = building, Before = continuousBeforeStorage, Prepared = storage, Profile = continuousProfile, HasProfile = continuousHasProfile });
#endif
            entityManager.SetComponentData(building.CombatEntity, storage);

            building.StoredOilBarrels = Math.Max(0f, storage.StoredOilBarrels);
            building.StoredFuelBarrels = Math.Max(0f, storage.StoredFuelBarrels);
#if UNITY_EDITOR
            if (continuousSyncActive) S002PhysicalStartupObservation.ContinuousSend(S002PhysicalStartupObservation.ContinuousSync, new S002PhysicalStartupObservation.ContinuousSyncFacts { Manager = entityManager, Building = building, Before = continuousBeforeStorage, Prepared = storage, Profile = continuousProfile, HasProfile = continuousHasProfile, After = true });
#endif
        }
    }
}
