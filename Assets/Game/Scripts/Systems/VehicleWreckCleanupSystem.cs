using Unity.Collections;
using Unity.Burst;
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

        var finalize = new NativeList<Entity>(Allocator.TempJob);
        new CollectExpiredWrecksJob
        {
            DeltaTime = dt,
            FinalizeEntities = finalize
        }.Run();

        for (int i = 0; i < finalize.Length; i++)
            UnitDeathSystem.FinalizeDeath(em, queueEntity, finalize[i], now, respawnDelay);

        finalize.Dispose();
    }

    [BurstCompile]
    private partial struct CollectExpiredWrecksJob : IJobEntity
    {
        public float DeltaTime;
        public NativeList<Entity> FinalizeEntities;

        public void Execute(Entity entity, ref VehicleWreckComponent wreck, in UnitHealth health)
        {
            if (health.Current > 0)
                return;

            wreck.TimeRemaining -= DeltaTime;
            if (wreck.TimeRemaining <= 0f)
                FinalizeEntities.Add(entity);
        }
    }
}
