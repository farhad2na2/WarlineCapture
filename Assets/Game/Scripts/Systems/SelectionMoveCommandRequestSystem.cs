using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionMoveCommandRequestSystem
{
    public bool ProcessPendingRequests(
        EntityManager em,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionOrderMarkerSystem orderMarkerSystem,
        SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
        SelectedMoveOrderCommandSystem.ClickedUnitResolver tryGetClickedUnit,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        bool processed = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Move)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            SelectedMoveOrderCommandSystem.Result result = selectedMoveOrderCommandSystem.TryIssueMoveOrder(
                em,
                screenPosition,
                selectedMoveQuery,
                gridConfigQuery,
                moveOrderSystem,
                orderMarkerSystem,
                tryGetClickedUnit,
                tryGetClickedCell,
                request.Frame);

            commandResults.Add(ToResultElement(request, result));
            processed = true;
        }

        return processed;
    }

    private static RtsSelectionCommandResultElement ToResultElement(
        RtsSelectionCommandIntentRequestElement request,
        SelectedMoveOrderCommandSystem.Result result)
    {
        TacticalCommandResult commandResult = result.CommandResult;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            ScreenPosition = request.ScreenPosition,
            HasCommandResult = 1,
            Accepted = commandResult.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)commandResult.ReasonCode,
            EmitScreenMarker = result.EmitScreenMarker ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.ShowWorldMarkers ? (byte)1 : (byte)0
        };
    }
}
