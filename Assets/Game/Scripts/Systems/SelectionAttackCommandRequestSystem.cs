using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionAttackCommandRequestSystem
{
    public bool ProcessPendingRequests(
        EntityManager em,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        AttackOrderCommandSystem attackOrderCommandSystem,
        UnitTargetOrderSystem targetOrderSystem,
        AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        bool processed = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Attack)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            AttackOrderCommandSystem.Result result = attackOrderCommandSystem.TryIssueAttackOrderToClickedUnit(
                em,
                screenPosition,
                targetOrderSystem,
                tryGetClickedUnitEntity,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                request.ExplicitAttackTargetMode != 0);

            commandResults.Add(ToResultElement(request, result));
            processed = true;
        }

        return processed;
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
