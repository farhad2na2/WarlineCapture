using Unity.Mathematics;

public sealed class MapSurfaceRuntimeValidationProbeSystem
{
    private const float MaxVehiclePitchRollDegrees = 20f;
    private readonly MapSurfaceQuerySystem _querySystem = new();
    private readonly MapSurfaceLayeredCellSystem _layeredCellSystem = new();
    private readonly MapSurfaceConnectionSystem _connectionSystem = new();
    private readonly MapSurfaceSlopeClassificationSystem _slopeClassificationSystem = new();

    public readonly struct Result
    {
        public readonly bool UnitMoveOverSlopeGrounded;
        public readonly bool TankVisualPitchRollResolved;
        public readonly bool BridgeAndHighwaySeparated;
        public readonly float SlopeHeight;
        public readonly float TankPitchDegrees;
        public readonly int BridgeSurfaceId;
        public readonly int HighwaySurfaceId;

        public Result(
            bool unitMoveOverSlopeGrounded,
            bool tankVisualPitchRollResolved,
            bool bridgeAndHighwaySeparated,
            float slopeHeight,
            float tankPitchDegrees,
            int bridgeSurfaceId,
            int highwaySurfaceId)
        {
            UnitMoveOverSlopeGrounded = unitMoveOverSlopeGrounded;
            TankVisualPitchRollResolved = tankVisualPitchRollResolved;
            BridgeAndHighwaySeparated = bridgeAndHighwaySeparated;
            SlopeHeight = slopeHeight;
            TankPitchDegrees = tankPitchDegrees;
            BridgeSurfaceId = bridgeSurfaceId;
            HighwaySurfaceId = highwaySurfaceId;
        }
    }

    public bool RunProbe(MapSurfaceComponent surface, int2 slopeCell, int2 layeredBridgeCell, out Result result)
    {
        result = default;
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return false;

        MapSurfaceQuerySystem.Context queryContext = new(surface);
        bool slopeResolved = _querySystem.TryGetPrimarySurface(queryContext, slopeCell, out MapSurfaceSample slopeSample);
        float slopeHeight = 0f;
        bool unitGrounded = slopeResolved &&
            _querySystem.TrySampleHeight(queryContext, slopeCell, out slopeHeight) &&
            _slopeClassificationSystem.AllowsMovement(slopeSample, MapSurfaceMovementMask.Infantry);
        float tankPitch = slopeResolved ? ResolveVehiclePitchDegrees(slopeSample.Normal) : 0f;
        bool tankAligned = math.abs(tankPitch) > 0.1f;
        bool separated = TryProbeBridgeHighwaySeparation(surface, layeredBridgeCell, out int bridgeSurfaceId, out int highwaySurfaceId);

        result = new Result(
            unitGrounded,
            tankAligned,
            separated,
            slopeResolved ? slopeHeight : 0f,
            tankPitch,
            bridgeSurfaceId,
            highwaySurfaceId);
        return unitGrounded && tankAligned && separated;
    }

    private bool TryProbeBridgeHighwaySeparation(
        MapSurfaceComponent surface,
        int2 layeredBridgeCell,
        out int bridgeSurfaceId,
        out int highwaySurfaceId)
    {
        bridgeSurfaceId = -1;
        highwaySurfaceId = -1;
        if (!_layeredCellSystem.TryGetSurfaceRange(surface, layeredBridgeCell, out MapSurfaceCellSurfaceRange range) ||
            range.SurfaceCount < 2)
        {
            return false;
        }

        MapSurfaceSample bridge = default;
        MapSurfaceSample highway = default;
        bool hasBridge = false;
        bool hasHighway = false;
        for (int i = 0; i < range.SurfaceCount; i++)
        {
            if (!_layeredCellSystem.TryGetSurface(surface, range, i, out MapSurfaceSample sample))
                continue;

            if (sample.SurfaceType == MapSurfaceType.BridgeDeck)
            {
                bridge = sample;
                bridgeSurfaceId = sample.SurfaceId;
                hasBridge = true;
            }
            else if (sample.SurfaceType == MapSurfaceType.Highway)
            {
                highway = sample;
                highwaySurfaceId = sample.SurfaceId;
                hasHighway = true;
            }
        }

        if (!hasBridge || !hasHighway)
            return false;

        MapSurfaceConnectionSystem.Context context = new(surface);
        return !_connectionSystem.TryFindConnection(
            context,
            bridge,
            highway.SurfaceId,
            int2.zero,
            MapSurfaceMovementMask.Infantry,
            out _);
    }

    private static float ResolveVehiclePitchDegrees(float3 normal)
    {
        float3 resolvedNormal = math.normalizesafe(normal, math.up());
        return math.clamp(
            math.degrees(math.atan2(resolvedNormal.z, resolvedNormal.y)),
            -MaxVehiclePitchRollDegrees,
            MaxVehiclePitchRollDegrees);
    }
}
