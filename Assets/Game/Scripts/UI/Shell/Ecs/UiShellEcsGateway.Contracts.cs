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
using Game.Missions.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMatchHudResourceValuesGateway, IUiCurrentMissionRestartGateway
    {
        bool IUiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            return TryEnqueueRouteRequest(intent, route, pushHistory);
        }

        bool IUiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind kind, int payloadId)
        {
            return TryEnqueueUiAction(kind, payloadId);
        }

        bool IUiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
            UiAssistantCommandIntentKind kind,
            bool fromTakeover)
        {
            return TryEnqueueAssistantCommandIntent(kind, fromTakeover);
        }

        bool IUiAssistantPanelStateGateway.TrySetAssistantPanelOpen(bool open)
        {
            return TrySetAssistantPanelOpen(open);
        }

        bool IUiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
        {
            return TryReadLoadingProgress(out loading);
        }

        bool IUiShellRuntimeGateway.TrySetLoadingProgress(float progress01, string status, bool complete)
        {
            return TrySetLoadingProgress(progress01, status, complete);
        }

        bool IUiShellRuntimeGateway.TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
        {
            return TryReadDiagnosticsOverlay(out diagnostics);
        }

        bool IUiShellRuntimeGateway.TryReadShellState(out UiShellStateModel state)
        {
            return TryReadShellState(out state);
        }

        bool IUiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
        {
            return TryReadCommanderProfile(out profile);
        }

        bool IUiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
        {
            return TryReadMainMenuResources(out resources);
        }

        bool IUiShellRuntimeGateway.TryReadMissionResult(out UiMissionResultPopupModel result)
        {
            return TryReadMissionResult(out result);
        }

        bool IUiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind action)
        {
            return TryEnqueueMissionResultAction(action);
        }

        bool IUiCurrentMissionRestartGateway.TryRestartCurrentMission()
        {
            return TryRestartCurrentMission();
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
        {
            return TryReadMatchHudSelection(out selection);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
        {
            return TryReadMatchHudCommandState(out commandState);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
        {
            return TryReadMatchHudPassengerDrawer(out passengerDrawer);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
        {
            return TryReadMatchHudSquadTray(out squadTray);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
        {
            return TryReadMatchHudHeader(out header);
        }

        bool IUiMatchHudResourceValuesGateway.TryReadMatchHudResourceValues(
            out UiMatchHudResourceValuesModel values)
        {
            return TryReadMatchHudResourceValues(out values);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
        {
            return TryReadMatchHudStatusSurfaces(out statusSurfaces);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
        {
            return TryReadMatchHudAssistantPanel(out assistantPanel);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
        {
            return TryReadMatchHudAssistantHighlight(out assistantHighlight);
        }

        bool IUiShellRuntimeGateway.TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
        {
            return TryReadMatchHudMinimap(out minimap);
        }

        bool IUiShellRuntimeGateway.TryReadBuildDrawer(out UiBuildDrawerModel drawer)
        {
            return TryReadBuildDrawer(out drawer);
        }

        bool IUiShellRuntimeGateway.TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            return TryReadResourceExchange(out exchange);
        }

        bool IUiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
        {
            return TryReadBuildPlacementConfirmationBar(out placementBar);
        }

        bool IUiShellRuntimeGateway.TryReadArmoryCategory(out ArmoryCatalogCategory category)
        {
            return TryReadArmoryCategory(out category);
        }

        bool IUiShellRuntimeGateway.TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
        {
            return TryEnqueueArmoryCategory(category);
        }

        bool IUiShellRuntimeGateway.TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
        {
            return TryConsumePresentationCommands(commands);
        }

        bool IUiShellRuntimeGateway.TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
        {
            return TryEnqueueTransitionComplete(completion);
        }
    }
}
