using Unity.Entities;

public sealed class PerformanceDiagnosticsReferenceSystem
{
    private Entity _referenceEntity;

    public void Register(PerformanceDiagnosticsSystem diagnostics)
    {
        if (diagnostics == null || !TryGetOrCreateReference(out EntityManager entityManager, out Entity entity))
            return;

        entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity).Diagnostics = diagnostics;
    }

    public void Clear(PerformanceDiagnosticsSystem diagnostics)
    {
        if (!TryGetReference(out EntityManager entityManager, out Entity entity))
            return;

        PerformanceDiagnosticsReferenceComponent reference =
            entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity);
        if (diagnostics == null || reference.Diagnostics == diagnostics)
            reference.Diagnostics = null;
    }

    public bool TryGet(World world, out PerformanceDiagnosticsSystem diagnostics)
    {
        diagnostics = null;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PerformanceDiagnosticsReferenceComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        if (!entityManager.Exists(entity) ||
            !entityManager.HasComponent<PerformanceDiagnosticsReferenceComponent>(entity))
        {
            return false;
        }

        diagnostics = entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity).Diagnostics;
        return diagnostics != null;
    }

    private bool TryGetReference(out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        if (_referenceEntity != Entity.Null &&
            entityManager.Exists(_referenceEntity) &&
            entityManager.HasComponent<PerformanceDiagnosticsReferenceComponent>(_referenceEntity))
        {
            entity = _referenceEntity;
            return true;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PerformanceDiagnosticsReferenceComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        entity = query.GetSingletonEntity();
        _referenceEntity = entity;
        return true;
    }

    private bool TryGetOrCreateReference(out EntityManager entityManager, out Entity entity)
    {
        if (TryGetReference(out entityManager, out entity))
            return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        entity = entityManager.CreateEntity();
        entityManager.SetName(entity, "PerformanceDiagnosticsReference");
        entityManager.AddComponentObject(entity, new PerformanceDiagnosticsReferenceComponent());
        _referenceEntity = entity;
        return true;
    }
}
