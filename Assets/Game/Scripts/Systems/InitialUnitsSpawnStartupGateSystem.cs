using Unity.Entities;

public readonly struct InitialUnitsSpawnStartupGateSystem
{
    public readonly struct Result
    {
        public readonly bool IsActionable;
        public readonly bool UseM01CompactRuntime;
        public readonly Entity BoundaryEntity;

        private Result(bool isActionable, bool useM01CompactRuntime, Entity boundaryEntity)
        {
            IsActionable = isActionable;
            UseM01CompactRuntime = useM01CompactRuntime;
            BoundaryEntity = boundaryEntity;
        }

        public static Result NotActionable()
        {
            return new Result(false, false, Entity.Null);
        }

        public static Result Actionable(bool useM01CompactRuntime, Entity boundaryEntity)
        {
            return new Result(true, useM01CompactRuntime, boundaryEntity);
        }
    }

    public Result Evaluate(EntityManager em, InitialUnitsSpawnQuerySystem.Context queryContext)
    {
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(
            queryContext.RuntimeGameplayStateQuery.GetSingletonEntity());
        if (runtimeState.PlayRequested == 0)
            return Result.NotActionable();

        bool useM01CompactRuntime = Chapter01M01PlayableRuntime.IsActiveMission();
        Entity boundaryEntity = TryGetBuildingRuntimeBoundaryEntity(em, queryContext, out Entity foundBoundaryEntity)
            ? foundBoundaryEntity
            : Entity.Null;

        return Result.Actionable(useM01CompactRuntime, boundaryEntity);
    }

    private static bool TryGetBuildingRuntimeBoundaryEntity(
        EntityManager em,
        InitialUnitsSpawnQuerySystem.Context queryContext,
        out Entity entity)
    {
        entity = Entity.Null;
        if (queryContext.BuildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = queryContext.BuildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && em.Exists(entity);
    }
}
