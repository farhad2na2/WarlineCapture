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

public readonly struct UiShellStateModel
{
    public readonly UiShellMode CurrentMode;
    public readonly UIRoute ActiveRoute;
    public readonly UiShellTransitionPhase Phase;
    public readonly int TransitionSequenceId;
    public readonly bool IsTransitionRunning;

    public UiShellStateModel(
        UiShellMode currentMode,
        UIRoute activeRoute,
        UiShellTransitionPhase phase,
        int transitionSequenceId,
        bool isTransitionRunning)
    {
        CurrentMode = currentMode;
        ActiveRoute = activeRoute;
        Phase = phase;
        TransitionSequenceId = transitionSequenceId;
        IsTransitionRunning = isTransitionRunning;
    }
}

public readonly struct UiShellLoadingProgressModel
{
    public readonly float Progress01;
    public readonly string Status;
    public readonly bool IsComplete;

    public UiShellLoadingProgressModel(float progress01, string status, bool isComplete)
    {
        Progress01 = progress01;
        Status = status;
        IsComplete = isComplete;
    }
}

public readonly struct UiShellPresentationCommandModel
{
    public readonly UiShellCommandKind Kind;
    public readonly UiShellRegionId Region;
    public readonly UIRoute Route;
    public readonly UiShellMode TargetMode;
    public readonly int SequenceId;

    public UiShellPresentationCommandModel(
        UiShellCommandKind kind,
        UiShellRegionId region,
        UIRoute route,
        UiShellMode targetMode,
        int sequenceId)
    {
        Kind = kind;
        Region = region;
        Route = route;
        TargetMode = targetMode;
        SequenceId = sequenceId;
    }
}

public readonly struct UiShellTransitionCompleteModel
{
    public readonly UiShellCommandKind Kind;
    public readonly UiShellRegionId Region;
    public readonly int SequenceId;

    public UiShellTransitionCompleteModel(
        UiShellCommandKind kind,
        UiShellRegionId region,
        int sequenceId)
    {
        Kind = kind;
        Region = region;
        SequenceId = sequenceId;
    }
}
