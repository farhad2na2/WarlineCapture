using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    public partial struct OperationsReconWaveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationsReconWaveComponent>();
            state.RequireForUpdate<UnitMoveOrderQueueComponent>();
            state.RequireForUpdate<MapSurfaceComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive == 0) return;
            var surface = SystemAPI.GetSingleton<MapSurfaceComponent>();
            Entity queue = SystemAPI.GetSingletonEntity<UnitMoveOrderQueueComponent>();
            var moves = SystemAPI.GetBuffer<UnitMoveOrderRequestElement>(queue);
            var sequence = SystemAPI.GetComponentRW<UnitMoveOrderQueueComponent>(queue);
            var commands = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (missionRef, waveRef, sites, evidence, session) in
                     SystemAPI.Query<RefRO<OperationsReconMissionComponent>, RefRW<OperationsReconWaveComponent>,
                         DynamicBuffer<OperationsReconSiteElement>, RefRO<OperationsReconEvidenceComponent>>().WithEntityAccess())
            {
                var mission = missionRef.ValueRO;
                if (mission.Phase != OperationsReconPhase.Playing) continue;
                ref var wave = ref waveRef.ValueRW;
                if (mission.FirstScanWaveTriggered != 0 && wave.WaveAAnnounced == 0)
                { wave.WaveAAnnounced = 1; wave.WaveAReleaseAt = mission.ElapsedSeconds + wave.WarningSeconds; }
                if (mission.EvidenceWaveTriggered != 0 && wave.WaveBAnnounced == 0)
                { wave.WaveBAnnounced = 1; wave.WaveBReleaseAt = mission.ElapsedSeconds + wave.EvidenceWarningSeconds; }
                float3 targetA = mission.FirstScanPosition;
                foreach (var (reserveRef, position, unit) in SystemAPI.Query<RefRW<OperationsReconReserveComponent>, RefRO<LocalTransform>>()
                             .WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
                {
                    var reserve = reserveRef.ValueRO;
                    if (reserve.Session != session || reserve.Released != 0) continue;
                    bool ready = reserve.Wave == 1
                        ? wave.WaveAAnnounced != 0 && mission.ElapsedSeconds >= wave.WaveAReleaseAt
                        : wave.WaveBAnnounced != 0 && mission.ElapsedSeconds >= wave.WaveBReleaseAt;
                    if (!ready) continue;
                    // Do not materialize reinforcements on top of a player's scouting group.
                    bool occupied = false;
                    foreach (var (friendly, faction, health) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Faction>, RefRO<UnitHealth>>())
                        if (faction.ValueRO.Id == 1 && health.ValueRO.Current > 0 &&
                            math.distancesq(friendly.ValueRO.Position.xz, position.ValueRO.Position.xz) < 3600f) occupied = true;
                    if (occupied) continue;
                    reserveRef.ValueRW.Released = 1;
                    commands.RemoveComponent<Disabled>(unit);
                    float3 target = reserve.Wave == 1 ? targetA : evidence.ValueRO.Position;
                    int2 goal = (int2)math.floor((target.xz - surface.GridOrigin.xz) / surface.CellSize);
                    moves.Add(new UnitMoveOrderRequestElement
                    {
                        RequestId = ++sequence.ValueRW.LastRequestId, Entity = unit, Goal = goal,
                        Kind = UnitMoveOrderRequestKind.TargetPathOnly, IssueGroundPathNow = 1
                    });
                }
            }
            commands.Playback(state.EntityManager);
            commands.Dispose();
        }
    }
}
