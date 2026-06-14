using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportBoardingSystem : ISystem
{
    private const int DiagnosticLogIntervalFrames = 180;
    private EntityQuery _boardingTargetQuery;
    private EntityQuery _diagnosticLogQueueQuery;
    private EntityQuery _diagnosticsStateQuery;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private ComponentLookup<UnitTransportCapacity> _transportCapacityLookup;
    private BufferLookup<UnitTransportPassengerElement> _passengerLookup;
    private ComponentLookup<UnitGrid> _unitGridLookup;
    private ComponentLookup<UnitFootprint> _unitFootprintLookup;
    private ComponentLookup<LocalTransform> _localTransformLookup;
    private ComponentLookup<UnitAirMovement> _airMovementLookup;
    private ComponentLookup<UnitAirComponent> _airComponentLookup;
    private ComponentLookup<UnitTransportRopeDisembarkRequest> _ropeDisembarkLookup;
    private ComponentLookup<UnitTarget> _unitTargetLookup;
    private ComponentLookup<UnitPathRequest> _pathRequestLookup;
    private ComponentLookup<UnitPathFollow> _pathFollowLookup;

    public void OnCreate(ref SystemState state)
    {
        _boardingTargetQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitTransportBoardingTarget>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new[] { ComponentType.ReadOnly<Disabled>() }
        });
        var diagnosticSystem = new UnitTransportBoardingDiagnosticSystem();
        _diagnosticLogQueueQuery = diagnosticSystem.CreateDiagnosticLogQueueQuery(ref state);
        _diagnosticsStateQuery = diagnosticSystem.CreateDiagnosticsStateQuery(ref state);
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        _transportCapacityLookup = state.GetComponentLookup<UnitTransportCapacity>(true);
        _passengerLookup = state.GetBufferLookup<UnitTransportPassengerElement>(true);
        _unitGridLookup = state.GetComponentLookup<UnitGrid>(true);
        _unitFootprintLookup = state.GetComponentLookup<UnitFootprint>(true);
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        _airMovementLookup = state.GetComponentLookup<UnitAirMovement>(true);
        _airComponentLookup = state.GetComponentLookup<UnitAirComponent>(true);
        _ropeDisembarkLookup = state.GetComponentLookup<UnitTransportRopeDisembarkRequest>(true);
        _unitTargetLookup = state.GetComponentLookup<UnitTarget>(true);
        _pathRequestLookup = state.GetComponentLookup<UnitPathRequest>(true);
        _pathFollowLookup = state.GetComponentLookup<UnitPathFollow>(true);
        state.RequireForUpdate<UnitTransportBoardingTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        _entityStorageInfoLookup.Update(ref state);
        _transportCapacityLookup.Update(ref state);
        _passengerLookup.Update(ref state);
        _unitGridLookup.Update(ref state);
        _unitFootprintLookup.Update(ref state);
        _localTransformLookup.Update(ref state);
        _airMovementLookup.Update(ref state);
        _airComponentLookup.Update(ref state);
        _ropeDisembarkLookup.Update(ref state);
        _unitTargetLookup.Update(ref state);
        _pathRequestLookup.Update(ref state);
        _pathFollowLookup.Update(ref state);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        EntityManager em = state.EntityManager;
        var diagnosticSystem = new UnitTransportBoardingDiagnosticSystem();
        bool shouldLogTransportBoarding = diagnosticSystem.ShouldQueueTransportBoardingDiagnostics(
            em,
            _diagnosticsStateQuery);
        bool shouldLogPeriodicTransportBoarding =
            shouldLogTransportBoarding && Time.frameCount % DiagnosticLogIntervalFrames == 0;
        var passengerStateSystem = new UnitTransportPassengerStateSystem();
        using NativeList<BoardingDecision> decisions = new(
            math.max(0, _boardingTargetQuery.CalculateEntityCount()),
            Allocator.TempJob);

        JobHandle collectHandle = new CollectBoardingDecisionsJob
        {
            Decisions = decisions,
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            TransportCapacityLookup = _transportCapacityLookup,
            PassengerLookup = _passengerLookup,
            UnitGridLookup = _unitGridLookup,
            UnitFootprintLookup = _unitFootprintLookup,
            LocalTransformLookup = _localTransformLookup,
            AirMovementLookup = _airMovementLookup,
            AirComponentLookup = _airComponentLookup,
            RopeDisembarkLookup = _ropeDisembarkLookup,
            UnitTargetLookup = _unitTargetLookup,
            PathRequestLookup = _pathRequestLookup,
            PathFollowLookup = _pathFollowLookup
        }.Schedule(state.Dependency);
        collectHandle.Complete();
        state.Dependency = collectHandle;

        Entity diagnosticQueueEntity = shouldLogTransportBoarding
            ? diagnosticSystem.EnsureTransportBoardingDiagnosticQueue(em, _diagnosticLogQueueQuery)
            : Entity.Null;

        for (int i = 0; i < decisions.Length; i++)
        {
            BoardingDecision decision = decisions[i];
            switch (decision.Kind)
            {
                case BoardingDecisionKind.TransportMissingOrInvalid:
                    if (shouldLogTransportBoarding)
                        diagnosticSystem.QueueCancelTransportMissingOrInvalid(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                    ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                    break;

                case BoardingDecisionKind.WaitingTransportNotLanded:
                    if (shouldLogPeriodicTransportBoarding)
                        diagnosticSystem.QueueWaitingTransportNotLanded(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                    break;

                case BoardingDecisionKind.NoSeats:
                    if (shouldLogTransportBoarding)
                        diagnosticSystem.QueueCancelNoSeats(
                            em,
                            diagnosticQueueEntity,
                            decision.Passenger,
                            decision.Transport,
                            decision.OccupiedSeats,
                            decision.Capacity);
                    ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                    break;

                case BoardingDecisionKind.WaitingNotReached:
                    if (shouldLogPeriodicTransportBoarding)
                    {
                        diagnosticSystem.QueueWaitingNotReached(
                            em,
                            diagnosticQueueEntity,
                            decision.Passenger,
                            decision.Transport,
                            decision.Reach,
                            decision.OccupiedSeats,
                            decision.Capacity);
                    }
                    break;

                case BoardingDecisionKind.ReadyToBoard:
                    if (!em.Exists(decision.Transport) ||
                        !em.HasComponent<UnitTransportCapacity>(decision.Transport) ||
                        !em.HasBuffer<UnitTransportPassengerElement>(decision.Transport))
                    {
                        if (shouldLogTransportBoarding)
                            diagnosticSystem.QueueCancelTransportMissingOrInvalid(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                        ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                        break;
                    }

                    DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(decision.Transport);
                    int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(decision.Transport).SoldierCapacity);
                    if (passengers.Length >= capacity)
                    {
                        if (shouldLogTransportBoarding)
                            diagnosticSystem.QueueCancelNoSeats(
                                em,
                                diagnosticQueueEntity,
                                decision.Passenger,
                                decision.Transport,
                                passengers.Length,
                                capacity);
                        ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                        break;
                    }

                    int occupiedSeats = passengerStateSystem.BoardPassenger(
                        em,
                        ref ecb,
                        passengers,
                        decision.Passenger,
                        decision.Transport);
                    if (shouldLogTransportBoarding)
                        diagnosticSystem.QueueBoarded(em, diagnosticQueueEntity, decision.Passenger, decision.Transport, occupiedSeats, capacity);
                    break;
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private enum BoardingDecisionKind : byte
    {
        ReadyToBoard,
        TransportMissingOrInvalid,
        WaitingTransportNotLanded,
        NoSeats,
        WaitingNotReached
    }

    private struct BoardingDecision
    {
        public Entity Passenger;
        public Entity Transport;
        public BoardingDecisionKind Kind;
        public int OccupiedSeats;
        public int Capacity;
        public TransportBoardingReachState Reach;
    }

    [BurstCompile]
    [WithNone(typeof(Disabled))]
    private partial struct CollectBoardingDecisionsJob : IJobEntity
    {
        public NativeList<BoardingDecision> Decisions;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        [ReadOnly] public ComponentLookup<UnitTransportCapacity> TransportCapacityLookup;
        [ReadOnly] public BufferLookup<UnitTransportPassengerElement> PassengerLookup;
        [ReadOnly] public ComponentLookup<UnitGrid> UnitGridLookup;
        [ReadOnly] public ComponentLookup<UnitFootprint> UnitFootprintLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public ComponentLookup<UnitAirMovement> AirMovementLookup;
        [ReadOnly] public ComponentLookup<UnitAirComponent> AirComponentLookup;
        [ReadOnly] public ComponentLookup<UnitTransportRopeDisembarkRequest> RopeDisembarkLookup;
        [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
        [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
        [ReadOnly] public ComponentLookup<UnitPathFollow> PathFollowLookup;

        private void Execute(
            Entity entity,
            in UnitTransportBoardingTarget boarding,
            in UnitGrid passengerGrid,
            in LocalTransform passengerTransform)
        {
            Entity transport = boarding.Transport;
            if (!EntityStorageInfoLookup.Exists(transport) ||
                !TransportCapacityLookup.HasComponent(transport) ||
                !PassengerLookup.HasBuffer(transport) ||
                !UnitGridLookup.HasComponent(transport) ||
                !UnitFootprintLookup.HasComponent(transport) ||
                !LocalTransformLookup.HasComponent(transport))
            {
                Decisions.AddNoResize(new BoardingDecision
                {
                    Passenger = entity,
                    Transport = transport,
                    Kind = BoardingDecisionKind.TransportMissingOrInvalid
                });
                return;
            }

            if (!IsTransportLandedForBoarding(transport))
            {
                Decisions.AddNoResize(new BoardingDecision
                {
                    Passenger = entity,
                    Transport = transport,
                    Kind = BoardingDecisionKind.WaitingTransportNotLanded
                });
                return;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = PassengerLookup[transport];
            int capacity = math.max(0, TransportCapacityLookup[transport].SoldierCapacity);
            if (passengers.Length >= capacity)
            {
                Decisions.AddNoResize(new BoardingDecision
                {
                    Passenger = entity,
                    Transport = transport,
                    Kind = BoardingDecisionKind.NoSeats,
                    OccupiedSeats = passengers.Length,
                    Capacity = capacity
                });
                return;
            }

            TransportBoardingReachState reach = EvaluateReach(
                entity,
                transport,
                passengerGrid.Cell,
                boarding.Goal,
                passengerTransform.Position);
            if (!reach.ReachedTransport)
            {
                Decisions.AddNoResize(new BoardingDecision
                {
                    Passenger = entity,
                    Transport = transport,
                    Kind = BoardingDecisionKind.WaitingNotReached,
                    OccupiedSeats = passengers.Length,
                    Capacity = capacity,
                    Reach = reach
                });
                return;
            }

            Decisions.AddNoResize(new BoardingDecision
            {
                Passenger = entity,
                Transport = transport,
                Kind = BoardingDecisionKind.ReadyToBoard,
                OccupiedSeats = passengers.Length,
                Capacity = capacity,
                Reach = reach
            });
        }

        private bool IsTransportLandedForBoarding(Entity transport)
        {
            if (!AirMovementLookup.HasComponent(transport))
                return true;

            if (!AirComponentLookup.HasComponent(transport) || !LocalTransformLookup.HasComponent(transport))
                return false;

            UnitAirComponent airState = AirComponentLookup[transport];
            LocalTransform transform = LocalTransformLookup[transport];
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            bool physicallyGrounded = transform.Position.y <= groundY + UnitTransportBoardingRuleSystem.AirBoardingGroundedHeightTolerance;
            return airState.Airborne == 0 &&
                   airState.TakeoffRolling == 0 &&
                   airState.LandingRolling == 0 &&
                   physicallyGrounded &&
                   !RopeDisembarkLookup.HasComponent(transport);
        }

        private TransportBoardingReachState EvaluateReach(
            Entity passenger,
            Entity transport,
            int2 passengerCell,
            int2 boardingGoal,
            float3 passengerPosition)
        {
            int2 transportCell = UnitGridLookup[transport].Cell;
            int2 transportSize = UnitFootprintLookup[transport].Size;
            float3 transportPosition = LocalTransformLookup[transport].Position;
            passengerPosition.y = transportPosition.y;
            bool airTransport = AirMovementLookup.HasComponent(transport);
            int boardingClearance = airTransport
                ? UnitTransportBoardingRuleSystem.AirBoardingClearanceCells
                : UnitTransportBoardingRuleSystem.BoardingClearanceCells;
            bool movementFinished =
                !UnitTargetLookup.HasComponent(passenger) &&
                !PathRequestLookup.HasComponent(passenger) &&
                !PathFollowLookup.HasComponent(passenger);
            int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
            bool reachedBoardingGoal = passengerCell.Equals(boardingGoal);
            int distanceToBoardingGoal = math.max(math.abs(passengerCell.x - boardingGoal.x), math.abs(passengerCell.y - boardingGoal.y));
            bool settledNearBoardingGoal = movementFinished && distanceToBoardingGoal <= (airTransport ? 0 : boardingClearance);
            bool nearTransportFootprint = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, passengerCell, boardingClearance);
            bool boardingGoalNearTransport = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, boardingGoal, boardingClearance);
            float boardDistanceSq = airTransport ? 1.25f * 1.25f : 4f;
            int boardCellDistance = airTransport ? 1 : 2;
            bool reachedTransport =
                nearTransportFootprint ||
                (boardingGoalNearTransport && (reachedBoardingGoal || settledNearBoardingGoal)) ||
                math.distancesq(passengerPosition, transportPosition) <= boardDistanceSq ||
                math.max(math.abs(passengerCell.x - transportCell.x), math.abs(passengerCell.y - transportCell.y)) <= boardCellDistance;

            return new TransportBoardingReachState(
                transportCell,
                transportSize,
                passengerCell,
                boardingGoal,
                boardingClearance,
                movementFinished,
                airTransport,
                reachedBoardingGoal,
                distanceToBoardingGoal,
                settledNearBoardingGoal,
                nearTransportFootprint,
                boardingGoalNearTransport,
                reachedTransport);
        }
    }
}
