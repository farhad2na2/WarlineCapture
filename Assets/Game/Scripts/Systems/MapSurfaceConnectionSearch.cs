using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct MapSurfaceConnectionSearch
    {
        public readonly struct Context
        {
            public readonly MapSurfaceComponent Surface;

            public Context(MapSurfaceComponent surface)
            {
                Surface = surface;
            }

            public bool IsCreated => Surface.HasSurfaceData != 0 && Surface.SurfaceBlob.IsCreated;
        }

        public bool TryCreateContext(EntityQuery surfaceQuery, out Context context)
        {
            context = default;
            if (surfaceQuery.IsEmptyIgnoreFilter)
                return false;

            MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return false;

            context = new Context(surface);
            return true;
        }

        public bool TryGetConnection(Context context, MapSurfaceSample sample, int connectionOffset, out MapSurfaceConnection connection)
        {
            connection = default;
            if (!context.IsCreated ||
                sample.ConnectionCount == 0 ||
                connectionOffset < 0 ||
                connectionOffset >= sample.ConnectionCount)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref context.Surface.SurfaceBlob.Value;
            return MapSurfaceBlobAccess.TryGetConnection(ref blob, sample.FirstConnectionIndex + connectionOffset, out connection);
        }

        public bool TryFindConnection(
            Context context,
            MapSurfaceSample fromSample,
            int toSurfaceId,
            int2 direction,
            MapSurfaceMovementMask movementMask,
            out MapSurfaceConnection connection)
        {
            connection = default;
            int2 normalizedDirection = math.clamp(direction, new int2(-1, -1), new int2(1, 1));
            for (int i = 0; i < fromSample.ConnectionCount; i++)
            {
                if (!TryGetConnection(context, fromSample, i, out MapSurfaceConnection candidate))
                    continue;
                if (candidate.ToSurfaceId != toSurfaceId)
                    continue;
                if (!candidate.Direction.Equals(normalizedDirection))
                    continue;
                if (!AllowsMovement(candidate, movementMask))
                    continue;

                connection = candidate;
                return true;
            }

            return false;
        }

        public bool AllowsMovement(MapSurfaceConnection connection, MapSurfaceMovementMask movementMask)
        {
            if (connection.ConnectionType == MapSurfaceConnectionType.Blocked ||
                movementMask == MapSurfaceMovementMask.None)
            {
                return false;
            }

            return (connection.MovementMask & movementMask) != 0;
        }
    }
}
