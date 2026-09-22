using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>Finite authored patrols issue normal path orders and yield to shared combat.</summary>
    [BurstCompile]
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    public partial struct OperationsReconPatrolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationsReconPatrolComponent>();
            state.RequireForUpdate<UnitMoveOrderQueueComponent>();
            state.RequireForUpdate<MapSurfaceComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive == 0) return;
            var missions = SystemAPI.GetComponentLookup<OperationsReconMissionComponent>(true);
            var routes = SystemAPI.GetBufferLookup<OperationsReconPatrolWaypoint>(true);
            var targets = SystemAPI.GetComponentLookup<EngageTarget>(true);
            var surface = SystemAPI.GetSingleton<MapSurfaceComponent>();
            if (surface.CellSize <= 0) return;
            Entity queue = SystemAPI.GetSingletonEntity<UnitMoveOrderQueueComponent>();
            var moves = SystemAPI.GetBuffer<UnitMoveOrderRequestElement>(queue);
            var sequence = SystemAPI.GetComponentRW<UnitMoveOrderQueueComponent>(queue);
            foreach (var (patrolRef, position, health, unit) in SystemAPI.Query<RefRW<OperationsReconPatrolComponent>, RefRO<LocalTransform>, RefRO<UnitHealth>>().WithEntityAccess())
            {
                ref var patrol = ref patrolRef.ValueRW;
                if (health.ValueRO.Current <= 0 || !missions.HasComponent(patrol.Session) || !routes.HasBuffer(patrol.Session)) continue;
                var mission = missions[patrol.Session];
                var route = routes[patrol.Session];
                if (mission.Phase != OperationsReconPhase.Playing || route.Length < 2 || mission.ElapsedSeconds < patrol.NextOrderAt) continue;
                patrol.NextOrderAt = mission.ElapsedSeconds + 2f;
                if (targets.HasComponent(unit) && targets[unit].Target != Entity.Null) continue;
                patrol.Waypoint = math.clamp(patrol.Waypoint, 0, route.Length - 1);
                float3 destination = route[patrol.Waypoint].Position + patrol.Offset;
                bool arrived = math.distancesq(position.ValueRO.Position.xz, destination.xz) <= 9f;
                if (arrived)
                {
                    patrol.Waypoint = (patrol.Waypoint + 1) % route.Length;
                    destination = route[patrol.Waypoint].Position + patrol.Offset;
                }
                // Reissue only after arriving, or when combat/path completion removed the
                // movement goal. Repeated frames must not continually restart a valid path.
                if (!arrived && patrol.Issued != 0 && SystemAPI.HasComponent<UnitTarget>(unit)) continue;
                moves.Add(new UnitMoveOrderRequestElement
                {
                    RequestId = ++sequence.ValueRW.LastRequestId, Entity = unit,
                    Goal = (int2)math.floor((destination.xz - surface.GridOrigin.xz) / surface.CellSize),
                    Kind = UnitMoveOrderRequestKind.TargetPathOnly, IssueGroundPathNow = 1
                });
                patrol.Issued = 1;
            }
        }
    }
}
