using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
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
