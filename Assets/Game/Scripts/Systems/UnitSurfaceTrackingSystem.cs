using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateBefore(typeof(UnitGroundingSystem))]
public partial struct UnitSurfaceTrackingSystem : ISystem
{
    private EntityQuery _surfaceQuery;

    public void OnCreate(ref SystemState state)
    {
        _surfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        state.RequireForUpdate(_surfaceQuery);
        state.RequireForUpdate<UnitSurfaceComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        MapSurfaceComponent surface = _surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return;

        var job = new TrackUnitSurfacesJob
        {
            Surface = surface
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [WithNone(typeof(UnitAirMovement))]
    private partial struct TrackUnitSurfacesJob : IJobEntity
    {
        [ReadOnly] public MapSurfaceComponent Surface;

        public void Execute(ref UnitSurfaceComponent unitSurface, in UnitGrid unitGrid)
        {
            if (!TryResolveSurface(unitGrid.Cell, unitSurface, out MapSurfaceSample sample))
                return;

            unitSurface.SurfaceId = sample.SurfaceId;
            unitSurface.LayerId = sample.LayerId;
            unitSurface.LastSampledHeight = sample.Height;
            unitSurface.LastSampledNormal = math.normalizesafe(sample.Normal, new float3(0f, 1f, 0f));
            unitSurface.HasSurface = 1;
            unitSurface.IsGrounded = 0;
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
            }

            if ((uint)firstSurfaceIndex >= (uint)blob.Samples.Length)
                return false;

            sample = blob.Samples[firstSurfaceIndex];
            return true;
        }
    }
}
