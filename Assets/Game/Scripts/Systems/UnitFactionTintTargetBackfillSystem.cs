using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(FactionVisualSystem))]
public partial struct UnitFactionTintTargetBackfillSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var targets = new NativeList<Entity>(Allocator.Temp);

        foreach (var (parent, entity) in SystemAPI
                 .Query<RefRO<Parent>>()
                 .WithAll<MaterialMeshInfo>()
                 .WithNone<FactionTintTarget>()
                 .WithEntityAccess())
        {
            if (IsUnitRenderable(em, entity, parent.ValueRO.Value))
                targets.Add(entity);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Entity target = targets[i];
            if (!em.Exists(target) || em.HasComponent<FactionTintTarget>(target))
                continue;

            em.AddComponent<FactionTintTarget>(target);
            em.AddComponentData(target, new FactionTintColor
            {
                Value = new float4(1f, 1f, 1f, 1f)
            });
            em.AddComponentData(target, new FactionSnivelerBaseColor
            {
                Value = new float4(1f, 1f, 1f, 1f)
            });
        }

        targets.Dispose();
    }

    private static bool IsUnitRenderable(EntityManager em, Entity renderEntity, Entity parent)
    {
        Entity current = renderEntity;
        for (int i = 0; i < 64; i++)
        {
            if (em.HasComponent<SelectionMarkerTag>(current) ||
                em.HasComponent<HealthBarFill>(current))
            {
                return false;
            }

            if (em.HasComponent<UnitGrid>(current) &&
                em.HasComponent<UnitSourcePrefabKey>(current))
            {
                return true;
            }

            if (i == 0)
            {
                current = parent;
                continue;
            }

            if (!em.HasComponent<Parent>(current))
                break;

            current = em.GetComponentData<Parent>(current).Value;
        }

        return false;
    }
}
