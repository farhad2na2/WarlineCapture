using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

public readonly struct UnitRenderBudgetLodReferenceSystem
{
    public struct Lookups
    {
        public ComponentLookup<UnitDetailedVisualReference> DetailedVisualReferenceLookup;
        public ComponentLookup<UnitMidLodPrefabReference> MidLodPrefabReferenceLookup;
        public ComponentLookup<UnitMidLodInstanceReference> MidLodInstanceReferenceLookup;
        public ComponentLookup<UnitLowLodPrefabReference> LowLodPrefabReferenceLookup;
        public ComponentLookup<UnitLowLodInstanceReference> LowLodInstanceReferenceLookup;

        public void Update(ref SystemState state)
        {
            DetailedVisualReferenceLookup.Update(ref state);
            MidLodPrefabReferenceLookup.Update(ref state);
            MidLodInstanceReferenceLookup.Update(ref state);
            LowLodPrefabReferenceLookup.Update(ref state);
            LowLodInstanceReferenceLookup.Update(ref state);
        }
    }

    public readonly struct UnitReferences
    {
        public readonly bool HasDetailRoot;
        public readonly Entity DetailRoot;
        public readonly bool HasMidLodPrefab;
        public readonly bool HasMidLodInstance;
        public readonly Entity MidRoot;
        public readonly bool HasLowLodPrefab;
        public readonly bool HasLowLodInstance;
        public readonly Entity LowRoot;

        public UnitReferences(
            bool hasDetailRoot,
            Entity detailRoot,
            bool hasMidLodPrefab,
            bool hasMidLodInstance,
            Entity midRoot,
            bool hasLowLodPrefab,
            bool hasLowLodInstance,
            Entity lowRoot)
        {
            HasDetailRoot = hasDetailRoot;
            DetailRoot = detailRoot;
            HasMidLodPrefab = hasMidLodPrefab;
            HasMidLodInstance = hasMidLodInstance;
            MidRoot = midRoot;
            HasLowLodPrefab = hasLowLodPrefab;
            HasLowLodInstance = hasLowLodInstance;
            LowRoot = lowRoot;
        }

        public bool HasAnyMeshLodPrefab => HasMidLodPrefab || HasLowLodPrefab;
        public bool HasAnyMeshLodInstance => HasMidLodInstance || HasLowLodInstance;
    }

    public UnitReferences ResolveUnitReferences(Entity unit, Lookups lookups)
    {
        bool hasDetailRoot = lookups.DetailedVisualReferenceLookup.HasComponent(unit);
        Entity detailRoot = hasDetailRoot
            ? lookups.DetailedVisualReferenceLookup[unit].Root
            : Entity.Null;

        bool hasMidLodPrefab = lookups.MidLodPrefabReferenceLookup.HasComponent(unit);
        bool hasMidLodInstance = lookups.MidLodInstanceReferenceLookup.HasComponent(unit);
        Entity midRoot = hasMidLodInstance
            ? lookups.MidLodInstanceReferenceLookup[unit].Instance
            : Entity.Null;

        bool hasLowLodPrefab = lookups.LowLodPrefabReferenceLookup.HasComponent(unit);
        bool hasLowLodInstance = lookups.LowLodInstanceReferenceLookup.HasComponent(unit);
        Entity lowRoot = hasLowLodInstance
            ? lookups.LowLodInstanceReferenceLookup[unit].Instance
            : Entity.Null;

        return new UnitReferences(
            hasDetailRoot,
            detailRoot,
            hasMidLodPrefab,
            hasMidLodInstance,
            midRoot,
            hasLowLodPrefab,
            hasLowLodInstance,
            lowRoot);
    }

    public UnitReferences ResolveUnitReferences(EntityManager em, Entity unit)
    {
        bool hasDetailRoot = em.HasComponent<UnitDetailedVisualReference>(unit);
        Entity detailRoot = hasDetailRoot
            ? em.GetComponentData<UnitDetailedVisualReference>(unit).Root
            : Entity.Null;

        bool hasMidLodPrefab = em.HasComponent<UnitMidLodPrefabReference>(unit);
        bool hasMidLodInstance = em.HasComponent<UnitMidLodInstanceReference>(unit);
        Entity midRoot = hasMidLodInstance
            ? em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance
            : Entity.Null;

        bool hasLowLodPrefab = em.HasComponent<UnitLowLodPrefabReference>(unit);
        bool hasLowLodInstance = em.HasComponent<UnitLowLodInstanceReference>(unit);
        Entity lowRoot = hasLowLodInstance
            ? em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance
            : Entity.Null;

        return new UnitReferences(
            hasDetailRoot,
            detailRoot,
            hasMidLodPrefab,
            hasMidLodInstance,
            midRoot,
            hasLowLodPrefab,
            hasLowLodInstance,
            lowRoot);
    }

    public bool TryResolveMeshLod(EntityManager em, Entity entity, out MeshLODComponent meshLod)
    {
        if (em.HasComponent<MeshLODComponent>(entity))
        {
            meshLod = em.GetComponentData<MeshLODComponent>(entity);
            return true;
        }

        meshLod = default;
        return false;
    }

    public bool TryResolveMeshLodGroup(EntityManager em, Entity group, out MeshLODGroupComponent lodGroup)
    {
        if (group != Entity.Null && em.Exists(group) && em.HasComponent<MeshLODGroupComponent>(group))
        {
            lodGroup = em.GetComponentData<MeshLODGroupComponent>(group);
            return true;
        }

        lodGroup = default;
        return false;
    }
}
