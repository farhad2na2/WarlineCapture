using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime.Combat
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct CombatDamageObservationBootstrapSystem : ISystem
    {
        private EntityQuery _queueQuery;

        public void OnCreate(ref SystemState state)
        {
            _queueQuery = state.GetEntityQuery(ComponentType.ReadWrite<CombatDamageObservationQueueComponent>());
            CombatDamageObservationUtility.EnsureQueue(state.EntityManager, _queueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }

    public static class CombatDamageObservationUtility
    {
        public const int Capacity = 64;
        private static readonly Dictionary<string, float> DamageTimes = new();

        public static void TraceSkirmishBaseDamage(EntityManager em, Entity source, Entity target, string kind, int before, int after)
        {
            SkirmishConvoyTruckDamageTrace.Trace(em, source, target, kind, before, after); // W37 diagnosis: CE truck hits, log only
            if (before <= after || !em.Exists(target) || !em.HasComponent<SkirmishAttemptOwnedComponent>(target) || !em.HasComponent<SkirmishObjectiveRoleComponent>(target)) return;
            var targetRole = em.GetComponentData<SkirmishObjectiveRoleComponent>(target).Role;
            if (targetRole != SkirmishObjectiveRoleKind.PlayerBase && targetRole != SkirmishObjectiveRoleKind.EnemyBase) return;
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishObjectiveClockComponent));
            using var sessions = query.ToEntityArray(Allocator.Temp);
            foreach (var session in sessions)
            {
                var match = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if (match.IsLegacy != 0 || match.Phase != SkirmishSessionPhase.Playing || !match.SessionId.Equals(owner.SessionId)) continue;
                float now = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
                string key = match.SessionId + ":" + target.Index + ":" + target.Version + ":" + source.Index + ":" + source.Version;
                if (DamageTimes.TryGetValue(key, out float last) && now >= last && now - last < 5f) return;
                DamageTimes[key] = now;
                string role = "unknown", faction = "unknown", stable = "unknown";
                if (source != Entity.Null && em.Exists(source))
                {
                    if (em.HasComponent<SkirmishUnitRoleComponent>(source)) role = em.GetComponentData<SkirmishUnitRoleComponent>(source).Role.ToString();
                    if (em.HasComponent<SkirmishAttemptOwnedComponent>(source)) { var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(source); faction = own.FactionId.ToString(); stable = own.StableObjectId.ToString(); }
                }
                UnityEngine.Debug.Log("[SkirmishEnemyTrace] event=base_damage session=" + match.SessionId + " base=" + targetRole + " attacker=" + source + " attacker_id=" + stable + " role=" + role + " faction=" + faction + " damage_source=" + kind + " damage=" + (before - after) + " health_after=" + after + " time=" + now.ToString("F2"));
                return;
            }
        }

        public static Entity EnsureQueue(EntityManager entityManager, EntityQuery queueQuery)
        {
            int queueCount = queueQuery.CalculateEntityCount();
            if (queueCount > 1)
                return Entity.Null;

            Entity queueEntity;
            if (queueCount == 0)
            {
                queueEntity = entityManager.CreateEntity(typeof(CombatDamageObservationQueueComponent));
                entityManager.SetName(queueEntity, "CombatDamageObservations");
            }
            else
            {
                queueEntity = queueQuery.GetSingletonEntity();
            }

            DynamicBuffer<CombatDamageObservationElement> observations =
                entityManager.HasBuffer<CombatDamageObservationElement>(queueEntity)
                    ? entityManager.GetBuffer<CombatDamageObservationElement>(queueEntity)
                    : entityManager.AddBuffer<CombatDamageObservationElement>(queueEntity);
            observations.EnsureCapacity(Capacity);
            while (observations.Length > Capacity)
                observations.RemoveAt(0);
            return queueEntity;
        }

        public static Entity TryGetQueue(EntityQuery queueQuery)
        {
            return queueQuery.CalculateEntityCount() == 1
                ? queueQuery.GetSingletonEntity()
                : Entity.Null;
        }

        public static bool Append(
            EntityManager entityManager,
            Entity queueEntity,
            Entity sourceEntity,
            Entity targetEntity,
            CombatDamageSourceKind sourceKind,
            int previousHealth,
            int currentHealth,
            int targetMaxHealth,
            float observedAt,
            float3 sourceWorldPosition,
            float3 targetWorldPosition)
        {
            int damageApplied = math.max(0, previousHealth - currentHealth);
            TraceSkirmishBaseDamage(entityManager, sourceEntity, targetEntity, sourceKind.ToString(), previousHealth, currentHealth);
            // Passive cancellation fact in the shared Components boundary;
            // Combat must not depend on the higher-level Runtime repair service.
            if(damageApplied>0&&entityManager.Exists(targetEntity)&&entityManager.HasComponent<VehicleRepairStateComponent>(targetEntity))
            {
                var repair=entityManager.GetComponentData<VehicleRepairStateComponent>(targetEntity);
                if(repair.Phase==VehicleRepairPhase.Working)
                {repair.IncomingDamage=1;entityManager.SetComponentData(targetEntity,repair);}
            }
            if (damageApplied == 0 ||
                queueEntity == Entity.Null ||
                !entityManager.Exists(queueEntity) ||
                !entityManager.HasComponent<CombatDamageObservationQueueComponent>(queueEntity) ||
                !entityManager.HasBuffer<CombatDamageObservationElement>(queueEntity))
            {
                return false;
            }

            CombatDamageObservationQueueComponent queue =
                entityManager.GetComponentData<CombatDamageObservationQueueComponent>(queueEntity);
            if (queue.LastEventId == int.MaxValue || queue.Version == uint.MaxValue)
                return false;

            queue.LastEventId++;
            queue.Version++;

            DynamicBuffer<CombatDamageObservationElement> observations =
                entityManager.GetBuffer<CombatDamageObservationElement>(queueEntity);
            while (observations.Length >= Capacity)
                observations.RemoveAt(0);

            observations.Add(new CombatDamageObservationElement
            {
                EventId = queue.LastEventId,
                Frame = Time.frameCount,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                SourceKind = sourceKind,
                DamageApplied = damageApplied,
                TargetHealthAfter = currentHealth,
                TargetMaxHealth = math.max(0, targetMaxHealth),
                ObservedAt = observedAt,
                SourceWorldPosition = sourceWorldPosition,
                TargetWorldPosition = targetWorldPosition
            });
            entityManager.SetComponentData(queueEntity, queue);
            return true;
        }
    }
}
