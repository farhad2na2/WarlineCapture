using Unity.Entities;
using Unity.Transforms;

public readonly struct UnitRenderBudgetSources
{
    public readonly struct Context
    {
        public readonly EntityQuery UnitQuery;
        public readonly EntityQuery AllUnitGridQuery;
        public readonly EntityQuery SpawnConfigQuery;
        public readonly EntityQuery SpawnProgressQuery;
        public readonly EntityQuery SpawnInitializedQuery;
        public readonly EntityQuery CameraReferenceQuery;

        public Context(
            EntityQuery unitQuery,
            EntityQuery allUnitGridQuery,
            EntityQuery spawnConfigQuery,
            EntityQuery spawnProgressQuery,
            EntityQuery spawnInitializedQuery,
            EntityQuery cameraReferenceQuery)
        {
            UnitQuery = unitQuery;
            AllUnitGridQuery = allUnitGridQuery;
            SpawnConfigQuery = spawnConfigQuery;
            SpawnProgressQuery = spawnProgressQuery;
            SpawnInitializedQuery = spawnInitializedQuery;
            CameraReferenceQuery = cameraReferenceQuery;
        }
    }

    public Context Create(ref SystemState state)
    {
        EntityQuery unitQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<Disabled>(),
            }
        });
        EntityQuery allUnitGridQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitGrid>());
        EntityQuery spawnConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery spawnProgressQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
        EntityQuery spawnInitializedQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        EntityQuery cameraReferenceQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());

        return new Context(
            unitQuery,
            allUnitGridQuery,
            spawnConfigQuery,
            spawnProgressQuery,
            spawnInitializedQuery,
            cameraReferenceQuery);
    }
}
