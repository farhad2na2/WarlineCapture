using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public readonly struct UnitRenderBudgetReadinessSystem
{
    public struct Lookups
    {
        public EntityStorageInfoLookup EntityStorageInfoLookup;
        public ComponentLookup<UnitRenderVisualReadyTag> VisualReadyLookup;

        public void Update(ref SystemState state)
        {
            EntityStorageInfoLookup.Update(ref state);
            VisualReadyLookup.Update(ref state);
        }
    }

    public bool IsVisualReadyForExclusiveDisplay(
        EntityCommandBuffer ecb,
        NativeHashSet<Entity> readyTaggedThisFrame,
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetAnimationReadinessSystem animationReadinessSystem,
        UnitRenderBudgetRenderableQuerySystem renderableQuerySystem,
        Lookups lookups,
        UnitRenderBudgetAnimationReadinessSystem.Lookups animationReadinessLookups,
        UnitRenderBudgetRenderableQuerySystem.Lookups renderableQueryLookups)
    {
        if (root == Entity.Null || !lookups.EntityStorageInfoLookup.Exists(root))
            return false;
        if (lookups.VisualReadyLookup.HasComponent(root) || readyTaggedThisFrame.Contains(root))
            return true;

        bool ready = animationReadinessSystem.IsAnimatedRenderReady(
            root,
            childLookup,
            renderableQuerySystem,
            renderableQueryLookups,
            animationReadinessLookups);
        if (ready)
        {
            readyTaggedThisFrame.Add(root);
            ecb.AddComponent<UnitRenderVisualReadyTag>(root);
        }

        return ready;
    }

    public bool IsVisualReadyForExclusiveDisplay(
        EntityManager em,
        EntityCommandBuffer ecb,
        NativeHashSet<Entity> readyTaggedThisFrame,
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetAnimationReadinessSystem animationReadinessSystem,
        UnitRenderBudgetRenderableQuerySystem renderableQuerySystem)
    {
        if (root == Entity.Null || !em.Exists(root))
            return false;
        if (em.HasComponent<UnitRenderVisualReadyTag>(root) || readyTaggedThisFrame.Contains(root))
            return true;

        bool ready = animationReadinessSystem.IsAnimatedRenderReady(em, root, childLookup, renderableQuerySystem);
        if (ready)
        {
            readyTaggedThisFrame.Add(root);
            ecb.AddComponent<UnitRenderVisualReadyTag>(root);
        }

        return ready;
    }

    public bool IsVisualReadyForExclusiveDisplay(
        EntityManager em,
        Entity root,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetAnimationReadinessSystem animationReadinessSystem,
        UnitRenderBudgetRenderableQuerySystem renderableQuerySystem)
    {
        if (root == Entity.Null || !em.Exists(root))
            return false;
        if (em.HasComponent<UnitRenderVisualReadyTag>(root))
            return true;

        return animationReadinessSystem.IsAnimatedRenderReady(em, root, childLookup, renderableQuerySystem);
    }
}
