using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
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
                    : ResolveReasonText(reasonCode);
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
                    : ResolveReasonText(reasonCode);
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
    }
}
