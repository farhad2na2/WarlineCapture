using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathResultApplySystem
{
    public void Apply(
        ref SystemState state,
        Entity gridEntity,
        ref PathPoolData pool,
        NativeArray<Entity> entities,
        NativeArray<UnitPathRequest> requests,
        NativeArray<int2> assignedGoals,
        NativeArray<byte> segmented,
        NativeArray<byte> manualMoves,
        NativeStream stream,
        NativeArray<byte> status,
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
        var retry = new UnitPathRetrySystem();

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            bool entityHasMatchingRequest =
                em.Exists(entity) &&
                em.HasComponent<UnitPathRequest>(entity) &&
                em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(requests[i].Goal);

            int count = reader.BeginForEachIndex(i);
            int start = pool.Cells.Length;
            if (entityHasMatchingRequest)
            {
                for (int j = 0; j < count; j++)
                    pool.Cells.Add(reader.Read<int2>());
            }
            else
            {
                for (int j = 0; j < count; j++)
                    reader.Read<int2>();
            }
            reader.EndForEachIndex();

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
}
