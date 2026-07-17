using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiShellRuntimeGateway, IUiAssistantPanelStateGateway
    {
        private static readonly UiShellEcsGateway Shared = new();
        private static World cachedWorld;
        private static EntityQuery boundaryQuery;
        private static EntityQuery focusedSelectionQuery;
        private static EntityQuery selectionInputQuery;
        private static EntityQuery selectedUnitsQuery;
        private static EntityQuery minimapMarkerQuery;
        private static EntityQuery gridConfigQuery;
        private static EntityQuery resourceStorageQuery;
        private static EntityQuery assistantMatchStartQuery;
        private static FixedString4096Bytes cachedDiagnosticsLogFixedText;
        private static string cachedDiagnosticsLogText;
        private static bool hasBoundaryQuery;
        private static bool hasFocusedSelectionQuery;
        private static bool hasSelectionInputQuery;
        private static bool hasSelectedUnitsQuery;
        private static bool hasMinimapMarkerQuery;
        private static bool hasGridConfigQuery;
        private static bool hasResourceStorageQuery;
        private static bool hasAssistantMatchStartQuery;
        private static bool hasCachedDiagnosticsLogText;
        private static bool hasCachedMatchHudHeader;
        private static World cachedMatchHudHeaderWorld;
        private static Entity cachedMatchHudHeaderBoundary;
        private static UiMatchHudHeaderComponent cachedMatchHudHeaderComponent;
        private static byte cachedMatchHudHeaderResourceSource;
        private static uint cachedMatchHudHeaderResourceVersion;
        private static int cachedMatchHudHeaderOil;
        private static int cachedMatchHudHeaderFuel;
        private static bool cachedMatchHudHeaderShowOil;
        private static UiMatchHudHeaderModel cachedMatchHudHeader;
        private static bool hasCachedAssistantPanel;
        private static World cachedAssistantPanelWorld;
        private static Entity cachedAssistantPanelBoundary;
        private static uint cachedAssistantPanelSourceVersion;
        private static uint cachedAssistantPanelRecommendationVersion;
        private static uint cachedAssistantPanelObjectiveVersion;
        private static uint cachedAssistantPanelMessageReadModelVersion;
        private static uint cachedAssistantPanelThreatVersion;
        private static uint cachedAssistantPanelTargetLockVersion;
        private static uint cachedAssistantPanelNarrationStateVersion;
        private static bool cachedAssistantPanelNarrationPulse;
        private static uint cachedAssistantPanelSettingsVersion;
        private static uint cachedAssistantPanelVersion;
        private static int cachedAssistantPanelGoalCount;
        private static int cachedAssistantPanelMessageCount;
        private static int cachedAssistantPanelRecommendationCount;
        private static AssistantControlState cachedAssistantPanelControlState;
        private static UiAssistantPanelModel cachedAssistantPanel;
        private static bool hasCachedAssistantHighlight;
        private static World cachedAssistantHighlightWorld;
        private static Entity cachedAssistantHighlightBoundary;
        private static uint cachedAssistantHighlightVersion;
        private static int cachedAssistantHighlightRequestId;
        private static UiAssistantHighlightModel cachedAssistantHighlight;

        private UiShellEcsGateway()
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void RegisterAsRuntimeGateway()
        {
            ResetWorldBoundQueries(null);
            hasCachedDiagnosticsLogText = false;
            hasCachedMatchHudHeader = false;
            cachedMatchHudHeaderWorld = null;
            cachedMatchHudHeaderBoundary = Entity.Null;
            cachedMatchHudHeaderComponent = default;
            cachedMatchHudHeaderResourceSource = 0;
            cachedMatchHudHeaderResourceVersion = 0;
            cachedMatchHudHeaderOil = 0;
            cachedMatchHudHeaderFuel = 0;
            cachedMatchHudHeaderShowOil = false;
            cachedMatchHudHeader = UiMatchHudHeaderModel.Default;
            hasCachedAssistantPanel = false;
            cachedAssistantPanelWorld = null;
            cachedAssistantPanelBoundary = Entity.Null;
            cachedAssistantPanelSourceVersion = 0;
            cachedAssistantPanelRecommendationVersion = 0;
            cachedAssistantPanelObjectiveVersion = 0;
            cachedAssistantPanelMessageReadModelVersion = 0;
            cachedAssistantPanelThreatVersion = 0;
            cachedAssistantPanelTargetLockVersion = 0;
            cachedAssistantPanelNarrationStateVersion = 0;
            cachedAssistantPanelNarrationPulse = false;
            cachedAssistantPanelSettingsVersion = 0;
            cachedAssistantPanelVersion = 0;
            cachedAssistantPanelGoalCount = 0;
            cachedAssistantPanelMessageCount = 0;
            cachedAssistantPanelRecommendationCount = 0;
            cachedAssistantPanelControlState = AssistantControlState.Player;
            cachedAssistantPanel = UiAssistantPanelModel.Empty;
            hasCachedAssistantHighlight = false;
            cachedAssistantHighlightWorld = null;
            cachedAssistantHighlightBoundary = Entity.Null;
            cachedAssistantHighlightVersion = 0;
            cachedAssistantHighlightRequestId = 0;
            cachedAssistantHighlight = UiAssistantHighlightModel.Empty;
            cachedDiagnosticsLogFixedText = default;
            cachedDiagnosticsLogText = string.Empty;
            UiShellRuntimeGateway.Register(Shared);
        }

        public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) =>
            UiShellRouteAdapter.TryEnqueueRouteRequest(intent, route, pushHistory);

        public static bool TryEnqueueUiAction(UiActionKind kind, int payloadId) =>
            UiShellActionAdapter.TryEnqueueUiAction(kind, payloadId);

        public static bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover) =>
            UiShellActionAdapter.TryEnqueueAssistantCommandIntent(kind, fromTakeover);

        public static bool TrySetAssistantPanelOpen(bool open) =>
            UiShellSettingsAdapter.TrySetAssistantPanelOpen(open);

        public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) =>
            UiShellReadModelAdapter.TryReadLoadingProgress(out loading);

        public static bool TrySetLoadingProgress(float progress01, string status, bool complete) =>
            UiShellActionAdapter.TrySetLoadingProgress(progress01, status, complete);

        public static bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) =>
            UiShellReadModelAdapter.TryReadDiagnosticsOverlay(out diagnostics);

        public static bool TryReadShellState(out UiShellStateModel state) =>
            UiShellReadModelAdapter.TryReadShellState(out state);

        public static bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) =>
            UiShellReadModelAdapter.TryReadCommanderProfile(out profile);

        public static bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) =>
            UiShellReadModelAdapter.TryReadMainMenuResources(out resources);

        public static bool TryReadMissionResult(out UiMissionResultPopupModel result) =>
            UiShellReadModelAdapter.TryReadMissionResult(out result);

        public static bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) =>
            UiShellReadModelAdapter.TryReadMatchHudSelection(out selection);

        public static bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState) =>
            UiShellReadModelAdapter.TryReadMatchHudCommandState(out commandState);

        public static bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) =>
            UiShellReadModelAdapter.TryReadMatchHudPassengerDrawer(out passengerDrawer);

        public static bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) =>
            UiShellReadModelAdapter.TryReadMatchHudSquadTray(out squadTray);

        public static bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) =>
            UiShellReadModelAdapter.TryReadMatchHudHeader(out header);

        public static bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces) =>
            UiShellReadModelAdapter.TryReadMatchHudStatusSurfaces(out statusSurfaces);

        public static bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) =>
            UiShellReadModelAdapter.TryReadMatchHudAssistantPanel(out assistantPanel);

        public static bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) =>
            UiShellReadModelAdapter.TryReadMatchHudAssistantHighlight(out assistantHighlight);

        public static bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) =>
            UiShellReadModelAdapter.TryReadMatchHudMinimap(out minimap);

        public static bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) =>
            UiShellReadModelAdapter.TryReadBuildDrawer(out drawer);

        public static bool TryReadResourceExchange(out UiResourceExchangeModel exchange) =>
            UiShellReadModelAdapter.TryReadResourceExchange(out exchange);

        public static bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) =>
            UiShellReadModelAdapter.TryReadBuildPlacementConfirmationBar(out placementBar);

        public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category) =>
            UiShellSettingsAdapter.TryReadArmoryCategory(out category);

        public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) =>
            UiShellSettingsAdapter.TryEnqueueArmoryCategory(category);

        public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) =>
            UiShellRouteAdapter.TryConsumePresentationCommands(commands);

        public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) =>
            UiShellRouteAdapter.TryEnqueueTransitionComplete(completion);

        private static bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
        {
            entityManager = default;
            boundary = Entity.Null;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (cachedWorld != world)
                ResetWorldBoundQueries(world);

            if (!hasBoundaryQuery)
            {
                boundaryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
                hasBoundaryQuery = true;
            }

            if (boundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entityManager = world.EntityManager;
            boundary = boundaryQuery.GetSingletonEntity();
            return true;
        }

    }
}
