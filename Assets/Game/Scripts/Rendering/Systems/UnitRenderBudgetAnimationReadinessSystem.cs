using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetAnimationReadiness
{
    public struct Lookups
    {
        public ComponentLookup<MeshLODComponent> MeshLodLookup;
        public ComponentLookup<MaterialAlphaCompleteTag> MaterialAlphaCompleteLookup;
        public byte HasGpuAnimationMaterialLookups;

        public void Update(ref SystemState state)
        {
            MeshLodLookup.Update(ref state);
            MaterialAlphaCompleteLookup.Update(ref state);
        }
    }

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
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableState renderableQuerySystem,
        UnitRenderBudgetRenderableState.Lookups renderableQueryLookups,
        Lookups lookups)
    {
        if (root == Entity.Null || !renderableQueryLookups.EntityStorageInfoLookup.Exists(root))
            return false;

        bool hasRenderable = false;
        bool waitingForGpuAnimationMaterial = false;
        CheckVisualReadinessRecursive(root, childLookup, renderableQuerySystem, renderableQueryLookups, lookups, ref hasRenderable, ref waitingForGpuAnimationMaterial);
        return hasRenderable && !waitingForGpuAnimationMaterial;
    }

    public bool IsAnimatedRenderReady(
        EntityManager em,
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableState renderableQuerySystem)
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
        Entity entity,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableState renderableQuerySystem,
        UnitRenderBudgetRenderableState.Lookups renderableQueryLookups,
        Lookups lookups,
        ref bool hasRenderable,
        ref bool waitingForGpuAnimationMaterial)
    {
        if (entity == Entity.Null || !renderableQueryLookups.EntityStorageInfoLookup.Exists(entity))
            return;

        if (renderableQuerySystem.IsRenderableEntity(entity, renderableQueryLookups))
            hasRenderable = true;

        if (lookups.HasGpuAnimationMaterialLookups != 0 &&
            lookups.MeshLodLookup.HasComponent(entity) &&
            !lookups.MaterialAlphaCompleteLookup.HasComponent(entity))
        {
            waitingForGpuAnimationMaterial = true;
        }

        if (!childLookup.HasBuffer(entity))
            return;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            CheckVisualReadinessRecursive(
                children[i].Value,
                childLookup,
                renderableQuerySystem,
                renderableQueryLookups,
                lookups,
                ref hasRenderable,
                ref waitingForGpuAnimationMaterial);
        }
    }

    private static void CheckVisualReadinessRecursive(
        EntityManager em,
        Entity entity,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetRenderableState renderableQuerySystem,
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
