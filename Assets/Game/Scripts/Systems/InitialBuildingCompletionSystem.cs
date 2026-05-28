using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly struct InitialBuildingCompletionSystem
{
    public bool Process(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        GridConfig grid,
        int initialBaseCoreRequestEntryIndex)
    {
        if (!new InitialBuildingBoundarySystem().TryGetRuntimeSpawnRequests(
                em,
                boundaryEntity,
                out DynamicBuffer<BuildingRuntimeSpawnRequest> requests))
            return false;

        bool sawRequest = false;
        bool hasPending = false;
        for (int i = requests.Length - 1; i >= 0; i--)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.PlanEntity != configEntity)
                continue;

            sawRequest = true;
            if (request.Status == BuildingRuntimeSpawnRequest.Pending)
            {
                hasPending = true;
                continue;
            }

            if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
            {
                if (request.FactionId == 0 &&
                    request.EntryIndex == initialBaseCoreRequestEntryIndex &&
                    !Chapter01M01PlayableRuntime.IsActiveMission())
                {
                    Vector3 coreFocus = GetFootprintCenterWorld(
                        new Vector2Int(request.ActualOrigin.x, request.ActualOrigin.y),
                        new Vector2Int(request.ActualFootprint.x, request.ActualFootprint.y),
                        grid);
                    InitialUnitsRuntimeState.InitialCameraFocusWorld = coreFocus;
                    InitialUnitsRuntimeState.InitialCameraFocusRequested = true;
                }
            }

            requests.RemoveAt(i);
        }

        if (hasPending)
            return false;

        return sawRequest;
    }

    private static Vector3 GetFootprintCenterWorld(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            grid.Origin.y,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }
}
