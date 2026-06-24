using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct UnitDestroyedVisualSystem : ISystem
{
    private ComponentLookup<LocalTransform> _localTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitDestroyedVisualReference>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _localTransformLookup = state.GetComponentLookup<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _localTransformLookup.Update(ref state);
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
        state.Dependency = new InitializeDestroyedVisualJob
        {
            LocalTransforms = _localTransformLookup,
            Ecb = ecb
        }.Schedule(state.Dependency);
    }

    public static void SetChildVisible(EntityManager em, Entity child, bool visible, float visibleScale = 1f)
    {
        if (!em.Exists(child) || !em.HasComponent<LocalTransform>(child))
            return;

        LocalTransform transform = em.GetComponentData<LocalTransform>(child);
        float targetScale = visible ? visibleScale : 0f;
        if (transform.Scale == targetScale)
            return;

        transform.Scale = targetScale;
        em.SetComponentData(child, transform);
    }

    [BurstCompile]
    [WithNone(typeof(UnitDestroyedVisualInitialized), typeof(VehicleWreckComponent))]
    private partial struct InitializeDestroyedVisualJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> LocalTransforms;
        public EntityCommandBuffer Ecb;

        private void Execute(Entity entity, in UnitDestroyedVisualReference visualRef)
        {
            SetChildVisible(ref LocalTransforms, visualRef.AliveVisual, true, visualRef.AliveVisibleScale);
            SetChildVisible(ref LocalTransforms, visualRef.DestroyedVisual, false, visualRef.DestroyedVisibleScale);
            Ecb.AddComponent<UnitDestroyedVisualInitialized>(entity);
        }

        private static void SetChildVisible(
            ref ComponentLookup<LocalTransform> localTransforms,
            Entity child,
            bool visible,
            float visibleScale)
        {
            if (child == Entity.Null || !localTransforms.HasComponent(child))
                return;

            LocalTransform transform = localTransforms[child];
            float targetScale = visible ? visibleScale : 0f;
            if (transform.Scale == targetScale)
                return;

            transform.Scale = targetScale;
            localTransforms[child] = transform;
        }
    }
}
