using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct UnitSurfaceTrackingSystem
    {
        private partial struct TrackUnitSurfacesJob
        {
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
}
