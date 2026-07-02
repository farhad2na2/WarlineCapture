using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct MapSurfaceComponent : IComponentData
    {
        public BlobAssetReference<MapSurfaceBlob> SurfaceBlob;
        public float3 GridOrigin;
        public float CellSize;
        public int2 Dimensions;
        public byte HasSurfaceData;
        public byte HasLayeredCells;
        public byte HasRoadSurfaces;
        public byte HasBridgeSurfaces;
    }

    public struct MapSurfaceFlatEquivalentRuntimeBlobTag : IComponentData
    {
    }

    public struct MapSurfaceRuntimeBakedBlobTag : IComponentData
    {
    }

    public struct MapSurfacePathCostComponent : IComponentData
    {
        public byte EnableSlopeCost;
        public int GentleSlopeTraversalCost;
        public int SteepSlopeTraversalCost;
    }

    public struct MapSurfaceSceneOverlay : IBufferElementData
    {
        public float3 Center;
        public quaternion Rotation;
        public float2 HalfExtents;
        public float Height;
        public float3 Normal;
        public MapSurfaceType SurfaceType;
        public MapSurfaceMovementMask MovementMask;
        public MapSurfaceFlags Flags;
        public int LayerId;
    }

    public struct MapSurfaceDiagnosticsComponent : IComponentData
    {
        public int CellCount;
        public int SurfaceCount;
        public int LayeredCellCount;
        public int RoadSurfaceCount;
        public int BridgeSurfaceCount;
        public int RampSurfaceCount;
        public int BlockedSurfaceCount;
        public int ConnectionCount;
        public byte HasSurfaceData;
    }

    public struct MapSurfaceBlob
    {
        public float3 GridOrigin;
        public float CellSize;
        public int2 Dimensions;
        public MapSurfaceRuntimeEncoding RuntimeEncoding;
        public float CompactMinHeight;
        public float CompactHeightStep;
        public BlobArray<MapSurfaceCell> Cells;
        public BlobArray<MapSurfaceSample> Samples;
        public BlobArray<MapSurfaceConnection> Connections;
        public BlobArray<MapSurfaceCompactSample> CompactSamples;
    }

    public struct MapSurfaceCell
    {
        public int FirstSurfaceIndex;
        public ushort SurfaceCount;
        public ushort InlineSurfaceIndex;
    }

    public struct MapSurfaceCellSurfaceRange
    {
        public int FirstSurfaceIndex;
        public ushort SurfaceCount;
        public ushort InlineSurfaceIndex;
        public byte IsLayered;
    }

    public struct MapSurfaceSample
    {
        public int2 Cell;
        public int SurfaceId;
        public int LayerId;
        public float Height;
        public float3 Normal;
        public float SlopeDegrees;
        public MapSurfaceType SurfaceType;
        public MapSurfaceMovementMask MovementMask;
        public MapSurfaceFlags Flags;
        public int FirstConnectionIndex;
        public ushort ConnectionCount;
    }

    public struct MapSurfaceConnection
    {
        public int FromSurfaceId;
        public int ToSurfaceId;
        public int2 Direction;
        public MapSurfaceConnectionType ConnectionType;
        public MapSurfaceMovementMask MovementMask;
    }

    public struct MapSurfaceCompactSample
    {
        public ushort PackedHeight;
        public short LayerId;
        public MapSurfaceMovementMask MovementMask;
        public MapSurfaceFlags Flags;
        public sbyte NormalX;
        public sbyte NormalY;
        public sbyte NormalZ;
        public MapSurfaceType SurfaceType;
    }

    public struct UnitSurfaceComponent : IComponentData
    {
        public int SurfaceId;
        public int LayerId;
        public float LastSampledHeight;
        public float3 LastSampledNormal;
        public byte HasSurface;
        public byte IsGrounded;
    }

    public struct UnitGroundOffsetComponent : IComponentData
    {
        public float Value;
    }

    public struct BuildingSurfaceComponent : IComponentData
    {
        public int SurfaceId;
        public int LayerId;
        public float FoundationHeight;
        public float MaxFootprintHeightDelta;
        public float MaxFootprintSlopeDegrees;
        public byte IsPlacementSurfaceValid;
    }

    public struct VehicleSurfaceAlignmentComponent : IComponentData
    {
        public float3 SurfaceNormal;
        public float PitchDegrees;
        public float RollDegrees;
        public float AlignmentWeight;
    }

    public enum MapSurfaceType : byte
    {
        Terrain = 0,
        Road = 1,
        DirtRoad = 2,
        BridgeDeck = 3,
        Highway = 4,
        Ramp = 5,
        Plaza = 6,
        Blocked = 255
    }

    [Flags]
    public enum MapSurfaceMovementMask : ushort
    {
        None = 0,
        Infantry = 1 << 0,
        WheeledVehicle = 1 << 1,
        TrackedVehicle = 1 << 2,
        AirGrounded = 1 << 3,
        BuildingPlacement = 1 << 4,
        AllGroundUnits = Infantry | WheeledVehicle | TrackedVehicle
    }

    [Flags]
    public enum MapSurfaceFlags : ushort
    {
        None = 0,
        Road = 1 << 0,
        Bridge = 1 << 1,
        Highway = 1 << 2,
        Ramp = 1 << 3,
        Layered = 1 << 4,
        Reserved = 1 << 5
    }

    public enum MapSurfaceConnectionType : byte
    {
        SameLayer = 0,
        Ramp = 1,
        BridgeApproach = 2,
        RoadJoin = 3,
        Blocked = 255
    }

    public enum MapSurfaceRuntimeEncoding : byte
    {
        Full = 0,
        SingleLayerCompact = 1
    }

    public enum MapSurfaceSlopeClass : byte
    {
        Flat = 0,
        Gentle = 1,
        Steep = 2,
        Blocked = 3
    }

    public enum MapSurfaceRoadPriority : byte
    {
        Neutral = 0,
        Preferred = 1,
        Avoided = 2
    }
}
