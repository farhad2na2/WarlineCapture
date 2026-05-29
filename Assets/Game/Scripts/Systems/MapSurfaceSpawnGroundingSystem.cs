using Unity.Entities;
using Unity.Mathematics;

public readonly struct MapSurfaceSpawnGroundingSystem
{
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
        int cellIndex = cell.x + cell.y * surface.Dimensions.x;
        if ((uint)cellIndex >= (uint)blob.Cells.Length)
            return false;

        MapSurfaceCell surfaceCell = blob.Cells[cellIndex];
        if (surfaceCell.SurfaceCount == 0 || (uint)surfaceCell.FirstSurfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[surfaceCell.FirstSurfaceIndex];
        return true;
    }
}
