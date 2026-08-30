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
            if (!TryGetMissionRoot(out EntityManager entityManager, out Entity root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionResultComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionCatalogComponent>(root))
                return false;

            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionResultComponent projection =
                entityManager.GetComponentData<CampaignMissionResultComponent>(root);
            if (runtime.Phase is not (
                    MissionPhaseKind.Result or MissionPhaseKind.ResultAfterDebrief) ||
                projection.SourceVersion == 0 ||
                !runtime.MissionId.Equals(projection.MissionId) ||
                !runtime.SessionToken.Equals(projection.SessionToken) ||
                runtime.AttemptOrdinal != projection.AttemptOrdinal)
                return false;

            byte settlementAccepted = 0;
            byte settlementFirstClear = 0;
            if (projection.Outcome == MissionOutcomeKind.Victory &&
                entityManager.HasBuffer<CampaignMissionSettlementResultElement>(root))
            {
                DynamicBuffer<CampaignMissionSettlementResultElement> settlements =
                    entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root, true);
                for (int index = settlements.Length - 1; index >= 0; index--)
                {
                    CampaignMissionSettlementResultElement candidate = settlements[index];
                    if (candidate.SourceVersion == projection.SourceVersion &&
                        candidate.SessionToken.Equals(projection.SessionToken))
                    {
                        settlementAccepted = candidate.Accepted;
                        settlementFirstClear = candidate.FirstClear;
                        break;
                    }
                }
            }
            if (projection.Outcome == MissionOutcomeKind.Victory && settlementAccepted == 0)
                return false;

            if (cachedMissionResultWorld == entityManager.World && cachedMissionResultRoot == root &&
                cachedMissionResultSession.Equals(projection.SessionToken) &&
                cachedMissionResultAttempt == projection.AttemptOrdinal &&
                cachedMissionResultVersion == projection.SourceVersion &&
                cachedMissionSettlementAccepted == settlementAccepted &&
                cachedMissionSettlementFirstClear == settlementFirstClear)
            {
                result = cachedMissionResult;
                return true;
            }

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!TryFindMissionDefinition(in catalog, in runtime, out int definitionIndex))
                return false;
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            CampaignMissionAttemptFactsComponent facts =
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool victory = projection.Outcome == MissionOutcomeKind.Victory;
            bool firstClear = settlementAccepted != 0 && settlementFirstClear != 0;
            bool establishBase = projection.MissionId.Equals(
                new FixedString64Bytes("saga.ch01.m02.establish_base"));
            ref BlobArray<CampaignMissionRewardBlob> rewards = ref (
                firstClear ? ref definition.FirstClearRewards : ref definition.ReplayRewards);
            string rewardText = victory ? BuildMissionRewardText(ref rewards) : "NO REWARD";
            int elapsedSeconds = projection.ElapsedMilliseconds / 1000;
            string subtitle = establishBase
                ? "ESTABLISH THE BASE • FORWARD POST"
                : "FIRST CONTACT • OLD MARKET";
            string summary = establishBase
                ? victory
                    ? firstClear
                        ? "Forward post operational. Dalia Rahim accepts field-lead duty. The clinic-route warning sector has gone dark."
                        : "Forward post defended. The clinic route remains under coalition control."
                    : "The forward post fell before the defense was secured. Rebuild and redeploy."
                : victory
                    ? "Hostile patrol neutralized. The Old Market corridor is secure."
                    : "The command squad was lost. Regroup and redeploy.";
            bool debriefRequired = establishBase && victory &&
                                   runtime.Phase != MissionPhaseKind.ResultAfterDebrief;
            if (debriefRequired)
                return false;
            result = new UiMissionResultPopupModel(
                projection.SourceVersion,
                projection.MissionId.ToString(),
                victory ? UiMissionResultOutcome.Victory : UiMissionResultOutcome.Loss,
                victory ? "VICTORY" : "MISSION FAILED",
                subtitle,
                summary,
                projection.Stars,
                $"{elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}",
                projection.SquadLossCount.ToString(),
                $"{facts.HostileDefeatedCount}/{facts.HostileTotalCount}",
                rewardText,
                victory ? "CONTINUE" : "RETRY",
                !victory || settlementAccepted != 0,
                !victory,
                firstClear,
                debriefRequired);
            cachedMissionResultVersion = projection.SourceVersion;
            cachedMissionSettlementAccepted = settlementAccepted;
            cachedMissionSettlementFirstClear = settlementFirstClear;
            cachedMissionResult = result;
            cachedMissionResultWorld = entityManager.World;
            cachedMissionResultRoot = root;
            cachedMissionResultSession = projection.SessionToken;
            cachedMissionResultAttempt = projection.AttemptOrdinal;
            return true;
        }

        private static bool TryFindMissionDefinition(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionRuntimeComponent runtime,
            out int definitionIndex)
        {
            definitionIndex = -1;
            if (!catalog.Blob.IsCreated)
                return false;

            ref BlobArray<CampaignMissionDefinitionBlob> missions = ref catalog.Blob.Value.Missions;
            for (int index = 0; index < missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob candidate = ref missions[index];
                if (candidate.MissionId.Equals(runtime.MissionId) &&
                    candidate.ScenarioId.Equals(runtime.ScenarioId) &&
                    candidate.OperationMapId.Equals(runtime.OperationMapId))
                {
                    definitionIndex = index;
                    return true;
                }
            }

            return false;
        }

        private static string BuildMissionRewardText(ref BlobArray<CampaignMissionRewardBlob> rewards)
        {
            if (rewards.Length == 0)
                return "No reward";
            string text = string.Empty;
            for (int index = 0; index < rewards.Length; index++)
            {
                ref CampaignMissionRewardBlob reward = ref rewards[index];
                if (index > 0) text += "  ·  ";
                string label = reward.Kind != MissionRewardKind.None
                    ? reward.Kind.ToString().ToUpperInvariant()
                    : reward.RewardConfigId.Equals(
                        new FixedString64Bytes("reward.commander_xp"))
                        ? "COMMANDER XP"
                        : reward.RewardConfigId.Equals(
                            new FixedString64Bytes("reward.ch01.m02.production_unlock"))
                            ? "BARRACKS UNLOCK"
                            : "REWARD";
                text += $"{reward.Amount:N0} {label}";
            }
            return text;
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
            if (!TryGetMissionBriefingBoundary(out EntityManager entityManager, out Entity boundary))
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
                    source.MissionRoleId.ToString(), source.TargetConfigId.ToString(),
                    source.Rule, source.RequiredCount,
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
                component.StartingCredits, component.StartingMaterials,
                component.AllowedBuildingConfigId.ToString(), component.AllowedBuildingCount,
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
