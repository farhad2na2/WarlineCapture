using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct VehicleWreckCleanupSystem : ISystem
{
    private EntityQuery _respawnQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _respawnQueueQuery = state.GetEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
        state.RequireForUpdate<VehicleWreckComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var queueEntity = RespawnQueueUtility.GetOrCreateQueue(ref state, _respawnQueueQuery);
        var queueState = SystemAPI.GetComponent<RespawnQueueComponent>(queueEntity);
        var em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        double now = SystemAPI.Time.ElapsedTime;
        double respawnDelay = Unity.Mathematics.math.max(0.01f, queueState.RespawnDelaySeconds);

        var finalize = new NativeList<Entity>(Allocator.Temp);
        foreach (var (wreck, health, entity) in SystemAPI
                 .Query<RefRW<VehicleWreckComponent>, RefRO<UnitHealth>>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
                continue;

            wreck.ValueRW.TimeRemaining -= dt;
            if (wreck.ValueRW.TimeRemaining <= 0f)
                finalize.Add(entity);
        }

        for (int i = 0; i < finalize.Length; i++)
            UnitDeathSystem.FinalizeDeath(em, queueEntity, finalize[i], now, respawnDelay);

        finalize.Dispose();
    }
}
