using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct UnitTransportBoardingDiagnostics
    {
        public EntityQuery CreateDiagnosticLogQueueQuery(ref SystemState state)
        {
            return state.GetEntityQuery(
                ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
        }

        public EntityQuery CreateDiagnosticsStateQuery(ref SystemState state)
        {
            return state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        }

        public bool ShouldQueueTransportBoardingDiagnostics(EntityManager em, EntityQuery diagnosticsStateQuery)
        {
            if (Application.isBatchMode)
                return true;

            return !diagnosticsStateQuery.IsEmptyIgnoreFilter &&
                   em.GetComponentData<RuntimeDiagnosticsStateComponent>(diagnosticsStateQuery.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
        }

        public Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em, EntityQuery diagnosticLogQueueQuery)
        {
            if (diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
            {
                Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
                em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
                em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
                return queueEntity;
            }

            return diagnosticLogQueueQuery.GetSingletonEntity();
        }

        public void EnqueueTransportBoardingDiagnostic(
            EntityManager em,
            Entity diagnosticQueueEntity,
            FixedString512Bytes message)
        {
            if (diagnosticQueueEntity == Entity.Null)
                return;

            DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs =
                em.GetBuffer<TransportBoardingDiagnosticLogComponent>(diagnosticQueueEntity);
            logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
        }

        public void QueueCancelTransportMissingOrInvalid(
            EntityManager em,
            Entity diagnosticQueueEntity,
            Entity passenger,
            Entity transport)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                diagnosticQueueEntity,
                $"[TransportBoard] result=Cancel reason=TransportMissingOrInvalid passenger={DescribeBoardingEntity(em, passenger)} transport={DescribeBoardingEntity(em, transport)}");
        }

        public void QueueWaitingTransportNotLanded(
            EntityManager em,
            Entity diagnosticQueueEntity,
            Entity passenger,
            Entity transport)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                diagnosticQueueEntity,
                $"[TransportBoard] result=Waiting reason=TransportNotLanded passenger={DescribeBoardingEntity(em, passenger)} transport={DescribeBoardingEntity(em, transport)} {DescribeAirState(em, transport)}");
        }

        public void QueueCancelNoSeats(
            EntityManager em,
            Entity diagnosticQueueEntity,
            Entity passenger,
            Entity transport,
            int occupiedSeats,
            int capacity)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                diagnosticQueueEntity,
                $"[TransportBoard] result=Cancel reason=NoSeats passenger={DescribeBoardingEntity(em, passenger)} transport={DescribeBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity}");
        }

        public void QueueWaitingNotReached(
            EntityManager em,
            Entity diagnosticQueueEntity,
            Entity passenger,
            Entity transport,
            TransportBoardingReachState reach,
            int occupiedSeats,
            int capacity)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                diagnosticQueueEntity,
                $"[TransportBoard] result=Waiting reason=NotReached passenger={DescribeBoardingEntity(em, passenger)} transport={DescribeBoardingEntity(em, transport)} " +
                $"passengerCell={reach.PassengerCell} goal={reach.BoardingGoal} transportCell={reach.TransportCell} transportSize={reach.TransportSize} " +
                $"distGoal={reach.DistanceToBoardingGoal} clearance={reach.BoardingClearance} movementFinished={(reach.MovementFinished ? 1 : 0)} " +
                $"hasTarget={(em.HasComponent<UnitTarget>(passenger) ? 1 : 0)} hasRequest={(em.HasComponent<UnitPathRequest>(passenger) ? 1 : 0)} hasFollow={(em.HasComponent<UnitPathFollow>(passenger) ? 1 : 0)} " +
                $"reachedGoal={(reach.ReachedBoardingGoal ? 1 : 0)} settledNearGoal={(reach.SettledNearBoardingGoal ? 1 : 0)} nearTransport={(reach.NearTransportFootprint ? 1 : 0)} seats={occupiedSeats}/{capacity}");
        }

        public void QueueBoarded(
            EntityManager em,
            Entity diagnosticQueueEntity,
            Entity passenger,
            Entity transport,
            int occupiedSeats,
            int capacity)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                diagnosticQueueEntity,
                $"[TransportBoard] result=Boarded passenger={DescribeBoardingEntity(em, passenger)} transport={DescribeBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity}");
        }

        public string DescribeBoardingEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null)
                return "null";
            if (!em.Exists(entity))
                return $"{entity}:missing";

            string sourceName = ResolveSourceName(em, entity);
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

            return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health}";
        }

        public string DescribeAirState(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
                return "air=none";
            if (!em.HasComponent<UnitAirComponent>(entity))
                return "air=missing-state";

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
            return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)}";
        }

        private static string ResolveSourceName(EntityManager em, Entity entity)
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
    }
}
