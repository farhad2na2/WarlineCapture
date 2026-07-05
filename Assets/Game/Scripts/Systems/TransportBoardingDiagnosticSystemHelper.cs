using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingDiagnosticSystemHelper
    {
        public static string DescribeTransportBoardingEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null)
                return "null";
            if (!em.Exists(entity))
                return $"{entity}:missing";

            string sourceName = ResolveUnitSourceName(em, entity);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = "<unnamed>";

            string cell = em.HasComponent<UnitGrid>(entity)
                ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
                : "no-cell";
            string faction = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id.ToString()
                : "no-faction";
            string health = em.HasComponent<UnitHealth>(entity)
                ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
                : "no-health";
            string capacity = em.HasComponent<UnitTransportCapacity>(entity)
                ? em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity.ToString()
                : "no-capacity";
            string passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
                ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length.ToString()
                : "no-passengers";

            return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health} seats={passengers}/{capacity}";
        }

        public static string DescribeTransportAirState(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
                return "air=none";
            if (!em.HasComponent<UnitAirComponent>(entity))
                return "air=missing-state";

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
            return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)} airdrop={(em.HasComponent<UnitTransportAirdropRequest>(entity) ? 1 : 0)}";
        }

        public static bool ShouldQueueTransportBoardingDiagnostics(EntityManager em)
        {
            if (Application.isBatchMode)
                return true;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            return !query.IsEmptyIgnoreFilter &&
                em.GetComponentData<RuntimeDiagnosticsStateComponent>(query.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
        }

        public static void EnqueueTransportBoardingDiagnostic(EntityManager em, FixedString512Bytes message)
        {
            Entity queueEntity = EnsureTransportBoardingDiagnosticQueue(em);
            DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs = em.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
            logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
        }

        private static string ResolveUnitSourceName(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity))
                return string.Empty;

            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(sourceName))
                    return sourceName;
            }

            return em.GetName(entity);
        }

        private static Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();

            Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
            em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
            return queueEntity;
        }
    }
}
