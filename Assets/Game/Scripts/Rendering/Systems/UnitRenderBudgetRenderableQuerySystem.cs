using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetRenderableState
    {
        public struct Lookups
        {
            public EntityStorageInfoLookup EntityStorageInfoLookup;
            public EntityQueryMask RenderableEntityMask;
            public ComponentLookup<Disabled> DisabledLookup;
            public ComponentLookup<DisableRendering> DisableRenderingLookup;
            public ComponentLookup<UnitRenderBudgetCulledTag> CulledTagLookup;
            public ComponentLookup<UnitSafeVisibleCharacterLodTag> SafeVisibleCharacterLodLookup;

            public void Update(ref SystemState state)
            {
                EntityStorageInfoLookup.Update(ref state);
                DisabledLookup.Update(ref state);
                DisableRenderingLookup.Update(ref state);
                CulledTagLookup.Update(ref state);
                SafeVisibleCharacterLodLookup.Update(ref state);
            }
        }

        public bool IsRenderableVisibleRecursive(Entity entity, BufferLookup<Child> childLookup, Lookups lookups)
        {
            if (entity == Entity.Null || !lookups.EntityStorageInfoLookup.Exists(entity))
                return false;

            if (IsRenderableEntity(entity, lookups) &&
                !lookups.DisabledLookup.HasComponent(entity) &&
                !lookups.DisableRenderingLookup.HasComponent(entity) &&
                !lookups.CulledTagLookup.HasComponent(entity))
            {
                return true;
            }

            if (!childLookup.HasBuffer(entity))
                return false;

            DynamicBuffer<Child> children = childLookup[entity];
            for (int i = 0; i < children.Length; i++)
            {
                if (IsRenderableVisibleRecursive(children[i].Value, childLookup, lookups))
                    return true;
            }

            return false;
        }

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

        public bool HasRenderableRecursive(Entity entity, BufferLookup<Child> childLookup, Lookups lookups)
        {
            if (entity == Entity.Null || !lookups.EntityStorageInfoLookup.Exists(entity))
                return false;

            if (IsRenderableEntity(entity, lookups))
                return true;

            if (!childLookup.HasBuffer(entity))
                return false;

            DynamicBuffer<Child> children = childLookup[entity];
            for (int i = 0; i < children.Length; i++)
            {
                if (HasRenderableRecursive(children[i].Value, childLookup, lookups))
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

        public bool IsSafeVisibleCharacterLod(Entity entity, Lookups lookups)
        {
            return entity != Entity.Null &&
                   lookups.EntityStorageInfoLookup.Exists(entity) &&
                   lookups.SafeVisibleCharacterLodLookup.HasComponent(entity);
        }

        public bool IsSafeVisibleCharacterLod(EntityManager em, Entity entity)
        {
            return entity != Entity.Null &&
                   em.Exists(entity) &&
                   em.HasComponent<UnitSafeVisibleCharacterLodTag>(entity);
        }

        public bool IsRenderableEntity(Entity entity, Lookups lookups)
        {
            return entity != Entity.Null &&
                   lookups.EntityStorageInfoLookup.Exists(entity) &&
                   lookups.RenderableEntityMask.MatchesIgnoreFilter(entity);
        }

        public bool IsRenderableEntity(EntityManager em, Entity entity)
        {
            return em.HasComponent<RenderFilterSettings>(entity) ||
                   em.HasComponent<Unity.Rendering.RenderBounds>(entity);
        }
    }
}
