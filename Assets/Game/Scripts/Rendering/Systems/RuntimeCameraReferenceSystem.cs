using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCameraReferenceSystem
{
    private Entity _cameraReferenceEntity;

    public void SetWorldCamera(Camera camera)
    {
        if (TryGetOrCreateCameraReference(out EntityManager entityManager, out Entity entity))
            entityManager.GetComponentObject<RuntimeCameraReferenceComponent>(entity).WorldCamera = camera;
    }

    public void ClearWorldCamera()
    {
        if (TryGetCameraReference(out EntityManager entityManager, out Entity entity))
            entityManager.GetComponentObject<RuntimeCameraReferenceComponent>(entity).WorldCamera = null;
    }

    public static bool TryGetWorldCamera(EntityManager entityManager, EntityQuery cameraReferenceQuery, out Camera camera)
    {
        camera = null;
        if (cameraReferenceQuery.IsEmptyIgnoreFilter)
            return false;

        Entity entity = cameraReferenceQuery.GetSingletonEntity();
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<RuntimeCameraReferenceComponent>(entity))
            return false;

        camera = entityManager.GetComponentObject<RuntimeCameraReferenceComponent>(entity).WorldCamera;
        return camera != null;
    }

    private bool TryGetCameraReference(out EntityManager entityManager, out Entity entity)
    {
        entityManager = default;
        entity = Entity.Null;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        if (_cameraReferenceEntity != Entity.Null &&
            entityManager.Exists(_cameraReferenceEntity) &&
            entityManager.HasComponent<RuntimeCameraReferenceComponent>(_cameraReferenceEntity))
        {
            entity = _cameraReferenceEntity;
            return true;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        entity = query.GetSingletonEntity();
        _cameraReferenceEntity = entity;
        return true;
    }

    private bool TryGetOrCreateCameraReference(out EntityManager entityManager, out Entity entity)
    {
        if (TryGetCameraReference(out entityManager, out entity))
            return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        entity = entityManager.CreateEntity();
        entityManager.SetName(entity, "RuntimeCameraReference");
        entityManager.AddComponentObject(entity, new RuntimeCameraReferenceComponent());
        _cameraReferenceEntity = entity;
        return true;
    }
}
