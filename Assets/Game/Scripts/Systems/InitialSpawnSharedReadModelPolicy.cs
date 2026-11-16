using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Source-owned, data-only policy used by both the gameplay owner and source harness.
    // Keep these calculations independent of visual placement and camera request systems.
    internal static class InitialSpawnSharedReadModelPolicy
    {
        internal static List<Vector2Int> BuildWallRunOrigins(
            Vector2Int start,
            Vector2Int end,
            Vector2Int footprint,
            bool vertical)
        {
            var origins = new List<Vector2Int> { start };
            if (start == end)
                return origins;

            if (vertical)
            {
                int stepCells = Mathf.Max(1, footprint.y);
                int delta = end.y - start.y;
                int direction = delta >= 0 ? 1 : -1;
                int segmentCount = Mathf.Abs(delta) / stepCells;
                for (int i = 1; i <= segmentCount; i++)
                    origins.Add(new Vector2Int(start.x, start.y + direction * stepCells * i));
            }
            else
            {
                int stepCells = Mathf.Max(1, footprint.x);
                int delta = end.x - start.x;
                int direction = delta >= 0 ? 1 : -1;
                int segmentCount = Mathf.Abs(delta) / stepCells;
                for (int i = 1; i <= segmentCount; i++)
                    origins.Add(new Vector2Int(start.x + direction * stepCells * i, start.y));
            }

            return origins;
        }

        internal static bool HasActiveCampaignMission(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            CampaignMissionRuntimeComponent runtime = query.GetSingleton<CampaignMissionRuntimeComponent>();
            return !runtime.MissionId.IsEmpty && !runtime.SessionToken.IsEmpty;
        }

        internal static Vector3 GetInitialBuildingFootprintCenterWorld(
            Vector2Int originCell,
            Vector2Int footprintCells,
            GridConfig grid)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                grid.Origin.y,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
        }
    }
}
