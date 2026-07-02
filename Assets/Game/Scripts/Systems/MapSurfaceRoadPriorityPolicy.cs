using Game.Components;

namespace Game.Runtime
{
    public readonly struct MapSurfaceRoadPriorityPolicy
    {
        public MapSurfaceFlags NormalizeFlagsForSurfaceType(MapSurfaceType surfaceType, MapSurfaceFlags flags)
        {
            switch (surfaceType)
            {
                case MapSurfaceType.Road:
                case MapSurfaceType.DirtRoad:
                    return flags | MapSurfaceFlags.Road;
                case MapSurfaceType.BridgeDeck:
                    return flags | MapSurfaceFlags.Road | MapSurfaceFlags.Bridge;
                case MapSurfaceType.Highway:
                    return flags | MapSurfaceFlags.Road | MapSurfaceFlags.Highway;
                case MapSurfaceType.Ramp:
                    return flags | MapSurfaceFlags.Road | MapSurfaceFlags.Ramp;
                default:
                    return flags;
            }
        }

        public MapSurfaceRoadPriority ResolveGridRoadPriority(byte sidewalk, byte dirtRoad, bool isVehicle)
        {
            if (isVehicle)
            {
                if (dirtRoad != 0)
                    return MapSurfaceRoadPriority.Preferred;
                if (sidewalk != 0)
                    return MapSurfaceRoadPriority.Avoided;
                return MapSurfaceRoadPriority.Neutral;
            }

            if (sidewalk != 0)
                return MapSurfaceRoadPriority.Preferred;
            if (dirtRoad != 0)
                return MapSurfaceRoadPriority.Avoided;

            return MapSurfaceRoadPriority.Neutral;
        }
    }
}
