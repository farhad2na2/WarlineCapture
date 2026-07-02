using System.Collections.Generic;

namespace Game.UI.Contracts
{
    public interface IUiShellRuntimeGateway
    {
        bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory);

        bool TryEnqueueUiAction(UiActionKind kind, int payloadId);

        bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading);

        bool TrySetLoadingProgress(float progress01, string status, bool complete);

        bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics);

        bool TryReadShellState(out UiShellStateModel state);

        bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile);

        bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources);

        bool TryReadMissionResult(out UiMissionResultPopupModel result);

        bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection);

        bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState);

        bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header);

        bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces);

        bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap);

        bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer);

        bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray);

        bool TryReadBuildDrawer(out UiBuildDrawerModel drawer);

        bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar);

        bool TryReadArmoryCategory(out ArmoryCatalogCategory category);

        bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category);

        bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands);

        bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion);
    }
}
