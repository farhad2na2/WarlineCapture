using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementQueryUiSystemHelper
    {
        public delegate int GetProductionCountDelegate(BuildingDefinition definition);
        public delegate GameObject GetProductionPrefabDelegate(BuildingDefinition definition, int index);
        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

        public readonly struct Source
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Func<int?> GetActiveBuildingId;
            public readonly GetProductionCountDelegate GetProductionCount;
            public readonly GetProductionPrefabDelegate GetProductionPrefab;
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;

            public Source(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Func<int?> getActiveBuildingId,
                GetProductionCountDelegate getProductionCount,
                GetProductionPrefabDelegate getProductionPrefab,
                TryGetEntityManagerDelegate tryGetEntityManager)
            {
                RuntimeBuildings = runtimeBuildings;
                GetActiveBuildingId = getActiveBuildingId;
                GetProductionCount = getProductionCount;
                GetProductionPrefab = getProductionPrefab;
                TryGetEntityManager = tryGetEntityManager;
            }
        }

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly int? ActiveBuildingId;
            public readonly GetProductionCountDelegate GetProductionCount;
            public readonly GetProductionPrefabDelegate GetProductionPrefab;
            public readonly bool HasEntityManager;
            public readonly EntityManager EntityManager;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                int? activeBuildingId,
                GetProductionCountDelegate getProductionCount,
                GetProductionPrefabDelegate getProductionPrefab,
                bool hasEntityManager,
                EntityManager entityManager)
            {
                RuntimeBuildings = runtimeBuildings;
                ActiveBuildingId = activeBuildingId;
                GetProductionCount = getProductionCount;
                GetProductionPrefab = getProductionPrefab;
                HasEntityManager = hasEntityManager;
                EntityManager = entityManager;
            }
        }

        public GameObject GetSelectedBuildingProductionPrefab(Context context, int productionIndex)
        {
            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return null;

            return context.GetProductionPrefab?.Invoke(building.Definition, productionIndex);
        }

        public Context CreateContext(Source source)
        {
            EntityManager entityManager = default;
            bool hasEntityManager = source.TryGetEntityManager != null &&
                source.TryGetEntityManager(out entityManager);
            return new Context(
                source.RuntimeBuildings,
                source.GetActiveBuildingId?.Invoke(),
                source.GetProductionCount,
                source.GetProductionPrefab,
                hasEntityManager,
                entityManager);
        }

        public void GetSelectedBuildingProductionPrefabs(Context context, List<GameObject> prefabs)
        {
            prefabs?.Clear();
            if (prefabs == null ||
                context.GetProductionCount == null ||
                context.GetProductionPrefab == null ||
                !TryGetActiveBuilding(context, out RuntimeBuildingEntity building) ||
                building?.Definition == null)
            {
                return;
            }

            int count = context.GetProductionCount(building.Definition);
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = context.GetProductionPrefab(building.Definition, i);
                if (prefab != null)
                    prefabs.Add(prefab);
            }
        }

        public string GetPlacementStatusText(BuildingPlacementInputUiSystemHelper.IPlacementState placement)
        {
            if (placement == null)
                return "Choose a build type.";

            string state = placement.IsValid ? "Valid placement" : "Blocked by road or blocker";
            Vector2Int origin = placement.OriginCell;
            Vector2Int size = placement.Definition.FootprintCells;
            return $"{placement.Definition.DisplayName}: {state} ({origin.x},{origin.y}) {size.x}x{size.y}";
        }

        public float GetActivePlacementDurationSeconds(BuildingPlacementInputUiSystemHelper.IPlacementState placement)
        {
            return placement?.Definition != null
                ? placement.Definition.ProductionDurationSeconds
                : 0f;
        }

        public string GetSelectedBuildingLabel(Context context)
        {
            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return "Building";

            return $"{building.Definition.DisplayName} ({building.OriginCell.x},{building.OriginCell.y})";
        }

        public string GetSelectedBuildingDisplayName(Context context)
        {
            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return "Building";

            return string.IsNullOrWhiteSpace(building.Definition.DisplayName)
                ? "Building"
                : building.Definition.DisplayName;
        }

        public string GetSelectedBuildingDescription(Context context)
        {
            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return "Select a building to see its options.";

            string description = string.IsNullOrWhiteSpace(building.Definition.Description)
                ? "Operational building."
                : building.Definition.Description;
            return $"{description} Footprint: {building.Definition.FootprintCells.x}x{building.Definition.FootprintCells.y}.";
        }

        public bool TryGetSelectedBuildingPreviewPrefab(Context context, out GameObject prefab)
        {
            prefab = null;
            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return false;

            prefab = building.Definition.Prefab;
            return prefab != null;
        }

        public bool TryGetSelectedBuildingHealth(Context context, out int current, out int max)
        {
            current = 0;
            max = 0;

            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return false;

            max = Mathf.Max(1, building.Definition.MaxHealth);
            current = max;

            if (building.CombatEntity == Entity.Null || !context.HasEntityManager)
                return true;

            EntityManager entityManager = context.EntityManager;
            if (!entityManager.Exists(building.CombatEntity) || !entityManager.HasComponent<UnitHealth>(building.CombatEntity))
                return true;

            UnitHealth health = entityManager.GetComponentData<UnitHealth>(building.CombatEntity);
            current = health.Current;
            max = Mathf.Max(1, health.Max);
            return true;
        }

        public bool TryGetSelectedBuildingResourceStorage(
            Context context,
            out int oilCurrent,
            out int oilCapacity,
            out int fuelCurrent,
            out int fuelCapacity)
        {
            oilCurrent = 0;
            oilCapacity = 0;
            fuelCurrent = 0;
            fuelCapacity = 0;

            if (!TryGetActiveBuilding(context, out RuntimeBuildingEntity building) || building?.Definition == null)
                return false;

            oilCapacity = Mathf.Max(0, building.OilStorageCapacity);
            fuelCapacity = Mathf.Max(0, building.FuelStorageCapacity);
            oilCurrent = Mathf.RoundToInt(Mathf.Max(0f, building.StoredOilBarrels));
            fuelCurrent = Mathf.RoundToInt(Mathf.Max(0f, building.StoredFuelBarrels));

            if (oilCapacity > 0)
                oilCurrent = Mathf.Min(oilCurrent, oilCapacity);
            if (fuelCapacity > 0)
                fuelCurrent = Mathf.Min(fuelCurrent, fuelCapacity);

            return oilCapacity > 0 || fuelCapacity > 0 || oilCurrent > 0 || fuelCurrent > 0;
        }

        private static bool TryGetActiveBuilding(Context context, out RuntimeBuildingEntity building)
        {
            building = null;
            return context.ActiveBuildingId.HasValue &&
                   context.RuntimeBuildings != null &&
                   context.RuntimeBuildings.TryGetValue(context.ActiveBuildingId.Value, out building);
        }
    }
}
