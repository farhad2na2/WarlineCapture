using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>
    /// Continues the player's M01 group-attack order across the surviving patrol members.
    /// This is mission-scoped order policy; health, damage, death, and mission progression
    /// remain owned by their existing runtime systems.
    /// </summary>
    public static class CampaignMissionGroupAttackUtility
    {
        public static bool TryContinueActiveMissionSquadAttack(EntityManager entityManager)
        {
            using EntityQuery runtimeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (runtimeQuery.CalculateEntityCount() != 1)
                return false;

            CampaignMissionRuntimeComponent runtime =
                runtimeQuery.GetSingleton<CampaignMissionRuntimeComponent>();
            if (runtime.Outcome != MissionOutcomeKind.None ||
                runtime.Phase is not (MissionPhaseKind.ConfirmThreat or MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor))
            {
                return false;
            }

            using EntityQuery combatantsQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<LocalTransform>());
            ContinueCommandedSquadAttack(entityManager, combatantsQuery, runtime.SessionToken);
            return true;
        }

        internal static void ContinueCommandedSquadAttack(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            FixedString64Bytes sessionToken)
        {
            using NativeArray<Entity> combatants = missionCombatantsQuery.ToEntityArray(Allocator.Temp);
            using NativeList<Entity> hostiles = new(Allocator.Temp);
            CollectLiveHostiles(entityManager, combatants, sessionToken, hostiles);
            if (hostiles.Length == 0)
                return;

            NativeArray<int> assignedCounts = new(hostiles.Length, Allocator.Temp);
            CountExistingAssignments(entityManager, combatants, hostiles, sessionToken, assignedCounts);

            EntityCommandBuffer orders = new(Allocator.Temp);
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity friendly = combatants[index];
                if (!IsEligibleFriendly(entityManager, friendly, sessionToken) ||
                    HasLiveHostileTarget(entityManager, friendly, hostiles))
                {
                    continue;
                }

                int targetIndex = FindLeastAssignedNearestTarget(
                    entityManager, friendly, hostiles, assignedCounts);
                Entity target = hostiles[targetIndex];
                assignedCounts[targetIndex]++;
                QueueAttackOrder(entityManager, ref orders, friendly, target);
            }

            orders.Playback(entityManager);
            orders.Dispose();
            assignedCounts.Dispose();
        }

        internal static int QueueHostileCounterfire(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            FixedString64Bytes sessionToken,
            ref EntityCommandBuffer orders)
        {
            using NativeArray<Entity> combatants = missionCombatantsQuery.ToEntityArray(Allocator.Temp);
            using NativeList<Entity> friendlies = new(Allocator.Temp);
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity candidate = combatants[index];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(candidate);
                Faction faction = entityManager.GetComponentData<Faction>(candidate);
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(candidate);
                if (role.SessionToken.Equals(sessionToken) &&
                    FactionIdentity.IsPlayerControlled(faction.Id) &&
                    health.Current > 0)
                {
                    friendlies.Add(candidate);
                }
            }

            if (friendlies.Length == 0)
                return 0;

            NativeArray<int> assignedCounts = new(friendlies.Length, Allocator.Temp);
            int queued = 0;
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity hostile = combatants[index];
                if (!IsEligibleHostile(entityManager, hostile, sessionToken))
                    continue;

                int targetIndex = FindLeastAssignedNearestTarget(
                    entityManager, hostile, friendlies, assignedCounts);
                assignedCounts[targetIndex]++;
                QueueAttackOrder(entityManager, ref orders, hostile, friendlies[targetIndex]);
                queued++;
            }

            assignedCounts.Dispose();
            return queued;
        }

        private static void CollectLiveHostiles(
            EntityManager entityManager,
            NativeArray<Entity> combatants,
            FixedString64Bytes sessionToken,
            NativeList<Entity> hostiles)
        {
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity candidate = combatants[index];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(candidate);
                Faction faction = entityManager.GetComponentData<Faction>(candidate);
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(candidate);
                if (role.SessionToken.Equals(sessionToken) &&
                    !FactionIdentity.IsPlayerControlled(faction.Id) &&
                    health.Current > 0)
                {
                    hostiles.Add(candidate);
                }
            }
        }

        private static void CountExistingAssignments(
            EntityManager entityManager,
            NativeArray<Entity> combatants,
            NativeList<Entity> hostiles,
            FixedString64Bytes sessionToken,
            NativeArray<int> assignedCounts)
        {
            for (int index = 0; index < combatants.Length; index++)
            {
                Entity friendly = combatants[index];
                if (!IsEligibleFriendly(entityManager, friendly, sessionToken) ||
                    !entityManager.HasComponent<EngageTarget>(friendly))
                {
                    continue;
                }

                EngageTarget current = entityManager.GetComponentData<EngageTarget>(friendly);
                int currentIndex = IndexOf(hostiles, current.Target);
                if (currentIndex >= 0)
                    assignedCounts[currentIndex]++;
            }
        }

        private static bool IsEligibleFriendly(
            EntityManager entityManager,
            Entity entity,
            FixedString64Bytes sessionToken)
        {
            CampaignMissionUnitRoleComponent role =
                entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
            UnitCombat combat = entityManager.GetComponentData<UnitCombat>(entity);
            return role.SessionToken.Equals(sessionToken) &&
                   FactionIdentity.IsPlayerControlled(faction.Id) &&
                   health.Current > 0 &&
                   combat.CanAttack != 0;
        }

        private static bool IsEligibleHostile(
            EntityManager entityManager,
            Entity entity,
            FixedString64Bytes sessionToken)
        {
            CampaignMissionUnitRoleComponent role =
                entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
            UnitCombat combat = entityManager.GetComponentData<UnitCombat>(entity);
            return role.SessionToken.Equals(sessionToken) &&
                   !FactionIdentity.IsPlayerControlled(faction.Id) &&
                   health.Current > 0 &&
                   combat.CanAttack != 0;
        }

        private static bool HasLiveHostileTarget(
            EntityManager entityManager,
            Entity friendly,
            NativeList<Entity> hostiles)
        {
            if (!entityManager.HasComponent<EngageTarget>(friendly))
                return false;
            EngageTarget current = entityManager.GetComponentData<EngageTarget>(friendly);
            return IndexOf(hostiles, current.Target) >= 0;
        }

        private static int FindLeastAssignedNearestTarget(
            EntityManager entityManager,
            Entity source,
            NativeList<Entity> targets,
            NativeArray<int> assignedCounts)
        {
            float3 sourcePosition = entityManager.GetComponentData<LocalTransform>(source).Position;
            int bestIndex = 0;
            float bestDistanceSq = float.MaxValue;
            int bestAssignedCount = int.MaxValue;
            for (int index = 0; index < targets.Length; index++)
            {
                float3 delta = entityManager.GetComponentData<LocalTransform>(targets[index]).Position -
                               sourcePosition;
                delta.y = 0f;
                float distanceSq = math.lengthsq(delta);
                int assignedCount = assignedCounts[index];
                if (assignedCount < bestAssignedCount ||
                    (assignedCount == bestAssignedCount && distanceSq < bestDistanceSq))
                {
                    bestAssignedCount = assignedCount;
                    bestDistanceSq = distanceSq;
                    bestIndex = index;
                }
            }
            return bestIndex;
        }

        private static void QueueAttackOrder(
            EntityManager entityManager,
            ref EntityCommandBuffer orders,
            Entity source,
            Entity target)
        {
            float3 targetPosition = entityManager.GetComponentData<LocalTransform>(target).Position;
            int2 targetCell = entityManager.HasComponent<UnitGrid>(target)
                ? entityManager.GetComponentData<UnitGrid>(target).Cell
                : default;
            EngageTarget continuation = new()
            {
                Target = target,
                Cell = targetCell,
                Position = targetPosition,
                IsCommanded = 1
            };
            if (entityManager.HasComponent<EngageTarget>(source))
                orders.SetComponent(source, continuation);
            else
                orders.AddComponent(source, continuation);

            RemoveIfPresent<ManualMoveOrderTag>(entityManager, ref orders, source);
            RemoveIfPresent<HoldPositionOrderTag>(entityManager, ref orders, source);
            RemoveIfPresent<UnitPathFollow>(entityManager, ref orders, source);
            RemoveIfPresent<UnitPathRange>(entityManager, ref orders, source);
            RemoveIfPresent<UnitPathRequest>(entityManager, ref orders, source);
            RemoveIfPresent<AutoWanderMoveTag>(entityManager, ref orders, source);
        }

        private static int IndexOf(NativeList<Entity> entities, Entity target)
        {
            for (int index = 0; index < entities.Length; index++)
                if (entities[index] == target)
                    return index;
            return -1;
        }

        private static void RemoveIfPresent<T>(
            EntityManager entityManager,
            ref EntityCommandBuffer orders,
            Entity entity)
            where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
                orders.RemoveComponent<T>(entity);
        }
    }
}
