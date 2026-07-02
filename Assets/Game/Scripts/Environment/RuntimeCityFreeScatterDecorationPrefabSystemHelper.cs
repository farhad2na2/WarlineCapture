using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityFreeScatterDecorationPrefabSystemHelper
    {
        private readonly RuntimeCityFreeScatterDecorationState _state = new();

        public RuntimeCityFreeScatterDecorationState State => _state;

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
            int maxDistance = townRadius + 3;

            while (placed < remainingCount && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int plotCell = context.BuildingPlotSystem.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);

                int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
                if (distance > maxDistance)
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
    }
}
