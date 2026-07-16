using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    public partial struct UnitTransportBoardingSystem : ISystem
    {
        private const int DiagnosticLogIntervalFrames = 180;
        private EntityQuery _boardingTargetQuery;
        private EntityQuery _diagnosticLogQueueQuery;
        private EntityQuery _diagnosticsStateQuery;
        private EntityQuery _gridQuery;
        private EntityStorageInfoLookup _entityStorageInfoLookup;
        private ComponentLookup<UnitTransportCapacity> _transportCapacityLookup;
        private ComponentLookup<UnitTransportCargoCapacity> _transportCargoCapacityLookup;
        private ComponentLookup<UnitTransportCargoPassenger> _transportCargoPassengerLookup;
        private ComponentLookup<UnitTransportBoardingTarget> _boardingTargetLookup;
        private BufferLookup<UnitTransportPassengerElement> _passengerLookup;
        private ComponentLookup<UnitGrid> _unitGridLookup;
        private ComponentLookup<UnitFootprint> _unitFootprintLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<UnitAirMovement> _airMovementLookup;
        private ComponentLookup<UnitAirComponent> _airComponentLookup;
        private ComponentLookup<UnitTransportPlaneDoorReference> _planeDoorReferenceLookup;
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
            var diagnostics = new UnitTransportBoardingDiagnostics();
            _diagnosticLogQueueQuery = diagnostics.CreateDiagnosticLogQueueQuery(ref state);
            _diagnosticsStateQuery = diagnostics.CreateDiagnosticsStateQuery(ref state);
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
            _transportCapacityLookup = state.GetComponentLookup<UnitTransportCapacity>(true);
            _transportCargoCapacityLookup = state.GetComponentLookup<UnitTransportCargoCapacity>(true);
            _transportCargoPassengerLookup = state.GetComponentLookup<UnitTransportCargoPassenger>(true);
            _boardingTargetLookup = state.GetComponentLookup<UnitTransportBoardingTarget>(true);
            _passengerLookup = state.GetBufferLookup<UnitTransportPassengerElement>(true);
            _unitGridLookup = state.GetComponentLookup<UnitGrid>(true);
            _unitFootprintLookup = state.GetComponentLookup<UnitFootprint>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _airMovementLookup = state.GetComponentLookup<UnitAirMovement>(true);
            _airComponentLookup = state.GetComponentLookup<UnitAirComponent>(true);
            _planeDoorReferenceLookup = state.GetComponentLookup<UnitTransportPlaneDoorReference>(true);
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
            _transportCargoCapacityLookup.Update(ref state);
            _transportCargoPassengerLookup.Update(ref state);
            _boardingTargetLookup.Update(ref state);
            _passengerLookup.Update(ref state);
            _unitGridLookup.Update(ref state);
            _unitFootprintLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _airMovementLookup.Update(ref state);
            _airComponentLookup.Update(ref state);
            _planeDoorReferenceLookup.Update(ref state);
            _ropeDisembarkLookup.Update(ref state);
            _unitTargetLookup.Update(ref state);
            _pathRequestLookup.Update(ref state);
            _pathFollowLookup.Update(ref state);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            EntityManager em = state.EntityManager;
            byte hasGrid = 0;
            GridConfig grid = default;
            if (!_gridQuery.IsEmptyIgnoreFilter)
            {
                grid = em.GetComponentData<GridConfig>(_gridQuery.GetSingletonEntity());
                hasGrid = 1;
            }

            var diagnostics = new UnitTransportBoardingDiagnostics();
            bool shouldLogTransportBoarding = diagnostics.ShouldQueueTransportBoardingDiagnostics(
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
                TransportCargoCapacityLookup = _transportCargoCapacityLookup,
                TransportCargoPassengerLookup = _transportCargoPassengerLookup,
                BoardingTargetLookup = _boardingTargetLookup,
                PassengerLookup = _passengerLookup,
                UnitGridLookup = _unitGridLookup,
                UnitFootprintLookup = _unitFootprintLookup,
                LocalTransformLookup = _localTransformLookup,
                AirMovementLookup = _airMovementLookup,
                AirComponentLookup = _airComponentLookup,
                PlaneDoorReferenceLookup = _planeDoorReferenceLookup,
                RopeDisembarkLookup = _ropeDisembarkLookup,
                UnitTargetLookup = _unitTargetLookup,
                PathRequestLookup = _pathRequestLookup,
                PathFollowLookup = _pathFollowLookup,
                Grid = grid,
                HasGrid = hasGrid
            }.Schedule(state.Dependency);
            collectHandle.Complete();
            state.Dependency = collectHandle;

            Entity diagnosticQueueEntity = shouldLogTransportBoarding
                ? diagnostics.EnsureTransportBoardingDiagnosticQueue(em, _diagnosticLogQueueQuery)
                : Entity.Null;

            for (int i = 0; i < decisions.Length; i++)
            {
                BoardingDecision decision = decisions[i];
                switch (decision.Kind)
                {
                    case BoardingDecisionKind.TransportMissingOrInvalid:
                        if (shouldLogTransportBoarding)
                            diagnostics.QueueCancelTransportMissingOrInvalid(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                        ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                        break;

                    case BoardingDecisionKind.WaitingTransportNotLanded:
                        if (shouldLogPeriodicTransportBoarding)
                            diagnostics.QueueWaitingTransportNotLanded(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                        break;

                    case BoardingDecisionKind.NoSeats:
                        if (shouldLogTransportBoarding)
                            diagnostics.QueueCancelNoSeats(
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
                            diagnostics.QueueWaitingNotReached(
                                em,
                                diagnosticQueueEntity,
                                decision.Passenger,
                                decision.Transport,
                                decision.Reach,
                                decision.OccupiedSeats,
                                decision.Capacity);
                        }

                        if (decision.Reach.MovementFinished)
                            ReissueBoardingMoveIfStopped(em, ref ecb, decision.Passenger, decision.Reach.BoardingGoal);
                        break;

                    case BoardingDecisionKind.ReadyToBoard:
                        if (!em.Exists(decision.Transport) ||
                            !em.HasComponent<UnitTransportCapacity>(decision.Transport) ||
                            !em.HasBuffer<UnitTransportPassengerElement>(decision.Transport))
                        {
                            if (shouldLogTransportBoarding)
                                diagnostics.QueueCancelTransportMissingOrInvalid(em, diagnosticQueueEntity, decision.Passenger, decision.Transport);
                            ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                            break;
                        }

                        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(decision.Transport);
                        int capacity = ResolvePassengerCapacity(em, decision.Transport, decision.PassengerKind);
                        int occupied = CountPassengerOccupancy(em, decision.Transport, decision.PassengerKind);
                        if (occupied >= capacity)
                        {
                            if (shouldLogTransportBoarding)
                                diagnostics.QueueCancelNoSeats(
                                    em,
                                    diagnosticQueueEntity,
                                    decision.Passenger,
                                    decision.Transport,
                                    occupied,
                                    capacity);
                            ecb.RemoveComponent<UnitTransportBoardingTarget>(decision.Passenger);
                            break;
                        }

                        passengerStateSystem.BoardPassenger(
                            em,
                            ref ecb,
                            passengers,
                            decision.Passenger,
                            decision.Transport,
                            decision.PassengerKind,
                            decision.CargoWeight);
                        if (shouldLogTransportBoarding)
                            diagnostics.QueueBoarded(em, diagnosticQueueEntity, decision.Passenger, decision.Transport, occupied + 1, capacity);
                        break;
                }
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static void ReissueBoardingMoveIfStopped(
            EntityManager em,
            ref EntityCommandBuffer ecb,
            Entity passenger,
            int2 boardingGoal)
        {
            if (!em.Exists(passenger) || em.HasComponent<Disabled>(passenger) || em.HasComponent<UnitAirMovement>(passenger))
                return;

            UnitMoveOrderRequestSystem.ApplyTargetPathMoveOrder(em, ecb, passenger, boardingGoal);
            if (!em.HasComponent<ManualMoveOrderTag>(passenger))
                ecb.AddComponent<ManualMoveOrderTag>(passenger);
            if (!em.HasComponent<ManualMoveGroupMemberTag>(passenger))
                ecb.AddComponent<ManualMoveGroupMemberTag>(passenger);
            if (em.HasComponent<UnitPathRetryCooldown>(passenger))
                ecb.RemoveComponent<UnitPathRetryCooldown>(passenger);
        }

        private static void SetOrAdd<T>(EntityManager em, ref EntityCommandBuffer ecb, Entity entity, T component)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity))
                ecb.SetComponent(entity, component);
            else
                ecb.AddComponent(entity, component);
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
            public byte PassengerKind;
            public int CargoWeight;
            public TransportBoardingReachState Reach;
        }

        [BurstCompile]
        [WithNone(typeof(Disabled))]
        private partial struct CollectBoardingDecisionsJob : IJobEntity
        {
            public NativeList<BoardingDecision> Decisions;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
            [ReadOnly] public ComponentLookup<UnitTransportCapacity> TransportCapacityLookup;
            [ReadOnly] public ComponentLookup<UnitTransportCargoCapacity> TransportCargoCapacityLookup;
            [ReadOnly] public ComponentLookup<UnitTransportCargoPassenger> TransportCargoPassengerLookup;
            [ReadOnly] public ComponentLookup<UnitTransportBoardingTarget> BoardingTargetLookup;
            [ReadOnly] public BufferLookup<UnitTransportPassengerElement> PassengerLookup;
            [ReadOnly] public ComponentLookup<UnitGrid> UnitGridLookup;
            [ReadOnly] public ComponentLookup<UnitFootprint> UnitFootprintLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<UnitAirMovement> AirMovementLookup;
            [ReadOnly] public ComponentLookup<UnitAirComponent> AirComponentLookup;
            [ReadOnly] public ComponentLookup<UnitTransportPlaneDoorReference> PlaneDoorReferenceLookup;
            [ReadOnly] public ComponentLookup<UnitTransportRopeDisembarkRequest> RopeDisembarkLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> PathFollowLookup;
            public GridConfig Grid;
            public byte HasGrid;

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
                byte passengerKind = UnitTransportBoardingCapacityRules.NormalizePassengerKind(boarding.PassengerKind);
                int cargoWeight = math.max(0, boarding.CargoWeight);
                int capacity = ResolvePassengerCapacity(transport, passengerKind);
                int occupied = CountPassengerOccupancy(transport, passengers, passengerKind);
                if (occupied >= capacity)
                {
                    Decisions.AddNoResize(new BoardingDecision
                    {
                        Passenger = entity,
                        Transport = transport,
                        Kind = BoardingDecisionKind.NoSeats,
                        OccupiedSeats = occupied,
                        Capacity = capacity,
                        PassengerKind = passengerKind,
                        CargoWeight = cargoWeight
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
                        OccupiedSeats = occupied,
                        Capacity = capacity,
                        PassengerKind = passengerKind,
                        CargoWeight = cargoWeight,
                        Reach = reach
                    });
                    return;
                }

                Decisions.AddNoResize(new BoardingDecision
                {
                    Passenger = entity,
                    Transport = transport,
                    Kind = BoardingDecisionKind.ReadyToBoard,
                    OccupiedSeats = occupied,
                    Capacity = capacity,
                    PassengerKind = passengerKind,
                    CargoWeight = cargoWeight,
                    Reach = reach
                });
            }

            private int ResolvePassengerCapacity(Entity transport, byte passengerKind)
            {
                bool hasCargoCapacity = TransportCargoCapacityLookup.HasComponent(transport);
                UnitTransportCargoCapacity cargoCapacity = hasCargoCapacity
                    ? TransportCargoCapacityLookup[transport]
                    : default;
                return UnitTransportBoardingCapacityRules.ResolveCapacity(
                    TransportCapacityLookup[transport],
                    hasCargoCapacity,
                    cargoCapacity,
                    passengerKind);
            }

            private int CountPassengerOccupancy(Entity transport, DynamicBuffer<UnitTransportPassengerElement> passengers, byte passengerKind)
            {
                int count = 0;
                for (int i = 0; i < passengers.Length; i++)
                {
                    Entity passenger = passengers[i].Passenger;
                    bool passengerExists = EntityStorageInfoLookup.Exists(passenger);
                    bool hasCargoPassenger = passengerExists && TransportCargoPassengerLookup.HasComponent(passenger);
                    UnitTransportCargoPassenger cargoPassenger = hasCargoPassenger
                        ? TransportCargoPassengerLookup[passenger]
                        : default;
                    bool hasBoardingTarget = passengerExists && BoardingTargetLookup.HasComponent(passenger);
                    UnitTransportBoardingTarget boardingTarget = hasBoardingTarget
                        ? BoardingTargetLookup[passenger]
                        : default;
                    if (UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
                            transport,
                            passengerKind,
                            passengerExists,
                            hasCargoPassenger,
                            cargoPassenger,
                            hasBoardingTarget,
                            boardingTarget))
                    {
                        count++;
                    }
                }

                return count;
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
                bool physicallyGrounded = transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
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
                LocalTransform transportTransform = LocalTransformLookup[transport];
                float3 transportPosition = transportTransform.Position;
                passengerPosition.y = transportPosition.y;
                bool airTransport = AirMovementLookup.HasComponent(transport);
                bool planeRampTransport = airTransport && PlaneDoorReferenceLookup.HasComponent(transport);
                int boardingClearance = airTransport
                    ? TransportBoardingData.AirBoardingClearanceCells
                    : TransportBoardingData.BoardingClearanceCells;
                bool movementFinished =
                    !UnitTargetLookup.HasComponent(passenger) &&
                    !PathRequestLookup.HasComponent(passenger) &&
                    !PathFollowLookup.HasComponent(passenger);
                int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
                bool reachedBoardingGoal = passengerCell.Equals(boardingGoal);
                int distanceToBoardingGoal = math.max(math.abs(passengerCell.x - boardingGoal.x), math.abs(passengerCell.y - boardingGoal.y));
                int boardingGoalTolerance = planeRampTransport
                    ? TransportBoardingData.AirBoardingClearanceCells
                    : airTransport ? 0 : boardingClearance;
                bool settledNearBoardingGoal = movementFinished && distanceToBoardingGoal <= boardingGoalTolerance;
                bool nearTransportFootprint = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, passengerCell, boardingClearance);
                bool boardingGoalNearTransport = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, boardingGoal, boardingClearance);
                int maxRampGoalDistanceFromTransport = math.max(16, math.cmax(UnitFootprintUtility.ClampSize(transportSize)) + 12);
                bool boardingGoalNearPlaneRampArea =
                    planeRampTransport &&
                    math.max(math.abs(boardingGoal.x - transportCell.x), math.abs(boardingGoal.y - transportCell.y)) <= maxRampGoalDistanceFromTransport;
                bool reachedResolvedPlaneRamp = false;
                if (planeRampTransport && HasGrid != 0)
                {
                    UnitTransportPlaneDoorReference doorReference = PlaneDoorReferenceLookup[transport];
                    float3 localApproach = doorReference.ApproachLocalPosition * transportTransform.Scale;
                    float3 worldApproach = transportTransform.Position + math.mul(transportTransform.Rotation, localApproach);
                    int2 rampCell = GridUtils.WorldToCell(Grid, worldApproach);
                    int distanceToRampCell = math.max(math.abs(passengerCell.x - rampCell.x), math.abs(passengerCell.y - rampCell.y));
                    int rampBoardingTolerance = math.max(4, TransportBoardingData.AirBoardingClearanceCells + 3);
                    reachedResolvedPlaneRamp = distanceToRampCell <= rampBoardingTolerance;
                }

                bool reachedPlaneRampGoal =
                    planeRampTransport &&
                    (reachedResolvedPlaneRamp ||
                     (boardingGoalNearPlaneRampArea &&
                      distanceToBoardingGoal <= TransportBoardingData.AirBoardingClearanceCells));
                float boardDistanceSq = airTransport ? 1.25f * 1.25f : 4f;
                int boardCellDistance = airTransport ? 1 : 2;
                bool reachedTransport =
                    nearTransportFootprint ||
                    reachedPlaneRampGoal ||
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

        private static int ResolvePassengerCapacity(EntityManager em, Entity transport, byte passengerKind)
        {
            bool hasCargoCapacity = em.HasComponent<UnitTransportCargoCapacity>(transport);
            UnitTransportCargoCapacity cargoCapacity = hasCargoCapacity
                ? em.GetComponentData<UnitTransportCargoCapacity>(transport)
                : default;
            return UnitTransportBoardingCapacityRules.ResolveCapacity(
                em.GetComponentData<UnitTransportCapacity>(transport),
                hasCargoCapacity,
                cargoCapacity,
                passengerKind);
        }

        private static int CountPassengerOccupancy(EntityManager em, Entity transport, byte passengerKind)
        {
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                return 0;

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int count = 0;
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                bool passengerExists = em.Exists(passenger);
                bool hasCargoPassenger = passengerExists && em.HasComponent<UnitTransportCargoPassenger>(passenger);
                UnitTransportCargoPassenger cargoPassenger = hasCargoPassenger
                    ? em.GetComponentData<UnitTransportCargoPassenger>(passenger)
                    : default;
                bool hasBoardingTarget = passengerExists && em.HasComponent<UnitTransportBoardingTarget>(passenger);
                UnitTransportBoardingTarget boardingTarget = hasBoardingTarget
                    ? em.GetComponentData<UnitTransportBoardingTarget>(passenger)
                    : default;
                if (UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
                        transport,
                        passengerKind,
                        passengerExists,
                        hasCargoPassenger,
                        cargoPassenger,
                        hasBoardingTarget,
                        boardingTarget))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
