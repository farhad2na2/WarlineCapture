using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(AssistantRecommendationSystem))]
    public partial struct AssistantThreatReadModelSystem : ISystem
    {
        private const int MaxVisibleThreats = 4;
        private const float ThreatLifetimeSeconds = 6f;

        private EntityQuery _boundaryQuery;
        private EntityQuery _matchStartQuery;
        private EntityQuery _observationQueueQuery;
        private Entity _activeBoundary;
        private byte _wasActive;

        public void OnCreate(ref SystemState state)
        {
            _boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            _matchStartQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchStartQueueComponent>());
            _observationQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CombatDamageObservationQueueComponent>());
            state.RequireForUpdate(_boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = _boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);

            if (!AssistantRuntimeStateUtility.IsActive(state.EntityManager, boundary, _matchStartQuery))
            {
                _wasActive = 0;
                _activeBoundary = Entity.Null;
                AssistantRuntimeStateUtility.ClearInactiveReadModels(state.EntityManager, boundary);
                return;
            }

            EntityManager em = state.EntityManager;
            Entity queueEntity = _observationQueueQuery.CalculateEntityCount() == 1
                ? _observationQueueQuery.GetSingletonEntity()
                : Entity.Null;
            CombatDamageObservationQueueComponent queue = queueEntity != Entity.Null
                ? em.GetComponentData<CombatDamageObservationQueueComponent>(queueEntity)
                : default;
            DynamicBuffer<AssistantThreatReadModelElement> threats =
                em.GetBuffer<AssistantThreatReadModelElement>(boundary);
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
                em.GetBuffer<MatchObjectiveRuntimeElement>(boundary, true);
            AssistantThreatReadModelStateComponent threatState =
                em.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);

            if (_wasActive == 0 || _activeBoundary != boundary)
            {
                bool clearedRows = threats.Length > 0;
                threats.Clear();
                threatState.LastConsumedEventId = queue.LastEventId;
                threatState.LastObservedQueueVersion = queue.Version;
                threatState.NextExpiryAt = 0f;
                threatState.VisibleCount = 0;
                if (clearedRows)
                    threatState.Version = AssistantRuntimeStateUtility.NextVersion(threatState.Version);
                em.SetComponentData(boundary, threatState);
                MarkAssistantDirty(em, boundary, clearedRows);
                _wasActive = 1;
                _activeBoundary = boundary;
                return;
            }

            float now = (float)SystemAPI.Time.ElapsedTime;
            bool queueAdvanced = queueEntity != Entity.Null &&
                                 (queue.Version != threatState.LastObservedQueueVersion ||
                                  queue.LastEventId > threatState.LastConsumedEventId);
            bool expiryDue = threatState.NextExpiryAt > 0f && now >= threatState.NextExpiryAt;
            if (!queueAdvanced && !expiryDue)
                return;

            bool rowsChanged = expiryDue && RemoveExpired(threats, now);
            if (queueAdvanced && em.HasBuffer<CombatDamageObservationElement>(queueEntity))
            {
                DynamicBuffer<CombatDamageObservationElement> observations =
                    em.GetBuffer<CombatDamageObservationElement>(queueEntity, true);
                for (int i = 0; i < observations.Length; i++)
                {
                    CombatDamageObservationElement observation = observations[i];
                    if (observation.EventId <= threatState.LastConsumedEventId ||
                        observation.EventId > queue.LastEventId)
                    {
                        continue;
                    }

                    rowsChanged |= TryUpsertThreat(em, threats, objectives, observation, now);
                }
            }

            if (queueAdvanced)
            {
                threatState.LastConsumedEventId = queue.LastEventId;
                threatState.LastObservedQueueVersion = queue.Version;
            }

            if (rowsChanged)
            {
                SortThreats(threats);
                threatState.Version = AssistantRuntimeStateUtility.NextVersion(threatState.Version);
            }

            threatState.VisibleCount = threats.Length;
            threatState.NextExpiryAt = CalculateNextExpiry(threats, now);
            em.SetComponentData(boundary, threatState);
            MarkAssistantDirty(em, boundary, rowsChanged);
        }

        private static bool TryUpsertThreat(
            EntityManager em,
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            CombatDamageObservationElement observation,
            float now)
        {
            if (observation.DamageApplied <= 0 ||
                observation.TargetEntity == Entity.Null ||
                !em.Exists(observation.TargetEntity))
            {
                return false;
            }

            bool hasFaction = em.HasComponent<Faction>(observation.TargetEntity);
            byte targetFactionId = hasFaction
                ? em.GetComponentData<Faction>(observation.TargetEntity).Id
                : FactionIdentity.NeutralFactionId;
            bool playerOwned = FactionIdentity.IsPlayerControlled(targetFactionId);
            bool objectiveProtected = IsObjectiveProtectedTarget(objectives, observation.TargetEntity);
            if (!playerOwned && !objectiveProtected)
                return false;

            AssistantThreatKind kind = ClassifyThreat(em, observation);
            int threatId = CalculateThreatId(observation.TargetEntity, observation.SourceEntity, kind);
            FixedString64Bytes friendlyName = ResolveName(
                em,
                observation.TargetEntity,
                new FixedString64Bytes("FRIENDLY UNIT"));
            FixedString64Bytes hostileName = observation.SourceEntity == Entity.Null
                ? new FixedString64Bytes("SOURCE UNKNOWN")
                : ResolveName(em, observation.SourceEntity, new FixedString64Bytes("HOSTILE SOURCE"));
            byte hostileFactionId = observation.SourceEntity != Entity.Null &&
                                    em.Exists(observation.SourceEntity) &&
                                    em.HasComponent<Faction>(observation.SourceEntity)
                ? em.GetComponentData<Faction>(observation.SourceEntity).Id
                : FactionIdentity.NeutralFactionId;
            float expiresAt = observation.ObservedAt + ThreatLifetimeSeconds;
            if (expiresAt <= now)
                return false;

            float2 horizontalDelta = new(
                observation.SourceWorldPosition.x - observation.TargetWorldPosition.x,
                observation.SourceWorldPosition.z - observation.TargetWorldPosition.z);
            AssistantMessagePriority priority = ResolvePriority(observation);
            var row = new AssistantThreatReadModelElement
            {
                ThreatId = threatId,
                SourceEventId = observation.EventId,
                Kind = kind,
                Priority = priority,
                FriendlyTarget = observation.TargetEntity,
                HostileSource = observation.SourceEntity,
                FriendlyFactionId = playerOwned ? targetFactionId : FactionIdentity.PlayerFactionId,
                HostileFactionId = hostileFactionId,
                FriendlyWorldPosition = observation.TargetWorldPosition,
                HostileWorldPosition = observation.SourceWorldPosition,
                Distance = observation.SourceEntity == Entity.Null ? 0f : math.length(horizontalDelta),
                Damage = observation.DamageApplied,
                FriendlyHealth = observation.TargetHealthAfter,
                FriendlyMaxHealth = observation.TargetMaxHealth,
                LastObservedAt = observation.ObservedAt,
                ExpiresAt = expiresAt,
                FriendlyName = friendlyName,
                HostileName = hostileName,
                Reason = BuildReason(friendlyName, hostileName)
            };

            int existingIndex = FindThreat(threats, threatId);
            if (existingIndex >= 0)
            {
                if (threats[existingIndex].Priority > row.Priority)
                    row.Priority = threats[existingIndex].Priority;
                threats[existingIndex] = row;
                return true;
            }

            if (threats.Length < MaxVisibleThreats)
            {
                threats.Add(row);
                return true;
            }

            int replacementIndex = FindLowestPriorityThreat(threats);
            if (!RanksAheadOf(row, threats[replacementIndex]))
                return false;

            threats[replacementIndex] = row;
            return true;
        }

        private static bool IsObjectiveProtectedTarget(
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            Entity target)
        {
            for (int i = 0; i < objectives.Length; i++)
            {
                MatchObjectiveRuntimeElement objective = objectives[i];
                if (objective.ProtectsTarget != 0 &&
                    objective.TargetEntity == target &&
                    objective.State != MatchObjectiveState.Complete &&
                    objective.State != MatchObjectiveState.Failed)
                {
                    return true;
                }
            }

            return false;
        }

        private static AssistantThreatKind ClassifyThreat(
            EntityManager em,
            CombatDamageObservationElement observation)
        {
            if (observation.SourceKind == CombatDamageSourceKind.GroundMissile ||
                observation.SourceKind == CombatDamageSourceKind.AirMissile)
            {
                return AssistantThreatKind.MissileAttack;
            }

            if (observation.SourceKind == CombatDamageSourceKind.BuildingDefense ||
                HasSourceComponent<RuntimeBuildingCombatTag>(em, observation.SourceEntity) ||
                HasSourceComponent<BuildingDefenseWeapon>(em, observation.SourceEntity))
            {
                return AssistantThreatKind.BuildingDefenseAttack;
            }

            if (HasSourceComponent<UnitAirComponent>(em, observation.SourceEntity))
                return AssistantThreatKind.AirAttack;

            return observation.SourceKind == CombatDamageSourceKind.DirectFire
                ? AssistantThreatKind.GroundAttack
                : AssistantThreatKind.FriendlyUnderAttack;
        }

        private static bool HasSourceComponent<T>(EntityManager em, Entity source)
            where T : unmanaged, IComponentData
        {
            return source != Entity.Null && em.Exists(source) && em.HasComponent<T>(source);
        }

        private static AssistantMessagePriority ResolvePriority(CombatDamageObservationElement observation)
        {
            bool critical = observation.TargetHealthAfter <= 0;
            if (observation.TargetMaxHealth > 0)
            {
                critical |= (long)observation.TargetHealthAfter * 4L <= observation.TargetMaxHealth;
                critical |= (long)observation.DamageApplied * 4L >= observation.TargetMaxHealth;
            }

            return critical ? AssistantMessagePriority.Critical : AssistantMessagePriority.High;
        }

        private static FixedString64Bytes ResolveName(
            EntityManager em,
            Entity entity,
            FixedString64Bytes fallback)
        {
            if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitDisplayInfo>(entity))
            {
                FixedString64Bytes name = em.GetComponentData<UnitDisplayInfo>(entity).Name;
                if (name.Length > 0)
                    return name;
            }

            return fallback;
        }

        private static FixedString128Bytes BuildReason(
            FixedString64Bytes friendlyName,
            FixedString64Bytes hostileName)
        {
            FixedString128Bytes reason = default;
            reason.Append(friendlyName);
            reason.Append(" under attack from ");
            reason.Append(hostileName);
            return reason;
        }

        private static int CalculateThreatId(
            Entity friendly,
            Entity hostile,
            AssistantThreatKind kind)
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)friendly.Index) * 16777619u;
            hash = (hash ^ (uint)friendly.Version) * 16777619u;
            hash = (hash ^ (uint)hostile.Index) * 16777619u;
            hash = (hash ^ (uint)hostile.Version) * 16777619u;
            hash = (hash ^ (byte)kind) * 16777619u;
            int result = (int)(hash & 0x7fffffffu);
            return result == 0 ? 1 : result;
        }

        private static int FindThreat(
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            int threatId)
        {
            for (int i = 0; i < threats.Length; i++)
            {
                if (threats[i].ThreatId == threatId)
                    return i;
            }

            return -1;
        }

        private static int FindLowestPriorityThreat(
            DynamicBuffer<AssistantThreatReadModelElement> threats)
        {
            int result = 0;
            for (int i = 1; i < threats.Length; i++)
            {
                if (RanksAheadOf(threats[result], threats[i]))
                    result = i;
            }

            return result;
        }

        private static bool RanksAheadOf(
            AssistantThreatReadModelElement left,
            AssistantThreatReadModelElement right)
        {
            if (left.Priority != right.Priority)
                return left.Priority > right.Priority;
            if (!left.LastObservedAt.Equals(right.LastObservedAt))
                return left.LastObservedAt > right.LastObservedAt;
            return left.ThreatId < right.ThreatId;
        }

        private static void SortThreats(DynamicBuffer<AssistantThreatReadModelElement> threats)
        {
            for (int i = 1; i < threats.Length; i++)
            {
                AssistantThreatReadModelElement value = threats[i];
                int destination = i;
                while (destination > 0 && RanksAheadOf(value, threats[destination - 1]))
                {
                    threats[destination] = threats[destination - 1];
                    destination--;
                }

                threats[destination] = value;
            }
        }

        private static bool RemoveExpired(
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            float now)
        {
            bool changed = false;
            for (int i = threats.Length - 1; i >= 0; i--)
            {
                if (threats[i].ExpiresAt > now)
                    continue;
                threats.RemoveAt(i);
                changed = true;
            }

            return changed;
        }

        private static float CalculateNextExpiry(
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            float now)
        {
            float next = 0f;
            for (int i = 0; i < threats.Length; i++)
            {
                float expiresAt = threats[i].ExpiresAt;
                if (expiresAt <= now)
                    continue;
                if (next <= 0f || expiresAt < next)
                    next = expiresAt;
            }

            return next;
        }

        private static void MarkAssistantDirty(EntityManager em, Entity boundary, bool changed)
        {
            if (!changed || !em.HasComponent<AssistantStateComponent>(boundary))
                return;

            AssistantStateComponent assistant = em.GetComponentData<AssistantStateComponent>(boundary);
            assistant.UiDirty = 1;
            em.SetComponentData(boundary, assistant);
        }
    }
}
