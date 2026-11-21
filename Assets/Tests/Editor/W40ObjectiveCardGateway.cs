using System.Collections.Generic;
using Game.UI.Contracts;
using Game.UI.Runtime;

// W40 fixture gateway: a Skirmish match HUD in MatchHudReady with a fixture match model and an
// optional public objective model, so the shipped SkirmishMatchView renders its objective card.
// Every request is refused (camera focus included). Test/capture only.
internal sealed class W40ObjectiveCardGateway : IUiShellRuntimeGateway, IUiSkirmishGateway, IUiSkirmishPublicObjectiveGateway
{
    public UiSkirmishModel Match;
    public UiSkirmishPublicObjectiveModel Objective;

    public bool TryReadSkirmish(out UiSkirmishModel model) { model = Match; return true; }
    public bool TryRequestSkirmish(UiSkirmishAction action) => false;
    public bool TryReadSkirmishPublicObjective(out UiSkirmishPublicObjectiveModel model) { model = Objective; return Objective != null; }
    public bool TryFocusSkirmishPublicObjective(string publicId) => false;

    public bool TryReadShellState(out UiShellStateModel state)
    {
        state = new UiShellStateModel(UiShellMode.MatchHud, default, UiShellTransitionPhase.MatchHudReady, 1, false);
        return true;
    }

    public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
    public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => false;
    public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover) => false;
    public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) { loading = default; return false; }
    public bool TrySetLoadingProgress(float progress01, string status, bool complete) => false;
    public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) { diagnostics = default; return false; }
    public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) { profile = default; return false; }
    public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) { resources = default; return false; }
    public bool TryReadMissionResult(out UiMissionResultPopupModel result) { result = default; return false; }
    public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) { selection = default; return false; }
    public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState) { commandState = default; return false; }
    public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) { header = default; return false; }
    public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces) { statusSurfaces = default; return false; }
    public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) { assistantPanel = default; return false; }
    public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = default; return false; }
    public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = default; return false; }
    public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) { passengerDrawer = default; return false; }
    public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) { squadTray = default; return false; }
    public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = default; return false; }
    public bool TryReadResourceExchange(out UiResourceExchangeModel exchange) { exchange = default; return false; }
    public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = default; return false; }
    public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = default; return false; }
    public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
    public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
    public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
}
