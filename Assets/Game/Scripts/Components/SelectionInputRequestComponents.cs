using Unity.Entities;
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
    BoardAllSelectedTransport
}

public enum BoardCommandModeDirection : byte
{
    None,
    PassengerToTransport,
    TransportToPassenger
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
    public Entity TargetEntity;
    public Entity SecondaryTargetEntity;
    public int2 TargetCell;
    public float2 ScreenPosition;
    public float2 DragStart;
    public float2 DragCurrent;
    public byte ExplicitAttackTargetMode;
    public byte HasTargetEntity;
    public byte HasSecondaryTargetEntity;
    public byte HasTargetCell;
    public byte HasScreenPosition;
    public byte HasScreenRect;
}

public struct RtsSelectionCommandResultElement : IBufferElementData
{
    public RtsSelectionCommandIntentKind Kind;
    public int RequestId;
    public int Frame;
    public int2 TargetCell;
    public float2 ScreenPosition;
    public float3 WorldPosition;
    public byte HasCommandResult;
    public byte Accepted;
    public int ReasonCode;
    public byte EmitScreenMarker;
    public byte MarkerFactionId;
    public byte HasTargetCell;
    public byte HasWorldPosition;
    public byte ShowWorldMarkers;
    public int RevealedCount;
    public int RadiusCells;
}
