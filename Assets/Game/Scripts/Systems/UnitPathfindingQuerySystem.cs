using Unity.Entities;

internal struct UnitPathfindingQuerySystem
{
    public EntityQuery GridQuery;
    public EntityQuery RequestQuery;
    public EntityQuery LiveUnitsQuery;
    public EntityQuery PendingManualMoveQuery;
    public EntityQuery PathFollowQuery;
    public EntityQuery LongDistanceMoveQuery;
    public EntityQuery RetryCooldownQuery;
    public EntityQuery ManualRequestQuery;
    public EntityQuery ManualPathFollowQuery;
    public EntityQuery MapSurfaceQuery;

    public void Initialize(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<DynamicBlockerComponent>();
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<GridRoad>();
        state.RequireForUpdate<GridRoadSidewalk>();
        state.RequireForUpdate<GridRoadDirt>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();

        GridQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>(),
            }
        });
        RequestQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitPathRequest>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<Faction>(),
            }
        });
        LiveUnitsQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });
        PendingManualMoveQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitTarget>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<EngageTarget>(),
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        PathFollowQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathFollow>(),
                ComponentType.ReadOnly<UnitPathRange>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        LongDistanceMoveQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitLongDistanceMove>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        RetryCooldownQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathRetryCooldown>(),
            }
        });
        ManualRequestQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathRequest>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        ManualPathFollowQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathFollow>(),
                ComponentType.ReadOnly<UnitPathRange>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        MapSurfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
    }
}
