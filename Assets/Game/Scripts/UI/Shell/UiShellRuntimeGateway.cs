using System.Collections.Generic;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        private static IUiShellRuntimeGateway current = NullUiShellRuntimeGateway.Instance;

        public static void Register(IUiShellRuntimeGateway gateway)
        {
            current = gateway ?? NullUiShellRuntimeGateway.Instance;
        }

        public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            return current.TryEnqueueRouteRequest(intent, route, pushHistory);
        }

        public static bool TryEnqueueUiAction(UiActionKind kind, int payloadId = 0)
        {
            return current.TryEnqueueUiAction(kind, payloadId);
        }

        public static bool TryEnqueueAssistantCommandIntent(
            UiAssistantCommandIntentKind kind,
            bool fromTakeover = false)
        {
            return current.TryEnqueueAssistantCommandIntent(kind, fromTakeover);
        }

        public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
        {
            return current.TryReadLoadingProgress(out loading);
        }

        public static bool TrySetLoadingProgress(float progress01, string status, bool complete)
        {
            return current.TrySetLoadingProgress(progress01, status, complete);
        }

        public static bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
        {
            return current.TryReadDiagnosticsOverlay(out diagnostics);
        }

        public static bool TryReadShellState(out UiShellStateModel state)
        {
            return current.TryReadShellState(out state);
        }

        public static bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
        {
            return current.TryReadCommanderProfile(out profile);
        }

        public static bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
        {
            return current.TryReadMainMenuResources(out resources);
        }

        public static bool TryReadMissionResult(out UiMissionResultPopupModel result)
        {
            return current.TryReadMissionResult(out result);
        }

        public static bool TryEnqueueMissionResultAction(UiMissionResultActionKind action)
        {
            return current.TryEnqueueMissionResultAction(action);
        }

        public static bool TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
        {
            return current.TryReadCampaignOperations(out campaign);
        }

        public static bool TryReadMissionBriefing(out UiMissionBriefingModel briefing)
        {
            return current.TryReadMissionBriefing(out briefing);
        }

        public static bool TryEnqueueCampaignMissionAction(
            UiCampaignMissionActionKind action, string missionId, bool value = false)
        {
            return current.TryEnqueueCampaignMissionAction(action, missionId, value);
        }

        public static bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
        {
            return current.TryReadMatchHudSelection(out selection);
        }

        public static bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
        {
            return current.TryReadMatchHudCommandState(out commandState);
        }

        public static bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
        {
            return current.TryReadMatchHudHeader(out header);
        }

        public static bool TryReadMatchHudResourceValues(out UiMatchHudResourceValuesModel values)
        {
            values = UiMatchHudResourceValuesModel.Invalid;
            return current is IUiMatchHudResourceValuesGateway resourceValuesGateway &&
                   resourceValuesGateway.TryReadMatchHudResourceValues(out values) &&
                   values.IsValid;
        }

        public static bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
        {
            return current.TryReadMatchHudStatusSurfaces(out statusSurfaces);
        }

        public static bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
        {
            return current.TryReadMatchHudAssistantPanel(out assistantPanel);
        }

        public static bool TrySetAssistantPanelOpen(bool open)
        {
            return current is IUiAssistantPanelStateGateway assistantPanelState &&
                   assistantPanelState.TrySetAssistantPanelOpen(open);
        }

        public static bool TryEnqueueTutorialNarration(byte tutorialStep, string text)
        {
            return current is IUiTutorialNarrationGateway tutorialNarration &&
                   tutorialNarration.TryEnqueueTutorialNarration(tutorialStep, text);
        }

        public static bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
        {
            return current.TryReadMatchHudAssistantHighlight(out assistantHighlight);
        }

        public static bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
        {
            return current.TryReadMatchHudMinimap(out minimap);
        }

        public static bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
        {
            return current.TryReadMatchHudPassengerDrawer(out passengerDrawer);
        }

        public static bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
        {
            return current.TryReadMatchHudSquadTray(out squadTray);
        }

        public static bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)
        {
            return current.TryReadBuildDrawer(out drawer);
        }

        public static bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            return current.TryReadResourceExchange(out exchange);
        }

        public static bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
        {
            return current.TryReadBuildPlacementConfirmationBar(out placementBar);
        }

        public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
        {
            return current.TryReadArmoryCategory(out category);
        }

        public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
        {
            return current.TryEnqueueArmoryCategory(category);
        }

        public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
        {
            return current.TryConsumePresentationCommands(commands);
        }

        public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
        {
            return current.TryEnqueueTransitionComplete(completion);
        }

        private sealed class NullUiShellRuntimeGateway : IUiShellRuntimeGateway
        {
            public static readonly NullUiShellRuntimeGateway Instance = new();

            public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
            {
                return false;
            }

            public bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
            {
                return false;
            }

            public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover)
            {
                return false;
            }

            public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
            {
                loading = default;
                return false;
            }

            public bool TrySetLoadingProgress(float progress01, string status, bool complete)
            {
                return false;
            }

            public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
            {
                diagnostics = UiDiagnosticsOverlayModel.Default;
                return false;
            }

            public bool TryReadShellState(out UiShellStateModel state)
            {
                state = default;
                return false;
            }

            public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
            {
                profile = default;
                return false;
            }

            public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
            {
                resources = default;
                return false;
            }

            public bool TryReadMissionResult(out UiMissionResultPopupModel result)
            {
                result = UiMissionResultPopupModel.VictoryDefault;
                return false;
            }

            public bool TryEnqueueMissionResultAction(UiMissionResultActionKind action)
            {
                return false;
            }

            public bool TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
            {
                campaign = default;
                return false;
            }

            public bool TryReadMissionBriefing(out UiMissionBriefingModel briefing)
            {
                briefing = default;
                return false;
            }

            public bool TryEnqueueCampaignMissionAction(
                UiCampaignMissionActionKind action, string missionId, bool value = false)
            {
                return false;
            }

            public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
            {
                selection = UiMatchHudSelectionPanelModel.Hidden;
                return false;
            }

            public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
            {
                commandState = default;
                return false;
            }

            public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
            {
                header = UiMatchHudHeaderModel.Default;
                return false;
            }

            public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
            {
                statusSurfaces = UiMatchHudStatusSurfacesModel.Default;
                return false;
            }

            public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
            {
                assistantPanel = UiAssistantPanelModel.Empty;
                return false;
            }

            public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
            {
                assistantHighlight = UiAssistantHighlightModel.Empty;
                return false;
            }

            public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
            {
                minimap = UiMatchHudMinimapModel.Default;
                return false;
            }

            public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
            {
                passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden;
                return false;
            }

            public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
            {
                squadTray = UiMatchHudSquadTrayModel.Default;
                return false;
            }

            public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)
            {
                drawer = UiBuildDrawerModel.Empty;
                return false;
            }

            public bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
            {
                exchange = UiResourceExchangeModel.Empty;
                return false;
            }

            public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
            {
                placementBar = UiBuildPlacementConfirmationBarModel.Hidden;
                return false;
            }

            public bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
            {
                category = ArmoryCatalogCategory.Characters;
                return false;
            }

            public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
            {
                return false;
            }

            public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
            {
                commands?.Clear();
                return false;
            }

            public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
            {
                return false;
            }
        }
    }
}
