using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct UnitRespawnSystem : ISystem
{
    private MapSurfaceSpawnGroundingSystem _spawnGroundingSystem;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<DynamicBlockerComponent>();
        state.RequireForUpdate<GridWalkable>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var queueEntity = RespawnQueueUtility.GetOrCreateQueue(ref state);
        var buffer = SystemAPI.GetBuffer<RespawnRequest>(queueEntity);
        if (buffer.Length == 0)
            return;

        var grid = SystemAPI.GetSingleton<GridConfig>();
        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = SystemAPI.GetComponent<DynamicOccupancyComponent>(gridEntity).Occupied;
        var dynamicBlocked = SystemAPI.GetComponent<DynamicBlockerComponent>(gridEntity).Blocked;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);

        var queueState = SystemAPI.GetComponentRW<RespawnQueueComponent>(queueEntity);
        var rng = new Unity.Mathematics.Random(queueState.ValueRW.RandomState == 0 ? 1u : queueState.ValueRW.RandomState);

        double now = SystemAPI.Time.ElapsedTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            var req = buffer[i];
            if (req.ReadyTime > now)
                continue;

            buffer.RemoveAt(i);

            if (req.Prefab == Entity.Null)
                continue;

            int2 center = ResolveFactionSpawnCell(state.EntityManager, queueEntity, req.FactionId);
            int2 footprintSize = state.EntityManager.HasComponent<UnitFootprint>(req.Prefab)
                ? state.EntityManager.GetComponentData<UnitFootprint>(req.Prefab).Size
                : new int2(1, 1);
            if (!SpawnCellUtility.TryFindSpawnCellNear(ref rng, grid, walkable, dynamicBlocked, occupied, ref reserved, center, queueState.ValueRO.SpawnRadiusCells, footprintSize, out int2 cell))
            {
                Debug.LogWarning($"[Respawn] no-free-cell faction={req.FactionId} prefab={req.Prefab} center={center} radius={queueState.ValueRO.SpawnRadiusCells} footprint={footprintSize}");
                continue;
            }

            var instance = ecb.Instantiate(req.Prefab);
            ecb.SetComponent(instance, new UnitGrid { Cell = cell });
            float3 pos = GridUtils.CellToWorldCenter(grid, cell);
            bool isAirUnit = state.EntityManager.HasComponent<UnitAirMovement>(req.Prefab);
            if (!isAirUnit)
                _spawnGroundingSystem.TryGroundCellCenter(state.EntityManager, grid, cell, ref pos, out _);
            ecb.SetComponent(instance, LocalTransform.FromPosition(pos));
            ecb.SetComponent(instance, new UnitPrevWorldPos { Value = pos });
            ecb.SetComponent(instance, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
            ecb.SetComponent(instance, new Faction { Id = req.FactionId });
            ecb.SetComponent(instance, new UnitRespawnPrefab { Prefab = req.Prefab });
            ecb.SetComponent(instance, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
            ecb.SetComponent(instance, new UnitIdleWanderComponent
            {
                RandomState = rng.NextUInt(),
                RetrySeconds = 0f,
                CurrentIdleDelaySeconds = 0f
            });
            ecb.RemoveComponent<UnitPathFollow>(instance);
            ecb.RemoveComponent<UnitPathRange>(instance);
            ecb.RemoveComponent<EngageTarget>(instance);
            ecb.RemoveComponent<UnitPathRequest>(instance);
            ecb.RemoveComponent<UnitTarget>(instance);
            ecb.RemoveComponent<AutoWanderMoveTag>(instance);
        }

        queueState.ValueRW.RandomState = rng.state;

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        reserved.Dispose();
    }

    private static int2 ResolveFactionSpawnCell(EntityManager em, Entity queueEntity, byte factionId)
    {
        if (!em.HasBuffer<RespawnFactionSpawnPoint>(queueEntity))
            return default;

        DynamicBuffer<RespawnFactionSpawnPoint> points = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].FactionId == factionId)
                return points[i].SpawnCell;
        }

        return points.Length > 0 ? points[0].SpawnCell : default;
    }
}
