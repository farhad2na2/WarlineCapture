using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantNarrationRequestSystem))]
    public partial struct AssistantNarrationAudioRequestSystem : ISystem
    {
        private const float DefaultVoiceCooldownSeconds = 0.6f;

        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationRequestElement>());
            matchStartQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>(),
                ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            if (!AssistantNarrationRequestSystem.IsNarrationRuntimeActive(
                    state.EntityManager,
                    boundary,
                    matchStartQuery))
            {
                AssistantNarrationRequestSystem.ClearNarrationBoundary(state.EntityManager, boundary);
                return;
            }

            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);

            DynamicBuffer<AssistantNarrationRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
            AssistantSettingsComponent settings =
                state.EntityManager.GetComponentData<AssistantSettingsComponent>(boundary);
            AssistantNarrationStateComponent narrationState =
                state.EntityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);

            if (settings.NarrationMode == AssistantNarrationMode.Off)
            {
                bool offStateChanged = CancelPendingRequests(requests) ||
                                       narrationState.ActiveAudioPlaybackRequestId != 0 ||
                                       narrationState.Mode != AssistantNarrationMode.Off ||
                                       narrationState.LastAudioStatus != AudioPlaybackRequestStatus.Pending ||
                                       narrationState.LastAudioFailureReason.Length != 0 ||
                                       narrationState.LastPresentedAt != 0f ||
                                       narrationState.IsSpeaking != 0;
                if (!offStateChanged)
                    return;

                narrationState.Version = NextVersion(narrationState.Version);
                narrationState.ActiveAudioPlaybackRequestId = 0;
                narrationState.Mode = AssistantNarrationMode.Off;
                narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Pending;
                narrationState.LastAudioFailureReason = default;
                narrationState.LastPresentedAt = 0f;
                narrationState.IsSpeaking = 0;
                narrationState.UiDirty = 1;
                state.EntityManager.SetComponentData(boundary, narrationState);
                return;
            }

            bool changed = false;
            float now = Time.unscaledTime;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantNarrationRequestElement request = requests[i];
                if (request.Status != AssistantCommandIntentStatus.Pending)
                    continue;

                int audioPlaybackRequestId = EnqueueVoiceRequest(state.EntityManager, request, now);
                request.AudioPlaybackRequestId = audioPlaybackRequestId;
                request.Status = audioPlaybackRequestId > 0 || request.AudioEventId.Length == 0
                    ? AssistantCommandIntentStatus.Accepted
                    : AssistantCommandIntentStatus.Rejected;
                requests[i] = request;
                changed = true;

                if (request.RequestId != narrationState.ActiveNarrationId)
                    continue;

                narrationState.ActiveAudioPlaybackRequestId = audioPlaybackRequestId;
                narrationState.LastAudioStatus = audioPlaybackRequestId > 0
                    ? AudioPlaybackRequestStatus.Pending
                    : request.AudioEventId.Length == 0
                        ? AudioPlaybackRequestStatus.Pending
                        : AudioPlaybackRequestStatus.Rejected;
                narrationState.LastAudioFailureReason = audioPlaybackRequestId <= 0 && request.AudioEventId.Length > 0
                    ? new FixedString64Bytes("Voice request unavailable")
                    : default;
                narrationState.LastPresentedAt = 0f;
            }

            if (!changed)
                return;

            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.Mode = settings.NarrationMode;
            narrationState.IsSpeaking = 0;
            narrationState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, narrationState);
        }

        private static int EnqueueVoiceRequest(
            EntityManager em,
            AssistantNarrationRequestElement request,
            float now)
        {
            if (request.AudioEventId.Length == 0)
                return 0;

            uint eventHash = request.AudioEventHash != 0u
                ? request.AudioEventHash
                : AudioEventIds.StableHash(request.AudioEventId.ToString());

            return AudioEventRequestSystem.EnqueueOneShot(
                em,
                request.AudioEventId,
                eventHash,
                new FixedString32Bytes("Voice"),
                ToAudioPriority(request.Priority),
                now,
                cooldownSeconds: DefaultVoiceCooldownSeconds,
                interruptsLowerPriority: request.InterruptsLowerPriority != 0);
        }

        private static bool CancelPendingRequests(DynamicBuffer<AssistantNarrationRequestElement> requests)
        {
            bool changed = false;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantNarrationRequestElement request = requests[i];
                if (request.Status != AssistantCommandIntentStatus.Pending)
                    continue;

                request.Status = AssistantCommandIntentStatus.Cancelled;
                requests[i] = request;
                changed = true;
            }

            return changed;
        }

        private static AudioPlaybackPriority ToAudioPriority(AssistantMessagePriority priority)
        {
            return priority switch
            {
                AssistantMessagePriority.Critical => AudioPlaybackPriority.Critical,
                AssistantMessagePriority.High => AudioPlaybackPriority.High,
                AssistantMessagePriority.Normal => AudioPlaybackPriority.Medium,
                _ => AudioPlaybackPriority.Low
            };
        }

        private static uint NextVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }
    }
}
