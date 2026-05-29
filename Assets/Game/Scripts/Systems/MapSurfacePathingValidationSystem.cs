using Unity.Mathematics;

public readonly struct MapSurfacePathingValidationSystem
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
        int index = cell.x + cell.y * surface.Dimensions.x;
        if ((uint)index >= (uint)blob.Cells.Length)
            return false;

        MapSurfaceCell surfaceCell = blob.Cells[index];
        if (surfaceCell.SurfaceCount == 0 || (uint)surfaceCell.FirstSurfaceIndex >= (uint)blob.Samples.Length)
            return false;

        MapSurfaceSample sample = blob.Samples[surfaceCell.FirstSurfaceIndex];
        if ((sample.MovementMask & movementMask) == 0 ||
            sample.SurfaceType == MapSurfaceType.Blocked ||
            (sample.Flags & MapSurfaceFlags.Reserved) != 0)
        {
            return false;
        }

        float maxSlopeDegrees = GetMaxSlopeForMovement(movementMask);
        return maxSlopeDegrees > 0f && math.max(0f, sample.SlopeDegrees) <= maxSlopeDegrees;
    }

    public MapSurfaceMovementMask ResolveMovementMask(bool isVehicle)
    {
        return isVehicle
            ? MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle
            : MapSurfaceMovementMask.Infantry;
    }

    private static float GetMaxSlopeForMovement(MapSurfaceMovementMask movementMask)
    {
        if ((movementMask & MapSurfaceMovementMask.Infantry) != 0)
            return MapSurfaceSlopeClassificationSystem.SteepSlopeDegrees;
        if ((movementMask & MapSurfaceMovementMask.TrackedVehicle) != 0)
            return MapSurfaceSlopeClassificationSystem.GentleSlopeDegrees;
        if ((movementMask & MapSurfaceMovementMask.WheeledVehicle) != 0)
            return MapSurfaceSlopeClassificationSystem.GentleSlopeDegrees;

        return 0f;
    }
}
