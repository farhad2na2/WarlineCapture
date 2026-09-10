using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    // Cache ownership and spatial indexing stay separate from unit contact sampling.
    public partial struct UnitSurfaceTrackingSystem
    {
        private const int SceneOverlayBinCellSize = 32;
        private MapSurfaceMeshCache _meshCache;
        private NativeArray<MapSurfaceSceneOverlay> _sceneSurfaceOverlayCache;
        private NativeParallelMultiHashMap<int, int> _sceneSurfaceOverlayBins;
        private int _sceneSurfaceOverlayCacheLength;
        private int2 _sceneSurfaceOverlayBinDimensions;
        private int2 _sceneSurfaceOverlaySurfaceDimensions;
        private float _sceneSurfaceOverlayCellSize;
        private float3 _sceneSurfaceOverlayGridOrigin;
        private uint _sceneSurfaceOverlayRevision;

        private void EnsureSceneOverlayCache(
            ref SystemState state,
            MapSurfaceComponent surface,
            DynamicBuffer<MapSurfaceSceneOverlay> overlays,
            uint revision)
        {
            int overlayCount = overlays.IsCreated ? overlays.Length : 0;
            bool mustRebuild =
                !_sceneSurfaceOverlayCache.IsCreated ||
                !_sceneSurfaceOverlayBins.IsCreated ||
                _sceneSurfaceOverlayCacheLength != overlayCount ||
                _sceneSurfaceOverlayRevision != revision ||
                !_sceneSurfaceOverlaySurfaceDimensions.Equals(surface.Dimensions) ||
                math.abs(_sceneSurfaceOverlayCellSize - surface.CellSize) > 0.0001f ||
                !MathApproximately(_sceneSurfaceOverlayGridOrigin, surface.GridOrigin);
            if (!mustRebuild)
                return;

            state.Dependency.Complete();
            DisposeSceneOverlayCache();
            _sceneSurfaceOverlayCacheLength = overlayCount;
            _sceneSurfaceOverlaySurfaceDimensions = surface.Dimensions;
            _sceneSurfaceOverlayCellSize = surface.CellSize;
            _sceneSurfaceOverlayGridOrigin = surface.GridOrigin;
            _sceneSurfaceOverlayRevision = revision;
            _sceneSurfaceOverlayBinDimensions = new int2(
                math.max(1, (surface.Dimensions.x + SceneOverlayBinCellSize - 1) / SceneOverlayBinCellSize),
                math.max(1, (surface.Dimensions.y + SceneOverlayBinCellSize - 1) / SceneOverlayBinCellSize));

            if (overlayCount <= 0)
            {
                _sceneSurfaceOverlayCache = new NativeArray<MapSurfaceSceneOverlay>(0, Allocator.Persistent);
                _sceneSurfaceOverlayBins = new NativeParallelMultiHashMap<int, int>(0, Allocator.Persistent);
                return;
            }

            _sceneSurfaceOverlayCache = new NativeArray<MapSurfaceSceneOverlay>(overlayCount, Allocator.Persistent);
            int binEntries = 0;
            for (int i = 0; i < overlayCount; i++)
                binEntries += CountSceneOverlayBins(surface, overlays[i]);

            _sceneSurfaceOverlayBins = new NativeParallelMultiHashMap<int, int>(
                math.max(overlayCount, binEntries),
                Allocator.Persistent);
            for (int i = 0; i < overlayCount; i++)
            {
                MapSurfaceSceneOverlay overlay = overlays[i];
                _sceneSurfaceOverlayCache[i] = overlay;
                AddSceneOverlayBins(surface, overlay, i);
            }
        }

        private int CountSceneOverlayBins(MapSurfaceComponent surface, MapSurfaceSceneOverlay overlay)
        {
            if (!TryResolveSceneOverlayBinRange(surface, overlay, out int2 minBin, out int2 maxBin))
                return 0;
            int2 span = maxBin - minBin + 1;
            return span.x * span.y;
        }

        private void AddSceneOverlayBins(MapSurfaceComponent surface, MapSurfaceSceneOverlay overlay, int overlayIndex)
        {
            if (!TryResolveSceneOverlayBinRange(surface, overlay, out int2 minBin, out int2 maxBin))
                return;

            for (int y = minBin.y; y <= maxBin.y; y++)
            {
                for (int x = minBin.x; x <= maxBin.x; x++)
                {
                    int key = x + y * _sceneSurfaceOverlayBinDimensions.x;
                    _sceneSurfaceOverlayBins.Add(key, overlayIndex);
                }
            }
        }

        private static bool TryResolveSceneOverlayBinRange(
            MapSurfaceComponent surface,
            MapSurfaceSceneOverlay overlay,
            out int2 minBin,
            out int2 maxBin)
        {
            float2 minWorld = new(overlay.Center.x - overlay.HalfExtents.x, overlay.Center.z - overlay.HalfExtents.y);
            float2 maxWorld = new(overlay.Center.x + overlay.HalfExtents.x, overlay.Center.z + overlay.HalfExtents.y);
            float2 surfaceMin = new(surface.GridOrigin.x, surface.GridOrigin.z);
            float2 surfaceMax = surfaceMin + new float2(surface.Dimensions) * surface.CellSize;
            if (maxWorld.x <= surfaceMin.x || maxWorld.y <= surfaceMin.y ||
                minWorld.x >= surfaceMax.x || minWorld.y >= surfaceMax.y)
            {
                minBin = default;
                maxBin = default;
                return false;
            }

            int2 minCell = new(
                (int)math.floor((minWorld.x - surface.GridOrigin.x) / surface.CellSize),
                (int)math.floor((minWorld.y - surface.GridOrigin.z) / surface.CellSize));
            int2 maxCell = new(
                (int)math.floor((maxWorld.x - surface.GridOrigin.x) / surface.CellSize),
                (int)math.floor((maxWorld.y - surface.GridOrigin.z) / surface.CellSize));
            minCell = math.clamp(minCell, int2.zero, surface.Dimensions - 1);
            maxCell = math.clamp(maxCell, int2.zero, surface.Dimensions - 1);
            minBin = minCell / SceneOverlayBinCellSize;
            maxBin = maxCell / SceneOverlayBinCellSize;
            return true;
        }

        private void DisposeSceneOverlayCache()
        {
            if (_sceneSurfaceOverlayCache.IsCreated)
                _sceneSurfaceOverlayCache.Dispose();
            if (_sceneSurfaceOverlayBins.IsCreated)
                _sceneSurfaceOverlayBins.Dispose();
            _sceneSurfaceOverlayCache = default;
            _sceneSurfaceOverlayBins = default;
            _sceneSurfaceOverlayCacheLength = 0;
            _sceneSurfaceOverlayBinDimensions = default;
            _sceneSurfaceOverlaySurfaceDimensions = default;
            _sceneSurfaceOverlayCellSize = 0f;
            _sceneSurfaceOverlayGridOrigin = default;
            _sceneSurfaceOverlayRevision = 0;
        }

        private static bool MathApproximately(float3 lhs, float3 rhs)
        {
            return math.lengthsq(lhs - rhs) <= 0.000001f;
        }

    }
}
