using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public readonly struct UnitRenderBudgetRenderSafetySystem
{
    private const int AlwaysVisibleLodMask = 0xFF;
    private const float AlwaysVisibleLodDistance = 1048576f;
    private static readonly float3 RenderBoundsMinExtents = new float3(64f, 64f, 64f);

    public int EnsureRenderSafetyRecursiveOnce(
        EntityManager em,
        EntityCommandBuffer ecb,
        NativeHashSet<Entity> taggedThisFrame,
        Entity entity,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetLodReferenceSystem lodReferenceSystem)
    {
        if (!em.Exists(entity))
            return 0;
        if (em.HasComponent<UnitRenderSafetyPatchedTag>(entity) || taggedThisFrame.Contains(entity))
            return 0;

        int patched = EnsureRenderSafetyRecursive(em, entity, childLookup, lodReferenceSystem);
        taggedThisFrame.Add(entity);
        ecb.AddComponent<UnitRenderSafetyPatchedTag>(entity);

        return patched;
    }

    private static int EnsureRenderSafetyRecursive(
        EntityManager em,
        Entity entity,
        BufferLookup<Child> childLookup,
        UnitRenderBudgetLodReferenceSystem lodReferenceSystem)
    {
        if (!em.Exists(entity))
            return 0;

        int patched = 0;
        if (em.HasComponent<Unity.Rendering.RenderBounds>(entity))
        {
            Unity.Rendering.RenderBounds bounds = em.GetComponentData<Unity.Rendering.RenderBounds>(entity);
            float3 extents = math.max(bounds.Value.Extents, RenderBoundsMinExtents);
            if (!math.all(bounds.Value.Extents == extents))
            {
                bounds.Value.Extents = extents;
                em.SetComponentData(entity, bounds);
                patched++;
            }
        }

        if (lodReferenceSystem.TryResolveMeshLod(em, entity, out MeshLODComponent meshLod))
        {
            if (meshLod.LODMask != AlwaysVisibleLodMask)
            {
                meshLod.LODMask = AlwaysVisibleLodMask;
                em.SetComponentData(entity, meshLod);
                patched++;
            }

            patched += PatchLodGroup(em, meshLod.Group, lodReferenceSystem);
            patched += PatchLodGroup(em, meshLod.ParentGroup, lodReferenceSystem);
        }

        if (!childLookup.HasBuffer(entity))
            return patched;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            patched += EnsureRenderSafetyRecursive(em, children[i].Value, childLookup, lodReferenceSystem);

        return patched;
    }

    private static int PatchLodGroup(
        EntityManager em,
        Entity group,
        UnitRenderBudgetLodReferenceSystem lodReferenceSystem)
    {
        if (!lodReferenceSystem.TryResolveMeshLodGroup(em, group, out MeshLODGroupComponent lodGroup))
            return 0;
        bool changed =
            lodGroup.ParentMask != AlwaysVisibleLodMask ||
            !math.all(lodGroup.LODDistances0 == new float4(AlwaysVisibleLodDistance)) ||
            !math.all(lodGroup.LODDistances1 == new float4(AlwaysVisibleLodDistance));
        if (!changed)
            return 0;

        lodGroup.ParentMask = AlwaysVisibleLodMask;
        lodGroup.LODDistances0 = new float4(AlwaysVisibleLodDistance);
        lodGroup.LODDistances1 = new float4(AlwaysVisibleLodDistance);
        em.SetComponentData(group, lodGroup);
        return 1;
    }
}
