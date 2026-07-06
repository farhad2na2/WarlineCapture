using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    [UpdateBefore(typeof(UnitTransportBoardingSystem))]
    public partial struct TransportBoardingCommandSystem : ISystem
    {
        private const int DefaultBoardingOrderCapacity = 32;
        private const int AirTransportClickPaddingCells = 24;
        private const int GroundTransportClickPaddingMinCells = 6;
        private const int GroundTransportClickPaddingExtraCells = 4;
        private const int PlaneRampSearchMinRadius = 8;
        private const int PlaneRampSearchFootprintPadding = 4;
        private const float RopeDisembarkMinimumTakeoffHeight = 3f;
        private const float RopeDisembarkDropIntervalSeconds = 0.8f;
        private const float PlaneDoorOpenSeconds = 2.5f;
        public const int TransportBoardingCommandMaxDistanceCells = 36;

        public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
        public delegate bool TryGetClickedCellDelegate(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint);

        public readonly struct Result
        {
            public readonly bool Accepted;
            public readonly TacticalCommandReasonCode ReasonCode;
            public readonly int2 MarkerCell;
            public readonly float3 MarkerPosition;
            public readonly byte MarkerFactionId;
            public readonly FixedString64Bytes Message;

            private Result(bool accepted, TacticalCommandReasonCode reasonCode, int2 markerCell, float3 markerPosition, byte markerFactionId, FixedString64Bytes message)
            {
                Accepted = accepted;
                ReasonCode = reasonCode;
                MarkerCell = markerCell;
                MarkerPosition = markerPosition;
                MarkerFactionId = markerFactionId;
                Message = message;
            }

            public static Result Rejected()
            {
                return Rejected(TacticalCommandReasonCode.CommandUnavailable);
            }

            public static Result Rejected(TacticalCommandReasonCode reasonCode, string message = null)
            {
                string displayMessage = !string.IsNullOrWhiteSpace(message)
                    ? message
                    : TacticalCommandFeedbackText.ToDisplayText(reasonCode);
                return new Result(false, reasonCode, default, default, 0, new FixedString64Bytes(displayMessage ?? string.Empty));
            }

            public static Result AcceptedAt(int2 markerCell, float3 markerPosition, byte markerFactionId, string message = null)
            {
                return new Result(true, TacticalCommandReasonCode.None, markerCell, markerPosition, markerFactionId, new FixedString64Bytes(message ?? string.Empty));
            }
        }

        private readonly struct DisembarkResult
        {
            public readonly bool Accepted;
            public readonly TacticalCommandReasonCode ReasonCode;
            public readonly bool ShowFeedback;
            public readonly FixedString64Bytes Message;

            private DisembarkResult(bool accepted, TacticalCommandReasonCode reasonCode, bool showFeedback, FixedString64Bytes message)
            {
                Accepted = accepted;
                ReasonCode = reasonCode;
                ShowFeedback = showFeedback;
                Message = message;
            }

            public static DisembarkResult Success(string message = null)
            {
                return new DisembarkResult(true, TacticalCommandReasonCode.None, false, new FixedString64Bytes(message ?? string.Empty));
            }

            public static DisembarkResult Rejected(TacticalCommandReasonCode reasonCode, bool showFeedback = true, string message = null)
            {
                string displayMessage = !string.IsNullOrWhiteSpace(message)
                    ? message
                    : TacticalCommandFeedbackText.ToDisplayText(reasonCode);
                return new DisembarkResult(false, reasonCode, showFeedback, new FixedString64Bytes(displayMessage ?? string.Empty));
            }
        }

        private bool _queriesInitialized;
        private EntityQuery _commandQueueQuery;
        private EntityQuery _selectedMoveQuery;
        private EntityQuery _selectedTagQuery;
        private EntityQuery _gridPathingQuery;
        private EntityQuery _allSelectableQuery;
        private EntityQuery _boardingCandidateQuery;
        private EntityQuery _pathingLiveUnitsQuery;

        public void OnCreate(ref SystemState state)
        {
            _commandQueueQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
                ComponentType.ReadWrite<RtsSelectionCommandResultElement>());
            EnsureEntityQueries(state.EntityManager);
            state.RequireForUpdate(_commandQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            long allocationProbeStartBytes = System.GC.GetAllocatedBytesForCurrentThread();
            bool allocationProbeHandled = false;
            try
            {
#endif
            bool handled = ProcessPreResolvedTransportRequests(state.EntityManager);
#if UNITY_EDITOR
            allocationProbeHandled = handled;
#endif
#if UNITY_EDITOR
            }
            finally
            {
                RuntimeDiagnosticsSystem.RecordEditorTransportBoardingUpdateAllocation(
                    System.GC.GetAllocatedBytesForCurrentThread() - allocationProbeStartBytes,
                    allocationProbeHandled);
            }
#endif
        }

        public void EnsureEntityQueries(EntityManager em)
        {
            if (_queriesInitialized)
                return;

            _queriesInitialized = true;
            _selectedMoveQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>());
            _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            _gridPathingQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            _allSelectableQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<LocalToWorld>());
            _boardingCandidateQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>());
            _pathingLiveUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitFootprint>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                }
            });
        }

        public bool ProcessCommandIntentRequests(
            EntityManager em,
            Entity commandEntity,
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
#if UNITY_EDITOR
            long allocationProbeStartBytes = System.GC.GetAllocatedBytesForCurrentThread();
            bool allocationProbeHandled = false;
            try
            {
#endif
            EnsureEntityQueries(em);
            bool handledAny = false;
            for (int i = 0; i < commandRequests.Length;)
            {
                RtsSelectionCommandIntentRequestElement request = commandRequests[i];
                if (!TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(request.Kind))
                {
                    i++;
                    continue;
                }

                commandRequests.RemoveAt(i);
                handledAny = true;
                RtsSelectionCommandResultElement result = request.Kind switch
                {
                    RtsSelectionCommandIntentKind.BoardTransport => request.HasTargetEntity != 0
                        ? ProcessBoardTransportTargetRequest(
                            em,
                            request,
                            transportAirPickupSystem,
                            moveOrderSystem,
                            selectionStateSystem)
                        : ProcessBoardTransportRequest(
                            em,
                            request,
                            transportAirPickupSystem,
                            moveOrderSystem,
                            selectionStateSystem,
                            tryGetClickedUnitEntity,
                            tryGetClickedCell),
                    RtsSelectionCommandIntentKind.BoardSelectedTransport => ProcessBoardSelectedTransportRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem,
                        tryGetClickedUnitEntity),
                    RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem),
                    RtsSelectionCommandIntentKind.BoardNearestSoldiers => ProcessBoardAllSelectedTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.BoardAllSelectedTransport => ProcessBoardAllSelectedTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem),
                    _ => ProcessDisembarkTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem)
                };
                TransportBoardingCommandRoutingSystemHelper.AddCommandResult(em, commandEntity, commandResults, result);
                TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(em, commandEntity, ref commandRequests, ref commandResults);
            }

#if UNITY_EDITOR
            allocationProbeHandled = handledAny;
#endif
            return handledAny;
#if UNITY_EDITOR
            }
            finally
            {
                RuntimeDiagnosticsSystem.RecordEditorTransportBoardingCommandAllocation(
                    System.GC.GetAllocatedBytesForCurrentThread() - allocationProbeStartBytes,
                    allocationProbeHandled);
            }
