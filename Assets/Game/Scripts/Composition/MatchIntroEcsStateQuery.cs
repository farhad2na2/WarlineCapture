using Unity.Entities;

internal sealed class MatchIntroEcsStateQuery : IMatchIntroStateQuery
{
    private World cachedWorld;
    private EntityQuery query;
    private bool hasQuery;

    public bool IsGameplayInputLocked()
    {
        return TryReadState(out MatchIntroTransitionComponent state) && state.InputLocked != 0;
    }

    public bool IsIntroComplete()
    {
        return !TryReadState(out MatchIntroTransitionComponent state) ||
               state.State == MatchIntroTransitionStateKind.Complete &&
               state.InputLocked == 0;
    }

    public void Reset()
    {
        cachedWorld = null;
        query = default;
        hasQuery = false;
    }

    private bool TryReadState(out MatchIntroTransitionComponent state)
    {
        state = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasQuery)
        {
            cachedWorld = world;
            query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            hasQuery = true;
        }

        if (query.IsEmptyIgnoreFilter)
            return false;

        state = world.EntityManager.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
        return true;
    }
}
