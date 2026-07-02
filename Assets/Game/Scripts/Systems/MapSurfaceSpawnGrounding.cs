using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct MapSurfaceSpawnGrounding
    {
        private const float MaxInfantrySupportLift = 1.5f;

        public bool TryGroundCellCenter(
            EntityManager entityManager,
            GridConfig grid,
            int2 cell,
            ref float3 worldPosition,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            sample = default;
            if (!TryGetSurface(entityManager, out MapSurfaceComponent surface) ||
                !TryGetSample(surface, cell, out sample))
            {
                return false;
            }

            if (TryGetHighestInfantrySupport(surface, cell, sample, out MapSurfaceSample supportSample) &&
                supportSample.Height > sample.Height &&
                supportSample.Height - sample.Height <= MaxInfantrySupportLift)
            {
                sample = supportSample;
            }

            worldPosition.y = sample.Height + groundOffset;
            return true;
        }

        public bool TryGroundWorldPosition(
            EntityManager entityManager,
            GridConfig grid,
            ref float3 worldPosition,
            out int2 cell,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            cell = GridUtils.WorldToCell(grid, worldPosition);
            return TryGroundCellCenter(entityManager, grid, cell, ref worldPosition, out sample, groundOffset);
        }

        private bool TryGetSurface(EntityManager entityManager, out MapSurfaceComponent surface)
        {
            surface = default;

            using EntityQuery surfaceQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.IsEmptyIgnoreFilter)
                return false;

            surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            return surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
        }

        private bool TryGetSample(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
        {
            sample = default;

            if ((uint)cell.x >= (uint)surface.Dimensions.x ||
                (uint)cell.y >= (uint)surface.Dimensions.y ||
                !surface.SurfaceBlob.IsCreated)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out sample);
        }

        private bool TryGetHighestInfantrySupport(
            MapSurfaceComponent surface,
            int2 centerCell,
            MapSurfaceSample anchor,
            out MapSurfaceSample supportSample)
        {
            supportSample = anchor;
            bool found = false;
            float bestHeight = anchor.Height;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int2 cell = centerCell + new int2(x, y);
                    if (!TryGetSample(surface, cell, out MapSurfaceSample candidate))
                        continue;
                    if (!CanUseInfantrySupportSample(anchor, candidate))
                        continue;
                    if (found && candidate.Height <= bestHeight)
                        continue;

                    supportSample = candidate;
                    bestHeight = candidate.Height;
                    found = true;
                }
            }

            return found;
        }

        private static bool CanUseInfantrySupportSample(MapSurfaceSample anchor, MapSurfaceSample candidate)
        {
            if ((candidate.MovementMask & MapSurfaceMovementMask.Infantry) == 0)
                return false;
            if (candidate.LayerId != anchor.LayerId)
                return false;

            bool anchorRoadLike = IsRoadLikeSurface(anchor.SurfaceType, anchor.Flags);
            bool candidateRoadLike = IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
            return anchorRoadLike == candidateRoadLike;
        }

        private static bool IsRoadLikeSurface(MapSurfaceType surfaceType, MapSurfaceFlags flags)
        {
            return surfaceType == MapSurfaceType.Road ||
                   surfaceType == MapSurfaceType.DirtRoad ||
                   surfaceType == MapSurfaceType.Highway ||
                   surfaceType == MapSurfaceType.BridgeDeck ||
                   surfaceType == MapSurfaceType.Ramp ||
                   (flags & MapSurfaceFlags.Road) != 0;
        }
    }
}
