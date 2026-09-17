using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    public partial struct PathPoolMaintenanceSystem : ISystem
    {
        private EntityQuery _activePaths;
        private EntityQuery _liveGridPool;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _liveGridPool = new EntityQueryBuilder(Allocator.Temp).WithAll<GridConfig>()
                .WithAllRW<PathPoolComponent>().Build(ref state);
            state.RequireForUpdate(_liveGridPool);
            _activePaths = state.GetEntityQuery(ComponentType.ReadOnly<UnitPathRange>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_activePaths.CalculateEntityCount() != 0)
                return;

            // Retired scene grids retain cleanup storage until disposal. They are
            // not active navigation pools and must not participate in this query.
            RefRW<PathPoolComponent> pool = _liveGridPool.GetSingletonRW<PathPoolComponent>();
            if (!pool.ValueRO.Cells.IsCreated || pool.ValueRO.Cells.Length == 0)
                return;

            pool.ValueRW.Cells.Clear();
        }
    }
}
