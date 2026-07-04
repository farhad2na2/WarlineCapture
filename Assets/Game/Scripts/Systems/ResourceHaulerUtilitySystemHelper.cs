using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed class ResourceHaulerUtilitySystemHelper
    {
        public enum TimedActionState : byte
        {
            Started = 0,
            Waiting = 1,
            Ready = 2
        }

        public enum ResourceHaulPhase : byte
        {
            None = 0,
            ToSource = 1,
            Loading = 2,
            ToDestination = 3,
            Unloading = 4
        }

        public enum ResourceHaulKind : byte
        {
            Oil = 0,
            Fuel = 1
        }

        public bool IsOilSourceBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            return building != null &&
                   building.OilBarrelsPerDay > 0f &&
                   building.OilStorageCapacity > 0;
        }

        public bool IsFuelBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            return building != null &&
                   building.FuelBarrelsPerDay > 0f;
        }

        public bool IsFuelStorageSourceBuilding(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            return building != null &&
                   building.FuelBarrelsPerDay > 0f &&
                   building.FuelStorageCapacity > 0;
        }

        public bool HasAvailableFuelForHauler(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            return IsFuelStorageSourceBuilding(building) &&
                   building.StoredFuelBarrels >= 1f;
        }

        internal bool HasAvailableFuelForHauler(EntityManager entityManager, RuntimeBuildingEntity building)
        {
            if (building == null || !IsFuelStorageSourceBuilding(building))
                return false;

            if (TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
                return storage.StoredFuelBarrels >= 1f;

            return building.StoredFuelBarrels >= 1f;
        }

        public UnitResourceHaulOrder CreateOrder(
            int sourceBuildingId,
            int destinationBuildingId,
            int2 targetCell,
            ResourceHaulKind resourceKind)
        {
            return new UnitResourceHaulOrder
            {
                SourceBuildingId = sourceBuildingId,
                DestinationBuildingId = destinationBuildingId,
                TargetCell = targetCell,
                ActionEndsAt = 0f,
                Phase = (byte)ResourceHaulPhase.ToSource,
                ResourceKind = (byte)resourceKind
            };
        }

        public void SetPhase(ref UnitResourceHaulOrder order, ResourceHaulPhase phase)
        {
            order.Phase = (byte)phase;
            order.ActionEndsAt = 0f;
        }

        public void SetTravelPhase(ref UnitResourceHaulOrder order, ResourceHaulPhase phase, int2 targetCell)
        {
            order.TargetCell = targetCell;
            SetPhase(ref order, phase);
        }

        public void ResetActionTimer(ref UnitResourceHaulOrder order)
        {
            order.ActionEndsAt = 0f;
        }

        public TimedActionState AdvanceTimedAction(ref UnitResourceHaulOrder order, float now, float durationSeconds)
        {
            if (order.ActionEndsAt <= 0f)
            {
                order.ActionEndsAt = now + Mathf.Max(0f, durationSeconds);
                return TimedActionState.Started;
            }

            return now < order.ActionEndsAt
                ? TimedActionState.Waiting
                : TimedActionState.Ready;
        }

        public float GetLoadAmount(UnitResourceHauler hauler)
        {
            return Mathf.Max(0f, hauler.BarrelCapacity);
        }

        public float GetCargo(UnitResourceHauler hauler, ResourceHaulKind resourceKind)
        {
            return resourceKind == ResourceHaulKind.Fuel
                ? Mathf.Max(0f, hauler.CargoFuelBarrels)
                : Mathf.Max(0f, hauler.CargoOilBarrels);
        }

        internal float GetStoredResource(EntityManager entityManager, RuntimeBuildingEntity building, ResourceHaulKind resourceKind)
        {
            if (building == null)
                return 0f;

            if (TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                return resourceKind == ResourceHaulKind.Fuel
                    ? Mathf.Max(0f, storage.StoredFuelBarrels)
                    : Mathf.Max(0f, storage.StoredOilBarrels);
            }

            return resourceKind == ResourceHaulKind.Fuel
                ? Mathf.Max(0f, building.StoredFuelBarrels)
                : Mathf.Max(0f, building.StoredOilBarrels);
        }

        public float GetOilReceivingFreeCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            if (building == null)
                return 0f;

            BuildingResourceStorageComponent storage = CreateResourceStorage(building);
            return BuildingResourceStorageTransferSystemHelper.GetOilReceivingFreeCapacity(storage);
        }

        public float GetFuelReceivingFreeCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding building)
        {
            if (building == null)
                return 0f;

            BuildingResourceStorageComponent storage = CreateResourceStorage(building);
            return BuildingResourceStorageTransferSystemHelper.GetFuelReceivingFreeCapacity(storage);
        }

        public bool HasEnoughSourceResource(FactionResourceCompositionSystemHelper.IResourceBuilding source, ResourceHaulKind resourceKind, float loadAmount)
        {
            if (source == null || loadAmount <= 0f)
                return false;

            BuildingResourceStorageComponent storage = CreateResourceStorage(source);
            return BuildingResourceStorageTransferSystemHelper.HasEnoughSourceResource(
                storage,
                ToStorageResourceKind(resourceKind),
                loadAmount);
        }

        internal bool HasEnoughSourceResource(EntityManager entityManager, RuntimeBuildingEntity source, ResourceHaulKind resourceKind, float loadAmount)
        {
            if (source == null || loadAmount <= 0f)
                return false;

            if (TryGetEntityResourceStorage(entityManager, source, out BuildingResourceStorageComponent storage))
            {
                return BuildingResourceStorageTransferSystemHelper.HasEnoughSourceResource(
                    storage,
                    ToStorageResourceKind(resourceKind),
                    loadAmount);
            }

            return HasEnoughSourceResource((FactionResourceCompositionSystemHelper.IResourceBuilding)source, resourceKind, loadAmount);
        }

        public bool TryCompleteLoad(
            FactionResourceCompositionSystemHelper.IResourceBuilding source,
            ResourceHaulKind resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            if (source == null)
                return false;

            BuildingResourceStorageComponent storage = CreateResourceStorage(source);
            bool loaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteLoad(
                ref storage,
                ToStorageResourceKind(resourceKind),
                loadAmount,
                ref hauler);
            if (!loaded)
                return false;

            ApplyResourceStorage(source, storage);
            return true;
        }

        internal bool TryCompleteLoad(
            EntityManager entityManager,
            RuntimeBuildingEntity source,
            ResourceHaulKind resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            if (source == null)
                return false;

            if (!TryGetEntityResourceStorage(entityManager, source, out BuildingResourceStorageComponent storage))
                return TryCompleteLoad((FactionResourceCompositionSystemHelper.IResourceBuilding)source, resourceKind, loadAmount, ref hauler);

            bool loaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteLoad(
                ref storage,
                ToStorageResourceKind(resourceKind),
                loadAmount,
                ref hauler);
            if (!loaded)
                return false;

            CommitEntityResourceStorage(entityManager, source, storage);
            return true;
        }

        public void RevertLoad(
            FactionResourceCompositionSystemHelper.IResourceBuilding source,
            ResourceHaulKind resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            if (source == null || loadAmount <= 0f)
                return;

            BuildingResourceStorageComponent storage = CreateResourceStorage(source);
            BuildingResourceHaulerTransferEcsSystem.RevertLoad(
                ref storage,
                ToStorageResourceKind(resourceKind),
                loadAmount,
                ref hauler);
            ApplyResourceStorage(source, storage);
        }

        internal void RevertLoad(
            EntityManager entityManager,
            RuntimeBuildingEntity source,
            ResourceHaulKind resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            if (source == null || loadAmount <= 0f)
                return;

            if (!TryGetEntityResourceStorage(entityManager, source, out BuildingResourceStorageComponent storage))
            {
                RevertLoad((FactionResourceCompositionSystemHelper.IResourceBuilding)source, resourceKind, loadAmount, ref hauler);
                return;
            }

            BuildingResourceHaulerTransferEcsSystem.RevertLoad(
                ref storage,
                ToStorageResourceKind(resourceKind),
                loadAmount,
                ref hauler);
            CommitEntityResourceStorage(entityManager, source, storage);
        }

        public bool HasReceivingCapacity(FactionResourceCompositionSystemHelper.IResourceBuilding destination, ResourceHaulKind resourceKind, float cargo)
        {
            if (destination == null || cargo <= 0f)
                return false;

            BuildingResourceStorageComponent storage = CreateResourceStorage(destination);
            return BuildingResourceStorageTransferSystemHelper.HasReceivingCapacity(
                storage,
                ToStorageResourceKind(resourceKind),
                cargo);
        }

        internal bool HasReceivingCapacity(EntityManager entityManager, RuntimeBuildingEntity destination, ResourceHaulKind resourceKind, float cargo)
        {
            if (destination == null || cargo <= 0f)
                return false;

            if (TryGetEntityResourceStorage(entityManager, destination, out BuildingResourceStorageComponent storage))
            {
                return BuildingResourceStorageTransferSystemHelper.HasReceivingCapacity(
                    storage,
                    ToStorageResourceKind(resourceKind),
                    cargo);
            }

            return HasReceivingCapacity((FactionResourceCompositionSystemHelper.IResourceBuilding)destination, resourceKind, cargo);
        }

        public bool TryCompleteUnload(
            FactionResourceCompositionSystemHelper.IResourceBuilding destination,
            ResourceHaulKind resourceKind,
            ref UnitResourceHauler hauler)
        {
            if (destination == null)
                return false;

            BuildingResourceStorageComponent storage = CreateResourceStorage(destination);
            bool unloaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteUnload(
                ref storage,
                ToStorageResourceKind(resourceKind),
                ref hauler);
            if (!unloaded)
                return false;

            ApplyResourceStorage(destination, storage);
            return true;
        }

        internal bool TryCompleteUnload(
            EntityManager entityManager,
            RuntimeBuildingEntity destination,
            ResourceHaulKind resourceKind,
            ref UnitResourceHauler hauler)
        {
            if (destination == null)
                return false;

            if (!TryGetEntityResourceStorage(entityManager, destination, out BuildingResourceStorageComponent storage))
                return TryCompleteUnload((FactionResourceCompositionSystemHelper.IResourceBuilding)destination, resourceKind, ref hauler);

            bool unloaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteUnload(
                ref storage,
                ToStorageResourceKind(resourceKind),
                ref hauler);
            if (!unloaded)
                return false;

            CommitEntityResourceStorage(entityManager, destination, storage);
            return true;
        }

        private static byte ToStorageResourceKind(ResourceHaulKind resourceKind)
        {
            return resourceKind == ResourceHaulKind.Fuel
                ? BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                : BuildingResourceStorageTransferSystemHelper.OilResourceKind;
        }

        private static BuildingResourceStorageComponent CreateResourceStorage(
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

        private static bool TryGetEntityResourceStorage(
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

        private static void CommitEntityResourceStorage(
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

        private static void ApplyResourceStorage(
            FactionResourceCompositionSystemHelper.IResourceBuilding building,
            in BuildingResourceStorageComponent storage)
        {
            building.StoredOilBarrels = storage.StoredOilBarrels;
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
        }
    }
}
