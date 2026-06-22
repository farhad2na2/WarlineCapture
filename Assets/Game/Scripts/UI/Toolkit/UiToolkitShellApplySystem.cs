using System;
using System.Collections.Generic;
using Unity.Entities;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public sealed partial class UiToolkitShellApplySystem : SystemBase
{
    private readonly List<UiShellPresentationCommandModel> commandScratch = new();
    private UiToolkitShellView shellView;
    private UiShellStateModel lastShellState;
    private UiShellLoadingProgressModel lastLoadingProgress;
    private UiDiagnosticsOverlayModel lastDiagnosticsOverlay;
    private UiShellCommanderProfileModel lastCommanderProfile;
    private UiShellMainMenuResourcesModel lastMainMenuResources;
    private UiMissionResultPopupModel lastMissionResult;
    private UiMatchHudSelectionPanelModel lastMatchHudSelection;
    private UiMatchHudCommandStateModel lastMatchHudCommandState;
    private UiMatchHudHeaderModel lastMatchHudHeader;
    private UiMatchHudStatusSurfacesModel lastMatchHudStatusSurfaces;
    private UiMatchHudMinimapModel lastMatchHudMinimap;
    private UiMatchHudPassengerDrawerModel lastMatchHudPassengerDrawer;
    private UiMatchHudSquadTrayModel lastMatchHudSquadTray;
    private UiBuildDrawerModel lastBuildDrawer;
    private UiBuildPlacementConfirmationBarModel lastBuildPlacementConfirmationBar;
    private ArmoryCatalogCategory lastArmoryCategory;
    private UiShellStateModel appliedShellState;
    private UiShellLoadingProgressModel appliedLoadingProgress;
    private UiDiagnosticsOverlayModel appliedDiagnosticsOverlay;
    private UiShellCommanderProfileModel appliedCommanderProfile;
    private UiShellMainMenuResourcesModel appliedMainMenuResources;
    private UiMissionResultPopupModel appliedMissionResult;
    private UiMatchHudSelectionPanelModel appliedMatchHudSelection;
    private UiMatchHudCommandStateModel appliedMatchHudCommandState;
    private UiMatchHudHeaderModel appliedMatchHudHeader;
    private UiMatchHudStatusSurfacesModel appliedMatchHudStatusSurfaces;
    private UiMatchHudMinimapModel appliedMatchHudMinimap;
    private UiMatchHudPassengerDrawerModel appliedMatchHudPassengerDrawer;
    private UiMatchHudSquadTrayModel appliedMatchHudSquadTray;
    private UiBuildDrawerModel appliedBuildDrawer;
    private UiBuildPlacementConfirmationBarModel appliedBuildPlacementConfirmationBar;
    private ArmoryCatalogCategory appliedArmoryCategory;
    private UiShellTransitionCompleteModel pendingCompletion;
    private bool hasShellState;
    private bool hasLoadingProgress;
    private bool hasDiagnosticsOverlay;
    private bool hasCommanderProfile;
    private bool hasMainMenuResources;
    private bool hasMissionResult;
    private bool hasMatchHudSelection;
    private bool hasMatchHudCommandState;
    private bool hasMatchHudHeader;
    private bool hasMatchHudStatusSurfaces;
    private bool hasMatchHudMinimap;
    private bool hasMatchHudPassengerDrawer;
    private bool hasMatchHudSquadTray;
    private bool hasBuildDrawer;
    private bool hasBuildPlacementConfirmationBar;
    private bool hasArmoryCategory;
    private bool hasAppliedShellState;
    private bool hasAppliedLoadingProgress;
    private bool hasAppliedDiagnosticsOverlay;
    private bool hasAppliedCommanderProfile;
    private bool hasAppliedMainMenuResources;
    private bool hasAppliedMissionResult;
    private bool hasAppliedMatchHudSelection;
    private bool hasAppliedMatchHudCommandState;
    private bool hasAppliedMatchHudHeader;
    private bool hasAppliedMatchHudStatusSurfaces;
    private bool hasAppliedMatchHudMinimap;
    private bool hasAppliedMatchHudPassengerDrawer;
    private bool hasAppliedMatchHudSquadTray;
    private bool hasAppliedBuildDrawer;
    private bool hasAppliedBuildPlacementConfirmationBar;
    private bool hasAppliedArmoryCategory;
    private bool hasPendingCompletion;
    private bool isExecuting;

    public UiToolkitShellView ShellView => shellView;
    public bool HasShellView => shellView != null;
    public bool HasMountedShellView => shellView != null && shellView.IsMounted;
    public bool HasShellState => hasShellState;
    public bool HasLoadingProgress => hasLoadingProgress;
    public bool HasDiagnosticsOverlay => hasDiagnosticsOverlay;
    public bool HasCommanderProfile => hasCommanderProfile;
    public bool HasMainMenuResources => hasMainMenuResources;
    public bool HasMissionResult => hasMissionResult;
    public bool HasMatchHudSelection => hasMatchHudSelection;
    public bool HasMatchHudCommandState => hasMatchHudCommandState;
    public bool HasMatchHudHeader => hasMatchHudHeader;
    public bool HasMatchHudStatusSurfaces => hasMatchHudStatusSurfaces;
    public bool HasMatchHudMinimap => hasMatchHudMinimap;
    public bool HasMatchHudPassengerDrawer => hasMatchHudPassengerDrawer;
    public bool HasMatchHudSquadTray => hasMatchHudSquadTray;
    public bool HasBuildDrawer => hasBuildDrawer;
    public bool HasBuildPlacementConfirmationBar => hasBuildPlacementConfirmationBar;
    public bool HasArmoryCategory => hasArmoryCategory;
    public UiShellStateModel LastShellState => lastShellState;
    public UiShellLoadingProgressModel LastLoadingProgress => lastLoadingProgress;
    public UiDiagnosticsOverlayModel LastDiagnosticsOverlay => lastDiagnosticsOverlay;
    public UiShellCommanderProfileModel LastCommanderProfile => lastCommanderProfile;
    public UiShellMainMenuResourcesModel LastMainMenuResources => lastMainMenuResources;
    public UiMissionResultPopupModel LastMissionResult => lastMissionResult;
    public UiMatchHudSelectionPanelModel LastMatchHudSelection => lastMatchHudSelection;
    public UiMatchHudCommandStateModel LastMatchHudCommandState => lastMatchHudCommandState;
    public UiMatchHudHeaderModel LastMatchHudHeader => lastMatchHudHeader;
    public UiMatchHudStatusSurfacesModel LastMatchHudStatusSurfaces => lastMatchHudStatusSurfaces;
    public UiMatchHudMinimapModel LastMatchHudMinimap => lastMatchHudMinimap;
    public UiMatchHudPassengerDrawerModel LastMatchHudPassengerDrawer => lastMatchHudPassengerDrawer;
    public UiMatchHudSquadTrayModel LastMatchHudSquadTray => lastMatchHudSquadTray;
    public UiBuildDrawerModel LastBuildDrawer => lastBuildDrawer;
    public UiBuildPlacementConfirmationBarModel LastBuildPlacementConfirmationBar => lastBuildPlacementConfirmationBar;
    public ArmoryCatalogCategory LastArmoryCategory => lastArmoryCategory;

    public void ConfigureShellView(UiToolkitShellView view)
    {
        if (!ReferenceEquals(shellView, view))
            ResetAppliedPresentationCache();

        shellView = view;
        if (shellView != null && !shellView.IsMounted)
            shellView.Mount();
    }

    public void ClearShellView(UiToolkitShellView view = null)
    {
        if (view == null || ReferenceEquals(shellView, view))
        {
            shellView = null;
            ResetAppliedPresentationCache();
        }
    }

    protected override void OnUpdate()
    {
        if (shellView == null)
            return;

        hasShellState = UiShellRuntimeGateway.TryReadShellState(out lastShellState);
        hasLoadingProgress = UiShellRuntimeGateway.TryReadLoadingProgress(out lastLoadingProgress);
        hasDiagnosticsOverlay = UiShellRuntimeGateway.TryReadDiagnosticsOverlay(out lastDiagnosticsOverlay);
        hasCommanderProfile = false;
        hasMainMenuResources = false;
        hasMissionResult = false;
        hasMatchHudSelection = false;
        hasMatchHudCommandState = false;
        hasMatchHudHeader = false;
        hasMatchHudStatusSurfaces = false;
        hasMatchHudMinimap = false;
        hasMatchHudPassengerDrawer = false;
        hasMatchHudSquadTray = false;
        hasBuildDrawer = false;
        hasBuildPlacementConfirmationBar = false;
        hasArmoryCategory = false;

        bool shellMounted = shellView != null && shellView.IsMounted;

        if (shellMounted &&
            shellView.HasMountedLoadingScreen &&
            hasLoadingProgress &&
            ShouldApplyLoadingProgress(lastLoadingProgress))
            MarkLoadingProgressApplied(shellView.ApplyLoadingProgress(lastLoadingProgress));

        if (shellMounted && hasDiagnosticsOverlay && ShouldApplyDiagnosticsOverlay(lastDiagnosticsOverlay))
            MarkDiagnosticsOverlayApplied(shellView.ApplyDiagnosticsOverlay(lastDiagnosticsOverlay));

        if (shellMounted && hasMissionResult && ShouldApplyMissionResult(lastMissionResult))
            MarkMissionResultApplied(shellView.ApplyMissionResult(lastMissionResult));

        if (shellMounted &&
            hasShellState &&
            lastShellState.CurrentMode == UiShellMode.MainMenu)
        {
            hasCommanderProfile = UiShellRuntimeGateway.TryReadCommanderProfile(out lastCommanderProfile);
            hasMainMenuResources = UiShellRuntimeGateway.TryReadMainMenuResources(out lastMainMenuResources);

            if (ShouldApplyShellState(lastShellState))
            {
                if (shellView.EnsureMainMenuVisible(lastShellState.ActiveRoute))
                {
                    MarkShellStateApplied();
                    hasAppliedCommanderProfile = false;
                    hasAppliedArmoryCategory = false;
                }
            }

            if (hasCommanderProfile && ShouldApplyCommanderProfile(lastCommanderProfile))
            {
                shellView.ApplyMainMenuCommanderProfile(lastCommanderProfile);
                shellView.ApplyCommanderProfile(lastCommanderProfile);
                MarkCommanderProfileApplied();
            }

            if (hasMainMenuResources && ShouldApplyMainMenuResources(lastMainMenuResources))
                MarkMainMenuResourcesApplied(shellView.ApplyMainMenuResources(lastMainMenuResources));

            hasArmoryCategory = lastShellState.ActiveRoute == UIRoute.Armory &&
                UiShellRuntimeGateway.TryReadArmoryCategory(out lastArmoryCategory);
            if (hasArmoryCategory && ShouldApplyArmoryCategory(lastArmoryCategory))
            {
                MarkArmoryCategoryApplied(shellView.ApplyArmoryCategory(lastArmoryCategory));
            }
        }

        if (shellMounted &&
            hasShellState &&
            lastShellState.CurrentMode == UiShellMode.MatchHud)
        {
            hasMatchHudSelection = UiShellRuntimeGateway.TryReadMatchHudSelection(out lastMatchHudSelection);
            hasMatchHudCommandState = UiShellRuntimeGateway.TryReadMatchHudCommandState(out lastMatchHudCommandState);
            hasMatchHudHeader = UiShellRuntimeGateway.TryReadMatchHudHeader(out lastMatchHudHeader);
            hasMatchHudStatusSurfaces =
                UiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out lastMatchHudStatusSurfaces);
            hasMatchHudMinimap = UiShellRuntimeGateway.TryReadMatchHudMinimap(out lastMatchHudMinimap);
            hasMatchHudPassengerDrawer =
                UiShellRuntimeGateway.TryReadMatchHudPassengerDrawer(out lastMatchHudPassengerDrawer);
            hasMatchHudSquadTray = UiShellRuntimeGateway.TryReadMatchHudSquadTray(out lastMatchHudSquadTray);

            if (ShouldApplyShellState(lastShellState))
            {
                shellView.MountMatchHudScreen();
                if (shellView.HasMountedMatchHudScreen)
                {
                    MarkShellStateApplied();
                    hasAppliedMatchHudSelection = false;
                    hasAppliedMatchHudCommandState = false;
                    hasAppliedMatchHudHeader = false;
                    hasAppliedMatchHudStatusSurfaces = false;
                    hasAppliedMatchHudMinimap = false;
                    hasAppliedMatchHudPassengerDrawer = false;
                    hasAppliedMatchHudSquadTray = false;
                    hasAppliedBuildPlacementConfirmationBar = false;
                }
            }

            if (hasMatchHudSelection && ShouldApplyMatchHudSelection(lastMatchHudSelection))
                MarkMatchHudSelectionApplied(shellView.ApplyMatchHudSelection(lastMatchHudSelection));
            if (hasMatchHudCommandState && ShouldApplyMatchHudCommandState(lastMatchHudCommandState))
                MarkMatchHudCommandStateApplied(shellView.ApplyMatchHudCommandState(lastMatchHudCommandState));
            if (hasMatchHudHeader && ShouldApplyMatchHudHeader(lastMatchHudHeader))
                MarkMatchHudHeaderApplied(shellView.ApplyMatchHudHeader(lastMatchHudHeader));
            if (hasMatchHudStatusSurfaces && ShouldApplyMatchHudStatusSurfaces(lastMatchHudStatusSurfaces))
                MarkMatchHudStatusSurfacesApplied(shellView.ApplyMatchHudStatusSurfaces(lastMatchHudStatusSurfaces));
            if (hasMatchHudMinimap && ShouldApplyMatchHudMinimap(lastMatchHudMinimap))
                MarkMatchHudMinimapApplied(shellView.ApplyMatchHudMinimap(lastMatchHudMinimap));
            if (hasMatchHudPassengerDrawer && ShouldApplyMatchHudPassengerDrawer(lastMatchHudPassengerDrawer))
                MarkMatchHudPassengerDrawerApplied(shellView.ApplyMatchHudPassengerDrawer(lastMatchHudPassengerDrawer));
            if (hasMatchHudSquadTray && ShouldApplyMatchHudSquadTray(lastMatchHudSquadTray))
                MarkMatchHudSquadTrayApplied(shellView.ApplyMatchHudSquadTray(lastMatchHudSquadTray));
            bool buildDrawerVisible = hasMatchHudCommandState && lastMatchHudCommandState.BuildDrawerVisible;
            hasBuildDrawer =
                buildDrawerVisible &&
                shellView.HasMountedBuildDrawerPopup &&
                UiShellRuntimeGateway.TryReadBuildDrawer(out lastBuildDrawer);
            if (hasBuildDrawer && ShouldApplyBuildDrawer(lastBuildDrawer))
                MarkBuildDrawerApplied(shellView.ApplyBuildDrawer(lastBuildDrawer));
            hasBuildPlacementConfirmationBar =
                UiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar(out lastBuildPlacementConfirmationBar);
            if (hasBuildPlacementConfirmationBar && ShouldApplyBuildPlacementConfirmationBar(lastBuildPlacementConfirmationBar))
            {
                bool applied = shellView.ApplyBuildPlacementConfirmationBar(lastBuildPlacementConfirmationBar);
                MarkBuildPlacementConfirmationBarApplied(applied || !lastBuildPlacementConfirmationBar.Visible);
            }
        }

        FlushPendingCompletion();

        if (isExecuting)
            return;
        if (shellView == null || !shellView.IsMounted)
            return;

        if (!UiShellRuntimeGateway.TryConsumePresentationCommands(commandScratch))
            return;

        UiShellPresentationCommandModel finalCommand = commandScratch[commandScratch.Count - 1];
        isExecuting = true;

        if (shellView != null && shellView.IsMounted)
            shellView.ApplyPresentationCommands(commandScratch);

        pendingCompletion = new UiShellTransitionCompleteModel(
            finalCommand.Kind,
            finalCommand.Region,
            finalCommand.SequenceId);
        hasPendingCompletion = true;
        isExecuting = false;
    }

    private void ResetAppliedPresentationCache()
    {
        hasAppliedShellState = false;
        hasAppliedLoadingProgress = false;
        hasAppliedDiagnosticsOverlay = false;
        hasAppliedCommanderProfile = false;
        hasAppliedMainMenuResources = false;
        hasAppliedMissionResult = false;
        hasAppliedMatchHudSelection = false;
        hasAppliedMatchHudCommandState = false;
        hasAppliedMatchHudHeader = false;
        hasAppliedMatchHudStatusSurfaces = false;
        hasAppliedMatchHudMinimap = false;
        hasAppliedMatchHudPassengerDrawer = false;
        hasAppliedMatchHudSquadTray = false;
        hasAppliedBuildDrawer = false;
        hasAppliedBuildPlacementConfirmationBar = false;
        hasAppliedArmoryCategory = false;
    }

    private bool ShouldApplyShellState(UiShellStateModel model) =>
        !hasAppliedShellState || !Same(appliedShellState, model);

    private bool ShouldApplyLoadingProgress(UiShellLoadingProgressModel model) =>
        !hasAppliedLoadingProgress || !Same(appliedLoadingProgress, model);

    private bool ShouldApplyDiagnosticsOverlay(UiDiagnosticsOverlayModel model)
    {
        if (!hasAppliedDiagnosticsOverlay)
            return true;

        if (appliedDiagnosticsOverlay.LogVisible != model.LogVisible)
            return true;

        return model.LogVisible && !Same(appliedDiagnosticsOverlay, model);
    }

    private bool ShouldApplyCommanderProfile(UiShellCommanderProfileModel model) =>
        !hasAppliedCommanderProfile || !Same(appliedCommanderProfile, model);

    private bool ShouldApplyMainMenuResources(UiShellMainMenuResourcesModel model) =>
        !hasAppliedMainMenuResources || !Same(appliedMainMenuResources, model);

    private bool ShouldApplyMissionResult(UiMissionResultPopupModel model) =>
        !hasAppliedMissionResult || !Same(appliedMissionResult, model);

    private bool ShouldApplyMatchHudSelection(UiMatchHudSelectionPanelModel model) =>
        !hasAppliedMatchHudSelection || !Same(appliedMatchHudSelection, model);

    private bool ShouldApplyMatchHudCommandState(UiMatchHudCommandStateModel model) =>
        !hasAppliedMatchHudCommandState || !Same(appliedMatchHudCommandState, model);

    private bool ShouldApplyMatchHudHeader(UiMatchHudHeaderModel model) =>
        !hasAppliedMatchHudHeader || !Same(appliedMatchHudHeader, model);

    private bool ShouldApplyMatchHudStatusSurfaces(UiMatchHudStatusSurfacesModel model) =>
        !hasAppliedMatchHudStatusSurfaces || !Same(appliedMatchHudStatusSurfaces, model);

    private bool ShouldApplyMatchHudMinimap(UiMatchHudMinimapModel model) =>
        !hasAppliedMatchHudMinimap || !Same(appliedMatchHudMinimap, model);

    private bool ShouldApplyMatchHudPassengerDrawer(UiMatchHudPassengerDrawerModel model) =>
        !hasAppliedMatchHudPassengerDrawer || !Same(appliedMatchHudPassengerDrawer, model);

    private bool ShouldApplyMatchHudSquadTray(UiMatchHudSquadTrayModel model) =>
        !hasAppliedMatchHudSquadTray || !Same(appliedMatchHudSquadTray, model);

    private bool ShouldApplyBuildDrawer(UiBuildDrawerModel model) =>
        !hasAppliedBuildDrawer || !Same(appliedBuildDrawer, model);

    private bool ShouldApplyBuildPlacementConfirmationBar(UiBuildPlacementConfirmationBarModel model) =>
        !hasAppliedBuildPlacementConfirmationBar || !Same(appliedBuildPlacementConfirmationBar, model);

    private bool ShouldApplyArmoryCategory(ArmoryCatalogCategory category) =>
        !hasAppliedArmoryCategory || appliedArmoryCategory != category;

    private void MarkShellStateApplied()
    {
        appliedShellState = lastShellState;
        hasAppliedShellState = true;
    }

    private void MarkLoadingProgressApplied(bool applied)
    {
        if (!applied)
            return;

        appliedLoadingProgress = lastLoadingProgress;
        hasAppliedLoadingProgress = true;
    }

    private void MarkDiagnosticsOverlayApplied(bool applied)
    {
        if (!applied)
            return;

        appliedDiagnosticsOverlay = lastDiagnosticsOverlay;
        hasAppliedDiagnosticsOverlay = true;
    }

    private void MarkCommanderProfileApplied()
    {
        appliedCommanderProfile = lastCommanderProfile;
        hasAppliedCommanderProfile = true;
    }

    private void MarkMainMenuResourcesApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMainMenuResources = lastMainMenuResources;
        hasAppliedMainMenuResources = true;
    }

    private void MarkMissionResultApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMissionResult = lastMissionResult;
        hasAppliedMissionResult = true;
    }

    private void MarkMatchHudSelectionApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudSelection = lastMatchHudSelection;
        hasAppliedMatchHudSelection = true;
    }

    private void MarkMatchHudCommandStateApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudCommandState = lastMatchHudCommandState;
        hasAppliedMatchHudCommandState = true;
    }

    private void MarkMatchHudHeaderApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudHeader = lastMatchHudHeader;
        hasAppliedMatchHudHeader = true;
    }

    private void MarkMatchHudStatusSurfacesApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudStatusSurfaces = lastMatchHudStatusSurfaces;
        hasAppliedMatchHudStatusSurfaces = true;
    }

    private void MarkMatchHudMinimapApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudMinimap = lastMatchHudMinimap;
        hasAppliedMatchHudMinimap = true;
    }

    private void MarkMatchHudPassengerDrawerApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudPassengerDrawer = lastMatchHudPassengerDrawer;
        hasAppliedMatchHudPassengerDrawer = true;
    }

    private void MarkMatchHudSquadTrayApplied(bool applied)
    {
        if (!applied)
            return;

        appliedMatchHudSquadTray = lastMatchHudSquadTray;
        hasAppliedMatchHudSquadTray = true;
    }

    private void MarkBuildDrawerApplied(bool applied)
    {
        if (!applied)
            return;

        appliedBuildDrawer = lastBuildDrawer;
        hasAppliedBuildDrawer = true;
    }

    private void MarkBuildPlacementConfirmationBarApplied(bool applied)
    {
        if (!applied)
            return;

        appliedBuildPlacementConfirmationBar = lastBuildPlacementConfirmationBar;
        hasAppliedBuildPlacementConfirmationBar = true;
    }

    private void MarkArmoryCategoryApplied(bool applied)
    {
        if (!applied)
            return;

        appliedArmoryCategory = lastArmoryCategory;
        hasAppliedArmoryCategory = true;
    }

    private static bool Same(UiShellStateModel a, UiShellStateModel b) =>
        a.CurrentMode == b.CurrentMode &&
        a.ActiveRoute == b.ActiveRoute &&
        a.Phase == b.Phase &&
        a.TransitionSequenceId == b.TransitionSequenceId &&
        a.IsTransitionRunning == b.IsTransitionRunning;

    private static bool Same(UiShellLoadingProgressModel a, UiShellLoadingProgressModel b) =>
        a.Progress01 == b.Progress01 &&
        SameText(a.Status, b.Status) &&
        a.IsComplete == b.IsComplete;

    private static bool Same(UiDiagnosticsOverlayModel a, UiDiagnosticsOverlayModel b) =>
        a.Fps == b.Fps &&
        a.LogVisible == b.LogVisible &&
        SameText(a.LogText, b.LogText);

    private static bool Same(UiShellCommanderProfileModel a, UiShellCommanderProfileModel b) =>
        SameText(a.Name, b.Name) &&
        SameText(a.Subtitle, b.Subtitle) &&
        SameText(a.PortraitClass, b.PortraitClass);

    private static bool Same(UiShellMainMenuResourcesModel a, UiShellMainMenuResourcesModel b) =>
        SameText(a.CreditsText, b.CreditsText) &&
        SameText(a.SuppliesText, b.SuppliesText) &&
        SameText(a.CommandText, b.CommandText);

    private static bool Same(UiMissionResultPopupModel a, UiMissionResultPopupModel b) =>
        a.Outcome == b.Outcome &&
        SameText(a.Title, b.Title) &&
        SameText(a.Subtitle, b.Subtitle) &&
        SameText(a.SummaryBody, b.SummaryBody) &&
        a.ReplayEnabled == b.ReplayEnabled;

    private static bool Same(UiMatchHudSelectionPanelModel a, UiMatchHudSelectionPanelModel b) =>
        a.Visible == b.Visible &&
        SameText(a.Title, b.Title) &&
        SameText(a.Subtitle, b.Subtitle) &&
        SameText(a.CurrentOrder, b.CurrentOrder) &&
        SameText(a.HealthText, b.HealthText) &&
        a.Health01 == b.Health01 &&
        a.BadgeVisible == b.BadgeVisible &&
        a.ReturnEnabled == b.ReturnEnabled &&
        a.DestroyEnabled == b.DestroyEnabled &&
        a.BoardEnabled == b.BoardEnabled;

    private static bool Same(UiMatchHudCommandStateModel a, UiMatchHudCommandStateModel b) =>
        a.ActiveCommandMode == b.ActiveCommandMode &&
        a.BuildDrawerVisible == b.BuildDrawerVisible;

    private static bool Same(UiMatchHudHeaderModel a, UiMatchHudHeaderModel b) =>
        SameText(a.OrderText, b.OrderText) &&
        SameText(a.SquadText, b.SquadText) &&
        SameText(a.CreditsText, b.CreditsText) &&
        SameText(a.FuelText, b.FuelText) &&
        SameText(a.SupplyText, b.SupplyText) &&
        SameText(a.CivilianRiskText, b.CivilianRiskText);

    private static bool Same(UiMatchHudObjectiveRowModel a, UiMatchHudObjectiveRowModel b) =>
        SameText(a.Text, b.Text) &&
        a.IconKind == b.IconKind;

    private static bool Same(UiMatchHudStatusSurfacesModel a, UiMatchHudStatusSurfacesModel b) =>
        SameText(a.ObjectivesTitle, b.ObjectivesTitle) &&
        Same(a.Objective0, b.Objective0) &&
        Same(a.Objective1, b.Objective1) &&
        Same(a.Objective2, b.Objective2) &&
        SameText(a.ElapsedText, b.ElapsedText) &&
        a.ThreatVisible == b.ThreatVisible &&
        SameText(a.ThreatTitle, b.ThreatTitle) &&
        SameText(a.ThreatSubtitle, b.ThreatSubtitle) &&
        a.JumpEnabled == b.JumpEnabled &&
        a.FeedbackVisible == b.FeedbackVisible &&
        SameText(a.FeedbackText, b.FeedbackText) &&
        a.BoardAllVisible == b.BoardAllVisible &&
        a.BoardAllEnabled == b.BoardAllEnabled &&
        a.CancelVisible == b.CancelVisible &&
        a.CancelEnabled == b.CancelEnabled;

    private static bool Same(UiMatchHudMinimapMarkerModel a, UiMatchHudMinimapMarkerModel b) =>
        a.Visible == b.Visible &&
        a.LeftPercent == b.LeftPercent &&
        a.TopPercent == b.TopPercent;

    private static bool Same(UiMatchHudMinimapModel a, UiMatchHudMinimapModel b) =>
        a.ViewportLeftPercent == b.ViewportLeftPercent &&
        a.ViewportTopPercent == b.ViewportTopPercent &&
        a.ViewportWidthPercent == b.ViewportWidthPercent &&
        a.ViewportHeightPercent == b.ViewportHeightPercent &&
        a.ZoomInEnabled == b.ZoomInEnabled &&
        a.ZoomOutEnabled == b.ZoomOutEnabled &&
        a.FocusEnabled == b.FocusEnabled &&
        Same(a.FriendlyA, b.FriendlyA) &&
        Same(a.FriendlyB, b.FriendlyB) &&
        Same(a.HostileA, b.HostileA) &&
        Same(a.Civilian, b.Civilian);

    private static bool Same(UiMatchHudPassengerRowModel a, UiMatchHudPassengerRowModel b) =>
        SameText(a.Name, b.Name) &&
        SameText(a.Role, b.Role) &&
        SameText(a.HealthText, b.HealthText) &&
        a.Health01 == b.Health01;

    private static bool Same(UiMatchHudPassengerDrawerModel a, UiMatchHudPassengerDrawerModel b) =>
        a.ChipVisible == b.ChipVisible &&
        a.DrawerVisible == b.DrawerVisible &&
        a.PassengerCount == b.PassengerCount &&
        a.PassengerCapacity == b.PassengerCapacity &&
        a.RowCount == b.RowCount &&
        Same(a.Row0, b.Row0) &&
        Same(a.Row1, b.Row1) &&
        Same(a.Row2, b.Row2);

    private static bool Same(UiMatchHudSquadTrayCardModel a, UiMatchHudSquadTrayCardModel b) =>
        a.Visible == b.Visible &&
        SameText(a.Title, b.Title) &&
        SameText(a.HealthText, b.HealthText) &&
        a.Health01 == b.Health01;

    private static bool Same(UiMatchHudSquadTrayModel a, UiMatchHudSquadTrayModel b) =>
        a.SelectedSlot == b.SelectedSlot &&
        Same(a.Card0, b.Card0) &&
        Same(a.Card1, b.Card1) &&
        Same(a.Card2, b.Card2) &&
        Same(a.Card3, b.Card3) &&
        Same(a.Card4, b.Card4);

    private static bool Same(UiBuildDrawerActiveProductionModel a, UiBuildDrawerActiveProductionModel b) =>
        a.Visible == b.Visible &&
        a.CancelEnabled == b.CancelEnabled &&
        a.ThumbnailSprite == b.ThumbnailSprite &&
        SameText(a.Name, b.Name) &&
        SameText(a.PercentText, b.PercentText) &&
        a.Progress01 == b.Progress01;

    private static bool Same(UiBuildDrawerCatalogItemModel a, UiBuildDrawerCatalogItemModel b) =>
        a.Visible == b.Visible &&
        a.Enabled == b.Enabled &&
        a.Selected == b.Selected &&
        a.ThumbnailSprite == b.ThumbnailSprite &&
        SameText(a.Title, b.Title) &&
        SameText(a.Role, b.Role) &&
        SameText(a.CreditsText, b.CreditsText) &&
        SameText(a.SuppliesText, b.SuppliesText) &&
        SameText(a.TimeText, b.TimeText);

    private static bool Same(UiBuildDrawerQueueRowModel a, UiBuildDrawerQueueRowModel b) =>
        a.Visible == b.Visible &&
        a.ActionEnabled == b.ActionEnabled &&
        a.ThumbnailSprite == b.ThumbnailSprite &&
        SameText(a.NumberText, b.NumberText) &&
        SameText(a.Name, b.Name) &&
        SameText(a.TimeText, b.TimeText);

    private static bool Same(UiBuildDrawerModel a, UiBuildDrawerModel b) =>
        SameText(a.Name, b.Name) &&
        SameText(a.Role, b.Role) &&
        SameText(a.Description, b.Description) &&
        SameText(a.FootprintText, b.FootprintText) &&
        SameText(a.RequirementsText, b.RequirementsText) &&
        SameText(a.PlacementText, b.PlacementText) &&
        SameText(a.ProductionTimeText, b.ProductionTimeText) &&
        SameText(a.CreditsCostText, b.CreditsCostText) &&
        SameText(a.SuppliesCostText, b.SuppliesCostText) &&
        SameText(a.InstructionText, b.InstructionText) &&
        SameText(a.ProductionTitle, b.ProductionTitle) &&
        SameText(a.ProductionCountText, b.ProductionCountText) &&
        a.BuildEnabled == b.BuildEnabled &&
        a.RushEnabled == b.RushEnabled &&
        a.ClearEnabled == b.ClearEnabled &&
        a.NoProductionVisible == b.NoProductionVisible &&
        Same(a.ActiveProduction, b.ActiveProduction) &&
        a.PreviewSprite == b.PreviewSprite &&
        a.ActiveCategory == b.ActiveCategory &&
        a.BuildingsCount == b.BuildingsCount &&
        a.VehiclesCount == b.VehiclesCount &&
        a.AircraftsCount == b.AircraftsCount &&
        a.SoldiersCount == b.SoldiersCount &&
        a.SelectedCatalogSlot == b.SelectedCatalogSlot &&
        a.CatalogItemCount == b.CatalogItemCount &&
        Same(a.CatalogItem0, b.CatalogItem0) &&
        Same(a.CatalogItem1, b.CatalogItem1) &&
        Same(a.CatalogItem2, b.CatalogItem2) &&
        Same(a.CatalogItem3, b.CatalogItem3) &&
        Same(a.CatalogItem4, b.CatalogItem4) &&
        Same(a.CatalogItem5, b.CatalogItem5) &&
        Same(a.CatalogItem6, b.CatalogItem6) &&
        a.QueueRowCount == b.QueueRowCount &&
        Same(a.QueueRow0, b.QueueRow0) &&
        Same(a.QueueRow1, b.QueueRow1);

    private static bool Same(UiBuildPlacementConfirmationBarModel a, UiBuildPlacementConfirmationBarModel b) =>
        a.Visible == b.Visible &&
        a.CanConfirm == b.CanConfirm &&
        a.CanCancel == b.CanCancel &&
        a.CanRotate == b.CanRotate &&
        SameText(a.Title, b.Title) &&
        SameText(a.Status, b.Status) &&
        SameText(a.CostText, b.CostText) &&
        SameText(a.DurationText, b.DurationText) &&
        SameText(a.InstructionText, b.InstructionText);

    private static bool SameText(string a, string b) =>
        string.Equals(a, b, StringComparison.Ordinal);

    private void FlushPendingCompletion()
    {
        if (!hasPendingCompletion)
            return;

        if (UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion))
            hasPendingCompletion = false;
    }
}
