using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct UnitDestroyedVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitDestroyedVisualReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var initialized = new NativeList<Entity>(Allocator.Temp);

        foreach (var (visualRef, entity) in SystemAPI
                 .Query<RefRO<UnitDestroyedVisualReference>>()
                 .WithNone<UnitDestroyedVisualInitialized, VehicleWreckComponent>()
                 .WithEntityAccess())
        {
            SetChildVisible(em, visualRef.ValueRO.AliveVisual, true, visualRef.ValueRO.AliveVisibleScale);
            SetChildVisible(em, visualRef.ValueRO.DestroyedVisual, false, visualRef.ValueRO.DestroyedVisibleScale);
            initialized.Add(entity);
        }

        for (int i = 0; i < initialized.Length; i++)
        {
            Entity entity = initialized[i];
            if (em.Exists(entity) && !em.HasComponent<UnitDestroyedVisualInitialized>(entity))
                em.AddComponent<UnitDestroyedVisualInitialized>(entity);
        }

        initialized.Dispose();
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
}
