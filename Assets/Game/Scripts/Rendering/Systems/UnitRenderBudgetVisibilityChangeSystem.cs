using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetVisibilityChangeSystem
{
    public void CollectRenderVisibilityChanges(
        EntityManager em,
        Entity root,
        bool visible,
        BufferLookup<Child> childLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!childLookup.HasBuffer(root))
            return;

        DynamicBuffer<Child> children = childLookup[root];
        for (int i = 0; i < children.Length; i++)
            CollectRenderVisibilityChangesRecursive(em, children[i].Value, visible, childLookup, entitiesToShow, entitiesToHide, ref changed);
    }

    public void CollectRenderVisibilityChangesRecursive(
        EntityManager em,
        Entity entity,
        bool visible,
        BufferLookup<Child> childLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!em.Exists(entity))
            return;

        bool isCulled = em.HasComponent<UnitRenderBudgetCulledTag>(entity);
        bool isHidden = em.HasComponent<Disabled>(entity) || em.HasComponent<DisableRendering>(entity);
        if (visible)
        {
            if (isCulled || isHidden)
            {
                entitiesToShow.Add(entity);
                changed++;
            }
        }
        else if (!isCulled || !isHidden)
        {
            entitiesToHide.Add(entity);
            changed++;
        }

        if (!childLookup.HasBuffer(entity))
            return;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            CollectRenderVisibilityChangesRecursive(em, children[i].Value, visible, childLookup, entitiesToShow, entitiesToHide, ref changed);
    }
}
