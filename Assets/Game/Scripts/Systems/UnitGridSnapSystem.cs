using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitGridSnapSystem : ISystem
    {
        private EntityQuery _gridQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<UnitGrid>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new SnapUnitGridJob
            {
                Grid = grid,
                Ecb = ecb
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        [WithNone(typeof(UnitGridInitialized), typeof(UnitAirMovement))]
        private partial struct SnapUnitGridJob : IJobEntity
        {
            public GridConfig Grid;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in UnitGrid unitGrid, ref LocalTransform transform)
            {
                transform.Position = GridUtils.CellToWorldCenter(Grid, unitGrid.Cell);
                Ecb.AddComponent<UnitGridInitialized>(entity);
            }
        }
    }
}
