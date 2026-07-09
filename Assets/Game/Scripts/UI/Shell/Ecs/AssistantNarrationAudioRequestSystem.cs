using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
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
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationRequestElement>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            UiShellStateComponent shellState =
                state.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
            if (!AllowsMatchNarration(shellState))
                return;

            DynamicBuffer<AssistantNarrationRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);

            float now = Time.unscaledTime;
            using NativeList<PendingNarrationAudioRequest> pendingAudio = new(Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantNarrationRequestElement request = requests[i];
                if (request.Status != AssistantCommandIntentStatus.Pending)
                    continue;

                if (request.AudioEventId.Length == 0)
                {
                    request.Status = AssistantCommandIntentStatus.Rejected;
                    requests[i] = request;
                    continue;
                }

                FixedString64Bytes eventId = request.AudioEventId;
                pendingAudio.Add(new PendingNarrationAudioRequest
                {
                    EventId = eventId,
                    EventHash = AudioEventIds.StableHash(eventId.ToString()),
                    Priority = ResolvePriority(request.Priority)
                });
                request.Status = AssistantCommandIntentStatus.Accepted;
                requests[i] = request;
            }

            for (int i = 0; i < pendingAudio.Length; i++)
            {
                PendingNarrationAudioRequest request = pendingAudio[i];
                AudioEventRequestSystem.EnqueueOneShot(
                    state.EntityManager,
                    request.EventId,
                    request.EventHash,
                    new FixedString32Bytes("Voice"),
                    request.Priority,
                    now);
            }
        }

        private static AudioPlaybackPriority ResolvePriority(AssistantMessagePriority priority)
        {
            return priority switch
            {
                AssistantMessagePriority.Critical => AudioPlaybackPriority.Critical,
                AssistantMessagePriority.High => AudioPlaybackPriority.High,
                AssistantMessagePriority.Normal => AudioPlaybackPriority.Medium,
                _ => AudioPlaybackPriority.Low
            };
        }

        private static bool AllowsMatchNarration(UiShellStateComponent shellState)
        {
            return shellState.ActiveRoute == UIRoute.Match &&
                   (shellState.CurrentMode == UiShellMode.MatchHud ||
                    shellState.CurrentMode == UiShellMode.PopupOnly);
        }

        private struct PendingNarrationAudioRequest
        {
            public FixedString64Bytes EventId;
            public uint EventHash;
            public AudioPlaybackPriority Priority;
        }
    }
}
