using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantRecommendationSystem))]
    public partial struct AssistantMessagePrioritySystem : ISystem
    {
        public const int ThreatMessageId = 810001;
        public const int FeedbackMessageId = 810002;

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

            UiMatchHudStatusSurfacesComponent status =
                state.EntityManager.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            DynamicBuffer<AssistantMessageElement> messages =
                state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);

            int sourceVersion = math.max(1, Time.frameCount);
            bool changed = UpsertOrRemoveThreat(messages, status, sourceVersion);
            changed |= UpsertOrRemoveFeedback(messages, status, sourceVersion);

            if (!changed)
                return;

            AssistantStateComponent assistantState =
                state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            assistantState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, assistantState);
        }

        private static bool UpsertOrRemoveThreat(
            DynamicBuffer<AssistantMessageElement> messages,
            UiMatchHudStatusSurfacesComponent status,
            int sourceVersion)
        {
            if (status.ThreatVisible == 0 || status.ThreatTitle.Length == 0)
                return RemoveMessage(messages, ThreatMessageId);

            FixedString128Bytes text = new FixedString128Bytes(status.ThreatTitle);
            if (status.ThreatSubtitle.Length > 0)
            {
                text.Append(": ");
                text.Append(status.ThreatSubtitle);
            }

            return UpsertMessage(
                messages,
                ThreatMessageId,
                sourceVersion,
                AssistantMessagePriority.High,
                AssistantRecommendationKind.DefensiveAlert,
                new FixedString64Bytes("assistant.threat"),
                text,
                new FixedString64Bytes("aria.threat"),
                requiresNarration: 1);
        }

        private static bool UpsertOrRemoveFeedback(
            DynamicBuffer<AssistantMessageElement> messages,
            UiMatchHudStatusSurfacesComponent status,
            int sourceVersion)
        {
            if (status.FeedbackVisible == 0 || status.FeedbackText.Length == 0)
                return RemoveMessage(messages, FeedbackMessageId);

            return UpsertMessage(
                messages,
                FeedbackMessageId,
                sourceVersion,
                AssistantMessagePriority.Normal,
                AssistantRecommendationKind.Explain,
                new FixedString64Bytes("assistant.feedback"),
                new FixedString128Bytes(status.FeedbackText),
                default,
                requiresNarration: 0);
        }

        private static bool UpsertMessage(
            DynamicBuffer<AssistantMessageElement> messages,
            int messageId,
            int sourceVersion,
            AssistantMessagePriority priority,
            AssistantRecommendationKind relatedKind,
            FixedString64Bytes suppressionKey,
            FixedString128Bytes text,
            FixedString64Bytes audioEventId,
            byte requiresNarration)
        {
            int index = FindMessage(messages, messageId);
            if (index < 0)
            {
                messages.Add(new AssistantMessageElement
                {
                    MessageId = messageId,
                    SourceVersion = sourceVersion,
                    Priority = priority,
                    RelatedKind = relatedKind,
                    SuppressionKey = suppressionKey,
                    Text = text,
                    AudioEventId = audioEventId,
                    CreatedAt = sourceVersion,
                    ExpiresAt = 0f,
                    RequiresNarration = requiresNarration,
                    Acknowledged = 0
                });
                return true;
            }

            AssistantMessageElement current = messages[index];
            if (current.Priority == priority
                && current.RelatedKind == relatedKind
                && current.SuppressionKey.Equals(suppressionKey)
                && current.Text.Equals(text)
                && current.AudioEventId.Equals(audioEventId)
                && current.RequiresNarration == requiresNarration
                && current.Acknowledged == 0)
            {
                return false;
            }

            current.SourceVersion = sourceVersion;
            current.Priority = priority;
            current.RelatedKind = relatedKind;
            current.SuppressionKey = suppressionKey;
            current.Text = text;
            current.AudioEventId = audioEventId;
            current.CreatedAt = current.CreatedAt > 0f ? current.CreatedAt : sourceVersion;
            current.ExpiresAt = 0f;
            current.RequiresNarration = requiresNarration;
            current.Acknowledged = 0;
            messages[index] = current;
            return true;
        }

        private static bool RemoveMessage(DynamicBuffer<AssistantMessageElement> messages, int messageId)
        {
            int index = FindMessage(messages, messageId);
            if (index < 0)
                return false;

            messages.RemoveAt(index);
            return true;
        }

        private static int FindMessage(DynamicBuffer<AssistantMessageElement> messages, int messageId)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].MessageId == messageId)
                    return i;
            }

            return -1;
        }
    }
}
