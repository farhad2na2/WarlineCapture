using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct VehicleWreckCleanupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleWreckState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var queueEntity = RespawnQueueUtils.GetOrCreateQueue(ref state);
        var queueState = SystemAPI.GetComponent<RespawnQueueState>(queueEntity);
        var em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        double now = SystemAPI.Time.ElapsedTime;
        double respawnDelay = Unity.Mathematics.math.max(0.01f, queueState.RespawnDelaySeconds);

        var finalize = new NativeList<Entity>(Allocator.Temp);
        foreach (var (wreck, health, entity) in SystemAPI
                 .Query<RefRW<VehicleWreckState>, RefRO<UnitHealth>>()
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
