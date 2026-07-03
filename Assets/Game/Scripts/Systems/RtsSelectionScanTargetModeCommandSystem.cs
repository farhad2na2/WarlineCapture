using Unity.Entities;
using Unity.Mathematics;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RtsSelectionScanTargetModeCommandSystem : ISystem
    {
        private EntityQuery _commandQueueQuery;
        private EntityQuery _runtimeStateQuery;

        public void OnCreate(ref SystemState state)
        {
            _commandQueueQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            _runtimeStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
            state.RequireForUpdate(_commandQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            ProcessPendingRequests(
                state.EntityManager,
                _commandQueueQuery,
                _runtimeStateQuery,
                UnityEngine.Time.frameCount);
        }

        public static bool ProcessPendingRequests(EntityManager em, int currentFrame)
        {
            using EntityQuery commandQueueQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            using EntityQuery runtimeStateQuery = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
            return ProcessPendingRequests(em, commandQueueQuery, runtimeStateQuery, currentFrame);
        }

        public static bool ProcessPendingRequests(
            EntityManager em,
            EntityQuery commandQueueQuery,
            EntityQuery runtimeStateQuery,
            int currentFrame)
        {
            if (commandQueueQuery.IsEmptyIgnoreFilter || runtimeStateQuery.IsEmptyIgnoreFilter)
                return false;

            Entity commandEntity = commandQueueQuery.GetSingletonEntity();
            Entity runtimeEntity = runtimeStateQuery.GetSingletonEntity();
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            if (!HasRequest(commandRequests, RtsSelectionCommandIntentKind.EnterScanTargetMode))
                return false;

            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"scanTargetModeRequestFound requests={commandRequests.Length} frame={currentFrame}");

            for (int i = 0; i < commandRequests.Length;)
            {
                RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
                if (kind == RtsSelectionCommandIntentKind.Move ||
                    kind == RtsSelectionCommandIntentKind.EnterScanTargetMode)
                {
                    commandRequests.RemoveAt(i);
                    continue;
                }

                i++;
            }

            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeEntity);
            ClearQueuedMoveOrder(ref inputState);
            ApplyEnterScanTargetMode(ref inputState, ref runtimeState, currentFrame);
            em.SetComponentData(commandEntity, inputState);
            em.SetComponentData(runtimeEntity, runtimeState);
            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"scanTargetModeApplied activeMode={(TacticalCommandMode)inputState.ActiveCommandMode} " +
                $"requiresTarget={inputState.ActiveCommandModeRequiresWorldTarget} oneShot={inputState.ActiveCommandModeOneShot} " +
                $"ignoreWorldUntil={inputState.IgnoreWorldCommandsUntilFrame} frame={currentFrame}");
            return true;
        }

        private static bool HasRequest(
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            RtsSelectionCommandIntentKind kind)
        {
            for (int i = 0; i < commandRequests.Length; i++)
            {
                if (commandRequests[i].Kind == kind)
                    return true;
            }

            return false;
        }

        private static void ApplyEnterScanTargetMode(
            ref RtsSelectionInputStateComponent inputState,
            ref RuntimeGameplayStateComponent runtimeState,
            int currentFrame)
        {
            float2 pointer = inputState.HasLastKnownPointerPosition != 0
                ? inputState.LastKnownPointerPosition
                : float2.zero;
            ResetSelectionDragState(ref inputState, pointer);
            inputState.IgnoreNextLeftMouseRelease = 1;
            inputState.SkipNextWorldReleaseAfterSelection = 1;
            inputState.IgnoreWorldCommandsUntilFrame = currentFrame + 1;
            ArmCommandMode(
                ref inputState,
                TacticalCommandMode.Scan,
                currentFrame,
                oneShot: true,
                requiresWorldTarget: true);
            runtimeState.SelectionModeActive = 0;
            runtimeState.SuppressNextWorldClick = 1;
        }

        private static void ResetSelectionDragState(ref RtsSelectionInputStateComponent inputState, float2 pointer)
        {
            inputState.DragStart = pointer;
            inputState.DragCurrent = pointer;
            inputState.LastPointerPosition = pointer;
            inputState.PointerPressedOverUi = 0;
            inputState.IsDraggingSelection = 0;
            inputState.SelectionModeHoldArmed = 0;
            inputState.LastLiveSelectionRect = new float4(pointer.x, pointer.y, pointer.x, pointer.y);
            inputState.HasLiveSelectionRect = 0;
            inputState.BoardPassengerDragArmed = 0;
        }

        private static void ClearQueuedMoveOrder(ref RtsSelectionInputStateComponent inputState)
        {
            inputState.QueuedMoveOrderToken++;
            inputState.HasQueuedMoveOrder = 0;
            inputState.QueuedMoveOrderScreenPosition = default;
            inputState.QueuedMoveOrderFrame = -1;
        }

        private static void ArmCommandMode(
            ref RtsSelectionInputStateComponent inputState,
            TacticalCommandMode mode,
            int frame,
            bool oneShot,
            bool requiresWorldTarget)
        {
            inputState.ActiveCommandMode = (int)mode;
            inputState.ActiveCommandModeFrame = frame;
            inputState.ActiveCommandModeOneShot = oneShot ? (byte)1 : (byte)0;
            inputState.ActiveCommandModeRequiresWorldTarget = requiresWorldTarget ? (byte)1 : (byte)0;
            inputState.ActiveBoardCommandDirection = 0;
            inputState.ActiveBoardTransport = Entity.Null;
        }
    }
}
