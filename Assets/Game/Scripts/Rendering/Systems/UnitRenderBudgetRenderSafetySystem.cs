using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetRenderSafety
    {
        private const int AlwaysVisibleLodMask = 0xFF;
        private const float AlwaysVisibleLodDistance = 1048576f;
        private static readonly float3 RenderBoundsMinExtents = new float3(64f, 64f, 64f);

        public struct Lookups
        {
            public EntityStorageInfoLookup EntityStorageInfoLookup;
            public ComponentLookup<UnitRenderSafetyPatchedTag> SafetyPatchedLookup;
            public ComponentLookup<RenderBounds> RenderBoundsLookup;
            public ComponentLookup<MeshLODComponent> MeshLodLookup;
            public ComponentLookup<MeshLODGroupComponent> MeshLodGroupLookup;

            public void Update(ref SystemState state)
            {
                EntityStorageInfoLookup.Update(ref state);
                SafetyPatchedLookup.Update(ref state);
                RenderBoundsLookup.Update(ref state);
                MeshLodLookup.Update(ref state);
                MeshLodGroupLookup.Update(ref state);
            }
        }

        public int EnsureRenderSafetyRecursiveOnce(
            EntityCommandBuffer ecb,
            NativeHashSet<Entity> taggedThisFrame,
            Entity entity,
            BufferLookup<Child> childLookup,
            Lookups lookups)
        {
            if (!lookups.EntityStorageInfoLookup.Exists(entity))
                return 0;
            if (lookups.SafetyPatchedLookup.HasComponent(entity) || taggedThisFrame.Contains(entity))
                return 0;

            int patched = EnsureRenderSafetyRecursive(ecb, entity, childLookup, lookups);
            taggedThisFrame.Add(entity);
            ecb.AddComponent<UnitRenderSafetyPatchedTag>(entity);

            return patched;
        }

        public int EnsureRenderSafetyRecursiveOnce(
            EntityManager em,
            EntityCommandBuffer ecb,
            NativeHashSet<Entity> taggedThisFrame,
            Entity entity,
            BufferLookup<Child> childLookup,
            UnitRenderBudgetLodReferences lodReferenceSystem)
        {
            if (!em.Exists(entity))
                return 0;
            if (em.HasComponent<UnitRenderSafetyPatchedTag>(entity) || taggedThisFrame.Contains(entity))
                return 0;

            int patched = EnsureRenderSafetyRecursive(em, ecb, entity, childLookup, lodReferenceSystem);
            taggedThisFrame.Add(entity);
            ecb.AddComponent<UnitRenderSafetyPatchedTag>(entity);

            return patched;
        }

        private static int EnsureRenderSafetyRecursive(
            EntityCommandBuffer ecb,
            Entity entity,
            BufferLookup<Child> childLookup,
            Lookups lookups)
        {
            if (!lookups.EntityStorageInfoLookup.Exists(entity))
                return 0;

            int patched = 0;
            if (lookups.RenderBoundsLookup.HasComponent(entity))
            {
                RenderBounds bounds = lookups.RenderBoundsLookup[entity];
                float3 extents = math.max(bounds.Value.Extents, RenderBoundsMinExtents);
                if (!math.all(bounds.Value.Extents == extents))
                {
                    bounds.Value.Extents = extents;
                    ecb.SetComponent(entity, bounds);
                    patched++;
                }
            }

            if (TryResolveMeshLod(entity, lookups, out MeshLODComponent meshLod))
            {
                if (meshLod.LODMask != AlwaysVisibleLodMask)
                {
                    meshLod.LODMask = AlwaysVisibleLodMask;
                    ecb.SetComponent(entity, meshLod);
                    patched++;
                }

                patched += PatchLodGroup(ecb, meshLod.Group, lookups);
                patched += PatchLodGroup(ecb, meshLod.ParentGroup, lookups);
            }

            if (!childLookup.HasBuffer(entity))
                return patched;

            DynamicBuffer<Child> children = childLookup[entity];
            for (int i = 0; i < children.Length; i++)
                patched += EnsureRenderSafetyRecursive(ecb, children[i].Value, childLookup, lookups);

            return patched;
        }

        private static int EnsureRenderSafetyRecursive(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity entity,
            BufferLookup<Child> childLookup,
            UnitRenderBudgetLodReferences lodReferenceSystem)
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
                    ecb.SetComponent(entity, bounds);
                    patched++;
                }
            }

            if (lodReferenceSystem.TryResolveMeshLod(em, entity, out MeshLODComponent meshLod))
            {
                if (meshLod.LODMask != AlwaysVisibleLodMask)
                {
                    meshLod.LODMask = AlwaysVisibleLodMask;
                    ecb.SetComponent(entity, meshLod);
                    patched++;
                }

                patched += PatchLodGroup(em, ecb, meshLod.Group, lodReferenceSystem);
                patched += PatchLodGroup(em, ecb, meshLod.ParentGroup, lodReferenceSystem);
            }

            if (!childLookup.HasBuffer(entity))
                return patched;

            DynamicBuffer<Child> children = childLookup[entity];
            for (int i = 0; i < children.Length; i++)
                patched += EnsureRenderSafetyRecursive(em, ecb, children[i].Value, childLookup, lodReferenceSystem);

            return patched;
        }

        private static int PatchLodGroup(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity group,
            UnitRenderBudgetLodReferences lodReferenceSystem)
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
            ecb.SetComponent(group, lodGroup);
            return 1;
        }

        private static bool TryResolveMeshLod(Entity entity, Lookups lookups, out MeshLODComponent meshLod)
        {
            if (lookups.MeshLodLookup.HasComponent(entity))
            {
                meshLod = lookups.MeshLodLookup[entity];
                return true;
            }

            meshLod = default;
            return false;
        }

        private static bool TryResolveMeshLodGroup(Entity group, Lookups lookups, out MeshLODGroupComponent lodGroup)
        {
            if (group != Entity.Null &&
                lookups.EntityStorageInfoLookup.Exists(group) &&
                lookups.MeshLodGroupLookup.HasComponent(group))
            {
                lodGroup = lookups.MeshLodGroupLookup[group];
                return true;
            }

            lodGroup = default;
            return false;
        }

        private static int PatchLodGroup(
            EntityCommandBuffer ecb,
            Entity group,
            Lookups lookups)
        {
            if (!TryResolveMeshLodGroup(group, lookups, out MeshLODGroupComponent lodGroup))
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
            ecb.SetComponent(group, lodGroup);
            return 1;
        }
    }
}
