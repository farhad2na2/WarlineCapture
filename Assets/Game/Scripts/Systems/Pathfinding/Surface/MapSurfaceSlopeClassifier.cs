using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime.Pathfinding
{
    public readonly struct MapSurfaceSlopeClassifier
    {
        public const float FlatSlopeDegrees = 5f;
        public const float GentleSlopeDegrees = 18f;
        public const float SteepSlopeDegrees = 35f;

        public MapSurfaceSlopeClass Classify(MapSurfaceSample sample)
        {
            if (sample.MovementMask == MapSurfaceMovementMask.None ||
                sample.SurfaceType == MapSurfaceType.Blocked ||
                (sample.Flags & MapSurfaceFlags.Reserved) != 0)
            {
                return MapSurfaceSlopeClass.Blocked;
            }

            float slope = math.max(0f, sample.SlopeDegrees);
            if (slope <= FlatSlopeDegrees)
                return MapSurfaceSlopeClass.Flat;
            if (slope <= GentleSlopeDegrees)
                return MapSurfaceSlopeClass.Gentle;
            if (slope <= SteepSlopeDegrees)
                return MapSurfaceSlopeClass.Steep;

            return MapSurfaceSlopeClass.Blocked;
        }

        public bool AllowsMovement(MapSurfaceSample sample, MapSurfaceMovementMask movementMask)
        {
            if (movementMask == MapSurfaceMovementMask.None)
                return false;

            return (sample.MovementMask & movementMask) != 0 && Classify(sample) != MapSurfaceSlopeClass.Blocked;
        }

        public float GetMaxSlopeForMovement(MapSurfaceMovementMask movementMask)
        {
            if ((movementMask & MapSurfaceMovementMask.Infantry) != 0)
                return SteepSlopeDegrees;
            if ((movementMask & MapSurfaceMovementMask.TrackedVehicle) != 0)
                return GentleSlopeDegrees;
            if ((movementMask & MapSurfaceMovementMask.WheeledVehicle) != 0)
                return GentleSlopeDegrees;
            if ((movementMask & MapSurfaceMovementMask.BuildingPlacement) != 0)
                return FlatSlopeDegrees;

            return 0f;
        }
    }
}
