using System.Collections.Generic;

public interface IUiShellRuntimeGateway
{
    bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory);

    bool TryEnqueueUiAction(UiActionKind kind, int payloadId);

    bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading);

    bool TrySetLoadingProgress(float progress01, string status, bool complete);

    bool TryReadShellState(out UiShellStateModel state);

    bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile);

    bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources);

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

public static class UiShellRuntimeGateway
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

    public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
    {
        return current.TryReadLoadingProgress(out loading);
    }

    public static bool TrySetLoadingProgress(float progress01, string status, bool complete)
    {
        return current.TrySetLoadingProgress(progress01, status, complete);
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

    public static bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
    {
        return current.TryReadMatchHudStatusSurfaces(out statusSurfaces);
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

        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
        {
            loading = default;
            return false;
        }

        public bool TrySetLoadingProgress(float progress01, string status, bool complete)
        {
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
