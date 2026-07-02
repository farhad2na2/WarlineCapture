using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
    public partial struct InitialUnitsBlockerChurnSystem : ISystem
    {
        private EntityQuery _blockersQuery;
        private EntityQuery _gridQuery;
        private EntityTypeHandle _entityType;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<InitialUnitsBlockerChurnConfig>();
            state.RequireForUpdate<InitialUnitsBlockerChurnComponent>();

            _blockersQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                }
            });
            _entityType = state.GetEntityTypeHandle();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _entityType.Update(ref state);

            foreach (var (cfg, churn, entity) in
                     SystemAPI.Query<RefRO<InitialUnitsBlockerChurnConfig>, RefRW<InitialUnitsBlockerChurnComponent>>().WithEntityAccess())
            {
                if (!cfg.ValueRO.Enabled)
                    continue;

                float dt = SystemAPI.Time.DeltaTime;
                churn.ValueRW.Timer += dt;

                if (churn.ValueRW.Timer < cfg.ValueRO.IntervalSeconds)
                    continue;

                churn.ValueRW.Timer = 0f;
                int n = cfg.ValueRO.AddRemovePerInterval;
                if (n <= 0)
                    continue;

                var rng = new Random(math.max(1u, churn.ValueRW.RandomState));
                churn.ValueRW.RandomState = rng.NextUInt();

                using NativeArray<ArchetypeChunk> chunks = _blockersQuery.ToArchetypeChunkArray(Allocator.Temp);
                using var existingBlockers = new NativeList<Entity>(_blockersQuery.CalculateEntityCount(), Allocator.Temp);
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(_entityType);
                    existingBlockers.AddRange(entities);
                }

                int toRemove = math.min(n, existingBlockers.Length);
                if (toRemove > 0)
                {
                    using var chosen = new NativeHashSet<int>(toRemove * 2, Allocator.Temp);
                    while (chosen.Count < toRemove)
                        chosen.Add(rng.NextInt(0, existingBlockers.Length));

                    foreach (var idx in chosen)
                        ecb.DestroyEntity(existingBlockers[idx]);
                }

                Entity blockerPrefab = churn.ValueRO.BlockerPrefab;

                for (int i = 0; i < n; i++)
                {
                    if (blockerPrefab == Entity.Null)
                        break;

                    var instance = ecb.Instantiate(blockerPrefab);
                    int2 cell = new(rng.NextInt(0, grid.Width), rng.NextInt(0, grid.Height));
                    ecb.SetComponent(instance, new UnitGrid { Cell = cell });
                    ecb.SetComponent(instance, LocalTransform.FromPosition(GridUtils.CellToWorldCenter(grid, cell)));
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
