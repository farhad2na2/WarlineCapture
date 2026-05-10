using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

[UpdateAfter(typeof(UnitRenderBudgetSystem))]
public partial struct M01LegacyEcsRenderingSuppressionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MaterialMeshInfo>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Chapter01M01PlayableRuntime.IsActiveMission())
            return;

        EntityManager em = state.EntityManager;
        EntityQuery query = SystemAPI.QueryBuilder()
            .WithAll<MaterialMeshInfo>()
            .WithNone<DisableRendering, MissionRuntimeEcsVisualTag>()
            .Build();
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.Exists(entity) && !em.HasComponent<DisableRendering>(entity))
                em.AddComponent<DisableRendering>(entity);
        }
    }
}
