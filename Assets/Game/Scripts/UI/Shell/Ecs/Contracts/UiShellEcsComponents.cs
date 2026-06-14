using Unity.Collections;
using Unity.Entities;

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
