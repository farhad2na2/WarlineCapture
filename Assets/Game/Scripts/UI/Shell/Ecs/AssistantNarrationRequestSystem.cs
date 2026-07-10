using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantMessagePrioritySystem))]
    public partial struct AssistantNarrationRequestSystem : ISystem
    {
        private const int MaxNarrationRows = 8;
        private const float LowPriorityCooldownSeconds = 12f;
        private const float ThreatSuppressionSeconds = 8f;
        private const int ThreatMessageBaseId = 810000;
        private const int CommandMessageBaseId = 820000;

        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudStatusSurfacesComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            matchStartQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>(),
                ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            EnsureNarrationBoundary(ref state, boundary);

            if (!IsNarrationRuntimeActive(state.EntityManager, boundary, matchStartQuery))
            {
                ClearNarrationBoundary(state.EntityManager, boundary);
                return;
            }

            AssistantSettingsComponent settings =
                state.EntityManager.GetComponentData<AssistantSettingsComponent>(boundary);
            AssistantNarrationStateComponent narrationState =
                state.EntityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
            if (narrationState.Mode != settings.NarrationMode)
            {
                narrationState.Mode = settings.NarrationMode;
                narrationState.Version = NextVersion(narrationState.Version);
                narrationState.UiDirty = 1;
                state.EntityManager.SetComponentData(boundary, narrationState);
            }

            DynamicBuffer<AssistantMessageElement> messages =
                state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);
            if (messages.Length == 0)
                return;

            float now = Time.unscaledTime;
            if (!TryFindBestMessage(messages, settings.NarrationMode, narrationState, now, out AssistantMessageElement message))
                return;

            DynamicBuffer<AssistantNarrationRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
            if (HasMatchingRequest(requests, messages, message, now))
                return;

            int currentFrame = Mathf.Max(1, Time.frameCount);
            int requestId = BuildRequestId(message, currentFrame);
            requests.Add(new AssistantNarrationRequestElement
            {
                RequestId = requestId,
                MessageId = message.MessageId,
                Priority = message.Priority,
                Status = AssistantCommandIntentStatus.Pending,
                Text = message.Text,
                AudioEventId = message.AudioEventId,
                AudioEventHash = ResolveAudioEventHash(message.AudioEventId),
                RequestedAt = now,
                InterruptsLowerPriority = message.Priority >= AssistantMessagePriority.High ? (byte)1 : (byte)0
            });
            TrimRequests(requests);

            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.ActiveNarrationId = requestId;
            narrationState.ActiveAudioPlaybackRequestId = 0;
            if (message.Priority <= AssistantMessagePriority.Normal)
                narrationState.LowPriorityCooldownUntil = now + LowPriorityCooldownSeconds;
            narrationState.Mode = settings.NarrationMode;
            narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Pending;
            narrationState.LastAudioFailureReason = default;
            narrationState.LastPresentedAt = 0f;
            narrationState.IsSpeaking = 0;
            narrationState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, narrationState);

            AssistantStateComponent assistantState =
                state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            assistantState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, assistantState);
        }

        internal static void EnsureNarrationBoundary(ref SystemState state, Entity boundary)
        {
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<AssistantSettingsComponent>(boundary))
                em.AddComponentData(boundary, AssistantSettingsPersistenceSystemHelper.LoadSettingsComponent());

            if (!em.HasComponent<AssistantNarrationStateComponent>(boundary))
            {
                AssistantSettingsComponent settings = em.GetComponentData<AssistantSettingsComponent>(boundary);
                em.AddComponentData(boundary, new AssistantNarrationStateComponent
                {
                    Version = 1,
                    Mode = settings.NarrationMode,
                    UiDirty = 1
                });
            }

            if (!em.HasBuffer<AssistantNarrationRequestElement>(boundary))
                em.AddBuffer<AssistantNarrationRequestElement>(boundary);
        }

        internal static bool IsNarrationRuntimeActive(
            EntityManager em,
            Entity boundary,
            EntityQuery matchStartQuery)
        {
            UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            if (shellState.ActiveRoute != UIRoute.Match ||
                shellState.CurrentMode != UiShellMode.MatchHud ||
                shellState.IsTransitionRunning != 0 ||
                matchStartQuery.CalculateEntityCount() != 1)
                return false;

            MatchStartQueueComponent matchStart = matchStartQuery.GetSingleton<MatchStartQueueComponent>();
            return matchStart.HasStarted != 0 && matchStart.IsStartPending == 0;
        }

        internal static bool ClearNarrationBoundary(EntityManager em, Entity boundary)
        {
            bool changed = false;
            if (em.HasBuffer<AssistantNarrationRequestElement>(boundary))
            {
                DynamicBuffer<AssistantNarrationRequestElement> requests =
                    em.GetBuffer<AssistantNarrationRequestElement>(boundary);
                if (requests.Length > 0)
                {
                    requests.Clear();
                    changed = true;
                }
            }

            if (!em.HasComponent<AssistantNarrationStateComponent>(boundary))
                return changed;

            AssistantNarrationStateComponent narrationState =
                em.GetComponentData<AssistantNarrationStateComponent>(boundary);
            AssistantNarrationMode mode = em.HasComponent<AssistantSettingsComponent>(boundary)
                ? em.GetComponentData<AssistantSettingsComponent>(boundary).NarrationMode
                : narrationState.Mode;
            changed |= narrationState.ActiveNarrationId != 0 ||
                       narrationState.ActiveAudioPlaybackRequestId != 0 ||
                       narrationState.LastSpokenMessageId != 0 ||
                       narrationState.LastSpokenAt != 0f ||
                       narrationState.LowPriorityCooldownUntil != 0f ||
                       narrationState.LastPresentedAt != 0f ||
                       narrationState.Mode != mode ||
                       narrationState.LastAudioStatus != AudioPlaybackRequestStatus.Pending ||
                       narrationState.LastAudioFailureReason.Length != 0 ||
                       narrationState.IsSpeaking != 0;
            if (!changed)
                return false;

            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.ActiveNarrationId = 0;
            narrationState.ActiveAudioPlaybackRequestId = 0;
            narrationState.LastSpokenMessageId = 0;
            narrationState.LastSpokenAt = 0f;
            narrationState.LowPriorityCooldownUntil = 0f;
            narrationState.LastPresentedAt = 0f;
            narrationState.Mode = mode;
            narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Pending;
            narrationState.LastAudioFailureReason = default;
            narrationState.IsSpeaking = 0;
            narrationState.UiDirty = 1;
            em.SetComponentData(boundary, narrationState);
            return true;
        }

        private static bool TryFindBestMessage(
            DynamicBuffer<AssistantMessageElement> messages,
            AssistantNarrationMode mode,
            AssistantNarrationStateComponent narrationState,
            float now,
            out AssistantMessageElement best)
        {
            best = default;
            bool found = false;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (!IsEligible(message, mode, narrationState, now))
                    continue;

                if (!found
                    || message.Priority > best.Priority
                    || (message.Priority == best.Priority && message.MessageId < best.MessageId))
                {
                    best = message;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsEligible(
            AssistantMessageElement message,
            AssistantNarrationMode mode,
            AssistantNarrationStateComponent narrationState,
            float now)
        {
            if (message.RequiresNarration == 0 || message.Acknowledged != 0 || message.Text.Length == 0)
                return false;

            if (message.Priority <= AssistantMessagePriority.Normal &&
                now < narrationState.LowPriorityCooldownUntil)
            {
                return false;
            }

            return mode switch
            {
                AssistantNarrationMode.Off => false,
                AssistantNarrationMode.CriticalOnly => message.Priority == AssistantMessagePriority.Critical,
                AssistantNarrationMode.Important => message.Priority >= AssistantMessagePriority.High,
                AssistantNarrationMode.All => true,
                _ => false
            };
        }

        private static bool HasMatchingRequest(
            DynamicBuffer<AssistantNarrationRequestElement> requests,
            DynamicBuffer<AssistantMessageElement> messages,
            AssistantMessageElement message,
            float now)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantNarrationRequestElement request = requests[i];
                if ((request.MessageId == message.MessageId ||
                     HasMatchingSuppressionKey(messages, request.MessageId, message.SuppressionKey)) &&
                    IsRequestSuppressed(request, message, now))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRequestSuppressed(
            AssistantNarrationRequestElement request,
            AssistantMessageElement message,
            float now)
        {
            bool threatMessage = message.MessageId >= ThreatMessageBaseId &&
                                 message.MessageId < CommandMessageBaseId;
            return !threatMessage || now - request.RequestedAt < ThreatSuppressionSeconds;
        }

        private static bool HasMatchingSuppressionKey(
            DynamicBuffer<AssistantMessageElement> messages,
            int requestMessageId,
            FixedString64Bytes suppressionKey)
        {
            if (suppressionKey.Length == 0)
                return false;

            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (message.MessageId == requestMessageId &&
                    message.SuppressionKey.Equals(suppressionKey))
                {
                    return true;
                }
            }

            return false;
        }

        private static int BuildRequestId(AssistantMessageElement message, int currentFrame)
        {
            int sourceVersion = message.SourceVersion > 0 ? message.SourceVersion : currentFrame;
            return message.MessageId * 31 + sourceVersion;
        }

        private static uint ResolveAudioEventHash(FixedString64Bytes audioEventId)
        {
            return audioEventId.Length == 0
                ? 0u
                : AudioEventIds.StableHash(audioEventId.ToString());
        }

        private static uint NextVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }

        private static void TrimRequests(DynamicBuffer<AssistantNarrationRequestElement> requests)
        {
            while (requests.Length > MaxNarrationRows)
                requests.RemoveAt(0);
        }
    }
}
