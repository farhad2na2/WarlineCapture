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

        public static bool TryReadMissionBriefing(out UiMissionBriefingModel briefing)
        {
            briefing = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary) ||
                !entityManager.HasComponent<UiMissionBriefingComponent>(boundary))
                return false;
            UiMissionBriefingComponent component =
                entityManager.GetComponentData<UiMissionBriefingComponent>(boundary);
            if (component.Version == 0 || component.MissionId.IsEmpty)
                return false;

            UiMissionObjectiveModel[] objectives = new UiMissionObjectiveModel[component.Objectives.Length];
            for (int index = 0; index < objectives.Length; index++)
            {
                UiMissionObjectiveProjectionData source = component.Objectives[index];
                objectives[index] = new UiMissionObjectiveModel(
                    source.ObjectiveId.ToString(), source.DisplayTextKey.ToString(),
                    source.MissionRoleId.ToString(), source.Rule, source.RequiredCount,
                    source.FailureOnRuleBreak != 0);
            }
            UiMissionRewardModel[] rewards = new UiMissionRewardModel[component.Rewards.Length];
            for (int index = 0; index < rewards.Length; index++)
            {
                UiMissionRewardProjectionData source = component.Rewards[index];
                rewards[index] = new UiMissionRewardModel(
                    source.Kind, source.RewardConfigId.ToString(),
                    source.DisplayTextKey.ToString(), source.Amount);
            }
            briefing = new UiMissionBriefingModel(
                component.Version, component.MissionId.ToString(), component.ScenarioId.ToString(),
                component.OperationMapId.ToString(), component.DisplayNameKey.ToString(),
                component.DisplaySummaryKey.ToString(), component.LocationNameKey.ToString(),
                objectives, rewards, component.HostileUnitCount,
                component.BuildingDisabled != 0, component.ProductionDisabled != 0,
                component.EconomyDisabled != 0, component.TransportDisabled != 0,
                component.AirDisabled != 0, component.Replay != 0, component.ReplayAllowed != 0,
                component.ReplayTutorialEnabled != 0, component.ReplayTutorialToggleVisible != 0,
                component.DeployQueued != 0);
            return briefing.IsValid;
        }

        }

        bool IUiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign)
        {
            return UiShellReadModelAdapter.TryReadCampaignOperations(out campaign);
        }

        bool IUiShellRuntimeGateway.TryReadMissionBriefing(out UiMissionBriefingModel briefing)
        {
            return UiShellReadModelAdapter.TryReadMissionBriefing(out briefing);
        }

        bool IUiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
            UiCampaignMissionActionKind action, string missionId, bool value)
        {
            return UiShellActionAdapter.TryEnqueueCampaignMissionAction(action, missionId, value);
        }
    }
}
