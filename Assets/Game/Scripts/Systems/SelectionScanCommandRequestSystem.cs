using Unity.Entities;
using UnityEngine;

public sealed class SelectionScanCommandRequestSystem
{
    private readonly System.Collections.Generic.List<RtsSelectionCommandIntentRequestElement> _pendingScanRequests = new();

    public bool ProcessPendingRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        EntityQuery gridConfigQuery,
        ScanIntelCommandSystem scanCommandSystem,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        _pendingScanRequests.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Scan)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _pendingScanRequests.Add(request);
        }

        for (int i = 0; i < _pendingScanRequests.Count; i++)
        {
            RtsSelectionCommandIntentRequestElement request = _pendingScanRequests[i];
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            ScanIntelCommandSystem.Result result = scanCommandSystem.TryIssueScan(
                em,
                screenPosition,
                request.RequestId,
                request.Frame,
                gridConfigQuery,
                tryGetClickedCell);

            AddCommandResult(em, commandEntity, commandResults, ToResultElement(request, result));
        }

        return _pendingScanRequests.Count > 0;
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
        ScanIntelCommandSystem.Result result)
    {
        TacticalCommandResult commandResult = result.CommandResult;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetCell = result.CenterCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.CenterWorld,
            HasCommandResult = 1,
            Accepted = commandResult.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)commandResult.ReasonCode,
            EmitScreenMarker = commandResult.Accepted ? (byte)1 : (byte)0,
            HasTargetCell = commandResult.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = result.HasWorldPosition ? (byte)1 : (byte)0,
            ShowWorldMarkers = commandResult.Accepted ? (byte)1 : (byte)0,
            RevealedCount = result.RevealedCount,
            RadiusCells = result.RadiusCells
        };
    }
}
