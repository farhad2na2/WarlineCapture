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
    public sealed partial class UiShellEcsGateway
    {
        private static partial class UiShellReadModelAdapter
        {
        public static bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
        {
            loading = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasComponent<UiShellLoadingProgressComponent>(boundary))
                return false;

            UiShellLoadingProgressComponent component =
                entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
            loading = new UiShellLoadingProgressModel(
                component.Progress01,
                component.Status.ToString(),
                component.IsComplete != 0);
            return true;
        }

        public static bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
        {
            diagnostics = UiDiagnosticsOverlayModel.Default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureDiagnosticsOverlayState(entityManager, boundary);
            UiDiagnosticsOverlayComponent component =
                entityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);
            bool logVisible = component.LogVisible != 0;
            diagnostics = new UiDiagnosticsOverlayModel(
                Mathf.Max(0, component.Fps),
                logVisible,
                logVisible ? GetDiagnosticsLogText(component.LogText) : string.Empty);
            return true;
        }

        public static bool TryReadShellState(out UiShellStateModel state)
        {
            state = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
                return false;

            UiShellStateComponent component = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            state = new UiShellStateModel(
                component.CurrentMode,
                component.ActiveRoute,
                component.Phase,
                component.TransitionSequenceId,
                component.IsTransitionRunning != 0);
            return true;
        }

        public static bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
        {
            profile = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureCommanderProfileState(entityManager, boundary);
            UiShellCommanderProfileComponent component =
                entityManager.GetComponentData<UiShellCommanderProfileComponent>(boundary);
            profile = new UiShellCommanderProfileModel(
                component.Name.ToString(),
                component.Subtitle.ToString(),
                component.PortraitClass.ToString());
            return true;
        }

        public static bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
        {
            resources = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureMainMenuResourcesState(entityManager, boundary);
            UiShellMainMenuResourcesComponent component =
                entityManager.GetComponentData<UiShellMainMenuResourcesComponent>(boundary);
            resources = new UiShellMainMenuResourcesModel(
                component.CreditsText.ToString(),
                component.CommandText.ToString());
            return true;
        }

        public static bool TryReadMissionResult(out UiMissionResultPopupModel result)
        {
            result = UiMissionResultPopupModel.VictoryDefault;
            return false;
        }

        public static bool TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
        {
            campaign = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary) ||
                !entityManager.HasComponent<UiCampaignOperationsComponent>(boundary))
                return false;

            UiCampaignOperationsComponent component =
                entityManager.GetComponentData<UiCampaignOperationsComponent>(boundary);
            if (component.Version == 0 || component.SelectedMissionId.IsEmpty)
                return false;
            UiCampaignMissionModel selected = new(
                component.SelectedMissionId.ToString(), component.ScenarioId.ToString(),
                component.OperationMapId.ToString(), component.DisplayName.ToString(),
                component.Available != 0, component.FirstClearCompleted != 0, component.PendingResume != 0,
                component.BestStars, component.BestCompletionMilliseconds, component.SuccessfulReplayCount,
                component.PrimaryAction, component.PrimaryActionLabel.ToString());
            campaign = new UiCampaignOperationsModel(
                component.Version, component.CatalogSourceVersion, component.ProgressSourceVersion,
                selected, component.NextMissionId.ToString(), component.NextMissionRevealed != 0);
            return campaign.IsValid;
        }

        }

        bool IUiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
        {
            return UiShellReadModelAdapter.TryReadCampaignOperations(out campaign);
        }

        bool IUiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
            UiCampaignMissionActionKind action, string missionId)
        {
            return UiShellActionAdapter.TryEnqueueCampaignMissionAction(action, missionId);
        }
    }
}
