using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityFreeScatterDecorationPrefabSystemHelper
    {
        private readonly RuntimeCityFreeScatterDecorationState _state = new();

        public RuntimeCityFreeScatterDecorationState State => _state;

        public void ConfigureDirectionalBias(bool enabled, Vector2Int direction)
        {
            _state.ConfigureDirectionalBias(enabled, direction);
        }

        public void ConfigureMaximumDistanceOffset(int offset)
        {
            _state.ConfigureMaximumDistanceOffset(offset);
        }

        public void ConfigureMaximumAxisDistanceInset(int inset)
        {
            _state.ConfigureMaximumAxisDistanceInset(inset);
        }

        public void PlaceFreeScatterDecorations(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int remainingCount,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints)
        {
            _state.PlaceFreeScatterDecorations(
                context,
                placementSystem,
                prefabs,
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

    internal sealed class RuntimeCityFreeScatterDecorationState
    {
        private const int DefaultMaximumDistanceOffset = 3;

        private bool _directionalBiasEnabled;
        private Vector2Int _directionalBias;
        private int _maximumDistanceOffset = DefaultMaximumDistanceOffset;
        private int _maximumAxisDistanceInset;

        public int MaximumDistanceOffset => _maximumDistanceOffset;

        public void ConfigureDirectionalBias(bool enabled, Vector2Int direction)
        {
            Vector2Int normalizedDirection = new(
                Mathf.Clamp(direction.x, -1, 1),
                Mathf.Clamp(direction.y, -1, 1));
            _directionalBiasEnabled = enabled && normalizedDirection != Vector2Int.zero;
            _directionalBias = _directionalBiasEnabled ? normalizedDirection : Vector2Int.zero;
        }

        public void ConfigureMaximumDistanceOffset(int offset)
        {
            _maximumDistanceOffset = Mathf.Max(0, offset);
        }

        public void ConfigureMaximumAxisDistanceInset(int inset)
        {
            _maximumAxisDistanceInset = Mathf.Max(0, inset);
        }

        internal int CalculateMaximumDistance(int townRadius)
        {
            return Mathf.Max(0, townRadius) + _maximumDistanceOffset;
        }

        public void PlaceFreeScatterDecorations(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int remainingCount,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints)
        {
            if (remainingCount <= 0)
                return;

            int attempts = 0;
            int placed = 0;
            int maxAttempts = Mathf.Max(160, remainingCount * 24);
            int maxDistance = CalculateMaximumDistance(townRadius);

            while (placed < remainingCount && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int plotCell = context.BuildingPlotSystem.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);
                if (_directionalBiasEnabled)
                    plotCell = ApplyDirectionalBias(plotCell, centerRoadCell, _directionalBias);

                if (!IsWithinMaximumDistance(
                        plotCell,
                        centerRoadCell,
                        maxDistance,
                        out _))
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
                        "City Decoration",
                        "Decorative old-town structure.",
                        context.Config.DefaultBuildingMaxHealth,
                        reservedFootprints,
                        0),
                        out _))
                {
                    continue;
                }

                usedPlotCells.Add(plotCell);
                placed++;
            }
        }

        internal static Vector2Int ApplyDirectionalBias(
            Vector2Int plotCell,
            Vector2Int centerRoadCell,
            Vector2Int priorityDirection)
        {
            return RuntimeCityBuildingPlotState.ApplyDirectionalBias(
                plotCell,
                centerRoadCell,
                priorityDirection);
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
