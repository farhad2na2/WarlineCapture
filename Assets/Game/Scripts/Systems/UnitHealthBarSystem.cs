using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct UnitHealthBarSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitHealth>();
        state.RequireForUpdate<HealthBarFill>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
        state.Dependency = new ExpireRecentDamageVisibilityJob
        {
            DeltaTime = deltaTime,
            Ecb = ecb
        }.Schedule(state.Dependency);

        var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);
        var recentDamageLookup = SystemAPI.GetComponentLookup<RecentDamageHealthBarVisibility>(true);
        var passengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true);
        var culledLookup = SystemAPI.GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true);

        var handle = new UpdateJob
        {
            HealthLookup = healthLookup,
            FactionLookup = factionLookup,
            RecentDamageLookup = recentDamageLookup,
            PassengerLookup = passengerLookup,
            CulledLookup = culledLookup
        }.ScheduleParallel(state.Dependency);

        state.Dependency = handle;
    }

    [BurstCompile]
    private partial struct ExpireRecentDamageVisibilityJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer Ecb;

        public void Execute(Entity entity, ref RecentDamageHealthBarVisibility recentDamage)
        {
            recentDamage.TimeRemaining -= DeltaTime;
            if (recentDamage.TimeRemaining <= 0f)
                Ecb.RemoveComponent<RecentDamageHealthBarVisibility>(entity);
        }
    }

    [BurstCompile]
    private partial struct UpdateJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<RecentDamageHealthBarVisibility> RecentDamageLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [ReadOnly] public ComponentLookup<UnitRenderBudgetCulledUnitTag> CulledLookup;

        public void Execute(ref HealthBarFill fill, ref LocalTransform transform, in Parent parent)
        {
            var unit = parent.Value;
            bool show = false;
            if (HealthLookup.HasComponent(unit) &&
                HealthLookup[unit].Current > 0 &&
                RecentDamageLookup.HasComponent(unit) &&
                RecentDamageLookup[unit].TimeRemaining > 0f &&
                !PassengerLookup.HasComponent(unit) &&
                !CulledLookup.HasComponent(unit))
            {
                show = true;
            }

            float targetScale = show ? 1f : 0f;
            if (math.abs(transform.Scale - targetScale) > 0.0001f)
                transform.Scale = targetScale;
            else if (!show)
                return;

            var h = HealthLookup[unit];
            float v = (h.Max > 0) ? ((float)h.Current / h.Max) : 0f;
            float clamped = math.clamp(v, 0f, 1f);
            if (math.abs(fill.Value - clamped) > 0.0001f)
                fill.Value = clamped;
        }
    }
}
