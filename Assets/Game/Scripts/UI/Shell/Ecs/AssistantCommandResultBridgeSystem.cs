using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantCommandIntentSystem))]
    public partial struct AssistantCommandResultBridgeSystem : ISystem
    {
        private const float DispatchTimeoutSeconds = 5f;
        private const int MaxResultRows = 16;

        private EntityQuery boundaryQuery;
        private EntityQuery selectionResultQuery;
        private EntityQuery moveResultQuery;
        private EntityQuery attackResultQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<AssistantCommandDispatchElement>(),
                ComponentType.ReadWrite<AssistantCommandIntentResultElement>());
            selectionResultQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RtsSelectionInputRequestQueueComponent>(),
                ComponentType.ReadOnly<RtsSelectionCommandResultElement>());
            moveResultQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
                ComponentType.ReadOnly<UnitMoveOrderResultElement>());
            attackResultQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitAttackOrderQueueComponent>(),
                ComponentType.ReadOnly<UnitAttackOrderResultElement>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            DynamicBuffer<AssistantCommandDispatchElement> dispatches =
                state.EntityManager.GetBuffer<AssistantCommandDispatchElement>(boundary);
            if (dispatches.Length == 0)
                return;

            DynamicBuffer<AssistantCommandIntentResultElement> assistantResults =
                state.EntityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
            float now = (float)SystemAPI.Time.ElapsedTime;
            bool changed = false;
            for (int i = 0; i < dispatches.Length; i++)
            {
                AssistantCommandDispatchElement dispatch = dispatches[i];
                if (dispatch.Status != AssistantCommandIntentStatus.Pending &&
                    dispatch.Status != AssistantCommandIntentStatus.Accepted)
                {
                    continue;
                }

                if (now - dispatch.RequestedAt >= DispatchTimeoutSeconds)
                {
                    dispatch.Status = AssistantCommandIntentStatus.TimedOut;
                    dispatch.ReasonCode = (int)Game.Tactical.Contracts.TacticalCommandReasonCode.CommandUnavailable;
                    dispatches[i] = dispatch;
                    AddTerminalResult(
                        assistantResults,
                        dispatch,
                        new FixedString64Bytes("Command response timed out."));
                    changed = true;
                    continue;
                }

                if (!TryResolveDispatchResult(state.EntityManager, dispatch, out bool accepted, out int reasonCode, out FixedString64Bytes message))
                    continue;

                dispatch.Status = accepted
                    ? AssistantCommandIntentStatus.Completed
                    : AssistantCommandIntentStatus.Rejected;
                dispatch.ReasonCode = reasonCode;
                dispatches[i] = dispatch;
                AddTerminalResult(assistantResults, dispatch, message);
                changed = true;
            }

            if (!changed)
                return;

            while (assistantResults.Length > MaxResultRows)
                assistantResults.RemoveAt(0);

            if (state.EntityManager.HasComponent<AssistantStateComponent>(boundary))
            {
                AssistantStateComponent assistant = state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
                assistant.UiDirty = 1;
                state.EntityManager.SetComponentData(boundary, assistant);
            }
        }

        private bool TryResolveDispatchResult(
            EntityManager entityManager,
            AssistantCommandDispatchElement dispatch,
            out bool accepted,
            out int reasonCode,
            out FixedString64Bytes message)
        {
            accepted = false;
            reasonCode = 0;
            message = default;
            switch (dispatch.DownstreamKind)
            {
                case AssistantDownstreamCommandKind.Selection:
                    if (selectionResultQuery.IsEmptyIgnoreFilter)
                        return false;
                    DynamicBuffer<RtsSelectionCommandResultElement> selectionResults =
                        entityManager.GetBuffer<RtsSelectionCommandResultElement>(selectionResultQuery.GetSingletonEntity(), true);
                    for (int i = selectionResults.Length - 1; i >= 0; i--)
                    {
                        RtsSelectionCommandResultElement result = selectionResults[i];
                        if (result.RequestId != dispatch.DownstreamRequestId || result.HasCommandResult == 0)
                            continue;
                        accepted = result.Accepted != 0;
                        reasonCode = result.ReasonCode;
                        message = result.Message;
                        if (message.Length == 0)
                            message = accepted ? new FixedString64Bytes("Selection ready.") : new FixedString64Bytes("Selection rejected.");
                        return true;
                    }
                    return false;

                case AssistantDownstreamCommandKind.MoveOrder:
                    if (moveResultQuery.IsEmptyIgnoreFilter)
                        return false;
                    DynamicBuffer<UnitMoveOrderResultElement> moveResults =
                        entityManager.GetBuffer<UnitMoveOrderResultElement>(moveResultQuery.GetSingletonEntity(), true);
                    for (int i = moveResults.Length - 1; i >= 0; i--)
                    {
                        UnitMoveOrderResultElement result = moveResults[i];
                        if (result.RequestId != dispatch.DownstreamRequestId)
                            continue;
                        accepted = result.Issued != 0;
                        reasonCode = result.RejectionReasonCode;
                        message = accepted ? new FixedString64Bytes("Move order accepted.") : new FixedString64Bytes("Move order rejected.");
                        return true;
                    }
                    return false;

                case AssistantDownstreamCommandKind.AttackOrder:
                    if (attackResultQuery.IsEmptyIgnoreFilter)
                        return false;
                    DynamicBuffer<UnitAttackOrderResultElement> attackResults =
                        entityManager.GetBuffer<UnitAttackOrderResultElement>(attackResultQuery.GetSingletonEntity(), true);
                    for (int i = attackResults.Length - 1; i >= 0; i--)
                    {
                        UnitAttackOrderResultElement result = attackResults[i];
                        if (result.RequestId != dispatch.DownstreamRequestId || result.HasCommandResult == 0)
                            continue;
                        accepted = result.Accepted != 0 || result.Issued != 0;
                        reasonCode = result.ReasonCode;
                        message = result.Message;
                        if (message.Length == 0)
                            message = accepted ? new FixedString64Bytes("Attack order accepted.") : new FixedString64Bytes("Attack order rejected.");
                        return true;
                    }
                    return false;

                case AssistantDownstreamCommandKind.Camera:
                    accepted = true;
                    message = new FixedString64Bytes("Camera focus accepted.");
                    return true;

                default:
                    return false;
            }
        }

        private static void AddTerminalResult(
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            AssistantCommandDispatchElement dispatch,
            FixedString64Bytes message)
        {
            results.Add(new AssistantCommandIntentResultElement
            {
                RequestId = dispatch.AssistantRequestId,
                RecommendationId = dispatch.RecommendationId,
                Kind = dispatch.IntentKind,
                Status = dispatch.Status,
                ReasonCode = dispatch.ReasonCode,
                Message = message
            });
        }
    }
}
