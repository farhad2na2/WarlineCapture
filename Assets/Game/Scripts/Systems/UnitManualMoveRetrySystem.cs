using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct UnitManualMoveRetrySystem : ISystem
{
    private const double FreezeLogThresholdSeconds = 0.05d;
    private static readonly bool EnableManualMoveRetryFreezeLogs = false;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        try
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int currentFrame = Time.frameCount;

            foreach (var (cooldown, entity) in SystemAPI
                         .Query<RefRO<UnitPathRetryCooldown>>()
                         .WithEntityAccess())
            {
                if (cooldown.ValueRO.ResumeFrame <= currentFrame)
                    ecb.RemoveComponent<UnitPathRetryCooldown>(entity);
            }

            using var staleGroupMembers = SystemAPI
                .QueryBuilder()
                .WithAll<ManualMoveGroupMemberTag>()
                .WithNone<ManualMoveOrderTag>()
                .Build()
                .ToEntityArray(Allocator.Temp);

            foreach (var entity in staleGroupMembers)
            {
                ecb.RemoveComponent<ManualMoveGroupMemberTag>(entity);
            }

            foreach (var (target, entity) in SystemAPI
                         .Query<RefRO<UnitTarget>>()
                         .WithAll<ManualMoveOrderTag>()
                         .WithNone<UnitPathRetryCooldown>()
                         .WithNone<UnitPathRequest>()
                         .WithNone<UnitPathFollow>()
                         .WithNone<EngageTarget>()
                         .WithNone<UnitAirMovement>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new UnitPathRequest { Goal = target.ValueRO.Cell });
            }

            foreach (var (longMove, entity) in SystemAPI
                         .Query<RefRO<UnitLongDistanceMove>>()
                         .WithNone<UnitPathRetryCooldown>()
                         .WithNone<UnitPathRequest>()
                         .WithNone<UnitPathFollow>()
                         .WithNone<EngageTarget>()
                         .WithNone<UnitAirMovement>()
                         .WithEntityAccess())
            {
                if (state.EntityManager.HasComponent<UnitTarget>(entity))
                    ecb.SetComponent(entity, new UnitTarget { Cell = longMove.ValueRO.FinalGoal });
                else
                    ecb.AddComponent(entity, new UnitTarget { Cell = longMove.ValueRO.FinalGoal });

                if (!state.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
                    ecb.AddComponent<ManualMoveOrderTag>(entity);

                ecb.AddComponent(entity, new UnitPathRequest { Goal = longMove.ValueRO.FinalGoal });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (EnableManualMoveRetryFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
                Debug.Log($"[FreezeDetect:ECS] UnitManualMoveRetrySystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms");
        }
    }
}
