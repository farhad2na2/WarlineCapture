using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateBefore(typeof(UnitGroundingSystem))]
public partial struct UnitSurfaceTrackingSystem : ISystem
{
    private const float MaxInfantryInterpolatedHeightSpan = 0.75f;
    private const float MaxInfantrySupportLift = 1.5f;
    private const int SceneOverlayBinCellSize = 32;
    private EntityQuery _surfaceQuery;
    private EntityQuery _runtimeSurfaceOverlayQuery;
    private NativeArray<MapSurfaceSceneOverlay> _sceneSurfaceOverlayCache;
    private NativeParallelMultiHashMap<int, int> _sceneSurfaceOverlayBins;
    private int _sceneSurfaceOverlayCacheLength;
    private int2 _sceneSurfaceOverlayBinDimensions;
    private int2 _sceneSurfaceOverlaySurfaceDimensions;
    private float _sceneSurfaceOverlayCellSize;
    private float3 _sceneSurfaceOverlayGridOrigin;

    public void OnCreate(ref SystemState state)
    {
        _surfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        _runtimeSurfaceOverlayQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
            ComponentType.ReadOnly<BuildingRuntimeSurfaceOverlay>());
        state.RequireForUpdate(_surfaceQuery);
        state.RequireForUpdate<UnitSurfaceComponent>();
    }

    public void OnDestroy(ref SystemState state)
    {
        DisposeSceneOverlayCache();
    }

    public void OnUpdate(ref SystemState state)
    {
        MapSurfaceComponent surface = _surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return;

        NativeArray<BuildingRuntimeSurfaceOverlay> runtimeSurfaceOverlays = default;
        Entity surfaceEntity = _surfaceQuery.GetSingletonEntity();
        DynamicBuffer<MapSurfaceSceneOverlay> sceneOverlayBuffer = default;
        bool hasSceneOverlayBuffer = false;
        if (state.EntityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
        {
            sceneOverlayBuffer = state.EntityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity, true);
            hasSceneOverlayBuffer = true;
        }
        EnsureSceneOverlayCache(surface, hasSceneOverlayBuffer ? sceneOverlayBuffer : default);

        if (!_runtimeSurfaceOverlayQuery.IsEmptyIgnoreFilter)
        {
            Entity boundaryEntity = _runtimeSurfaceOverlayQuery.GetSingletonEntity();
            if (state.EntityManager.HasBuffer<BuildingRuntimeSurfaceOverlay>(boundaryEntity))
            {
                DynamicBuffer<BuildingRuntimeSurfaceOverlay> overlayBuffer =
                    state.EntityManager.GetBuffer<BuildingRuntimeSurfaceOverlay>(boundaryEntity, true);
                if (overlayBuffer.Length > 0)
                    runtimeSurfaceOverlays = overlayBuffer.ToNativeArray(Allocator.TempJob);
            }
        }
        if (!runtimeSurfaceOverlays.IsCreated)
            runtimeSurfaceOverlays = new NativeArray<BuildingRuntimeSurfaceOverlay>(0, Allocator.TempJob);

        var job = new TrackUnitSurfacesJob
        {
            Surface = surface,
            SceneSurfaceOverlays = _sceneSurfaceOverlayCache,
            SceneSurfaceOverlayBins = _sceneSurfaceOverlayBins.IsCreated
                ? _sceneSurfaceOverlayBins.AsReadOnly()
                : default,
            SceneSurfaceOverlayBinDimensions = _sceneSurfaceOverlayBinDimensions,
            SceneOverlayBinCellSize = SceneOverlayBinCellSize,
            RuntimeSurfaceOverlays = runtimeSurfaceOverlays
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency = runtimeSurfaceOverlays.Dispose(state.Dependency);
    }

    private void EnsureSceneOverlayCache(MapSurfaceComponent surface, DynamicBuffer<MapSurfaceSceneOverlay> overlays)
    {
        int overlayCount = overlays.IsCreated ? overlays.Length : 0;
        bool mustRebuild =
            !_sceneSurfaceOverlayCache.IsCreated ||
            !_sceneSurfaceOverlayBins.IsCreated ||
            _sceneSurfaceOverlayCacheLength != overlayCount ||
            !_sceneSurfaceOverlaySurfaceDimensions.Equals(surface.Dimensions) ||
            math.abs(_sceneSurfaceOverlayCellSize - surface.CellSize) > 0.0001f ||
            !MathApproximately(_sceneSurfaceOverlayGridOrigin, surface.GridOrigin);
        if (!mustRebuild)
            return;

        DisposeSceneOverlayCache();
        _sceneSurfaceOverlayCacheLength = overlayCount;
        _sceneSurfaceOverlaySurfaceDimensions = surface.Dimensions;
        _sceneSurfaceOverlayCellSize = surface.CellSize;
        _sceneSurfaceOverlayGridOrigin = surface.GridOrigin;
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
        ResolveSceneOverlayBinRange(surface, overlay, out int2 minBin, out int2 maxBin);
        int2 span = maxBin - minBin + 1;
        return math.max(1, span.x * span.y);
    }

    private void AddSceneOverlayBins(MapSurfaceComponent surface, MapSurfaceSceneOverlay overlay, int overlayIndex)
    {
        ResolveSceneOverlayBinRange(surface, overlay, out int2 minBin, out int2 maxBin);

        for (int y = minBin.y; y <= maxBin.y; y++)
        {
            for (int x = minBin.x; x <= maxBin.x; x++)
            {
                int key = x + y * _sceneSurfaceOverlayBinDimensions.x;
                _sceneSurfaceOverlayBins.Add(key, overlayIndex);
            }
        }
    }

    private static void ResolveSceneOverlayBinRange(
        MapSurfaceComponent surface,
        MapSurfaceSceneOverlay overlay,
        out int2 minBin,
        out int2 maxBin)
    {
        float2 minWorld = new(overlay.Center.x - overlay.HalfExtents.x, overlay.Center.z - overlay.HalfExtents.y);
        float2 maxWorld = new(overlay.Center.x + overlay.HalfExtents.x, overlay.Center.z + overlay.HalfExtents.y);
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
    }

    private static bool MathApproximately(float3 lhs, float3 rhs)
    {
        return math.lengthsq(lhs - rhs) <= 0.000001f;
    }

    [BurstCompile]
    [WithNone(typeof(UnitAirMovement))]
    private partial struct TrackUnitSurfacesJob : IJobEntity
    {
        [ReadOnly] public MapSurfaceComponent Surface;
        [ReadOnly] public NativeArray<MapSurfaceSceneOverlay> SceneSurfaceOverlays;
        [ReadOnly] public NativeParallelMultiHashMap<int, int>.ReadOnly SceneSurfaceOverlayBins;
        [ReadOnly] public int2 SceneSurfaceOverlayBinDimensions;
        [ReadOnly] public int SceneOverlayBinCellSize;
        [ReadOnly] public NativeArray<BuildingRuntimeSurfaceOverlay> RuntimeSurfaceOverlays;

        public void Execute(
            ref UnitSurfaceComponent unitSurface,
            in UnitGrid unitGrid,
            in UnitMovementBehavior movementBehavior,
            in LocalTransform transform)
        {
            if (!TrySampleInterpolatedSurface(transform.Position, unitSurface, movementBehavior, out MapSurfaceSample sample, out float height, out float3 normal))
            {
                if (!TryResolveSurface(unitGrid.Cell, unitSurface, out sample))
                    return;

                height = sample.Height;
                normal = math.normalizesafe(sample.Normal, new float3(0f, 1f, 0f));
            }

            MapSurfaceMovementMask movementMask = movementBehavior.UsesVehicleMotion != 0
                ? MapSurfaceMovementMask.AllGroundUnits
                : MapSurfaceMovementMask.Infantry;
            if (TryResolveSceneSurfaceOverlay(transform.Position, movementMask, out MapSurfaceSceneOverlay sceneOverlay) &&
                ShouldApplySceneOverlay(sceneOverlay, height))
            {
                height = sceneOverlay.Height;
                normal = math.normalizesafe(sceneOverlay.Normal, new float3(0f, 1f, 0f));
                sample.SurfaceType = sceneOverlay.SurfaceType;
                sample.MovementMask = sceneOverlay.MovementMask;
                sample.Flags = sceneOverlay.Flags;
                sample.LayerId = sceneOverlay.LayerId;
            }

            if (TryResolveRuntimeSurfaceOverlay(transform.Position, movementMask, out BuildingRuntimeSurfaceOverlay overlay) &&
                ShouldApplyRuntimeOverlay(overlay, height))
            {
                height = overlay.Height;
                normal = math.normalizesafe(overlay.Normal, new float3(0f, 1f, 0f));
                sample.SurfaceType = overlay.SurfaceType;
                sample.MovementMask = overlay.MovementMask;
            }

            unitSurface.SurfaceId = sample.SurfaceId;
            unitSurface.LayerId = sample.LayerId;
            unitSurface.LastSampledHeight = height;
            unitSurface.LastSampledNormal = normal;
            unitSurface.HasSurface = 1;
            unitSurface.IsGrounded = 0;
        }

        private bool TrySampleInterpolatedSurface(
            float3 worldPosition,
            UnitSurfaceComponent unitSurface,
            UnitMovementBehavior movementBehavior,
            out MapSurfaceSample sample,
            out float height,
            out float3 normal)
        {
            sample = default;
            height = 0f;
            normal = new float3(0f, 1f, 0f);

            if (Surface.HasSurfaceData == 0 ||
                !Surface.SurfaceBlob.IsCreated ||
                Surface.CellSize <= 0f)
            {
                return false;
            }

            float2 local = new float2(
                (worldPosition.x - Surface.GridOrigin.x) / Surface.CellSize - 0.5f,
                (worldPosition.z - Surface.GridOrigin.z) / Surface.CellSize - 0.5f);
            int2 minCell = (int2)math.floor(local);
            float2 t = math.saturate(local - minCell);
            int2 currentCell = (int2)math.floor(new float2(
                (worldPosition.x - Surface.GridOrigin.x) / Surface.CellSize,
                (worldPosition.z - Surface.GridOrigin.z) / Surface.CellSize));

            bool hasCurrentSample = TryResolveSurface(currentCell, unitSurface, out MapSurfaceSample currentSample);
            if (hasCurrentSample)
            {
                sample = currentSample;
            }
            else if (!TryResolveSurface(minCell, unitSurface, out sample))
            {
                return false;
            }

            float h00 = sample.Height;
            float3 n00 = math.normalizesafe(sample.Normal, new float3(0f, 1f, 0f));
            if (!TryResolveSurface(minCell, unitSurface, out MapSurfaceSample s00))
            {
                s00 = sample;
            }
            else
            {
                h00 = s00.Height;
                n00 = math.normalizesafe(s00.Normal, new float3(0f, 1f, 0f));
            }

            if (!TryResolveSurface(minCell + new int2(1, 0), unitSurface, out MapSurfaceSample s10))
                s10 = s00;
            if (!TryResolveSurface(minCell + new int2(0, 1), unitSurface, out MapSurfaceSample s01))
                s01 = s00;
            if (!TryResolveSurface(minCell + new int2(1, 1), unitSurface, out MapSurfaceSample s11))
                s11 = s00;

            if (hasCurrentSample &&
                !CanInterpolateWithNeighborSamples(currentSample, s00, s10, s01, s11, movementBehavior))
            {
                sample = currentSample;
                height = currentSample.Height;
                normal = math.normalizesafe(currentSample.Normal, new float3(0f, 1f, 0f));
                if (movementBehavior.UsesVehicleMotion == 0 &&
                    TryResolveHighestNearbySupport(currentCell, sample, out MapSurfaceSample currentSupportSample) &&
                    currentSupportSample.Height > height &&
                    currentSupportSample.Height - height <= MaxInfantrySupportLift)
                {
                    sample = currentSupportSample;
                    height = currentSupportSample.Height;
                    normal = math.normalizesafe(currentSupportSample.Normal, normal);
                }

                return true;
            }

            float h10 = s10.Height;
            float h01 = s01.Height;
            float h11 = s11.Height;
            float hx0 = math.lerp(h00, h10, t.x);
            float hx1 = math.lerp(h01, h11, t.x);
            height = math.lerp(hx0, hx1, t.y);

            float3 n10 = math.normalizesafe(s10.Normal, n00);
            float3 n01 = math.normalizesafe(s01.Normal, n00);
            float3 n11 = math.normalizesafe(s11.Normal, n00);
            float3 nx0 = math.lerp(n00, n10, t.x);
            float3 nx1 = math.lerp(n01, n11, t.x);
            normal = math.normalizesafe(math.lerp(nx0, nx1, t.y), new float3(0f, 1f, 0f));

            if (hasCurrentSample &&
                movementBehavior.UsesVehicleMotion == 0 &&
                currentSample.Height > height)
            {
                sample = currentSample;
                height = currentSample.Height;
                normal = math.normalizesafe(currentSample.Normal, normal);
            }

            if (movementBehavior.UsesVehicleMotion == 0 &&
                TryResolveHighestNearbySupport(currentCell, sample, out MapSurfaceSample nearbySupportSample) &&
                nearbySupportSample.Height > height &&
                nearbySupportSample.Height - height <= MaxInfantrySupportLift)
            {
                sample = nearbySupportSample;
                height = nearbySupportSample.Height;
                normal = math.normalizesafe(nearbySupportSample.Normal, normal);
            }

            return true;
        }

        private bool TryResolveHighestNearbySupport(
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
                    if (!TryResolveSurface(cell, default, out MapSurfaceSample candidate))
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

        private static bool CanInterpolateWithNeighborSamples(
            MapSurfaceSample current,
            MapSurfaceSample s00,
            MapSurfaceSample s10,
            MapSurfaceSample s01,
            MapSurfaceSample s11,
            UnitMovementBehavior movementBehavior)
        {
            bool currentRoadLike = IsRoadLikeSurface(current.SurfaceType, current.Flags);
            if (!CanInterpolateSample(current, s00, currentRoadLike) ||
                !CanInterpolateSample(current, s10, currentRoadLike) ||
                !CanInterpolateSample(current, s01, currentRoadLike) ||
                !CanInterpolateSample(current, s11, currentRoadLike))
            {
                return false;
            }

            float minHeight = math.min(math.min(s00.Height, s10.Height), math.min(s01.Height, s11.Height));
            float maxHeight = math.max(math.max(s00.Height, s10.Height), math.max(s01.Height, s11.Height));
            float maxSpan = movementBehavior.UsesVehicleMotion != 0
                ? 0.5f
                : MaxInfantryInterpolatedHeightSpan;
            return maxHeight - minHeight <= maxSpan;
        }

        private static bool CanInterpolateSample(MapSurfaceSample current, MapSurfaceSample candidate, bool currentRoadLike)
        {
            if (candidate.LayerId != current.LayerId)
                return false;

            bool candidateRoadLike = IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
            if (candidateRoadLike != currentRoadLike)
                return false;

            if ((candidate.MovementMask & current.MovementMask) == 0)
                return false;

            return true;
        }

        private bool TryResolveSceneSurfaceOverlay(
            float3 worldPosition,
            MapSurfaceMovementMask movementMask,
            out MapSurfaceSceneOverlay overlay)
        {
            overlay = default;
            if (!SceneSurfaceOverlays.IsCreated || SceneSurfaceOverlays.Length == 0)
                return false;
            if (!SceneSurfaceOverlayBins.IsCreated ||
                SceneSurfaceOverlayBinDimensions.x <= 0 ||
                SceneSurfaceOverlayBinDimensions.y <= 0 ||
                SceneOverlayBinCellSize <= 0)
            {
                return false;
            }

            bool found = false;
            float bestHeight = float.NegativeInfinity;
            int2 cell = new(
                (int)math.floor((worldPosition.x - Surface.GridOrigin.x) / Surface.CellSize),
                (int)math.floor((worldPosition.z - Surface.GridOrigin.z) / Surface.CellSize));
            if ((uint)cell.x >= (uint)Surface.Dimensions.x ||
                (uint)cell.y >= (uint)Surface.Dimensions.y)
            {
                return false;
            }

            int2 bin = cell / SceneOverlayBinCellSize;
            int key = bin.x + bin.y * SceneSurfaceOverlayBinDimensions.x;
            if (!SceneSurfaceOverlayBins.TryGetFirstValue(key, out int overlayIndex, out NativeParallelMultiHashMapIterator<int> iterator))
                return false;

            do
            {
                if ((uint)overlayIndex >= (uint)SceneSurfaceOverlays.Length)
                    continue;

                MapSurfaceSceneOverlay candidate = SceneSurfaceOverlays[overlayIndex];
                if ((candidate.MovementMask & movementMask) == 0)
                    continue;
                if (!Contains(candidate, worldPosition))
                    continue;
                if (found && candidate.Height <= bestHeight)
                    continue;

                overlay = candidate;
                bestHeight = candidate.Height;
                found = true;
            }
            while (SceneSurfaceOverlayBins.TryGetNextValue(out overlayIndex, ref iterator));

            return found;
        }

        private static bool ShouldApplySceneOverlay(MapSurfaceSceneOverlay overlay, float currentHeight)
        {
            return overlay.Height > currentHeight || IsRoadLikeSurface(overlay.SurfaceType, overlay.Flags);
        }

        private static bool ShouldApplyRuntimeOverlay(BuildingRuntimeSurfaceOverlay overlay, float currentHeight)
        {
            return overlay.Height > currentHeight || IsRoadLikeSurface(overlay.SurfaceType);
        }

        private static bool IsRoadLikeSurface(MapSurfaceType surfaceType, MapSurfaceFlags flags = MapSurfaceFlags.None)
        {
            return surfaceType == MapSurfaceType.Road ||
                   surfaceType == MapSurfaceType.DirtRoad ||
                   surfaceType == MapSurfaceType.Highway ||
                   surfaceType == MapSurfaceType.BridgeDeck ||
                   surfaceType == MapSurfaceType.Ramp ||
                   (flags & MapSurfaceFlags.Road) != 0;
        }

        private static bool Contains(MapSurfaceSceneOverlay overlay, float3 worldPosition)
        {
            quaternion inverseRotation = math.inverse(overlay.Rotation);
            float3 local = math.mul(inverseRotation, worldPosition - overlay.Center);
            return math.abs(local.x) <= overlay.HalfExtents.x &&
                   math.abs(local.z) <= overlay.HalfExtents.y;
        }

        private bool TryResolveRuntimeSurfaceOverlay(
            float3 worldPosition,
            MapSurfaceMovementMask movementMask,
            out BuildingRuntimeSurfaceOverlay overlay)
        {
            overlay = default;
            if (!RuntimeSurfaceOverlays.IsCreated || RuntimeSurfaceOverlays.Length == 0)
                return false;

            bool found = false;
            float bestHeight = float.NegativeInfinity;
            for (int i = 0; i < RuntimeSurfaceOverlays.Length; i++)
            {
                BuildingRuntimeSurfaceOverlay candidate = RuntimeSurfaceOverlays[i];
                if ((candidate.MovementMask & movementMask) == 0)
                    continue;
                if (!Contains(candidate, worldPosition))
                    continue;
                if (found && candidate.Height <= bestHeight)
                    continue;

                overlay = candidate;
                bestHeight = candidate.Height;
                found = true;
            }

            return found;
        }

        private static bool Contains(BuildingRuntimeSurfaceOverlay overlay, float3 worldPosition)
        {
            quaternion inverseRotation = math.inverse(overlay.Rotation);
            float3 local = math.mul(inverseRotation, worldPosition - overlay.Center);
            return math.abs(local.x) <= overlay.HalfExtents.x &&
                   math.abs(local.z) <= overlay.HalfExtents.y;
        }

        private bool TryResolveSurface(int2 cell, UnitSurfaceComponent unitSurface, out MapSurfaceSample sample)
        {
            sample = default;

            if (Surface.HasSurfaceData == 0 ||
                !Surface.SurfaceBlob.IsCreated ||
                (uint)cell.x >= (uint)Surface.Dimensions.x ||
                (uint)cell.y >= (uint)Surface.Dimensions.y)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref Surface.SurfaceBlob.Value;
            if (!MapSurfaceBlobAccess.TryGetSurfaceRange(ref blob, cell, out MapSurfaceCellSurfaceRange range))
                return false;

            if (unitSurface.HasSurface != 0)
            {
                for (int i = 0; i < range.SurfaceCount; i++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurface(ref blob, range, i, out MapSurfaceSample candidate))
                        break;

                    if (candidate.SurfaceId != unitSurface.SurfaceId ||
                        candidate.LayerId != unitSurface.LayerId)
                    {
                        continue;
                    }

                    sample = candidate;
                    return true;
                }

                for (int i = 0; i < range.SurfaceCount; i++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurface(ref blob, range, i, out MapSurfaceSample candidate))
                        break;

                    if (candidate.LayerId != unitSurface.LayerId)
                        continue;

                    sample = candidate;
                    return true;
                }
            }

            return MapSurfaceBlobAccess.TryGetSurface(ref blob, range, 0, out sample);
        }
    }
}
