using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionMoveCommandRequestSystem
{
    private readonly System.Collections.Generic.List<RtsSelectionCommandIntentRequestElement> _pendingMoveRequests = new();

    public bool ProcessPendingRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        IReadOnlyList<Entity> cachedSelectedMoveEntities,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionOrderMarkerSystem orderMarkerSystem,
        SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
        SelectedMoveOrderCommandSystem.ClickedUnitResolver tryGetClickedUnit,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        _pendingMoveRequests.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Move)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _pendingMoveRequests.Add(request);
        }

        for (int i = 0; i < _pendingMoveRequests.Count; i++)
        {
            RtsSelectionCommandIntentRequestElement request = _pendingMoveRequests[i];
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"requestProcess requestId={request.RequestId} requestFrame={request.Frame} " +
                    $"screen={screenPosition} pendingCount={_pendingMoveRequests.Count}");
            }

            SelectedMoveOrderCommandSystem.Result result = selectedMoveOrderCommandSystem.TryIssueMoveOrder(
                em,
                screenPosition,
                selectedMoveQuery,
                gridConfigQuery,
                mapSurfaceQuery,
                moveOrderSystem,
                orderMarkerSystem,
                tryGetClickedUnit,
                tryGetClickedCell,
                request.Frame,
                cachedSelectedMoveEntities);

            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"requestResult requestId={request.RequestId} accepted={result.CommandResult.Accepted} " +
                    $"reason={result.CommandResult.ReasonCode} emitMarker={result.EmitScreenMarker} showWorldMarkers={result.ShowWorldMarkers}");
            }

            AddCommandResult(em, commandEntity, commandResults, ToResultElement(request, result));
        }

        return _pendingMoveRequests.Count > 0;
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
