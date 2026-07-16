using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Composition
{
    internal static class OperationMapGridStartupBinding
    {
        private const float FloatTolerance = 0.0001f;

        public static bool TryResolve(
            EntityManager entityManager,
            GridAuthoringConfig compatibilityConfig,
            out GridConfig grid,
            out Vector2Int[] blockedCells,
            out bool hasActiveMap,
            out string error)
        {
            grid = default;
            blockedCells = Array.Empty<Vector2Int>();
            bool resolved = OperationMapMetadataUtility.TryResolveActiveNavigationMetadata(
                entityManager,
                out OperationMapGridBlob mapGrid,
                out OperationMapNavigationMetadataBlob navigation,
                out hasActiveMap,
                out error);
            if (!resolved)
            {
                if (hasActiveMap)
                    return false;
                return TryResolveCompatibility(compatibilityConfig, out grid, out blockedCells, out error);
            }

            if (navigation.UsesSurfaceMovementMetadata != 1 ||
                navigation.SupportsDynamicBlockers != 1 ||
                navigation.SupportsDynamicOccupancy != 1)
            {
                error = "Active operation map does not declare the required surface, blocker, and occupancy capabilities.";
                return false;
            }

            grid = ToGridConfig(in mapGrid);
            Vector2Int[] authoredBlockedCells = compatibilityConfig != null
                ? compatibilityConfig.BlockedCells ?? Array.Empty<Vector2Int>()
                : Array.Empty<Vector2Int>();
            if (authoredBlockedCells.Length != mapGrid.AuthoredBlockedCellCount)
            {
                error = $"Active operation-map blocked-cell count {mapGrid.AuthoredBlockedCellCount} does not match compatibility grid count {authoredBlockedCells.Length}.";
                return false;
            }

            if (compatibilityConfig != null && !Matches(compatibilityConfig, in mapGrid))
            {
                error = "Active operation-map grid metadata does not match the compatibility grid identity.";
                return false;
            }

            blockedCells = authoredBlockedCells;
            error = null;
            return true;
        }

        private static bool TryResolveCompatibility(
            GridAuthoringConfig compatibilityConfig,
            out GridConfig grid,
            out Vector2Int[] blockedCells,
            out string error)
        {
            grid = default;
            blockedCells = Array.Empty<Vector2Int>();
            if (compatibilityConfig == null)
            {
                error = "Match startup requires active operation-map navigation metadata or a compatibility grid config.";
                return false;
            }

            grid = new GridConfig
            {
                Width = compatibilityConfig.Width,
                Height = compatibilityConfig.Height,
                CellSize = compatibilityConfig.CellSize,
                Origin = compatibilityConfig.Origin
            };
            blockedCells = compatibilityConfig.BlockedCells ?? Array.Empty<Vector2Int>();
            error = null;
            return true;
        }

        private static GridConfig ToGridConfig(in OperationMapGridBlob source) => new()
        {
            Width = source.Dimensions.x,
            Height = source.Dimensions.y,
            CellSize = source.CellSize,
            Origin = source.Origin
        };

        private static bool Matches(GridAuthoringConfig config, in OperationMapGridBlob metadata) =>
            config.Width == metadata.Dimensions.x &&
            config.Height == metadata.Dimensions.y &&
            math.abs(config.CellSize - metadata.CellSize) <= FloatTolerance &&
            math.all(math.abs((float3)config.Origin - metadata.Origin) <= new float3(FloatTolerance));
    }
}
