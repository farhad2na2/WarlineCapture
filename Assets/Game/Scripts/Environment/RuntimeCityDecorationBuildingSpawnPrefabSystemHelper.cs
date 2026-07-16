using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityDecorationBuildingSpawnPrefabSystemHelper
    {
        private readonly RuntimeCityDecorationBuildingSpawnState _state = new();

        public RuntimeCityDecorationBuildingSpawnState State => _state;

        public void ConfigureMinimumFreeScatterCount(int count)
        {
            _state.ConfigureMinimumFreeScatterCount(count);
        }

        public void PlaceCityDecorationBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            RuntimeCityDecorationPrefabGroupState decorationPrefabGroupSystem,
            RuntimeCityClothCoverSpawnState clothCoverSpawnSystem,
            RuntimeCityArchwaySpawnState archwaySpawnSystem,
            RuntimeCityFreeScatterDecorationState freeScatterDecorationSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> shopAndHouseFootprints)
        {
            _state.PlaceCityDecorationBuildings(
                context,
                placementSystem,
                decorationPrefabGroupSystem,
                clothCoverSpawnSystem,
                archwaySpawnSystem,
                freeScatterDecorationSystem,
                prefabs,
                count,
                centerRoadCell,
                townRadius,
                roadCellSizeInGridCells,
                roadCells,
                ref rng,
                usedPlotCells,
                reservedFootprints,
                shopAndHouseFootprints);
        }
    }

    internal sealed class RuntimeCityDecorationBuildingSpawnState
    {
        private int _minimumFreeScatterCount;

        public void ConfigureMinimumFreeScatterCount(int count)
        {
            _minimumFreeScatterCount = Mathf.Max(0, count);
        }

        internal int CalculateArchwayBudget(int totalCount, int clothPlaced)
        {
            int remainingCount = Mathf.Max(0, totalCount - clothPlaced);
            int reservedFreeScatterCount = Mathf.Min(_minimumFreeScatterCount, remainingCount);
            return remainingCount - reservedFreeScatterCount;
        }

        public void PlaceCityDecorationBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            RuntimeCityDecorationPrefabGroupState decorationPrefabGroupSystem,
            RuntimeCityClothCoverSpawnState clothCoverSpawnSystem,
            RuntimeCityArchwaySpawnState archwaySpawnSystem,
            RuntimeCityFreeScatterDecorationState freeScatterDecorationSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> shopAndHouseFootprints)
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0)
                return;

            RuntimeCityDecorationGroupPrefabSystemHelper.Groups decorationGroups = decorationPrefabGroupSystem.CreateGroups(prefabs);
            int clothPlaced = clothCoverSpawnSystem.PlaceClothCoverBuildings(
                context,
                placementSystem,
                decorationGroups.ClothCoverPrefabs,
                count,
                ref rng,
                reservedFootprints,
                shopAndHouseFootprints);
            int archwaysPlaced = archwaySpawnSystem.PlaceCentralArchwayBuildings(
                context,
                placementSystem,
                decorationGroups.ArchwayPrefabs,
                CalculateArchwayBudget(count, clothPlaced),
                centerRoadCell,
                roadCellSizeInGridCells,
                roadCells,
                ref rng,
                usedPlotCells,
                reservedFootprints);
            int remainingCount = count - clothPlaced - archwaysPlaced;
            if (remainingCount <= 0)
                return;

            List<GameObject> randomPrefabs = decorationGroups.FreeScatterPrefabs.Count > 0 ? decorationGroups.FreeScatterPrefabs : prefabs;
            freeScatterDecorationSystem.PlaceFreeScatterDecorations(
                context,
                placementSystem,
                randomPrefabs,
                remainingCount,
                centerRoadCell,
                townRadius,
                roadCellSizeInGridCells,
                roadCells,
                ref rng,
                usedPlotCells,
                reservedFootprints);
        }
    }
}
