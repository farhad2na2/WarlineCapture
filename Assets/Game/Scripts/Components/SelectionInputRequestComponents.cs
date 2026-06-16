using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public enum RtsSelectionPointerRequestKind : byte
{
    Pressed,
    Released,
    Clicked,
    DragUpdated,
    CameraDragDelta,
    SelectionRectUpdated,
    SelectionRectCommitted,
    SelectionRectCancelled
}

public enum RtsSelectionCommandIntentKind : byte
{
    None,
    Move,
    Attack,
    BoardTransport,
    DisembarkTransport,
    FocusUnit,
    SelectAll,
    SelectAllSoldiers,
    SelectAllVehicles,
    EnterSelectionMode,
    ExitSelectionMode,
    DeselectAll,
    EnterMoveTargetMode,
    EnterAttackTargetMode,
    HoldPosition,
    Stop,
    ReturnToBase,
    DestroyFocusedUnit,
    ToggleAttackTargetMode,
    CancelAttackTargetMode,
    EnterScanTargetMode,
    Scan,
    BoardNearestSoldiers,
    EnterBoardTargetMode,
    BoardSelectedTransport,
    BoardSelectedTransportPassenger,
    CancelActiveCommandMode,
    BoardAllSelectedTransport,
    DisembarkTransportPassenger
}

public enum BoardCommandModeDirection : byte
{
    None,
    PassengerToTransport,
    TransportToPassenger
}

public enum RtsSelectionCommandTargetKind : byte
{
    None,
    Entity,
    Cell,
    WorldPosition,
    ScreenRect
}

public enum RtsSelectionCommandFeedbackLifetime : byte
{
    Hidden,
    Persistent,
    Transient
}

public struct RtsSelectionInputRequestQueueComponent : IComponentData
{
    public int LastRequestId;
}

public struct RtsSelectionInputStateComponent : IComponentData
{
    public uint QueuedMoveOrderToken;
    public float2 DragStart;
    public float2 DragCurrent;
    public float2 LastPointerPosition;
    public byte PointerPressedOverUi;
    public byte IsDraggingSelection;
    public byte IgnoreNextLeftMouseRelease;
    public byte SkipNextWorldReleaseAfterSelection;
    public int IgnoreWorldCommandsUntilFrame;
    public byte IgnoreUiClickUntilRelease;
    public byte SelectionModeHoldArmed;
    public float SelectionModeHoldStartTime;
    public byte HasQueuedMoveOrder;
    public float2 QueuedMoveOrderScreenPosition;
    public int QueuedMoveOrderFrame;
    public float4 LastLiveSelectionRect;
    public byte HasLiveSelectionRect;
    public float2 LastKnownPointerPosition;
    public byte HasLastKnownPointerPosition;
    public int ActiveCommandMode;
    public int ActiveCommandModeFrame;
    public byte ActiveCommandModeOneShot;
    public byte ActiveCommandModeRequiresWorldTarget;
    public byte ActiveBoardCommandDirection;
    public Entity ActiveBoardTransport;
    public byte BoardPassengerDragArmed;
    public byte HasLastMoveTargetClick;
    public float2 LastMoveTargetClickScreenPosition;
    public float LastMoveTargetClickTime;
}

public struct RtsSelectionPointerRequestElement : IBufferElementData
{
    public RtsSelectionPointerRequestKind Kind;
    public int RequestId;
    public int Frame;
    public float2 ScreenPosition;
    public float2 ScreenDelta;
    public float2 DragStart;
    public float2 DragCurrent;
    public byte SelectionFilter;
}

public struct RtsSelectionCommandIntentRequestElement : IBufferElementData
{
    public RtsSelectionCommandIntentKind Kind;
    public int RequestId;
    public int Frame;
    public Entity SourceEntity;
    public Entity TargetEntity;
    public Entity SecondaryTargetEntity;
    public int2 TargetCell;
    public float3 WorldPosition;
    public float2 ScreenPosition;
    public float2 DragStart;
    public float2 DragCurrent;
    public RtsSelectionCommandTargetKind TargetKind;
    public byte ExplicitAttackTargetMode;
    public byte HasSourceEntity;
    public byte HasTargetEntity;
    public byte HasSecondaryTargetEntity;
    public byte HasTargetCell;
    public byte HasWorldPosition;
    public byte HasScreenPosition;
    public byte HasScreenRect;
}

public struct RtsSelectionCommandResultElement : IBufferElementData
{
    public RtsSelectionCommandIntentKind Kind;
    public int RequestId;
    public int Frame;
    public Entity SourceEntity;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float2 ScreenPosition;
    public float3 WorldPosition;
    public RtsSelectionCommandTargetKind TargetKind;
    public int CommandMode;
    public byte HasCommandResult;
    public byte Accepted;
    public int ReasonCode;
    public RtsSelectionCommandFeedbackLifetime FeedbackLifetime;
    public float FeedbackDurationSeconds;
    public byte EmitScreenMarker;
    public byte MarkerFactionId;
    public byte HasSourceEntity;
    public byte DeferredToSource;
    public byte HasTargetEntity;
    public byte HasTargetCell;
    public byte HasWorldPosition;
    public byte ShowWorldMarkers;
    public int RevealedCount;
    public int RadiusCells;
    public FixedString64Bytes Message;
}

public struct BuildingTargetMoveOrderQueueComponent : IComponentData
{
    public int LastRequestId;
}

public struct BuildingTargetMoveOrderRequestElement : IBufferElementData
{
    public int RequestId;
    public int2 OriginCell;
    public int2 FootprintCells;
}

public struct BuildingTargetMoveOrderResultElement : IBufferElementData
{
    public int RequestId;
    public int2 OriginCell;
    public int2 FootprintCells;
    public int2 GoalCell;
    public int IssuedUnitCount;
    public byte Accepted;
}
