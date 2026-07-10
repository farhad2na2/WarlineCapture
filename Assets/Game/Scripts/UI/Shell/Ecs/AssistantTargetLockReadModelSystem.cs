using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantRecommendationSystem))]
    [UpdateAfter(typeof(AssistantCommandIntentSystem))]
    [UpdateAfter(typeof(AssistantCommandResultBridgeSystem))]
    public partial struct AssistantTargetLockReadModelSystem : ISystem
    {
        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>(),
                ComponentType.ReadOnly<Game.UI.Shell.Contracts.Ecs.UiMatchHudHeaderComponent>());
            matchStartQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            if (!AssistantRuntimeStateUtility.IsActive(state.EntityManager, boundary, matchStartQuery))
                return;

            DynamicBuffer<AssistantRecommendationElement> recommendations =
                state.EntityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);
            DynamicBuffer<AssistantPreviewHighlightElement> previews =
                state.EntityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary, true);
            DynamicBuffer<AssistantCommandDispatchElement> dispatches =
                state.EntityManager.GetBuffer<AssistantCommandDispatchElement>(boundary, true);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                state.EntityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary, true);
            DynamicBuffer<AssistantThreatReadModelElement> threats =
                state.EntityManager.GetBuffer<AssistantThreatReadModelElement>(boundary, true);

            AssistantTargetLockReadModelComponent next = BuildTargetLock(
                state.EntityManager,
                recommendations,
                previews,
                dispatches,
                results,
                threats);
            AssistantTargetLockReadModelComponent current =
                state.EntityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary);
            if (Matches(current, next))
                return;

            next.Version = AssistantRuntimeStateUtility.NextVersion(current.Version);
            state.EntityManager.SetComponentData(boundary, next);
        }

        private static AssistantTargetLockReadModelComponent BuildTargetLock(
            EntityManager entityManager,
            DynamicBuffer<AssistantRecommendationElement> recommendations,
            DynamicBuffer<AssistantPreviewHighlightElement> previews,
            DynamicBuffer<AssistantCommandDispatchElement> dispatches,
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            DynamicBuffer<AssistantThreatReadModelElement> threats)
        {
            AssistantTargetLockReadModelComponent model = default;
            AssistantRecommendationElement recommendation =
                recommendations.Length > 0 ? recommendations[0] : default;
            AssistantPreviewHighlightElement preview = FindActivePreview(previews);
            AssistantCommandDispatchElement dispatch = FindLatestDispatch(dispatches);

            if (recommendation.RecommendationId != 0 && HasTarget(recommendation))
            {
                model.RecommendationId = recommendation.RecommendationId;
                model.TargetKind = recommendation.TargetKind;
                model.SourceEntity = recommendation.SourceEntity;
                model.TargetEntity = recommendation.TargetEntity;
                model.TargetCell = recommendation.TargetCell;
                model.WorldPosition = recommendation.WorldPosition;
                model.HasTargetCell = recommendation.HasTargetCell;
                model.HasWorldPosition = recommendation.HasWorldPosition;
                model.Reason = recommendation.RejectionReason.Length > 0
                    ? CopyTo128(recommendation.RejectionReason)
                    : recommendation.Reason;
                model.State = recommendation.CanExecute != 0
                    ? AssistantTargetLockState.Executable
                    : AssistantTargetLockState.Candidate;
            }
            else if (preview.Active != 0)
            {
                model.RecommendationId = preview.RecommendationId;
                model.TargetKind = preview.TargetKind;
                model.SourceEntity = preview.SourceEntity;
                model.TargetEntity = preview.TargetEntity;
                model.TargetCell = preview.TargetCell;
                model.WorldPosition = preview.WorldPosition;
                model.HasTargetCell = preview.TargetKind == AssistantTargetKind.Cell ? (byte)1 : (byte)0;
                model.HasWorldPosition = IsFinite(preview.WorldPosition) ? (byte)1 : (byte)0;
                model.State = AssistantTargetLockState.Preview;
            }
            else if (dispatch.AssistantRequestId != 0)
            {
                model.RecommendationId = dispatch.RecommendationId;
                model.State = dispatch.Status == AssistantCommandIntentStatus.Pending ||
                              dispatch.Status == AssistantCommandIntentStatus.Accepted
                    ? AssistantTargetLockState.Executing
                    : AssistantTargetLockState.Invalid;
            }
            else if (threats.Length > 0)
            {
                AssistantThreatReadModelElement threat = threats[0];
                model.ThreatId = threat.ThreatId;
                model.TargetKind = AssistantTargetKind.Entity;
                model.SourceEntity = threat.FriendlyTarget;
                model.TargetEntity = threat.HostileSource;
                model.WorldPosition = threat.HostileWorldPosition;
                model.HasWorldPosition = 1;
                model.State = AssistantTargetLockState.Candidate;
                model.Reason = threat.Reason;
            }
            else
            {
                return model;
            }

            if (preview.Active != 0 &&
                (model.RecommendationId == 0 || preview.RecommendationId == model.RecommendationId))
            {
                model.State = AssistantTargetLockState.Preview;
            }

            if (dispatch.AssistantRequestId != 0 && dispatch.RecommendationId == model.RecommendationId)
            {
                model.State = dispatch.Status == AssistantCommandIntentStatus.Pending ||
                              dispatch.Status == AssistantCommandIntentStatus.Accepted
                    ? AssistantTargetLockState.Executing
                    : dispatch.Status == AssistantCommandIntentStatus.Rejected ||
                      dispatch.Status == AssistantCommandIntentStatus.TimedOut
                        ? AssistantTargetLockState.Invalid
                        : model.State;
            }

            AssistantCommandIntentResultElement latestResult = FindLatestResult(results, model.RecommendationId);
            if (latestResult.RequestId != 0 &&
                (latestResult.Status == AssistantCommandIntentStatus.Rejected ||
                 latestResult.Status == AssistantCommandIntentStatus.TimedOut))
            {
                model.State = AssistantTargetLockState.Invalid;
                model.Reason = CopyTo128(latestResult.Message);
            }

            Enrich(entityManager, ref model);
            model.Visible = HasTarget(model) ? (byte)1 : (byte)0;
            return model;
        }

        private static void Enrich(EntityManager entityManager, ref AssistantTargetLockReadModelComponent model)
        {
            bool hasSourcePosition = TryResolvePosition(entityManager, model.SourceEntity, out float3 sourcePosition);
            bool hasTargetPosition = TryResolvePosition(entityManager, model.TargetEntity, out float3 targetPosition);
            if (!hasTargetPosition && model.HasWorldPosition != 0 && IsFinite(model.WorldPosition))
            {
                targetPosition = model.WorldPosition;
                hasTargetPosition = true;
            }
            else if (hasTargetPosition)
            {
                model.WorldPosition = targetPosition;
                model.HasWorldPosition = 1;
            }

            model.SourceName = ResolveName(entityManager, model.SourceEntity, new FixedString64Bytes("FRIENDLY UNIT"));
            model.TargetName = ResolveName(entityManager, model.TargetEntity, new FixedString64Bytes("TARGET"));
            model.FactionRelation = ResolveFactionRelation(entityManager, model.TargetEntity);
            if (model.TargetEntity != Entity.Null && model.TargetName.Equals(new FixedString64Bytes("TARGET")))
            {
                model.TargetName = model.FactionRelation == AssistantFactionRelation.Hostile
                    ? new FixedString64Bytes("HOSTILE SOURCE")
                    : new FixedString64Bytes("TARGET");
            }

            if (hasSourcePosition && hasTargetPosition)
            {
                model.Distance = math.distance(
                    new float2(sourcePosition.x, sourcePosition.z),
                    new float2(targetPosition.x, targetPosition.z));
                model.HasDistance = 1;
            }

            if (model.TargetEntity != Entity.Null &&
                entityManager.Exists(model.TargetEntity) &&
                entityManager.HasComponent<UnitHealth>(model.TargetEntity))
            {
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(model.TargetEntity);
                model.HealthCurrent = math.max(0, health.Current);
                model.HealthMax = math.max(0, health.Max);
                model.HasHealth = health.Max > 0 ? (byte)1 : (byte)0;
            }
        }

        private static AssistantPreviewHighlightElement FindActivePreview(
            DynamicBuffer<AssistantPreviewHighlightElement> previews)
        {
            for (int i = previews.Length - 1; i >= 0; i--)
            {
                if (previews[i].Active != 0)
                    return previews[i];
            }
            return default;
        }

        private static AssistantCommandDispatchElement FindLatestDispatch(
            DynamicBuffer<AssistantCommandDispatchElement> dispatches)
        {
            return dispatches.Length > 0 ? dispatches[dispatches.Length - 1] : default;
        }

        private static AssistantCommandIntentResultElement FindLatestResult(
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            int recommendationId)
        {
            for (int i = results.Length - 1; i >= 0; i--)
            {
                if (results[i].RecommendationId == recommendationId)
                    return results[i];
            }
            return default;
        }

        private static bool HasTarget(AssistantRecommendationElement recommendation)
        {
            return recommendation.TargetEntity != Entity.Null ||
                   recommendation.HasTargetCell != 0 ||
                   recommendation.HasWorldPosition != 0;
        }

        private static bool HasTarget(AssistantTargetLockReadModelComponent model)
        {
            return model.TargetEntity != Entity.Null ||
                   model.HasTargetCell != 0 ||
                   model.HasWorldPosition != 0;
        }

        private static bool TryResolvePosition(EntityManager entityManager, Entity entity, out float3 position)
        {
            position = default;
            if (entity == Entity.Null ||
                !entityManager.Exists(entity) ||
                !entityManager.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            position = entityManager.GetComponentData<LocalTransform>(entity).Position;
            return IsFinite(position);
        }

        private static FixedString64Bytes ResolveName(
            EntityManager entityManager,
            Entity entity,
            FixedString64Bytes fallback)
        {
            if (entity != Entity.Null &&
                entityManager.Exists(entity) &&
                entityManager.HasComponent<UnitDisplayInfo>(entity))
            {
                FixedString64Bytes name = entityManager.GetComponentData<UnitDisplayInfo>(entity).Name;
                if (name.Length > 0)
                    return name;
            }
            return fallback;
        }

        private static AssistantFactionRelation ResolveFactionRelation(EntityManager entityManager, Entity entity)
        {
            if (entity == Entity.Null ||
                !entityManager.Exists(entity) ||
                !entityManager.HasComponent<Faction>(entity))
            {
                return AssistantFactionRelation.Unknown;
            }

            byte factionId = entityManager.GetComponentData<Faction>(entity).Id;
            if (FactionIdentity.IsPlayerControlled(factionId))
                return AssistantFactionRelation.Friendly;
            if (FactionIdentity.IsHostileToPlayer(factionId))
                return AssistantFactionRelation.Hostile;
            return FactionIdentity.IsNeutral(factionId)
                ? AssistantFactionRelation.Neutral
                : AssistantFactionRelation.Unknown;
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }

        private static FixedString128Bytes CopyTo128(FixedString64Bytes value)
        {
            FixedString128Bytes result = default;
            result.Append(value);
            return result;
        }

        private static bool Matches(
            AssistantTargetLockReadModelComponent current,
            AssistantTargetLockReadModelComponent next)
        {
            return current.RecommendationId == next.RecommendationId &&
                   current.ThreatId == next.ThreatId &&
                   current.State == next.State &&
                   current.TargetKind == next.TargetKind &&
                   current.FactionRelation == next.FactionRelation &&
                   current.SourceEntity == next.SourceEntity &&
                   current.TargetEntity == next.TargetEntity &&
                   current.TargetCell.Equals(next.TargetCell) &&
                   current.WorldPosition.Equals(next.WorldPosition) &&
                   current.Distance.Equals(next.Distance) &&
                   current.HealthCurrent == next.HealthCurrent &&
                   current.HealthMax == next.HealthMax &&
                   current.Visible == next.Visible &&
                   current.HasTargetCell == next.HasTargetCell &&
                   current.HasWorldPosition == next.HasWorldPosition &&
                   current.HasDistance == next.HasDistance &&
                   current.HasHealth == next.HasHealth &&
                   current.SourceName.Equals(next.SourceName) &&
                   current.TargetName.Equals(next.TargetName) &&
                   current.Reason.Equals(next.Reason);
        }
    }
}
