using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class FactionResourceCompositionSystemHelper
    {
        private readonly BuildingResourceStorageQueryCache _storageQueryCache = new();

        public readonly struct ResourceEconomySnapshot
        {
            public readonly float StoredOilBarrels;
            public readonly float StoredFuelBarrels;
            public readonly float OilBarrelsPerDay;
            public readonly float FuelBarrelsPerDay;
            public readonly int ResourceBuildingCount;

            public ResourceEconomySnapshot(
                float storedOilBarrels,
                float storedFuelBarrels,
                float oilBarrelsPerDay,
                float fuelBarrelsPerDay,
                int resourceBuildingCount)
            {
                StoredOilBarrels = storedOilBarrels;
                StoredFuelBarrels = storedFuelBarrels;
                OilBarrelsPerDay = oilBarrelsPerDay;
                FuelBarrelsPerDay = fuelBarrelsPerDay;
                ResourceBuildingCount = resourceBuildingCount;
            }
        }

        public readonly struct FactionResourceEconomySnapshot
        {
            public readonly float StoredOilBarrels;
            public readonly float StoredFuelBarrels;
            public readonly float OilBarrelsPerDay;
            public readonly float FuelBarrelsPerDay;
            public readonly int ResourceBuildingCount;

            public FactionResourceEconomySnapshot(
                float storedOilBarrels,
                float storedFuelBarrels,
                float oilBarrelsPerDay,
                float fuelBarrelsPerDay,
                int resourceBuildingCount)
            {
                StoredOilBarrels = storedOilBarrels;
                StoredFuelBarrels = storedFuelBarrels;
                OilBarrelsPerDay = oilBarrelsPerDay;
                FuelBarrelsPerDay = fuelBarrelsPerDay;
                ResourceBuildingCount = resourceBuildingCount;
            }
        }

        public readonly struct UsableStorageSnapshot
        {
            public readonly float StoredOilBarrels;
            public readonly float StoredFuelBarrels;
            public readonly float CurrentFuelBarrels;
            public readonly float FuelProducedBarrels;
            public readonly float FuelDeliveredBarrels;
            public readonly float FuelSpentBarrels;
            public readonly int OilStorageCapacity;
            public readonly int FuelStorageCapacity;
            public readonly uint Version;
            public readonly int StorageBuildingCount;

            public UsableStorageSnapshot(
                float storedOilBarrels,
                float storedFuelBarrels,
                int oilStorageCapacity,
                int fuelStorageCapacity,
                float fuelProducedBarrels,
                float fuelSpentBarrels,
                uint version,
                int storageBuildingCount)
            {
                StoredOilBarrels = storedOilBarrels;
                StoredFuelBarrels = storedFuelBarrels;
                CurrentFuelBarrels = storedFuelBarrels;
                FuelProducedBarrels = fuelProducedBarrels;
                FuelDeliveredBarrels = storedFuelBarrels;
                FuelSpentBarrels = fuelSpentBarrels;
                OilStorageCapacity = oilStorageCapacity;
                FuelStorageCapacity = fuelStorageCapacity;
                Version = version;
                StorageBuildingCount = storageBuildingCount;
            }
        }

        public readonly struct ResourceProductionTickResult
        {
            public readonly float OilExtractedBarrels;
            public readonly float FuelProducedBarrels;

            public ResourceProductionTickResult(float oilExtractedBarrels, float fuelProducedBarrels)
            {
                OilExtractedBarrels = oilExtractedBarrels;
                FuelProducedBarrels = fuelProducedBarrels;
            }
        }

        public interface IResourceBuilding
        {
            bool IsDestroyed { get; }
            bool HasOwnerFaction { get; }
            byte OwnerFactionId { get; }
            int OilStorageCapacity { get; }
            int FuelStorageCapacity { get; }
            float OilBarrelsPerDay { get; }
            float FuelBarrelsPerDay { get; }
            float StoredOilBarrels { get; set; }
            float StoredFuelBarrels { get; set; }
        }

        public int GetDisplayedOilCapacity(IResourceBuilding building, float oilBarrelsPerFuelBarrel)
        {
            if (building == null)
                return 0;

            int explicitOilCapacity = Mathf.Max(0, building.OilStorageCapacity);
            if (explicitOilCapacity > 0)
                return explicitOilCapacity;

            if (building.FuelBarrelsPerDay > 0f)
            {
                int derivedFromFuel = Mathf.CeilToInt(Mathf.Max(1f, building.FuelStorageCapacity) * Mathf.Max(0f, oilBarrelsPerFuelBarrel));
                return Mathf.Max(1, derivedFromFuel);
            }

            return 0;
        }

        public bool TryGetPrimaryCapacityInfo(IResourceBuilding building, float oilBarrelsPerFuelBarrel, out int current, out int max, out float progress01)
        {
            current = 0;
            max = 0;
            progress01 = 0f;
            if (building == null || building.IsDestroyed)
                return false;

            max = GetDisplayedOilCapacity(building, oilBarrelsPerFuelBarrel);
            if (max > 0)
            {
                current = Mathf.Clamp(Mathf.CeilToInt(building.StoredOilBarrels), 0, max);
                progress01 = Mathf.Clamp01(building.StoredOilBarrels / max);
                return true;
            }

            return TryGetFuelCapacityInfo(building, out current, out max, out progress01);
        }

        public bool TryGetFuelCapacityInfo(IResourceBuilding building, out int current, out int max, out float progress01)
        {
            current = 0;
            max = 0;
            progress01 = 0f;
            if (building == null || building.IsDestroyed)
                return false;

            max = Mathf.Max(0, building.FuelStorageCapacity);
            if (max <= 0)
                return false;

            current = Mathf.Clamp(Mathf.FloorToInt(building.StoredFuelBarrels), 0, max);
            progress01 = Mathf.Clamp01(building.StoredFuelBarrels / max);
            return true;
        }

        public void GetResourceTotals<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, out int oilBarrels, out int fuelBarrels)
            where TBuilding : class, IResourceBuilding
        {
            oilBarrels = 0;
            fuelBarrels = 0;
            if (buildings == null)
                return;

            if (buildings is Dictionary<int, TBuilding> buildingMap)
            {
                foreach (KeyValuePair<int, TBuilding> entry in buildingMap)
                    AddResourceTotals(entry.Value, ref oilBarrels, ref fuelBarrels);
            }
            else
            {
                foreach (KeyValuePair<int, TBuilding> entry in buildings)
                    AddResourceTotals(entry.Value, ref oilBarrels, ref fuelBarrels);
            }
        }

        internal void GetResourceTotals(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
            out int oilBarrels,
            out int fuelBarrels)
        {
            oilBarrels = 0;
            fuelBarrels = 0;
            if (buildings == null)
                return;

            if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildingMap)
                    AddResourceTotals(entityManager, entry.Value, ref oilBarrels, ref fuelBarrels);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildings)
                    AddResourceTotals(entityManager, entry.Value, ref oilBarrels, ref fuelBarrels);
            }
        }

        public bool TryGetFactionResourceEconomy<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, byte factionId, out ResourceEconomySnapshot snapshot)
            where TBuilding : class, IResourceBuilding
        {
            float oil = 0f;
            float fuel = 0f;
            float oilRate = 0f;
            float fuelRate = 0f;
            int resourceBuildingCount = 0;

            if (buildings != null)
            {
                if (buildings is Dictionary<int, TBuilding> buildingMap)
                {
                    foreach (KeyValuePair<int, TBuilding> entry in buildingMap)
                        AddFactionResourceEconomy(entry.Value, factionId, ref oil, ref fuel, ref oilRate, ref fuelRate, ref resourceBuildingCount);
                }
                else
                {
                    foreach (KeyValuePair<int, TBuilding> entry in buildings)
                        AddFactionResourceEconomy(entry.Value, factionId, ref oil, ref fuel, ref oilRate, ref fuelRate, ref resourceBuildingCount);
                }
            }

            snapshot = new ResourceEconomySnapshot(oil, fuel, oilRate, fuelRate, resourceBuildingCount);
            return resourceBuildingCount > 0;
        }

        internal bool TryGetFactionResourceEconomy(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
            byte factionId,
            out ResourceEconomySnapshot snapshot)
        {
            float oil = 0f;
            float fuel = 0f;
            float oilRate = 0f;
            float fuelRate = 0f;
            int resourceBuildingCount = 0;

            if (buildings != null)
            {
                if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
                {
                    foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildingMap)
                        AddFactionResourceEconomy(entityManager, entry.Value, factionId, ref oil, ref fuel, ref oilRate, ref fuelRate, ref resourceBuildingCount);
                }
                else
                {
                    foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildings)
                        AddFactionResourceEconomy(entityManager, entry.Value, factionId, ref oil, ref fuel, ref oilRate, ref fuelRate, ref resourceBuildingCount);
                }
            }

            snapshot = new ResourceEconomySnapshot(oil, fuel, oilRate, fuelRate, resourceBuildingCount);
            return resourceBuildingCount > 0;
        }

        internal bool TryGetFactionUsableStorageSummary(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
            byte factionId,
            out UsableStorageSnapshot snapshot)
        {
            float oil = 0f;
            float fuel = 0f;
            int oilCapacity = 0;
            int fuelCapacity = 0;
            uint version = 0;
            int storageBuildingCount = 0;

            if (buildings != null)
            {
                if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
                {
                    foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildingMap)
                    {
                        AddFactionUsableStorageSummary(
                            entityManager,
                            entry.Value,
                            factionId,
                            ref oil,
                            ref fuel,
                            ref oilCapacity,
                            ref fuelCapacity,
                            ref version,
                            ref storageBuildingCount);
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildings)
                    {
                        AddFactionUsableStorageSummary(
                            entityManager,
                            entry.Value,
                            factionId,
                            ref oil,
                            ref fuel,
                            ref oilCapacity,
                            ref fuelCapacity,
                            ref version,
                            ref storageBuildingCount);
                    }
                }
            }

            snapshot = new UsableStorageSnapshot(
                oil,
                fuel,
                oilCapacity,
                fuelCapacity,
                fuelProducedBarrels: 0f,
                fuelSpentBarrels: 0f,
                version: version,
                storageBuildingCount: storageBuildingCount);
            return storageBuildingCount > 0;
        }

        public float DrainFactionResource<TBuilding>(
            IReadOnlyDictionary<int, TBuilding> buildings,
            byte factionId,
            float requestedBarrels,
            ResourceKind resourceKind)
            where TBuilding : class, IResourceBuilding
        {
            if (buildings == null || requestedBarrels <= 0f)
                return 0f;

            float remaining = requestedBarrels;
            if (buildings is Dictionary<int, TBuilding> buildingMap)
            {
                foreach (KeyValuePair<int, TBuilding> entry in buildingMap)
                {
                    DrainFactionResource(entry.Value, factionId, resourceKind, ref remaining);
                    if (remaining <= 0.001f)
                        break;
                }
            }
            else
            {
                foreach (KeyValuePair<int, TBuilding> entry in buildings)
                {
                    DrainFactionResource(entry.Value, factionId, resourceKind, ref remaining);
                    if (remaining <= 0.001f)
                        break;
                }
            }

            return requestedBarrels - remaining;
        }

        internal float DrainFactionResource(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
            byte factionId,
            float requestedBarrels,
            ResourceKind resourceKind)
        {
            if (buildings == null || requestedBarrels <= 0f)
                return 0f;

            float remaining = requestedBarrels;
            if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildingMap)
                {
                    DrainFactionResource(entityManager, entry.Value, factionId, resourceKind, ref remaining);
                    if (remaining <= 0.001f)
                        break;
                }
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in buildings)
                {
                    DrainFactionResource(entityManager, entry.Value, factionId, resourceKind, ref remaining);
                    if (remaining <= 0.001f)
                        break;
                }
            }

            return requestedBarrels - remaining;
        }

        public ResourceProductionTickResult UpdateResourceProduction<TBuilding>(
            IReadOnlyDictionary<int, TBuilding> buildings,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel)
            where TBuilding : class, IResourceBuilding
        {
            if (buildings == null || buildings.Count == 0)
                return new ResourceProductionTickResult(0f, 0f);

            secondsPerDay = Mathf.Max(1f, secondsPerDay);
            deltaTime = Mathf.Max(0f, deltaTime);
            oilBarrelsPerFuelBarrel = Mathf.Max(0.001f, oilBarrelsPerFuelBarrel);

            float oilExtracted = 0f;
            float fuelProduced = 0f;

            if (buildings is Dictionary<int, TBuilding> buildingMap)
            {
                foreach (KeyValuePair<int, TBuilding> pair in buildingMap)
                    UpdateResourceProductionForBuilding(
                        pair.Value,
                        secondsPerDay,
                        deltaTime,
                        oilBarrelsPerFuelBarrel,
                        ref oilExtracted,
                        ref fuelProduced);
            }
            else
            {
                foreach (KeyValuePair<int, TBuilding> pair in buildings)
                    UpdateResourceProductionForBuilding(
                        pair.Value,
                        secondsPerDay,
                        deltaTime,
                        oilBarrelsPerFuelBarrel,
                        ref oilExtracted,
                        ref fuelProduced);
            }

            return new ResourceProductionTickResult(oilExtracted, fuelProduced);
        }

        internal ResourceProductionTickResult UpdateResourceProduction(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel)
        {
            secondsPerDay = Mathf.Max(1f, secondsPerDay);
            deltaTime = Mathf.Max(0f, deltaTime);
            oilBarrelsPerFuelBarrel = Mathf.Max(0.001f, oilBarrelsPerFuelBarrel);

            EntityQuery storageQuery = _storageQueryCache.Get(entityManager);
            if (!storageQuery.IsEmptyIgnoreFilter)
            {
                BuildingResourceProductionEcsSystem.TickResult queryResult =
                    BuildingResourceProductionEcsSystem.ApplyStorageQuery(
                        entityManager,
                        storageQuery,
                        secondsPerDay,
                        deltaTime,
                        oilBarrelsPerFuelBarrel);
                SyncRuntimeBuildingsFromEcsStorage(entityManager, buildings);
                return new ResourceProductionTickResult(
                    queryResult.OilExtractedBarrels,
                    queryResult.FuelProducedBarrels);
            }

            if (buildings == null || buildings.Count == 0)
                return new ResourceProductionTickResult(0f, 0f);

            float oilExtracted = 0f;
            float fuelProduced = 0f;

            if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in buildingMap)
                    UpdateRuntimeBuildingResourceProduction(
                        entityManager,
                        pair.Value,
                        secondsPerDay,
                        deltaTime,
                        oilBarrelsPerFuelBarrel,
                        ref oilExtracted,
                        ref fuelProduced);
            }
            else
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in buildings)
                    UpdateRuntimeBuildingResourceProduction(
                        entityManager,
                        pair.Value,
                        secondsPerDay,
                        deltaTime,
                        oilBarrelsPerFuelBarrel,
                        ref oilExtracted,
                        ref fuelProduced);
            }

            return new ResourceProductionTickResult(oilExtracted, fuelProduced);
        }

        private void AddResourceTotals<TBuilding>(TBuilding building, ref int oilBarrels, ref int fuelBarrels)
            where TBuilding : class, IResourceBuilding
        {
            if (!IsResourceStorageBuilding(building))
                return;

            if (building.OilStorageCapacity > 0)
                oilBarrels += Mathf.Max(0, Mathf.FloorToInt(building.StoredOilBarrels));
            if (building.FuelStorageCapacity > 0)
                fuelBarrels += Mathf.Max(0, Mathf.FloorToInt(building.StoredFuelBarrels));
        }

        private void AddResourceTotals(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            ref int oilBarrels,
            ref int fuelBarrels)
        {
            if (!IsResourceStorageBuilding(building))
                return;

            if (!TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                AddResourceTotals<RuntimeBuildingEntity>(building, ref oilBarrels, ref fuelBarrels);
                return;
            }

            if (building.OilStorageCapacity > 0)
                oilBarrels += Mathf.Max(0, Mathf.FloorToInt(storage.StoredOilBarrels));
            if (building.FuelStorageCapacity > 0)
                fuelBarrels += Mathf.Max(0, Mathf.FloorToInt(storage.StoredFuelBarrels));
        }

        private void AddFactionResourceEconomy<TBuilding>(
            TBuilding building,
            byte factionId,
            ref float oil,
            ref float fuel,
            ref float oilRate,
            ref float fuelRate,
            ref int resourceBuildingCount)
            where TBuilding : class, IResourceBuilding
        {
            if (!IsFactionResourceBuilding(building, factionId))
                return;

            resourceBuildingCount++;
            oil += Mathf.Max(0f, building.StoredOilBarrels);
            fuel += Mathf.Max(0f, building.StoredFuelBarrels);
            oilRate += Mathf.Max(0f, building.OilBarrelsPerDay);
            fuelRate += Mathf.Max(0f, building.FuelBarrelsPerDay);
        }

        private void AddFactionResourceEconomy(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            byte factionId,
            ref float oil,
            ref float fuel,
            ref float oilRate,
            ref float fuelRate,
            ref int resourceBuildingCount)
        {
            if (!IsFactionResourceBuilding(building, factionId))
                return;

            resourceBuildingCount++;
            if (TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                oil += Mathf.Max(0f, storage.StoredOilBarrels);
                fuel += Mathf.Max(0f, storage.StoredFuelBarrels);
            }
            else
            {
                oil += Mathf.Max(0f, building.StoredOilBarrels);
                fuel += Mathf.Max(0f, building.StoredFuelBarrels);
            }

            oilRate += Mathf.Max(0f, building.OilBarrelsPerDay);
            fuelRate += Mathf.Max(0f, building.FuelBarrelsPerDay);
        }

        private void AddFactionUsableStorageSummary(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            byte factionId,
            ref float oil,
            ref float fuel,
            ref int oilCapacity,
            ref int fuelCapacity,
            ref uint version,
            ref int storageBuildingCount)
        {
            if (!IsFactionUsableStorageBuilding(building, factionId))
                return;

            storageBuildingCount++;
            if (TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                oil += Mathf.Max(0f, storage.StoredOilBarrels);
                fuel += Mathf.Max(0f, storage.StoredFuelBarrels);
                oilCapacity += Mathf.Max(0, storage.OilStorageCapacity);
                fuelCapacity += Mathf.Max(0, storage.FuelStorageCapacity);
                version = CombineVersion(version, storage.Version);
                return;
            }

            oil += Mathf.Max(0f, building.StoredOilBarrels);
            fuel += Mathf.Max(0f, building.StoredFuelBarrels);
            oilCapacity += Mathf.Max(0, building.OilStorageCapacity);
            fuelCapacity += Mathf.Max(0, building.FuelStorageCapacity);
            version = CombineVersion(version, (uint)Mathf.Max(0, building.Id));
        }

        private void DrainFactionResource<TBuilding>(
            TBuilding building,
            byte factionId,
            ResourceKind resourceKind,
            ref float remaining)
            where TBuilding : class, IResourceBuilding
        {
            if (!IsFactionResourceBuilding(building, factionId))
                return;

            float stored = resourceKind == ResourceKind.Fuel ? building.StoredFuelBarrels : building.StoredOilBarrels;
            float drained = Mathf.Min(Mathf.Max(0f, stored), remaining);
            if (drained <= 0f)
                return;

            if (resourceKind == ResourceKind.Fuel)
                building.StoredFuelBarrels = Mathf.Max(0f, building.StoredFuelBarrels - drained);
            else
                building.StoredOilBarrels = Mathf.Max(0f, building.StoredOilBarrels - drained);

            remaining -= drained;
        }

        private void DrainFactionResource(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            byte factionId,
            ResourceKind resourceKind,
            ref float remaining)
        {
            if (!IsFactionResourceBuilding(building, factionId))
                return;

            if (!TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                DrainFactionResource<RuntimeBuildingEntity>(building, factionId, resourceKind, ref remaining);
                return;
            }

            float stored = resourceKind == ResourceKind.Fuel ? storage.StoredFuelBarrels : storage.StoredOilBarrels;
            float drained = Mathf.Min(Mathf.Max(0f, stored), remaining);
            if (drained <= 0f)
                return;

            if (resourceKind == ResourceKind.Fuel)
                storage.StoredFuelBarrels = Mathf.Max(0f, storage.StoredFuelBarrels - drained);
            else
                storage.StoredOilBarrels = Mathf.Max(0f, storage.StoredOilBarrels - drained);

            CommitEntityResourceStorage(entityManager, building, storage);
            remaining -= drained;
        }

        private static void UpdateResourceProductionForBuilding<TBuilding>(
            TBuilding building,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel,
            ref float oilExtracted,
            ref float fuelProduced)
            where TBuilding : class, IResourceBuilding
        {
            if (building == null || building.IsDestroyed)
                return;

            var storage = new BuildingResourceStorageComponent
            {
                OilStorageCapacity = building.OilStorageCapacity,
                FuelStorageCapacity = building.FuelStorageCapacity,
                OilBarrelsPerDay = building.OilBarrelsPerDay,
                FuelBarrelsPerDay = building.FuelBarrelsPerDay,
                StoredOilBarrels = building.StoredOilBarrels,
                StoredFuelBarrels = building.StoredFuelBarrels
            };

            BuildingResourceProductionEcsSystem.TickResult result = BuildingResourceProductionEcsSystem.ApplyTick(
                ref storage,
                secondsPerDay,
                deltaTime,
                oilBarrelsPerFuelBarrel);

            building.StoredOilBarrels = storage.StoredOilBarrels;
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
            oilExtracted += result.OilExtractedBarrels;
            fuelProduced += result.FuelProducedBarrels;
        }

        private static void UpdateRuntimeBuildingResourceProduction(
            EntityManager entityManager,
            RuntimeBuildingEntity building,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel,
            ref float oilExtracted,
            ref float fuelProduced)
        {
            if (building == null || building.IsDestroyed)
                return;

            if (!TryGetEntityResourceStorage(entityManager, building, out BuildingResourceStorageComponent storage))
            {
                UpdateResourceProductionForBuilding(
                    building,
                    secondsPerDay,
                    deltaTime,
                    oilBarrelsPerFuelBarrel,
                    ref oilExtracted,
                    ref fuelProduced);
                return;
            }

            BuildingResourceProductionEcsSystem.TickResult result = BuildingResourceProductionEcsSystem.ApplyTick(
                ref storage,
                secondsPerDay,
                deltaTime,
                oilBarrelsPerFuelBarrel);

            CommitEntityResourceStorage(entityManager, building, storage);
            oilExtracted += result.OilExtractedBarrels;
            fuelProduced += result.FuelProducedBarrels;
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

            building.StoredOilBarrels = storage.StoredOilBarrels;
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
        }

        private static void SyncRuntimeBuildingsFromEcsStorage(
            EntityManager entityManager,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings)
        {
            if (buildings == null || buildings.Count == 0)
                return;

            if (buildings is Dictionary<int, RuntimeBuildingEntity> buildingMap)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in buildingMap)
                    SyncRuntimeBuildingFromEcsStorage(entityManager, pair.Value);
                return;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in buildings)
                SyncRuntimeBuildingFromEcsStorage(entityManager, pair.Value);
        }

        private static void SyncRuntimeBuildingFromEcsStorage(
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
            building.StoredOilBarrels = storage.StoredOilBarrels;
            building.StoredFuelBarrels = storage.StoredFuelBarrels;
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

        public bool IsResourceStorageBuilding(IResourceBuilding building)
        {
            if (building == null || building.IsDestroyed)
                return false;

            bool storesOil = building.OilStorageCapacity > 0;
            bool storesFuel = building.FuelStorageCapacity > 0;
            bool producesOil = building.OilBarrelsPerDay > 0f;
            bool producesFuel = building.FuelBarrelsPerDay > 0f;
            return (storesOil || storesFuel) && !producesOil && !producesFuel;
        }

        public bool IsFactionResourceBuilding(IResourceBuilding building, byte factionId)
        {
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                return false;

            return building.OilStorageCapacity > 0 ||
                   building.FuelStorageCapacity > 0 ||
                   building.OilBarrelsPerDay > 0f ||
                   building.FuelBarrelsPerDay > 0f;
        }

        private bool IsFactionUsableStorageBuilding(IResourceBuilding building, byte factionId)
        {
            if (building == null || !IsFactionResourceBuilding(building, factionId))
                return false;

            return IsResourceStorageBuilding(building);
        }

        private static uint CombineVersion(uint current, uint value)
        {
            unchecked
            {
                return (current * 16777619u) ^ value;
            }
        }
    }
}
