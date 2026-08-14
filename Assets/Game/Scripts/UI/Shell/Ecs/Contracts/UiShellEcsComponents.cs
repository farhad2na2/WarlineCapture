using Unity.Collections;
using Unity.Entities;
using Game.UI.Contracts;

namespace Game.UI.Shell.Contracts.Ecs
{
    public struct UiShellRootComponent : IComponentData
    {
    }

    public enum UiShellStartupDisposition : byte
    {
        Pending = 0,
        FirstLaunch = 1,
        EnterMenu = 2,
        EnterMission = 3
    }

    public struct UiShellStartupDispositionComponent : IComponentData
    {
        public UiShellStartupDisposition Value;
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

    public struct UiShellLoadingProgressRequestComponent : IBufferElementData
    {
        public float Progress01;
        public FixedString64Bytes Status;
        public byte IsComplete;
    }

    public struct UiDiagnosticsOverlayComponent : IComponentData
    {
        public int Fps;
        public byte LogVisible;
        public FixedString4096Bytes LogText;
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

    public struct UiShellCommanderProfileComponent : IComponentData
    {
        public FixedString64Bytes Name;
        public FixedString64Bytes Subtitle;
        public FixedString64Bytes PortraitClass;
    }

    public struct UiShellMainMenuResourcesComponent : IComponentData
    {
        public FixedString32Bytes CreditsText;
        public FixedString32Bytes CommandText;
    }

    public struct UiShellActivePopupComponent : IComponentData
    {
        public UiShellPopupKind PopupKind;
        public byte Visible;
    }

    public struct UiMatchHudPassengerDrawerStateComponent : IComponentData
    {
        public byte Visible;
    }

    public struct UiMatchHudSquadTrayStateComponent : IComponentData
    {
        public MatchHudSquadTraySlot SelectedSlot;
    }

    public struct UiMatchHudHeaderComponent : IComponentData
    {
        public uint ResourceVersion;
        public FixedString32Bytes OrderText;
        public FixedString32Bytes SquadText;
        public FixedString32Bytes FuelText;
        public FixedString32Bytes MaterialsText;
        public FixedString32Bytes CivilianRiskText;
    }

    public struct UiMatchIdentityReadModelComponent : IComponentData
    {
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes MissionId;
        public uint Version;
    }

    public struct UiMatchHudStatusSurfacesComponent : IComponentData
    {
        public FixedString32Bytes ObjectivesTitle;
        public FixedString64Bytes Objective0Text;
        public FixedString64Bytes Objective1Text;
        public FixedString64Bytes Objective2Text;
        public UiMatchHudObjectiveIconKind Objective0IconKind;
        public UiMatchHudObjectiveIconKind Objective1IconKind;
        public UiMatchHudObjectiveIconKind Objective2IconKind;
        public FixedString32Bytes ElapsedText;
        public byte ThreatVisible;
        public FixedString64Bytes ThreatTitle;
        public FixedString64Bytes ThreatSubtitle;
        public FixedString64Bytes ThreatAudioEventId;
        public byte JumpEnabled;
        public byte FeedbackVisible;
        public FixedString64Bytes FeedbackText;
        public FixedString64Bytes FeedbackAudioEventId;
        public byte BoardAllVisible;
        public byte BoardAllEnabled;
        public byte CancelVisible;
        public byte CancelEnabled;
    }

    public struct UiMatchHudMinimapComponent : IComponentData
    {
        public float ViewportLeftPercent;
        public float ViewportTopPercent;
        public float ViewportWidthPercent;
        public float ViewportHeightPercent;
        public byte ZoomInEnabled;
        public byte ZoomOutEnabled;
        public byte FocusEnabled;
        public byte FriendlyAVisible;
        public float FriendlyALeftPercent;
        public float FriendlyATopPercent;
        public byte FriendlyBVisible;
        public float FriendlyBLeftPercent;
        public float FriendlyBTopPercent;
        public byte HostileAVisible;
        public float HostileALeftPercent;
        public float HostileATopPercent;
        public byte CivilianVisible;
        public float CivilianLeftPercent;
        public float CivilianTopPercent;
    }

    public struct UiBuildDrawerDetailComponent : IComponentData
    {
        public FixedString64Bytes Name;
        public FixedString32Bytes Role;
        public FixedString64Bytes PreviewSpriteKey;
        public FixedString128Bytes Description;
        public FixedString32Bytes FootprintText;
        public FixedString64Bytes RequirementsText;
        public FixedString64Bytes PlacementText;
        public FixedString32Bytes ProductionTimeText;
        public FixedString32Bytes MaterialsCostText;
        public FixedString32Bytes FuelCostText;
        public FixedString128Bytes InstructionText;
        public FixedString32Bytes ProductionTitle;
        public FixedString32Bytes ProductionCountText;
        public BuildingUiCommandFailure DisabledReason;
        public byte BuildEnabled;
        public byte RushEnabled;
        public byte ClearEnabled;
        public byte NoProductionVisible;
    }

