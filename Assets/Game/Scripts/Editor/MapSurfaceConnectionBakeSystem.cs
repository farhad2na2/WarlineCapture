using Unity.Mathematics;

public sealed class MapSurfaceConnectionBakeSystem
{
    public MapSurfaceConnection CreateBridgeApproachConnection(
        int roadSurfaceId,
        int bridgeDeckSurfaceId,
        int2 direction,
        MapSurfaceMovementMask movementMask)
    {
        return CreateConnection(
            roadSurfaceId,
            bridgeDeckSurfaceId,
            direction,
            MapSurfaceConnectionType.BridgeApproach,
            movementMask);
    }

    public MapSurfaceConnection CreateRampConnection(
        int fromSurfaceId,
        int toSurfaceId,
        int2 direction,
        MapSurfaceMovementMask movementMask)
    {
        return CreateConnection(
            fromSurfaceId,
            toSurfaceId,
            direction,
            MapSurfaceConnectionType.Ramp,
            movementMask);
    }

    public MapSurfaceConnection CreateBridgeDeckSameLayerConnection(
        int fromBridgeDeckSurfaceId,
        int toBridgeDeckSurfaceId,
        int2 direction,
        MapSurfaceMovementMask movementMask)
    {
        return CreateConnection(
            fromBridgeDeckSurfaceId,
            toBridgeDeckSurfaceId,
            direction,
            MapSurfaceConnectionType.SameLayer,
            movementMask);
    }

    public MapSurfaceConnection CreateLowerRoadSameLayerConnection(
        int fromLowerSurfaceId,
        int toLowerSurfaceId,
        int2 direction,
        MapSurfaceMovementMask movementMask)
    {
        return CreateConnection(
            fromLowerSurfaceId,
            toLowerSurfaceId,
            direction,
            MapSurfaceConnectionType.SameLayer,
            movementMask);
    }

    private static MapSurfaceConnection CreateConnection(
        int fromSurfaceId,
        int toSurfaceId,
        int2 direction,
        MapSurfaceConnectionType connectionType,
        MapSurfaceMovementMask movementMask)
    {
        return new MapSurfaceConnection
        {
            FromSurfaceId = fromSurfaceId,
            ToSurfaceId = toSurfaceId,
            Direction = math.clamp(direction, new int2(-1, -1), new int2(1, 1)),
            ConnectionType = connectionType,
            MovementMask = movementMask
        };
    }
}
