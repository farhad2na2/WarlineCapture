using Game.Components;
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

        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudStatusSurfacesComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            EnsureNarrationBoundary(ref state, boundary);

            DynamicBuffer<AssistantMessageElement> messages =
                state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);
            if (messages.Length == 0)
                return;

            AssistantSettingsComponent settings =
                state.EntityManager.GetComponentData<AssistantSettingsComponent>(boundary);
            AssistantNarrationStateComponent narrationState =
                state.EntityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
            float now = Time.time;
            if (!TryFindBestMessage(messages, settings.NarrationMode, narrationState, now, out AssistantMessageElement message))
                return;

            DynamicBuffer<AssistantNarrationRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
            if (HasMatchingRequest(requests, messages, message))
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
                RequestedAt = now,
                InterruptsLowerPriority = message.Priority >= AssistantMessagePriority.High ? (byte)1 : (byte)0
            });
            TrimRequests(requests);

            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.ActiveNarrationId = requestId;
            narrationState.LastSpokenMessageId = message.MessageId;
            narrationState.LastSpokenAt = now;
            if (message.Priority <= AssistantMessagePriority.Normal)
                narrationState.LowPriorityCooldownUntil = now + LowPriorityCooldownSeconds;
            narrationState.Mode = settings.NarrationMode;
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
            {
                em.AddComponentData(boundary, new AssistantSettingsComponent
                {
                    GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
                    NarrationMode = AssistantNarrationMode.Important,
                    AllowTakeover = 1,
                    SubtitlesEnabled = 1
                });
            }

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
            AssistantMessageElement message)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].MessageId == message.MessageId ||
                    HasMatchingSuppressionKey(messages, requests[i].MessageId, message.SuppressionKey))
                {
                    return true;
                }
            }

            return false;
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
