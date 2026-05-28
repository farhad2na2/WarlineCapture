using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

public readonly struct UnitRenderBudgetLodReferenceSystem
{
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
