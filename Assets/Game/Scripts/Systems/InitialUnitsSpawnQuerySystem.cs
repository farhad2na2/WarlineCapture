using Unity.Entities;

public readonly struct InitialUnitsSpawnQuerySystem
{
    public readonly struct Context
    {
        public readonly EntityQuery BuildingRuntimeBoundaryQuery;
        public readonly EntityQuery RuntimeGameplayStateQuery;
        public readonly EntityQuery GridContextQuery;
        public readonly EntityQuery PendingInitQuery;
        public readonly EntityQuery ProgressQuery;

        public Context(
            EntityQuery buildingRuntimeBoundaryQuery,
            EntityQuery runtimeGameplayStateQuery,
            EntityQuery gridContextQuery,
            EntityQuery pendingInitQuery,
            EntityQuery progressQuery)
        {
            BuildingRuntimeBoundaryQuery = buildingRuntimeBoundaryQuery;
            RuntimeGameplayStateQuery = runtimeGameplayStateQuery;
            GridContextQuery = gridContextQuery;
            PendingInitQuery = pendingInitQuery;
            ProgressQuery = progressQuery;
        }
    }

    public Context Create(ref SystemState state)
    {
        EntityQuery buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
            ComponentType.ReadOnly<BuildingFactionProductionSpawnPointReadModel>(),
            ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());

        EntityQuery runtimeGameplayStateQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<RuntimeGameplayStateComponent>());

        EntityQuery gridContextQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());

        EntityQuery pendingInitQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>()
            }
        });

        EntityQuery progressQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>()
            }
        });

        return new Context(
            buildingRuntimeBoundaryQuery,
            runtimeGameplayStateQuery,
            gridContextQuery,
            pendingInitQuery,
            progressQuery);
    }
}
