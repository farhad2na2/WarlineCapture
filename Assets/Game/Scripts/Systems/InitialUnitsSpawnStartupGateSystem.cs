using Unity.Entities;

public readonly struct InitialUnitsSpawnStartupGateSystem
{
    public readonly struct Result
    {
        public readonly bool IsActionable;
        public readonly Entity BoundaryEntity;

        private Result(bool isActionable, Entity boundaryEntity)
        {
            IsActionable = isActionable;
            BoundaryEntity = boundaryEntity;
        }

        public static Result NotActionable()
        {
            return new Result(false, Entity.Null);
        }

        public static Result Actionable(Entity boundaryEntity)
        {
            return new Result(true, boundaryEntity);
        }
    }

    public Result Evaluate(EntityManager em, InitialUnitsSpawnQuerySystem.Context queryContext)
    {
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(
            queryContext.RuntimeGameplayStateQuery.GetSingletonEntity());
        if (runtimeState.PlayRequested == 0)
            return Result.NotActionable();

        Entity boundaryEntity = TryGetBuildingRuntimeBoundaryEntity(em, queryContext, out Entity foundBoundaryEntity)
            ? foundBoundaryEntity
            : Entity.Null;

        return Result.Actionable(boundaryEntity);
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
