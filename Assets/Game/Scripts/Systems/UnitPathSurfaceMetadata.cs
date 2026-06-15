using Unity.Entities;
using Unity.Mathematics;

internal readonly struct UnitPathSurfaceMetadata
{
    public DynamicBuffer<UnitPathSurfaceNode> PrepareBuffer(EntityManager em, Entity entity)
    {
        DynamicBuffer<UnitPathSurfaceNode> buffer = em.HasBuffer<UnitPathSurfaceNode>(entity)
            ? em.GetBuffer<UnitPathSurfaceNode>(entity)
            : em.AddBuffer<UnitPathSurfaceNode>(entity);
        buffer.Clear();
        return buffer;
    }

    public void ClearIfPresent(EntityManager em, Entity entity)
    {
        if (em.HasBuffer<UnitPathSurfaceNode>(entity))
            em.GetBuffer<UnitPathSurfaceNode>(entity).Clear();
    }

    public void Append(
        DynamicBuffer<UnitPathSurfaceNode> buffer,
        MapSurfaceComponent surface,
        byte hasSurfaceData,
        int2 cell,
        UnitSurfaceComponent currentSurface)
    {
        if (TryResolvePathSurface(surface, hasSurfaceData, cell, currentSurface, out MapSurfaceSample sample))
        {
            buffer.Add(new UnitPathSurfaceNode
            {
                SurfaceId = sample.SurfaceId,
                LayerId = sample.LayerId
            });
            return;
        }

        buffer.Add(new UnitPathSurfaceNode
        {
            SurfaceId = currentSurface.HasSurface != 0 ? currentSurface.SurfaceId : -1,
            LayerId = currentSurface.HasSurface != 0 ? currentSurface.LayerId : -1
        });
    }

    private bool TryResolvePathSurface(
        MapSurfaceComponent surface,
        byte hasSurfaceData,
        int2 cell,
        UnitSurfaceComponent currentSurface,
        out MapSurfaceSample sample)
    {
        sample = default;
        if (hasSurfaceData == 0 ||
            surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            (uint)cell.x >= (uint)surface.Dimensions.x ||
            (uint)cell.y >= (uint)surface.Dimensions.y)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        int cellIndex = cell.x + cell.y * surface.Dimensions.x;
        if ((uint)cellIndex >= (uint)blob.Cells.Length)
            return false;

        MapSurfaceCell surfaceCell = blob.Cells[cellIndex];
        if (surfaceCell.SurfaceCount == 0)
            return false;

        if (currentSurface.HasSurface != 0)
        {
            for (int i = 0; i < surfaceCell.SurfaceCount; i++)
            {
                int surfaceIndex = surfaceCell.FirstSurfaceIndex + i;
                if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
                    break;

                MapSurfaceSample candidate = blob.Samples[surfaceIndex];
                if (candidate.SurfaceId == currentSurface.SurfaceId &&
                    candidate.LayerId == currentSurface.LayerId)
                {
                    sample = candidate;
                    return true;
                }
            }
        }

        if ((uint)surfaceCell.FirstSurfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[surfaceCell.FirstSurfaceIndex];
        return true;
    }
}
