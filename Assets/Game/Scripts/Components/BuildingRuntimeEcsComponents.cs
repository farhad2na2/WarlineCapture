using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum ResourceKind : byte
    {
        Oil = 0,
        Fuel = 1
    }

    public enum FuelLogisticsTaskStatusCode : byte
    {
        None = 0,
        Idle = 1,
        Assigned = 2,
        ToSource = 3,
        Loading = 4,
        ToDestination = 5,
        Unloading = 6,
        Blocked = 7
    }

    public enum FuelLogisticsBlockReasonCode : byte
    {
        None = 0,
        SourceUnavailable = 1,
        DestinationUnavailable = 2,
        DestinationFull = 3,
        RouteUnavailable = 4,
        ReservationFailed = 5,
        HaulerUnavailable = 6,
        InsufficientUsableFuel = 7
    }

    public struct FuelLogisticsOilSourceTag : IComponentData
    {
    }

    public struct FuelLogisticsRefineryInputTag : IComponentData
    {
    }

    public struct FuelLogisticsRefineryOutputTag : IComponentData
    {
    }

    public struct FuelLogisticsFuelStorageTag : IComponentData
    {
    }

    public struct BuildingRuntimeStateTag : IComponentData
    {
    }

    public struct BuildingConfiguredSpawnableReadModel : IBufferElementData
    {
        public FixedString128Bytes BuildingId;
        public FixedString128Bytes DisplayName;
        public int Price;
        public int MaterialsCost;
        public int2 FootprintCells;
        public byte CanRequest;
    }

    public struct BuildingConfiguredUnitReadModel : IBufferElementData
    {
        public FixedString128Bytes UnitId;
        public FixedString128Bytes DisplayName;
        public int Price;
        public byte CanRequest;
        public byte IsVehicle;
    }

    public struct BuildingProductionSlotReadModel : IBufferElementData
    {
        public FixedString128Bytes BuildingId;
        public int SlotIndex;
        public FixedString64Bytes UnitSourceKey;
        public FixedString128Bytes UnitId;
    }

    public struct BuildingProductionSpawnRequest : IBufferElementData
    {
        public const byte Pending = 0;
        public const byte Succeeded = 1;
        public const byte Failed = 2;

        public int RequestId;
        public int BuildingRuntimeId;
        public int ProductionIndex;
        public int ReservedProductionSlotIndex;
        public byte OwnerFactionId;
        public byte HasOwnerFaction;
        public byte HasOverrideWorldPosition;
        public byte HasOverrideCell;
        public byte Status;
        public FixedString64Bytes UnitSourceKey;
        public Entity PrefabEntity;
        public Entity ProducedUnit;
        public int2 SpawnCell;
        public float3 SpawnWorldPosition;
    }

    public struct BuildingRecentSpawnReservation : IBufferElementData
    {
        public int2 Cell;
        public int2 Size;
        public float ExpiresAt;
    }

    public struct BuildingProducedUnitReadModel : IBufferElementData
    {
        public int BuildingRuntimeId;
        public int ProductionSlotBuildingRuntimeId;
        public int ProductionIndex;
        public int ProductionSlotIndex;
        public byte OwnerFactionId;
        public byte HasOwnerFaction;
        public Entity Unit;
        public FixedString64Bytes UnitSourceKey;
    }

    public struct MapVehiclePlacementReadModel : IBufferElementData
    {
        public int PlacementIndex;
        public FixedString128Bytes SourcePath;
        public FixedString128Bytes Category;
        public FixedString64Bytes VehicleSourceKey;
        public Entity Prefab;
        public int2 FootprintCells;
        public byte FactionId;
        public byte HasPrefab;
        public float3 WorldCenter;
        public float3 WorldPosition;
        public float3 WorldEulerAngles;
        public float3 WorldScale;
    }

    public struct MapVehiclePlacementProgressState : IComponentData
    {
        public const uint InitialRandomState = 0x6D2B79F5u;

        public byte Queued;
        public byte AuthoringHidden;
        public int NextPlacementIndex;
        public int LastClearedBlockerCells;
        public uint RandomState;
    }

    public struct BuildingRuntimeFactionSummary : IBufferElementData
    {
        public byte FactionId;
        public int BuildingCount;
        public float StoredOilBarrels;
        public float StoredFuelBarrels;
        public float OilBarrelsPerDay;
        public float FuelBarrelsPerDay;
    }

    public struct BuildingRuntimeFactionUsableFuelSummary : IBufferElementData
    {
        public byte FactionId;
        public float StoredOilBarrels;
        public float StoredFuelBarrels;
        public float CurrentFuelBarrels;
        public float FuelProducedBarrels;
        public float FuelDeliveredBarrels;
        public float FuelSpentBarrels;
        public int OilStorageCapacity;
        public int FuelStorageCapacity;
        public uint Version;
    }

    public struct BuildingResourceStorageComponent : IComponentData
    {
        public int RuntimeBuildingId;
        public byte OwnerFactionId;
        public int OilStorageCapacity;
        public int FuelStorageCapacity;
        public float OilBarrelsPerDay;
        public float FuelBarrelsPerDay;
        public float StoredOilBarrels;
        public float StoredFuelBarrels;
        public float ReservedOilInboundBarrels;
        public float ReservedOilOutboundBarrels;
        public float ReservedFuelInboundBarrels;
        public float ReservedFuelOutboundBarrels;
        public uint Version;
    }

    public struct BuildingRuntimeOwnedBuildingSummary : IBufferElementData
    {
        public byte FactionId;
        public FixedString128Bytes BuildingId;
        public int Count;
    }

    public struct BuildingRuntimeUnitProductionSummary : IBufferElementData
    {
        public byte FactionId;
        public FixedString128Bytes UnitId;
        public int ProducedCount;
        public int QueuedCount;
    }

    public struct BuildingFactionProductionSpawnPointReadModel : IBufferElementData
    {
        public byte FactionId;
        public FixedString128Bytes BuildingId;
        public int BuildingRuntimeId;
        public int SlotIndex;
        public int2 Cell;
        public float3 WorldPosition;
    }

    public struct BuildingFactionRunwayReadModel : IBufferElementData
    {
        public byte FactionId;
        public FixedString128Bytes BuildingId;
        public int BuildingRuntimeId;
        public int2 TakeoffCell;
        public int2 LandingCell;
        public float3 TakeoffPosition;
        public float3 LandingPosition;
        public float3 Center;
        public float3 Direction;
        public float2 HalfExtents;
    }

    public struct BuildingRuntimeSurfaceOverlay : IBufferElementData
    {
        public int BuildingRuntimeId;
        public float3 Center;
        public quaternion Rotation;
        public float2 HalfExtents;
        public float Height;
        public float3 Normal;
        public MapSurfaceType SurfaceType;
        public MapSurfaceMovementMask MovementMask;
    }

    public struct BuildingUiProductionCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct BuildingUiProductionCommandRequestElement : IBufferElementData
    {
        public const byte KindSelectedBuildingUnit = 0;
        public const byte KindBuildingUnit = 1;
        public const byte KindCancelProduction = 2;

        public int RequestId;
        public int BuildingId;
        public int ProductionIndex;
        public int FrameCount;
        public byte RequestKind;
    }

    public struct BuildingUiProductionCommandResultElement : IBufferElementData
    {
        public const byte Queued = 0;
        public const byte MissingActiveBuilding = 1;
        public const byte MissingProducerBuilding = 2;
        public const byte MissingUnitConfig = 3;
        public const byte NotArmed = 4;
        public const byte QueueRejected = 5;
        public const byte MissingPendingProduction = 6;
        public const byte CancelRejected = 7;
        public const byte Cancelled = 8;
        public const byte UnavailablePrefab = 9;
        public const byte QueueFull = 10;
        public const byte GlobalQueueFull = 11;

        public int RequestId;
        public int BuildingId;
        public int ProductionIndex;
        public byte RequestKind;
        public byte Accepted;
        public byte ResultCode;
        public int ReasonCode;
    }

    public struct BuildingUiPlacementCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct BuildingUiPlacementCommandRequestElement : IBufferElementData
    {
        public const byte KindConfirmPlacement = 0;
        public const byte KindCancelPlacement = 1;
        public const byte KindRotatePlacement = 2;
        public const byte KindExitBuildMode = 3;
        public const byte KindBeginConfiguredPlacement = 4;

        public int RequestId;
        public int EconomyTransactionId;
        public FixedString128Bytes BuildingId;
        public byte RequestKind;
        public byte ClearBuildingSelection;
    }

    public struct BuildingUiPlacementCommandResultElement : IBufferElementData
    {
        public const byte Completed = 0;
        public const byte MissingSession = 1;
        public const byte Rejected = 2;
        public const byte MissingActivePlacement = 3;
        public const byte InvalidPlacement = 4;
        public const byte BlockedPlacement = 5;
        public const byte NotEnoughMoney = 6;
        public const byte MissingConfig = 7;
        public const byte InsufficientMaterials = 8;
        public const byte InsufficientCreditsAndMaterials = 9;
        public const byte DuplicateTransaction = 10;
        public const byte RegistrationFailed = 11;
        public const byte TransactionRejected = 12;

        public int RequestId;
        public int EconomyTransactionId;
        public byte RequestKind;
        public byte Accepted;
        public byte ResultCode;
        public int ReasonCode;
    }

    public struct BuildingUiCampItemCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct BuildingUiCampItemCommandRequestElement : IBufferElementData
    {
        public int RequestId;
        public FixedString128Bytes ItemId;
        public int Price;
        public byte FocusProducerOnSuccess;
    }

    public struct BuildingUiCampItemCommandResultElement : IBufferElementData
    {
        public const byte PlacementStarted = 0;
        public const byte ProductionQueued = 1;
        public const byte NotEnoughMoney = 2;
        public const byte MissingProducerBuilding = 3;
        public const byte InvalidSelection = 4;
        public const byte ProductionQueueFull = 5;
        public const byte GlobalProductionQueueFull = 6;

        public int RequestId;
        public FixedString128Bytes ItemId;
        public FixedString128Bytes RequiredBuildingDisplayName;
        public int Price;
        public byte Accepted;
        public byte ResultCode;
        public int ReasonCode;
    }

    public struct BuildingUiSelectionCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct BuildingUiSelectionCommandRequestElement : IBufferElementData
    {
        public const byte KindDeleteSelectedBuilding = 0;
        public const byte KindClearSelectedBuilding = 1;

        public int RequestId;
        public byte RequestKind;
    }

    public struct BuildingUiSelectionCommandResultElement : IBufferElementData
    {
        public const byte Completed = 0;
        public const byte MissingSelection = 1;
        public const byte DeleteRejected = 2;
        public const byte MissingRuntimeSystem = 3;

        public int RequestId;
        public int BuildingId;
        public byte RequestKind;
        public byte Accepted;
        public byte ResultCode;
    }

    public struct RoadBuildCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct RoadBuildCommandRequestElement : IBufferElementData
    {
        public const byte KindEnterRoadBuildMode = 0;
        public const byte KindConfirmRoadBuildSession = 1;
        public const byte KindCancelRoadBuildSession = 2;
        public const byte KindExitBuildMode = 3;

        public int RequestId;
        public byte RequestKind;
    }

    public struct RoadBuildCommandResultElement : IBufferElementData
    {
        public const byte Completed = 0;
        public const byte MissingSession = 1;
        public const byte MissingRuntimeState = 2;
        public const byte MissingSessionState = 3;
        public const byte Rejected = 4;

        public int RequestId;
        public byte RequestKind;
        public byte Accepted;
        public byte ResultCode;
    }

    public struct BuildingFactionUnitProductionRequest : IBufferElementData
    {
        public const byte Pending = 0;
        public const byte Succeeded = 1;
        public const byte Failed = 2;
        public const byte MissingUnitConfig = 1;
        public const byte MissingProducerBuilding = 2;
        public const byte ProducerUnavailable = 3;

        public int RequestId;
        public byte FactionId;
        public FixedString128Bytes UnitId;
        public byte Status;
        public byte ResultCode;
        public FixedString128Bytes ProducerDisplayName;
        public FixedString128Bytes UnitDisplayName;
        public int Cost;
        public int QueueCount;
        public int ProducedCount;
    }

    public struct BuildingFactionResourceSellRequest : IBufferElementData
    {
        public const byte Pending = 0;
        public const byte Succeeded = 1;
        public const byte Failed = 2;
        public const byte NoneSold = 1;

        public int RequestId;
        public byte FactionId;
        public float RequestedOilBarrels;
        public float RequestedFuelBarrels;
        public byte Status;
        public byte ResultCode;
        public float SoldOilBarrels;
        public float SoldFuelBarrels;
    }

    public struct BuildingRuntimeSpawnRequest : IBufferElementData
    {
        public const byte Pending = 0;
        public const byte Succeeded = 1;
        public const byte Failed = 2;
        public const byte MissingConfig = 1;
        public const byte Blocked = 2;
        public const byte KindBuilding = 0;
        public const byte KindWallRun = 1;
        public const byte KindWallSegment = 2;

        public int RequestId;
        public byte RequestKind;
        public byte FactionId;
        public byte HasOwnerFaction;
        public FixedString128Bytes BuildingId;
        public int2 PreferredOrigin;
        public int2 EndOrigin;
        public byte RotateVertical;
        public byte AllowExistingWallOverlap;
        public byte Status;
        public byte ResultCode;
        public Entity PlanEntity;
        public int EntryIndex;
        public int Cost;
        public int MaterialsCost;
        public FixedString128Bytes DisplayName;
        public int BuildingRuntimeId;
        public int2 ActualOrigin;
        public int2 ActualFootprint;
        public int SpawnedCount;
    }

    public struct BuildingRuntimeDeleteRequest : IBufferElementData
    {
        public int BuildingRuntimeId;
    }
}
