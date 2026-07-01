using Unity.Mathematics;

public readonly struct MapSurfaceTraversalValidation
{
    public bool CanTraverse(MapSurfaceComponent surface, byte hasSurfaceData, int2 cell, MapSurfaceMovementMask movementMask)
    {
        if (hasSurfaceData == 0)
            return true;

        if (movementMask == MapSurfaceMovementMask.None ||
            surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            (uint)cell.x >= (uint)surface.Dimensions.x ||
            (uint)cell.y >= (uint)surface.Dimensions.y)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        if (!MapSurfaceBlobAccess.TryGetSurfaceRange(ref blob, cell, out MapSurfaceCellSurfaceRange range))
            return false;

        float maxSlopeDegrees = GetMaxSlopeForMovement(movementMask);
        if (maxSlopeDegrees <= 0f)
            return false;

        for (int i = 0; i < range.SurfaceCount; i++)
        {
            if (MapSurfaceBlobAccess.TryGetSurface(ref blob, range, i, out MapSurfaceSample sample) &&
                CanTraverseSample(sample, movementMask, maxSlopeDegrees))
            {
                return true;
            }
        }

        return false;
    }

    public MapSurfaceMovementMask ResolveMovementMask(bool isVehicle)
    {
        return isVehicle
            ? MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle
            : MapSurfaceMovementMask.Infantry;
    }

    public bool CanTraverseFootprint(
        MapSurfaceComponent surface,
        byte hasSurfaceData,
        in GridConfig grid,
        int2 cell,
        int2 footprintSize,
        bool isVehicle)
    {
        if (hasSurfaceData == 0)
            return true;

        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        MapSurfaceMovementMask movementMask = ResolveMovementMask(isVehicle);
        for (int y = min.y; y < max.y; y++)
        {
            for (int x = min.x; x < max.x; x++)
            {
                if (!CanTraverse(surface, hasSurfaceData, new int2(x, y), movementMask))
                    return false;
            }
        }

        return true;
    }

    private static float GetMaxSlopeForMovement(MapSurfaceMovementMask movementMask)
    {
        if ((movementMask & MapSurfaceMovementMask.Infantry) != 0)
            return MapSurfaceSlopeClassifier.SteepSlopeDegrees;
        if ((movementMask & MapSurfaceMovementMask.TrackedVehicle) != 0)
            return MapSurfaceSlopeClassifier.GentleSlopeDegrees;
        if ((movementMask & MapSurfaceMovementMask.WheeledVehicle) != 0)
            return MapSurfaceSlopeClassifier.GentleSlopeDegrees;

        return 0f;
    }

    private static bool CanTraverseSample(MapSurfaceSample sample, MapSurfaceMovementMask movementMask, float maxSlopeDegrees)
    {
        if ((sample.MovementMask & movementMask) == 0 ||
            sample.SurfaceType == MapSurfaceType.Blocked ||
            (sample.Flags & MapSurfaceFlags.Reserved) != 0)
        {
            return false;
        }

        if (IsRoadLikeSurface(sample.SurfaceType, sample.Flags))
            return true;

        return math.max(0f, sample.SlopeDegrees) <= maxSlopeDegrees;
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
