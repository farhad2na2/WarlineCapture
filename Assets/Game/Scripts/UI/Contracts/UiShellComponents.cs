using UnityEngine;
using Game.Tactical.Contracts;

namespace Game.UI.Contracts
{
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
        BuildDrawer,
        RewardUnlock,
        Settings,
        ResourceExchange
    }

    public enum UiShellPopupIntent
    {
        Show,
        Hide
    }

    public enum UiActionKind
    {
        None,
        MatchMenu,
        ReturnSelection,
        DestroySelection,
        BoardSelection,
        TogglePassengerDrawer,
        ExitAllPassengers,
        ClosePassengerDrawer,
        JumpToThreat,
        Pause,
        OpenSettings,
        RightBuild,
        RightSupport,
        SquadSlot1,
        SquadSlot2,
        SquadSlot3,
        SquadSlot4,
        SquadSlot5,
        Select,
        Move,
        Attack,
        Hold,
        Stop,
        Build,
        Scan,
        Support,
        MinimapZoomIn,
        MinimapZoomOut,
        MinimapFocus,
        BoardAll,
        CancelFeedback,
        CloseBuildDrawer,
        BuildCatalogItem,
        BuildProductionRush,
        BuildProductionClear,
        BuildProductionCancelActive,
        BuildProductionCancelQueued,
        BuildDrawerPrimaryBuild,
        BuildDrawerTab,
        BuildPlacementConfirm,
        BuildPlacementCancel,
        BuildPlacementRotate,
        ToggleDiagnosticsOverlay,
        CloseDiagnosticsOverlay,
        OpenResourceExchange
    }

    public enum UiBuildProductionActionKind : byte
    {
        Rush,
        Clear,
        CancelActive,
        CancelQueued
    }

    public readonly struct UiBuildPlacementConfirmationBarModel
    {
        public readonly bool Visible;
        public readonly bool CanConfirm;
        public readonly bool CanCancel;
        public readonly bool CanRotate;
        public readonly string Title;
        public readonly string Status;
        public readonly string CostText;
        public readonly string DurationText;
        public readonly string InstructionText;

        public UiBuildPlacementConfirmationBarModel(
            bool visible,
            bool canConfirm,
            bool canCancel,
            bool canRotate,
            string title,
            string status,
            string costText,
            string durationText,
            string instructionText)
        {
            Visible = visible;
            CanConfirm = canConfirm;
            CanCancel = canCancel;
            CanRotate = canRotate;
            Title = title;
            Status = status;
            CostText = costText;
            DurationText = durationText;
            InstructionText = instructionText;
        }

        public static UiBuildPlacementConfirmationBarModel Hidden =>
            new(false, false, false, false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
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

    public readonly struct UiDiagnosticsOverlayModel
    {
        public readonly int Fps;
        public readonly bool LogVisible;
        public readonly string LogText;

        public UiDiagnosticsOverlayModel(int fps, bool logVisible, string logText)
        {
            Fps = fps;
            LogVisible = logVisible;
            LogText = logText;
        }

        public static UiDiagnosticsOverlayModel Default => new(0, false, string.Empty);
    }

    public readonly struct UiActionRequestModel
    {
        public readonly UiActionKind Kind;
        public readonly int PayloadId;

        public UiActionRequestModel(UiActionKind kind, int payloadId = 0)
        {
            Kind = kind;
            PayloadId = payloadId;
        }
    }

    public readonly struct UiShellCommanderProfileModel
    {
        public readonly string Name;
        public readonly string Subtitle;
        public readonly string PortraitClass;

        public UiShellCommanderProfileModel(string name, string subtitle, string portraitClass)
        {
            Name = name;
            Subtitle = subtitle;
            PortraitClass = portraitClass;
        }
    }

    public readonly struct UiShellMainMenuResourcesModel
    {
        public readonly string CreditsText;
        public readonly string SuppliesText;
        public readonly string CommandText;

        public UiShellMainMenuResourcesModel(string creditsText, string suppliesText, string commandText)
        {
            CreditsText = creditsText;
            SuppliesText = suppliesText;
            CommandText = commandText;
        }
    }

    public enum UiMissionResultOutcome : byte
    {
        Victory,
        Loss
    }

    public readonly struct UiMissionResultPopupModel
    {
        public readonly UiMissionResultOutcome Outcome;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string SummaryBody;
        public readonly bool ReplayEnabled;

        public UiMissionResultPopupModel(
            UiMissionResultOutcome outcome,
            string title,
            string subtitle,
            string summaryBody,
            bool replayEnabled)
        {
            Outcome = outcome;
            Title = title;
            Subtitle = subtitle;
            SummaryBody = summaryBody;
            ReplayEnabled = replayEnabled;
        }

        public static UiMissionResultPopupModel VictoryDefault =>
            new(
                UiMissionResultOutcome.Victory,
                "VICTORY",
                "Sector secured. Command net restored.",
                "Primary objectives completed with acceptable losses. Civilian risk stabilized and remaining hostile cells are retreating.",
                true);

        public static UiMissionResultPopupModel LossDefault =>
            new(
                UiMissionResultOutcome.Loss,
                "MISSION FAILED",
                "Command net disrupted. Extraction required.",
                "Primary objectives were not completed. Regroup, resupply, and redeploy when command authorizes a new operation.",
                true);
    }

    public readonly struct UiMatchHudSelectionPanelModel
    {
        public readonly bool Visible;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string CurrentOrder;
        public readonly string HealthText;
        public readonly float Health01;
        public readonly bool BadgeVisible;
        public readonly bool ReturnEnabled;
        public readonly bool DestroyEnabled;
        public readonly bool BoardEnabled;

        public UiMatchHudSelectionPanelModel(
            bool visible,
            string title,
            string subtitle,
            string currentOrder,
            string healthText,
            float health01,
            bool badgeVisible,
            bool returnEnabled,
            bool destroyEnabled,
            bool boardEnabled)
        {
            Visible = visible;
            Title = title;
            Subtitle = subtitle;
            CurrentOrder = currentOrder;
            HealthText = healthText;
            Health01 = health01;
            BadgeVisible = badgeVisible;
            ReturnEnabled = returnEnabled;
            DestroyEnabled = destroyEnabled;
            BoardEnabled = boardEnabled;
        }

        public static UiMatchHudSelectionPanelModel Hidden =>
            new(false, string.Empty, string.Empty, string.Empty, string.Empty, 0f, false, false, false, false);
    }

    public readonly struct UiMatchHudCommandStateModel
    {
        public readonly TacticalCommandMode ActiveCommandMode;
        public readonly bool BuildDrawerVisible;

        public UiMatchHudCommandStateModel(TacticalCommandMode activeCommandMode, bool buildDrawerVisible)
        {
            ActiveCommandMode = activeCommandMode;
            BuildDrawerVisible = buildDrawerVisible;
        }
    }

    public readonly struct UiBuildDrawerCatalogItemModel
    {
        public readonly bool Visible;
        public readonly bool Enabled;
        public readonly bool Selected;
        public readonly Sprite ThumbnailSprite;
        public readonly string Title;
        public readonly string Role;
        public readonly string CreditsText;
        public readonly string SuppliesText;
        public readonly string TimeText;

        public UiBuildDrawerCatalogItemModel(
            bool visible,
            bool enabled,
            string title,
            string role,
            string creditsText,
            string suppliesText,
            string timeText)
            : this(visible, enabled, false, null, title, role, creditsText, suppliesText, timeText)
        {
        }

        public UiBuildDrawerCatalogItemModel(
            bool visible,
            bool enabled,
            bool selected,
            string title,
            string role,
            string creditsText,
            string suppliesText,
            string timeText)
            : this(visible, enabled, selected, null, title, role, creditsText, suppliesText, timeText)
        {
        }

        public UiBuildDrawerCatalogItemModel(
            bool visible,
            bool enabled,
            bool selected,
            Sprite thumbnailSprite,
            string title,
            string role,
            string creditsText,
            string suppliesText,
            string timeText)
        {
            Visible = visible;
            Enabled = enabled;
            Selected = selected;
            ThumbnailSprite = thumbnailSprite;
            Title = title;
            Role = role;
            CreditsText = creditsText;
            SuppliesText = suppliesText;
            TimeText = timeText;
        }
    }

    public readonly struct UiBuildDrawerQueueRowModel
    {
        public readonly bool Visible;
        public readonly bool ActionEnabled;
        public readonly Sprite ThumbnailSprite;
        public readonly string NumberText;
        public readonly string Name;
        public readonly string TimeText;

        public UiBuildDrawerQueueRowModel(
            bool visible,
            bool actionEnabled,
            string numberText,
            string name,
            string timeText)
            : this(visible, actionEnabled, null, numberText, name, timeText)
        {
        }

        public UiBuildDrawerQueueRowModel(
            bool visible,
            bool actionEnabled,
            Sprite thumbnailSprite,
            string numberText,
            string name,
            string timeText)
        {
            Visible = visible;
            ActionEnabled = actionEnabled;
            ThumbnailSprite = thumbnailSprite;
            NumberText = numberText;
            Name = name;
            TimeText = timeText;
        }
    }

    public readonly struct UiBuildDrawerActiveProductionModel
    {
        public readonly bool Visible;
        public readonly bool CancelEnabled;
        public readonly Sprite ThumbnailSprite;
        public readonly string Name;
        public readonly string PercentText;
        public readonly float Progress01;

        public UiBuildDrawerActiveProductionModel(
            bool visible,
            bool cancelEnabled,
            string name,
            string percentText,
            float progress01)
            : this(visible, cancelEnabled, null, name, percentText, progress01)
        {
        }

        public UiBuildDrawerActiveProductionModel(
            bool visible,
            bool cancelEnabled,
            Sprite thumbnailSprite,
            string name,
            string percentText,
            float progress01)
        {
            Visible = visible;
            CancelEnabled = cancelEnabled;
            ThumbnailSprite = thumbnailSprite;
            Name = name;
            PercentText = percentText;
            Progress01 = progress01;
        }
    }

    public readonly struct UiBuildDrawerModel
    {
        public const int MaxCatalogItems = 7;
        public const int MaxQueueRows = 2;

        public readonly string Name;
        public readonly string Role;
        public readonly string Description;
        public readonly string FootprintText;
        public readonly string RequirementsText;
        public readonly string PlacementText;
        public readonly string ProductionTimeText;
        public readonly string CreditsCostText;
        public readonly string SuppliesCostText;
        public readonly string InstructionText;
        public readonly string ProductionTitle;
        public readonly string ProductionCountText;
        public readonly bool BuildEnabled;
        public readonly bool RushEnabled;
        public readonly bool ClearEnabled;
        public readonly bool NoProductionVisible;
        public readonly UiBuildDrawerActiveProductionModel ActiveProduction;
        public readonly Sprite PreviewSprite;
        public readonly BuildDrawerCategory ActiveCategory;
        public readonly int BuildingsCount;
        public readonly int VehiclesCount;
        public readonly int AircraftsCount;
        public readonly int SoldiersCount;
        public readonly int SelectedCatalogSlot;
        public readonly int CatalogItemCount;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem0;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem1;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem2;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem3;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem4;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem5;
        public readonly UiBuildDrawerCatalogItemModel CatalogItem6;
        public readonly int QueueRowCount;
        public readonly UiBuildDrawerQueueRowModel QueueRow0;
        public readonly UiBuildDrawerQueueRowModel QueueRow1;

        public UiBuildDrawerModel(
            string name,
            string role,
            string description,
            string footprintText,
            string requirementsText,
            string placementText,
            string productionTimeText,
            string creditsCostText,
            string suppliesCostText,
            string instructionText,
            string productionTitle,
            string productionCountText,
            bool buildEnabled,
            bool rushEnabled,
            bool clearEnabled,
            bool noProductionVisible,
            UiBuildDrawerActiveProductionModel activeProduction,
            int catalogItemCount,
            UiBuildDrawerCatalogItemModel catalogItem0,
            UiBuildDrawerCatalogItemModel catalogItem1,
            UiBuildDrawerCatalogItemModel catalogItem2,
            UiBuildDrawerCatalogItemModel catalogItem3,
            UiBuildDrawerCatalogItemModel catalogItem4,
            UiBuildDrawerCatalogItemModel catalogItem5,
            UiBuildDrawerCatalogItemModel catalogItem6,
            int queueRowCount,
            UiBuildDrawerQueueRowModel queueRow0,
            UiBuildDrawerQueueRowModel queueRow1)
            : this(
                name,
                role,
                description,
                footprintText,
                requirementsText,
                placementText,
                productionTimeText,
                creditsCostText,
                suppliesCostText,
                instructionText,
                productionTitle,
                productionCountText,
                buildEnabled,
                rushEnabled,
                clearEnabled,
                noProductionVisible,
                activeProduction,
                null,
                BuildDrawerCategory.Buildings,
                0,
                0,
                0,
                0,
                0,
                catalogItemCount,
                catalogItem0,
                catalogItem1,
                catalogItem2,
                catalogItem3,
                catalogItem4,
                catalogItem5,
                catalogItem6,
                queueRowCount,
                queueRow0,
                queueRow1)
        {
        }

        public UiBuildDrawerModel(
            string name,
            string role,
            string description,
            string footprintText,
            string requirementsText,
            string placementText,
            string productionTimeText,
            string creditsCostText,
            string suppliesCostText,
            string instructionText,
            string productionTitle,
            string productionCountText,
            bool buildEnabled,
            bool rushEnabled,
            bool clearEnabled,
            bool noProductionVisible,
            UiBuildDrawerActiveProductionModel activeProduction,
            BuildDrawerCategory activeCategory,
            int buildingsCount,
            int vehiclesCount,
            int aircraftsCount,
            int soldiersCount,
            int selectedCatalogSlot,
            int catalogItemCount,
            UiBuildDrawerCatalogItemModel catalogItem0,
            UiBuildDrawerCatalogItemModel catalogItem1,
            UiBuildDrawerCatalogItemModel catalogItem2,
            UiBuildDrawerCatalogItemModel catalogItem3,
            UiBuildDrawerCatalogItemModel catalogItem4,
            UiBuildDrawerCatalogItemModel catalogItem5,
            UiBuildDrawerCatalogItemModel catalogItem6,
            int queueRowCount,
            UiBuildDrawerQueueRowModel queueRow0,
            UiBuildDrawerQueueRowModel queueRow1)
            : this(
                name,
                role,
                description,
                footprintText,
                requirementsText,
                placementText,
                productionTimeText,
                creditsCostText,
                suppliesCostText,
                instructionText,
                productionTitle,
                productionCountText,
                buildEnabled,
                rushEnabled,
                clearEnabled,
                noProductionVisible,
                activeProduction,
                null,
                activeCategory,
                buildingsCount,
                vehiclesCount,
                aircraftsCount,
                soldiersCount,
                selectedCatalogSlot,
                catalogItemCount,
                catalogItem0,
                catalogItem1,
                catalogItem2,
                catalogItem3,
                catalogItem4,
                catalogItem5,
                catalogItem6,
                queueRowCount,
                queueRow0,
                queueRow1)
        {
        }

        public UiBuildDrawerModel(
            string name,
            string role,
            string description,
            string footprintText,
            string requirementsText,
            string placementText,
            string productionTimeText,
            string creditsCostText,
            string suppliesCostText,
            string instructionText,
            string productionTitle,
            string productionCountText,
            bool buildEnabled,
            bool rushEnabled,
            bool clearEnabled,
            bool noProductionVisible,
            UiBuildDrawerActiveProductionModel activeProduction,
            Sprite previewSprite,
            BuildDrawerCategory activeCategory,
            int buildingsCount,
            int vehiclesCount,
            int aircraftsCount,
            int soldiersCount,
            int selectedCatalogSlot,
            int catalogItemCount,
            UiBuildDrawerCatalogItemModel catalogItem0,
            UiBuildDrawerCatalogItemModel catalogItem1,
            UiBuildDrawerCatalogItemModel catalogItem2,
            UiBuildDrawerCatalogItemModel catalogItem3,
            UiBuildDrawerCatalogItemModel catalogItem4,
            UiBuildDrawerCatalogItemModel catalogItem5,
            UiBuildDrawerCatalogItemModel catalogItem6,
            int queueRowCount,
            UiBuildDrawerQueueRowModel queueRow0,
            UiBuildDrawerQueueRowModel queueRow1)
        {
            Name = name;
            Role = role;
            Description = description;
            FootprintText = footprintText;
            RequirementsText = requirementsText;
            PlacementText = placementText;
            ProductionTimeText = productionTimeText;
            CreditsCostText = creditsCostText;
            SuppliesCostText = suppliesCostText;
            InstructionText = instructionText;
            ProductionTitle = productionTitle;
            ProductionCountText = productionCountText;
            BuildEnabled = buildEnabled;
            RushEnabled = rushEnabled;
            ClearEnabled = clearEnabled;
            NoProductionVisible = noProductionVisible;
            ActiveProduction = activeProduction;
            PreviewSprite = previewSprite;
            ActiveCategory = activeCategory;
            BuildingsCount = buildingsCount;
            VehiclesCount = vehiclesCount;
            AircraftsCount = aircraftsCount;
            SoldiersCount = soldiersCount;
            SelectedCatalogSlot = selectedCatalogSlot;
            CatalogItemCount = catalogItemCount;
            CatalogItem0 = catalogItem0;
            CatalogItem1 = catalogItem1;
            CatalogItem2 = catalogItem2;
            CatalogItem3 = catalogItem3;
            CatalogItem4 = catalogItem4;
            CatalogItem5 = catalogItem5;
            CatalogItem6 = catalogItem6;
            QueueRowCount = queueRowCount;
            QueueRow0 = queueRow0;
            QueueRow1 = queueRow1;
        }

        public static UiBuildDrawerModel Empty =>
            new(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                false,
                false,
                true,
                default,
                0,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                0,
                default,
                default);

        public UiBuildDrawerCatalogItemModel GetCatalogItem(int index)
        {
            return index switch
            {
                0 => CatalogItem0,
                1 => CatalogItem1,
                2 => CatalogItem2,
                3 => CatalogItem3,
                4 => CatalogItem4,
                5 => CatalogItem5,
                6 => CatalogItem6,
                _ => default
            };
        }

        public UiBuildDrawerQueueRowModel GetQueueRow(int index)
        {
            return index switch
            {
                0 => QueueRow0,
                1 => QueueRow1,
                _ => default
            };
        }
    }

    public readonly struct UiMatchHudHeaderModel
    {
        public readonly string OrderText;
        public readonly string SquadText;
        public readonly string CreditsText;
        public readonly string OilText;
        public readonly string FuelText;
        public readonly string SupplyText;
        public readonly string CivilianRiskText;
        public readonly bool ShowOil;

        public UiMatchHudHeaderModel(
            string orderText,
            string squadText,
            string creditsText,
            string fuelText,
            string supplyText,
            string civilianRiskText,
            string oilText = "",
            bool showOil = false)
        {
            OrderText = orderText;
            SquadText = squadText;
            CreditsText = creditsText;
            OilText = oilText;
            FuelText = fuelText;
            SupplyText = supplyText;
            CivilianRiskText = civilianRiskText;
            ShowOil = showOil;
        }

        public static UiMatchHudHeaderModel Default =>
            new("MOVE ORDER", "RIFLE SQUAD", "187,540", "2,860", "92/120", "MED", "0");
    }

    public enum UiMatchHudObjectiveIconKind : byte
    {
        Unchecked,
        Checked,
        Star
    }

    public readonly struct UiMatchHudObjectiveRowModel
    {
        public readonly string Text;
        public readonly UiMatchHudObjectiveIconKind IconKind;

        public UiMatchHudObjectiveRowModel(string text, UiMatchHudObjectiveIconKind iconKind)
        {
            Text = text;
            IconKind = iconKind;
        }
    }

    public readonly struct UiMatchHudStatusSurfacesModel
    {
        public readonly string ObjectivesTitle;
        public readonly UiMatchHudObjectiveRowModel Objective0;
        public readonly UiMatchHudObjectiveRowModel Objective1;
        public readonly UiMatchHudObjectiveRowModel Objective2;
        public readonly string ElapsedText;
        public readonly bool ThreatVisible;
        public readonly string ThreatTitle;
        public readonly string ThreatSubtitle;
        public readonly bool JumpEnabled;
        public readonly bool FeedbackVisible;
        public readonly string FeedbackText;
        public readonly bool BoardAllVisible;
        public readonly bool BoardAllEnabled;
        public readonly bool CancelVisible;
        public readonly bool CancelEnabled;

        public UiMatchHudStatusSurfacesModel(
            string objectivesTitle,
            UiMatchHudObjectiveRowModel objective0,
            UiMatchHudObjectiveRowModel objective1,
            UiMatchHudObjectiveRowModel objective2,
            string elapsedText,
            bool threatVisible,
            string threatTitle,
            string threatSubtitle,
            bool jumpEnabled,
            bool feedbackVisible,
            string feedbackText,
            bool boardAllVisible,
            bool boardAllEnabled,
            bool cancelVisible,
            bool cancelEnabled)
        {
            ObjectivesTitle = objectivesTitle;
            Objective0 = objective0;
            Objective1 = objective1;
            Objective2 = objective2;
            ElapsedText = elapsedText;
            ThreatVisible = threatVisible;
            ThreatTitle = threatTitle;
            ThreatSubtitle = threatSubtitle;
            JumpEnabled = jumpEnabled;
            FeedbackVisible = feedbackVisible;
            FeedbackText = feedbackText;
            BoardAllVisible = boardAllVisible;
            BoardAllEnabled = boardAllEnabled;
            CancelVisible = cancelVisible;
            CancelEnabled = cancelEnabled;
        }

        public static UiMatchHudStatusSurfacesModel Default =>
            new(
                "OBJECTIVES",
                new UiMatchHudObjectiveRowModel("Neutralize hostile patrol", UiMatchHudObjectiveIconKind.Unchecked),
                new UiMatchHudObjectiveRowModel("Protect civilians", UiMatchHudObjectiveIconKind.Checked),
                new UiMatchHudObjectiveRowModel("Keep losses low", UiMatchHudObjectiveIconKind.Star),
                "ELAPSED: 07:42",
                true,
                "HOSTILE CELL SPOTTED",
                "Market quarter, 140m",
                true,
                true,
                "Blocked: civilian zone",
                true,
                true,
                true,
                true);
    }

    public readonly struct UiAssistantPanelModel
    {
        public readonly uint Version;
        public readonly string GoalsText;
        public readonly string AlertsText;
        public readonly string NarrationSubtitleText;
        public readonly bool NarrationSubtitlesVisible;
        public readonly bool HasAlerts;
        public readonly bool HasRecommendation;
        public readonly string RecommendationTitle;
        public readonly string RecommendationBody;
        public readonly string RecommendationPriorityText;
        public readonly string RecommendationActionLabel;
        public readonly bool CanShow;
        public readonly bool CanExecute;
        public readonly bool CanStop;
        public readonly bool CanTakeControl;
        public readonly string OwnershipText;
        public readonly string OwnershipDetailText;

        public UiAssistantPanelModel(
            uint version,
            string goalsText,
            string alertsText,
            string narrationSubtitleText,
            bool narrationSubtitlesVisible,
            bool hasAlerts,
            bool hasRecommendation,
            string recommendationTitle,
            string recommendationBody,
            string recommendationPriorityText,
            string recommendationActionLabel,
            bool canShow,
            bool canExecute,
            bool canStop,
            bool canTakeControl,
            string ownershipText,
            string ownershipDetailText)
        {
            Version = version;
            GoalsText = goalsText;
            AlertsText = alertsText;
            NarrationSubtitleText = narrationSubtitleText;
            NarrationSubtitlesVisible = narrationSubtitlesVisible;
            HasAlerts = hasAlerts;
            HasRecommendation = hasRecommendation;
            RecommendationTitle = recommendationTitle;
            RecommendationBody = recommendationBody;
            RecommendationPriorityText = recommendationPriorityText;
            RecommendationActionLabel = recommendationActionLabel;
            CanShow = canShow;
            CanExecute = canExecute;
            CanStop = canStop;
            CanTakeControl = canTakeControl;
            OwnershipText = ownershipText;
            OwnershipDetailText = ownershipDetailText;
        }

        public static UiAssistantPanelModel Empty =>
            new(
                0,
                "No active objectives",
                "No priority alerts",
                "No active narration",
                true,
                false,
                false,
                "No recommendation",
                "ARIA is waiting for live battlefield context.",
                "LOW",
                "SHOW ME",
                false,
                false,
                false,
                false,
                "PLAYER CONTROL",
                "You are issuing orders directly.");
    }

    public readonly struct UiAssistantHighlightModel
    {
        public readonly uint Version;
        public readonly bool Active;
        public readonly int RequestId;
        public readonly int RecommendationId;
        public readonly byte TargetKind;
        public readonly float WorldX;
        public readonly float WorldY;
        public readonly float WorldZ;
        public readonly float Strength;

        public UiAssistantHighlightModel(
            uint version,
            bool active,
            int requestId,
            int recommendationId,
            byte targetKind,
            float worldX,
            float worldY,
            float worldZ,
            float strength)
        {
            Version = version;
            Active = active;
            RequestId = requestId;
            RecommendationId = recommendationId;
            TargetKind = targetKind;
            WorldX = worldX;
            WorldY = worldY;
            WorldZ = worldZ;
            Strength = strength;
        }

        public static UiAssistantHighlightModel Empty => new(0, false, 0, 0, 0, 0f, 0f, 0f, 0f);
    }

    public readonly struct UiMatchHudMinimapMarkerModel
    {
        public readonly bool Visible;
        public readonly float LeftPercent;
        public readonly float TopPercent;

        public UiMatchHudMinimapMarkerModel(bool visible, float leftPercent, float topPercent)
        {
            Visible = visible;
            LeftPercent = leftPercent;
            TopPercent = topPercent;
        }
    }

    public readonly struct UiMatchHudMinimapModel
    {
        public readonly float ViewportLeftPercent;
        public readonly float ViewportTopPercent;
        public readonly float ViewportWidthPercent;
        public readonly float ViewportHeightPercent;
        public readonly bool ZoomInEnabled;
        public readonly bool ZoomOutEnabled;
        public readonly bool FocusEnabled;
        public readonly UiMatchHudMinimapMarkerModel FriendlyA;
        public readonly UiMatchHudMinimapMarkerModel FriendlyB;
        public readonly UiMatchHudMinimapMarkerModel HostileA;
        public readonly UiMatchHudMinimapMarkerModel Civilian;

        public UiMatchHudMinimapModel(
            float viewportLeftPercent,
            float viewportTopPercent,
            float viewportWidthPercent,
            float viewportHeightPercent,
            bool zoomInEnabled,
            bool zoomOutEnabled,
            bool focusEnabled,
            UiMatchHudMinimapMarkerModel friendlyA,
            UiMatchHudMinimapMarkerModel friendlyB,
            UiMatchHudMinimapMarkerModel hostileA,
            UiMatchHudMinimapMarkerModel civilian)
        {
            ViewportLeftPercent = viewportLeftPercent;
            ViewportTopPercent = viewportTopPercent;
            ViewportWidthPercent = viewportWidthPercent;
            ViewportHeightPercent = viewportHeightPercent;
            ZoomInEnabled = zoomInEnabled;
            ZoomOutEnabled = zoomOutEnabled;
            FocusEnabled = focusEnabled;
            FriendlyA = friendlyA;
            FriendlyB = friendlyB;
            HostileA = hostileA;
            Civilian = civilian;
        }

        public static UiMatchHudMinimapModel Default =>
            new(
                26f,
                34f,
                40f,
                34f,
                true,
                true,
                true,
                new UiMatchHudMinimapMarkerModel(true, 47f, 57f),
                new UiMatchHudMinimapMarkerModel(true, 58f, 63f),
                new UiMatchHudMinimapMarkerModel(true, 55f, 37f),
                new UiMatchHudMinimapMarkerModel(true, 75f, 52f));
    }

    public readonly struct UiMatchHudPassengerRowModel
    {
        public readonly string Name;
        public readonly string Role;
        public readonly string HealthText;
        public readonly float Health01;

        public UiMatchHudPassengerRowModel(string name, string role, string healthText, float health01)
        {
            Name = name;
            Role = role;
            HealthText = healthText;
            Health01 = health01;
        }
    }

    public readonly struct UiMatchHudPassengerDrawerModel
    {
        public const int MaxRows = 3;

        public readonly bool ChipVisible;
        public readonly bool DrawerVisible;
        public readonly int PassengerCount;
        public readonly int PassengerCapacity;
        public readonly int RowCount;
        public readonly UiMatchHudPassengerRowModel Row0;
        public readonly UiMatchHudPassengerRowModel Row1;
        public readonly UiMatchHudPassengerRowModel Row2;

        public UiMatchHudPassengerDrawerModel(
            bool chipVisible,
            bool drawerVisible,
            int passengerCount,
            int passengerCapacity,
            int rowCount,
            UiMatchHudPassengerRowModel row0,
            UiMatchHudPassengerRowModel row1,
            UiMatchHudPassengerRowModel row2)
        {
            ChipVisible = chipVisible;
            DrawerVisible = drawerVisible;
            PassengerCount = passengerCount;
            PassengerCapacity = passengerCapacity;
            RowCount = rowCount;
            Row0 = row0;
            Row1 = row1;
            Row2 = row2;
        }

        public static UiMatchHudPassengerDrawerModel Hidden =>
            new(false, false, 0, 0, 0, default, default, default);

        public UiMatchHudPassengerRowModel GetRow(int index)
        {
            return index switch
            {
                0 => Row0,
                1 => Row1,
                2 => Row2,
                _ => default
            };
        }
    }

    public readonly struct UiMatchHudSquadTrayCardModel
    {
        public readonly bool Visible;
        public readonly string Title;
        public readonly string HealthText;
        public readonly float Health01;

        public UiMatchHudSquadTrayCardModel(bool visible, string title, string healthText, float health01)
        {
            Visible = visible;
            Title = title;
            HealthText = healthText;
            Health01 = health01;
        }
    }

    public readonly struct UiMatchHudSquadTrayModel
    {
        public const int MaxCards = 5;

        public readonly MatchHudSquadTraySlot SelectedSlot;
        public readonly UiMatchHudSquadTrayCardModel Card0;
        public readonly UiMatchHudSquadTrayCardModel Card1;
        public readonly UiMatchHudSquadTrayCardModel Card2;
        public readonly UiMatchHudSquadTrayCardModel Card3;
        public readonly UiMatchHudSquadTrayCardModel Card4;

        public UiMatchHudSquadTrayModel(
            MatchHudSquadTraySlot selectedSlot,
            UiMatchHudSquadTrayCardModel card0,
            UiMatchHudSquadTrayCardModel card1,
            UiMatchHudSquadTrayCardModel card2,
            UiMatchHudSquadTrayCardModel card3,
            UiMatchHudSquadTrayCardModel card4)
        {
            SelectedSlot = selectedSlot;
            Card0 = card0;
            Card1 = card1;
            Card2 = card2;
            Card3 = card3;
            Card4 = card4;
        }

        public static UiMatchHudSquadTrayModel Default =>
            new(
                MatchHudSquadTraySlot.None,
                new UiMatchHudSquadTrayCardModel(true, "RIFLE SQUAD", "120/120", 1f),
                new UiMatchHudSquadTrayCardModel(true, "ARMOR", "240/240", 1f),
                new UiMatchHudSquadTrayCardModel(true, "GUNSHIP", "80/80", 1f),
                new UiMatchHudSquadTrayCardModel(true, "JET WING", "100/100", 1f),
                new UiMatchHudSquadTrayCardModel(true, "TRANSPORT", "0/0", 0f));

        public UiMatchHudSquadTrayCardModel GetCard(int index)
        {
            return index switch
            {
                0 => Card0,
                1 => Card1,
                2 => Card2,
                3 => Card3,
                4 => Card4,
                _ => default
            };
        }
    }

    public readonly struct UiShellPresentationCommandModel
    {
        public readonly UiShellCommandKind Kind;
        public readonly UiShellRegionId Region;
        public readonly UIRoute Route;
        public readonly UiShellMode TargetMode;
        public readonly UiShellPopupKind PopupKind;
        public readonly int SequenceId;

        public UiShellPresentationCommandModel(
            UiShellCommandKind kind,
            UiShellRegionId region,
            UIRoute route,
            UiShellMode targetMode,
            int sequenceId,
            UiShellPopupKind popupKind = UiShellPopupKind.ThreatAlert)
        {
            Kind = kind;
            Region = region;
            Route = route;
            TargetMode = targetMode;
            PopupKind = popupKind;
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
}
