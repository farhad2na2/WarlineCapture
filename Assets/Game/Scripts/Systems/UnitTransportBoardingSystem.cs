using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportBoardingSystem : ISystem
{
    private const int DiagnosticLogIntervalFrames = 180;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _diagnosticsStateQuery;

    public void OnCreate(ref SystemState state)
    {
        var diagnosticSystem = new UnitTransportBoardingDiagnosticSystem();
        _diagnosticLogQueueQuery = diagnosticSystem.CreateDiagnosticLogQueueQuery(ref state);
        _diagnosticsStateQuery = diagnosticSystem.CreateDiagnosticsStateQuery(ref state);
        state.RequireForUpdate<UnitTransportBoardingTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        EntityManager em = state.EntityManager;
        var diagnosticSystem = new UnitTransportBoardingDiagnosticSystem();
        bool shouldLogTransportBoarding = diagnosticSystem.ShouldQueueTransportBoardingDiagnostics(
            em,
            _diagnosticsStateQuery);
        bool shouldLogPeriodicTransportBoarding =
            shouldLogTransportBoarding && Time.frameCount % DiagnosticLogIntervalFrames == 0;
        Entity diagnosticQueueEntity = shouldLogTransportBoarding
            ? diagnosticSystem.EnsureTransportBoardingDiagnosticQueue(em, _diagnosticLogQueueQuery)
            : Entity.Null;
        var boardingRuleSystem = new UnitTransportBoardingRuleSystem();
        var passengerStateSystem = new UnitTransportPassengerStateSystem();

        foreach (var (boarding, passengerGrid, passengerTransform, entity) in
                 SystemAPI.Query<RefRO<UnitTransportBoardingTarget>, RefRO<UnitGrid>, RefRO<LocalTransform>>()
                     .WithNone<Disabled>()
                     .WithEntityAccess())
        {
            Entity transport = boarding.ValueRO.Transport;
            if (!em.Exists(transport) ||
                !em.HasComponent<UnitTransportCapacity>(transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                if (shouldLogTransportBoarding)
                    diagnosticSystem.QueueCancelTransportMissingOrInvalid(em, diagnosticQueueEntity, entity, transport);
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            if (!boardingRuleSystem.IsTransportLandedForBoarding(em, transport))
            {
                if (shouldLogPeriodicTransportBoarding)
                    diagnosticSystem.QueueWaitingTransportNotLanded(em, diagnosticQueueEntity, entity, transport);
                continue;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
            if (passengers.Length >= capacity)
            {
                if (shouldLogTransportBoarding)
                    diagnosticSystem.QueueCancelNoSeats(em, diagnosticQueueEntity, entity, transport, passengers.Length, capacity);
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            UnitTransportBoardingRuleSystem.ReachState reach = boardingRuleSystem.EvaluateReach(
                em,
                entity,
                transport,
                passengerGrid.ValueRO.Cell,
                boarding.ValueRO.Goal,
                passengerTransform.ValueRO.Position);
            if (!reach.ReachedTransport)
            {
                if (shouldLogPeriodicTransportBoarding)
                {
                    diagnosticSystem.QueueWaitingNotReached(
                        em,
                        diagnosticQueueEntity,
                        entity,
                        transport,
                        reach,
                        passengers.Length,
                        capacity);
                }

                continue;
            }

            int occupiedSeats = passengerStateSystem.BoardPassenger(
                em,
                ref ecb,
                passengers,
                entity,
                transport);
            if (shouldLogTransportBoarding)
                diagnosticSystem.QueueBoarded(em, diagnosticQueueEntity, entity, transport, occupiedSeats, capacity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

}
