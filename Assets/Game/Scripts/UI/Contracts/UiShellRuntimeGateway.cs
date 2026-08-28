using System.Collections.Generic;

namespace Game.UI.Contracts
{
    public enum UiAssistantCommandIntentKind : byte
    {
        None = 0,
        ShowRecommendation = 1,
        ExecuteRecommendation = 2,
        StopAssistantControl = 3
    }

    public enum UiTutorialNarrationPhase : byte
    {
        PrimaryAction = 0,
        WorldTarget = 1
    }

    public interface IUiShellRuntimeGateway
    {
        bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory);

        bool TryEnqueueUiAction(UiActionKind kind, int payloadId);

        bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover);

        bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading);

        bool TrySetLoadingProgress(float progress01, string status, bool complete);

        bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics);

        bool TryReadShellState(out UiShellStateModel state);

        bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile);

        bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources);

        bool TryReadMissionResult(out UiMissionResultPopupModel result);

        bool TryEnqueueMissionResultAction(UiMissionResultActionKind action) => false;

        bool TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
        {
            campaign = default;
            return false;
        }

        bool TryReadMissionBriefing(out UiMissionBriefingModel briefing)
        {
            briefing = default;
            return false;
        }

        bool TryEnqueueCampaignMissionAction(
            UiCampaignMissionActionKind action, string missionId, bool value = false) => false;

        bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection);

        bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState);

        bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header);

        bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces);

        bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel);

        bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight);

        bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap);

        bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer);

        bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray);

        bool TryReadBuildDrawer(out UiBuildDrawerModel drawer);

        bool TryReadResourceExchange(out UiResourceExchangeModel exchange);

        bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar);

        bool TryReadArmoryCategory(out ArmoryCatalogCategory category);

        bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category);

        bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands);

        bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion);
    }

    public interface IUiMatchHudResourceValuesGateway
    {
        bool TryReadMatchHudResourceValues(out UiMatchHudResourceValuesModel values);
    }

    public interface IUiAssistantPanelStateGateway
    {
        bool TrySetAssistantPanelOpen(bool open);
    }

    public interface IUiTutorialNarrationGateway
    {
        bool TryEnqueueTutorialNarration(
            byte tutorialStep,
            byte tutorialStepCount,
            UiTutorialNarrationPhase phase,
            string text);
    }

    public enum UiCampaignGuidanceTargetKind : byte
    {
        None = 0,
        BuildButton = 1,
        BarracksCatalogItem = 2,
        ResourceStrip = 3,
        RifleProduction = 4
    }

    public interface IUiCampaignGuidanceGateway
    {
        bool TryAcknowledgeCampaignGuidanceTarget(UiCampaignGuidanceTargetKind target);
    }
}
