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

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationRequestElement>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);

            Entity boundary = boundaryQuery.GetSingletonEntity();
            DynamicBuffer<AssistantNarrationRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);

            bool changed = false;
            float now = Time.time;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantNarrationRequestElement request = requests[i];
                if (request.Status != AssistantCommandIntentStatus.Pending)
                    continue;

                request.Status = TryEnqueueVoiceRequest(state.EntityManager, request, now)
                    ? AssistantCommandIntentStatus.Completed
                    : AssistantCommandIntentStatus.Rejected;
                requests[i] = request;
                changed = true;
            }

            if (!changed)
                return;

            AssistantNarrationStateComponent narrationState =
                state.EntityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.IsSpeaking = 0;
            narrationState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, narrationState);
        }

        private static bool TryEnqueueVoiceRequest(
            EntityManager em,
            AssistantNarrationRequestElement request,
            float now)
        {
            if (request.AudioEventId.Length == 0)
                return false;

            uint eventHash = request.AudioEventHash != 0u
                ? request.AudioEventHash
                : AudioEventIds.StableHash(request.AudioEventId.ToString());

            AudioEventRequestSystem.EnqueueOneShot(
                em,
                request.AudioEventId,
                eventHash,
                new FixedString32Bytes("Voice"),
                ToAudioPriority(request.Priority),
                now,
                cooldownSeconds: DefaultVoiceCooldownSeconds);
            return true;
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
