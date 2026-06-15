using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathResultApply
{
    public void Apply(
        ref SystemState state,
        Entity gridEntity,
        ref PathPoolComponent pool,
        NativeArray<Entity> entities,
        NativeArray<UnitPathRequest> requests,
        NativeArray<int2> assignedGoals,
        NativeArray<byte> segmented,
        NativeArray<byte> manualMoves,
        MapSurfacePathfindingSnapshot.Context surfaceContext,
        NativeStream stream,
        NativeArray<byte> status,
        NativeArray<int> failureCodes,
        NativeArray<int> expansionCounts,
        out int completedCount,
        out int completedSegmentCount,
        out int manualCompletedCount,
        out int retriedCount,
        out int retriedSegmentCount,
        out int manualRetriedCount,
        out int abandonedCount)
    {
        completedCount = 0;
        completedSegmentCount = 0;
        manualCompletedCount = 0;
        retriedCount = 0;
        retriedSegmentCount = 0;
        manualRetriedCount = 0;
        abandonedCount = 0;
        var em = state.EntityManager;
        var reader = stream.AsReader();
        var follow = new UnitPathFollow { PathIndex = 0 };
        var retry = new UnitPathRetry();
        var surfaceMetadata = new UnitPathSurfaceMetadata();
        int manualTraceCount = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            bool entityHasMatchingRequest =
                em.Exists(entity) &&
                em.HasComponent<UnitPathRequest>(entity) &&
                em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(requests[i].Goal);

            int count = reader.BeginForEachIndex(i);
            int start = pool.Cells.Length;
            bool writePathSurfaceMetadata = entityHasMatchingRequest && status[i] == 1 && count > 0;
            DynamicBuffer<UnitPathSurfaceNode> surfaceBuffer = default;
            UnitSurfaceComponent currentSurface = default;
            if (writePathSurfaceMetadata)
            {
                surfaceBuffer = surfaceMetadata.PrepareBuffer(em, entity);
                if (em.HasComponent<UnitSurfaceComponent>(entity))
                    currentSurface = em.GetComponentData<UnitSurfaceComponent>(entity);
            }

            if (entityHasMatchingRequest)
            {
                for (int j = 0; j < count; j++)
                {
                    int2 pathCell = reader.Read<int2>();
                    pool.Cells.Add(pathCell);
                    if (writePathSurfaceMetadata)
                        surfaceMetadata.Append(surfaceBuffer, surfaceContext.Surface, surfaceContext.HasSurfaceData, pathCell, currentSurface);
                }
            }
            else
            {
                for (int j = 0; j < count; j++)
                    reader.Read<int2>();
            }
            reader.EndForEachIndex();

            if (manualMoves[i] != 0 && manualTraceCount < 12)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"pathResult frame={UnityEngine.Time.frameCount} index={i} entity={DescribePathEntity(em, entity)} " +
                    $"matchingRequest={entityHasMatchingRequest} status={status[i]} pathCount={count} pathStart={start} " +
                    $"requestGoal={requests[i].Goal} assignedGoal={assignedGoals[i]} segmented={segmented[i]} " +
                    $"failure={DescribeFailure(failureCodes[i])} expansions={expansionCounts[i]}");
                manualTraceCount++;
            }

            if (!entityHasMatchingRequest)
                continue;

            if (status[i] == 1 && count > 0)
            {
                completedCount++;
                if (segmented[i] != 0)
                    completedSegmentCount++;
                if (manualMoves[i] != 0)
                    manualCompletedCount++;
                if (em.HasComponent<UnitPathRetryCooldown>(entity))
                    em.RemoveComponent<UnitPathRetryCooldown>(entity);

                if (em.HasComponent<UnitTarget>(entity))
                    em.SetComponentData(entity, new UnitTarget { Cell = assignedGoals[i] });
                else
                    em.AddComponentData(entity, new UnitTarget { Cell = assignedGoals[i] });

                if (em.HasComponent<UnitPathFollow>(entity))
                    em.SetComponentData(entity, follow);
                else
                    em.AddComponentData(entity, follow);

                var range = new UnitPathRange { Start = start, Length = count };
                if (em.HasComponent<UnitPathRange>(entity))
                    em.SetComponentData(entity, range);
                else
                    em.AddComponentData(entity, range);

                if (segmented[i] != 0)
                {
                    if (em.HasComponent<UnitLongDistanceMove>(entity))
                        em.SetComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });
                    else
                        em.AddComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });
                }
                else if (em.HasComponent<UnitLongDistanceMove>(entity))
                {
                    em.RemoveComponent<UnitLongDistanceMove>(entity);
                }
            }
            else
            {
                if (em.HasComponent<UnitPathFollow>(entity))
                    em.RemoveComponent<UnitPathFollow>(entity);
                if (em.HasComponent<UnitPathRange>(entity))
                    em.RemoveComponent<UnitPathRange>(entity);
                surfaceMetadata.ClearIfPresent(em, entity);
                if (em.HasComponent<AutoWanderMoveTag>(entity))
                    em.RemoveComponent<AutoWanderMoveTag>(entity);

                if (retry.ShouldRetryManualMove(em, entity, segmented[i]))
                {
                    retry.ApplyRetry(em, entity, requests[i], segmented[i], manualMoves[i], ref retriedCount, ref retriedSegmentCount, ref manualRetriedCount);
                }
                else
                {
                    retry.ApplyAbandon(em, entity, ref abandonedCount);
                }
            }

            if (em.HasComponent<UnitPathRequest>(entity))
                em.RemoveComponent<UnitPathRequest>(entity);
        }
    }

    private static string DescribePathEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        string grid = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
        string target = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
        string pathRequest = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
        bool pathFollow = em.HasComponent<UnitPathFollow>(entity);
        bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
        bool retry = em.HasComponent<UnitPathRetryCooldown>(entity);
        bool longMove = em.HasComponent<UnitLongDistanceMove>(entity);
        return $"{entity}/{source}/grid={grid}/target={target}/pathRequest={pathRequest}/pathFollow={pathFollow}/manual={manual}/retry={retry}/longMove={longMove}";
    }

    private static string DescribeFailure(int code)
    {
        return code switch
        {
            0 => "None",
            1 => "StartOutOfBounds",
            2 => "StartGridNotWalkable",
            3 => "StartSurfaceBlocked",
            4 => "GoalOutOfBounds",
            5 => "GoalGridNotWalkable",
            6 => "GoalSurfaceBlocked",
            7 => "GoalPlacementBlocked",
            8 => "GoalSurfaceFootprintBlocked",
            9 => "ExpansionLimit",
            10 => "NoPath",
            11 => "PathReconstructionFailed",
            12 => "ProgressFallbackFailed",
            _ => $"Unknown({code})"
        };
    }
}
