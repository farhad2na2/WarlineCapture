using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// Clears invalid EngageTarget BEFORE UnitEngagementSystem so units can re-acquire immediately,
// and restores UnitPathRequest so they don't get stuck idling after a kill.
[UpdateAfter(typeof(UnitHealthBarSystem))]
[UpdateBefore(typeof(UnitEngagementSystem))]
public partial struct EngageTargetValidateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EngageTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();

        var targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var targetHealthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
        var targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
        var airStateLookup = SystemAPI.GetComponentLookup<UnitAirComponent>();

        // Apply immediately this frame (no ECB system) so UnitEngagementSystem can re-acquire right away.
        // Use an ECB to avoid structural changes while iterating.
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (engage, entity) in SystemAPI.Query<RefRO<EngageTarget>>().WithEntityAccess())
        {
            var t = engage.ValueRO.Target;
            bool invalid = false;
            if (t == Entity.Null)
                invalid = true;
            else if (!targetTransformLookup.HasComponent(t))
                invalid = true;
            else if (targetHealthLookup.HasComponent(t) && targetHealthLookup[t].Current <= 0)
                invalid = true;

            if (!invalid)
                continue;

            ecb.RemoveComponent<EngageTarget>(entity);

            if (airStateLookup.HasComponent(entity))
            {
                UnitAirComponent airState = airStateLookup[entity];
                airState.ReturningHome = 1;
                airStateLookup[entity] = airState;
            }
            else if (!pathRequestLookup.HasComponent(entity) && targetLookup.HasComponent(entity))
                ecb.AddComponent(entity, new UnitPathRequest { Goal = targetLookup[entity].Cell });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
