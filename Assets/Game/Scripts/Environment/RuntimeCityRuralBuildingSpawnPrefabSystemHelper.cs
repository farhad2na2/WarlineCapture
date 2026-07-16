using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityRuralBuildingSpawnPrefabSystemHelper
    {
        private readonly RuntimeCityRuralBuildingSpawnState _state = new();

        public RuntimeCityRuralBuildingSpawnState State => _state;

        public void ConfigureMaximumAxisDistanceInset(int inset)
        {
            _state.ConfigureMaximumAxisDistanceInset(inset);
        }

        public void PlaceRuralBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int maximumDistanceFromCenter,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            Vector2Int scatterBiasDirection,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            _state.PlaceRuralBuildings(
                context,
                placementSystem,
                prefabs,
                count,
                centerRoadCell,
                maximumDistanceFromCenter,
                roadCellSizeInGridCells,
                roadCells,
                scatterBiasDirection,
                ref rng,
                usedPlotCells,
                reservedFootprints,
                placementAnchors,
                secondaryPlacementAnchors);
        }
    }

    internal sealed class RuntimeCityRuralBuildingSpawnState
    {
        private int _maximumAxisDistanceInset;

        public void ConfigureMaximumAxisDistanceInset(int inset)
        {
            _maximumAxisDistanceInset = Mathf.Max(0, inset);
        }

        public void PlaceRuralBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int maximumDistanceFromCenter,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            Vector2Int scatterBiasDirection,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0)
                return;

            maximumDistanceFromCenter = Mathf.Max(
                context.Config.HallPlazaRadiusRoadCells + 5,
                maximumDistanceFromCenter);
            int attempts = 0;
            int placed = 0;
            int maxAttempts = Mathf.Max(120, count * 20);
            while (placed < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int plotCell = new(
                    centerRoadCell.x + rng.NextInt(-maximumDistanceFromCenter, maximumDistanceFromCenter + 1),
                    centerRoadCell.y + rng.NextInt(-maximumDistanceFromCenter, maximumDistanceFromCenter + 1));
                if (scatterBiasDirection != Vector2Int.zero)
                {
                    plotCell = RuntimeCityBuildingPlotState.ApplyDirectionalBias(
                        plotCell,
                        centerRoadCell,
                        scatterBiasDirection);
                }

                if (!IsWithinMaximumDistance(
                        plotCell,
                        centerRoadCell,
                        maximumDistanceFromCenter,
                        out int distance) || distance < context.Config.HallPlazaRadiusRoadCells + 5)
                    continue;
                if (roadCells.Contains(plotCell))
                    continue;
                if (!context.BuildingPlotSystem.HasPlotSpacing(plotCell, usedPlotCells, 1))
                    continue;

                GameObject prefab = context.PrefabSelectionSystem.GetRandomPrefab(prefabs, ref rng);
                if (prefab == null)
                    continue;

                Vector2Int footprint = placementSystem.GetFootprint(context, prefab);
                Vector2Int preferredOrigin = context.BuildingPlotSystem.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
                if (!placementSystem.TrySpawnAndReserve(
                        context,
                        new RuntimeCityBuildingPlacementPrefabSystemHelper.Request(
                            prefab,
                            preferredOrigin,
                            footprint,
                            "House",
                            "Rural old town house.",
                            context.Config.DefaultBuildingMaxHealth,
                            reservedFootprints,
                            0),
                        out _,
                        placementAnchors,
                        secondaryPlacementAnchors))
                {
                    continue;
                }

                usedPlotCells.Add(plotCell);
                placed++;
            }
        }

        internal bool IsWithinMaximumDistance(
            Vector2Int plotCell,
            Vector2Int centerRoadCell,
            int maximumDistanceFromCenter,
            out int distance)
        {
            int deltaX = Mathf.Abs(plotCell.x - centerRoadCell.x);
            int deltaY = Mathf.Abs(plotCell.y - centerRoadCell.y);
            distance = deltaX + deltaY;
            int maximumAxisDistance = Mathf.Max(
                0,
                maximumDistanceFromCenter - _maximumAxisDistanceInset);
            return distance <= maximumDistanceFromCenter &&
                   deltaX <= maximumAxisDistance &&
                   deltaY <= maximumAxisDistance;
        }
    }
}
