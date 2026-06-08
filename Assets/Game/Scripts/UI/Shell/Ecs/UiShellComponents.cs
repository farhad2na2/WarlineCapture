using Unity.Collections;
using Unity.Entities;

public enum UiShellMode
{
    None,
    Loading,
    MainMenu,
    MatchHud,
    PopupOnly
}

public enum UiShellTransitionPhase
{
    Idle,
    ShowingLoading,
    ExitingLoading,
    EnteringMenu,
    MenuReady,
    ExitingMenu,
    EnteringMatchHud,
    MatchHudReady,
    ShowingPopup,
    PopupVisible,
    HidingPopup
}

public enum MatchIntroTransitionStateKind : byte
{
    Inactive,
    WaitingForWorldReady,
    ExitingLoading,
    EnteringHud,
    FadingCurtain,
    Complete
}

public enum UiShellRouteIntent
{
    OpenMenuRoute,
    EnterMatch,
    ReturnToMainMenu,
    OpenSettings,
    BackMenuRoute
}

public enum UiShellPopupKind
{
    ThreatAlert,
    Pause,
    RewardUnlock
}

public enum UiShellPopupIntent
{
    Show,
    Hide
}

public enum ArmoryCatalogCategory
{
    Characters = 0,
    Vehicles = 1,
    Aircrafts = 2,
    Buildings = 3,
    Support = 4
}

public enum UiShellCommandKind
{
    ShowLoading,
    ExitLoading,
    EnterMenu,
    ExitMenu,
    SwapMenuMiddle,
    SwapLeftRegion,
    SwapRightRegion,
    EnterMatchHud,
    ExitMatchHud,
    ShowPopup,
    HidePopup
}

public enum UiShellRegionId
{
    None,
    LoadingLayer,
    HeaderRegion,
    LeftRegion,
    MiddleRegion,
    RightRegion,
    FooterRegion,
    PopupLayer
}

public struct UiShellBoundaryComponent : IComponentData
{
}

public struct UiShellStateComponent : IComponentData
{
    public UiShellMode CurrentMode;
    public UIRoute ActiveRoute;
    public UiShellTransitionPhase Phase;
    public int TransitionSequenceId;
    public byte IsTransitionRunning;
}

public struct UiShellLoadingProgressComponent : IComponentData
{
    public float Progress01;
    public FixedString64Bytes Status;
    public byte IsComplete;
}

public struct MatchIntroTransitionComponent : IComponentData
{
    public MatchIntroTransitionStateKind State;
    public float Progress01;
    public byte InputLocked;
    public int SequenceId;
    public FixedString64Bytes Status;
}

public struct UiShellArmoryCategoryComponent : IComponentData
{
    public ArmoryCatalogCategory Category;
}

public struct UiShellArmoryCategoryRequestComponent : IBufferElementData
{
    public ArmoryCatalogCategory Category;
}

public struct UiShellRouteRequestComponent : IBufferElementData
{
    public UIRoute Route;
    public UiShellRouteIntent Intent;
    public byte PushHistory;
}

public struct UiShellRouteHistoryComponent : IBufferElementData
{
    public UIRoute Route;
}

public struct UiShellPopupRequestComponent : IBufferElementData
{
    public UiShellPopupKind PopupKind;
    public UiShellPopupIntent Intent;
    public int PayloadId;
}

public struct UiShellPresentationCommandComponent : IBufferElementData
{
    public UiShellCommandKind Kind;
    public UiShellRegionId Region;
    public UIRoute Route;
    public UiShellMode TargetMode;
    public int SequenceId;
}

public struct UiShellTransitionCompleteComponent : IBufferElementData
{
    public UiShellCommandKind Kind;
    public UiShellRegionId Region;
    public int SequenceId;
}
