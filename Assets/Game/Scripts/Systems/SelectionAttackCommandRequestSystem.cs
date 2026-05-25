using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionAttackCommandRequestSystem
{
    private readonly System.Collections.Generic.List<RtsSelectionCommandIntentRequestElement> _pendingAttackRequests = new();

    public bool ProcessPendingRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        AttackOrderCommandSystem attackOrderCommandSystem,
        UnitTargetOrderSystem targetOrderSystem,
        AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        _pendingAttackRequests.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Attack)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _pendingAttackRequests.Add(request);
        }

        for (int i = 0; i < _pendingAttackRequests.Count; i++)
        {
            RtsSelectionCommandIntentRequestElement request = _pendingAttackRequests[i];
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            AttackOrderCommandSystem.Result result = attackOrderCommandSystem.TryIssueAttackOrderToClickedUnit(
                em,
                screenPosition,
                targetOrderSystem,
                tryGetClickedUnitEntity,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                request.ExplicitAttackTargetMode != 0);

            AddCommandResult(em, commandEntity, commandResults, ToResultElement(request, result));
        }

        return _pendingAttackRequests.Count > 0;
    }

    private static void AddCommandResult(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults,
        RtsSelectionCommandResultElement result)
    {
        if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
            return;
        }

        fallbackResults.Add(result);
    }

    private static RtsSelectionCommandResultElement ToResultElement(
        RtsSelectionCommandIntentRequestElement request,
        AttackOrderCommandSystem.Result result)
    {
        TacticalCommandResult commandResult = result.HasCommandResult
            ? result.CommandResult
            : default;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.TargetPosition,
            HasCommandResult = result.HasCommandResult ? (byte)1 : (byte)0,
            Accepted = result.Issued ? (byte)1 : (byte)0,
            ReasonCode = result.HasCommandResult ? (int)commandResult.ReasonCode : 0,
            EmitScreenMarker = result.Issued ? (byte)1 : (byte)0,
            HasWorldPosition = result.Issued ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.Issued ? (byte)1 : (byte)0
        };
    }
}