#endif
        }

        private bool ProcessPreResolvedTransportRequests(EntityManager em)
        {
            if (_commandQueueQuery.IsEmptyIgnoreFilter)
                return false;

            Entity commandEntity = _commandQueueQuery.GetSingletonEntity();
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
                em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            bool handledAny = false;
            var transportCapacitySystem = new UnitTransportCapacitySystem();
            var transportAirPickupSystem = new UnitTransportAirPickupSystem();
            var moveOrderSystem = new UnitMoveOrderSystem();
            var selectionStateSystem = new SelectionStateCompositionSystemHelper();

            for (int i = 0; i < commandRequests.Length;)
            {
                RtsSelectionCommandIntentRequestElement request = commandRequests[i];
                if (!TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(request))
                {
                    i++;
                    continue;
                }

                commandRequests.RemoveAt(i);
                handledAny = true;
                RtsSelectionCommandResultElement result = request.Kind switch
                {
                    RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportTargetRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem,
                        selectionStateSystem),
                    RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                        em,
                        request,
                        transportAirPickupSystem,
                        moveOrderSystem),
                    RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem),
                    _ => ProcessDisembarkTransportRequest(
                        em,
                        request,
                        transportCapacitySystem,
                        moveOrderSystem)
                };

                TransportBoardingCommandRoutingSystemHelper.AddCommandResult(em, commandEntity, commandResults, result);
                TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(em, commandEntity, ref commandRequests, ref commandResults);
            }

            return handledAny;
        }

        private RtsSelectionCommandResultElement ProcessBoardTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryRequestBoardTransportOrderToClickedUnit(
                em,
                screenPosition,
                transportAirPickupSystem,
                moveOrderSystem,
                selectionStateSystem,
                tryGetClickedUnitEntity,
                tryGetClickedCell);

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardTransportTargetRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            Result result = request.HasTargetEntity != 0
                ? TryIssueBoardTransportOrderToTransport(
                    em,
                    request.TargetEntity,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    selectionStateSystem)
                : Result.Rejected();

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardSelectedTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
        {
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryIssueBoardSelectedTransportOrderToClickedPassenger(
                em,
                request.TargetEntity,
                screenPosition,
                transportAirPickupSystem,
                moveOrderSystem,
                tryGetClickedUnitEntity);

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardSelectedTransportPassengerRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportAirPickupSystem transportAirPickupSystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            Result result = request.HasTargetEntity != 0 &&
                request.HasSecondaryTargetEntity != 0
                ? TryIssueBoardSelectedTransportOrderToPassenger(
                    em,
                    request.TargetEntity,
                    request.SecondaryTargetEntity,
                    transportAirPickupSystem,
                    moveOrderSystem)
                : Result.Rejected();

            return TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessBoardAllSelectedTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            if (!TryResolveSelectedBoardTransport(em, selectionStateSystem, out Entity transport))
            {
                return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                    request,
                    false,
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Select a transport vehicle or aircraft first.");
            }

            if (!TryIssueBoardNearestSoldierOrders(
                    em,
                    transport,
                    transportCapacitySystem,
                    out int orderedCount))
            {
                return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                    request,
                    false,
                    TacticalCommandReasonCode.CommandUnavailable,
                    "No nearby units can board this transport.");
            }

            string message = TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllAcceptedMessage(orderedCount);
            return TransportBoardingCommandRoutingSystemHelper.ToBoardAllCommandResultElement(
                request,
                true,
                TacticalCommandReasonCode.None,
                message);
        }

        private RtsSelectionCommandResultElement ProcessDisembarkTransportRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            DisembarkResult result = request.HasTargetEntity != 0
                ? TryDisembarkTransport(
                    em,
                    request.TargetEntity,
                    transportCapacitySystem,
                    moveOrderSystem,
                    _gridPathingQuery,
                    request.TargetCell,
                    request.HasTargetCell)
                : DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport, showFeedback: false);
            return ToDisembarkCommandResultElement(request, result);
        }

        private RtsSelectionCommandResultElement ProcessDisembarkTransportPassengerRequest(
            EntityManager em,
            RtsSelectionCommandIntentRequestElement request,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            DisembarkResult result = request.HasTargetEntity != 0 && request.HasSecondaryTargetEntity != 0
                ? TryDisembarkTransportPassenger(
                    em,
                    request.TargetEntity,
                    request.SecondaryTargetEntity,
                    transportCapacitySystem,
                    moveOrderSystem,
                    _gridPathingQuery,
                    request.TargetCell,
                    request.HasTargetCell)
                : DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing, showFeedback: false);
            return ToDisembarkCommandResultElement(request, result);
        }

        private static RtsSelectionCommandResultElement ToDisembarkCommandResultElement(
            RtsSelectionCommandIntentRequestElement request,
            DisembarkResult result)
        {
            bool accepted = result.Accepted;
            bool showFeedback = !accepted && result.ShowFeedback;
            return new RtsSelectionCommandResultElement
            {
                Kind = request.Kind,
                RequestId = request.RequestId,
                Frame = request.Frame,
                TargetEntity = request.TargetEntity,
                TargetKind = request.HasTargetEntity != 0 ? RtsSelectionCommandTargetKind.Entity : RtsSelectionCommandTargetKind.None,
                CommandMode = (int)TacticalCommandMode.Board,
                HasCommandResult = accepted || showFeedback ? (byte)1 : (byte)0,
                Accepted = accepted ? (byte)1 : (byte)0,
                ReasonCode = accepted ? 0 : (int)result.ReasonCode,
                FeedbackLifetime = accepted
                    ? RtsSelectionCommandFeedbackLifetime.Transient
                    : showFeedback
                        ? RtsSelectionCommandFeedbackLifetime.Transient
                        : RtsSelectionCommandFeedbackLifetime.Hidden,
                HasTargetEntity = request.HasTargetEntity,
                Message = result.Message
            };
        }

        public Result TryRequestBoardTransportOrderToClickedUnit(
            EntityManager em,
            Vector2 screenPosition,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
            EnsureEntityQueries(em);
            if (!TryGetClickedOrNearbyBoardableTransport(
                    screenPosition,
                    em,
                    tryGetClickedUnitEntity,
                    tryGetClickedCell,
                    out Entity transport))
            {
                return Result.Rejected();
            }

            return TryIssueBoardTransportOrderToTransport(
                em,
                transport,
                airPickupSystem,
                moveOrderSystem,
                selectionStateSystem);
        }

        public Result TryIssueBoardTransportOrderToTransport(
            EntityManager em,
            Entity transport,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            EnsureEntityQueries(em);
            selectionStateSystem ??= new SelectionStateCompositionSystemHelper();
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "TransportNotBoardable",
                    true,
                    out bool airTransport,
                    out bool transportLanded,
                    out bool cargoPlaneTransport,
                    out Result rejectedTransportResult))
            {
                return rejectedTransportResult;
            }

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            if (!slotAvailability.HasAnyAvailableSlot)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoSeats transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity}");
                }
                return Result.Rejected(TacticalCommandReasonCode.TransportFull);
            }

            List<Entity> selectedBoardingSourceEntities = new();
            int selectedCount = CollectSelectedBoardingSourceEntities(em, selectionStateSystem, selectedBoardingSourceEntities, out int selectedTagCount, out int selectedMoveCount, out bool usedCachedSelection);
            if (selectedCount == 0)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoSelectedPassengers transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity} " +
                        $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} cached={selectionStateSystem.CachedSelectedMoveEntities.Count}");
                }

                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
            }

            if (_gridPathingQuery.IsEmptyIgnoreFilter)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} usedCache={(usedCachedSelection ? 1 : 0)}");
                return Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
            }

            Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray blocked = blockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
            int2 originalTransportCell = transportCell;
            int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
            using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
            CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
            NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
            NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
            NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();
            HashSet<Entity> ignoredSelectedBoardingEntities = new();
            HashSet<int> ignoredSelectedBoardingOccupiedCells = new();
            BuildSelectedBoardingPassengerIgnoreSets(
                em,
                grid,
                transport,
                selectedBoardingSourceEntities,
                slotAvailability.AvailableSoldierSeats,
                slotAvailability.AvailableVehicleSlots,
                ignoredSelectedBoardingEntities,
                ignoredSelectedBoardingOccupiedCells);

            bool hasPendingAirPickupLanding = false;
            int2 pendingAirPickupCell = default;
            if (airTransport && !transportLanded)
            {
                if (!airPickupSystem.TryFindAirTransportPickupForBoarding(
                        em,
                        transport,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        transportCell,
                        transportSize,
                        selectedBoardingSourceEntities,
                        selectedCount,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        out pendingAirPickupCell))
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount}");
                    return Result.Rejected(
                        TacticalCommandReasonCode.NoEligiblePassengers,
                        "No landing zone for selected transport.");
                }

                transportCell = pendingAirPickupCell;
                hasPendingAirPickupLanding = true;
            }

            List<PendingTransportBoardingOrder> boardingOrders =
                TransportBoardingOrderPlanningSystemHelper.CreatePlannedBoardingOrderList(DefaultBoardingOrderCapacity);
            HashSet<int> reservedBoardingCells = new();
            TransportBoardingPlannedSlotCounts plannedSlots = default;
            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            for (int i = 0; i < selectedCount; i++)
            {
                Entity passenger = selectedBoardingSourceEntities[i];
                byte passengerKind = default;
                int cargoWeight = 0;
                bool hasPassengerKind =
                    passenger != transport &&
                    TryResolveBoardingPassengerKind(em, transport, passenger, out passengerKind, out cargoWeight);
                SelectedTransportBoardingCandidateDecisionKind candidateDecision =
                    TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                        passenger,
                        transport,
                        hasPassengerKind,
                        passengerKind,
                        slotAvailability,
                        plannedSlots);
                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipTransport)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=IsTransport passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNotBoardingCandidate)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NotBoardingCandidate passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNoVehicleSlots)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NoVehicleSlots passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} vehicles={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedVehicleOccupancy(slotAvailability.OccupiedVehicleSlots, plannedSlots)}/{slotAvailability.VehicleCapacity}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNoSoldierSeats)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NoSoldierSeats passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} soldiers={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSoldierOccupancy(slotAvailability.OccupiedSoldierSeats, plannedSlots)}/{slotAvailability.SoldierCapacity}");

                    continue;
                }

                if (!TryCreateTransportBoardingGoalOrder(
                        em,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        transport,
                        transportCell,
                        boardingTransportSize,
                        originalTransportCell,
                        transportSize,
                        passenger,
                        passengerKind,
                        cargoWeight,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        reservedBoardingCells,
                        directBoardingCells,
                        ignoredSelectedBoardingEntities,
                        ignoredSelectedBoardingOccupiedCells,
                        out PendingTransportBoardingOrder boardingOrder,
                        out int2 referenceCell))
                {
                    if (shouldLogTransportBoarding)
                    {
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                            em,
                            $"[TransportBoard] result=NoApproach passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                            $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
                    }

                    continue;
                }

                TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
                    boardingOrders,
                    boardingOrder,
                    slotAvailability,
                    ref plannedSlots);
            }

            if (boardingOrders.Count <= 0)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoBoardingOrders transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} " +
                        $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} usedCache={(usedCachedSelection ? 1 : 0)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity}");
                }

                return Result.Rejected(
                    TacticalCommandReasonCode.NoEligiblePassengers,
                    "No boarding path to selected transport.");
            }

            if (hasPendingAirPickupLanding)
            {
                airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
            }

            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                for (int i = 0; i < boardingOrders.Count; i++)
                {
                    Entity passenger = boardingOrders[i].Passenger;
                    int2 goal = boardingOrders[i].Goal;
                    if (!em.Exists(passenger) ||
                        !IsBoardingCandidateForTransport(em, transport, passenger))
                    {
                        continue;
                    }

                    UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
                    UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, goal);
                    passengerStateSystem.ApplyBoardingOrderState(
                        em,
                        ref boardingStateEcb,
                        passenger,
                        transport,
                        goal,
                        boardingOrders[i].PassengerKind,
                        boardingOrders[i].CargoWeight);

                    if (shouldLogTransportBoarding)
                    {
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                            em,
                            $"[TransportBoard] result=Order passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                            $"from={boardingOrders[i].PassengerCell} goal={goal} kind={boardingOrders[i].PassengerKind} direct={(boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} " +
                            $"soldiers={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSoldierOccupancy(slotAvailability.OccupiedSoldierSeats, plannedSlots)}/{slotAvailability.SoldierCapacity} vehicles={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedVehicleOccupancy(slotAvailability.OccupiedVehicleSlots, plannedSlots)}/{slotAvailability.VehicleCapacity}");
                    }
                }

                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
            return Result.AcceptedAt(
                transportCell,
                markerPosition,
                0,
                TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(cargoPlaneTransport, plannedSlots));
        }

        public Result TryIssueBoardSelectedTransportOrderToClickedPassenger(
            EntityManager em,
            Entity transport,
            Vector2 screenPosition,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
        {
            EnsureEntityQueries(em);
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "SelectedTransportNotBoardable",
                    false,
                    out _,
                    out _,
                    out _,
                    out Result rejectedTransportResult))
                return rejectedTransportResult;

            Entity passenger = Entity.Null;
            if (tryGetClickedUnitEntity == null ||
                !tryGetClickedUnitEntity(screenPosition, em, out passenger) ||
                passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedPassengerNotBoardable passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
            }

            return TryIssueBoardSelectedTransportOrderToPassenger(
                em,
                transport,
                passenger,
                airPickupSystem,
                moveOrderSystem);
        }

        public Result TryIssueBoardSelectedTransportOrderToPassenger(
            EntityManager em,
            Entity transport,
            Entity passenger,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem)
        {
            EnsureEntityQueries(em);
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "SelectedTransportNotBoardable",
                    false,
                    out bool airTransport,
                    out bool transportLanded,
                    out bool cargoPlaneTransport,
                    out Result rejectedTransportResult))
            {
                return rejectedTransportResult;
            }

            if (passenger == Entity.Null ||
                !em.Exists(passenger) ||
                !TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out int cargoWeight) ||
                passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=PassengerNotBoardable passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
            }

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            slotAvailability.GetPassengerKindCounts(passengerKind, out int occupiedSlots, out int slotCapacity, out int availableSlots);
            if (availableSlots <= 0)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoSeats transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} kind={passengerKind} slots={occupiedSlots}/{slotCapacity}");
                return Result.Rejected(TacticalCommandReasonCode.TransportFull);
            }

            if (_gridPathingQuery.IsEmptyIgnoreFilter)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)}");
                return Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
            }

            Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray blocked = blockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
            int2 originalTransportCell = transportCell;
            int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
            using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
            CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
            NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
            NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
            NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();

            bool hasPendingAirPickupLanding = false;
            int2 pendingAirPickupCell = default;
            List<Entity> targetedBoardingSourceEntities = new(1) { passenger };
            if (airTransport && !transportLanded)
            {
                if (!airPickupSystem.TryFindAirTransportPickupForBoarding(
                        em,
                        transport,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        transportCell,
                        transportSize,
                        targetedBoardingSourceEntities,
                        1,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        out pendingAirPickupCell))
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)}");
                    return Result.Rejected(TacticalCommandReasonCode.NoEligiblePassengers);
                }

                transportCell = pendingAirPickupCell;
                hasPendingAirPickupLanding = true;
            }

            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            HashSet<int> targetedReservedBoardingCells = new();
            if (!TryCreateTransportBoardingGoalOrder(
                    em,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transport,
                    transportCell,
                    boardingTransportSize,
                    originalTransportCell,
                    transportSize,
                    passenger,
                    passengerKind,
                    cargoWeight,
                    liveUnitEntityArray,
                    liveUnitGridArray,
                    liveUnitFootprintArray,
                    targetedReservedBoardingCells,
                    directBoardingCells,
                    null,
                    null,
                    out PendingTransportBoardingOrder boardingOrder,
                    out int2 referenceCell))
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoApproach passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                        $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
                }

                return Result.Rejected(TacticalCommandReasonCode.NoEligiblePassengers);
            }

            if (hasPendingAirPickupLanding)
            {
                airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
            }

            UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
            UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, boardingOrder.Goal);
            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                passengerStateSystem.ApplyBoardingOrderState(
                    em,
                    ref boardingStateEcb,
                    passenger,
                    transport,
                    boardingOrder.Goal,
                    passengerKind,
                    cargoWeight);
                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            if (shouldLogTransportBoarding)
            {
                TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=Order passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                    $"from={referenceCell} goal={boardingOrder.Goal} kind={passengerKind} direct={boardingOrder.DirectBoarding} selectedTransport=1 slots={occupiedSlots}/{slotCapacity}");
            }

            float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
            return Result.AcceptedAt(
                transportCell,
                markerPosition,
                0,
                TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(cargoPlaneTransport, passengerKind));
        }

        private static bool TryValidateBoardingTransport(
            EntityManager em,
            Entity transport,
            bool shouldLogTransportBoarding,
            string invalidTransportDiagnosticResult,
            bool appendInvalidTransportAirState,
            out bool airTransport,
            out bool transportLanded,
            out bool cargoPlaneTransport,
            out Result rejectedResult)
        {
            airTransport = false;
            transportLanded = false;
            cargoPlaneTransport = false;
            rejectedResult = default;

            if (!IsBoardablePlayerTransport(em, transport))
            {
                if (shouldLogTransportBoarding)
                {
                    string message = $"[TransportBoard] result={invalidTransportDiagnosticResult} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}";
                    if (appendInvalidTransportAirState)
                        message += $" {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}";
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, message);
                }

                rejectedResult = Result.Rejected(TacticalCommandReasonCode.InvalidTransport);
                return false;
            }

            airTransport = em.HasComponent<UnitAirMovement>(transport);
            transportLanded = IsTransportLandedForBoarding(em, transport);
            cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (!transportLanded && (!airTransport || cargoPlaneTransport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotLanded transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                rejectedResult = Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
                return false;
            }

            if (!transportLanded && em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportBusyRopeDisembark transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                rejectedResult = Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
                return false;
            }

            return true;
        }

        private static bool TryCreateTransportBoardingGoalOrder(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            Entity transport,
            int2 transportCell,
            int2 boardingTransportSize,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            Entity passenger,
            byte passengerKind,
            int cargoWeight,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            HashSet<int> reservedBoardingCells,
            int directBoardingCells,
            HashSet<Entity> ignoredBoardingEntities,
            HashSet<int> ignoredBoardingOccupiedCells,
            out PendingTransportBoardingOrder order,
            out int2 passengerCell)
        {
            order = default;
            passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            if (!TryFindTransportBoardingGoal(
                    em,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    boardingTransportSize,
                    passengerCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    transport,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedBoardingCells,
                    directBoardingCells,
                    passengerFaction,
                    ignoredBoardingEntities,
                    ignoredBoardingOccupiedCells,
                    out int2 goal))
            {
                return false;
            }

            order = TransportBoardingOrderPlanningSystemHelper.CreatePendingBoardingOrder(
                passenger,
                passengerCell,
                goal,
                passengerKind,
                cargoWeight);
            ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
            return true;
        }

        private bool TryResolveSelectedBoardTransport(
            EntityManager em,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            out Entity transport)
        {
            transport = Entity.Null;
            EnsureEntityQueries(em);
            if (selectionStateSystem != null &&
                selectionStateSystem.FocusedUnit != Entity.Null &&
                IsBoardablePlayerTransport(em, selectionStateSystem.FocusedUnit))
            {
                transport = selectionStateSystem.FocusedUnit;
                return true;
            }

            if (_selectedTagQuery.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity selected = entities[i];
                    if (!IsBoardablePlayerTransport(em, selected))
                        continue;

                    transport = selected;
                    return true;
                }
            }

            return false;
        }

        private bool TryIssueBoardNearestSoldierOrders(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            out int orderedCount)
        {
            orderedCount = 0;
            EnsureEntityQueries(em);
            if (!IsBoardablePlayerTransport(em, transport))
                return false;

            bool transportLanded = IsTransportLandedForBoarding(em, transport);
            if (!transportLanded || !transportCapacitySystem.TryEnsureTransportCapacity(em, transport))
                return false;

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            if (!slotAvailability.HasAnyAvailableSlot || _gridPathingQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray blocked = blockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;

            int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
            using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
            CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
            NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
            NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
            NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();

            List<BoardAllTransportCandidate> candidates = new(math.max(1, _boardingCandidateQuery.CalculateEntityCount()));
            CollectNearestBoardingCandidates(em, transport, candidates);
            if (candidates.Count == 0)
                return false;

            List<Entity> candidateEntities = new(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                candidateEntities.Add(candidates[i].Entity);

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 boardingTransportSize = em.HasComponent<UnitAirMovement>(transport) ? new int2(1, 1) : transportSize;
            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            HashSet<int> reservedBoardingCells = new();
            HashSet<Entity> ignoredBoardingEntities = new();
            HashSet<int> ignoredBoardingOccupiedCells = new();
            BuildSelectedBoardingPassengerIgnoreSets(
                em,
                grid,
                transport,
                candidateEntities,
                slotAvailability.AvailableSoldierSeats,
                slotAvailability.AvailableVehicleSlots,
                ignoredBoardingEntities,
                ignoredBoardingOccupiedCells);
            List<PendingTransportBoardingOrder> plannedOrders =
                TransportBoardingOrderPlanningSystemHelper.CreatePlannedBoardingOrderList(
                    TransportBoardingOrderPlanningSystemHelper.ResolvePlannedOrderCapacity(
                        candidates.Count,
                        slotAvailability.TotalAvailableSlots));
            TransportBoardingPlannedSlotCounts plannedSlots = default;

            for (int i = 0; i < candidates.Count; i++)
            {
                Entity passenger = candidates[i].Entity;
                if (!em.Exists(passenger) ||
                    !TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out int cargoWeight))
                {
                    continue;
                }

                if (TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                        passengerKind,
                        slotAvailability,
                        plannedSlots) != TransportBoardingPlannedSlotRejectionKind.None)
                {
                    continue;
                }

                if (!TryCreateTransportBoardingGoalOrder(
                        em,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        transport,
                        transportCell,
                        boardingTransportSize,
                        transportCell,
                        transportSize,
                        passenger,
                        passengerKind,
                        cargoWeight,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        reservedBoardingCells,
                        directBoardingCells,
                        ignoredBoardingEntities,
                        ignoredBoardingOccupiedCells,
                        out PendingTransportBoardingOrder boardingOrder,
                        out _))
                {
                    continue;
                }

                TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
                    plannedOrders,
                    boardingOrder,
                    slotAvailability,
                    ref plannedSlots);
            }

            if (plannedOrders.Count <= 0)
                return false;

            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                for (int i = 0; i < plannedOrders.Count; i++)
                {
                    Entity passenger = plannedOrders[i].Passenger;
                    if (!em.Exists(passenger) ||
                        !IsBoardingCandidateForTransport(em, transport, passenger))
                    {
                        continue;
                    }

                    UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
                    UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, plannedOrders[i].Goal);
                    passengerStateSystem.ApplyBoardingOrderState(
                        em,
                        ref boardingStateEcb,
                        passenger,
                        transport,
                        plannedOrders[i].Goal,
                        plannedOrders[i].PassengerKind,
                        plannedOrders[i].CargoWeight);
                    orderedCount++;
                }

                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            return orderedCount > 0;
        }

        private void CollectNearestBoardingCandidates(
            EntityManager em,
            Entity transport,
            List<BoardAllTransportCandidate> candidates)
        {
            candidates.Clear();
            EnsureEntityQueries(em);
            if (_boardingCandidateQuery.IsEmptyIgnoreFilter || !em.HasComponent<UnitGrid>(transport))
                return;

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _boardingCandidateQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (candidate == transport ||
                        !IsBoardingCandidateForTransport(em, transport, candidate))
                    {
                        continue;
                    }

                    if (em.HasComponent<UnitTransportBoardingTarget>(candidate) &&
                        em.GetComponentData<UnitTransportBoardingTarget>(candidate).Transport != transport)
                    {
                        continue;
                    }

                    int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                    int distance = math.abs(cell.x - transportCell.x) + math.abs(cell.y - transportCell.y);
                    if (distance > TransportBoardingCommandMaxDistanceCells)
                        continue;

                    candidates.Add(new BoardAllTransportCandidate(candidate, distance));
                }
            }

            candidates.Sort();
        }

        public static bool IsWithinTransportBoardingCommandRange(EntityManager em, Entity transport, Entity passenger)
        {
            if (transport == Entity.Null ||
                passenger == Entity.Null ||
                !em.Exists(transport) ||
                !em.Exists(passenger) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitGrid>(passenger))
            {
                return false;
            }

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            int distance = math.abs(passengerCell.x - transportCell.x) + math.abs(passengerCell.y - transportCell.y);
            return distance <= TransportBoardingCommandMaxDistanceCells;
        }

        public bool IsBoardablePlayerTransportClick(
            EntityManager em,
            Vector2 screenPosition,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
            EnsureEntityQueries(em);
            return TryGetClickedOrNearbyBoardableTransport(
                screenPosition,
                em,
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out _,
                false);
        }

        public bool TryResolveBoardablePlayerTransportClick(
            EntityManager em,
            Vector2 screenPosition,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell,
            out Entity transport)
        {
            EnsureEntityQueries(em);
            return TryGetClickedOrNearbyBoardableTransport(
                screenPosition,
                em,
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out transport,
                false);
        }

        private int CollectSelectedBoardingSourceEntities(
            EntityManager em,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            List<Entity> selectedEntities,
            out int selectedTagCount,
            out int selectedMoveCount,
            out bool usedCachedSelection)
        {
            selectedEntities.Clear();
            selectedTagCount = 0;
            selectedMoveCount = 0;
            usedCachedSelection = false;

            EnsureEntityQueries(em);
            selectedMoveCount = _selectedMoveQuery.CalculateEntityCount();
            if (selectedMoveCount > 0)
            {
                selectionStateSystem.CachedSelectedMoveEntities.Clear();
                CollectEntities(em, _selectedMoveQuery, selectedEntities);
                for (int i = 0; i < selectedEntities.Count; i++)
                {
                    Entity entity = selectedEntities[i];
                    if (SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, entity))
                        selectionStateSystem.CachedSelectedMoveEntities.Add(entity);
                }

                selectedTagCount = selectedMoveCount;
                return selectedEntities.Count;
            }

            selectedTagCount = _selectedTagQuery.CalculateEntityCount();
            if (selectedTagCount > 0)
            {
                CollectEntities(em, _selectedTagQuery, selectedEntities);
                return selectedEntities.Count;
            }

            List<Entity> cachedSelectedMoveEntities = selectionStateSystem.CachedSelectedMoveEntities;
            for (int i = cachedSelectedMoveEntities.Count - 1; i >= 0; i--)
            {
                Entity entity = cachedSelectedMoveEntities[i];
                if (!SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, entity))
                {
                    cachedSelectedMoveEntities.RemoveAt(i);
                    continue;
                }

                selectedEntities.Add(entity);
            }

            if (selectedEntities.Count > 0)
                usedCachedSelection = true;
            return selectedEntities.Count;
        }

        private static void BuildSelectedBoardingPassengerIgnoreSets(
            EntityManager em,
            in GridConfig grid,
            Entity transport,
            List<Entity> selectedBoardingSourceEntities,
            int availableSoldierSeats,
            int availableVehicleSlots,
            HashSet<Entity> ignoredEntities,
            HashSet<int> ignoredOccupiedCells)
        {
            if (selectedBoardingSourceEntities == null ||
                ignoredEntities == null ||
                ignoredOccupiedCells == null)
            {
                return;
            }

            int plannedSoldierSeats = 0;
            int plannedVehicleSlots = 0;
            for (int i = 0; i < selectedBoardingSourceEntities.Count; i++)
            {
                Entity passenger = selectedBoardingSourceEntities[i];
                if (passenger == transport ||
                    !em.Exists(passenger) ||
                    !em.HasComponent<UnitGrid>(passenger) ||
                    !em.HasComponent<UnitFootprint>(passenger) ||
                    !TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out _))
                {
                    continue;
                }

                if (!TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
                        passengerKind,
                        availableSoldierSeats,
                        availableVehicleSlots,
                        ref plannedSoldierSeats,
                        ref plannedVehicleSlots))
                {
                    continue;
                }

                ignoredEntities.Add(passenger);
                ReserveFootprintCells(
                    grid,
                    em.GetComponentData<UnitGrid>(passenger).Cell,
                    em.GetComponentData<UnitFootprint>(passenger).Size,
                    ignoredOccupiedCells);
            }
        }

        private static void CollectEntities(EntityManager em, EntityQuery query, List<Entity> entities)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < chunkEntities.Length; i++)
                    entities.Add(chunkEntities[i]);
            }
        }

        private void CollectPathingLiveUnits(
            EntityManager em,
            NativeList<Entity> entities,
            NativeList<UnitGrid> grids,
            NativeList<UnitFootprint> footprints)
        {
            entities.Clear();
            grids.Clear();
            footprints.Clear();

            EnsureEntityQueries(em);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> gridType = em.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
            using NativeArray<ArchetypeChunk> chunks = _pathingLiveUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> chunkEntities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> chunkGrids = chunk.GetNativeArray(ref gridType);
                NativeArray<UnitFootprint> chunkFootprints = chunk.GetNativeArray(ref footprintType);
                for (int i = 0; i < chunkEntities.Length; i++)
                {
                    entities.Add(chunkEntities[i]);
                    grids.Add(chunkGrids[i]);
                    footprints.Add(chunkFootprints[i]);
                }
            }
        }

        private bool TryGetClickedOrNearbyBoardableTransport(
            Vector2 screenPosition,
            EntityManager em,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell,
            out Entity transport,
            bool logDiagnostics = true)
        {
            transport = Entity.Null;
            bool shouldLogTransportBoarding = logDiagnostics && TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            Entity clickedEntity = Entity.Null;
            bool hasClickedEntity = tryGetClickedUnitEntity(screenPosition, em, out clickedEntity);
            if (hasClickedEntity && IsBoardablePlayerTransport(em, clickedEntity))
            {
                transport = clickedEntity;
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedTransport transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                return true;
            }

            if (!tryGetClickedCell(screenPosition, em, out int2 clickedCell, out _))
            {
                if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoClickedCell clicked={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, clickedEntity)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, clickedEntity)}");
                return false;
            }

            if (TryFindNearbyBoardableTransport(em, clickedCell, out transport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NearbyTransport clickedCell={clickedCell} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                return true;
            }

            if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
            {
                TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=ClickedTransportRejected clicked={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, clickedEntity)} " +
                    $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, clickedEntity)}");
            }

            if (hasClickedEntity &&
                em.Exists(clickedEntity) &&
                em.HasComponent<UnitMove>(clickedEntity) &&
                !em.HasComponent<RuntimeBuildingCombatTag>(clickedEntity) &&
                !em.HasComponent<StaticGridBlocker>(clickedEntity))
            {
                return false;
            }

            return false;
        }

        private bool TryFindNearbyBoardableTransport(
            EntityManager em,
            int2 clickedCell,
            out Entity transport)
        {
            transport = Entity.Null;
            EnsureEntityQueries(em);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _allSelectableQuery.ToArchetypeChunkArray(Allocator.Temp);
            int bestScore = int.MaxValue;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (!IsBoardablePlayerTransport(em, candidate))
                        continue;

                    int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                    int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
                    int clickPaddingCells = GetTransportBoardingClickPaddingCells(em, candidate, footprint);
                    if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, clickPaddingCells))
                        continue;

                    int2 delta = clickedCell - cell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    transport = candidate;
                }
            }

            return transport != Entity.Null;
        }

        internal static bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
        {
            if (!em.HasComponent<UnitAirMovement>(transport))
                return true;

            if (!em.HasComponent<UnitAirComponent>(transport) || !em.HasComponent<LocalTransform>(transport))
                return false;

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            bool physicallyGrounded = transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
            return airState.Airborne == 0 &&
                   airState.TakeoffRolling == 0 &&
                   airState.LandingRolling == 0 &&
                   physicallyGrounded &&
                   !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
        }

        internal static int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
        {
            return em.HasComponent<UnitAirMovement>(transport)
                ? TransportBoardingData.AirBoardingClearanceCells
                : TransportBoardingData.BoardingClearanceCells;
        }

        private static int GetTransportBoardingClickPaddingCells(EntityManager em, Entity transport, int2 footprint)
        {
            int footprintMax = math.max(footprint.x, footprint.y);
            if (em.Exists(transport) && em.HasComponent<UnitAirMovement>(transport))
                return math.max(AirTransportClickPaddingCells, footprintMax + AirTransportClickPaddingCells);

            return math.max(GroundTransportClickPaddingMinCells, footprintMax + GroundTransportClickPaddingExtraCells);
        }

        public static bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.IsBoardablePlayerTransport(em, transport);
        }

        public static bool IsBoardingCandidateForTransport(EntityManager em, Entity transport, Entity passenger)
        {
            return TryResolveBoardingPassengerKind(em, transport, passenger, out _, out _);
        }

        public static bool TryResolveBoardingPassengerKind(
            EntityManager em,
            Entity transport,
            Entity passenger,
            out byte passengerKind,
            out int cargoWeight)
        {
            return TransportBoardingCapacitySystemHelper.TryResolveBoardingPassengerKind(
                em,
                transport,
                passenger,
                out passengerKind,
                out cargoWeight);
        }

        public static bool HasAvailableTransportBoardingSlot(
            EntityManager em,
            Entity transport,
            byte passengerKind,
            out int occupied,
            out int capacity)
        {
            return TransportBoardingCapacitySystemHelper.HasAvailableTransportBoardingSlot(
                em,
                transport,
                passengerKind,
                out occupied,
                out capacity);
        }

        public static bool HasAnyAvailableTransportBoardingSlot(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.HasAnyAvailableTransportBoardingSlot(em, transport);
        }

        public static bool IsPotentialVehicleCargoPassenger(EntityManager em, Entity entity)
        {
            return TransportBoardingCapacitySystemHelper.IsPotentialVehicleCargoPassenger(em, entity);
        }

        private static bool IsPotentialVehicleCargoPassenger(EntityManager em, Entity entity, bool allowLoadedPassenger)
        {
            return TransportBoardingCapacitySystemHelper.IsPotentialVehicleCargoPassenger(
                em,
                entity,
                allowLoadedPassenger);
        }

        public static bool IsVehicleBoardingCandidateForTransport(EntityManager em, Entity transport, Entity passenger)
        {
            return TransportBoardingCapacitySystemHelper.IsVehicleBoardingCandidateForTransport(em, transport, passenger);
        }

        public static bool IsCargoPlaneTransport(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.IsCargoPlaneTransport(em, transport);
        }

        public static bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
        {
            return TransportBoardingCapacitySystemHelper.IsSoldierBoardingCandidate(em, entity);
        }

        public static bool TryFindAirTransportPickupCellNearPassenger(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 passengerCell,
            Entity transport,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            byte factionId,
            out int2 pickupCell)
        {
            return TransportBoardingApproachSystemHelper.TryFindAirTransportPickupCellNearPassenger(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                passengerCell,
                transport,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                factionId,
                out pickupCell);
        }

        private static bool TryFindTransportBoardingGoal(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            int directBoardingCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            if (IsCargoPlaneTransport(em, ignoredOccupancyEntity))
            {
                if (TryFindPlaneRampApproachCell(
                    em,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    ignoredOccupancySize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedCells,
                    factionId,
                    ignoredLiveEntities,
                    ignoredOccupiedCells,
                    out goal))
                {
                    return true;
                }

                int fallbackDirectBoardingCells = math.max(directBoardingCells, TransportBoardingData.BoardingClearanceCells);
                return TryFindTransportApproachCell(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    ignoredOccupancySize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedCells,
                    fallbackDirectBoardingCells,
                    factionId,
                    ignoredLiveEntities,
                    ignoredOccupiedCells,
                    out goal);
            }

            return TryFindTransportApproachCell(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                referenceCell,
                passengerFootprint,
                passenger,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize,
                reservedCells,
                directBoardingCells,
                factionId,
                ignoredLiveEntities,
                ignoredOccupiedCells,
                out goal);
        }

        private static bool TryFindPlaneRampApproachCell(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity transport,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            goal = default;
            if (!em.Exists(transport) ||
                !em.HasComponent<UnitTransportPlaneDoorReference>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                return false;
            }

            if (!TryResolvePlaneRampApproachCell(em, grid, transport, out int2 rampCell))
                return false;
            int2 clampedFootprint = UnitFootprintUtility.ClampSize(passengerFootprint);
            int maxRadius = CalculatePlaneRampSearchRadius(clampedFootprint);
            int bestScore = int.MaxValue;
            bool found = false;

            for (int radius = 0; radius <= maxRadius; radius++)
            {
                int minX = rampCell.x - radius;
                int minY = rampCell.y - radius;
                int maxX = rampCell.x + radius;
                int maxY = rampCell.y + radius;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!IsPlaneRampSearchRingCandidate(radius, x, y, minX, minY, maxX, maxY))
                            continue;

                        int2 candidate = new int2(x, y);
                        if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                            continue;

                        if (UnitFootprintUtility.Overlaps(candidate, clampedFootprint, transportCell, transportSize))
                            continue;

                        if (!TransportBoardingApproachSystemHelper.IsTransportApproachPassable(
                                grid,
                                walkable,
                                blocked,
                                friendlyPassFactionIds,
                                occupied,
                                candidate,
                                clampedFootprint,
                                referenceCell,
                                passenger,
                                liveUnitEntities,
                                liveUnitGrids,
                                liveUnitFootprints,
                                transport,
                                ignoredOccupancyCell,
                                ignoredOccupancySize,
                                reservedCells,
                                referenceCell,
                                factionId,
                                candidate.Equals(referenceCell),
                                ignoredLiveEntities,
                                ignoredOccupiedCells))
                        {
                            continue;
                        }

                        int score = ScorePlaneRampApproachCandidate(candidate, rampCell, referenceCell);
                        if (score >= bestScore)
                            continue;

                        bestScore = score;
                        goal = candidate;
                        found = true;
                    }
                }

                if (found)
                    return true;
            }

            return false;
        }

        private static int CalculatePlaneRampSearchRadius(int2 clampedFootprint)
        {
            return math.max(
                PlaneRampSearchMinRadius,
                math.max(clampedFootprint.x, clampedFootprint.y) + PlaneRampSearchFootprintPadding);
        }

        private static bool IsPlaneRampSearchRingCandidate(
            int radius,
            int x,
            int y,
            int minX,
            int minY,
            int maxX,
            int maxY)
        {
            return radius == 0 || x == minX || x == maxX || y == minY || y == maxY;
        }

        private static int ScorePlaneRampApproachCandidate(int2 candidate, int2 rampCell, int2 referenceCell)
        {
            int2 rampDelta = candidate - rampCell;
            int2 passengerDelta = candidate - referenceCell;
            return (math.abs(rampDelta.x) + math.abs(rampDelta.y)) * 100 +
                   math.abs(passengerDelta.x) + math.abs(passengerDelta.y);
        }

        internal static int2 ResolvePlaneRampApproachCell(EntityManager em, in GridConfig grid, Entity transport)
        {
            if (TryResolvePlaneRampApproachCell(em, grid, transport, out int2 rampCell))
                return rampCell;

            if (em.Exists(transport) && em.HasComponent<LocalTransform>(transport))
                return GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);

            return em.Exists(transport) && em.HasComponent<UnitGrid>(transport)
                ? em.GetComponentData<UnitGrid>(transport).Cell
                : default;
        }

        internal static bool TryResolvePlaneRampApproachCell(
            EntityManager em,
            in GridConfig grid,
            Entity transport,
            out int2 rampCell)
        {
            rampCell = default;
            if (!em.Exists(transport) ||
                !em.HasComponent<LocalTransform>(transport) ||
                !em.HasComponent<UnitTransportPlaneDoorReference>(transport))
            {
                return false;
            }

            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            UnitTransportPlaneDoorReference reference = em.GetComponentData<UnitTransportPlaneDoorReference>(transport);
            float3 localApproach = reference.ApproachLocalPosition * transform.Scale;
            float3 worldApproach = transform.Position + math.mul(transform.Rotation, localApproach);
            rampCell = GridUtils.WorldToCell(grid, worldApproach);
            return true;
        }

        public static bool TryFindTransportApproachCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            int directBoardingCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            return TransportBoardingApproachSystemHelper.TryFindTransportApproachCell(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                referenceCell,
                passengerFootprint,
                passenger,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize,
                reservedCells,
                directBoardingCells,
                factionId,
                ignoredLiveEntities,
                ignoredOccupiedCells,
                out goal);
        }

        public static void ReserveFootprintCells(GridConfig grid, int2 cell, int2 footprintSize, HashSet<int> reservedCells)
        {
            TransportBoardingApproachSystemHelper.ReserveFootprintCells(grid, cell, footprintSize, reservedCells);
        }

        public static bool TryFindTransportDisembarkCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            HashSet<int> reservedCells,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            out int2 goal)
        {
            return TransportBoardingApproachSystemHelper.TryFindTransportDisembarkCell(
                grid,
                walkable,
                blocked,
                occupied,
                reservedCells,
                transportCell,
                transportSize,
                referenceCell,
                out goal);
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

        private static bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
        {
            if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
                return false;

            string sourceName = ResolveSourceName(em, transport);
            return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StartRopeDisembarkTransport(
            EntityManager em,
            Entity transport,
            int2 referenceCell,
            UnitMoveOrderSystem moveOrderSystem)
        {
            if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
                return false;

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            if (passengers.Length <= 0)
                return false;

            UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, transport);
            if (em.HasComponent<UnitAirMovement>(transport) &&
                em.HasComponent<UnitAirComponent>(transport) &&
                em.HasComponent<LocalTransform>(transport))
            {
                UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
                LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
                float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
                if (airState.Airborne == 0)
                {
                    transform.Position.y = groundY + math.max(RopeDisembarkMinimumTakeoffHeight, airMovement.CruiseHeight);
                    em.SetComponentData(transport, transform);
                }

                airState.ReturningHome = 0;
                airState.Airborne = 1;
                airState.TakeoffRolling = 0;
                airState.LandingRolling = 0;
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                em.SetComponentData(transport, airState);
            }

            UnitTransportRopeDisembarkRequest request = new()
            {
                ReferenceCell = referenceCell,
                NextDropAt = 0f,
                DropIntervalSeconds = RopeDisembarkDropIntervalSeconds
            };

            if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);

            return true;
        }

        private static void RequestPlaneDoorOpen(EntityManager em, Entity transport)
        {
            if (!em.Exists(transport) || !em.HasComponent<UnitTransportPlaneDoorState>(transport))
                return;

            var request = new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = PlaneDoorOpenSeconds };
            if (em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);
        }

        private static DisembarkResult TryStartPlaneAirdrop(
            EntityManager em,
            Entity transport,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 fallbackReferenceCell,
            int2 requestedDropCell,
            byte hasRequestedDropCell,
            int maxDropCount)
        {
            if (!CanStartPlaneAirdrop(em, transport, out TacticalCommandReasonCode reasonCode))
                return DisembarkResult.Rejected(reasonCode);

            int dropCount = math.min(maxDropCount, passengers.Length);
            if (dropCount <= 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            int2 dropReferenceCell = hasRequestedDropCell != 0 ? requestedDropCell : fallbackReferenceCell;
            if (!TryValidateAirdropReferenceCell(grid, walkable, dropReferenceCell, out TacticalCommandReasonCode dropCellReason))
                return DisembarkResult.Rejected(dropCellReason, message: ResolveAirdropRejectedMessage(dropCellReason));

            TransportBoardingCapacitySystemHelper.CountLoadedPassengerKinds(
                em,
                transport,
                passengers,
                dropCount,
                out int soldierDropCount,
                out int vehicleDropCount);
            if (soldierDropCount <= 0 && vehicleDropCount <= 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            if (!TryValidatePlaneAirdropPassengers(
                    em,
                    transport,
                    passengers,
                    dropCount,
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    dropReferenceCell,
                    out TacticalCommandReasonCode airdropReason,
                    out string airdropMessage))
            {
                return DisembarkResult.Rejected(airdropReason, message: airdropMessage);
            }

            SetPlaneAirdropRequest(em, transport, dropReferenceCell, soldierDropCount, vehicleDropCount);
            RequestPlaneDoorOpen(em, transport);
            return DisembarkResult.Success("Airdrop in progress.");
        }

        public static bool TryIssueDeployDisembark(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            Entity attackTarget,
            int2 attackTargetCell,
            float3 attackTargetPosition,
            byte attackAfterDeploy,
            out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                reasonCode = TacticalCommandReasonCode.InvalidTransport;
                return false;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            List<Entity> passengerSnapshot = new(passengers.Length);
            for (int i = 0; i < passengers.Length; i++)
                passengerSnapshot.Add(passengers[i].Passenger);

            DisembarkResult result = TryDisembarkTransport(
                em,
                transport,
                transportCapacitySystem,
                moveOrderSystem,
                gridPathingQuery,
                requestedDropCell,
                hasRequestedDropCell: 1);

            reasonCode = result.ReasonCode;
            if (!result.Accepted)
                return false;

            if (attackAfterDeploy != 0)
            {
                MarkDeployPassengersForAttack(
                    em,
                    passengerSnapshot,
                    attackTarget,
                    attackTargetCell,
                    attackTargetPosition);
            }

            return true;
        }

        private static void MarkDeployPassengersForAttack(
            EntityManager em,
            List<Entity> passengers,
            Entity attackTarget,
            int2 attackTargetCell,
            float3 attackTargetPosition)
        {
            if (attackTarget == Entity.Null || !em.Exists(attackTarget))
                return;

            if (em.HasComponent<LocalTransform>(attackTarget))
                attackTargetPosition = em.GetComponentData<LocalTransform>(attackTarget).Position;
            if (em.HasComponent<UnitGrid>(attackTarget))
                attackTargetCell = em.GetComponentData<UnitGrid>(attackTarget).Cell;

            UnitTransportDeployAttackTarget attack = new()
            {
                TargetEntity = attackTarget,
                TargetCell = attackTargetCell,
                TargetPosition = attackTargetPosition
            };

            for (int i = 0; i < passengers.Count; i++)
            {
                Entity passenger = passengers[i];
                if (!em.Exists(passenger))
                    continue;

                if (em.HasComponent<UnitTransportDeployAttackTarget>(passenger))
                    em.SetComponentData(passenger, attack);
                else
                    em.AddComponentData(passenger, attack);
            }
        }

        private static bool CanStartPlaneAirdrop(EntityManager em, Entity transport, out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!em.Exists(transport) ||
                !IsCargoPlaneTransport(em, transport) ||
                !em.HasComponent<UnitAirComponent>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                reasonCode = TacticalCommandReasonCode.InvalidTransport;
                return false;
            }

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
            if (airState.TakeoffRolling != 0 || airState.LandingRolling != 0)
            {
                reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            if (airState.Airborne == 0 && !IsTransportLandedForBoarding(em, transport))
            {
                if (!IsTransportPhysicallyAirborneForAirdrop(em, transport, airState))
                {
                    reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                    return false;
                }
            }

            return true;
        }

        private static bool IsTransportPhysicallyAirborneForAirdrop(EntityManager em, Entity transport, UnitAirComponent airState)
        {
            if (!em.HasComponent<LocalTransform>(transport))
                return false;

            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            return transform.Position.y > groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
        }

        private static bool TryValidatePlaneAirdropPassengers(
            EntityManager em,
            Entity transport,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            int dropCount,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode,
            out string message)
        {
            reasonCode = TacticalCommandReasonCode.None;
            message = null;
            int validatedCount = 0;
            int count = math.min(dropCount, passengers.Length);
            for (int i = 0; i < passengers.Length && validatedCount < count; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!em.Exists(passenger))
                    continue;

                if (!TryValidatePlaneAirdropPassenger(
                        em,
                        transport,
                        passenger,
                        validatedCount,
                        grid,
                        walkable,
                        blocked,
                        occupied,
                        dropReferenceCell,
                        out reasonCode,
                        out message))
                {
                    return false;
                }

                validatedCount++;
            }

            if (validatedCount > 0)
                return true;

            reasonCode = TacticalCommandReasonCode.TransportPassengerMissing;
            return false;
        }

        private static bool TryValidatePlaneAirdropPassenger(
            EntityManager em,
            Entity transport,
            Entity passenger,
            int dropOrdinal,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode,
            out string message)
        {
            reasonCode = TacticalCommandReasonCode.None;
            message = null;
            byte passengerKind = TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(em, transport, passenger);
            if (!UnitTransportAirdropSystem.HasResolvableDropVisualPrefab(em, transport, passengerKind))
            {
                reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                message = passengerKind == UnitTransportPassengerKind.Vehicle
                    ? "Emergency drop visual missing."
                    : "Parachute visual missing.";
                return false;
            }

            int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                ? em.GetComponentData<UnitFootprint>(passenger).Size
                : new int2(1, 1);
            if (UnitTransportAirdropSystem.TryFindLandingCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    dropReferenceCell,
                    passengerFootprint,
                    dropOrdinal + passenger.Index,
                    out _))
            {
                return true;
            }

            reasonCode = TacticalCommandReasonCode.TargetBlocked;
            message = "No clear airdrop landing zone.";
            return false;
        }

        private static bool TryValidateAirdropReferenceCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!GridUtils.InBounds(dropReferenceCell, grid.Width, grid.Height))
            {
                reasonCode = TacticalCommandReasonCode.TargetOutOfBounds;
                return false;
            }

            int index = GridUtils.CellToIndex(dropReferenceCell, grid.Width);
            if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
            {
                reasonCode = TacticalCommandReasonCode.TargetBlocked;
                return false;
            }

            return true;
        }

        private static string ResolveAirdropRejectedMessage(TacticalCommandReasonCode reasonCode)
        {
            return reasonCode == TacticalCommandReasonCode.TargetBlocked
                ? "Cargo drop blocked."
                : TacticalCommandFeedbackText.ToDisplayText(reasonCode);
        }

        private static void SetPlaneAirdropRequest(
            EntityManager em,
            Entity transport,
            int2 dropReferenceCell,
            int soldierDropCount,
            int vehicleDropCount)
        {
            int totalDropCount = soldierDropCount + vehicleDropCount;
            byte dropMode = soldierDropCount > 0 && vehicleDropCount > 0
                ? UnitTransportAirdropMode.Mixed
                : vehicleDropCount > 0
                    ? UnitTransportAirdropMode.VehicleOnly
                    : UnitTransportAirdropMode.SoldierOnly;
            UnitTransportAirdropRequest request = new()
            {
                DropReferenceCell = dropReferenceCell,
                NextDropAt = 0f,
                DropIntervalSeconds = 0.65f,
                DropCount = totalDropCount,
                SoldierDropCount = soldierDropCount,
                VehicleDropCount = vehicleDropCount,
                DropMode = dropMode
            };

            if (em.HasComponent<UnitTransportAirdropRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);

            if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
                em.RemoveComponent<UnitTransportRopeDisembarkRequest>(transport);

            if (em.HasComponent<UnitAirComponent>(transport))
            {
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
                airState.ReturningHome = 0;
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                em.SetComponentData(transport, airState);
            }
        }

        private static bool TryPlanPassengerDisembarkCells(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            HashSet<int> reservedDisembarkCells,
            bool usePlaneRampDisembark,
            int2 referenceCell,
            int2 transportCell,
            int2 transportSize,
            int2 passengerFootprint,
            out int2 disembarkCell,
            out int2 rolloutCell)
        {
            bool foundDisembarkCell = usePlaneRampDisembark
                ? TransportBoardingApproachSystemHelper.TryFindPlaneRampDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    referenceCell,
                    passengerFootprint,
                    out disembarkCell)
                : TryFindTransportDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    transportCell,
                    transportSize,
                    referenceCell,
                    out disembarkCell);
            if (!foundDisembarkCell)
            {
                rolloutCell = default;
                return false;
            }

            ReserveFootprintCells(grid, disembarkCell, passengerFootprint, reservedDisembarkCells);
            rolloutCell = disembarkCell;
            if (usePlaneRampDisembark &&
                TransportBoardingApproachSystemHelper.TryFindPlaneRampRolloutCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    referenceCell,
                    transportCell,
                    passengerFootprint,
                    out int2 candidateRolloutCell))
            {
                rolloutCell = candidateRolloutCell;
                ReserveFootprintCells(grid, rolloutCell, passengerFootprint, reservedDisembarkCells);
            }

            return true;
        }

        private static DisembarkResult TryDisembarkTransport(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            byte hasRequestedDropCell)
        {
            if (!em.Exists(transport) ||
                !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                gridPathingQuery.IsEmptyIgnoreFilter)
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
            }

            Entity gridEntity = gridPathingQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 referenceCell = transportCell;
            if (em.HasComponent<LocalTransform>(transport))
                referenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);

            if (IsRopeDisembarkTransport(em, transport))
            {
                return StartRopeDisembarkTransport(em, transport, referenceCell, moveOrderSystem)
                    ? DisembarkResult.Success()
                    : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            bool cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (cargoPlaneTransport && (hasRequestedDropCell != 0 || !IsTransportLandedForBoarding(em, transport)))
            {
                return TryStartPlaneAirdrop(
                    em,
                    transport,
                    passengers,
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    referenceCell,
                    requestedDropCell,
                    hasRequestedDropCell,
                    passengers.Length);
            }

            int2 rampReferenceCell = default;
            bool usePlaneRampDisembark = cargoPlaneTransport &&
                                         TryResolvePlaneRampApproachCell(
                                             em,
                                             grid,
                                             transport,
                                             out rampReferenceCell);
            if (usePlaneRampDisembark)
                referenceCell = rampReferenceCell;

            List<Entity> passengerSnapshot = new(passengers.Length);
            for (int i = 0; i < passengers.Length; i++)
                passengerSnapshot.Add(passengers[i].Passenger);
            if (passengerSnapshot.Count == 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            passengers.Clear();
            HashSet<int> reservedDisembarkCells = new();
            List<Entity> remainingPassengers = new();
            List<Entity> disembarkingPassengers = new();
            List<int2> disembarkCells = new();
            List<int2> rolloutCells = new();
            for (int i = 0; i < passengerSnapshot.Count; i++)
            {
                Entity passenger = passengerSnapshot[i];
                if (!em.Exists(passenger))
                    continue;

                int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                    ? em.GetComponentData<UnitFootprint>(passenger).Size
                    : new int2(1, 1);
                if (!TryPlanPassengerDisembarkCells(
                        grid,
                        walkable,
                        blocked,
                        occupied,
                        reservedDisembarkCells,
                        usePlaneRampDisembark,
                        referenceCell,
                        transportCell,
                        transportSize,
                        passengerFootprint,
                        out int2 cell,
                        out int2 rolloutCell))
                {
                    remainingPassengers.Add(passenger);
                    continue;
                }

                disembarkingPassengers.Add(passenger);
                disembarkCells.Add(cell);
                rolloutCells.Add(rolloutCell);
            }

            if (usePlaneRampDisembark && disembarkingPassengers.Count > 0)
                RequestPlaneDoorOpen(em, transport);

            EntityCommandBuffer ecb = new(Allocator.Temp);
            for (int i = 0; i < disembarkingPassengers.Count; i++)
            {
                Entity passenger = disembarkingPassengers[i];
                int2 cell = disembarkCells[i];
                if (!em.Exists(passenger))
                    continue;

                moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
                moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
                moveOrderSystem.RemoveComponentIfPresent<UnitTransportCargoPassenger>(em, ecb, passenger);
                UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, passenger);

                if (em.HasComponent<UnitGrid>(passenger))
                    ecb.SetComponent(passenger, new UnitGrid { Cell = cell });
                if (em.HasComponent<LocalTransform>(passenger))
                {
                    LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
                    transform.Position = GridUtils.CellToWorldCenter(grid, cell);
                    ecb.SetComponent(passenger, transform);
                }
            }
            ecb.Playback(em);
            ecb.Dispose();

            for (int i = 0; i < disembarkingPassengers.Count; i++)
            {
                Entity passenger = disembarkingPassengers[i];
                if (em.Exists(passenger))
                {
                    UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
                    if (usePlaneRampDisembark &&
                        i < rolloutCells.Count &&
                        !rolloutCells[i].Equals(disembarkCells[i]))
                    {
                        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, rolloutCells[i]);
                    }
                }
            }

            if (remainingPassengers.Count > 0 && em.Exists(transport) && em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                DynamicBuffer<UnitTransportPassengerElement> remainingBuffer = em.GetBuffer<UnitTransportPassengerElement>(transport);
                for (int i = 0; i < remainingPassengers.Count; i++)
                {
                    Entity passenger = remainingPassengers[i];
                    if (em.Exists(passenger))
                        remainingBuffer.Add(new UnitTransportPassengerElement { Passenger = passenger });
                }
            }

            return disembarkingPassengers.Count > 0 || remainingPassengers.Count != passengerSnapshot.Count
                ? DisembarkResult.Success()
                : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
        }

        private static DisembarkResult TryDisembarkTransportPassenger(
            EntityManager em,
            Entity transport,
            Entity passenger,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            byte hasRequestedDropCell)
        {
            if (!em.Exists(transport) ||
                !em.Exists(passenger) ||
                !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int passengerIndex = IndexOfPassenger(passengers, passenger);
            if (passengerIndex < 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            if (IsRopeDisembarkTransport(em, transport))
            {
                if (passengerIndex > 0)
                {
                    UnitTransportPassengerElement selected = passengers[passengerIndex];
                    passengers.RemoveAt(passengerIndex);
                    passengers.Insert(0, selected);
                }

                int2 ropeReferenceCell = em.HasComponent<UnitGrid>(transport)
                    ? em.GetComponentData<UnitGrid>(transport).Cell
                    : default;
                if (em.HasComponent<LocalTransform>(transport) && !gridPathingQuery.IsEmptyIgnoreFilter)
                {
                    Entity gridEntity = gridPathingQuery.GetSingletonEntity();
                    GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                    ropeReferenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);
                }

                return StartRopeDisembarkTransport(em, transport, ropeReferenceCell, moveOrderSystem)
                    ? DisembarkResult.Success()
                    : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            if (!em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                gridPathingQuery.IsEmptyIgnoreFilter)
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
            }

            Entity pathingGridEntity = gridPathingQuery.GetSingletonEntity();
            GridConfig pathingGrid = em.GetComponentData<GridConfig>(pathingGridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(pathingGridEntity).AsNativeArray();
            NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(pathingGridEntity).Blocked;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(pathingGridEntity).Occupied;
            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 groundReferenceCell = transportCell;
            if (em.HasComponent<LocalTransform>(transport))
                groundReferenceCell = GridUtils.WorldToCell(pathingGrid, em.GetComponentData<LocalTransform>(transport).Position);

            HashSet<int> reservedDisembarkCells = new();
            bool cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (cargoPlaneTransport && (hasRequestedDropCell != 0 || !IsTransportLandedForBoarding(em, transport)))
            {
                if (!CanStartPlaneAirdrop(em, transport, out TacticalCommandReasonCode reasonCode))
                    return DisembarkResult.Rejected(reasonCode);

                int2 dropReferenceCell = hasRequestedDropCell != 0 ? requestedDropCell : groundReferenceCell;
                if (!TryValidateAirdropReferenceCell(pathingGrid, walkable, dropReferenceCell, out TacticalCommandReasonCode dropCellReason))
                    return DisembarkResult.Rejected(dropCellReason, message: ResolveAirdropRejectedMessage(dropCellReason));

                if (passengerIndex > 0)
                {
                    UnitTransportPassengerElement selected = passengers[passengerIndex];
                    passengers.RemoveAt(passengerIndex);
                    passengers.Insert(0, selected);
                }

                byte passengerKind = TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(em, transport, passenger);
                if (!TryValidatePlaneAirdropPassenger(
                        em,
                        transport,
                        passenger,
                        0,
                        pathingGrid,
                        walkable,
                        blocked,
                        occupied,
                        dropReferenceCell,
                        out TacticalCommandReasonCode airdropReason,
                        out string airdropMessage))
                {
                    return DisembarkResult.Rejected(airdropReason, message: airdropMessage);
                }

                int soldierDropCount = passengerKind == UnitTransportPassengerKind.Vehicle ? 0 : 1;
                int vehicleDropCount = passengerKind == UnitTransportPassengerKind.Vehicle ? 1 : 0;
                SetPlaneAirdropRequest(em, transport, dropReferenceCell, soldierDropCount, vehicleDropCount);
                RequestPlaneDoorOpen(em, transport);
                return DisembarkResult.Success("Airdrop in progress.");
            }

            int2 rampReferenceCell = default;
            bool usePlaneRampDisembark = cargoPlaneTransport &&
                                         TryResolvePlaneRampApproachCell(
                                             em,
                                             pathingGrid,
                                             transport,
                                             out rampReferenceCell);
            if (usePlaneRampDisembark)
                groundReferenceCell = rampReferenceCell;

            int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                ? em.GetComponentData<UnitFootprint>(passenger).Size
                : new int2(1, 1);
            if (!TryPlanPassengerDisembarkCells(
                    pathingGrid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    usePlaneRampDisembark,
                    groundReferenceCell,
                    transportCell,
                    transportSize,
                    passengerFootprint,
                    out int2 cell,
                    out int2 rolloutCell))
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            passengers.RemoveAt(passengerIndex);
            if (usePlaneRampDisembark)
                RequestPlaneDoorOpen(em, transport);

            EntityCommandBuffer ecb = new(Allocator.Temp);
            moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportCargoPassenger>(em, ecb, passenger);
            UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, passenger);

            if (em.HasComponent<UnitGrid>(passenger))
                ecb.SetComponent(passenger, new UnitGrid { Cell = cell });
            if (em.HasComponent<LocalTransform>(passenger))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
                transform.Position = GridUtils.CellToWorldCenter(pathingGrid, cell);
                ecb.SetComponent(passenger, transform);
            }

            ecb.Playback(em);
            ecb.Dispose();
            UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
            if (usePlaneRampDisembark && !rolloutCell.Equals(cell))
                UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, rolloutCell);
            return DisembarkResult.Success();
        }

        private static int IndexOfPassenger(DynamicBuffer<UnitTransportPassengerElement> passengers, Entity passenger)
        {
            for (int i = 0; i < passengers.Length; i++)
            {
                if (passengers[i].Passenger == passenger)
                    return i;
            }

            return -1;
        }

        private static bool IsKnownPersonnelTransport(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity))
                return false;

            if (em.HasComponent<UnitTransportCapacity>(entity) &&
                math.max(0, em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity) > 0)
            {
                return true;
            }

            return new UnitTransportCapacitySystem().ResolveTransportCapacity(em, entity) > 0;
        }

        private static bool IsPlayerFaction(EntityManager em, Entity entity)
        {
            return em.Exists(entity) &&
                   em.HasComponent<Faction>(entity) &&
                   FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
        }

    }
}
