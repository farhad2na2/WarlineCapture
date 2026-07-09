using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum ResourceExchangeRouteType : byte
    {
        Export = 0,
        Import = 1
    }

    public enum ResourceExchangeQueueState : byte
    {
        None = 0,
        Pending = 1,
        InProgress = 2,
        Completing = 3,
        Completed = 4,
        Cancelled = 5,
        Blocked = 6
    }

    public enum ResourceExchangeReason : byte
    {
        None = 0,
        ExchangeUnavailable = 1,
        RecipeLocked = 2,
        InsufficientCredits = 3,
        InsufficientMaterials = 4,
        InsufficientOil = 5,
        InsufficientFuel = 6,
        InputBelowMinimum = 7,
        InputAboveMaximum = 8,
        InputStepInvalid = 9,
        QueueFull = 10,
        StorageFull = 11,
        StorageMissing = 12,
        TransportUnavailable = 13,
        RushUnavailable = 14,
        InsufficientRushTickets = 15,
        CancelUnavailable = 16,
        MissionEnding = 17,
        MissingRecipeId = 18,
        DuplicateRecipeId = 19,
        InvalidRecipe = 20,
        InvalidResource = 21,
        InvalidRate = 22,
        InvalidDuration = 23,
        InvalidRushRule = 24,
        InvalidScenarioGate = 25
    }

    public enum ResourceExchangeResourceKind : byte
    {
        Credits = 0,
        Materials = 1,
        Oil = 2,
        Fuel = 3,
        RushTickets = 4
    }

    public enum ResourceExchangeRequestKind : byte
    {
        Start = 0,
        Cancel = 1,
        Rush = 2,
        RushAll = 3,
        ClearCompleted = 4,
        MissionEnd = 5
    }

    public enum ResourceExchangeResultKind : byte
    {
        RequestAccepted = 0,
        RequestRejected = 1,
        QueueStarted = 2,
        QueueBlocked = 3,
        QueueCompleted = 4,
        QueueCancelled = 5,
        RushAccepted = 6,
        RushRejected = 7
    }

    public enum ResourceExchangeVisualCueKind : byte
    {
        None = 0,
        ExchangeStarted = 1,
        ExportLoadStarted = 2,
        TransportPlaneLanding = 3,
        TransportPlaneDeparting = 4,
        ImportUnloadStarted = 5,
        ExchangeCompleted = 6,
        ExchangeCancelled = 7
    }

    public enum ResourceExchangePresentationAnchorKind : byte
    {
        None = 0,
        BaseDepot = 1,
        RunwayLandingZone = 2,
        Storage = 3,
        FallbackSafe = 4
    }

    public enum ResourceExchangeDeltaFlyoutKind : byte
    {
        None = 0,
        InputReserved = 1,
        OutputGranted = 2,
        InputRefunded = 3,
        RushTicketsSpent = 4
    }

    public struct ResourceExchangeEnabledComponent : IComponentData
    {
        public byte Enabled;
        public byte FactionId;
        public byte AllowRush;
        public byte AllowWorldPresentation;
        public int MaxQueueItems;
        public FixedString64Bytes ScenarioTag;
        public uint Version;
    }

    public struct ResourceExchangeWalletComponent : IComponentData
    {
        public byte FactionId;
        public int Credits;
        public int Materials;
        public int Oil;
        public int Fuel;
        public int RushTickets;
        public int MaterialsCapacity;
        public int OilCapacity;
        public int FuelCapacity;
        public uint Version;
    }

    public struct ResourceExchangeRequestQueueComponent : IComponentData
    {
        public int LastRequestId;
        public int LastQueueItemId;
    }

    public struct ResourceExchangeRecipeComponent : IBufferElementData
    {
        public FixedString128Bytes RecipeId;
        public FixedString128Bytes DisplayName;
        public ResourceExchangeRouteType RouteType;
        public ResourceExchangeResourceKind InputResource;
        public ResourceExchangeResourceKind OutputResource;
        public int InputAmountMin;
        public int InputAmountMax;
        public int InputStep;
        public float OutputPerInput;
        public float FeePercent;
        public float DurationSecondsBase;
        public float DurationSecondsPerStep;
        public int RushTicketSecondsPerTicket;
        public int MaxRushTickets;
        public byte RequiresStorage;
        public byte RequiresTransportPlane;
        public byte RequiresTruckPresentation;
        public byte Enabled;
        public FixedString64Bytes MissionTag;
        public ResourceExchangeReason DisabledReason;
        public int SortOrder;
    }

    public struct ResourceExchangeRequestComponent : IBufferElementData
    {
        public int RequestId;
        public ResourceExchangeRequestKind RequestKind;
        public byte FactionId;
        public FixedString128Bytes RecipeId;
        public int InputAmount;
        public int QueueItemId;
        public int RushTickets;
        public int FrameCount;
    }

    public struct ResourceExchangeQueueComponent : IBufferElementData
    {
        public int QueueItemId;
        public byte FactionId;
        public FixedString128Bytes RecipeId;
        public ResourceExchangeRouteType RouteType;
        public ResourceExchangeResourceKind InputResource;
        public ResourceExchangeResourceKind OutputResource;
        public int InputAmount;
        public int ReservedInputAmount;
        public int OutputAmount;
        public ResourceExchangeQueueState State;
        public ResourceExchangeReason StateReason;
        public float StartTimeSeconds;
        public float DurationSeconds;
        public float RemainingSeconds;
        public int RushTicketsSpent;
        public byte PresentationStarted;
        public byte OutputApplied;
        public byte VisualStartedEmitted;
        public byte VisualLoadEmitted;
        public byte VisualLandingEmitted;
        public byte VisualDepartingEmitted;
        public byte VisualUnloadEmitted;
        public byte VisualCompletionEmitted;
        public byte VisualCancellationEmitted;
        public uint Version;
    }

    public struct ResourceExchangeResultComponent : IBufferElementData
    {
        public int RequestId;
        public int QueueItemId;
        public byte FactionId;
        public ResourceExchangeResultKind ResultKind;
        public byte Accepted;
        public ResourceExchangeReason Reason;
        public FixedString128Bytes RecipeId;
        public ResourceExchangeResourceKind InputResource;
        public ResourceExchangeResourceKind OutputResource;
        public int InputAmount;
        public int OutputAmount;
        public int RushTicketsSpent;
    }

    public struct ResourceExchangeSummaryComponent : IComponentData
    {
        public byte FactionId;
        public byte Enabled;
        public byte AllowRush;
        public byte AllowWorldPresentation;
        public int QueueCount;
        public int ActiveCount;
        public int CompletedCount;
        public int MaxQueueItems;
        public ResourceExchangeReason LastReason;
        public uint Version;
    }

    public struct ResourceExchangeEconomyEventComponent : IBufferElementData
    {
        public int QueueItemId;
        public byte FactionId;
        public ResourceExchangeResultKind ResultKind;
        public ResourceExchangeResourceKind ResourceKind;
        public int Amount;
        public FixedString128Bytes RecipeId;
    }

    public struct ResourceExchangeDeltaFlyoutComponent : IBufferElementData
    {
        public int SequenceId;
        public int QueueItemId;
        public byte FactionId;
        public ResourceExchangeDeltaFlyoutKind FlyoutKind;
        public ResourceExchangeResultKind ResultKind;
        public ResourceExchangeResourceKind ResourceKind;
        public int Amount;
        public FixedString128Bytes RecipeId;
    }

    public struct ResourceExchangeVisualRequestComponent : IBufferElementData
    {
        public int QueueItemId;
        public byte FactionId;
        public ResourceExchangeVisualCueKind CueKind;
        public FixedString128Bytes RecipeId;
        public ResourceExchangeRouteType RouteType;
        public ResourceExchangeResourceKind InputResource;
        public ResourceExchangeResourceKind OutputResource;
        public int InputAmount;
        public int OutputAmount;
        public ResourceExchangePresentationAnchorKind RequestedAnchorKind;
        public ResourceExchangePresentationAnchorKind ResolvedAnchorKind;
        public float3 AnchorPosition;
        public quaternion AnchorRotation;
        public float AnchorRadius;
        public byte AnchorResolved;
        public byte UsedFallbackAnchor;
    }

    public struct ResourceExchangePresentationAnchorComponent : IBufferElementData
    {
        public byte FactionId;
        public ResourceExchangePresentationAnchorKind AnchorKind;
        public FixedString64Bytes AnchorId;
        public float3 Position;
        public quaternion Rotation;
        public float Radius;
        public byte IsValid;
    }
}
