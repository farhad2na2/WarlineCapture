using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateBefore(typeof(UnitGroundingSystem))]
public partial struct UnitSurfaceTrackingSystem : ISystem
{
    private EntityQuery _surfaceQuery;
    private EntityQuery _runtimeSurfaceOverlayQuery;

    public void OnCreate(ref SystemState state)
    {
        _surfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        _runtimeSurfaceOverlayQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeSurfaceOverlay>());
        state.RequireForUpdate(_surfaceQuery);
        state.RequireForUpdate<UnitSurfaceComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        MapSurfaceComponent surface = _surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return;

        NativeArray<BuildingRuntimeSurfaceOverlay> runtimeSurfaceOverlays = default;
        NativeArray<MapSurfaceSceneOverlay> sceneSurfaceOverlays = default;
        Entity surfaceEntity = _surfaceQuery.GetSingletonEntity();
        if (state.EntityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
        {
            DynamicBuffer<MapSurfaceSceneOverlay> sceneOverlayBuffer =
                state.EntityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity, true);
            if (sceneOverlayBuffer.Length > 0)
                sceneSurfaceOverlays = sceneOverlayBuffer.ToNativeArray(Allocator.TempJob);
        }
        if (!sceneSurfaceOverlays.IsCreated)
            sceneSurfaceOverlays = new NativeArray<MapSurfaceSceneOverlay>(0, Allocator.TempJob);

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
            SceneSurfaceOverlays = sceneSurfaceOverlays,
            RuntimeSurfaceOverlays = runtimeSurfaceOverlays
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency = sceneSurfaceOverlays.Dispose(state.Dependency);
        state.Dependency = runtimeSurfaceOverlays.Dispose(state.Dependency);
    }

    [WithNone(typeof(UnitAirMovement))]
    private partial struct TrackUnitSurfacesJob : IJobEntity
    {
        [ReadOnly] public MapSurfaceComponent Surface;
        [ReadOnly] public NativeArray<MapSurfaceSceneOverlay> SceneSurfaceOverlays;
        [ReadOnly] public NativeArray<BuildingRuntimeSurfaceOverlay> RuntimeSurfaceOverlays;

        public void Execute(
            ref UnitSurfaceComponent unitSurface,
            in UnitGrid unitGrid,
            in UnitMovementBehavior movementBehavior,
            in LocalTransform transform)
        {
            if (!TrySampleInterpolatedSurface(transform.Position, unitSurface, out MapSurfaceSample sample, out float height, out float3 normal))
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
                sceneOverlay.Height > height)
            {
                height = sceneOverlay.Height;
                normal = math.normalizesafe(sceneOverlay.Normal, new float3(0f, 1f, 0f));
                sample.SurfaceType = sceneOverlay.SurfaceType;
                sample.MovementMask = sceneOverlay.MovementMask;
                sample.Flags = sceneOverlay.Flags;
                sample.LayerId = sceneOverlay.LayerId;
            }

            if (TryResolveRuntimeSurfaceOverlay(transform.Position, movementMask, out BuildingRuntimeSurfaceOverlay overlay) &&
                overlay.Height > height)
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

            if (!TryResolveSurface(currentCell, unitSurface, out sample) &&
                !TryResolveSurface(minCell, unitSurface, out sample))
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

            bool found = false;
            float bestHeight = float.NegativeInfinity;
            for (int i = 0; i < SceneSurfaceOverlays.Length; i++)
            {
                MapSurfaceSceneOverlay candidate = SceneSurfaceOverlays[i];
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

            int cellIndex = cell.x + cell.y * Surface.Dimensions.x;
            ref MapSurfaceBlob blob = ref Surface.SurfaceBlob.Value;
            if ((uint)cellIndex >= (uint)blob.Cells.Length)
                return false;

            MapSurfaceCell surfaceCell = blob.Cells[cellIndex];
            if (surfaceCell.SurfaceCount == 0)
                return false;

            int firstSurfaceIndex = surfaceCell.FirstSurfaceIndex;
            int surfaceCount = surfaceCell.SurfaceCount;
            if (unitSurface.HasSurface != 0)
            {
                for (int i = 0; i < surfaceCount; i++)
                {
                    int surfaceIndex = firstSurfaceIndex + i;
                    if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
                        break;

                    MapSurfaceSample candidate = blob.Samples[surfaceIndex];
                    if (candidate.SurfaceId != unitSurface.SurfaceId ||
                        candidate.LayerId != unitSurface.LayerId)
                    {
                        continue;
                    }

                    sample = candidate;
                    return true;
                }

                for (int i = 0; i < surfaceCount; i++)
                {
                    int surfaceIndex = firstSurfaceIndex + i;
                    if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
                        break;

                    MapSurfaceSample candidate = blob.Samples[surfaceIndex];
                    if (candidate.LayerId != unitSurface.LayerId)
                        continue;

                    sample = candidate;
                    return true;
                }
            }

            if ((uint)firstSurfaceIndex >= (uint)blob.Samples.Length)
                return false;

            sample = blob.Samples[firstSurfaceIndex];
            return true;
        }
    }
}
