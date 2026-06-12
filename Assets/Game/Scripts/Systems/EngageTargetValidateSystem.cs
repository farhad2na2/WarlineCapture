using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// Clears invalid EngageTarget BEFORE UnitEngagementSystem so units can re-acquire immediately,
// and restores UnitPathRequest so they don't get stuck idling after a kill.
[BurstCompile]
[UpdateAfter(typeof(UnitHealthBarSystem))]
[UpdateBefore(typeof(UnitEngagementSystem))]
public partial struct EngageTargetValidateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EngageTarget>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var targetHealthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
        var targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
        var airStateLookup = SystemAPI.GetComponentLookup<UnitAirComponent>();

        // Apply immediately this frame (no ECB system) so UnitEngagementSystem can re-acquire right away.
        // Use an ECB to avoid structural changes while iterating.
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        state.Dependency = new ValidateEngageTargetJob
        {
            TargetTransformLookup = targetTransformLookup,
            TargetHealthLookup = targetHealthLookup,
            PathRequestLookup = pathRequestLookup,
            TargetLookup = targetLookup,
            AirStateLookup = airStateLookup,
            Ecb = ecb
        }.Schedule(state.Dependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private partial struct ValidateEngageTargetJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TargetTransformLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> TargetHealthLookup;
        [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
        [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
        public ComponentLookup<UnitAirComponent> AirStateLookup;
        public EntityCommandBuffer Ecb;

        private void Execute(Entity entity, in EngageTarget engage)
        {
            Entity target = engage.Target;
            bool invalid = false;
            if (target == Entity.Null)
                invalid = true;
            else if (!TargetTransformLookup.HasComponent(target))
                invalid = true;
            else if (TargetHealthLookup.HasComponent(target) && TargetHealthLookup[target].Current <= 0)
                invalid = true;

            if (!invalid)
                return;

            Ecb.RemoveComponent<EngageTarget>(entity);

            if (AirStateLookup.HasComponent(entity))
            {
                UnitAirComponent airState = AirStateLookup[entity];
                airState.ReturningHome = 1;
                AirStateLookup[entity] = airState;
            }
            else if (!PathRequestLookup.HasComponent(entity) && TargetLookup.HasComponent(entity))
                Ecb.AddComponent(entity, new UnitPathRequest { Goal = TargetLookup[entity].Cell });
        }
    }
}
