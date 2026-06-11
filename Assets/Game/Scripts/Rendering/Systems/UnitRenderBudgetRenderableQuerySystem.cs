using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetRenderableQuerySystem
{
    public bool IsRenderableVisibleRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (IsRenderableEntity(em, entity) &&
            !em.HasComponent<Disabled>(entity) &&
            !em.HasComponent<DisableRendering>(entity) &&
            !em.HasComponent<UnitRenderBudgetCulledTag>(entity))
        {
            return true;
        }

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (IsRenderableVisibleRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    public bool HasRenderableRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (IsRenderableEntity(em, entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasRenderableRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    public bool IsSafeVisibleCharacterLod(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               em.HasComponent<UnitSafeVisibleCharacterLodTag>(entity);
    }

    public bool IsRenderableEntity(EntityManager em, Entity entity)
    {
        return em.HasComponent<RenderFilterSettings>(entity) ||
               em.HasComponent<Unity.Rendering.RenderBounds>(entity);
    }
}
