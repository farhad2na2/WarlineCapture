using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

// Clears invalid EngageTarget BEFORE UnitEngagementSystem so units can re-acquire immediately,
// and restores UnitPathRequest so they don't get stuck idling after a kill.

namespace Game.Runtime
{
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
                FogOwned = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                FogSight = SystemAPI.GetComponentLookup<SkirmishContactSightComponent>(true),
                FogKnowledge = SystemAPI.GetBufferLookup<SkirmishContactKnowledgeElement>(true),
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
            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwned;
            [ReadOnly] public ComponentLookup<SkirmishContactSightComponent> FogSight;
            [ReadOnly] public BufferLookup<SkirmishContactKnowledgeElement> FogKnowledge;
            [ReadOnly] public ComponentLookup<LocalTransform> TargetTransformLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> TargetHealthLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
            public ComponentLookup<UnitAirComponent> AirStateLookup;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in EngageTarget engage)
            {
                Entity target = engage.Target;
                var facts = new EngageValidationFacts { TargetNull=target==Entity.Null };
                if (!facts.TargetNull)
                {
                    facts.TargetHasTransform=TargetTransformLookup.HasComponent(target);
                    if (facts.TargetHasTransform)
                    {
                        facts.TargetHasHealth=TargetHealthLookup.HasComponent(target);
                        if (facts.TargetHasHealth) facts.TargetHealth=TargetHealthLookup[target].Current;
                    }
                }
                if (!EngageTargetLifecycleKernel.IsInvalid(facts) && FogOwned.HasComponent(entity) && FogOwned.HasComponent(target))
                {
                    var observer=FogOwned[entity];facts.PairedFog=true;
                    facts.SameFogSession=observer.SessionId.Equals(FogOwned[target].SessionId);
                    if (facts.SameFogSession) facts.FogVisible=SkirmishFogConsumerUtility.IsVisible(FogOwned,FogSight,FogKnowledge,target,observer.FactionId);
                }
                if (!EngageTargetLifecycleKernel.IsInvalid(facts)) return;

                Ecb.RemoveComponent<EngageTarget>(entity);

                bool hasAir=AirStateLookup.HasComponent(entity);
                bool hasPath=!hasAir&&PathRequestLookup.HasComponent(entity);
                bool hasTarget=!hasAir&&!hasPath&&TargetLookup.HasComponent(entity);
                var effects=EngageTargetLifecycleKernel.PlanInvalidation(hasAir,hasPath,hasTarget);
                if (effects.ReturningHome)
                {
                    UnitAirComponent airState = AirStateLookup[entity];
                    airState.ReturningHome = 1;
                    AirStateLookup[entity] = airState;
                }
                else if (effects.RestorePathRequest)
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = TargetLookup[entity].Cell });
            }
        }
    }
}
