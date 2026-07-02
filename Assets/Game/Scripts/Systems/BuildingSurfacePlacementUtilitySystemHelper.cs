using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingSurfacePlacementUtilitySystemHelper
    {
        public readonly struct Result
        {
            public readonly bool IsValid;
            public readonly float FoundationHeight;
            public readonly float MaxFootprintHeightDelta;
            public readonly float MaxFootprintSlopeDegrees;
            public readonly int SurfaceId;
            public readonly int LayerId;
            public readonly int SampleCount;

            public Result(
                bool isValid,
                float foundationHeight,
                float maxFootprintHeightDelta,
                float maxFootprintSlopeDegrees,
                int surfaceId,
                int layerId,
                int sampleCount)
            {
                IsValid = isValid;
                FoundationHeight = foundationHeight;
                MaxFootprintHeightDelta = maxFootprintHeightDelta;
                MaxFootprintSlopeDegrees = maxFootprintSlopeDegrees;
                SurfaceId = surfaceId;
                LayerId = layerId;
                SampleCount = sampleCount;
            }
        }

        public bool TryEvaluateFootprint(
            EntityManager entityManager,
            Vector2Int originCell,
            Vector2Int footprintCells,
            float maxAllowedHeightDelta,
            float maxAllowedSlopeDegrees,
            out Result result)
        {
            result = default;
            if (!TryGetSurface(entityManager, out MapSurfaceComponent surface))
                return false;

            return TryEvaluateFootprint(surface, originCell, footprintCells, maxAllowedHeightDelta, maxAllowedSlopeDegrees, out result);
        }

        public bool TryEvaluateFootprint(
            MapSurfaceComponent surface,
            Vector2Int originCell,
            Vector2Int footprintCells,
            float maxAllowedHeightDelta,
            float maxAllowedSlopeDegrees,
            out Result result)
        {
            result = default;
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return false;

            int width = math.max(1, footprintCells.x);
            int height = math.max(1, footprintCells.y);
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            float heightSum = 0f;
            float maxSlope = 0f;
            int surfaceId = -1;
            int layerId = 0;
            int sampleCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int2 cell = new(originCell.x + x, originCell.y + y);
                    if (!TryGetPrimarySample(surface, cell, out MapSurfaceSample sample))
                        return false;

                    if (surfaceId < 0)
                    {
                        surfaceId = sample.SurfaceId;
                        layerId = sample.LayerId;
                    }
                    else if (sample.SurfaceId != surfaceId || sample.LayerId != layerId)
                    {
                        result = new Result(false, heightSum / math.max(1, sampleCount), maxHeight - minHeight, maxSlope, surfaceId, layerId, sampleCount);
                        return true;
                    }

                    minHeight = math.min(minHeight, sample.Height);
                    maxHeight = math.max(maxHeight, sample.Height);
                    heightSum += sample.Height;
                    maxSlope = math.max(maxSlope, sample.SlopeDegrees);
                    sampleCount++;
                }
            }

            float heightDelta = maxHeight - minHeight;
            float foundationHeight = sampleCount > 0 ? heightSum / sampleCount : 0f;
            bool valid = sampleCount == width * height &&
                heightDelta <= math.max(0f, maxAllowedHeightDelta) &&
                maxSlope <= math.max(0f, maxAllowedSlopeDegrees);
            result = new Result(valid, foundationHeight, heightDelta, maxSlope, surfaceId, layerId, sampleCount);
            return true;
        }

        public BuildingSurfaceComponent ToComponent(Result result)
        {
            return new BuildingSurfaceComponent
            {
                SurfaceId = result.SurfaceId,
                LayerId = result.LayerId,
                FoundationHeight = result.FoundationHeight,
                MaxFootprintHeightDelta = result.MaxFootprintHeightDelta,
                MaxFootprintSlopeDegrees = result.MaxFootprintSlopeDegrees,
                IsPlacementSurfaceValid = (byte)(result.IsValid ? 1 : 0)
            };
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

        private bool TryGetPrimarySample(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
        {
            sample = default;
            if ((uint)cell.x >= (uint)surface.Dimensions.x ||
                (uint)cell.y >= (uint)surface.Dimensions.y)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out sample);
        }
    }
}
