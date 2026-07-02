using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityRuralBuildingSpawnPrefabSystemHelper
    {
        private readonly RuntimeCityRuralBuildingSpawnState _state = new();

        public RuntimeCityRuralBuildingSpawnState State => _state;

        public void PlaceRuralBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
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
                townRadius,
                roadCellSizeInGridCells,
                roadCells,
                ref rng,
                usedPlotCells,
                reservedFootprints,
                placementAnchors,
                secondaryPlacementAnchors);
        }
    }

    internal sealed class RuntimeCityRuralBuildingSpawnState
    {
        public void PlaceRuralBuildings(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            RuntimeCityBuildingPlacementState placementSystem,
            List<GameObject> prefabs,
            int count,
            Vector2Int centerRoadCell,
            int townRadius,
            int roadCellSizeInGridCells,
            HashSet<Vector2Int> roadCells,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0)
                return;

            int attempts = 0;
            int placed = 0;
            int maxAttempts = Mathf.Max(120, count * 20);
            while (placed < count && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int plotCell = new(
                    centerRoadCell.x + rng.NextInt(-(townRadius + 3), townRadius + 4),
                    centerRoadCell.y + rng.NextInt(-(townRadius + 3), townRadius + 4));

                int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
                if (distance < context.Config.HallPlazaRadiusRoadCells + 5 || distance > townRadius + 3)
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
    }
}
