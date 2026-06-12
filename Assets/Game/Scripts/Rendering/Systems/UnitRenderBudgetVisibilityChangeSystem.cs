using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetVisibilityChangeSystem
{
    public void CollectRenderVisibilityChanges(
        Entity root,
        bool visible,
        BufferLookup<Child> childLookup,
        EntityStorageInfoLookup entityStorageInfoLookup,
        ComponentLookup<Disabled> disabledLookup,
        ComponentLookup<DisableRendering> disableRenderingLookup,
        ComponentLookup<UnitRenderBudgetCulledTag> culledTagLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!childLookup.HasBuffer(root))
            return;

        DynamicBuffer<Child> children = childLookup[root];
        for (int i = 0; i < children.Length; i++)
        {
            CollectRenderVisibilityChangesRecursive(
                children[i].Value,
                visible,
                childLookup,
                entityStorageInfoLookup,
                disabledLookup,
                disableRenderingLookup,
                culledTagLookup,
                entitiesToShow,
                entitiesToHide,
                ref changed);
        }
    }

    public void CollectRenderVisibilityChangesRecursive(
        Entity entity,
        bool visible,
        BufferLookup<Child> childLookup,
        EntityStorageInfoLookup entityStorageInfoLookup,
        ComponentLookup<Disabled> disabledLookup,
        ComponentLookup<DisableRendering> disableRenderingLookup,
        ComponentLookup<UnitRenderBudgetCulledTag> culledTagLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!entityStorageInfoLookup.Exists(entity))
            return;

        bool isCulled = culledTagLookup.HasComponent(entity);
        bool isHidden = disabledLookup.HasComponent(entity) || disableRenderingLookup.HasComponent(entity);
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
        {
            CollectRenderVisibilityChangesRecursive(
                children[i].Value,
                visible,
                childLookup,
                entityStorageInfoLookup,
                disabledLookup,
                disableRenderingLookup,
                culledTagLookup,
                entitiesToShow,
                entitiesToHide,
                ref changed);
        }
    }
}
