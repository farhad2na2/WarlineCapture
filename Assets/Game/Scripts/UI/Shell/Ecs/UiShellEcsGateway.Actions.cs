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
        private static class UiShellActionAdapter
        {
        public static bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
        {
            if (kind == UiActionKind.None ||
                !TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            {
                return false;
            }

            EnsureUiActionRequestBuffer(entityManager, boundary);
            DynamicBuffer<UiActionRequestComponent> requests =
                entityManager.GetBuffer<UiActionRequestComponent>(boundary);
            requests.Add(new UiActionRequestComponent
            {
                Kind = kind,
                PayloadId = payloadId
            });
            return true;
        }

        public static bool TryEnqueueMissionResultAction(UiMissionResultActionKind action)
        {
            if (action is not (UiMissionResultActionKind.Retry or UiMissionResultActionKind.Continue) ||
                !TryGetMissionRoot(out EntityManager entityManager, out Entity root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasBuffer<CampaignMissionActionRequestElement>(root))
                return false;

            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (runtime.Phase != MissionPhaseKind.Result ||
                action == UiMissionResultActionKind.Retry && runtime.Outcome != MissionOutcomeKind.Defeat ||
                action == UiMissionResultActionKind.Continue && runtime.Outcome != MissionOutcomeKind.Victory)
                return false;

            MissionActionKind missionAction = action == UiMissionResultActionKind.Retry
                ? MissionActionKind.Retry : MissionActionKind.Continue;

            DynamicBuffer<CampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionActionRequestElement>(root);
            for (int index = 0; index < requests.Length; index++)
                if (requests[index].Action == missionAction &&
                    requests[index].TransitionToken == runtime.TransitionToken &&
                    requests[index].SessionToken.Equals(runtime.SessionToken) &&
                    requests[index].AttemptOrdinal == runtime.AttemptOrdinal)
                    return false;

            requests.Add(new CampaignMissionActionRequestElement
            {
                Action = missionAction,
                TransitionToken = runtime.TransitionToken,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                ReplayTutorialEnabled = runtime.ReplayTutorialEnabled
            });
            return true;
        }

        public static bool TryEnqueueCampaignMissionAction(
            UiCampaignMissionActionKind action, string missionId, bool value = false)
        {
            if (action == UiCampaignMissionActionKind.None || string.IsNullOrWhiteSpace(missionId) ||
                missionId.Length > 60 || !TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            FixedString64Bytes fixedMissionId = new(missionId.Trim());
            if (action != UiCampaignMissionActionKind.Refresh)
            {
                if (!entityManager.HasComponent<UiCampaignOperationsComponent>(boundary)) return false;
                UiCampaignOperationsComponent campaign =
                    entityManager.GetComponentData<UiCampaignOperationsComponent>(boundary);
                if (campaign.Available == 0 || !campaign.SelectedMissionId.Equals(fixedMissionId)) return false;
            }

            if (!entityManager.HasBuffer<UiCampaignMissionActionRequestElement>(boundary))
                entityManager.AddBuffer<UiCampaignMissionActionRequestElement>(boundary);
            entityManager.GetBuffer<UiCampaignMissionActionRequestElement>(boundary).Add(
                new UiCampaignMissionActionRequestElement
                {
                    Action = action,
                    MissionId = fixedMissionId,
                    Value = value ? (byte)1 : (byte)0
                });
            return true;
        }

        public static bool TryEnqueueAssistantCommandIntent(
            UiAssistantCommandIntentKind kind,
            bool fromTakeover)
        {
            if (kind == UiAssistantCommandIntentKind.None ||
                !TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            {
                return false;
            }

            if (!IsAssistantRuntimeActive(entityManager, boundary))
                return false;

            if (kind == UiAssistantCommandIntentKind.StopAssistantControl)
            {
                EnsureAssistantCommandIntentBuffers(entityManager, boundary);
                DynamicBuffer<AssistantCommandIntentRequestElement> stopRequests =
                    entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
                DynamicBuffer<AssistantCommandIntentResultElement> stopResults =
                    entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
                stopRequests.Add(new AssistantCommandIntentRequestElement
                {
                    RequestId = NextAssistantCommandIntentRequestId(stopRequests, stopResults),
                    Frame = Time.frameCount,
                    RecommendationId = 0,
                    Kind = AssistantCommandIntentKind.StopAssistantControl,
                    TargetKind = AssistantTargetKind.None,
                    FromTakeover = fromTakeover ? (byte)1 : (byte)0
                });
                return true;
            }

            if (fromTakeover && !AssistantSettingsPersistenceSystemHelper.TakeoverAllowed(entityManager, boundary))
                return false;

            if (!entityManager.HasBuffer<AssistantRecommendationElement>(boundary))
                return false;

            DynamicBuffer<AssistantRecommendationElement> recommendations =
                entityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);
            if (recommendations.Length == 0 || recommendations[0].RecommendationId == 0)
                return false;

            AssistantRecommendationElement recommendation = recommendations[0];
            AssistantCommandIntentKind ecsKind = ToAssistantCommandIntentKind(kind, recommendation.Kind);
            if (ecsKind == AssistantCommandIntentKind.None)
                return false;

            if (ecsKind == AssistantCommandIntentKind.ShowRecommendation && recommendation.CanShow == 0)
                return false;
            if (kind == UiAssistantCommandIntentKind.ExecuteRecommendation && recommendation.CanExecute == 0)
                return false;

            EnsureAssistantCommandIntentBuffers(entityManager, boundary);
            DynamicBuffer<AssistantCommandIntentRequestElement> requests =
                entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
            requests.Add(new AssistantCommandIntentRequestElement
            {
                RequestId = NextAssistantCommandIntentRequestId(requests, results),
                Frame = Time.frameCount,
                RecommendationId = recommendation.RecommendationId,
                RecommendationSourceVersion = recommendation.SourceVersion,
                Kind = ecsKind,
                RecommendationKind = recommendation.Kind,
                TargetKind = recommendation.TargetKind,
                SourceEntity = recommendation.SourceEntity,
                TargetEntity = recommendation.TargetEntity,
                TargetCell = recommendation.TargetCell,
                WorldPosition = recommendation.WorldPosition,
                TargetId = recommendation.TargetId,
                FromTakeover = fromTakeover ? (byte)1 : (byte)0
            });
            return true;
        }

        private static AssistantCommandIntentKind ToAssistantCommandIntentKind(
            UiAssistantCommandIntentKind kind,
            AssistantRecommendationKind recommendationKind)
        {
            return kind switch
            {
                UiAssistantCommandIntentKind.ShowRecommendation => AssistantCommandIntentKind.ShowRecommendation,
                UiAssistantCommandIntentKind.ExecuteRecommendation => ToExecutableIntentKind(recommendationKind),
                UiAssistantCommandIntentKind.StopAssistantControl => AssistantCommandIntentKind.StopAssistantControl,
                _ => AssistantCommandIntentKind.None
            };
        }

        private static AssistantCommandIntentKind ToExecutableIntentKind(AssistantRecommendationKind recommendationKind)
        {
            return recommendationKind switch
            {
                AssistantRecommendationKind.Select => AssistantCommandIntentKind.SelectEntity,
                AssistantRecommendationKind.Move => AssistantCommandIntentKind.MoveToWorldPosition,
                AssistantRecommendationKind.Attack => AssistantCommandIntentKind.AttackEntity,
                AssistantRecommendationKind.CameraFocus => AssistantCommandIntentKind.FocusCamera,
                AssistantRecommendationKind.Stop => AssistantCommandIntentKind.StopAssistantControl,
                _ => AssistantCommandIntentKind.None
            };
        }

        public static bool TrySetLoadingProgress(float progress01, string status, bool complete)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureLoadingProgressRequestBuffer(entityManager, boundary);
            DynamicBuffer<UiShellLoadingProgressRequestComponent> requests =
                entityManager.GetBuffer<UiShellLoadingProgressRequestComponent>(boundary);
            requests.Add(new UiShellLoadingProgressRequestComponent
            {
                Progress01 = Mathf.Clamp01(progress01),
                Status = new FixedString64Bytes(status ?? string.Empty),
                IsComplete = complete ? (byte)1 : (byte)0
            });
            return true;
        }

        internal static bool IsAssistantRuntimeActive(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
                return false;

            UiShellStateComponent shell = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            if (shell.ActiveRoute != UIRoute.Match ||
                shell.CurrentMode != UiShellMode.MatchHud ||
                shell.IsTransitionRunning != 0)
            {
                return false;
            }

            if (!hasAssistantMatchStartQuery || cachedWorld != entityManager.World)
            {
                assistantMatchStartQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<MatchStartQueueComponent>());
                hasAssistantMatchStartQuery = true;
            }

            return !assistantMatchStartQuery.IsEmptyIgnoreFilter &&
                   assistantMatchStartQuery.GetSingleton<MatchStartQueueComponent>().HasStarted != 0;
        }

        private static void EnsureUiActionRequestBuffer(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<UiActionRequestComponent>(boundary))
                entityManager.AddBuffer<UiActionRequestComponent>(boundary);
        }

        private static void EnsureAssistantCommandIntentBuffers(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary))
                entityManager.AddBuffer<AssistantCommandIntentRequestElement>(boundary);

            if (!entityManager.HasBuffer<AssistantCommandIntentResultElement>(boundary))
                entityManager.AddBuffer<AssistantCommandIntentResultElement>(boundary);

            if (!entityManager.HasBuffer<AssistantCommandDispatchElement>(boundary))
                entityManager.AddBuffer<AssistantCommandDispatchElement>(boundary);

            if (!entityManager.HasBuffer<AssistantPreviewHighlightElement>(boundary))
                entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary);
        }

        private static int NextAssistantCommandIntentRequestId(
            DynamicBuffer<AssistantCommandIntentRequestElement> requests,
            DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            int requestId = 0;
            for (int i = 0; i < requests.Length; i++)
                requestId = math.max(requestId, requests[i].RequestId);
            for (int i = 0; i < results.Length; i++)
                requestId = math.max(requestId, results[i].RequestId);

            return requestId + 1;
        }

        private static void EnsureLoadingProgressRequestBuffer(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasBuffer<UiShellLoadingProgressRequestComponent>(boundary))
                entityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        }


        }
    }
}