    public struct UiBuildDrawerStateComponent : IComponentData
    {
        public BuildDrawerCategory ActiveCategory;
        public int SelectedCatalogSlot;
        public int BuildingsCount;
        public int VehiclesCount;
        public int AircraftsCount;
        public int SoldiersCount;
    }

    public struct UiBuildDrawerActiveProductionComponent : IComponentData
    {
        public byte Visible;
        public byte CancelEnabled;
        public FixedString64Bytes ThumbnailSpriteKey;
        public FixedString64Bytes Name;
        public FixedString32Bytes PercentText;
        public float Progress01;
    }

    public struct UiBuildDrawerCatalogItemComponent : IBufferElementData
    {
        public byte Visible;
        public byte Enabled;
        public byte Selected;
        public BuildingUiCommandFailure DisabledReason;
        public BuildDrawerCategory Category;
        public FixedString64Bytes ThumbnailSpriteKey;
        public FixedString64Bytes Title;
        public FixedString32Bytes Role;
        public FixedString32Bytes MaterialsText;
        public FixedString32Bytes FuelText;
        public FixedString32Bytes TimeText;
    }

    public struct UiBuildDrawerQueueRowComponent : IBufferElementData
    {
        public byte Visible;
        public byte ActionEnabled;
        public FixedString64Bytes ThumbnailSpriteKey;
        public FixedString32Bytes NumberText;
        public FixedString64Bytes Name;
        public FixedString32Bytes TimeText;
    }

    public enum UiResourceExchangeTab : byte
    {
        Export = 0,
        Import = 1
    }

    public enum UiResourceExchangeQueueState : byte
    {
        None = 0,
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4,
        Blocked = 5
    }

    public struct UiResourceExchangeStateComponent : IComponentData
    {
        public UiResourceExchangeTab ActiveTab;
        public int SelectedRecipeSlot;
        public int SelectedInputAmount;
        public int ExportRecipeCount;
        public int ImportRecipeCount;
        public int QueueCount;
        public int ActiveCount;
        public int CompletedCount;
        public int MaxQueueItems;
        public FixedString32Bytes QueueCapacityText;
        public FixedString32Bytes MaterialsText;
        public FixedString32Bytes OilText;
        public FixedString32Bytes FuelText;
        public FixedString32Bytes RushTicketsText;
        public byte ExchangeEnabled;
        public byte RushAllEnabled;
        public byte ClearCompletedEnabled;
        public uint Version;
    }

    public struct UiResourceExchangeDetailComponent : IComponentData
    {
        public FixedString128Bytes RecipeId;
        public FixedString64Bytes Name;
        public FixedString32Bytes RouteText;
        public FixedString64Bytes RateText;
        public FixedString32Bytes AmountText;
        public FixedString32Bytes InputCostText;
        public FixedString32Bytes OutputPreviewText;
        public FixedString32Bytes DurationText;
        public FixedString64Bytes RequirementsText;
        public FixedString128Bytes InstructionText;
        public byte ConfirmEnabled;
        public byte WarningVisible;
    }

    public struct UiResourceExchangeRecipeCardComponent : IBufferElementData
    {
        public byte Visible;
        public byte Enabled;
        public byte Selected;
        public byte Locked;
        public byte WarningVisible;
        public UiResourceExchangeTab Tab;
        public FixedString128Bytes RecipeId;
        public FixedString64Bytes Title;
        public FixedString32Bytes InputText;
        public FixedString32Bytes OutputText;
        public FixedString32Bytes DurationText;
        public FixedString64Bytes ReasonText;
    }

    public struct UiResourceExchangeQueueRowComponent : IBufferElementData
    {
        public byte Visible;
        public byte RushEnabled;
        public byte CancelEnabled;
        public byte CompletedVisible;
        public int QueueItemId;
        public UiResourceExchangeQueueState State;
        public FixedString32Bytes NumberText;
        public FixedString64Bytes Name;
        public FixedString32Bytes InputText;
        public FixedString32Bytes OutputText;
        public FixedString32Bytes TimeText;
        public FixedString32Bytes PercentText;
        public FixedString64Bytes StateText;
        public float Progress01;
    }

    public struct UiBuildPlacementConfirmationBarComponent : IComponentData
    {
        public byte Visible;
        public byte CanConfirm;
        public byte CanCancel;
        public byte CanRotate;
        public FixedString64Bytes Title;
        public FixedString64Bytes Status;
        public FixedString32Bytes CostText;
        public FixedString32Bytes DurationText;
        public FixedString128Bytes InstructionText;
    }

    public struct UiShellArmoryCategoryRequestComponent : IBufferElementData
    {
        public ArmoryCatalogCategory Category;
    }

    public struct UiActionRequestComponent : IBufferElementData
    {
        public UiActionKind Kind;
        public int PayloadId;
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
        public UiShellPopupKind PopupKind;
        public int SequenceId;
    }

    public struct UiShellTransitionCompleteComponent : IBufferElementData
    {
        public UiShellCommandKind Kind;
        public UiShellRegionId Region;
        public int SequenceId;
    }
}
