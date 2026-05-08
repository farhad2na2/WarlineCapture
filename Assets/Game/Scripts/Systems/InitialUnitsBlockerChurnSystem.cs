using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
public partial struct InitialUnitsBlockerChurnSystem : ISystem
{
    private EntityQuery _blockersQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<InitialUnitsBlockerChurnConfig>();
        state.RequireForUpdate<InitialUnitsBlockerChurnState>();

        _blockersQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<UnitGrid>(),
            }
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (cfg, churn, entity) in
                 SystemAPI.Query<RefRO<InitialUnitsBlockerChurnConfig>, RefRW<InitialUnitsBlockerChurnState>>().WithEntityAccess())
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

            using var existingBlockers = _blockersQuery.ToEntityArray(Allocator.Temp);
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
