using Unity.Entities;
using UnityEngine.SceneManagement;

public sealed class MatchSceneReferenceSystem
{
    private Entity _referenceEntity;

    public void Register(MatchSceneView view)
    {
        if (view == null || !TryGetOrCreateReference(out EntityManager entityManager, out Entity entity))
            return;

        entityManager.GetComponentObject<MatchSceneReferenceComponent>(entity).View = view;
    }

    public void Clear(MatchSceneView view)
    {
        if (!TryGetReference(out EntityManager entityManager, out Entity entity))
            return;

        MatchSceneReferenceComponent reference =
            entityManager.GetComponentObject<MatchSceneReferenceComponent>(entity);
        if (view == null || reference.View == view)
            reference.View = null;
    }

    public bool TryGetLoadedMatchSceneView(World world, out MatchSceneView view)
    {
        view = null;
        if (!TryGetReference(world, out EntityManager entityManager, out Entity entity))
            return false;

        MatchSceneView candidate = entityManager.GetComponentObject<MatchSceneReferenceComponent>(entity).View;
        if (!IsLoadedMatchSceneView(candidate))
            return false;

        view = candidate;
        return true;
    }

    private bool TryGetReference(out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return TryGetReference(entityManager, out entity);
    }

    private bool TryGetReference(World world, out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;

        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return TryGetReference(entityManager, out entity);
    }

    private bool TryGetReference(EntityManager entityManager, out Entity entity)
    {
        entity = Entity.Null;
        if (_referenceEntity != Entity.Null &&
            entityManager.Exists(_referenceEntity) &&
            entityManager.HasComponent<MatchSceneReferenceComponent>(_referenceEntity))
        {
            entity = _referenceEntity;
            return true;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MatchSceneReferenceComponent>());
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
        entityManager.SetName(entity, "MatchSceneReference");
        entityManager.AddComponentObject(entity, new MatchSceneReferenceComponent());
        _referenceEntity = entity;
        return true;
    }

    private static bool IsLoadedMatchSceneView(MatchSceneView view)
    {
        if (view == null || view.gameObject == null)
            return false;

        Scene scene = view.gameObject.scene;
        return scene.IsValid() &&
               scene.isLoaded &&
               scene.name == SceneLifecycleSystem.MatchSceneName;
    }
}
