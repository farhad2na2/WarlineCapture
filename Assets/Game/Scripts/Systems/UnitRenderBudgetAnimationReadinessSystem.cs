using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetAnimationReadinessSystem
{
    public bool HasAnimationIndexRecursive(
        Entity entity,
        ComponentLookup<MaterialAnimationIndex> animationIndexLookup,
        BufferLookup<Child> childLookup)
    {
        if (entity == Entity.Null)
            return false;

        if (animationIndexLookup.HasComponent(entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasAnimationIndexRecursive(children[i].Value, animationIndexLookup, childLookup))
                return true;
        }

        return false;
    }

    public bool IsAnimatedRenderReady(
        EntityManager em,
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableQuerySystem renderableQuerySystem)
    {
        if (root == Entity.Null || !em.Exists(root))
            return false;

        bool hasRenderable = false;
        bool waitingForGpuAnimationMaterial = false;
        CheckVisualReadinessRecursive(em, root, childLookup, renderableQuerySystem, ref hasRenderable, ref waitingForGpuAnimationMaterial);
        return hasRenderable && !waitingForGpuAnimationMaterial;
    }

    public bool HasMaterialAlphaCompleteRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (em.HasComponent<MaterialAlphaCompleteTag>(entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasMaterialAlphaCompleteRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    private static void CheckVisualReadinessRecursive(
        EntityManager em,
        Entity entity,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableQuerySystem renderableQuerySystem,
        ref bool hasRenderable,
        ref bool waitingForGpuAnimationMaterial)
    {
        if (!em.Exists(entity))
            return;

        if (renderableQuerySystem.IsRenderableEntity(em, entity))
            hasRenderable = true;

        if (em.HasComponent<MeshLODComponent>(entity) &&
            !em.HasComponent<MaterialAlphaCompleteTag>(entity))
        {
            waitingForGpuAnimationMaterial = true;
        }

        if (!childLookup.HasBuffer(entity))
            return;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            CheckVisualReadinessRecursive(em, children[i].Value, childLookup, renderableQuerySystem, ref hasRenderable, ref waitingForGpuAnimationMaterial);
    }
}
