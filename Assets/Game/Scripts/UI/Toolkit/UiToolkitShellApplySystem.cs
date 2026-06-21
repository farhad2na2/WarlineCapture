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
        shellView = view;
        if (shellView != null && !shellView.IsMounted)
            shellView.Mount();
    }

    public void ClearShellView(UiToolkitShellView view = null)
    {
        if (view == null || ReferenceEquals(shellView, view))
            shellView = null;
    }

    protected override void OnUpdate()
    {
        hasShellState = UiShellRuntimeGateway.TryReadShellState(out lastShellState);
        hasLoadingProgress = UiShellRuntimeGateway.TryReadLoadingProgress(out lastLoadingProgress);
        hasDiagnosticsOverlay = UiShellRuntimeGateway.TryReadDiagnosticsOverlay(out lastDiagnosticsOverlay);
        hasCommanderProfile = UiShellRuntimeGateway.TryReadCommanderProfile(out lastCommanderProfile);
        hasMainMenuResources = UiShellRuntimeGateway.TryReadMainMenuResources(out lastMainMenuResources);
        hasMissionResult = UiShellRuntimeGateway.TryReadMissionResult(out lastMissionResult);
        hasMatchHudSelection = UiShellRuntimeGateway.TryReadMatchHudSelection(out lastMatchHudSelection);
        hasMatchHudCommandState = UiShellRuntimeGateway.TryReadMatchHudCommandState(out lastMatchHudCommandState);
        hasMatchHudHeader = UiShellRuntimeGateway.TryReadMatchHudHeader(out lastMatchHudHeader);
        hasMatchHudStatusSurfaces =
            UiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out lastMatchHudStatusSurfaces);
        hasMatchHudMinimap = UiShellRuntimeGateway.TryReadMatchHudMinimap(out lastMatchHudMinimap);
        hasMatchHudPassengerDrawer =
            UiShellRuntimeGateway.TryReadMatchHudPassengerDrawer(out lastMatchHudPassengerDrawer);
        hasMatchHudSquadTray = UiShellRuntimeGateway.TryReadMatchHudSquadTray(out lastMatchHudSquadTray);
        hasBuildDrawer = UiShellRuntimeGateway.TryReadBuildDrawer(out lastBuildDrawer);
        hasBuildPlacementConfirmationBar =
            UiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar(out lastBuildPlacementConfirmationBar);
        hasArmoryCategory = UiShellRuntimeGateway.TryReadArmoryCategory(out lastArmoryCategory);

        if (shellView != null && shellView.IsMounted && hasLoadingProgress)
            shellView.ApplyLoadingProgress(lastLoadingProgress);

        if (shellView != null && shellView.IsMounted && hasDiagnosticsOverlay)
            shellView.ApplyDiagnosticsOverlay(lastDiagnosticsOverlay);

        if (shellView != null && shellView.IsMounted && hasMissionResult)
            shellView.ApplyMissionResult(lastMissionResult);

        if (shellView != null &&
            shellView.IsMounted &&
            hasShellState &&
            lastShellState.CurrentMode == UiShellMode.MainMenu)
        {
            shellView.EnsureMainMenuVisible(lastShellState.ActiveRoute);
            shellView.ApplyMainMenuRouteState(lastShellState.ActiveRoute);
            if (hasCommanderProfile)
            {
                shellView.ApplyMainMenuCommanderProfile(lastCommanderProfile);
                shellView.ApplyCommanderProfile(lastCommanderProfile);
            }
            if (hasMainMenuResources)
                shellView.ApplyMainMenuResources(lastMainMenuResources);
            if (lastShellState.ActiveRoute == UIRoute.Armory && hasArmoryCategory)
                shellView.ApplyArmoryCategory(lastArmoryCategory);
        }

        if (shellView != null &&
            shellView.IsMounted &&
            hasShellState &&
            lastShellState.CurrentMode == UiShellMode.MatchHud)
        {
            if (hasMatchHudSelection)
                shellView.ApplyMatchHudSelection(lastMatchHudSelection);
            if (hasMatchHudCommandState)
                shellView.ApplyMatchHudCommandState(lastMatchHudCommandState);
            if (hasMatchHudHeader)
                shellView.ApplyMatchHudHeader(lastMatchHudHeader);
            if (hasMatchHudStatusSurfaces)
                shellView.ApplyMatchHudStatusSurfaces(lastMatchHudStatusSurfaces);
            if (hasMatchHudMinimap)
                shellView.ApplyMatchHudMinimap(lastMatchHudMinimap);
            if (hasMatchHudPassengerDrawer)
                shellView.ApplyMatchHudPassengerDrawer(lastMatchHudPassengerDrawer);
            if (hasMatchHudSquadTray)
                shellView.ApplyMatchHudSquadTray(lastMatchHudSquadTray);
            if (hasBuildDrawer)
                shellView.ApplyBuildDrawer(lastBuildDrawer);
            if (hasBuildPlacementConfirmationBar)
                shellView.ApplyBuildPlacementConfirmationBar(lastBuildPlacementConfirmationBar);
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

    private void FlushPendingCompletion()
    {
        if (!hasPendingCompletion)
            return;

        if (UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion))
            hasPendingCompletion = false;
    }
}
