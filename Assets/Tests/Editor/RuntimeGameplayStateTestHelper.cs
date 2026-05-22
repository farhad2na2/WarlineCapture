#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Unity.Entities;

internal static class RuntimeGameplayStateTestHelper
{
    public static void SetPlayRequested(EntityManager entityManager, bool playRequested)
    {
        InitialUnitsRuntimeState.PlayRequested = playRequested;
        Entity entity = GetOrCreateRuntimeStateEntity(entityManager);
        RuntimeGameplayStateComponent state = entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
        state.PlayRequested = playRequested ? (byte)1 : (byte)0;
        entityManager.SetComponentData(entity, state);
    }

    public static void SetBuildingPlacement(EntityManager entityManager, BuildingPlacementSystem buildingPlacement)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
        Entity entity = query.CalculateEntityCount() > 0
            ? query.GetSingletonEntity()
            : entityManager.CreateEntity();

        if (!entityManager.HasComponent<BuildingPlacementRuntimeComponent>(entity))
            entityManager.AddComponentObject(entity, new BuildingPlacementRuntimeComponent());

        BuildingPlacementRuntimeComponent runtime = entityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity);
        runtime.BuildingPlacement = buildingPlacement;
    }

    private static Entity GetOrCreateRuntimeStateEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
        if (query.CalculateEntityCount() > 0)
        {
            Entity entity = query.GetSingletonEntity();
            EnsureCameraComponents(entityManager, entity);
            return entity;
        }

        return entityManager.CreateEntity(
            typeof(RuntimeGameplayStateComponent),
            typeof(RuntimeCameraInputComponent),
            typeof(RuntimeCameraFocusRequestComponent));
    }

    private static void EnsureCameraComponents(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
            entityManager.AddComponent<RuntimeCameraInputComponent>(entity);
        if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
            entityManager.AddComponent<RuntimeCameraFocusRequestComponent>(entity);
    }
}
#endif
