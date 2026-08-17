using Game.Components;
using Game.Configs;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantThreatReadModelSystem))]
    [UpdateAfter(typeof(AssistantCommandResultBridgeSystem))]
    [UpdateAfter(typeof(AssistantTargetLockReadModelSystem))]
    public partial struct AssistantMessagePrioritySystem : ISystem
    {
        private const int MaxMessageRows = 16;
        private const int ThreatMessageBaseId = 810000;
        private const int CommandMessageBaseId = 820000;

        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            matchStartQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            if (!AssistantRuntimeStateUtility.IsActive(state.EntityManager, boundary, matchStartQuery))
                return;

            DynamicBuffer<AssistantMessageElement> messages =
                state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);
            DynamicBuffer<AssistantThreatReadModelElement> threats =
                state.EntityManager.GetBuffer<AssistantThreatReadModelElement>(boundary, true);
            DynamicBuffer<AssistantCommandIntentResultElement> commandResults =
                state.EntityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
            AssistantMessageReadModelComponent readModel =
                state.EntityManager.GetComponentData<AssistantMessageReadModelComponent>(boundary);
            float now = (float)SystemAPI.Time.ElapsedTime;

            bool boundaryReached = readModel.NextAgeBoundaryAt > 0f && now >= readModel.NextAgeBoundaryAt;
            bool changed = RemoveExpired(messages, now);
            changed |= SynchronizeThreatMessages(messages, threats, now);
            changed |= SynchronizeCommandMessages(messages, commandResults, ref readModel, now);
            changed |= TrimMessages(messages);
            float nextBoundary = CalculateNextAgeBoundary(messages, now);

            if (!changed && !boundaryReached && math.abs(nextBoundary - readModel.NextAgeBoundaryAt) < 0.001f)
                return;

            readModel.Version = AssistantRuntimeStateUtility.NextVersion(readModel.Version);
            readModel.VisibleCount = CountVisible(messages, now);
            readModel.NextAgeBoundaryAt = nextBoundary;
            state.EntityManager.SetComponentData(boundary, readModel);

            AssistantStateComponent assistant = state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            assistant.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, assistant);
        }

        private static bool SynchronizeThreatMessages(
            DynamicBuffer<AssistantMessageElement> messages,
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            float now)
        {
            bool changed = false;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (message.MessageId < ThreatMessageBaseId || message.MessageId >= CommandMessageBaseId)
                    continue;

                int threatId = message.MessageId - ThreatMessageBaseId;
                if (!ContainsThreat(threats, threatId))
                {
                    messages.RemoveAt(i--);
                    changed = true;
                }
            }

            for (int i = 0; i < threats.Length; i++)
            {
                AssistantThreatReadModelElement threat = threats[i];
                if (threat.ExpiresAt > 0f && now >= threat.ExpiresAt)
                    continue;
                int normalizedThreatId = math.abs(threat.ThreatId % 10000);
                int messageId = ThreatMessageBaseId + normalizedThreatId;
                FixedString64Bytes suppressionKey = new("assistant.threat.");
                suppressionKey.Append(normalizedThreatId);
                FixedString128Bytes body = BuildThreatBody(threat);
                FixedString64Bytes audioEventId = ThreatAudioEventId(threat.Kind);
                changed |= UpsertMessage(
                    messages,
                    messageId,
                    threat.SourceEventId,
                    threat.Priority,
                    AssistantRecommendationKind.DefensiveAlert,
                    suppressionKey,
                    body,
                    audioEventId,
                    now,
                    threat.ExpiresAt,
                    requiresNarration: audioEventId.Length > 0 ? (byte)1 : (byte)0);
            }

            return changed;
        }

        private static bool SynchronizeCommandMessages(
            DynamicBuffer<AssistantMessageElement> messages,
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            ref AssistantMessageReadModelComponent readModel,
            float now)
        {
            bool changed = false;
            int newestResultVersion = readModel.LastConsumedCommandResultVersion;
            for (int i = 0; i < results.Length; i++)
            {
                AssistantCommandIntentResultElement result = results[i];
                int resultVersion = result.RequestId * 8 + (int)result.Status;
                newestResultVersion = math.max(newestResultVersion, resultVersion);
                if (result.Status != AssistantCommandIntentStatus.Rejected &&
                    result.Status != AssistantCommandIntentStatus.Completed &&
                    result.Status != AssistantCommandIntentStatus.TimedOut)
                {
                    continue;
                }

                int messageId = CommandMessageBaseId + math.abs(result.RequestId % 100000);
                FixedString64Bytes suppressionKey = new("assistant.command.");
                suppressionKey.Append(result.RequestId);
                AssistantMessagePriority priority = result.Status == AssistantCommandIntentStatus.Completed
                    ? AssistantMessagePriority.Low
                    : AssistantMessagePriority.High;
                changed |= UpsertMessage(
                    messages,
                    messageId,
                    resultVersion,
                    priority,
                    result.Status == AssistantCommandIntentStatus.Completed
                        ? AssistantRecommendationKind.Explain
                        : AssistantRecommendationKind.DefensiveAlert,
                    suppressionKey,
                    CopyTo128(result.Message),
                    default,
                    now,
                    now + 6f,
                    requiresNarration: 0);
            }

            if (newestResultVersion != readModel.LastConsumedCommandResultVersion)
            {
                readModel.LastConsumedCommandResultVersion = newestResultVersion;
                changed = true;
            }

            return changed;
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
            float createdAt,
            float expiresAt,
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
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt,
                    RequiresNarration = requiresNarration
                });
                return true;
            }

            AssistantMessageElement current = messages[index];
            if (current.SourceVersion == sourceVersion &&
                current.Priority == priority &&
                current.RelatedKind == relatedKind &&
                current.SuppressionKey.Equals(suppressionKey) &&
                current.Text.Equals(text) &&
                current.AudioEventId.Equals(audioEventId) &&
                current.ExpiresAt.Equals(expiresAt) &&
                current.RequiresNarration == requiresNarration &&
                current.Acknowledged == 0)
            {
                return false;
            }

            bool priorityEscalated = priority > current.Priority;
            current.SourceVersion = sourceVersion;
            current.Priority = priority;
            current.RelatedKind = relatedKind;
            current.SuppressionKey = suppressionKey;
            current.Text = text;
            current.AudioEventId = audioEventId;
            current.ExpiresAt = expiresAt;
            current.RequiresNarration = priorityEscalated ? requiresNarration : current.RequiresNarration;
            current.Acknowledged = 0;
            messages[index] = current;
            return true;
        }

        private static bool RemoveExpired(DynamicBuffer<AssistantMessageElement> messages, float now)
        {
            bool changed = false;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (message.Acknowledged == 0 && (message.ExpiresAt <= 0f || now < message.ExpiresAt))
                    continue;

                messages.RemoveAt(i--);
                changed = true;
            }

            return changed;
        }

        private static bool TrimMessages(DynamicBuffer<AssistantMessageElement> messages)
        {
            bool changed = false;
            while (messages.Length > MaxMessageRows)
            {
                int removeIndex = 0;
                for (int i = 1; i < messages.Length; i++)
                {
                    if (messages[i].Priority < messages[removeIndex].Priority ||
                        (messages[i].Priority == messages[removeIndex].Priority &&
                         messages[i].CreatedAt < messages[removeIndex].CreatedAt))
                    {
                        removeIndex = i;
                    }
                }

                messages.RemoveAt(removeIndex);
                changed = true;
            }

            return changed;
        }

        private static int CountVisible(DynamicBuffer<AssistantMessageElement> messages, float now)
        {
            int count = 0;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (message.Acknowledged == 0 && (message.ExpiresAt <= 0f || now < message.ExpiresAt))
                    count++;
            }

            return count;
        }

        private static float CalculateNextAgeBoundary(DynamicBuffer<AssistantMessageElement> messages, float now)
        {
            float next = 0f;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                ConsiderBoundary(message.CreatedAt + 5f, now, ref next);
                if (message.ExpiresAt > 0f)
                {
                    ConsiderBoundary(message.ExpiresAt - 1f, now, ref next);
                    ConsiderBoundary(message.ExpiresAt, now, ref next);
                }
            }

            return next;
        }

        private static void ConsiderBoundary(float candidate, float now, ref float next)
        {
            if (candidate <= now)
                return;
            if (next <= 0f || candidate < next)
                next = candidate;
        }

        private static bool ContainsThreat(DynamicBuffer<AssistantThreatReadModelElement> threats, int normalizedThreatId)
        {
            for (int i = 0; i < threats.Length; i++)
            {
                if (math.abs(threats[i].ThreatId % 10000) == normalizedThreatId)
                    return true;
            }

            return false;
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

        private static FixedString128Bytes BuildThreatBody(AssistantThreatReadModelElement threat)
        {
            FixedString128Bytes text = default;
            text.Append(threat.FriendlyName.Length > 0
                ? threat.FriendlyName
                : new FixedString64Bytes("FRIENDLY UNIT"));
            text.Append(" under attack from ");
            text.Append(threat.HostileName.Length > 0
                ? threat.HostileName
                : new FixedString64Bytes("SOURCE UNKNOWN"));
            if (threat.Damage > 0)
            {
                text.Append("; damage ");
                text.Append(threat.Damage);
            }
            return text;
        }

        private static FixedString64Bytes ThreatAudioEventId(AssistantThreatKind kind)
        {
            if (kind == AssistantThreatKind.AirAttack)
                return new FixedString64Bytes(AudioEventIds.VOARIAMessageWarningAirAttackType);
            if (kind == AssistantThreatKind.GroundAttack)
                return new FixedString64Bytes(AudioEventIds.VOARIAMessageWarningGroundAttackType);
            return default;
        }

        private static FixedString128Bytes CopyTo128(FixedString64Bytes source)
        {
            FixedString128Bytes result = default;
            result.Append(source);
            return result;
        }
    }
}
